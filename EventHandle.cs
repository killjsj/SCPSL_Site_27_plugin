using AutoEvent;
using AutoEvent.Commands;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.RemoteAdmin.Dummies;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Toys;
using Exiled.Events.Commands.Reload;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Features;
using Google.Protobuf.WellKnownTypes;
using InventorySystem.Configs;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Keycards.Snake;
using InventorySystem.Items.MicroHID;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP;
using PlayerRoles;
using PlayerStatsSystem;
using ProjectMER.Commands.Modifying.Position;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using YamlDotNet.Core.Tokens;
using static RoundSummary;
using Player = Exiled.API.Features.Player;
namespace Next_generationSite_27.UnionP
{
    class EventHandle
    {

        PConfig Config = null;
        public Dictionary<Player, CoroutineHandle> cs = new Dictionary<Player, CoroutineHandle>();
        public Dictionary<Player, Stopwatch> BroadcastTime = new Dictionary<Player, Stopwatch>();
        public Dictionary<ushort, Player> snakepairs = new Dictionary<ushort, Player>();
        public Dictionary<Player, int> cachedHighestPairs = new Dictionary<Player, int>();
        MySQLConnect MysqlConnect = Plugin.plugin.connect;
        public (string userid, string name, int? highscore, DateTime? time) cachedHighest = (string.Empty, string.Empty, null, DateTime.MinValue);
        public void ChangedItem(ChangedItemEventArgs ev)
        {
            if (ev.OldItem != null)
            {
                snakepairs.Remove(ev.OldItem.Serial);
            }

        }
        public void InspectedKeycard(PlayerInspectedKeycardEventArgs ev)
        {
            if (ev.KeycardItem.Base is ChaosKeycardItem chaos)
            {
                if (!snakepairs.ContainsKey(chaos.ItemSerial))
                {
                    snakepairs.Add(chaos.ItemSerial, ev.Player);
                }

            }
        }
        public void OnSnakeMovementDirChanged(ushort? Nid, Vector2Int Head)
        {
            if (Nid != null)
            {

                ushort id = Nid.Value;
                if (snakepairs.TryGetValue(id, out var player))
                {

                    var SE = ChaosKeycardItem.SnakeSessions[id];
                    if (MysqlConnect.connected)
                    {
                        if (SE != null)
                        {

                            if (!cachedHighestPairs.ContainsKey(player))
                            {
                                var highscore = MysqlConnect.Query(player.UserId).highscore;
                                if (highscore != null)
                                {
                                    cachedHighestPairs.Add(player, highscore.Value);
                                }
                                else
                                {
                                    cachedHighestPairs.Add(player, 0);
                                }
                            }
                            if (!cachedHighest.highscore.HasValue)
                            {
                                cachedHighest = MysqlConnect.QueryHighest();
                                if (!cachedHighest.highscore.HasValue)
                                {
                                    cachedHighest.highscore = 0;
                                }
                            }
                            if (cachedHighestPairs[player] < SE.Score)
                            {
                                cachedHighestPairs[player] = SE.Score;
                                if (SE.Score > cachedHighest.highscore)
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast()
                                    {
                                        Content = $"<size=15>恭喜你更新服务器最高分:{cachedHighestPairs[player]}",
                                        Duration = 1
                                    });
                                    cachedHighest = (player.UserId, player.DisplayNickname, SE.Score, DateTime.Now);
                                }
                                else
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast()
                                    {
                                        Content = $"<size=15>恭喜你更新个人最高分:{cachedHighestPairs[player]}",
                                        Duration = 1
                                    });
                                }
                            }
                        }
                    }
                }

            }
        }
        public void RestartingRound()
        {
            update();
            stopBroadcast();
        }
        public void update()
        {
            var MysqlConnect = Plugin.plugin.connect;

            if (cachedHighestPairs != null)
            {
                foreach (var item in cachedHighestPairs)
                {
                    MysqlConnect.Update(item.Key.UserId, item.Key.Nickname, item.Value, DateTime.Now);
                }
            }
        }
        public EventHandle(PConfig config)
        {
            Config = config;
        }
        public void Generated()
        {
            InventoryLimits.StandardCategoryLimits[ItemCategory.SpecialWeapon] = (sbyte)Config.MaxSpecialWeaponLimit;
            ServerConfigSynchronizer.Singleton.RefreshCategoryLimits();
        }
        // 添加字段来跟踪受保护的玩家
        public Dictionary<Player, CoroutineHandle> ProtectionCoroutines = new Dictionary<Player, CoroutineHandle>();
        public Dictionary<Player, Stopwatch> BroadcastTimers = new Dictionary<Player, Stopwatch>();
        public bool RoundEnded
        {
            get
            {

                return Round.IsEnded;
            }
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (RoundEnded) return;

            if (ev.Wave is NtfMiniWave || ev.Wave is NtfSpawnWave ||
                ev.Wave is ChaosMiniWave || ev.Wave is ChaosSpawnWave)
            {
                foreach (var player in ev.Players)
                {
                    ApplySpawnProtection(player);
                }
            }
        }

        private void ApplySpawnProtection(Player player)
        {
            try
            {
                // 先清理旧的保护
                if (ProtectionCoroutines.ContainsKey(player))
                {
                    Timing.KillCoroutines(ProtectionCoroutines[player]);
                    ProtectionCoroutines.Remove(player);
                }

                // 应用保护效果
                player.EnableEffect(EffectType.SpawnProtected, SpawnProtected.SpawnDuration);

                // 启动保护文本更新协程
                var coroutine = ProtectTextUpdate(player);
                var handle = MEC.Timing.RunCoroutine(coroutine);

                ProtectionCoroutines[player] = handle;

                Log.Info($"[出生保护] 为玩家 {player.Nickname} 应用保护，持续 {SpawnProtected.SpawnDuration} 秒");
            }
            catch (Exception ex)
            {
                Log.Error($"[出生保护] 为玩家 {player.Nickname} 应用保护时出错: {ex.Message}");
            }
        }

        public IEnumerator<float> ProtectTextUpdate(Player player) // AI太好用了你们知道吗
        {

            // 检查玩家有效性
            if (player == null || !player.IsConnected)
                yield break;

            var spawnProtectedEffect = player.GetEffect(EffectType.SpawnProtected);
            if (spawnProtectedEffect == null)
                yield break;
            //Log.Info(spawnProtectedEffect.TimeLeft);
            while (spawnProtectedEffect.TimeLeft > 0)
            {
                try
                {
                    // 检查玩家是否仍然有效
                    if (player == null || !player.IsConnected || !player.IsAlive)
                    {
                        if (!player.IsAlive)
                        {
                            player.DisableEffect(EffectType.SpawnProtected);

                        }
                        break;
                    }
                    var remainingTime = spawnProtectedEffect.TimeLeft;
                    var text = $"<size=27><color=#{Config.InProtectColor}>🔰刷新保护剩余 {remainingTime:F0} 秒🔰\n开枪将取消保护</color></size>";

                    player.Broadcast(new Exiled.API.Features.Broadcast()
                    {
                        Content = text,
                        Duration = 1
                    }, true);
                }
                catch (Exception ex)
                {
                    Log.Error($"[出生保护] 文本更新出错: {ex.Message}");
                }

                yield return MEC.Timing.WaitForSeconds(1f);
            }

            // 保护结束，清理字典
            if (player != null && ProtectionCoroutines.ContainsKey(player))
            {
                ProtectionCoroutines.Remove(player);
            }

            Log.Info($"[出生保护] 玩家 {player?.Nickname ?? "Unknown"} 保护结束");
        }


        public void Shot(ShotEventArgs ev)
        {
            if (ev.Player.GetEffect<SpawnProtected>() != null)
            {
                var a = ev.Player.GetEffect<SpawnProtected>();
                if (Config.NoProtectWhenShoot && (ProtectionCoroutines.ContainsKey(ev.Player)))
                {
                    try
                    {
                        // 移除保护效果
                        ev.Player.GetEffect<SpawnProtected>().TimeLeft = 0;
                        ev.Player.DisableEffect(EffectType.SpawnProtected);
                        a.ServerDisable();

                        // 停止保护协程
                        if (ProtectionCoroutines.TryGetValue(ev.Player, out var handle))
                        {
                            Timing.KillCoroutines(handle);
                            ProtectionCoroutines.Remove(ev.Player);
                        }

                        // 显示取消保护提示
                        ShowProtectionCancelledMessage(ev.Player);
                        Log.Info($"[出生保护] 玩家 {ev.Player.Nickname} 因开枪取消保护");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[出生保护] 取消保护时出错: {ex.Message}");
                    }
                }
                else
                {
                    if (a.Intensity != 0)
                    {
                        Log.Info($"[出生保护] 玩家 {ev.Player.Nickname} 因开枪取消保护");
                        a.ServerDisable();
                    }
                }
            }
        }

        private void ShowProtectionCancelledMessage(Player player)
        {
            var text = $"<size=27><color=#{Config.OutProtectColor}>保护已取消 - 因开枪</color></size>";
            player.Broadcast(new Exiled.API.Features.Broadcast()
            {
                Content = text,
                Duration = 3
            }, true);
            if (ProtectionCoroutines.ContainsKey(player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[player]);
                ProtectionCoroutines.Remove(player);
            }
        }

        // 在玩家断开连接时清理资源
        public void OnPlayerLeave(LeftEventArgs ev)
        {
            if (ProtectionCoroutines.ContainsKey(ev.Player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[ev.Player]);
                ProtectionCoroutines.Remove(ev.Player);
            }

            if (BroadcastTimers.ContainsKey(ev.Player))
            {
                BroadcastTimers.Remove(ev.Player);
            }
        }

        // 在回合结束时清理所有保护
        public void OnRoundEnd(RoundEndedEventArgs ev)
        {
            // 清理所有保护协程
            foreach (var handle in ProtectionCoroutines.Values)
            {
                Timing.KillCoroutines(handle);
            }
            ProtectionCoroutines.Clear();
            Plugin.plugin.scpChangeReqs = new List<ScpChangeReq>();

            Plugin.plugin.superSCP.stop();

            // 清理计时器
            BroadcastTimers.Clear();
        }

        public void RoundStarted()
        {
            if (Player.List.Count >= Config.EnableSuperScpCount && Config.EnableSuperScp)
            {
                Plugin.enableSSCP = true;
                Plugin.plugin.superSCP.start();

            }
        }

        public void SentValidCommand(SentValidCommandEventArgs ev)
        {
            if (ev.Player.RemoteAdminAccess)
            {
                MysqlConnect.LogAdminPermission(ev.Player.UserId, ev.Player.DisplayNickname, Server.Port, ev.Query, ev.Response, group: ev.Player.Group.Name);
            }
        }
        public Dictionary<ReferenceHub, List<(EffectType, byte, float)>> effects = new Dictionary<ReferenceHub, List<(EffectType, byte, float)>>();
        public void Escaped(EscapedEventArgs ev)
        {
            Log.Info($"{ev.Player}成功撤离 时间:{ev.EscapeTime}");
            if (effects.ContainsKey(ev.Player.ReferenceHub))
            {
                foreach (var item in effects[ev.Player.ReferenceHub])
                {
                    if (ev.Player.TryGetEffect(item.Item1, out var statusEffect))
                    {
                        ev.Player.EnableEffect(statusEffect, item.Item2, item.Item3, false);
                        ev.Player.ReferenceHub.playerEffectsController.ServerSyncEffect(statusEffect);

                    }


                    Log.Info($"对{ev.Player}施加了效果:{item} Intensity:{item.Item2} 撤离");
                }
                effects.Remove(ev.Player.ReferenceHub);

            }
        }
        public void Escaping(EscapingEventArgs ev)
        {
            if (effects.ContainsKey(ev.Player.ReferenceHub))
            {
                effects[ev.Player.ReferenceHub].Clear();
                foreach (var item in ev.Player.ActiveEffects)
                {
                    if (item.GetEffectType() == EffectType.Scp1344)
                    {
                        continue;
                    }
                    effects[ev.Player.ReferenceHub].Add((item.GetEffectType(), item.Intensity, item.Duration));

                }
            }
            else
            {
                effects.Add(ev.Player.ReferenceHub, new List<(EffectType, byte, float)>());
                foreach (var item in ev.Player.ActiveEffects)
                {
                    if (item.GetEffectType() == EffectType.Scp1344)
                    {
                        continue;
                    }
                    effects[ev.Player.ReferenceHub].Add((item.GetEffectType(), item.Intensity, item.Duration));

                }
            }
        }
        public void RespawningTeam(RespawningTeamEventArgs ev)
        {
            SpawnableWaveBase newW = ev.Wave.Base;
            List<ReferenceHub> players = new List<ReferenceHub>();
            Log.Info($"RespawningTeam IsMiniWave {ev.Wave.IsMiniWave}");

            if (ev.Wave.IsMiniWave)
            {
                ev.IsAllowed = false;

                switch (ev.Wave.Faction)
                {
                    case PlayerRoles.Faction.FoundationStaff:
                        {
                            newW = new NtfSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);
                            break;
                        }
                    case PlayerRoles.Faction.FoundationEnemy:
                        {
                            newW = new ChaosSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);


                            break;
                        }
                }

                ev.Wave.Timer.SetTime(0);
                RespawnedTeam(new RespawnedTeamEventArgs(newW, players));

            }
        }
        public CoroutineHandle BroadcasterHandler;
        public List<GameObject> SPC = new List<GameObject>();
        public List<ReferenceHub> SPD = new List<ReferenceHub>();
        public void assing()
        {
            foreach (ReferenceHub obj in SPD)
            {
                NetworkServer.Destroy(obj.gameObject);
            }
            foreach (GameObject p in Plugin.SOB.AttachedBlocks)
            {
                if (p.name == "DDP")
                {
                    SPC.Add(p);
                    var dd = p.GetComponent<coH>();
                    dd.PlayerEnter -= Dd_PlayerEnter;
                }
                if (p.name == "SCIP")
                {
                    SPC.Add(p);
                    var SCI = p.GetComponent<coH>();
                    SCI.PlayerEnter -= SCI_PlayerEnter;
                }
                if (p.name == "SCPP")
                {
                    SPC.Add(p);
                    var SCP = p.GetComponent<coH>();
                    SCP.PlayerEnter -= SCP_PlayerEnter;
                }
                if (p.name == "GRP")
                {
                    SPC.Add(p);
                    var GR = p.GetComponent<coH>();
                    GR.PlayerEnter -= GR_PlayerEnter;
                }
            }
        }
        public void WaitingForPlayers()
        {
            StopBroadcast = false;
            Plugin.enableSSCP = false;
            PrefabManager.RegisterPrefabs();
            var ss = new SerializableSchematic
            {
                SchematicName = "SpawnRoom",
                Position = new Vector3(0, 290, -90)
            };
            GameObject gameObject = ss.SpawnOrUpdateObject();
            Plugin.SOB = gameObject.GetComponent<SchematicObject>();
            //Log.Info($"outside {Exiled.API.Features.Room.Get(RoomType.Surface).Position}");
            foreach (GameObject p in Plugin.SOB.AttachedBlocks)
            {
                //Log.Info("2.3");

                //var p = tp.gameObject;
                //Log.Info("2.4");

                if (p != null && p.name != null)
                {
                    Log.Info(p.name);
                    if (p.name == "DDP")
                    {
                        SPC.Add(p);
                        var dd = p.AddComponent<coH>();
                        if (dd != null)
                        {
                            dd.PlayerEnter += Dd_PlayerEnter;
                        }
                    }
                    if (p.name == "SCIP")
                    {
                        SPC.Add(p);
                        var SCI = p.AddComponent<coH>();
                        if (SCI != null)
                        {
                            SCI.PlayerEnter += SCI_PlayerEnter;
                        }
                    }
                    if (p.name == "SCPP")
                    {
                        SPC.Add(p);
                        var SCP = p.AddComponent<coH>();
                        if (SCP != null)
                        {
                            SCP.PlayerEnter += SCP_PlayerEnter;
                        }
                    }
                    if (p.name == "GRP")
                    {
                        SPC.Add(p);
                        var GR = p.AddComponent<coH>();
                        if (GR != null)
                        {
                            GR.PlayerEnter += GR_PlayerEnter;
                        }
                    }
                    if (p.name == "DD")
                    {
                        var r = DummyUtils.SpawnDummy("选择当DD");
                        r.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
                        var pl = Player.Get(r);
                        pl.Position = p.transform.position;
                        pl.Rotation = p.transform.rotation;
                                                                        r.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.GodMode, true);
                        SPD.Add(r);
                    }
                    if (p.name == "SCI")
                    {
                        var r = DummyUtils.SpawnDummy("选择当科学");
                        r.roleManager.ServerSetRole(RoleTypeId.Scientist, RoleChangeReason.RoundStart);
                        var pl = Player.Get(r);
                        pl.Position = p.transform.position;
                        pl.Rotation = p.transform.rotation;
                                                                        r.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.GodMode, true);
                        SPD.Add(r);
                    }
                    if (p.name == "SCP")
                    {
                        var r = DummyUtils.SpawnDummy("选择当SCP");
                        r.roleManager.ServerSetRole(RoleTypeId.Scp939, RoleChangeReason.RoundStart);
                        var pl = Player.Get(r);
                        pl.Position = p.transform.position;
                        pl.Rotation = p.transform.rotation;
                                                r.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.GodMode, true);
                        SPD.Add(r);
                    }
                    if (p.name == "GR")
                    {
                        var r = DummyUtils.SpawnDummy("选择当保安");
                        r.roleManager.ServerSetRole(RoleTypeId.FacilityGuard, RoleChangeReason.RoundStart);
                        var pl = Player.Get(r);
                        pl.Position = p.transform.position;
                        pl.Rotation = p.transform.rotation;
                        r.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.GodMode, true);
                        SPD.Add(r);
                    }
                }

                //Log.Info("3");
            }
            BroadcasterHandler = MEC.Timing.RunCoroutine(Broadcaster());

        }
        public Dictionary<RoleTypeId, List<ReferenceHub>> targetRole = new Dictionary<RoleTypeId, List<ReferenceHub>>() {
            {RoleTypeId.Scientist ,new List<ReferenceHub>()},
            {RoleTypeId.Scp939 ,new List<ReferenceHub>()},
            {RoleTypeId.FacilityGuard ,new List<ReferenceHub>()},
            {RoleTypeId.ClassD ,new List<ReferenceHub>()}
        };
        void GR_PlayerEnter(Player pl)
        {
            targetRole[RoleTypeId.FacilityGuard].Add(pl.ReferenceHub);
            targetRole[RoleTypeId.Scp939].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.Scientist].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.ClassD].Remove(pl.ReferenceHub);
        }

        void SCP_PlayerEnter(Player pl)
        {
            targetRole[RoleTypeId.Scp939].Add(pl.ReferenceHub);
            targetRole[RoleTypeId.FacilityGuard].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.Scientist].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.ClassD].Remove(pl.ReferenceHub);
        }

        void SCI_PlayerEnter(Player pl)
        {
            targetRole[RoleTypeId.Scientist].Add(pl.ReferenceHub);
            targetRole[RoleTypeId.Scp939].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.FacilityGuard].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.ClassD].Remove(pl.ReferenceHub);
        }

        void Dd_PlayerEnter(Player pl)
        {
            targetRole[RoleTypeId.ClassD].Add(pl.ReferenceHub);
            targetRole[RoleTypeId.Scp939].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.FacilityGuard].Remove(pl.ReferenceHub);
            targetRole[RoleTypeId.ClassD].Remove(pl.ReferenceHub);
        }

        public void stopBroadcast()
        {
            StopBroadcast = true;

            MEC.Timing.KillCoroutines(BroadcasterHandler);


        }
        public bool StopBroadcast = false;
        public int BroadcastIndex = 0;
        public int BroadcastCounter = 0;
        public IEnumerator<float> Broadcaster()
        {
            for (; ; )
            {
                BroadcastCounter++;

                if (BroadcastCounter <= Config.BroadcastWaitTime)
                {
                    yield return MEC.Timing.WaitForSeconds(1);

                }
                else
                {
                    BroadcastCounter = 0;
                    foreach (var item in Player.List)
                    {
                        item.Broadcast(new Exiled.API.Features.Broadcast()
                        {
                            Content = $"<size={Config.BroadcastSize}><color={Config.BroadcastColor}>{Config.BroadcastContext[BroadcastIndex]}</color></size>",
                            Duration = (ushort)Config.BroadcastShowTime
                        });
                    }

                    BroadcastIndex++;
                    BroadcastIndex %= Config.BroadcastContext.Count;
                }

            }
        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (cs.TryGetValue(ev.Player, out var CH))
            {
                ev.Player.DisableEffect(EffectType.SpawnProtected);
                MEC.Timing.KillCoroutines(CH);
                cs.Remove(ev.Player);
            }
            if (Plugin.plugin.superSCP.PatchedPlayers.Contains(ev.Player))
            {
                Plugin.plugin.superSCP.PatchedPlayers.Remove(ev.Player);
            }
        }
        public void EndingRound(EndingRoundEventArgs ev)
        {

            foreach (var item in cs)
            {
                item.Key.DisableEffect(EffectType.SpawnProtected);
                MEC.Timing.KillCoroutines(item.Value);
                cs.Remove(item.Key);
            }
        }

        public void ChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
        {
        }
        public void Joined(JoinedEventArgs ev)
        {
            //if(ev.Player.UserId)
        }
    }
    public class coH : MonoBehaviour
    {
        public delegate void onplayerenter(Player pl);
        public event onplayerenter PlayerEnter;

        void OnCollisionEnter(Collision collision)
        {
            try
            {

                if (collision == null)
                {
                    Log.Error("wat");
                }

                if (!collision.collider)
                {
                    Log.Error("water");
                }

                if (collision.collider.gameObject == null)
                {
                    Log.Error("pepehm");
                }

                if (Player.Get(collision.collider) != null)
                {
                    PlayerEnter.Invoke(Player.Get(collision.collider));
                }


            }
            catch (Exception arg)
            {
                Log.Error(string.Format("{0} error:\n{1}", "OnCollisionEnter", arg));
                UnityEngine.Object.Destroy(this);
            }
        }
    }
}
namespace GwangjuRunningManLoader
{
    public class EventHandler
    {
        RunningMan _plugin;
        public EventHandler(RunningMan plugin)
        {
            _plugin = plugin;
        }
        public void died(DiedEventArgs ev)
        {
            if (ev.TargetOldRole == PlayerRoles.RoleTypeId.NtfCaptain || _plugin.Jailor.Contains(ev.Player))
            {
                return;
            }
            if (_plugin.Deaths.TryGetValue(ev.Player, out int v))
            {
                if (v == _plugin.Config.PrisonerLives - 1)
                {
                    return;
                }
                else
                {
                    _plugin.Deaths[ev.Player] += 1;
                }
            }
            else
            {
                _plugin.Deaths[ev.Player] = 1;
            }
            ev.Player.GiveLoadout(_plugin.Config.PrisonerLoadouts);
            ev.Player.Position = _plugin.SpawnPoints.Where(r => r.name == "Spawnpoint").ToList().RandomItem().transform.position;
            ev.Player.Health = 100;

        }
        //public void Hit(HurtEventArgs ev)
        //{
        //    if (ev.Attacker.IsNTF && ev.Attacker.CurrentItem.Category == ItemCategory.SpecialWeapon)
        //    {
        //        ev.Attacker.RemoveItem(ev.Attacker.CurrentItem);

        //        ev.Attacker.CurrentItem = ev.Attacker.AddItem(ItemType.Jailbird);
        //    }
        //}
        public void OnChargingJailbird(ChargingJailbirdEventArgs ev)
        {
            ev.IsAllowed = false;
        }
    }
}