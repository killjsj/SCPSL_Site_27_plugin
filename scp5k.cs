using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Doors;
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
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.Enums;
using Next_generationSite_27.Features.PlayerHuds;
using Org.BouncyCastle.Tls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.PlayableScps.Scp106;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
        public static void WarheadDetonated(Exiled.Events.EventArgs.Warhead.DetonatingEventArgs ev)
        {
            if (Is5kRound && GocSpawned)
            {
                Round.EndRound(true);
            }
            GocSpawnable = false;
        }
        public static int UiUSpawnTime = config.UiUSpawnTime - config.UiUSpawnFloatTime + UnityEngine.Random.Range(0, config.UiUSpawnFloatTime * 2);
        public static int AndSpawnTime = config.AndSpawnTime - config.AndSpawnFloatTime + UnityEngine.Random.Range(0, config.AndSpawnFloatTime * 2);
        public static bool UiuDownloadBroadcasted = false;
        public static bool UiuSpawned = false;
        public static bool HammerSpawned = false;
        public static bool GocSpawned = false;
        public static bool GocSpawnable = true;
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
                            var UiuWave = new List<Player>(diedPlayer.Take(Math.Min( config.UiuMaxCount,diedPlayer.Count-1)));
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
                        if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && !GocSpawned && GocSpawnable)
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
                        else if(Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && GocSpawnable)
                        {
                        Log.Info("small goc");
                            if (CustomRole.TryGet(31, out var role))
                            {
                                if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && GocTimer.Elapsed.TotalSeconds >= config.GocSpawnTime && GocSpawnable)
                                {
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
                        Log.Info("andbot");
                            if (CustomRole.TryGet(botID, out var role))
                            {
                                if (role.TrackedPlayers.Count < config.AndMaxCount && AndTimer.Elapsed.TotalSeconds >= 220)
                                {
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
                        Log.Info("Hammer");
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
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.5f);
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
            HammerSpawned = false; 
            UiuDownloadBroadcasted = false;
            UiUSpawnTime = config.UiUSpawnTime - config.UiUSpawnFloatTime + UnityEngine.Random.Range(0, config.UiUSpawnFloatTime * 2);
            AndSpawnTime = config.AndSpawnTime - config.AndSpawnFloatTime + UnityEngine.Random.Range(0, config.AndSpawnFloatTime * 2);
            UiuSpawned = false;
            GocSpawned = false;
            GocSpawnable = true;
            AndTimer = new Stopwatch();
            GocTimer = new Stopwatch();
            if (refresher.IsRunning)
            {
                Timing.KillCoroutines(refresher);
            }
            Timing.CallDelayed(0.2f, () =>
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
                            CustomItem.TrySpawn(Scp055ItemID, s055Pos.Position + new Vector3(0f, 1f, 0f), out var s5);
                            CustomItem.TrySpawn(AEHItemID, AEHPos.Position + new Vector3(0f, 1f, 0f), out var aeh);
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
                            Log.Info("refresher start");

                            refresher = MEC.Timing.RunCoroutine(Refresher());

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
            [RoleTypeId.Tutorial] = 1,
            [RoleTypeId.CustomRole] = 1,
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
        public static void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (Is5kRound)
            {
                var p = LabApi.Features.Wrappers.Player.Get(ev.Player.ReferenceHub);
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
                            p.AddMessage("messID", "你是 清收势力 尽与反基金会势力合作可能的逃跑\n<color=yellow>中立:GOC,UIU,ClassD,科学家,保安,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);

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
                            p.AddMessage("messID", "你是 反基金会势力 消灭一切基金会团队的成员\n <color=green>友好:GOC,UIU,混沌,安德森机器人</color>\n<color=yellow>中立:ClassD,科学家,保安</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                            ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(AntiSCPFF);
                            break;
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
                if (Scp055Escaped)
                {
                    foreach (var s in LabApi.Features.Wrappers.Player.List)
                    {
                        PlayerHudUtils.AddMessage(s, "messid", "Normal Ending:055进入了579 宇宙重启");
                    }
                    ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.Draw;
                }
                else if (Warhead.IsDetonated)
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
                    else
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

        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_C : CustomRole
        {

            public override uint Id { get; set; } = 32;
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
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU成员</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往办公有电脑的地方下载资料 下载完资料后撤离</color></size>", 4);
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
                float time = 0f;
                bool Downloaded = false;
                var p = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);
                var hud = PlayerHud.Get(player);

                while (true)
                {
                    try
                    {
                        if (!Check(player))
                        {
                            break;
                        }
                        if (!Downloaded)
                        {
                            if (player.CurrentRoom != null)
                            {
                                if (player.CurrentRoom.RoomName == MapGeneration.RoomName.EzOfficeSmall || player.CurrentRoom.RoomName == MapGeneration.RoomName.EzOfficeLarge)
                                {
                                    time += 0.2f;
                                    if (time >= 300f)
                                    {
                                        hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage("messID", "<size=30><color=red>你已成功下载资料,请尽快撤离!</color></size>", 04f, ScreenLocation.CenterBottom));
                                        Downloaded = true;
                                        if (hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.RemoveMessage("UIUdownloading");
                                        }
                                    }
                                    else
                                    {
                                        if(time >= 120f && !UiuDownloadBroadcasted)
                                        {
                                            Cassie.MessageTranslated("Security alert . U I U down load activity detected . Security personnel , proceed with standard protocols", "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。前往办公区");
                                            UiuDownloadBroadcasted = true;
                                        } else if (time <= 20 )
                                        {
                                            UiuDownloadBroadcasted = false;

                                        }
                                        //p.SendConsoleMessage($"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {time:F0} 秒 下载结束: 300 秒</color></size>");
                                        if (!hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.DynamicMessage(
                                                    "UIUdownloading",
                                                    p,
                                                    (x) => new string[] { $"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {time:F0} 秒 下载结束: 300 秒</color></size>" },
                                                    2f,
                                                    ScreenLocation.CenterBottom
                                                ));
                                        }
                                    }
                                }
                                else
                                {
                                    if (time > 0f)
                                        time -= 0.4f;
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
                if (player != null)
                {
                    CH[player.UserId] = MEC.Timing.RunCoroutine(PlayerUpdate(player));
                    player.Position = new Vector3(0, 302, -41);
                }
            }
        }
        [CustomRole(RoleTypeId.Tutorial)]

        public class scp5k_Uiu_P : CustomRole
        {

            public override uint Id { get; set; } = 28;
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
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU成员</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往办公有电脑的地方下载资料 下载完资料后撤离</color></size>", 4);

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
                float time = 0f;
                bool Downloaded = false;
                var p = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);
                var hud = PlayerHud.Get(player);

                while (true)
                {
                    try
                    {
                        if (!Check(player))
                        {
                            break;
                        }
                        if (!Downloaded)
                        {
                            if (player.CurrentRoom != null)
                            {
                                if (player.CurrentRoom.RoomName == MapGeneration.RoomName.EzOfficeSmall || player.CurrentRoom.RoomName == MapGeneration.RoomName.EzOfficeLarge)
                                {
                                    time += 0.2f;
                                    if (time >= 300f)
                                    {
                                        hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage("messID", "<size=30><color=red>你已成功下载资料,请尽快撤离!</color></size>", 04f, ScreenLocation.CenterBottom));
                                        if (hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.RemoveMessage("UIUdownloading");
                                        }
                                        Downloaded = true;
                                    }
                                    else
                                    {
                                        if (time >= 120f && !UiuDownloadBroadcasted)
                                        {
                                            Cassie.MessageTranslated("Security alert . U I U down load activity detected . Security personnel , proceed with standard protocols", "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。前往办公区");
                                            UiuDownloadBroadcasted = true;
                                        }
                                        else if (time <= 20)
                                        {
                                            UiuDownloadBroadcasted = false;

                                        }
                                        //p.SendConsoleMessage($"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {time:F0} 秒 下载结束: 300 秒</color></size>");
                                        if (!hud.HasMessage("UIUdownloading"))
                                        {
                                            hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.DynamicMessage(
                                                    "UIUdownloading",
                                                    p,
                                                    (x) => new string[] { $"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已持续: {time:F0} 秒 下载结束: 300 秒</color></size>" },
                                                    2f,
                                                    ScreenLocation.CenterBottom
                                                ));
                                        }
                                    }
                                }
                                else
                                {
                                    if (time > 0f)
                                        time -= 0.4f;
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
                if (player != null)
                {
                    CH[player.UserId] = MEC.Timing.RunCoroutine(PlayerUpdate(player));
                    player.Position = new Vector3(0, 302, -41);
                }
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

                if (player != null)
                {
                    totalLives += config.AndLives;
                    player.Position = Room.Get(Exiled.API.Enums.RoomType.EzGateA).Position + new UnityEngine.Vector3(0, 3f, 0);

                    //MEC.Timing.RunCoroutine(PlayerUpdate(player));
                }
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
        [CustomRole(RoleTypeId.Tutorial)]

        public class scp5k_Goc_C : CustomRole
        {

            public override uint Id { get; set; } = 30;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc_C";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; }
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "与反scp基金会势力合作 \n 开启核弹毁灭站点";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 120;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc特工</color></size>\n<size=30><color=yellow>开启核弹毁灭站点</color></size>", 4);
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
                if (player != null)
                {
                    //MEC.Timing.RunCoroutine(PlayerUpdate(player));
                    player.Position = new Vector3(16, 292, -41);
                }
                GocSpawned = true;
                base.RoleAdded(player);
            }
        }
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_P : CustomRole
        {

            public override uint Id { get; set; } = 31;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc_P";
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
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU队长</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n下载完资料后撤离</color></size>", 4);
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
                if (player != null)
                {
                    //MEC.Timing.RunCoroutine(PlayerUpdate(player));
                    player.Position = new Vector3(16, 292, -41);
                }
                GocSpawned = true;

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
                if (player != null)
                {
                }
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
    }
}
// i dont want to do this