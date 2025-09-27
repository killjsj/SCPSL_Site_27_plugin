using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using GameObjectPools;
using InventorySystem.Items.Firearms.Extensions;
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.Enums;
using Next_generationSite_27.Features.PlayerHuds;
using Org.BouncyCastle.Tls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Objects;
using RelativePositioning;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using RoundRestarting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Next_generationSite_27.UnionP.Scp5k
{
    class Scp5k_Control
    {
        public static bool Is5kRound { get; set; } = false;
        public static bool Scp055Escaped { get; set; } = false;
        public static bool UiuEscaped
        {
            get { return _UiuEscaped; }
            set
            {
                if (value && !_UiuEscaped)
                {
                    OnUIUEscaped();
                }
                _UiuEscaped = value;
            }
        }
        public static void OnUIUEscaped()
        {
            foreach (var s in LabApi.Features.Wrappers.Player.List)
            {
                PlayerHudUtils.AddMessage(s, "messid", "<size=40><color=red>UIU已撤离</color></size>");
            }
            var w = WaveManager.Waves.FirstOrDefault(x => x is ChaosSpawnWave) as ChaosSpawnWave;
            w.RespawnTokens += 1;
            FactionInfluenceManager.Add(Faction.FoundationEnemy, FactionInfluenceManager.Get(Faction.FoundationEnemy));
            w.Timer.AddTime(60);
        }
        public static bool _UiuEscaped = false;
        public static float UiuDownloadTime = 0;
        public static float UiuDownloadTick
        {
            get
            {
                if (CustomRole.TryGet(UiuCID, out var role) && CustomRole.TryGet(UiuPID, out var Prole))
                {
                    var count = Prole.TrackedPlayers.Count + role.TrackedPlayers.Count;
                    if (count > 0)
                    {
                        // 目标：6人 → 90秒 → 每秒总进度 = 100/90
                        // 每人每秒贡献 = (100/90) / 6
                        return ((100f / 90f) / 5f) * 0.2f * count;
                    }
                }
                return 0.1f; // 默认极慢速度（无人时）
            }
        }
        public static void WarheadDetonated(Exiled.Events.EventArgs.Warhead.DetonatingEventArgs ev)
        {
            if (Is5kRound && GocSpawned)
            {
                Round.EndRound(true);
            }
            GocSpawnable = false;
        }
        public static void PlayerDamaged(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (Is5kRound)
            {
                if (ev.DamageHandler.Base is Scp096DamageHandler DH)
                {
                    DH.Damage = 99;
                }
                else if (ev.DamageHandler.Base is Scp049DamageHandler handler)
                {
                    if (handler.DamageSubType == Scp049DamageHandler.AttackType.Scp0492 && ev.Player.Health <= ev.Amount)
                    {
                        ev.Player.DropItems();
                        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                        {
                            ev.Player.RoleManager.ServerSetRole(RoleTypeId.Scp0492, RoleChangeReason.Died, RoleSpawnFlags.AssignInventory);
                        });
                    }
                }
                else if (ev.DamageHandler.Base is ScpDamageHandler scpDamageHandler)
                {
                    if (scpDamageHandler.Attacker.Hub.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp173)
                    {
                        if (ev.Player.Role.Type == RoleTypeId.NtfSpecialist ||
                            ev.Player.Role.Type == RoleTypeId.NtfCaptain ||
                            ev.Player.Role.Type == RoleTypeId.NtfPrivate ||
                            ev.Player.Role.Type == RoleTypeId.NtfSergeant
                            )
                        {
                            ev.IsAllowed = false;
                        }
                    }
                }
            }
        }
        public static int UiUSpawnTime = config.UiUSpawnTime - config.UiUSpawnFloatTime + UnityEngine.Random.Range(0, config.UiUSpawnFloatTime * 2);
        public static int AndSpawnTime = config.AndSpawnTime - config.AndSpawnFloatTime + UnityEngine.Random.Range(0, config.AndSpawnFloatTime * 2);
        public static bool UiuDownloadBroadcasted = false;
        public static bool UiuSpawned = false;
        public static bool HammerSpawned = false;
        public static bool GocSpawned = false;
        public static GameObject _GOCBOmb;
        public static GameObject GOCBOmb
        {
            set
            {
                _GOCBOmb = value;
                //Scp5k.GOCAnim.Playstart(_GOCBOmb.gameObject);
            }
            get { return _GOCBOmb; }
        }

        public static bool GocNuke = false;
        public static bool GocSpawnable = true;
        [Description("内部锁定")]
        public static bool NLock = false;
        public static List<Player> diedPlayer { get { return ReferenceHub.AllHubs.Where(x => x.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Spectator).Select((x) => Player.Get(x)).ToList(); } }
        public static Stopwatch AndTimer = new Stopwatch();
        public static Stopwatch GocTimer = new Stopwatch();
        public static PConfig config => Plugin.Instance.Config;
        public static IEnumerator<float> Refresher()
        {
            Log.Info("Refresher In!");
            while (true)
            {
                try
                {
                    foreach (var item in Player.List)
                    {
                        item.EnableEffect(Exiled.API.Enums.EffectType.SoundtrackMute, 1, 600f);
                        item.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 1, 600f);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                try
                {
                    //if (Round.IsEnded)
                    //{
                    //    Log.Info("Manba out!");
                    //    break;
                    //}


                    if (WaveSpawner.AnyPlayersAvailable)
                    {
                        //Log.Info("刷新中");

                        if (Round.ElapsedTime.TotalSeconds > UiUSpawnTime && !UiuSpawned)
                        {
                            Log.Info("uiu");

                            diedPlayer.ShuffleList();
                            var UiuWave = new List<Player>(diedPlayer.Take(Math.Min(config.UiuMaxCount, diedPlayer.Count - 1)));
                            diedPlayer.RemoveRange(0, Math.Min(config.UiuMaxCount, diedPlayer.Count - 1));
                            if (CustomRole.TryGet(28, out var role) && UiuWave.Count > 0)
                            {
                                if (CustomRole.TryGet(32, out var Prole))
                                {
                                    Prole.AddRole(UiuWave[0]);
                                }
                                diedPlayer.RemoveRange(0, 1);
                                foreach (var item in UiuWave)
                                {
                                    role.AddRole(item);
                                }
                                Cassie.MessageTranslated("Security alert . Substantial U I U activity detected . Security personnel , proceed with standard protocols ", "安保警戒，侦测到UIU的活动。安保人员请继续执行标准协议。阻止下载资料");
                            }
                            UiuSpawned = true;
                        }
                        if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && !GocSpawned && GocSpawnable && diedPlayer.Count >= GOCBomb.installCount)
                        {
                            Log.Info("goc");
                            diedPlayer.ShuffleList();
                            var GocWave = new List<Player>(Math.Min(config.GocMaxCount, diedPlayer.Count - 1));
                            diedPlayer.RemoveRange(0, Math.Min(config.GocMaxCount, diedPlayer.Count - 1));
                            if (CustomRole.TryGet(31, out var role) && GocWave.Count > 0)
                            {
                                if (CustomRole.TryGet(30, out var Prole))
                                {
                                    Prole.AddRole(GocWave[0]);
                                }
                                diedPlayer.RemoveRange(0, 1);
                                foreach (var item in GocWave)
                                {
                                    role.AddRole(item);
                                }
                            }
                            DeadmanSwitch.ForceCountdownToggle = false;
                            GocSpawned = true;
                            Cassie.MessageTranslated("Security alert . Substantial G o c activity detected . Security personnel ,  proceed with standard protocols , Protect the warhead ", "安保警戒，侦测到大量GOC的活动。安保人员请继续执行标准协议，保护核弹。");
                        }
                        else if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && GocSpawnable && diedPlayer.Count >= GOCBomb.installCount)
                        {
                            if (CustomRole.TryGet(31, out var role))
                            {
                                if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && GocTimer.Elapsed.TotalSeconds >= config.GocSpawnTime && GocSpawnable)
                                {
                                    Log.Info("small goc");
                                    int spawnint = config.GocMaxCount - role.TrackedPlayers.Count;
                                    diedPlayer.ShuffleList();
                                    var GocWave = new List<Player>(diedPlayer.Take(Math.Min(spawnint, diedPlayer.Count - 1)));
                                    diedPlayer.RemoveRange(0, Math.Min(spawnint, diedPlayer.Count - 1));

                                    if (GocWave.Count > 0)
                                    {
                                        if (CustomRole.TryGet(30, out var Prole))
                                        {
                                            Prole.AddRole(GocWave[0]);
                                        }
                                        diedPlayer.RemoveRange(0, 1);
                                        foreach (var item in GocWave)
                                        {
                                            role.AddRole(item);
                                        }
                                        Cassie.MessageTranslated("Attention security personnel , G O C spotted at Gate A . Protect the warhead", "安保人员请注意，已在A大门处发现GOC，保护核弹。");

                                    }
                                    GocTimer.Restart();
                                }
                            }
                        }
                        else if (Round.ElapsedTime.TotalSeconds > AndSpawnTime)
                        {
                            if (CustomRole.TryGet(botID, out var role))
                            {
                                if (role.TrackedPlayers.Count < config.AndMaxCount && AndTimer.Elapsed.TotalSeconds >= 220)
                                {
                                    Log.Info("andbot");
                                    AndTimer.Restart();
                                    int spawnint = config.AndMaxCount - role.TrackedPlayers.Count;
                                    diedPlayer.ShuffleList();
                                    var botWave = new List<Player>(diedPlayer.Take(Math.Min(spawnint, diedPlayer.Count - 1)));
                                    diedPlayer.RemoveRange(0, Math.Min(spawnint, diedPlayer.Count - 1));
                                    foreach (var item in botWave)
                                    {
                                        role.AddRole(item);

                                    }
                                    Cassie.MessageTranslated("Attention security personnel , And saw spotted at Gate A", "安保人员请注意，已在A大门处发现安德森机器人");

                                }
                            }
                        }
                        else if (Round.ElapsedTime.TotalSeconds > config.HammerStartSpawnTime && !HammerSpawned)
                        {
                            int c = 0;
                            int s = 0;
                            foreach (var p in ReferenceHub.AllHubs.Where(x => x.roleManager.CurrentRole.RoleTypeId.IsAlive()))
                            {

                                if (p.roleManager.CurrentRole.RoleTypeId.IsScp() || p.roleManager.CurrentRole.RoleTypeId.IsNtf())
                                {
                                    s++;
                                }
                                else
                                {
                                    c++;
                                }
                            }
                            if (c - s > config.HammerSpawnCount)
                            {
                            Log.Info("Hammer");
                                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                                if (w.RespawnTokens > 0)
                                {
                                    w.RespawnTokens -= 1;
                                }
                                w.Timer.Reset();
                                Exiled.API.Features.Respawn.SummonNtfChopper();

                                // 替换原来的调用：
                                Timing.RunCoroutine(HammerSpawnCoroutine(w));

                                HammerSpawned = true;

                            }
                        }
                    }
                    if (!Round.IsLocked)
                    {
                        int ntfScp = 0;
                        int lightRunner = 0;
                        int ChaosIn = 0;
                        foreach (var hub in ReferenceHub.AllHubs)
                        {
                            switch (hub.GetTeam())
                            {
                                case Team.SCPs:
                                case Team.FoundationForces:
                                    if (hub.GetRoleId() == RoleTypeId.Scp0492)
                                    {

                                    }
                                    else if (hub.GetRoleId() == RoleTypeId.FacilityGuard)
                                    {
                                        lightRunner++;
                                    }
                                    else
                                    {
                                        ntfScp++;

                                    }
                                    break;
                                case Team.Scientists:
                                case Team.ClassD:
                                    lightRunner++;

                                    break;
                                case Team.ChaosInsurgency:
                                    ChaosIn++;
                                    break;
                            }
                        }
                        ChaosIn += SpecRolesCount;
                        if (ChaosIn == 0 && lightRunner == 0 && ntfScp != 0)
                        {
                            Round.EndRound(true);
                        }
                        else if ((ChaosIn == 0 || lightRunner != 0) && ntfScp == 0)
                        {
                            Round.EndRound(true);
                        }
                        else if (ChaosIn != 0 && ntfScp == 0)
                        {
                            Round.EndRound(true);
                        }
                        else if (ChaosIn == 0 && lightRunner == 0 && ntfScp == 0)
                        {
                            Round.EndRound(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1f);
            }
        }
        private static IEnumerator<float> HammerSpawnCoroutine(NtfSpawnWave w)
        {
            yield return Timing.WaitForSeconds(w.AnimationDuration);
            diedPlayer.ShuffleList();
            var HammerWave = new List<Player>(diedPlayer.Take(Math.Min(config.HammerMaxCount, diedPlayer.Count - 1)));
            diedPlayer.RemoveRange(0, Math.Min(config.HammerMaxCount, diedPlayer.Count - 1));
            foreach (var item in HammerWave)
            {
                item.Role.Set(RoleTypeId.NtfCaptain);
                Timing.CallDelayed(0.1f, () =>
                {
                    foreach (var item1 in item.Items)
                    {
                        if (item1.Type == ItemType.Adrenaline)
                        {
                            item.RemoveItem(item1);
                        }
                    }
                    item.AddItem(ItemType.Jailbird);
                    item.AddItem(ItemType.Jailbird);
                });
            }
            Cassie.MessageTranslated("Mobile Task Force Unit Nu 7 has entered the facility", "机动特遣队Nu-7小队已进入设施。");

            yield break;
        }
        static CoroutineHandle refresher;
        public static void RoundStarted()
        {
            UiuEscaped = false;
            Scp055Escaped = false;
            HammerSpawned = false;
            UiuDownloadBroadcasted = false;
            UiUSpawnTime = config.UiUSpawnTime - config.UiUSpawnFloatTime + UnityEngine.Random.Range(0, config.UiUSpawnFloatTime * 2);
            AndSpawnTime = config.AndSpawnTime - config.AndSpawnFloatTime + UnityEngine.Random.Range(0, config.AndSpawnFloatTime * 2);
        UiuSpawned = false;
            GocSpawned = false;
            GocSpawnable = true;
            AndTimer = new Stopwatch(); 
            UiuDownloadTime = 0;
            GocTimer = new Stopwatch();
            if (refresher.IsRunning)
            {
                Timing.KillCoroutines(refresher);
            }
            if (Is5kRound)
            {
                Log.Info("refresher start");

                refresher = MEC.Timing.RunCoroutine(Refresher());

            }
            Timing.CallDelayed(0.05f, () =>
                {
                    try
                    {
                        if (Is5kRound)
                        {
                            var AEHPossableList = Pickup.List.Where(x => x.Type.IsArmor() && x.Room.Zone == Exiled.API.Enums.ZoneType.HeavyContainment && x.Room.Type != Exiled.API.Enums.RoomType.Hcz079 && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointA && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointB
                            && x.Room.Type != Exiled.API.Enums.RoomType.HczElevatorA && x.Room.Type != Exiled.API.Enums.RoomType.HczElevatorB).ToList();
                            Door s055Pos = Door.List.Where(x => !x.IsGate && !x.IsElevator && x.Room.Type != Exiled.API.Enums.RoomType.Hcz079 && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointA && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointB && x.Room.Zone == Exiled.API.Enums.ZoneType.HeavyContainment && x.Room.Type != Exiled.API.Enums.RoomType.Hcz079 && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointA && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointB
                            && x.Room.Type != Exiled.API.Enums.RoomType.HczElevatorA && x.Room.Type != Exiled.API.Enums.RoomType.HczElevatorB).ToList().RandomItem();
                            var AEHPos = AEHPossableList.GetRandomValue();

                            var BombPossableList = Pickup.List.Where(x => (x.Type == ItemType.GunCrossvec || x.Type == ItemType.GunCOM15) && x.Room.Zone == Exiled.API.Enums.ZoneType.HeavyContainment && x.Room.Type != Exiled.API.Enums.RoomType.Hcz079 && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointA && x.Room.Type != Exiled.API.Enums.RoomType.HczEzCheckpointB
                            && x.Room.Type != Exiled.API.Enums.RoomType.HczElevatorA && x.Room.Type != Exiled.API.Enums.RoomType.HczElevatorB).ToList();
                            var BOmbPos = BombPossableList.GetRandomValue();
                            CustomItem.TrySpawn(Scp055ItemID, s055Pos.Position + new Vector3(0f, 1f, 0f), out var s5);
                            CustomItem.TrySpawn(AEHItemID, AEHPos.Position + new Vector3(0f, 1f, 0f), out var aeh);


                            if (BOmbPos != null)
                            {
                                CustomItem.TrySpawn(BombgunItemID, BOmbPos.Position + new Vector3(0f, 1f, 0f), out var bomb);
                                Log.Info($"Bombgun spawned AT:{BOmbPos.Room.RoomName} {BOmbPos.Room.RoomShape} {BOmbPos.Position}");
                                Log.Info($"Bombgun spawned AT:{BOmbPos.Room.RoomName} {BOmbPos.Room.RoomShape} {BOmbPos.Position}");
                                BOmbPos.Destroy(); // 安全销毁原物品
                            }
                            else
                            {
                                Log.Warn("未能找到符合条件的 Crossvec 枪，无法生成 Bombgun。");
                            }
                            Log.Info($"SCP055 spawned AT:{s5.Room.RoomName} {s5.Room.RoomShape} {s5.Position}");
                            Log.Info($"AEH spawned AT:{aeh.Room.RoomName} {aeh.Room.RoomShape} {aeh.Position}");
                            AEHPos.Destroy();
                            if (Player.List.Where(x => x.Role.Type == RoleTypeId.Scientist).ToList().Count() > 0)
                            {
                                var Luck = Player.List.Where(x => x.Role.Type == RoleTypeId.Scientist).ToList().RandomItem();
                                if (CustomRole.TryGet(SciID, out var role))
                                {
                                    role.AddRole(Luck);
                                }
                            }
                            if (Player.List.Where(x => x.Role.Type == RoleTypeId.ClassD).ToList().Count() > 2)
                            {
                                var Luck = Player.List.Where(x => x.Role.Type == RoleTypeId.ClassD).ToList().RandomItem();
                                if (CustomRole.TryGet(GocSpyID, out var role))
                                {
                                    role.AddRole(Luck);
                                }
                            }

                            foreach (var item in Player.List)
                            {
                                if (item.Role.Type == RoleTypeId.Scp079)
                                {
                                    item.RoleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart);
                                    item.Position = Room.Get(Exiled.API.Enums.RoomType.Hcz096).Position + new UnityEngine.Vector3(0, 0.5f, 0);

                                }
                                if (item.Role.Type == RoleTypeId.FacilityGuard)
                                {
                                    if (RoleSpawnpointManager.TryGetSpawnpointForRole(RoleTypeId.Scientist, out var spawnpoint) && spawnpoint != null)
                                    {
                                        if (spawnpoint.TryGetSpawnpoint(out var position, out _))
                                        {
                                            item.Position = position + new UnityEngine.Vector3(0, 0.5f, 0);
                                        }
                                    }
                                    else
                                    {
                                        item.Position = Room.Get(Exiled.API.Enums.RoomType.EzCheckpointHallwayA).Position + new UnityEngine.Vector3(0, 3f, 0);

                                        Log.Warn("未找到科学家出生点，无法设置 FacilityGuard 位置。");
                                    }
                                    //spawnpoint.TryGetSpawnpoint(out var position, out _);
                                    //item.Position = position + new UnityEngine.Vector3(0, 0.5f, 0);
                                }
                            }

                            //Pickup.CreateAndSpawn(ItemType.Gu)

                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex.ToString());
                    }
                });
        }
        public static Dictionary<RoleTypeId, float> SCPFF = new Dictionary<RoleTypeId, float>()
        {
            [RoleTypeId.ChaosRifleman] = 1,
            [RoleTypeId.ChaosRepressor] = 1,
            [RoleTypeId.ChaosConscript] = 1,
            [RoleTypeId.ChaosMarauder] = 1,
            [RoleTypeId.FacilityGuard] = 1,
            [RoleTypeId.NtfCaptain] = 0,
            [RoleTypeId.NtfPrivate] = 0,
            [RoleTypeId.NtfSergeant] = 0,
            [RoleTypeId.NtfSpecialist] = 0,
            [RoleTypeId.Scientist] = 1,
            [RoleTypeId.ClassD] = 1,
            [RoleTypeId.Tutorial] = 0,
            [RoleTypeId.CustomRole] = 0,
            [RoleTypeId.Scp049] = 0,
            [RoleTypeId.Scp079] = 0,
            [RoleTypeId.Scp096] = 0,
            [RoleTypeId.Scp3114] = 0,
            [RoleTypeId.Scp939] = 0,
            [RoleTypeId.Scp173] = 0,
            [RoleTypeId.Scp106] = 0,
        };
        public static Dictionary<RoleTypeId, float> AntiSCPFF = new Dictionary<RoleTypeId, float>()
        {
            [RoleTypeId.ChaosRifleman] = 0,
            [RoleTypeId.ChaosRepressor] = 0,
            [RoleTypeId.ChaosConscript] = 0,
            [RoleTypeId.ChaosMarauder] = 0,
            [RoleTypeId.NtfCaptain] = 1,
            [RoleTypeId.NtfPrivate] = 1,
            [RoleTypeId.NtfSergeant] = 1,
            [RoleTypeId.NtfSpecialist] = 1,
            [RoleTypeId.FacilityGuard] = 1,
            [RoleTypeId.Scientist] = 1,
            [RoleTypeId.ClassD] = 1,
            [RoleTypeId.Tutorial] = 0,
            [RoleTypeId.CustomRole] = 0,
            [RoleTypeId.Scp049] = 1,
            [RoleTypeId.Scp079] = 1,
            [RoleTypeId.Scp096] = 1,
            [RoleTypeId.Scp3114] = 1,
            [RoleTypeId.Scp939] = 1,
            [RoleTypeId.Scp173] = 1,
            [RoleTypeId.Scp106] = 1,
        };
        public static Dictionary<RoleTypeId, float> GOCFF = new Dictionary<RoleTypeId, float>()
        {
            [RoleTypeId.ChaosRifleman] = 1,
            [RoleTypeId.ChaosRepressor] = 1,
            [RoleTypeId.ChaosConscript] = 1,
            [RoleTypeId.ChaosMarauder] = 1,
            [RoleTypeId.NtfCaptain] = 1,
            [RoleTypeId.NtfPrivate] = 1,
            [RoleTypeId.NtfSergeant] = 1,
            [RoleTypeId.NtfSpecialist] = 1,
            [RoleTypeId.FacilityGuard] = 1,
            [RoleTypeId.Scientist] = 1,
            [RoleTypeId.ClassD] = 1,
            [RoleTypeId.Tutorial] = 1,
            [RoleTypeId.CustomRole] = 1,
            [RoleTypeId.Scp049] = 1,
            [RoleTypeId.Scp079] = 1,
            [RoleTypeId.Scp096] = 1,
            [RoleTypeId.Scp3114] = 1,
            [RoleTypeId.Scp939] = 1,
            [RoleTypeId.Scp173] = 1,
            [RoleTypeId.Scp106] = 1,
        };
        public static Dictionary<RoleTypeId, float> EscaperFF = new Dictionary<RoleTypeId, float>()
        {
            [RoleTypeId.ChaosRifleman] = 1,
            [RoleTypeId.ChaosRepressor] = 1,
            [RoleTypeId.ChaosConscript] = 1,
            [RoleTypeId.ChaosMarauder] = 1,
            [RoleTypeId.NtfCaptain] = 1,
            [RoleTypeId.NtfPrivate] = 1,
            [RoleTypeId.NtfSergeant] = 1,
            [RoleTypeId.NtfSpecialist] = 1,
            [RoleTypeId.FacilityGuard] = 0,
            [RoleTypeId.Scientist] = 0,
            [RoleTypeId.ClassD] = 0,
            [RoleTypeId.Tutorial] = 1,
            [RoleTypeId.CustomRole] = 1,
            [RoleTypeId.Scp049] = 1,
            [RoleTypeId.Scp079] = 1,
            [RoleTypeId.Scp096] = 1,
            [RoleTypeId.Scp3114] = 1,
            [RoleTypeId.Scp939] = 1,
            [RoleTypeId.Scp173] = 1,
            [RoleTypeId.Scp106] = 1,
        };
        public static int SpecRolesCount
        {
            get
            {
                return CustomRole.Get(botID).TrackedPlayers.Count + CustomRole.Get(UiuCID).TrackedPlayers.Count + CustomRole.Get(UiuPID).TrackedPlayers.Count +
                        CustomRole.Get(GocCID).TrackedPlayers.Count +
                        CustomRole.Get(GocPID).TrackedPlayers.Count;
            }
        }
        public static void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (Is5kRound)
            {
                var p = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);
                switch (ev.NewRole)
                {
                    case RoleTypeId.Scp079:
                        {
                            Timing.CallDelayed(0.05f, () =>
                            {
                                try
                                {
                                    var a = ev.Player.Role as Exiled.API.Features.Roles.Scp079Role;
                                    FieldInfo _minTierIndex = typeof(Scp079DoorLockChanger).GetField("_minTierIndex", BindingFlags.NonPublic | BindingFlags.Instance);

                                    // 检查字段是否存在（防止游戏更新后字段名变化）
                                    if (_minTierIndex == null)
                                    {
                                        Log.Error("Failed to get one or more private fields from Scp079PingAbility. Field names may have changed.");

                                    }
                                    else
                                    {
                                        // ✅ 使用 SetValue 强制修改私有字段
                                        _minTierIndex.SetValue(a.DoorLockChanger, (int) 1 );
                                    }


                                }
                                catch (Exception ex)
                                {
                                    Log.Warn(ex);
                                }

                            });
                            
                            break;
                        }
                    case RoleTypeId.Scp106:
                        {
                            Timing.CallDelayed(0.05f, () =>
                            {
                                try
                                {
                                    var a = ev.Player.Role as Exiled.API.Features.Roles.Scp106Role;
                                    a.CaptureCooldown -= 0.5f;
                                }
                                catch (Exception ex)
                                {
                                    Log.Warn(ex);
                                }

                            });
                            break;

                        }
                    case RoleTypeId.Scp3114:
                        {
                            Timing.CallDelayed(0.05f, () =>
                            {
                                try
                                {
                                    ev.Player.MaxHealth = 2600;
                                    ev.Player.Health = 2600;
                                }
                                catch (Exception ex)
                                {
                                    Log.Warn(ex);
                                }

                            });
                            break;
                        }
                }
                switch (ev.NewRole)
                {
                    case RoleTypeId.Scp106:
                    case RoleTypeId.NtfCaptain:
                    case RoleTypeId.Scp079:
                    case RoleTypeId.Scp096:
                    case RoleTypeId.Scp939:
                    case RoleTypeId.NtfSergeant:
                    case RoleTypeId.Scp049:
                    case RoleTypeId.Scp0492:
                    case RoleTypeId.NtfSpecialist:
                    case RoleTypeId.Scp3114:
                    case RoleTypeId.Scp173:
                        {
                            p.AddMessage("messID", "你是 基金会势力 消灭一切除基金会势力外的成员 \n <color=green>友好:SCP,九尾狐</color>\n<color=red>敌对:GOC,UIU,ClassD,科学家,保安,混沌,安德森机器人</color>", 5f, ScreenLocation.CenterTop);
                            ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(SCPFF);
                            break;
                        }

                    case RoleTypeId.Spectator:
                    case RoleTypeId.Overwatch:
                    case RoleTypeId.Destroyed:
                    case RoleTypeId.Filmmaker:
                    case RoleTypeId.None:
                        break;
                    case RoleTypeId.FacilityGuard:
                    case RoleTypeId.Scientist:
                    case RoleTypeId.ClassD:
                        {
                            if (!CustomRole.TryGet(Scp5k_Control.GocSpyID, out var customGocSpy))
                            {
                                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocSpy 请联系技术</size></color>", 3f);
                                return;
                            }
                            bool isGocActing = false;
                            if (customGocSpy.Check(ev.Player))
                            {
                                isGocActing = true;
                            }
                            if (ev.Player.UniqueRole == customGocSpy.Name)
                            {

                                isGocActing = true;
                            }
                            if(isGocActing)
                            {
                                p.AddMessage("messID", "你是 GOC势力 与反基金会势力合作 前往广播召唤GOC\n<color=yellow>中立:ClassD,科学家,保安,UIU,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                                ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(GOCFF);
                                ev.Player.SetCustomRoleFriendlyFire("Goc_Spy", RoleTypeId.Tutorial, 0);
                                break;
                            }
                            p.AddMessage("messID", "你是 清收势力 与反基金会势力合作 尽可能的逃跑\n<color=yellow>中立:GOC,UIU,ClassD,科学家,保安,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);

                            ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(EscaperFF);
                            break;
                            //Npc
                        }

                    case RoleTypeId.CustomRole:
                    case RoleTypeId.Tutorial:
                    case RoleTypeId.ChaosConscript:
                    case RoleTypeId.ChaosMarauder:
                    case RoleTypeId.ChaosRepressor:
                    case RoleTypeId.ChaosRifleman:
                        {
                            if (!CustomRole.TryGet(Scp5k_Control.GocCID, out var customGocC))
                            {
                                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocC 请联系技术</size></color>", 3f);
                                return;
                            }
                            if (!CustomRole.TryGet(Scp5k_Control.GocPID, out var customGocP))
                            {
                                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocP 请联系技术</size></color>", 3f);
                                return;
                            }
                            if (!CustomRole.TryGet(Scp5k_Control.UiuCID, out var customUiuC))
                            {
                                p.AddMessage("Failed", "<color=red><size=27>未获取角色:customUiuC 请联系技术</size></color>", 3f);
                                return;
                            }
                            if (!CustomRole.TryGet(Scp5k_Control.UiuPID, out var customUiuP))
                            {
                                p.AddMessage("Failed", "<color=red><size=27>未获取角色:customUiuP 请联系技术</size></color>", 3f);
                                return;
                            }
                            if (!CustomRole.TryGet(Scp5k_Control.botID, out var customBot))
                            {
                                p.AddMessage("Failed", "<color=red><size=27>未获取角色:customBot 请联系技术</size></color>", 3f);
                                return;
                            }
                            bool isGocActing = false;
                            if (customGocC.Check(ev.Player) || customGocP.Check(ev.Player))
                            {
                                isGocActing = true;
                            }
                            if (ev.Player.UniqueRole == customGocC.Name || ev.Player.UniqueRole == customGocP.Name)
                            {
                                isGocActing = true;
                            }
                            if (!isGocActing)
                            {
                                p.AddMessage("messID", "你是 反基金会势力 消灭一切基金会团队的成员\n <color=green>友好:GOC,UIU,混沌,安德森机器人</color>\n<color=yellow>中立:ClassD,科学家,保安</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                                ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(AntiSCPFF);
                                break;
                            }
                            else
                            {
                                p.AddMessage("messID", "你是 GOC势力 消灭一切基金会团队的成员\n <color=green>友好:goc</color>\n<color=yellow>中立:ClassD,科学家,保安,UIU,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                                if (ev.Player != null)
                                {
                                    ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(GOCFF);
                                    ev.Player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                                    ev.Player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                                }
                                break;
                            }
                        }
                    case RoleTypeId.Flamingo:
                    case RoleTypeId.AlphaFlamingo:
                    case RoleTypeId.ZombieFlamingo:
                        break;
                }

            }
        }
        public static void RoundEnding(EndingRoundEventArgs ev)
        {
            if (Is5kRound)
            {
                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    GOCAnim.donating = false;
                Goc_Spy_broadcasted = false;
                GOCBomb.Inited = false;
                GOCBomb.Played = false;
                GOCBomb.installCount = 2;
                GOCBomb.GOCBombList = new List<GOCBomb>();
                GOCBomb.installAt = new List<Room>();
                GOCBomb.installedRoom = new Dictionary<GOCBomb, Room>();
                GOCBomb.P2B = new Dictionary<Exiled.API.Features.Player, GOCBomb>();
                GOCBomb.Questions = new List<(string q, string a)>();
                GOCBomb.QuestionCount = 1;
               uiu_broadcasted = false;
        GOCBomb.countDown = GOCBomb.countDownStart;
        GOCBomb.QuestionPoint = -1;
                GocNuke = false;
                if (Scp055Escaped)
                {
                    foreach (var s in LabApi.Features.Wrappers.Player.List)
                    {
                        PlayerHudUtils.AddMessage(s, "messid", "Normal Ending:055进入了579 宇宙重启");
                    }
                    ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.Draw;
                }
                else if ((Warhead.IsDetonated && GocSpawned) || GocNuke)
                {
                    ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.ChaosInsurgency;
                    foreach (var s in LabApi.Features.Wrappers.Player.List)
                    {
                        PlayerHudUtils.AddMessage(s, "messid", "GOC Ending: GOC成功引爆核弹");
                    }
                }
                else
                {
                    RoundSummary.SumInfo_ClassList newList = default(RoundSummary.SumInfo_ClassList);
                    foreach (ReferenceHub hub in ReferenceHub.AllHubs)
                    {
                        switch (hub.GetTeam())
                        {
                            case Team.SCPs:
                            case Team.FoundationForces:
                                if (hub.GetRoleId() == RoleTypeId.Scp0492)
                                {

                                }
                                else
                                {
                                    newList.mtf_and_guards++;

                                }
                                break;
                            case Team.Scientists:
                            case Team.ClassD:
                            case Team.ChaosInsurgency:
                                newList.chaos_insurgents++;
                                break;
                        }
                        if (hub.GetRoleId() == RoleTypeId.CustomRole)
                        {
                            newList.chaos_insurgents++;
                        }
                    }
                    if (newList.chaos_insurgents != 0 && newList.mtf_and_guards == 0)
                    {
                        ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.ChaosInsurgency;
                    }
                    else if (newList.chaos_insurgents == 0 && newList.mtf_and_guards != 0)
                    {
                        ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.FacilityForces;
                    }
                }
                Is5kRound = false;
            }
            else
            {
                Is5kRound = UnityEngine.Random.Range(1, 100) <= config.scp5kPercent;
            }
            if (refresher.IsRunning)
            {
                Timing.KillCoroutines(refresher);
            }
        }
        public static bool uiu_broadcasted = false;
        public static uint UiuCID = 32;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_C : CustomRole
        {

            public override uint Id { get; set; } = UiuCID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "UIU_C";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }

            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {

                Description = "与安德森机器人合作 调查基金会为什么毁灭人类\n下载完资料后撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 120;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU成员</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往机房下载资料 下载完资料后撤离</color></size>", 4);
                //p.AddMessage("messID", "你是 反基金会 团队 消灭一切基金会团队的成员", 2f, ScreenLocation.CenterBottom));
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.GunCrossvec)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();

                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            void OnChangingRole(ChangingRoleEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if (CH.TryGetValue(ev.Player.UserId, out var CH1))
                    {
                        if (CH1.IsRunning)
                        {
                            Timing.KillCoroutines(CH1);
                        }
                    }
                }
            }
            public Dictionary<string, CoroutineHandle> CH = new Dictionary<string, CoroutineHandle>();
            public IEnumerator<float> PlayerUpdate(Player player)
            {
                bool Downloaded = false;
                var p = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);
                while (true)
                {
                    try
                    {
                        if (player == null)
                        {
                            yield break;
                        }
                        var hud = PlayerHud.Get(player);
                        if (hud == null)
                        {
                            yield break; // 或者 continue，取决于你想怎么处理
                        }
                        if (!Check(player))
                        {
                            break;
                        }
                        if (!Downloaded)
                        {
                            if (player.CurrentRoom != null && player.CurrentRoom.RoomName != null && player.CurrentRoom.RoomName != RoomName.Unnamed)

                            {

                                if (player.CurrentRoom.RoomName == MapGeneration.RoomName.HczServers)
                                {
                                    if (Scp5k_Control.UiuDownloadTime >= 100f)
                                    {
                                        Downloaded = true;
                                        if (!uiu_broadcasted)
                                        {
                                            Cassie.MessageTranslated("Security alert . U I U down load d . Security personnel , proceed with standard protocols", "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。阻止撤离");
                                            uiu_broadcasted = true;
                                        }
                                        hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage("messID", "<size=30><color=red>你已成功下载资料,请尽快撤离!</color></size>", 04f, ScreenLocation.CenterBottom));
                                        if (hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.RemoveMessage("UIUdownloading");
                                        }
                                        UiuDownloadTime = 100f;
                                    }
                                    else
                                    {
                                        UiuDownloadTime += UiuDownloadTick;
                                        //Log.Info($"UiuDownloadTick:{UiuDownloadTick}");
                                        float remainTime = (100f - Scp5k_Control.UiuDownloadTime) * 0.2f;
                                        if (UiuDownloadTime >= 30f && !UiuDownloadBroadcasted)
                                        {
                                            Cassie.MessageTranslated("Security alert . U I U down load activity detected . Security personnel , proceed with standard protocols", "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。前往机房");
                                            UiuDownloadBroadcasted = true;
                                        }
                                        else if (UiuDownloadTime <= 20)
                                        {
                                            UiuDownloadBroadcasted = false;

                                        }
                                        //p.SendConsoleMessage($"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {time:F0} 秒 下载结束: 300 秒</color></size>");
                                        if (!hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.DynamicMessage(
                                                    "UIUdownloading",
                                                    p,
                                                    (x) => new string[] { $"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {UiuDownloadTime:F1}% 预计下载结束: {remainTime:F0} 秒</color></size>" },
                                                    2f,
                                                    ScreenLocation.CenterBottom
                                                ));
                                        }
                                    }
                                }
                                else
                                {
                                    if (UiuDownloadTime > 0f)
                                        UiuDownloadTime -= UiuDownloadTick;
                                }
                            }
                        }
                        else
                        {
                            if (Escape.CanEscape(player.ReferenceHub, out var role, out var zone))
                            {
                                if (hud.HasMessage("UIUdownloading"))
                                {
                                    hud.RemoveMessage("UIUdownloading");
                                }
                                hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage("messID", "<size=30><color=yellow>你作为uiu成功撤离</color></size>", 4f, ScreenLocation.CenterBottom));
                                Scp5k_Control.UiuEscaped = true;
                                RemoveRole(player);
                                yield break;
                            }

                        }
                    }


                    catch (Exception ex)
                    {
                        Log.Warn(ex.ToString());
                    }
                    yield return Timing.WaitForSeconds(0.2f);

                }
                Log.Debug("Out!");

            }
            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        CH[player.UserId] = MEC.Timing.RunCoroutine(PlayerUpdate(player));
                        player.Position = new Vector3(0, 302, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });

            }
        }
        public static uint UiuPID = 28;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_P : CustomRole
        {

            public override uint Id { get; set; } = UiuPID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "UIU_P";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "与安德森机器人合作 调查基金会为什么毁灭人类\n下载完资料后撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 130;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU成员</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往机房下载资料 下载完资料后撤离</color></size>", 4);


                //foreach (var item in FFMul)
                //{
                //    SetFriendlyFire(item);
                //}
                this.IgnoreSpawnSystem = true;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.GunE11SR),
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();

                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            void OnChangingRole(ChangingRoleEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if (CH.TryGetValue(ev.Player.UserId, out var CH1))
                    {
                        if (CH1.IsRunning)
                        {
                            Timing.KillCoroutines(CH1);
                        }
                    }
                }
            }
            public Dictionary<string, CoroutineHandle> CH = new Dictionary<string, CoroutineHandle>();
            public IEnumerator<float> PlayerUpdate(Player player)
            {
                bool Downloaded = false;
                var p = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);

                while (true)
                {
                    try
                    {
                        if (player == null)
                        {
                            yield break;
                        }
                        if (!Check(player))
                        {
                            break;
                        }
                        var hud = PlayerHud.Get(player);
                        if (hud == null)
                        {
                            yield break; // 或者 continue，取决于你想怎么处理
                        }
                        if (!Downloaded)
                        {
                            if (player.CurrentRoom != null && player.CurrentRoom.RoomName != null && player.CurrentRoom.RoomName != RoomName.Unnamed)

                            {

                                if (player.CurrentRoom.RoomName == MapGeneration.RoomName.HczServers)
                                {
                                    if (Scp5k_Control.UiuDownloadTime >= 100f)
                                    {
                                        Downloaded = true;
                                        if (!uiu_broadcasted)
                                        {
                                            Cassie.MessageTranslated("Security alert . U I U down load d . Security personnel , proceed with standard protocols", "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。阻止撤离");
                                            uiu_broadcasted = true;
                                        }
                                        hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage("messID", "<size=30><color=red>你已成功下载资料,请尽快撤离!</color></size>", 04f, ScreenLocation.CenterBottom));
                                        if (hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.RemoveMessage("UIUdownloading");
                                        }
                                        UiuDownloadTime = 100f;
                                    }
                                    else
                                    {
                                        UiuDownloadTime += UiuDownloadTick;
                                        //Log.Info($"UiuDownloadTick:{UiuDownloadTick}");
                                        float remainTime = (100f - Scp5k_Control.UiuDownloadTime) * 0.2f;
                                        if (UiuDownloadTime >= 30f && !UiuDownloadBroadcasted)
                                        {
                                            Cassie.MessageTranslated("Security alert . U I U down load activity detected . Security personnel , proceed with standard protocols", "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。前往机房");
                                            UiuDownloadBroadcasted = true;
                                        }
                                        else if (UiuDownloadTime <= 20)
                                        {
                                            UiuDownloadBroadcasted = false;

                                        }
                                        //p.SendConsoleMessage($"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {time:F0} 秒 下载结束: 300 秒</color></size>");
                                        if (!hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.DynamicMessage(
                                                    "UIUdownloading",
                                                    p,
                                                    (x) => new string[] { $"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {UiuDownloadTime:F1}% 预计下载结束: {remainTime:F0} 秒</color></size>" },
                                                    2f,
                                                    ScreenLocation.CenterBottom
                                                ));
                                        }
                                    }
                                }
                                else
                                {
                                    if (UiuDownloadTime > 0f)
                                        UiuDownloadTime -= UiuDownloadTick;
                                }
                            }
                        }
                        else
                        {
                            if (Escape.CanEscape(player.ReferenceHub, out var role, out var zone))
                            {
                                if (hud.HasMessage("UIUdownloading"))
                                {
                                    hud.RemoveMessage("UIUdownloading");
                                }
                                hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage("messID", "<size=30><color=yellow>你作为uiu成功撤离</color></size>", 4f, ScreenLocation.CenterBottom));
                                Scp5k_Control.UiuEscaped = true;
                                RemoveRole(player);
                                yield break;
                            }

                        }
                    }


                    catch (Exception ex)
                    {
                        Log.Warn(ex.ToString());
                    }
                    yield return Timing.WaitForSeconds(0.2f);

                }
                Log.Debug("Out!");

            }
            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        CH[player.UserId] = MEC.Timing.RunCoroutine(PlayerUpdate(player));
                        player.Position = new Vector3(0, 302, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });
            }
        }
        public static uint botID = 29;

        [CustomRole(RoleTypeId.Tutorial)]

        public class scp5k_Bot : CustomRole
        {
            public override uint Id { get; set; } = botID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Bot";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "与反scp基金会势力合作";
                this.Role = RoleTypeId.Tutorial;
                this.Gravity = new UnityEngine.Vector3(0, -14f, 0);
                MaxHealth = 150;
                this.DisplayCustomItemMessages = true;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是安德森机器人</color></size>\n<size=30><color=yellow>与反scp基金会势力合作</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.ParticleDisruptor),
                string.Format("{0}", ItemType.GunE11SR)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();
                Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
                Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            public int totalLives = 0;
            public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    //var p = LabApi.Features.Wrappers.Player.Get(Player.ReferenceHub);
                    var p = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);
                    if (ev.Player.Role.Type == RoleTypeId.Tutorial && Check(ev.Player))
                    {
                        if (totalLives > 0)
                        {
                            ev.Player.EnableEffect(type: Exiled.API.Enums.EffectType.Flashed, 0.1f);
                            totalLives = totalLives - 1;
                            ev.IsAllowed = false;
                            ev.Player.Health = ev.Player.MaxHealth;
                            ev.Player.Position = Room.Get(Exiled.API.Enums.RoomType.EzGateA).Position + new UnityEngine.Vector3(0, 3f, 0);
                            ev.Player.ClearItems();
                            foreach (string itemName in Inventory)
                            {
                                TryAddItem(ev.Player, itemName);
                            }
                            p.AddMessage("messID", $"<color=red><size=30>你还有 {totalLives} 次复活机会</size></color>", 1.5f, ScreenLocation.CenterBottom);
                        }
                        else
                        {
                            RemoveRole(ev.Player);

                        }
                    }


                }
            }
            protected override void RoleAdded(Player player)
            {


                Timing.CallDelayed(0.2f, () =>
                {
                    if (player != null)
                    {
                        totalLives += config.AndLives;
                        player.Position = Room.Get(Exiled.API.Enums.RoomType.EzGateA).Position + new UnityEngine.Vector3(0, 3f, 0);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                        //MEC.Timing.RunCoroutine(PlayerUpdate(player));
                    }
                });
                base.RoleAdded(player);
            }

            //public IEnumerator<float> PlayerUpdate(Player player)
            //{
            //    while (player.IsAlive && player.Role.Type == RoleTypeId.Tutorial && Check(player))
            //    {
            //        yield return Timing.WaitForSeconds(0.3f);
            //    }
            //}
            public override void AddRole(Player player)
            {

                base.AddRole(player);
            }
        }
        public static uint GocCID = 30;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_C : CustomRole
        {

            public override uint Id { get; set; } = GocCID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc_C";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启核弹或奇术核弹毁灭站点";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 120;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc特工</color></size>\n<size=30><color=yellow>开启核弹或奇术核弹毁灭站点</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunLogicer)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }

            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        //MEC.Timing.RunCoroutine(PlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in GOCFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    var g = CustomItem.Get(GocBombItemId);
                    if (g != null)
                    {
                        g.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint GocPID = 31;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_P : CustomRole
        {

            public override uint Id { get; set; } = GocPID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc_P";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启核弹或奇术核弹毁灭站点";

                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 130;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc队长</color></size>\n<size=30><color=yellow>开启核弹或奇术核弹毁灭站点</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunFRMG0)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        //MEC.Timing.RunCoroutine(PlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in GOCFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    var g = CustomItem.Get(GocBombItemId);
                    if (g != null)
                    {
                        g.Give(player);
                    }
                });
                base.RoleAdded(player);
            }
        }
        public static bool Goc_Spy_broadcasted = false;
        public static uint GocSpyID = 33;
        [CustomRole(RoleTypeId.ClassD)]
        public class scp5k_Goc_spy : CustomRole
        {

            public override uint Id { get; set; } = GocSpyID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc_spy";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
            {
                Limit = 1,
            };
            public override void Init()
            {
                Description = "你是Goc间谍\n前往广播室呼叫阵营";

                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 130;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc间谍</color></size>\n<size=30><color=yellow>前往广播室呼叫阵营</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardGuard),
                string.Format("{0}", ItemType.Radio),
                string.Format("{0}", ItemType.GunCOM18)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                Exiled.Events.Handlers.Player.UsingItem += UsingItem;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UsingItem(UsingItemEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if(ev.Item.Type != ItemType.Radio) return;
                    if (ev.Player.CurrentRoom != null && ev.Player.CurrentRoom.RoomName == RoomName.EzIntercom && Goc_Spy_broadcasted && GocSpawnable)
                    {
                        Log.Info("goc");
                        diedPlayer.ShuffleList();
                        var GocWave = new List<Player>(Math.Min(config.GocMaxCount, diedPlayer.Count - 1));
                        diedPlayer.RemoveRange(0, Math.Min(config.GocMaxCount, diedPlayer.Count - 1));
                        if (CustomRole.TryGet(30, out var role) && GocWave.Count > 0)
                        {
                            foreach (var item in GocWave)
                            {
                                Goc_Spy_broadcasted = true;
                                role.AddRole(item);
                            }
                        }
                        if (Goc_Spy_broadcasted)
                        {
                            Cassie.MessageTranslated("Security alert . Substantial G o c activity detected . Security personnel ,  proceed with standard protocols , Protect the warhead ", "安保警戒，侦测到大量GOC的活动。安保人员请继续执行标准协议，保护核弹。");
                            LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub).AddMessage("messID", "<size=30><color=green>你成功呼叫支援!</color></size>", 3f, ScreenLocation.CenterBottom);
                        } else
                        {
                            LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub).AddMessage("messID", "<size=30><color=green>失败!</color></size>", 3f, ScreenLocation.CenterBottom);

                        }
                    }
                    else
                    {
                        var p = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);
                        p.AddMessage("messID", "<size=30><color=red>你必须在广播室使用无线电呼叫阵营!</color></size>", 3f, ScreenLocation.CenterBottom);
                    }
                }
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                Exiled.Events.Handlers.Player.UsingItem -= UsingItem;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
            }
        }
        public static uint SciID = 34;
        [CustomRole(RoleTypeId.Scientist)]
        public class scp5k_Sci : CustomRole
        {

            public override uint Id { get; set; } = SciID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "DrPW";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "逃出site27,前往site62c \n带着055撤离";
                this.Role = RoleTypeId.Scientist;
                MaxHealth = 130;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Pietro Wilson</color></size>\n<size=30><color=yellow>携带SCP055逃出site27,前往site62c\n(带着055撤离)</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardResearchCoordinator),
            };
                base.Init();
            }
            void Escaping(EscapingEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    var p = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);

                    if (CustomItem.TryGet(Scp055ItemID, out var item))
                    {
                        if (!item.Check(ev.Player))
                        {
                            ev.IsAllowed = false;
                        }
                    }
                    else
                    {
                        ev.IsAllowed = false;
                    }
                    if (!ev.IsAllowed)
                    {
                        p.AddMessage("messID", "<size=40><color=red>你必须携带055逃离!</color></size>", 2f, ScreenLocation.CenterBottom);

                    }
                    else
                    {
                        Timing.CallDelayed(30f, () =>
                        {
                            Scp055Escaped = true;
                            if (Round.IsStarted)
                                Round.EndRound(true);
                        });
                        p.AddMessage("messID", "<size=40><color=red>你携带055成功逃离! 30秒后回合结束!</color></size>", 3f, ScreenLocation.CenterBottom);
                    }
                }
            }
            protected override void SubscribeEvents()
            {
                Exiled.Events.Handlers.Player.Escaping += Escaping;
                base.SubscribeEvents();
            }

            protected override void UnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.Escaping -= Escaping;
                base.UnsubscribeEvents();
            }
            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.2f, () =>
                {
                    if (player != null)
                    {
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });
            }
        }
        public static uint Scp055ItemID = 5055;
        [CustomItem(ItemType.Lantern)]
        public class scp5k_055 : CustomItem
        {
            public override uint Id { get; set; } = Scp055ItemID;
            public override string Name { get; set; } = "SCP-055";
            public override string Description { get; set; } = "他不是圆的";

            public override float Weight { get; set; } = 55;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            public override Vector3 Scale { get; set; } = new Vector3(2f, 2f, 2f);
            protected override void OnOwnerChangingRole(OwnerChangingRoleEventArgs ev)
            {
                foreach (var item in ev.Player.Items)
                {
                    if (Check(item))
                    {
                        ev.Player.DropItem(item);
                    }
                }
                base.OnOwnerChangingRole(ev);
            }
            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                    base.OnUpgrading(ev);
                }
            }
            void SearchingItem(SearchingPickupEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    if (scp5k_Sci.TryGet(SciID, out var item))
                    {
                        if (!item.Check(ev.Player))
                        {
                            ev.IsAllowed = false;
                        }
                    }
                    else
                    {
                        ev.IsAllowed = false;

                    }
                }
            }
            protected override void ShowPickedUpMessage(Player player)
            {
                return;
            }
            protected override void SubscribeEvents()
            {
                Exiled.Events.Handlers.Player.SearchingPickup += SearchingItem;
                base.SubscribeEvents();
            }
            protected override void UnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.SearchingPickup -= SearchingItem;
                base.UnsubscribeEvents();
            }
            public override void Init()
            {
                base.Init();
            }
        }
        public static uint GocBombItemId = 5514;
        [CustomItem(ItemType.Coin)]
        public class scp5k_GocBomb : CustomItem
        {
            public override uint Id { get; set; } = GocBombItemId;
            public override string Name { get; set; } = "Goc奇术发生器";
            public override string Description { get => $"要安放在{GOCBomb.installCount}个互相离得最远的重收房间"; set { } }

            public override float Weight { get; set; } = 25;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            public override Vector3 Scale { get; set; } = new Vector3(5f, 5f, 5f);
            protected override void OnOwnerChangingRole(OwnerChangingRoleEventArgs ev)
            {
                foreach (var item in ev.Player.Items)
                {
                    if (Check(item))
                    {
                        ev.Player.DropItem(item);
                        break;
                    }
                }
                base.OnOwnerChangingRole(ev);
            }

            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                    base.OnUpgrading(ev);
                }
            }
            void SearchingItem(SearchingPickupEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    if (scp5k_Sci.TryGet(GocPID, out var item))
                    {
                        if (item.Check(ev.Player))
                        {
                            ev.IsAllowed = true;
                            return;

                        }
                    }
                    if (scp5k_Sci.TryGet(GocCID, out var item2))
                    {
                        if (item2.Check(ev.Player))
                        {
                            ev.IsAllowed = true;
                            return;
                        }
                    }
                    ev.IsAllowed = false;
                    if (!LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub).HasMessage("No!"))
                    {
                        LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub).AddMessage("No!", "<color=red><size=27>此物品为goc专属</size></color>", 3f, ScreenLocation.Center);
                    }

                }
            }
            protected override void ShowSelectedMessage(Player player)
            {
                string w = "";
                if (GOCBomb.installAt.Where(x => !GOCBomb.installedRoom.Any(y => y.Value == x)).Count() != 0)
                {
                    foreach (var item in GOCBomb.installAt.Where(x => !GOCBomb.installedRoom.Any(y => y.Value == x)))
                    {
                        w += $"{item.Type} ";
                    }
                    LabApi.Features.Wrappers.Player.Get(player.ReferenceHub).AddMessage("Wait", $"<color=green><size=20>还剩下没有安装的房间:{w}</size></color>", 3f, ScreenLocation.Center);
                }
                else
                {
                    LabApi.Features.Wrappers.Player.Get(player.ReferenceHub).AddMessage("Wait", $"<color=green><size=27>安装完成</size></color>", 3f, ScreenLocation.Center);

                }

            }
            static CachedLayerMask RoomDetectionMask = new CachedLayerMask(new string[]
{
            "Default",
            "InvisibleCollider",
            "Fence",
            "Glass","Door",
            "CCTV"
});
            public void Flip(FlippingCoinEventArgs ev)
            {

                try
                {
                    //Log.Info($">>> OnUsedItem 被触发！玩家: {ev.Player.Nickname}，物品类型: {ev.Item.Type}");

                    var GocC4 = CustomItem.Get(GocBombItemId);
                    if (GocC4 == null)
                    {
                        Log.Error("❌ GocC4 自定义物品未找到！GocBombItemId = " + GocBombItemId);
                        return;
                    }

                    if (!GocC4.Check(ev.Item))
                    {
                        //Log.Info($"❌ 当前物品不是 GocC4，类型为: {ev.Item.Type}");
                        return;
                    }
                    //Log.Info("✅ GocC4 检查通过");

                    var lp = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);
                    if (lp == null)
                    {
                        //Log.Error("❌ 无法获取 LabApi Player Wrapper");
                        return;
                    }
                    Vector3 Install_Pos = ev.Player.Position;
                    //Vector3 rotateDir = ev.Player.CameraTransform.forward;
                    //if (Physics.Raycast(ev.Player.CameraTransform.position, rotateDir, out RaycastHit hitInfo, 10f, RoomDetectionMask.Mask))
                    //{
                    //    rotateDir = hitInfo.normal;
                    //    Install_Pos = hitInfo.point + (hitInfo.normal * 0.1f); // 沿法线方向偏移0.1单位,防止嵌入墙内
                    //}
                    //else
                    //{
                    //    lp.AddMessage("no", "<color=red>请对着墙安装</color>", 3f);
                    //    return;
                    //}
                    var currentRoom = Room.Get(Install_Pos);
                    if (currentRoom == null)
                    {
                        //Log.Warn("玩家当前房间为 null");
                        lp.AddMessage("no", "<color=red>无法获取房间</color>", 3f);
                        return;
                    }

                    if (GOCBomb.installAt.Contains(currentRoom))
                    {
                        //Log.Info($"✅ 玩家在允许安装的房间: {currentRoom.Name}");

                        if (GOCBomb.installedRoom.Any(x => x.Value == currentRoom))
                        {
                            //Log.Info("❌ 房间已安装过炸弹");/
                            lp.AddMessage("NO!", "<color=red><size=27>该房间已安装!</size></color>", 3f);
                            return;
                        }

                        if ((scp5k_Sci.TryGet(GocPID, out var sciRole) && sciRole.Check(ev.Player)) || (scp5k_Sci.TryGet(GocCID, out var sciCRole) && sciCRole.Check(ev.Player)))
                        {
                            //Log.Info("✅ 玩家拥有安装权限");

                           
                            //var pickup = ev.Item.CreatePickup(Install_Pos,Quaternion.Euler(rotateDir),true);
                            var pickup = ev.Item.CreatePickup(Install_Pos);
                            ev.Item.Destroy();
                            
                            pickup.Rigidbody.isKinematic = true;
                            pickup.PhysicsModule.Rb.isKinematic = true;
                            foreach (var item in pickup.GameObject.transform.GetComponentsInChildren<NetworkIdentity>())
                            {
                                Exiled.API.Extensions.MirrorExtensions.EditNetworkObject(item, (_) => { });
                            }
                            
                            if (pickup != null)
                            {
                                GOCBomb.installbomb(pickup);
                                Log.Info($"💣 炸弹成功安装在房间: {currentRoom.Name}");
                            }
                            else
                            {
                                //Log.Warn("❌ DropItem 返回 null，安装失败");
                                lp.AddMessage("no", "<color=red>丢弃物品失败，请重试</color>", 3f);
                            }
                        }
                        else

                        {
                            //Log.Info("❌ 玩家没有 GocPID 权限");
                            lp.AddMessage("NO!", "<color=red>你没有权限安装此炸弹</color>", 3f);
                        }
                    }
                    else
                    {
                        //Log.Info($"❌ 当前房间不允许安装: {currentRoom.Name}，允许的房间: {string.Join(", ", GOCBomb.installAt.Select(r => r?.Name ?? "null"))}");

                        lp.AddMessage("NO!", "<color=red><size=27>不在该房间安装!</size></color>", 3f);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("OnUsedItem 发生异常: " + ex);
                }
            }

            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                if (Check(ev.Item))
                {
                    ev.IsAllowed = false;
                    var p = ev.Item.CreatePickup(ev.Player.Position, ev.Player.Rotation, true);
                    p.Scale = this.Scale;
                    ev.Player.RemoveItem(ev.Item);
                }
                base.OnDroppingItem(ev);
            }
            protected override void SubscribeEvents()
            {
                Exiled.Events.Handlers.Player.SearchingPickup += SearchingItem;

                Exiled.Events.Handlers.Player.FlippingCoin += Flip;
                base.SubscribeEvents();
            }



            protected override void UnsubscribeEvents()
            {

                Exiled.Events.Handlers.Player.SearchingPickup -= SearchingItem;
                Exiled.Events.Handlers.Player.FlippingCoin -= Flip;
                base.UnsubscribeEvents();
            }
            public override void Init()
            {
                base.Init();
            }
        }

        public static uint AEHItemID = 5159;
        [CustomItem(ItemType.ArmorHeavy)]
        public class scp5k_AEH : CustomArmor
        {
            public override uint Id { get; set; } = AEHItemID;
            public override string Name { get; set; } = "绝对排斥护具";
            public override string Description { get; set; } = "主动隐身45秒";
            public override float Weight { get; set; } = 55;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            public override Vector3 Scale { get; set; } = new Vector3(2f, 2f, 2f);
            protected override void ShowPickedUpMessage(Player player)
            {
                player.Broadcast(4, "", global::Broadcast.BroadcastFlags.Normal, true);
                var p = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);

                p.AddMessage("messID", "<size=28><color=red>你获得了绝对排斥护具,请查看Server-Specific修改按键</color></size>", 4f, ScreenLocation.CenterBottom);

                base.ShowPickedUpMessage(player);
            }
            protected override void OnPickingUp(PickingUpItemEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    if (!Plugin.MenuCache.Any(x => x.Id == Plugin.plugin.Config.SettingIds[Features.AEHKey]))
                        Plugin.MenuCache.AddRange(MenuInit());
                    SettingBase.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));
                    SettingBase.Register(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));
                    base.OnPickingUp(ev);
                }
            }
            void SearchingItem(SearchingPickupEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    var p = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);
                    //

                }
            }
            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                if (Check(ev.Item))
                {
                    SettingBase.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));
                }
                base.OnDroppingItem(ev);
            }
            // 修复 OnOwnerChangingRole 方法中的集合修改异常
            protected override void OnOwnerChangingRole(OwnerChangingRoleEventArgs ev)
            {
                // 先收集需要移除的 item，避免在遍历时修改集合
                var itemsToDrop = ev.Player.Items.Where(Check).ToList();
                foreach (var item in itemsToDrop)
                {
                    ev.Player.DropItem(item);
                    SettingBase.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));
                }
                base.OnOwnerChangingRole(ev);
            }
            protected override void SubscribeEvents()
            {
                Exiled.Events.Handlers.Player.SearchingPickup += SearchingItem;
                base.SubscribeEvents();
            }
            protected override void UnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.SearchingPickup -= SearchingItem;
                base.UnsubscribeEvents();
            }
            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                    base.OnUpgrading(ev);
                }
            }
            public static Dictionary<string, Stopwatch> CoolDowns = new Dictionary<string, Stopwatch>();
            public List<SettingBase> MenuInit()
            {
                var settings = new List<SettingBase>();
                settings.Add(new HeaderSetting(Plugin.Instance.Config.SettingIds[Features.Scp5kHeader], "5k插件"));

                settings.Add(new KeybindSetting(
                    Plugin.Instance.Config.SettingIds[Features.AEHKey], "隐身", KeyCode.F8, false, false, "隐身45秒",

                    onChanged: (player, SB) =>
                    {
                        try
                        {
                            var CoolDown = new Stopwatch();
                            if (CoolDowns.TryGetValue(player.UserId, out var cd))
                            {
                                CoolDown = cd;

                            }
                            else
                            {
                                CoolDowns[player.UserId] = CoolDown;
                            }
                            if (CustomArmor.TryGet(AEHItemID, out var item))
                            {
                                var p = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);

                                foreach (var items in player.Items)
                                {
                                    if (item.Check(items))
                                    {
                                        if (CoolDown.Elapsed.TotalSeconds > 105 || !CoolDown.IsRunning)
                                        {
                                            CoolDown.Restart();
                                            player.EnableEffect(Exiled.API.Enums.EffectType.Fade, 255, 45);

                                            p.AddMessage("messID", "<color=red>成功隐身</color>", 3f, ScreenLocation.CenterBottom);
                                            Timing.CallDelayed(45, () =>
                                            {
                                                p.AddMessage("messID", "<color=red>隐身结束</color>", 3f, ScreenLocation.CenterBottom);
                                                CoolDown.Restart();
                                            });
                                            break;
                                        }
                                        else
                                        {
                                            p.AddMessage("messID", "<color=red>冷却中</color>", 3f, ScreenLocation.CenterBottom);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());

                        }
                    }));
                return settings;
            }
            public override void Init()
            {
                // 将 Plugin.plugin.MenuCache 替换为 Plugin.MenuCache
                if (!Plugin.MenuCache.Any(x => x.Id == Plugin.plugin.Config.SettingIds[Features.AEHKey]))
                    Plugin.MenuCache.AddRange(MenuInit());
                base.Init();
            }
        }
        public static uint BombgunItemID = 5808;
        [CustomItem(ItemType.GunRevolver)]
        public class scp5k_bombgun : CustomWeapon
        {
            public override uint Id { get; set; } = BombgunItemID;
            public override string Name { get; set; } = "榴弹枪";
            public override float Damage { get; set; } = 0;
            public override string Description { get; set; } = "";
            public override float Weight { get; set; } = 2;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            public override Vector3 Scale { get; set; } = new Vector3(2f, 2f, 2f);
            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                    base.OnUpgrading(ev);
                }
            }
            protected override void OnAcquired(Player player, Item item, bool displayMessage)
            {
                if (Check(item))
                {
                    BombHandle.RegisterAGun(item);
                }
                base.OnAcquired(player, item, displayMessage);
            }
            public override void Init()
            {
                base.Init();
            }
        }
    }
}
// i dont want to do this