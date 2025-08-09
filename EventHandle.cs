using AutoEvent;
using CommandSystem.Commands.RemoteAdmin;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Features;
using InventorySystem.Configs;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Keycards.Snake;
using InventorySystem.Items.MicroHID;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using Next_generationSite_27.UnionP;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
                    MysqlConnect.Update(item.Key.UserId,item.Key.Nickname, item.Value, DateTime.Now);
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
        public bool RoundEnded { get {

                return Round.IsEnded;
            } }
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
            if (RoundEnded) return;

            if (Config.NoProtectWhenShoot && ProtectionCoroutines.ContainsKey(ev.Player))
            {
                try
                {
                    // 移除保护效果
                    ev.Player.DisableEffect(EffectType.SpawnProtected);

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
        public void RespawningTeam(RespawningTeamEventArgs ev)
        {
            SpawnableWaveBase newW = ev.Wave.Base;
            List<ReferenceHub> players = new List<ReferenceHub>();
            Log.Info($"RespawningTeam IsMiniWave {ev.Wave.IsMiniWave}");

            if (ev.Wave.IsMiniWave)
            {
                ev.IsAllowed = false;
                //NtfSpawnWave
                //Respawn.TryGetWaveBase(ev.Wave.SpawnableFaction, out var spawnableWave);
                //var IL = spawnableWave as ILimitedWave;
                //IL.RespawnTokens -= 1;
                
                switch (ev.Wave.Faction)
                {
                    case PlayerRoles.Faction.FoundationStaff:
                        {
                            newW = new NtfSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);
                            RespawnedTeam(new RespawnedTeamEventArgs(newW, players));
                            break;
                        }
                    case PlayerRoles.Faction.FoundationEnemy:
                        {
                             newW = new ChaosSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);
                            RespawnedTeam(new RespawnedTeamEventArgs(newW, players));

                            break;
                        }
                }

                ev.Wave.Timer.SetTime(0);


            }

        }
        public CoroutineHandle BroadcasterHandler;
        public void WaitingForPlayers()
        {
            StopBroadcast = false;
            Plugin.enableSSCP = false;


            BroadcasterHandler = MEC.Timing.RunCoroutine(Broadcaster());
            
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