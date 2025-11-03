using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Speakers;
using CommandSystem.Commands.Console;
using Decals;
using DrawableLine;
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
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using Exiled.Events.Patches.Events.Player;
using Footprinting;
using GameObjectPools;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LightContainmentZoneDecontamination;
using MapGeneration;
using MapGeneration.StaticHelpers;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.SpawnPorject;
using Next_generationSite_27.UnionP.UI;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Tls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using ProjectMER.Commands.Modifying.Rotation;
using ProjectMER.Commands.Utility;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Objects;
using RelativePositioning;
using Respawning;
using Respawning.NamingRules;
using Respawning.Waves;
using Respawning.Waves.Generic;
using RoundRestarting;
using Subtitles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.Networking;
using VoiceChat.Networking;
using YamlDotNet.Core.Tokens;
using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
using static RoundSummary;
using Object = UnityEngine.Object;
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
            foreach (var s in Player.Enumerable)
            {
                s.AddMessage("UIU_ESCAPED" + DateTime.Now.ToString(), "<size=40><color=red>UIU已撤离</color></size>");
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
                        return (100f / 90f) / 5f * count;
                    }
                }
                return 0.2f; // 默认极慢速度（无人时）
            }
        }
        public static Stopwatch Decont_Counter = new Stopwatch();
        public static bool Decont_NextIsOepn
        {
            get => _Decont_NextIsOepn; set
            {
                _Decont_NextIsOepn = value;
                Decont_Counter.Restart();
            }
        }
        public static bool _Decont_NextIsOepn = true;
        //public static double DecontTotalSeconds = 480;
        //public static double DecontOepnSeconds = 320;
        public static double DecontTotalSeconds = 310;
        public static double DecontOepnSeconds = 60;
        public static IEnumerator<float> DecontUpdate()
        {
            if (!Is5kRound)
            {
                yield break;
            }
            while (!Round.InProgress)
            {
                yield return Timing.WaitForSeconds(1f);
            }
            while (!Map.IsLczDecontaminated)
            {
                if (Round.IsEnded)
                {
                    yield break;
                }
                yield return Timing.WaitForSeconds(1f);
            }

            Log.Info("IsLczDecontaminated");
            Decont_NextIsOepn = true;
            while (Round.InProgress)
            {
                try
                {
                    if (Decont_NextIsOepn)
                    {
                        DecontaminationController.Singleton.Network_elevatorsLockedText = $"下一次开放:{DecontOepnSeconds - Decont_Counter.Elapsed.TotalSeconds:F0}";
                        if (Decont_Counter.Elapsed.TotalSeconds >= DecontOepnSeconds)
                        {
                            Decont_NextIsOepn = false;
                            DecontaminationController.Singleton.DecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
                            Cassie.MessageTranslated("LIGHT CONTAINMENT Decontamination has been stoped .", "轻收容已重新开放.");
                        }
                    }
                    else
                    {
                        DecontaminationController.Singleton.Network_elevatorsLockedText = $"下一次封闭:{DecontTotalSeconds - Decont_Counter.Elapsed.TotalSeconds:F0}";
                        //switch (Decont_Counter.Elapsed.TotalSeconds)
                        {
                            if ((int)Decont_Counter.Elapsed.TotalSeconds == DecontTotalSeconds - 5 * 60 + 5)
                            {
                                DefaultAudioManager.Instance.PlayGlobalAudioWithFilter("decont_5", false, 1f, AudioManagerAPI.Features.Enums.AudioPriority.Max, x =>
                                {
                                    if (x is ISpeakerWithPlayerFilter filterSpeaker)
                                    {
                                        filterSpeaker.SetValidPlayers(p => p.Zone == FacilityZone.LightContainment || (p.CurrentlySpectating != null && p.CurrentlySpectating.Zone == FacilityZone.LightContainment));

                                    }
                                });


                                List<SubtitlePart> list = new List<SubtitlePart>(1);
                                list.Add(new SubtitlePart(SubtitleType.DecontaminationMinutes, new string[]
{
                                                                "5"
}));
                                foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
                                {
                                    if (IsAudibleForClient(referenceHub))
                                    {
                                        new SubtitleMessage(list.ToArray()).SendToSpectatorsOf(referenceHub, true);
                                    }
                                }
                                //break;
                            }
                            else if ((int)Decont_Counter.Elapsed.TotalSeconds == DecontTotalSeconds - 1 * 60 + 5)
                            {
                                DefaultAudioManager.Instance.PlayGlobalAudioWithFilter("decont_1", false, 1f, AudioManagerAPI.Features.Enums.AudioPriority.Max, x =>
                                {
                                    if (x is ISpeakerWithPlayerFilter filterSpeaker)
                                    {
                                        filterSpeaker.SetValidPlayers(p => p.Zone == FacilityZone.LightContainment || (p.CurrentlySpectating != null && p.CurrentlySpectating.Zone == FacilityZone.LightContainment));

                                    }
                                });

                                List<SubtitlePart> list = new List<SubtitlePart>(1);
                                list.Add(new SubtitlePart(SubtitleType.Decontamination1Minute, null));
                                foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
                                {
                                    if (IsAudibleForClient(referenceHub))
                                    {
                                        new SubtitleMessage(list.ToArray()).SendToSpectatorsOf(referenceHub, true);
                                    }
                                }
                                //break;
                            }
                            else if ((int)Decont_Counter.Elapsed.TotalSeconds == DecontTotalSeconds - 38)
                            {
                                DefaultAudioManager.Instance.PlayGlobalAudioWithFilter("decont_countdown", false, 1f, AudioManagerAPI.Features.Enums.AudioPriority.Max, x =>
                                {
                                    if (x is ISpeakerWithPlayerFilter filterSpeaker)
                                    {
                                        filterSpeaker.SetValidPlayers(p => p.Zone == FacilityZone.LightContainment || (p.CurrentlySpectating != null && p.CurrentlySpectating.Zone == FacilityZone.LightContainment));
                                    }
                                });
                                List<SubtitlePart> list = new List<SubtitlePart>(1);
                                list.Add(new SubtitlePart(SubtitleType.DecontaminationCountdown, null));
                                foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
                                {
                                    if (IsAudibleForClient(referenceHub))
                                    {
                                        new SubtitleMessage(list.ToArray()).SendToSpectatorsOf(referenceHub, true);
                                    }
                                }
                                //break;
                            }

                            else if ((int)Decont_Counter.Elapsed.TotalSeconds == DecontTotalSeconds - 28)
                            {
                                DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconEvac);
                            }
                        }
                        if ((int)Decont_Counter.Elapsed.TotalSeconds >= DecontTotalSeconds)
                        {
                            Decont_NextIsOepn = true;
                            DecontaminationController.Singleton.DecontaminationOverride = DecontaminationController.DecontaminationStatus.Forced;
                            DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconFinish);
                            DefaultAudioManager.Instance.PlayGlobalAudioWithFilter("decont_begun", false, 1f, AudioManagerAPI.Features.Enums.AudioPriority.Max, x =>
                            {
                                if (x is ISpeakerWithPlayerFilter filterSpeaker)
                                {
                                    filterSpeaker.SetValidPlayers(p => IsAudibleForClient(p.ReferenceHub, true) || p.Zone == FacilityZone.LightContainment || (p.CurrentlySpectating != null && p.CurrentlySpectating.Zone == FacilityZone.LightContainment));

                                }
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Info(e);
                }
                yield return Timing.WaitForSeconds(1f);
            }
        }
        private static bool IsAudibleForClient(ReferenceHub hub, bool global = false)
        {
            if (global) return true;
            PlayerRoles.PlayableScps.Scp079.Scp079Role scp079Role = hub.roleManager.CurrentRole as PlayerRoles.PlayableScps.Scp079.Scp079Role;
            if (scp079Role != null)
            {
                return scp079Role.CurrentCamera.Room.Zone == FacilityZone.LightContainment;
            }
            if (hub.roleManager.CurrentRole is PlayerRoles.Spectating.SpectatorRole SR)
            {
                Player player = Player.Get(SR.SyncedSpectatedNetId);
                if (!(player.ReferenceHub != hub))
                {
                    return false;
                }

                return player.Zone.GetZone() == FacilityZone.LightContainment;
            }
            return hub.GetCurrentZone() == FacilityZone.LightContainment;
        }
        public static bool Spawn_Nuke_GOC = false;
        public static bool Nuke_GOC_WinCon = false;
        public static bool Nuke_GOC_COunt
        {
            get
            {
                if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                {
                    return false;
                }
                return Player.Enumerable.Any(x =>
                {
                    return x.UniqueRole == GocNukeP.Name || x.UniqueRole == GocNukeC.Name;
                });
            }
        }
        public static int Nuke_GOC_count
        {
            get
            {
                if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                {
                    return 0;
                }
                return Player.Enumerable.Count(x =>
                {
                    return x.UniqueRole == GocNukeP.Name || x.UniqueRole == GocNukeC.Name;
                });
            }
        }
        public static int RespawnedCount = 0;
        public static void RespawningTeam(RespawningTeamEventArgs ev)
        {
            if (!Is5kRound)
                return;

            SpawnableWaveBase newW = ev.Wave.Base;

            // 尝试从 ev 中安全获取玩家列表（反射以兼容不同版本）
            List<Player> players = ev.Players;


            // 如果没有从事件获取到玩家，则回退到 diedPlayer 快照（原代码意图）
            if (players.Count == 0)
            {
                players = diedPlayer.ToList();
            }

            RespawnedCount++;

            switch (ev.Wave.Faction)
            {
                case PlayerRoles.Faction.FoundationStaff:
                    {
                        if (RespawnedCount <= 2)
                        {
                            if (UnityEngine.Random.Range(0, 100) < 50)
                            {
                                ev.IsAllowed = false;
                                TrySpawnHammer(players, true);
                            }
                            else if (UnityEngine.Random.Range(0, 100) < 50)
                            {
                                TrySpawnNu22(players, true, true);
                                ev.IsAllowed = false;
                            }
                            else if (UnityEngine.Random.Range(0, 100) > 80)
                            {
                                TrySpawnO1(players, true, true); ev.IsAllowed = false;
                            }
                            else
                            {
                                // 保持原行为：允许重生
                            }
                        }
                        break;
                    }
                case PlayerRoles.Faction.FoundationEnemy:
                    {
                        if (RespawnedCount < 2)
                        {
                            if (UnityEngine.Random.Range(0, 100) < 50)
                            {
                                TrySpawnUiu(players); ev.IsAllowed = false;
                            }
                            else if (UnityEngine.Random.Range(0, 100) < 30)
                            {
                                TrySpawnGoc(players); ev.IsAllowed = false;
                            }
                            
                        }

                        break;
                    }
            }

            ev.Wave.Timer.SetTime(0);
        }
        public static bool DeadmanSwitchInitiated { get; set; } = false;
        public static bool Scp610Ending { get; set; } = false;
        public static void DeadmanSwitchInitiating(DeadmanSwitchInitiatingEventArgs ev)
        {
            if (IsO5NukeEnd)
            {
                return;
            }
            if (!Is5kRound)
            {
                return;
            }

            if (diedPlayer.Count < GOCBomb.installCount + 1)
            {
                return;
            }
            if (DeadmanSwitchInitiated)
            {
                ev.IsAllowed = false;
                return;
            }
            ev.IsAllowed = false;
            DeadmanSwitchInitiated = true;
            Scp610Ending = true;
            Cassie.MessageTranslated("BY ORDER OF O5 COMMAND . SCP 6 1 0 WILL BE REALLY SEE did INTO the facility", "根据O5指挥部指令 SCP-610即将被投放到设施");
            for (int i = 0; i < 6; i++)
            {
                Player player = Player.Enumerable.Where(x => x.IsAlive && !x.IsScp).GetRandomValue();
                scp5k_Scp610_mother.instance.AddRole(player);
            }
            Timing.CallDelayed(5f, () =>
            {
                TrySpawnGoc(diedPlayer, true);
            });

        }
        public static uint Scp610MID = 43;
        [CustomRole(RoleTypeId.Scp0492)]
        public class scp5k_Scp610_mother : CustomRole, IDeathBroadcastable
        {
            public static scp5k_Scp610_mother instance { get; private set; }
            public override uint Id { get => Scp610MID; set => Scp610MID = value; }
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Scp610母体";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Scp610母体";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override Vector3 Scale { get => base.Scale; set => base.Scale = value; }

            public string CassieBroadcast => "SCP 6 1 0";

            public string ShowingToPlayer => "SCP610";

            public override void Init()
            {
                Scale = new Vector3(1.2f, 1.2f, 1.2f);
                Description = "杀死所有非scp生物";
                this.Role = RoleTypeId.Scp0492;
                MaxHealth = 20000;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是SCP610母体</color></size>\n<size=30><color=yellow>杀死所有非scp生物</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                base.Init();
            }
            protected override void RoleAdded(Player player)
            {
                base.RoleAdded(player);
            }
        }
        public static uint Scp610SID = 46;
        [CustomRole(RoleTypeId.Scp0492)]
        public class scp5k_Scp610 : CustomRole
        {
            public static scp5k_Scp610 instance { get; private set; }
            public string CassieBroadcast => "SCP 6 1 0";

            public string ShowingToPlayer => "SCP610";
            public override uint Id { get => Scp610SID; set => Scp610SID = value; }
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Scp610子体";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Scp610子体";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "杀死所有非scp生物";
                this.Role = RoleTypeId.Scp0492;
                MaxHealth = 1200;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是SCP610子体</color></size>\n<size=30><color=yellow>杀死所有非scp生物</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                base.Init();
            }
            protected override void RoleAdded(Player player)
            {
                Timing.CallDelayed(0.4f, () =>
                {
                    player.EnableEffect(Exiled.API.Enums.EffectType.MovementBoost, 30, 0f);
                });
                base.RoleAdded(player);
            }
        }
        public static bool Nuke_GOC_Spawned = false;
        public static void WarheadDetonated(Exiled.Events.EventArgs.Warhead.DetonatingEventArgs ev)
        {
            if (Is5kRound && Nuke_GOC_Spawned && LastChangedWarheadIsGoc && !Player.Enumerable.Any(x => x.IsScp))
            {
                Nuke_GOC_WinCon = true;
                Round.EndRound(true);
            }
            GocSpawnable = false;
        }
        public static bool LastChangedWarheadIsGoc = false;
        public static void ChangingLeverStatus(Exiled.Events.EventArgs.Warhead.ChangingLeverStatusEventArgs ev)
        {
            if (Is5kRound)
            {
                if (ev.Player != null)
                {
                    if (ev.CurrentState && ev.IsAllowed && !Warhead.IsInProgress)
                    {
                        if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                        {
                            return;
                        }
                        if (ev.Player.UniqueRole == GocNukeP.Name || ev.Player.UniqueRole == GocNukeC.Name)
                        {
                            Cassie.MessageTranslated("", "GOC正在强制启动核弹!所有人前去关闭!");
                            LastChangedWarheadIsGoc = true;
                            Warhead.Start(false, trigger: ev.Player);
                        }
                    }
                    else if (Warhead.IsInProgress && !ev.CurrentState && !LastChangedWarheadIsGoc)
                    {

                        if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                        {
                            Warhead.Stop(ev.Player);
                            return;
                        }
                        if (ev.Player.UniqueRole == GocNukeP.Name || ev.Player.UniqueRole == GocNukeC.Name)
                        {
                            ev.Player.AddMessage($"FailedToControlWarhead_{ev.Player.Nickname}", "<size=27><color=red>无法控制核弹! 核弹已启用!</color></size>", 2f, ScreenLocation.Scp914);
                            ev.IsAllowed = false;
                        }
                        else
                        {
                            LastChangedWarheadIsGoc = false;
                            Warhead.Stop(ev.Player);
                        }
                    }
                }
            }
        }
        public static void Escaping(EscapingEventArgs ev)
        {
            if (Is5kRound)
            {
                ev.EscapeScenario = EscapeScenario.ClassD;
            }
        }
        public static bool IsMotherDied = false;
        public static void Died(DyingEventArgs ev)
        {
            if (ev.Attacker != null)
            {
                if (ev.Player.UniqueRole != "" || ev.Attacker.UniqueRole != "")
                {
                    string text2;
                    string str;
                    string text3;
                    string str2;
                    if (ConvertToDeath(ev.Player, out text2, out str))
                    {
                        GetTeamKillText(ev.Attacker, out text3, out str2);
                        Cassie.MessageTranslated($"SCP {str} CONTAINEDSUCCESSFULLY BY {str2}", $"{text2} 已被{text3}重新收容。");
                    }
                }
            }
            if (Is5kRound)
            {
                if (ev.IsAllowed)
                {
                    if (ev.Player.UniqueRole == scp5k_Scp610_mother.instance.Name)
                    {
                        IsMotherDied = true;
                        foreach (var p in Player.Enumerable)
                        {
                            if (p.UniqueRole == scp5k_Scp610.instance.Name)
                            {
                                p.Kill("Mother is dead");
                            }
                        }
                        Round.EndRound();
                    }
                }
            }
        }
        public static void AnnouncingScpTermination(AnnouncingScpTerminationEventArgs ev)
        {
            if (ev.Player.UniqueRole != "" || ev.Attacker.UniqueRole != "")
            {
                ev.IsAllowed = false;
            }

        }
        public static void PlayerDamaged(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (Is5kRound)
            {
                if (ev.Attacker != null)
                {

                    if (ev.DamageHandler.Base is Scp096DamageHandler DH)
                    {
                        DH.Damage = 99;
                    }
                    else if (ev.DamageHandler.Base is Scp049DamageHandler handler)
                    {
                        if (ev.Attacker.UniqueRole == scp5k_Scp610_mother.instance.Name)
                        {
                            if (ev.Player.UniqueRole == scp5k_Goc_610_C.ins.Name || ev.Player.UniqueRole == scp5k_Goc_610_P.ins.Name)
                            {
                                ev.Amount = 50;
                            }
                            else
                            {
                                ev.Player.DropItems();
                                Timing.CallDelayed(1f, () =>
                                {
                                    scp5k_Scp610.instance.AddRole(ev.Player);
                                });
                                return;
                            }
                        }
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
        }
        public static bool ConvertToDeath(Player role, out string withoutSpace, out string withSpace)
        {
            if (role.UniqueRole != "")
            {
                var r = CustomRole.Get(role.UniqueRole);
                if (r != null)
                {
                    if (r is IDeathBroadcastable DB)
                    {
                        withoutSpace = DB.ShowingToPlayer;
                        withSpace = DB.CassieBroadcast;
                        return withoutSpace != "" && withSpace != "";
                    }
                }
            }
            if (!role.Role.Type.IsScp())
            {
                withoutSpace = string.Empty;
                withSpace = string.Empty;
                return false;
            }
            if (role.Role == RoleTypeId.Scp0492)
            {
                withoutSpace = string.Empty;
                withSpace = string.Empty;
                return false;
            }
            ;
            if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role.Role, out var result))
            {
                withoutSpace = string.Empty;
                withSpace = string.Empty;
                return false;
            }
            else
            {
                NineTailedFoxAnnouncer.ConvertSCP(result.RoleName, out withoutSpace, out withSpace);
                withoutSpace = "SCP-" + withoutSpace;
                withSpace = "SCP " + withSpace;
                return withoutSpace != "SCP-" && withSpace != "SCP ";
            }
        }
        public static bool GetTeamKillText(in Player Attacker, out string withoutSpace, out string withSpace)
        {
            withSpace = "";
            withoutSpace = "";
            if (Attacker.UniqueRole != null)
            {
                var r = CustomRole.Get(Attacker.UniqueRole);
                if (r != null)
                {
                    if (r is IDeathBroadcaster DB)
                    {
                        withoutSpace = DB.ShowingToPlayer;
                        withSpace = DB.CassieBroadcast;
                        return true;
                    }
                }
            }
            switch (Attacker.Role.Team)
            {
                case Team.SCPs:
                case Team.Flamingos:
                    {
                        NineTailedFoxAnnouncer.ConvertSCP(Attacker.Role, out withoutSpace, out withSpace);
                        return true;
                    }
                case Team.FoundationForces:
                    {
                        UnitNamingRule unitNamingRule;
                        if (!NamingRulesManager.TryGetNamingRule(Attacker.Role.Team, out unitNamingRule))
                        {
                            withSpace = "CONTAINMENTUNIT UNKNOWN";
                            withoutSpace = "未知";
                            return true;
                        }
                        withSpace = unitNamingRule.TranslateToCassie(Attacker.UnitName);
                        withSpace = "CONTAINMENTUNIT " + withSpace;
                        withoutSpace = Attacker.UnitName;
                        return true;
                    }
                case Team.ChaosInsurgency:
                    withSpace = "CHAOSINSURGENCY";
                    withoutSpace = "混沌分裂者";
                    return true;
                case Team.Scientists:
                    withSpace = "SCIENCE PERSONNEL";
                    withoutSpace = "科学家";
                    return true;
                case Team.ClassD:
                    withSpace = "CLASSD PERSONNEL";
                    withoutSpace = "D级人员";
                    return true;
            }
            return false;
        }
        public interface IDeathBroadcastable
        {
            public string CassieBroadcast { get; }
            public string ShowingToPlayer { get; }
        }
        public interface IDeathBroadcaster
        {
            public string CassieBroadcast { get; }
            public string ShowingToPlayer { get; }
        }
        public static int GocKilledScp = 0;
        public static int UiUSpawnTime = config.UiUSpawnTime - config.UiUSpawnFloatTime + UnityEngine.Random.Range(0, config.UiUSpawnFloatTime * 2);
        public static int AndSpawnTime = config.AndSpawnTime - config.AndSpawnFloatTime + UnityEngine.Random.Range(0, config.AndSpawnFloatTime * 2);
        public static bool UiuDownloadBroadcasted = false;
        public static bool UiuSpawned = false;
        public static bool HammerSpawned = false;
        public static bool GocSpawnedOnce = false;
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
        public static List<Player> diedPlayer
        {
            get
            {
                var t = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.Spectator).ToList();
                t.ShuffleList();
                return t;
            }
        }
        public static Stopwatch AndTimer = new Stopwatch();
        public static Stopwatch GocTimer = new Stopwatch();
        public static PConfig config => Plugin.Instance.Config;
        public static IEnumerator<float> Refresher()
        {
            Log.Info("Refresher!");

            const float REFRESH_INTERVAL = 2.0f; // 每 2 秒刷新一次
            const float STATUS_CHECK_INTERVAL = 5.0f; // 每 5 秒更新一次阵营统计

            float statusTimer = 0f;
            float waveTimer = 0f;

            var tempList = new List<Player>(64);
            //var sw = Stopwatch.StartNew();

            while (true)
            {
                //sw.Restart();
                try
                {
                    if (GOCAnim.donating)
                    {
                        foreach (var item in Player.Enumerable)
                        {
                            item.EnableEffect(Exiled.API.Enums.EffectType.SoundtrackMute, 1, 600f);
                            item.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 1, 600f);
                        }
                    }

                    // --- 阵营统计与胜负判断（每 5 秒执行一次） ---
                    statusTimer += REFRESH_INTERVAL;
                    if (statusTimer >= STATUS_CHECK_INTERVAL && !Round.IsLocked)
                    {
                        statusTimer = 0f;

                        int ntfScp = 0;
                        //int lightRunner = 0;
                        int chaos = 0;
                        int Scp610C = 0;

                        foreach (var hub in Player.Enumerable)
                        {
                            var role = hub.Role.Type;

                            if (!role.IsAlive()) continue;
                            if (hub.UniqueRole == scp5k_Scp610_mother.instance.Name || hub.UniqueRole == scp5k_Scp610.instance.Name)
                            {
                                Scp610C++;
                            }
                            switch (role)
                            {
                                case RoleTypeId.NtfCaptain:
                                case RoleTypeId.NtfPrivate:
                                case RoleTypeId.NtfSergeant:
                                case RoleTypeId.NtfSpecialist:
                                case RoleTypeId.Scp049:
                                case RoleTypeId.Scp079:
                                case RoleTypeId.Scp096:
                                case RoleTypeId.Scp106:
                                case RoleTypeId.Scp173:
                                case RoleTypeId.Scp3114:
                                case RoleTypeId.Scp939:
                                    ntfScp++;
                                    break;

                                case RoleTypeId.FacilityGuard:
                                case RoleTypeId.ClassD:
                                case RoleTypeId.Scientist:
                                case RoleTypeId.ChaosConscript:
                                case RoleTypeId.ChaosMarauder:
                                case RoleTypeId.ChaosRepressor:
                                case RoleTypeId.ChaosRifleman:
                                    chaos++;
                                    break;
                            }
                        }

                        chaos += SpecRolesCount;
                        Log.Info($"[RoundCheck] chaos:{chaos} ntfScp:{ntfScp}");

                        if (ntfScp == 0 && chaos == 0)
                        {
                            Log.Info("[RoundCheck] 所有阵营灭绝 → 结束回合。");
                            Round.EndRound();
                        }
                        else if (ntfScp == 0 && chaos != 0)
                        {
                            Log.Info("[RoundCheck] NTF/SCP 全灭，Chaos 胜利。");
                            Round.EndRound();
                        }
                        else if (chaos == 0 && ntfScp != 0)
                        {
                            Log.Info("[RoundCheck] Chaos + Light 全灭，NTF/SCP 胜利。");
                            Round.EndRound();
                        }
                        if (Scp610C >= Player.Enumerable.Where(x => x.IsAlive).Count() - 2 && Scp610Ending)
                        {
                            Log.Info("[RoundCheck] Scp610胜利。");
                            Round.EndRound();

                        }
                        // GOC 胜利条件检测
                        if (Nuke_GOC_count > 0 && !Player.Enumerable.Any(x => x.IsScp) || IsMotherDied)
                        {
                            Nuke_GOC_WinCon = true;
                            Round.EndRound();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("[Refresher_Ender] " + ex);

                }

                // --- 波次与刷新控制 ---
                waveTimer += REFRESH_INTERVAL;

                if (!WaveSpawner.AnyPlayersAvailable)
                {
                    //Log.Debug($"sw:{sw.Elapsed.TotalSeconds}");
                    yield return Timing.WaitForSeconds(REFRESH_INTERVAL);
                    continue;
                }
                try
                {
                    // 临时死亡玩家列表（缓存+洗牌）
                    tempList.Clear();
                    tempList.AddRange(diedPlayer);
                    ShuffleListFast(tempList);

                    // --- UIU 生成 ---
                    if (Round.ElapsedTime.TotalSeconds > UiUSpawnTime && !UiuSpawned)
                    {
                        TrySpawnUiu(tempList);
                        UiuSpawned = true;
                    }

                    // --- GOC 生成 ---
                    else if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && !GocSpawnedOnce && GocSpawnable && tempList.Count >= GOCBomb.installCount + 1)
                    {
                        TrySpawnGoc(tempList);
                        GocSpawnedOnce = true;
                    }

                    // --- 小规模 GOC 补充波 ---
                    else if (Round.ElapsedTime.TotalSeconds > config.GocStartSpawnTime && GocSpawnable && tempList.Count >= GOCBomb.installCount)
                    {
                        if (GocTimer.Elapsed.TotalSeconds >= config.GocSpawnTime)
                        {
                            TrySpawnGocSmall(tempList);
                            GocTimer.Restart();
                        }
                    }

                    // --- AND Bot ---
                    else if (Round.ElapsedTime.TotalSeconds > AndSpawnTime && !Warhead.IsDetonated)
                    {
                        TrySpawnAndBots(tempList);
                    }

                    // --- Hammer ---
                    else if (Round.ElapsedTime.TotalSeconds > config.HammerStartSpawnTime && !HammerSpawned)
                    {
                        TrySpawnHammer(tempList);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("[Refresher] " + ex);
                }
                //Log.Debug($"sw:{sw.Elapsed.TotalSeconds}");
                yield return Timing.WaitForSeconds(REFRESH_INTERVAL);
            }
        }
        private static void ShuffleListFast<T>(List<T> list)
        {
            // Fisher–Yates 洗牌，O(n)
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
        private static void TrySpawnUiu(List<Player> candidates)
        {
            if (candidates.Count == 0) return;

            int count = Math.Min(config.UiuMaxCount, candidates.Count);
            var spawnList = candidates.Take(count).ToList();

            if (CustomRole.TryGet(28, out var uiuRole))
            {
                if (CustomRole.TryGet(32, out var pioneer))
                    pioneer.AddRole(spawnList[0]);

                foreach (var p in spawnList)
                    uiuRole.AddRole(p);

                Cassie.MessageTranslated(
                    "Security alert . Substantial U I U activity detected . Security personnel , proceed with standard protocols",
                    "安保警戒，侦测到UIU的活动。安保人员请继续执行标准协议。阻止下载资料"
                );
            }
        }
        private static void TrySpawnGoc(List<Player> candidates, bool Is610Time = false)
        {
            int count = Math.Min(config.GocMaxCount, candidates.Count);
            var wave = candidates.Take(count).ToList();

            uint cid = Spawn_Nuke_GOC ? GocNukeCID : Goc2CID;
            uint pid = Spawn_Nuke_GOC ? GocNukePID : Goc2PID;
            cid = Is610Time ? Goc610CID : cid;
            pid = Is610Time ? Goc610PID : cid;
            if (CustomRole.TryGet(cid, out var role))
            {
                if (CustomRole.TryGet(pid, out var pioneer))
                    pioneer.AddRole(wave[0]);

                foreach (var p in wave)
                {
                    if (UnityEngine.Random.Range(0, 100) >= 60)
                    {
                        role.AddRole(p);
                    }else
                    {
                        scp5k_Goc_nuke_scan.ins.AddRole(p);
                    }
                }

                Cassie.MessageTranslated(
                    "Security alert . Substantial G O C activity detected . Security personnel , proceed with standard protocols , Protect the warhead",
                    "安保警戒，侦测到大量GOC的活动。安保人员请继续执行标准协议，保护核弹。"
                );
            }
        }
        private static void TrySpawnGocSmall(List<Player> candidates, bool Is610Time = false)
        {
            uint cid = Spawn_Nuke_GOC ? GocNukeCID : Goc2CID;
            uint pid = Spawn_Nuke_GOC ? GocNukePID : Goc2PID;
            cid = Is610Time ? Goc610CID : cid;
            pid = Is610Time ? Goc610PID : cid;
            if (!CustomRole.TryGet(cid, out var role)) return;

            int need = config.GocMaxCount - role.TrackedPlayers.Count;
            if (need <= 0) return;

            var wave = candidates.Take(need).ToList();
            if (CustomRole.TryGet(pid, out var pioneer))
                pioneer.AddRole(wave[0]);

            foreach (var p in wave)
            {
                if (UnityEngine.Random.Range(0, 100) >= 60)
                {
                    role.AddRole(p);
                }
                else
                {
                    scp5k_Goc_nuke_scan.ins.AddRole(p);
                }
            }

            Cassie.MessageTranslated(
                "Attention security personnel , G O C spotted at Gate A . Protect the warhead",
                "安保人员请注意，已在A大门处发现GOC，保护核弹。"
            );
        }
        private static void TrySpawnAndBots(List<Player> candidates)
        {
            if (!CustomRole.TryGet(botID, out var role)) return;

            if (role.TrackedPlayers.Count < config.AndMaxCount &&
                (AndTimer.Elapsed.TotalSeconds >= 220 || !AndTimer.IsRunning) &&
                AndRefreshCount <= config.AndRefreshMaxCount)
            {
                AndTimer.Restart();
                int need = config.AndMaxCount - role.TrackedPlayers.Count;
                var bots = candidates.Take(need).ToList();

                foreach (var p in bots)
                    role.AddRole(p);

                AndRefreshCount++;
                Cassie.MessageTranslated(
                    "Attention security personnel , And saw spotted at Gate A",
                    "安保人员请注意，已在A大门处发现安德森机器人"
                );
            }
        }
        private static void TrySpawnHammer(List<Player> candidates, bool imm = false)
        {
            int chaos = 0, scp = 0;

            foreach (var h in ReferenceHub.AllHubs)
            {
                var r = h.roleManager.CurrentRole.RoleTypeId;
                if (!r.IsAlive()) continue;
                if (r.IsScp() || r.IsNtf()) scp++; else chaos++;
            }

            if (chaos - scp > config.HammerSpawnCount)
            {
                Log.Info("Hammer wave triggered");
                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                if (w != null)
                {
                    if (w.RespawnTokens > 0)
                        w.RespawnTokens--;

                    w.Timer.Reset();
                    if (!imm) Exiled.API.Features.Respawn.SummonNtfChopper();
                    Timing.RunCoroutine(HammerSpawnCoroutine(w, candidates, imm));
                }
            }
        }

        private static IEnumerator<float> HammerSpawnCoroutine(NtfSpawnWave w, List<Player> spawntarget, bool imm = false)
        {
            if (!imm)
            {
                if (w != null)
                {
                    yield return Timing.WaitForSeconds(w.AnimationDuration);
                }
            }
            var HammerWave = new List<Player>(spawntarget.Take(Math.Min(config.HammerMaxCount, spawntarget.Count - 1)));
            if (HammerWave.Count == 0)
            {
                yield break;
            }
            spawntarget.RemoveRange(0, Math.Min(config.HammerMaxCount, spawntarget.Count - 1));
            scp5k_Nu7_P.instance.AddRole(HammerWave[0]);
            spawntarget.RemoveRange(0, 1);
            foreach (var item in HammerWave)
            {
                if (UnityEngine.Random.Range(0, 100) < 40)
                {
                    scp5k_Nu7_S.instance.AddRole(item);
                }
                else
                {
                    scp5k_Nu7_S.instance.AddRole(item);
                }


            }
            HammerSpawned = true;
            if (!HammerSpawnedBroadcast)
            {
                Cassie.MessageTranslated("Mobile Task Force Unit Nu 7 has entered the facility", "机动特遣队Nu-7小队已进入设施。");
                //HammerSpawnedBroadcast = true;
            }

            yield break;
        }
        public static uint Nu7PID = 44;
        [CustomRole(RoleTypeId.NtfCaptain)]
        public class scp5k_Nu7_P : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Nu7_P instance { get; private set; }
            public override uint Id { get; set; } = Nu7PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Nu-7 小队 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Nu-7 小队 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }

            public string CassieBroadcast => "NU 7";

            public string ShowingToPlayer => "Nu-7";

            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfCaptain;
                MaxHealth = 450;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Nu-7 小队 队长</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFCaptain),
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }

                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint Nu7SID = 45;
        [CustomRole(RoleTypeId.NtfSergeant)]
        public class scp5k_Nu7_S : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Nu7_S instance { get; private set; }
            public override uint Id { get; set; } = Nu7SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Nu-7 小队 重装";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Nu-7 小队 重装";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "NU 7";

            public string ShowingToPlayer => "Nu-7";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 350;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Nu-7 小队 重装</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunE11SR)
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint Nu22PID = 50;
        [CustomRole(RoleTypeId.NtfCaptain)]
        public class scp5k_Nu22_P : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Nu22_P instance { get; private set; }
            public override uint Id { get; set; } = Nu22PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Nu-22 小队 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Nu-22 小队 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }

            public string CassieBroadcast => "NU 22";

            public string ShowingToPlayer => "Nu-22";

            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfCaptain;
                MaxHealth = 450;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Nu-22 小队 队长</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFCaptain),
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }

                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint Nu22SID = 51;
        [CustomRole(RoleTypeId.NtfSergeant)]
        public class scp5k_Nu22_S : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Nu22_S instance { get; private set; }
            public override uint Id { get; set; } = Nu22SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Nu-22 小队 重装";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Nu-22 小队 重装";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "NU 2 2";

            public string ShowingToPlayer => "Nu-22";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 350;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Nu-22 小队 重装</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunE11SR)
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }

        public static uint Scp1440ID = 52;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Scp1440 : CustomRole, IDeathBroadcastable, IDeathBroadcaster
        {

            public static scp5k_Scp1440 instance { get; private set; }
            public override uint Id { get; set; } = Scp1440ID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "SCP-1440";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "SCP-1440";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "SCP 1 4 4 0";

            public string ShowingToPlayer => "SCP1440";
            public override void Init()
            {
                Description = "等待设施毁灭";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 2000;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 SCP-1440</color></size>\n<size=30><color=yellow>等待设施毁灭</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        Timing.RunCoroutine(Scp1440Update(player));
                        //SpeedBuildItem.instance.Give(player);
                        if (player.IsNPC)
                        {
                            var n = (Npc)Npc.Get(player);
                            //n.Follow
                            if (scp5k_Nu22_P.instance.TrackedPlayers.Count > 0)
                            {
                                n.Follow(scp5k_Nu22_P.instance.TrackedPlayers.Where(x => x.Zone == ZoneType.Surface).GetRandomValue());
                            }
                            else
                            if (scp5k_Nu22_S.instance.TrackedPlayers.Count > 0)
                            {
                                n.Follow(scp5k_Nu22_S.instance.TrackedPlayers.Where(x => x.Zone == ZoneType.Surface).GetRandomValue());
                            }

                        }
                    }
                });

                base.RoleAdded(player);
            }
        }
        public static int Scp1440DestoryTime = 350;
        public static Stopwatch Scp1440Timer = new Stopwatch();

        public static IEnumerator<float> Scp1440Update(Player player)
        {
            Scp1440Timer.Restart();
            while (true)
            {
                if (!player.IsAlive || player.Role.Type != RoleTypeId.Tutorial)
                {
                    yield break;
                }
                if (((int)Scp1440Timer.Elapsed.TotalSeconds) % 100 == 99)
                {
                    Cassie.Message($"SCP-1440目前在: {player.Zone} 所有人员保护SCP-1440", isSubtitles: true);
                }
                player.Heal(0.5f);
                if (Scp1440Timer.Elapsed.TotalSeconds >= Scp1440DestoryTime)
                {
                    Cassie.MessageTranslated("The facility is being destroyed in TMINUS 10 seconds . good BY.", "设施将在10秒后毁灭。再见。");
                    Timing.CallDelayed(10f, () =>
                    {
                        Warhead.Shake();
                        Timing.CallDelayed(0.2f, () =>
                        {
                            foreach (var item in Player.Enumerable)
                            {
                                ServerConsole.Disconnect(item.ReferenceHub.gameObject, "你被从现实中移除了 原因:SCP-1440毁灭了部分现实\n注:服务器正在重启 你可能无法在列表上看到服务器 等一等就好了");
                            }
                            Round.Restart(false, false, ServerStatic.NextRoundAction.Restart);
                        });
                    });

                    yield break;
                }
                yield return Timing.WaitForSeconds(1f);
            }
        }
        public static uint O1PID = 59;
        [CustomRole(RoleTypeId.NtfCaptain)]
        public class scp5k_Omega1_P : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Omega1_P instance { get; private set; }
            public override uint Id { get; set; } = O1PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Omega-1 小队 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Omega-1 小队 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Omega 1";

            public string ShowingToPlayer => "Omega-1";

            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfCaptain;
                MaxHealth = 70;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 队长</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                //string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFCaptain),
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.EnableEffect(EffectType.Fade, 255, 0f);

                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint O1SID = 58;
        [CustomRole(RoleTypeId.NtfSergeant)]
        public class scp5k_Omega1_S : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Omega1_S instance { get; private set; }
            public override uint Id { get; set; } = O1SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Omega-1 小队 奇术师";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Omega-1 小队 奇术师";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Omega 1";

            public string ShowingToPlayer => "Omega-1";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 50;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 奇术师</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunE11SR)
            };
                MenuInit();
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.EnableEffect(EffectType.Fade, 255, 0f);
                        Plugin.Register(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
            public static void MenuInit()
            {
                var m = new List<SettingBase>() {
                    new ButtonSetting(Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce], "修改引力", "", 0.5f, "使周围50m内所有人受到随机引力改变并给队友上伤害抗性", onChanged: (player, sb) =>
                    {
                        foreach (var p in Player.Enumerable)
                        {
                            if (Vector3.Distance(player.Position,p.Position) <= 50f)
                            {
                                if (!HitboxIdentity.IsEnemy(p.ReferenceHub,player.ReferenceHub))
                                {
                                    p.EnableEffect(EffectType.DamageReduction, 20, 30f);
                                }
                                if(p.Role is FpcRole fpcRole)
                                {
                                    Vector3 targetG = new Vector3(
                                        UnityEngine.Random.Range(-10f,10f),
                                        UnityEngine.Random.Range(-10f,10f),
                                        UnityEngine.Random.Range(-10f,10f)
                                        );
                                    fpcRole.Gravity += targetG;
                                    Timing.CallDelayed(30f, () =>
                                    {
                                        fpcRole.Gravity -= targetG;
                                    });
                                }
                            }
                        }
                    })
                };
                Plugin.MenuCache.AddRange(m);
            }
        }
        public static uint O1NID = 59;
        [CustomRole(RoleTypeId.NtfPrivate)]
        public class scp5k_Omega1_N : CustomRole, IDeathBroadcaster
        {

            public static scp5k_Omega1_N instance { get; private set; }
            public override uint Id { get; set; } = O1SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Omega-1 小队 队员";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Omega-1 小队 队员";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Omega 1";

            public string ShowingToPlayer => "Omega-1";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 60;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 队员</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunCrossvec)
            };
                //MenuInit();
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
                        foreach (var item in SCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.EnableEffect(EffectType.Fade, 255, 0f);
                        //Plugin.Register(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }
        private static void TrySpawnNu22(List<Player> candidates, bool forced = false, bool imm = false)
        {
            int chaos = 0, scp = 0;

            foreach (var h in ReferenceHub.AllHubs)
            {
                var r = h.roleManager.CurrentRole.RoleTypeId;
                if (!r.IsAlive()) continue;
                if (r.IsScp() || r.IsNtf()) scp++; else chaos++;
            }

            if (chaos - scp > config.HammerSpawnCount || forced)
            {
                Log.Info("Nu22 wave triggered");
                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                if (w != null)
                {
                    if (w.RespawnTokens > 0)
                        w.RespawnTokens--;

                    w.Timer.Reset();
                    if (!imm) Exiled.API.Features.Respawn.SummonNtfChopper();
                    Timing.RunCoroutine(Nu22SpawnCoroutine(w, candidates, imm));
                }
            }
        }
        private static void TrySpawnO1(List<Player> candidates, bool forced = false, bool imm = false)
        {
            int chaos = 0, scp = 0;

            foreach (var h in ReferenceHub.AllHubs)
            {
                var r = h.roleManager.CurrentRole.RoleTypeId;
                if (!r.IsAlive()) continue;
                if (r.IsScp() || r.IsNtf()) scp++; else chaos++;
            }

            if (chaos - scp > config.HammerSpawnCount || forced)
            {
                Log.Info("O1 wave triggered");
                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                if (w != null)
                {
                    if (w.RespawnTokens > 0)
                        w.RespawnTokens--;

                    w.Timer.Reset();
                    if (!imm) Exiled.API.Features.Respawn.SummonNtfChopper();
                    Timing.RunCoroutine(O1SpawnCoroutine(w, candidates, imm));
                }
            }
        }
        private static IEnumerator<float> O1SpawnCoroutine(NtfSpawnWave w, List<Player> spawntarget, bool imm = false)
        {
            if (!imm)
            {
                if (w != null)
                {
                    yield return Timing.WaitForSeconds(w.AnimationDuration);
                }
            }
            var HammerWave = new List<Player>(spawntarget.Take(Math.Min(config.HammerMaxCount, spawntarget.Count - 1)));
            if (HammerWave.Count == 0)
            {
                yield break;
            }
            spawntarget.RemoveRange(0, Math.Min(config.HammerMaxCount, spawntarget.Count - 1));
            scp5k_Nu22_P.instance.AddRole(HammerWave[0]);
            spawntarget.RemoveRange(0, 1);
            foreach (var item in HammerWave)
            {
                if (UnityEngine.Random.Range(0, 100) < 40)
                {
                    scp5k_Omega1_S.instance.AddRole(item);
                }
                else
                {
                    scp5k_Omega1_N.instance.AddRole(item);
                }


            }
            if (true)
            {
                Cassie.MessageTranslated("Mobile Task Force Unit Omega 1 has entered the facility", "机动特遣队Omega-1小队已进入设施。");
                //HammerSpawnedBroadcast = true;
            }

            yield break;
        }
        public static bool Nu22Spawned = true;
        private static IEnumerator<float> Nu22SpawnCoroutine(NtfSpawnWave w, List<Player> spawntarget, bool imm = false)
        {
            if (!imm)
            {
                if (w != null)
                {
                    yield return Timing.WaitForSeconds(w.AnimationDuration);
                }
            }
            var HammerWave = new List<Player>(spawntarget.Take(Math.Min(config.HammerMaxCount, spawntarget.Count - 1)));
            if (HammerWave.Count == 0)
            {
                yield break;
            }
            spawntarget.RemoveRange(0, Math.Min(config.HammerMaxCount, spawntarget.Count - 1));
            scp5k_Nu22_P.instance.AddRole(HammerWave[0]);
            spawntarget.RemoveRange(0, 1);
            foreach (var item in HammerWave)
            {
                if (UnityEngine.Random.Range(0, 100) < 40)
                {
                    scp5k_Nu22_S.instance.AddRole(item);
                }
                else
                {
                    scp5k_Nu22_S.instance.AddRole(item);
                }


            }

            var n = Npc.Spawn("SCP-1440", RoleTypeId.Tutorial, new Vector3(0, 300, 0));
            Timing.CallDelayed(0.4f, () =>
            {
                n.Position = HammerWave[0].Position;

                scp5k_Scp1440.instance.AddRole(n);
                //new Scp1440DummyAi(n);
            });
            Nu22Spawned = true;
            if (true)
            {
                Cassie.MessageTranslated("Mobile Task Force Unit Nu 2 2 with SCP 1 4 4 0 has entered the facility . Please wait the facilitys destruction .", "机动特遣队Nu-22小队已携带SCP1440进入设施。请等待设施毁灭");
                //HammerSpawnedBroadcast = true;
            }

            yield break;
        }
        public static uint SpeedBuildItemID = 5096;
        [CustomItem(ItemType.GrenadeFlash)]
        public class SpeedBuildItem : CustomItem
        {

            public static CustomItem instance { get; private set; }
            public override uint Id { get; set; } = SpeedBuildItemID;
            public override string Name { get; set; } = "速凝掩体";
            public override string Description { get; set; }
            public override Vector3 Scale { get => new Vector3(0.5f, 0.5f, 0.5f); set => base.Scale = value; }
            public override float Weight { get; set; } = 1f;
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
            {
            };
            public override void Destroy()
            {
                IUnsubscribeEvents();
                base.Destroy();
            }
            public override void Init()
            {
                ISubscribeEvents();
                instance = this;
                base.Init();
            }
            protected void ISubscribeEvents()
            {
                Exiled.Events.Handlers.Player.ThrownProjectile += OnDroppedItem;
            }
            protected void IUnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.ThrownProjectile -= OnDroppedItem;
            }
            public void OnDroppedItem(ThrownProjectileEventArgs ev)
            {
                if (this.Check(ev.Pickup))
                {
                    ev.Pickup.Base.gameObject.AddComponent<Builder>().init(ev.Pickup, ev.Player.Rotation, ev.Player);
                }
            }
        }

        class Builder : MonoBehaviour
        {
            public Pickup pickup = null;
            public Quaternion playerRotation;
            public Player Owner = null;
            void OnCollisionEnter(Collision collision)
            {
                if (Owner == null)
                {
                    Log.Error("Owner is null!");
                }

                if (pickup == null)
                {
                    Log.Error("pickup is null!");
                }

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

                if (!(collision.collider.gameObject == Owner.GameObject) && (Player.Get(collision.gameObject) != Owner) && !Spawned)
                {
                    Spawned = true;
                    Vector3 wallNormal = collision.contacts[0].normal;
                    Quaternion bunkerRotation = CalculateBunkerRotation(wallNormal, playerRotation);

                    CreateBunker(collision.contacts[0].point, bunkerRotation);
                    pickup.Destroy();
                    Destroy(this);
                }
            }
            public bool Spawned = false;
            public void init(Pickup Pickup, Quaternion rotation, Player player)
            {
                playerRotation = rotation;
                pickup = Pickup;
                Owner = player;
            }

            private Quaternion CalculateBunkerRotation(Vector3 wallNormal, Quaternion playerRot)
            {
                Vector3 playerForward = playerRot * Vector3.forward;
                Vector3 playerRight = playerRot * Vector3.right;
                Vector3 projectedForward = Vector3.ProjectOnPlane(playerForward, wallNormal).normalized;
                Vector3 projectedRight = Vector3.ProjectOnPlane(playerRight, wallNormal).normalized;
                if (projectedForward.magnitude < 0.1f)
                {
                    projectedForward = Vector3.ProjectOnPlane(Vector3.forward, wallNormal).normalized;
                    projectedRight = Vector3.ProjectOnPlane(Vector3.right, wallNormal).normalized;
                }

                // 创建旋转：向前方向是投影后的玩家方向，向上方向是墙面法线
                return Quaternion.LookRotation(projectedForward, wallNormal);
            }
        }



        public static void CreateBunker(Vector3 pos, Quaternion rot)
        {
            Primitive p = Primitive.Get(Object.Instantiate(Primitive.Prefab));
            p.Position = pos;
            p.Base.NetworkPrimitiveType = PrimitiveType.Cube;
            p.Rotation = rot;
            p.Scale = new Vector3(2.3f, 2.3f, 0.3f);
            p.Color = Color.gray;
            p.Collidable = true;
            p.Visible = true;
            foreach (var item in p.GameObject.GetComponentsInChildren<Transform>())
            {
                item.gameObject.layer = LayerMask.GetMask("Glass");
            }
            p.GameObject.layer = LayerMask.GetMask("Glass");
            p.Spawn();

            var BW = p.GameObject.AddComponent<Scp5k_Control.bunker>();
            BW.Health = 200;
        }
        public static RaycastHit CreateRaycastHit(Vector3 from, Vector3 to)
        {
            RaycastHit hit;
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);

            if (Physics.Raycast(from, direction, out hit, distance))
            {
                return hit;
            }

            // 如果没有命中，可以手动设置一些值
            hit.point = to;
            hit.normal = Vector3.up;
            hit.distance = distance;
            // 其他字段需要根据实际情况设置

            return hit;
        }
        public class bunker : NetworkBehaviour, IDestructible, IBlockStaticBatching
        {
            public uint NetworkId
            {
                get
                {
                    return base.netId;
                }
            }

            // Token: 0x17000016 RID: 22
            // (get) Token: 0x06000043 RID: 67 RVA: 0x00002B88 File Offset: 0x00000D88
            public Vector3 CenterOfMass
            {
                get
                {
                    return base.transform.position;
                }
            }
            private void ServerSendImpactDecal(RaycastHit hit, Vector3 origin, DecalPoolType decalType, ImpactEffectsModule impactEffectsModule)
            {
                typeof(ImpactEffectsModule).GetMethod("ServerSendImpactDecal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(impactEffectsModule, new object[] { hit, origin, decalType });
            }
            public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
            {
                AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
                if (attackerDamageHandler == null)
                {
                    this.ServerDamageWindow(damage);
                    return true;
                }
                if (!this.CheckDamagePerms(attackerDamageHandler.Attacker.Role))
                {
                    return false;
                }
                this.LastAttacker = attackerDamageHandler.Attacker;
                Player attacker = Player.Get(attackerDamageHandler.Attacker);
                this.ServerDamageWindow(damage);
                if ((handler is MicroHidDamageHandler)) return true;
                if (attackerDamageHandler.Attacker.Hub.inventory.CurItem != null)
                {
                    if (attacker.CurrentItem is Exiled.API.Features.Items.Firearm firearm)
                    {
                        {
                            if (firearm.HitscanHitregModule is HitscanHitregModuleBase hitscan)
                            {
                                var mod = (ImpactEffectsModule)typeof(HitscanHitregModuleBase).GetField("_impactEffectsModule", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(hitscan);
                                if (mod != null)
                                {
                                    var r = CreateRaycastHit(attackerDamageHandler.Attacker.Hub.GetPosition(), pos);
                                    ServerSendImpactDecal(r, attacker.Position, DecalPoolType.Bullet, mod);
                                }
                            }
                        }
                    }
                }
                return true;
            }
            private void Update()
            {
                if (!this.IsBroken || this._prevStatus)
                {
                    return;
                }
                base.StartCoroutine(this.BreakWindow());
                this._prevStatus = true;
            }

            // Token: 0x0600004B RID: 75 RVA: 0x00002D1B File Offset: 0x00000F1B
            private IEnumerator BreakWindow()
            {
                GameObject.Destroy(base.gameObject);
                yield break;
            }

            // Token: 0x0600004C RID: 76 RVA: 0x00002D2C File Offset: 0x00000F2C
            private bool CheckDamagePerms(RoleTypeId roleType)
            {
                PlayerRoleBase playerRoleBase;
                return !this._preventScpDamage || (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(roleType, out playerRoleBase) && playerRoleBase.Team > Team.SCPs);
            }

            // Token: 0x0600004D RID: 77 RVA: 0x00002D58 File Offset: 0x00000F58
            [ServerCallback]
            private void ServerDamageWindow(float damage)
            {
                if (!NetworkServer.active)
                {
                    return;
                }
                this.Health -= damage;
                if (this.Health <= 0f)
                {
                    this.NetworkIsBroken = true;
                }
            }
            public bool NetworkIsBroken
            {
                get
                {
                    return this.IsBroken;
                }
                set
                {
                    this.IsBroken = value;
                }
            }
            public Footprint LastAttacker;
            public float Health = 30f;
            public bool IsBroken;
            private bool _preventScpDamage = false;
            private bool _prevStatus;
        }
        public static bool HammerSpawnedBroadcast = false;
        public static bool IsO5NukeEnd = true;
        public static bool IsForce5kRound = false;
        public static int startedScpCount = 0;
        static CoroutineHandle refresher;
        public static void RoundStarted()
        {
            //if(FFManager.isInitialized == false)
            FFManager.InitializeFastLookup();
            Scp5k_Control.Is5kRound = UnityEngine.Random.Range(1, 100) <= config.scp5kPercent;
            Is5kRound = Is5kRound | IsForce5kRound;
            DeadmanSwitchInitiated = false;
            GocKilledScp = 0;
            IsMotherDied = false;
            GocNuke = false;
            IsForce5kRound = false;
            UiuEscaped = false;
            LastChangedWarheadIsGoc = false;
            Scp055Escaped = false;
            HammerSpawned = false;
            IsO5NukeEnd = true;
            _Decont_NextIsOepn = true;
            Nu22Spawned = false;
            UiuDownloadBroadcasted = false; Nuke_GOC_Spawned = false;
            HammerSpawnedBroadcast = false;
            Spawn_Nuke_GOC = UnityEngine.Random.Range(0, 100) > 50;
            IsO5NukeEnd = UnityEngine.Random.Range(0, 100) < 50;
            UiUSpawnTime = config.UiUSpawnTime - config.UiUSpawnFloatTime + UnityEngine.Random.Range(0, config.UiUSpawnFloatTime * 2);
            AndSpawnTime = config.AndSpawnTime - config.AndSpawnFloatTime + UnityEngine.Random.Range(0, config.AndSpawnFloatTime * 2);
            UiuSpawned = false;
            GocSpawned = false;
            RespawnedCount = 0;
            Nuke_GOC_WinCon = false;
            GocSpawnedOnce = false;
            GocSpawnable = true;
            AndTimer = new Stopwatch();
            GOCBomb.CountdownStarted = false;
            UiuDownloadTime = 0;
            AndRefreshCount = 0;
            GocTimer = new Stopwatch();
            Plugin.RunCoroutine(UiuBackendUpdate());
            if (refresher.IsRunning)
            {
                Timing.KillCoroutines(refresher);
            }
            if (Is5kRound)
            {
                Log.Info("refresher start");
                Plugin.RunCoroutine(DecontUpdate());

                refresher = Plugin.RunCoroutine(Refresher());

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
                            if (Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.Scientist).ToList().Count() > 0)
                            {
                                var Luck = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.Scientist).ToList().RandomItem();
                                if (CustomRole.TryGet(SciID, out var role))
                                {
                                    role.AddRole(Luck);
                                }
                            }
                            if (Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.ClassD).ToList().Count() > 3)
                            {
                                var Luck1 = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.ClassD).ToList().RandomItem();
                                if (CustomRole.TryGet(GocSpyID, out var role))
                                {
                                    role.AddRole(Luck1);
                                }
                                var Luck2 = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.ClassD && x != Luck1).ToList().RandomItem();
                                Scp5k.Scp5k_Control.ColorChangerRole.instance.AddRole(Luck2);

                            }
                            startedScpCount = Player.Enumerable.Where(x => x.Role.Team == Team.SCPs).ToList().Count();
                            if (startedScpCount > 5)
                            {
                                var Luck1 = Player.Enumerable.Where(x => x.Role.Team != Team.SCPs).ToList().RandomItem();
                                Luck1.RoleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart);
                                Luck1.Position = Room.Get(Exiled.API.Enums.RoomType.Hcz096).Position + new UnityEngine.Vector3(0, 0.5f, 0);


                            }
                            else if (Player.Enumerable.Count() >= 32)
                            {
                                var Luck1 = Player.Enumerable.Where(x => x.Role.Team != Team.SCPs).ToList().RandomItem();
                                Luck1.RoleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart);
                                Luck1.Position = Room.Get(Exiled.API.Enums.RoomType.Hcz096).Position + new UnityEngine.Vector3(0, 0.5f, 0);
                            }
                            foreach (var item in Player.Enumerable)
                            {

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
                                        item.Position = Room.Get(Exiled.API.Enums.RoomType.LczCafe).Position + new UnityEngine.Vector3(0, 3f, 0);

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
            [RoleTypeId.Scp0492] = 0,
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
            [RoleTypeId.FacilityGuard] = 0.5f,
            [RoleTypeId.Scientist] = 0.5f,
            [RoleTypeId.ClassD] = 0.5f,
            [RoleTypeId.Tutorial] = 0,
            [RoleTypeId.CustomRole] = 0,
            [RoleTypeId.Scp049] = 1,
            [RoleTypeId.Scp079] = 1,
            [RoleTypeId.Scp096] = 1,
            [RoleTypeId.Scp3114] = 1,
            [RoleTypeId.Scp0492] = 1,
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
            [RoleTypeId.Scp0492] = 1,
            [RoleTypeId.Scp079] = 1,
            [RoleTypeId.Scp096] = 1,
            [RoleTypeId.Scp3114] = 1,
            [RoleTypeId.Scp939] = 1,
            [RoleTypeId.Scp173] = 1,
            [RoleTypeId.Scp106] = 1,
        };
        public static Dictionary<RoleTypeId, float> EscaperFF = new Dictionary<RoleTypeId, float>()
        {
            [RoleTypeId.ChaosRifleman] = 0.25f,
            [RoleTypeId.ChaosRepressor] = 0.25f,
            [RoleTypeId.ChaosConscript] = 0.25f,
            [RoleTypeId.ChaosMarauder] = 0.25f,
            [RoleTypeId.NtfCaptain] = 1,
            [RoleTypeId.NtfPrivate] = 1,
            [RoleTypeId.NtfSergeant] = 1,
            [RoleTypeId.NtfSpecialist] = 1,
            [RoleTypeId.FacilityGuard] = 0,
            [RoleTypeId.Scientist] = 0,
            [RoleTypeId.ClassD] = 0,
            [RoleTypeId.Tutorial] = 0.25f,
            [RoleTypeId.CustomRole] = 0.25f,
            [RoleTypeId.Scp049] = 1,
            [RoleTypeId.Scp0492] = 1,
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
                        CustomRole.Get(Goc610CID).TrackedPlayers.Count +
                        CustomRole.Get(Goc610PID).TrackedPlayers.Count + Nuke_GOC_count;
            }
        }

        public static int AndRefreshCount { get; private set; } = 0;

        public static void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (Is5kRound)
            {

                var p = Player.Get(ev.Player.ReferenceHub);
                if (p.HasMessage("donationCount"))
                {
                    p.RemoveMessage("donationCount");
                }

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
                                        _minTierIndex.SetValue(a.DoorLockChanger, (int)1);
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
                Timing.CallDelayed(0.5f, () =>
                {
                    if (ev.Reason == SpawnReason.RoundStart)
                    {
                        if (ev.NewRole == RoleTypeId.FacilityGuard)
                        {
                            if (RoleSpawnpointManager.TryGetSpawnpointForRole(RoleTypeId.Scientist, out var spawnpoint) && spawnpoint != null)
                            {
                                if (spawnpoint.TryGetSpawnpoint(out var position, out _))
                                {
                                    ev.Player.Position = position + new UnityEngine.Vector3(0, 0.5f, 0);
                                }
                            }
                            else
                            {
                                ev.Player.Position = Room.Get(Exiled.API.Enums.RoomType.LczCafe).Position + new UnityEngine.Vector3(0, 3f, 0);

                                Log.Warn("未找到科学家出生点，无法设置 FacilityGuard 位置。");
                            }
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
                        case RoleTypeId.NtfPrivate:
                        case RoleTypeId.Scp049:
                        case RoleTypeId.Scp0492:
                        case RoleTypeId.NtfSpecialist:
                        case RoleTypeId.Scp3114:
                        case RoleTypeId.Scp173:
                            {
                                p.AddMessage("", "你是 基金会势力 消灭一切除基金会势力外的成员 \n <color=green>友好:SCP,九尾狐</color>\n<color=red>敌对:GOC,UIU,ClassD,科学家,保安,混沌,安德森机器人</color>", 5f, ScreenLocation.CenterTop);
                                ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(SCPFF);
                                foreach (var item in SCPFF)
                                {
                                    ev.Player.SetFriendlyFire(item);
                                }
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
                                    foreach (var item in EscaperFF)
                                    {
                                        ev.Player.SetFriendlyFire(item);
                                    }
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
                                if (isGocActing)
                                {
                                    p.AddMessage("", "你是 GOC势力 与反基金会势力合作 前往广播召唤GOC\n<color=yellow>中立:ClassD,科学家,保安,UIU,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                                    ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(GOCFF);
                                    ev.Player.SetCustomRoleFriendlyFire("Goc_Spy", RoleTypeId.Tutorial, 0);
                                    break;
                                }
                                p.AddMessage("", "你是 清收势力 与反基金会势力合作 尽可能的逃跑\n<color=yellow>中立:GOC,UIU,ClassD,科学家,保安,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);

                                ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(EscaperFF);
                                foreach (var item in EscaperFF)
                                {
                                    ev.Player.SetFriendlyFire(item);
                                }
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
                                if (!CustomRole.TryGet(Scp5k_Control.Goc610CID, out var customGocC))
                                {
                                    p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocC 请联系技术</size></color>", 3f);
                                    return;
                                }
                                if (!CustomRole.TryGet(Scp5k_Control.Goc610PID, out var customGocP))
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
                                    p.AddMessage("", "你是 反基金会势力 消灭一切基金会团队的成员\n <color=green>友好:GOC,UIU,混沌,安德森机器人</color>\n<color=yellow>中立:ClassD,科学家,保安</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                                    ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(AntiSCPFF);
                                    foreach (var item in AntiSCPFF)
                                    {
                                        ev.Player.SetFriendlyFire(item);
                                    }
                                    break;
                                }
                                else
                                {
                                    p.AddMessage("", "你是 GOC势力 消灭一切基金会团队的成员\n <color=green>友好:goc</color>\n<color=yellow>中立:ClassD,科学家,保安,UIU,混沌,安德森机器人</color>\n<color=red>敌对:SCP,九尾狐</color>", 5f, ScreenLocation.CenterTop);
                                    if (ev.Player != null)
                                    {
                                        //ev.Player.FriendlyFireMultiplier = new Dictionary<RoleTypeId, float>(GOCFF);
                                        foreach (var item in GOCFF)
                                        {
                                            ev.Player.SetFriendlyFire(item);
                                        }
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

                });

            }
        }

        public static void RoundEnding(EndingRoundEventArgs ev)
        {
            if (Is5kRound)
            {
                //Log.Info($"Ending Allow:{ev.IsAllowed}");
                //if (ev.IsAllowed)
                {
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    GOCAnim.donating = false;
                    Goc_Spy_broadcasted = false;
                    GOCBomb.Inited = false;
                    GOCBomb.Played = false;
                    GOCBomb.installCount = 4;
                    GOCBomb.GOCBombList = new List<GOCBomb>();
                    GOCBomb.installAt = new List<Room>();
                    GOCBomb.installedRoom = new Dictionary<GOCBomb, Room>();
                    GOCBomb.P2B = new Dictionary<Exiled.API.Features.Player, GOCBomb>();
                    GOCBomb.Questions = new List<(string q, string a)>();
                    GOCBomb.QuestionCount = 1;
                    uiu_broadcasted = false;
                    GOCBomb.countDown = GOCBomb.countDownStart;
                    GOCBomb.QuestionPoint = -1;

                    if (Scp055Escaped)
                    {
                        foreach (var s in Player.Enumerable)
                        {
                            s.AddMessage("", "Normal Ending:055进入了579 宇宙重启");
                        }
                        ev.IsAllowed = true;
                        ev.LeadingTeam = (Exiled.API.Enums.LeadingTeam)4;
                    }
                    else if ((Warhead.IsDetonated && Nuke_GOC_Spawned) || Nuke_GOC_WinCon)
                    {
                        ev.IsAllowed = true;
                        ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.ChaosInsurgency;
                        foreach (var s in Player.Enumerable)
                        {
                            s.AddMessage("", "GOC Ending: GOC成功引爆核弹");
                        }
                    }
                    else if ((GocSpawned) || GocNuke)
                    {
                        ev.IsAllowed = true;
                        ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.ChaosInsurgency;
                        foreach (var s in Player.Enumerable)
                        {
                            s.AddMessage("", "GOC Ending: GOC成功引爆奇术核弹");
                        }
                    }
                    else if (IsMotherDied && Scp610Ending)
                    {
                        ev.IsAllowed = true;
                        ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.ChaosInsurgency;
                        foreach (var s in Player.Enumerable)
                        {
                            s.AddMessage("", "GOC Ending: GOC成功消灭scp610");
                        }
                    }
                    else
                    {
                        RoundSummary.SumInfo_ClassList newList = default(RoundSummary.SumInfo_ClassList);
                        int ntfScp = 0;
                        //int lightRunner = 0;
                        int chaos = 0;
                        int Scp610C = 0;

                        foreach (var hub in Player.Enumerable)
                        {
                            var role = hub.Role.Type;

                            if (!role.IsAlive()) continue;
                            if (hub.UniqueRole == scp5k_Scp610_mother.instance.Name || hub.UniqueRole == scp5k_Scp610.instance.Name)
                            {
                                Scp610C++;
                            }
                            switch (role)
                            {
                                case RoleTypeId.NtfCaptain:
                                case RoleTypeId.NtfPrivate:
                                case RoleTypeId.NtfSergeant:
                                case RoleTypeId.NtfSpecialist:
                                case RoleTypeId.Scp049:
                                case RoleTypeId.Scp079:
                                case RoleTypeId.Scp096:
                                case RoleTypeId.Scp106:
                                case RoleTypeId.Scp173:
                                case RoleTypeId.Scp3114:
                                case RoleTypeId.Scp939:
                                    ntfScp++;
                                    break;

                                case RoleTypeId.FacilityGuard:
                                case RoleTypeId.ClassD:
                                case RoleTypeId.Scientist:
                                case RoleTypeId.ChaosConscript:
                                case RoleTypeId.ChaosMarauder:
                                case RoleTypeId.ChaosRepressor:
                                case RoleTypeId.ChaosRifleman:
                                    chaos++;
                                    break;
                            }
                        }
                        if (Scp610C >= Player.Enumerable.Where(x => x.IsAlive).Count() - 2 && Scp610Ending)
                        {
                            foreach (var s in Player.Enumerable)
                            {
                                s.AddMessage("", "SCP610 Ending: scp610占领了这里");
                            }
                            ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.Anomalies;
                            ev.IsAllowed = true;
                            return;

                        }
                        chaos += SpecRolesCount;
                        if (chaos != 0 && ntfScp == 0)
                        {
                            ev.IsAllowed = true;
                            ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.ChaosInsurgency;
                        }
                        else if (chaos == 0 && ntfScp != 0)
                        {
                            ev.IsAllowed = true;
                            ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.FacilityForces;
                        }
                        else
                        {
                            ev.IsAllowed = false;
                        }

                    }
                    //Scp5k_Control.Is5kRound = UnityEngine.Random.Range(1, 100) <= config.scp5kPercent;

                }

                if (refresher.IsRunning && ev.IsAllowed)
                {
                    Timing.KillCoroutines(refresher);
                }
            }

        }
        public static uint MagicGun1_JS_L1_ID = 5807;
        [CustomItem(ItemType.GunA7)]
        public class MagicGun1_JS_L1 : CustomWeapon
        {
            public static MagicGun1_JS_L1 ins;
            public override uint Id { get; set; } = MagicGun1_JS_L1_ID;
            public override string Name { get; set; } = "JS-L1";
            public override string Description { get; set; } = "对沿途目标照成流血+减速伤害";
            public override float Weight { get; set; } = 10;
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();
            public override Vector3 Scale { get => base.Scale; set => base.Scale = value; }
            public override AttachmentName[] Attachments { get => base.Attachments; set => base.Attachments = value; }
            public override ItemType Type { get => base.Type; set => base.Type = value; }
            public override float Damage { get => base.Damage; set => base.Damage = value; }
            public override byte ClipSize { get => base.ClipSize; set => base.ClipSize = value; }

            public override void Init()
            {
                ins = this;
                Type = ItemType.GunA7;
                Damage = 1;
                ClipSize = 50;
                base.Init();
                //DrawableLine.DrawableLines.IsDebugModeEnabled = true;
            }

            protected override void OnHurting(HurtingEventArgs ev)
            {
                base.OnHurting(ev);
            }

            protected override void OnOwnerEscaping(OwnerEscapingEventArgs ev)
            {
                base.OnOwnerEscaping(ev);
            }
            public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[]
{
            "Default",
            "Hitbox",
            "Glass",
            "CCTV",
            "Door"
});
            protected override void OnShooting(ShootingEventArgs ev)
            {
                var r = new Ray(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward);
                var raycasts = Physics.SphereCastAll(r, 0.2f, 120, HitregMask.Mask);
                var l = raycasts.ToList();
                l.Sort((RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
                IDestructible destructible;
                foreach (var raycast in l)
                {
                    if (raycast.collider.TryGetComponent<IDestructible>(out destructible))
                    {
                        //destructibles.Add(destructible);
                        if (destructible is HitboxIdentity HI)
                        {
                            var p = Player.Get(HI.TargetHub);
                            if (p == ev.Player)
                            {
                                continue;
                            }
                            if (HitboxIdentity.IsEnemy(HI.TargetHub, ev.Player.ReferenceHub))
                            {
                                p.EnableEffect(EffectType.Blurred, 10f);
                                if (p.IsScp)
                                {
                                    p.EnableEffect(EffectType.Slowness, 15, 10);
                                    p.EnableEffect(EffectType.Bleeding, 15, 20);
                                    p.Hurt(new FirearmDamageHandler(ev.Firearm.Base, 5, 1));
                                }
                                else
                                {
                                    p.Hurt(new FirearmDamageHandler(ev.Firearm.Base, 0.07f, 1));

                                }
                                new DrawableLineMessage(0.6f, Color.red, new Vector3[2] { ev.Player.CameraTransform.position + 0.2f * Vector3.down, raycast.point }).SendToAuthenticated();
                            }
                            else
                            {
                                p.AddRegeneration(0.1f, 10);
                                //p.Hurt(new FirearmDamageHandler(ev.Firearm.Base, 5, 1));
                                new DrawableLineMessage(0.6f, Color.green, new Vector3[2] { ev.Player.CameraTransform.position + 0.2f * Vector3.down, raycast.point }).SendToAuthenticated();
                            }

                        }
                        else
                        {
                            destructible.Damage(10, new FirearmDamageHandler(ev.Firearm.Base, 8, 1), raycast.point);
                            new DrawableLineMessage(0.6f, Color.yellow, new Vector3[2] { ev.Player.CameraTransform.position + 0.2f * Vector3.down, raycast.point }).SendToAuthenticated();

                        }
                    }
                }
                int num = UnityEngine.Random.Range(5, 7);
                Vector3[] array = new Vector3[num];
                Vector3 vector = ev.Player.CameraTransform.position + 0.2f * Vector3.down;
                for (int i = 0; i < num; i++)
                {
                    if (i != 0)
                    {
                        if (i % 5 == 0)
                        {
                            vector += ev.Player.CameraTransform.forward * GetRandomDistance(false);
                        }
                        else
                        {
                            Vector3 a = ev.Player.CameraTransform.forward * GetRandomDistance(false);
                            Vector3 b = a;
                            if (i % 5 == 4)
                            {
                                b = ev.Player.CameraTransform.up * -0.5f * GetRandomDistance(true);
                            }
                            else if (i % 5 == 3)
                            {
                                b = ev.Player.CameraTransform.up * 0.5f * GetRandomDistance(true);
                            }
                            else if (i % 5 == 2)
                            {
                                //b = ev.Player.CameraTransform.up * -1 * GetRandomDistance(true);
                                b = ev.Player.CameraTransform.right * 0.5f * GetRandomDistance(true);
                            }
                            else if (i % 5 == 1)
                            {

                                b = ev.Player.CameraTransform.right * -0.5f * GetRandomDistance(true);
                            }
                            vector += a + b;
                        }
                    }
                    array[i] = vector;
                }
                new DrawableLineMessage(0.14f, Color.blue, array).SendToAuthenticated();

                base.OnShooting(ev);
            }

            private static float GetRandomDistance(bool allowNegativeValues = true)
            {
                bool flag = allowNegativeValues && UnityEngine.Random.Range(0, 4) % 2 == 0;
                float num = UnityEngine.Random.Range(0.4f, 0.2f);
                if (!flag)
                {
                    return num;
                }
                return -num;
            }
            protected override void OnShot(ShotEventArgs ev)
            {

                base.OnShot(ev);
            }

            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();
            }

            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
            }
        }

        public static int UiuInServerRoom = 0;
        public static IEnumerator<float> UiuBackendUpdate()
        {
            bool Downloaded = false;
            while (true)
            {
                try
                {
                    if (UiuDownloadTime >= 100f)
                    {
                        Downloaded = true;
                        if (!uiu_broadcasted)
                        {
                            Cassie.MessageTranslated("Security alert . U I U down load d . Security personnel , proceed with standard protocols",
                                                     "安保警戒，侦测到机房资料下载完成。安保人员请继续执行标准协议。阻止uiu撤离");
                            uiu_broadcasted = true;
                        }
                        UiuDownloadTime = 100f;
                    }
                    UiuInServerRoom = 0;
                    foreach (var item in scp5k_Uiu_C.ins.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                UiuInServerRoom += 1;
                            }
                        }
                    }
                    foreach (var item in scp5k_Uiu_P.ins.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                UiuInServerRoom += 1;
                            }
                        }
                    }
                    if (!Downloaded)
                    {
                        if (UiuInServerRoom > 0)
                        {
                            UiuDownloadTime += UiuDownloadTick * 0.2f * UiuInServerRoom;
                            if (UiuDownloadTime >= 30f && !UiuDownloadBroadcasted)
                            {
                                Cassie.MessageTranslated("Security alert . U I U down load activity detected . Security personnel , proceed with standard protocols",
                                                         "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。前往机房");
                                UiuDownloadBroadcasted = true;
                            }
                            else if (UiuDownloadTime <= 20f)
                            {
                                UiuDownloadBroadcasted = false;
                            }
                        }
                        else
                        {
                            if (UiuDownloadTime > 0f)
                                UiuDownloadTime -= UiuDownloadTick * 0.2f;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.2f);
            }
        }
        public static Dictionary<string, CoroutineHandle> CH = new Dictionary<string, CoroutineHandle>();
        public static IEnumerator<float> UiuPlayerUpdate(Player player)
        {
            bool Downloaded = false;
            var p = player;
            while (true)
            {
                try
                {
                    if (player == null)
                    {
                        yield break;
                    }
                    var hud = HSM_hintServ.GetPlayerHUD(player);
                    if (hud == null)
                    {
                        yield break;
                    }

                    if (!scp5k_Uiu_C.ins.Check(player) && !scp5k_Uiu_P.ins.Check(player))
                    {
                        break;
                    }
                    if (!Downloaded)
                    {
                        if (player.CurrentRoom != null && player.CurrentRoom.RoomName != RoomName.Unnamed)

                        {

                            if (player.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                if (UiuDownloadTime >= 100f)
                                {
                                    Downloaded = true;
                                    if (hud.HasMessage("UIUdownloading"))
                                        hud.RemoveMessage("UIUdownloading");
                                    hud.AddMessage("UIU_DOWNLOAD_DONE" + DateTime.Now.ToString(),
                                                   "<size=30><color=red>你已成功下载资料,请尽快撤离!</color></size>",
                                                   4f, ScreenLocation.CenterBottom);


                                }
                                else
                                {
                                    float remainTime = 100;

                                    if (!hud.HasMessage("UIUdownloading"))
                                    {
                                        hud.AddMessage(
                                            "UIUdownloading",
                                            (x) =>
                                            {
                                                if (UiuDownloadTime >= 100f)
                                                {
                                                    if (hud.HasMessage("UIUdownloading"))
                                                        hud.RemoveMessage("UIUdownloading");
                                                    return Array.Empty<string>();
                                                }
                                                remainTime = UiuDownloadTick > 0f ? (100f - UiuDownloadTime) / UiuDownloadTick * UiuInServerRoom : float.PositiveInfinity;

                                                return new string[]
                                                {
                        $"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已下载: {UiuDownloadTime:F1}% 预计下载结束: {remainTime:F1} 秒</color></size>"
                                                };
                                            },
                                            -1f,
                                            ScreenLocation.CenterBottom
                                        );
                                    }
                                }

                            }
                            else
                            {

                                if (hud.HasMessage("UIUdownloading"))
                                {
                                    hud.RemoveMessage("UIUdownloading");
                                }
                            }
                        }
                        else
                        {
                            if (hud.HasMessage("UIUdownloading"))
                            {
                                hud.RemoveMessage("UIUdownloading");
                            }
                        }
                    }
                    else
                    {
                        if (Escape.CanEscape(player.ReferenceHub, out var role, out var zone))
                        {
                            if (hud.HasMessage("UIUdownloading" + player.Nickname))
                            {
                                hud.RemoveMessage("UIUdownloading" + player.Nickname);
                            }
                            hud.AddMessage("UIU_ESCAPED" + DateTime.Now.ToString(), "<size=30><color=yellow>你作为uiu成功撤离</color></size>", 4f, ScreenLocation.Center);
                            Scp5k_Control.UiuEscaped = true;
                            player.Role.Set(RoleTypeId.Spectator, reason: SpawnReason.Respawn);
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
            if (p.HasMessage("UIUdownloading" + player.Nickname))
            {
                p.RemoveMessage("UIUdownloading" + player.Nickname);
            }
            Log.Debug("Out!");

        }
        public static bool uiu_broadcasted = false;
        public static uint UiuCID = 32;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_C : CustomRole, IDeathBroadcaster
        {
            public string CassieBroadcast => "U I U";

            public string ShowingToPlayer => "UIU";
            public static scp5k_Uiu_C ins;
            public override uint Id { get; set; } = UiuCID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "UIU 突袭小组 队员";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "UIU 突袭小组 队员";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }

            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "与安德森机器人合作 调查基金会为什么毁灭人类\n下载完资料后撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 130;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU 突袭小组 队员</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往机房下载资料 下载完资料后撤离</color></size>", 4);
                //p.AddMessage("messID", "你是 反基金会 团队 消灭一切基金会团队的成员", 2f, ScreenLocation.Center));
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.GunCrossvec),
                string.Format("{0}", ItemType.GrenadeHE)
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
                            var hud = HSM_hintServ.GetPlayerHUD(ev.Player);
                            Timing.KillCoroutines(CH1);
                            if (hud.HasMessage("UIUdownloading" + ev.Player.Nickname))
                            {
                                hud.RemoveMessage("UIUdownloading" + ev.Player.Nickname);
                            }
                        }
                    }
                }
            }

            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {

                        foreach (var item in AntiSCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        CH[player.UserId] = Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(0, 302, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });

            }
        }
        public static uint UiuPID = 28;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_P : CustomRole, IDeathBroadcaster
        {
            public string CassieBroadcast => "U I U";

            public string ShowingToPlayer => "UIU";
            public static scp5k_Uiu_P ins;

            public override uint Id { get; set; } = UiuPID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "UIU 突袭小组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "UIU 突袭小组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "与安德森机器人合作 调查基金会为什么毁灭人类\n下载完资料后撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 150;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU 突袭小组 队长</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往机房下载资料 下载完资料后撤离</color></size>", 4);


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
                string.Format("{0}", ItemType.GrenadeHE)

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
            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        foreach (var item in AntiSCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        CH[player.UserId] = Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(0, 302, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });
            }
        }
        public static uint botID = 29;

        [CustomRole(RoleTypeId.Tutorial)]

        public class scp5k_Bot : CustomRole, IDeathBroadcaster
        {
            public static scp5k_Bot ins;
            public string CassieBroadcast => "And Saw";

            public string ShowingToPlayer => "安德森机器人";
            public override uint Id { get; set; } = botID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "安德森机器人";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "安德森机器人";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "与反scp基金会势力合作";
                this.Role = RoleTypeId.Tutorial;
                this.Gravity = new UnityEngine.Vector3(0, -14f, 0);
                MaxHealth = 140;
                this.DisplayCustomItemMessages = true;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是安德森机器人</color></size>\n<size=30><color=yellow>与反scp基金会势力合作</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                //string.Format("{0}", ItemType.ArmorCombat),
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
                    var p = ev.Player;
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
                                scp5k_Scanner.ins.Give(p, false);
                                TryAddItem(ev.Player, itemName);
                            }
                            p.AddMessage("", $"<color=red><size=30>你还有 {totalLives} 次复活机会</size></color>", 1.5f, ScreenLocation.Center);
                        }
                        else
                        {
                            RemoveRole(ev.Player);

                        }
                    }


                }
            }
            protected override void RoleRemoved(Player player)
            {
                if (!(player.ReferenceHub.roleManager.CurrentRole is IFpcRole fpcRole))
                {
                    return;
                }
                fpcRole.FpcModule.Motor.GravityController.Gravity = FpcGravityController.DefaultGravity;
                base.RoleRemoved(player);
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
                                scp5k_Scanner.ins.Give(player, false);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                        foreach (var item in AntiSCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        //Plugin.RunCoroutine(UiuPlayerUpdate(player));
                    }
                });
                base.RoleAdded(player);
            }

            //public IEnumerator<float> UiuPlayerUpdate(Player player)
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
        public static uint Goc2CID = 47;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_2_C : CustomRole, IDeathBroadcaster
        {
            public static scp5k_Goc_2_C ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc2CID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术1组 特工";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术1组 特工";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "使用奇术核弹毁灭站点";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 120;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术1组 特工</color></size>\n<size=30><color=yellow>使用奇术核弹毁灭站点</color></size>", 4);
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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
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
                    SpeedBuildItem.instance.Give(player, false);
                });

                base.RoleAdded(player);
            }
        }
        public static uint Goc2PID = 48;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_2_P : CustomRole
        {
            public static scp5k_Goc_2_P ins;

            public override uint Id { get; set; } = Goc2PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术1组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术1组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启奇术核弹毁灭站点";

                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 150;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术1组 队长</color></size>\n<size=30><color=yellow>开启奇术核弹毁灭站点</color></size>", 4);
                ins = this;

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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
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
                    SpeedBuildItem.instance.Give(player, false);
                });
                base.RoleAdded(player);
            }
        }
        public static uint GocNukeCID = 42;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_nuke_C : CustomRole, IDeathBroadcaster
        {
            public static scp5k_Goc_nuke_C ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = GocNukeCID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 消灭1组 特工";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 消灭1组 特工";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "开启核弹并消灭所有SCP撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 190;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 消灭1组 特工</color></size>\n<size=30><color=yellow>开启核弹并消灭所有SCP撤离</color></size>", 4);
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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in AntiSCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    Nuke_GOC_Spawned = true;
                });

                base.RoleAdded(player);
            }
        }
        public static uint GocNukePID = 41;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_nuke_P : CustomRole, IDeathBroadcaster
        {
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public static scp5k_Goc_nuke_P ins;
            public override uint Id { get; set; } = GocNukePID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 消灭1组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 消灭1组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启核弹并消灭所有SCP撤离";
                ins = this;
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 230;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 消灭1组 队长</color></size>\n<size=30><color=yellow>开启核弹并消灭所有SCP撤离</color></size>", 4);

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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in AntiSCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                    }
                    GocSpawned = true;
                    Nuke_GOC_Spawned = true;
                });
                base.RoleAdded(player);
            }
        }
        public static uint GocNukeScanID = 57;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_nuke_scan : CustomRole, IDeathBroadcaster
        {
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public static scp5k_Goc_nuke_scan ins;
            public override uint Id { get; set; } = GocNukeScanID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 消灭1组 扫描专员";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 消灭1组 扫描专员";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "消灭所有SCP撤离";
                ins = this;
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 200;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 消灭1组 扫描专员</color></size>\n<size=30><color=yellow>消灭所有SCP撤离</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        //string.Format("{0}", ItemType.ArmorHeavy),
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardChaosInsurgency),
        string.Format("{0}", ItemType.SCP207),
        //string.Format("{0}", ItemType.GunFRMG0)
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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in AntiSCPFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                    }
                    MagicGun1_JS_L1.ins.Give(player, false);
                    scp5k_Scanner.ins.Give(player, false);
                    GocSpawned = true;
                    Nuke_GOC_Spawned = true;
                });
                base.RoleAdded(player);
            }
        }
        public static bool Goc_Spy_broadcasted = false;
        public static uint GocSpyID = 33;
        [CustomRole(RoleTypeId.ClassD)]
        public class scp5k_Goc_spy : CustomRole
        {
            public static scp5k_Goc_spy ins;
            public override uint Id { get; set; } = GocSpyID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 间谍";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
            {
                Limit = 1,
            };
            public override void Init()
            {
                ins = this;
                Description = "你是Goc间谍\n前往广播室呼叫阵营";

                this.Role = RoleTypeId.ClassD;
                MaxHealth = 100;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc间谍</color></size>\n<size=30><color=yellow>前往广播室呼叫阵营</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardGuard),
        string.Format("{0}", ItemType.Coin),
        string.Format("{0}", ItemType.GunCOM18)
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
                Exiled.Events.Handlers.Player.FlippingCoin += Flip;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void Flip(FlippingCoinEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if (diedPlayer.Count == 0)
                    {

                        ev.Player.AddMessage("spy_failed_no_body_died" + DateTime.Now.ToString(), "<size=30><color=green>失败 没有GOC在待命!</color></size>", 3f, ScreenLocation.Center);
                        return;
                    }
                    if (ev.Player.CurrentRoom != null && ev.Player.CurrentRoom.RoomName == RoomName.EzIntercom && !Goc_Spy_broadcasted && GocSpawnable)
                    {
                        Log.Info("goc");
                        var tempdiedPlayer = diedPlayer;
                        tempdiedPlayer.ShuffleList();
                        var GocWave = new List<Player>(Math.Min(config.GocMaxCount, tempdiedPlayer.Count - 1));
                        GocWave.AddRange(tempdiedPlayer.Take(Math.Min(config.GocMaxCount, tempdiedPlayer.Count - 1)));
                        tempdiedPlayer.RemoveRange(0, Math.Min(config.GocMaxCount, tempdiedPlayer.Count - 1));
                        if (Spawn_Nuke_GOC)
                        {
                            if (CustomRole.TryGet(GocNukeCID, out var role) && GocWave.Count > 0)
                            {
                                if (CustomRole.TryGet(GocNukePID, out var Prole))
                                {
                                    Prole.AddRole(GocWave[0]);
                                }
                                tempdiedPlayer.RemoveRange(0, 1);
                                foreach (var item in GocWave)
                                {
                                    role.AddRole(item);
                                }
                            }
                        }
                        else
                        {
                            if (CustomRole.TryGet(Goc610CID, out var role) && GocWave.Count > 0)
                            {
                                if (CustomRole.TryGet(Goc610PID, out var Prole))
                                {
                                    Prole.AddRole(GocWave[0]);
                                }
                                tempdiedPlayer.RemoveRange(0, 1);
                                foreach (var item in GocWave)
                                {
                                    role.AddRole(item);
                                }
                            }
                            GocSpawned = true;
                        }
                        if (Goc_Spy_broadcasted)
                        {
                            Cassie.MessageTranslated("Security alert . Substantial G o c activity detected . Security personnel ,  proceed with standard protocols , Protect the warhead ", "安保警戒，侦测到大量GOC的活动。安保人员请继续执行标准协议，保护核弹。");
                            ev.Player.AddMessage("Spy_goc_spawned" + DateTime.Now.ToString(), "<size=30><color=green>你成功呼叫支援!</color></size>", 3f, ScreenLocation.Center);
                        }
                        else
                        {
                            ev.Player.AddMessage("spy_failed_no_body" + DateTime.Now.ToString(), "<size=30><color=green>失败!</color></size>", 3f, ScreenLocation.Center);

                        }
                    }
                    else
                    {
                        var p = ev.Player;
                        p.AddMessage("Spy_Failed_not_in_intercom" + DateTime.Now.ToString(), "<size=30><color=red>你必须在广播室使用硬币呼叫阵营!</color></size>", 3f, ScreenLocation.Center);
                    }
                }
            }
            protected override void UnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.FlippingCoin -= Flip;
                base.UnsubscribeEvents();
            }
            protected override void RoleAdded(Player player)

            {
                player.InfoArea = PlayerInfoArea.Nickname | PlayerInfoArea.Badge | PlayerInfoArea.Role | PlayerInfoArea.UnitName;
                base.RoleAdded(player);
            }
        }
        public static uint Goc610CID = 30;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_610_C : CustomRole, IDeathBroadcaster
        {
            public static scp5k_Goc_610_C ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc610CID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术2组 特工";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术2组 特工";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "使用奇术核弹毁灭站点";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 250;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术2组 特工</color></size>\n<size=30><color=yellow>使用奇术核弹毁灭站点</color></size>", 4);
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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
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
                    SpeedBuildItem.instance.Give(player, false);
                });

                base.RoleAdded(player);
            }
        }
        public static uint Goc610PID = 31;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_610_P : CustomRole, IDeathBroadcaster
        {
            public static scp5k_Goc_610_P ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc610PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术2组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术2组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启奇术核弹毁灭站点";

                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 350;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术2组 队长</color></size>\n<size=30><color=yellow>开启奇术核弹毁灭站点</color></size>", 4);
                ins = this;

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
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
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
                    SpeedBuildItem.instance.Give(player, false);
                });
                base.RoleAdded(player);
            }
        }

        public static uint ColorChanger = 35;
        [CustomRole(RoleTypeId.ClassD)]
        public class ColorChangerRole : CustomRole
        {

            public static ColorChangerRole instance { get; private set; }
            public override uint Id { get; set; } = ColorChanger;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "变色龙";
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
                Description = "你是变色龙 想方法活下去";

                this.Role = RoleTypeId.ClassD;
                MaxHealth = 100;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是变色龙</color></size>\n<size=30><color=yellow>想方法活下去\n使用Server-specific变身</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.KeycardScientist),
            };
                instance = this;
                Plugin.MenuCache.AddRange(MenuInit());
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public static Dictionary<RoleTypeId, string> RoleTrans = new Dictionary<RoleTypeId, string>() {
                {RoleTypeId.Scp049, "Scp049" },
                {RoleTypeId.Scp096, "Scp096" },
                {RoleTypeId.Scp3114, "Scp3114" },
                {RoleTypeId.Scp173, "Scp173" },
                {RoleTypeId.Scp939, "Scp939" },
                {RoleTypeId.Scp0492, "小僵尸" },
                {RoleTypeId.Scp079, "Scp079" },
                {RoleTypeId.Scp106, "Scp106" },

                {RoleTypeId.NtfCaptain, "狗官" },
                {RoleTypeId.NtfPrivate, "列兵" },
                {RoleTypeId.NtfSergeant, "中士" },
                {RoleTypeId.NtfSpecialist, "收容专家" },
                {RoleTypeId.FacilityGuard, "保安" },
                {RoleTypeId.Scientist, "科学家" },

                {RoleTypeId.ChaosRifleman, "混沌步兵" },
                {RoleTypeId.ChaosMarauder, "混沌掠夺" },
                {RoleTypeId.ChaosRepressor, "混沌机枪" },
                {RoleTypeId.ChaosConscript, "混沌招募" },
                {RoleTypeId.ClassD, "ClassD " },

                {RoleTypeId.None, "不变" },
                {RoleTypeId.Overwatch, "Overwatch" },
                {RoleTypeId.Spectator, "观察者" },
                {RoleTypeId.Tutorial, "教程角色" },
            };
            public static Dictionary<RoleTypeId, string> RoleChangaeableTrans = new Dictionary<RoleTypeId, string>() {

                {RoleTypeId.None, "不变" },
                {RoleTypeId.ClassD, "ClassD " },
                {RoleTypeId.ChaosRifleman, "混沌步兵" },
                {RoleTypeId.ChaosMarauder, "混沌掠夺" },
                {RoleTypeId.ChaosRepressor, "混沌机枪" },
                {RoleTypeId.ChaosConscript, "混沌招募" },

                {RoleTypeId.NtfCaptain, "狗官" },
                {RoleTypeId.NtfPrivate, "列兵" },
                {RoleTypeId.NtfSergeant, "中士" },
                {RoleTypeId.NtfSpecialist, "收容专家" },
                {RoleTypeId.FacilityGuard, "保安" },
                {RoleTypeId.Scientist, "科学家" },



                {RoleTypeId.Tutorial, "教程角色" },
            };


            public static Dictionary<Player, RoleTypeId> PlayerToRole = new Dictionary<Player, RoleTypeId>();
            public static Dictionary<Player, ChangeEffect> PlayerToChangeEffect = new Dictionary<Player, ChangeEffect>();
            public static List<SettingBase> MenuInit()
            {
                var settings = new List<SettingBase>();
                var s = new Exiled.API.Features.Core.UserSettings.DropdownSetting(
                    Plugin.Instance.Config.SettingIds[Features.ColorChangerRole], $"变身目标", RoleChangaeableTrans.Values, isServerOnly: true, dropdownEntryType: UserSettings.ServerSpecific.SSDropdownSetting.DropdownEntryType.Hybrid,
                    onChanged: (player, SB) =>
                    {
                        try
                        {

                            if (instance == null)
                            {
                                return;
                            }
                            var lp = player;
                            if (SB is DropdownSetting UTI)
                            {
                                if (!PlayerToRole.TryGetValue(player, out var OldRole))
                                {
                                    OldRole = RoleTypeId.None;
                                }
                                var targetRole = RoleChangaeableTrans.ElementAt(UTI.SelectedIndex).Key;
                                PlayerToRole[player] = targetRole;
                                Log.Info($"Changing Appearance for:{player} to {targetRole}");
                                if (!PlayerToChangeEffect.TryGetValue(player, out var a))
                                {
                                    a = ChangeEffect.Enabled(player.ReferenceHub, 40);

                                }
                                a.ChangeTarget(targetRole);
                                PlayerToChangeEffect[lp] = a;
                                if (targetRole != RoleTypeId.None)
                                {
                                    if (targetRole == OldRole)
                                    {
                                        lp.AddMessage("Changer_Failed_already_is" + DateTime.Now.ToString(), "<size=30><color=red>你已经是这个外表了!</color></size>", 3f, ScreenLocation.Center);
                                        return;
                                    }

                                }
                                else
                                {
                                    a.DisableEffect();
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());

                        }
                    });
                //s.Base.
                settings.Add(s);
                return settings;
            }

            protected override void RoleAdded(Player player)

            {
                Plugin.Register(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ColorChangerRole]));
                player.InfoArea = PlayerInfoArea.Nickname | PlayerInfoArea.Badge | PlayerInfoArea.Role | PlayerInfoArea.UnitName;

                base.RoleAdded(player);
            }
            protected override void RoleRemoved(Player player)
            {
                Plugin.Unregister(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ColorChangerRole]));
                base.RoleRemoved(player);
            }
        }
        public static uint SciID = 34;
        [CustomRole(RoleTypeId.Scientist)]
        public class scp5k_Sci : CustomRole
        {
            public static scp5k_Sci ins;
            public override uint Id { get; set; } = SciID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Pietro Wilson";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Pietro Wilson";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "逃出site27,前往site62c \n带着055撤离";
                this.Role = RoleTypeId.Scientist;
                MaxHealth = 100;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Pietro Wilson</color></size>\n<size=30><color=yellow>携带SCP055逃出site27,前往site62c\n(带着055撤离)</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                ins = this;
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
                    var p = ev.Player;

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
                        p.AddMessage("Doc_escape_failed_no055" + DateTime.Now.ToString(), "<size=40><color=red>你必须携带055逃离!</color></size>", 2f, ScreenLocation.Center);

                    }
                    else
                    {
                        Timing.CallDelayed(30f, () =>
                        {
                            Scp055Escaped = true;
                            if (Round.IsStarted)
                                Round.EndRound(true);
                        });
                        p.AddMessage("Doc_escape_successful" + DateTime.Now.ToString(), "<size=40><color=red>你携带055成功逃离! 30秒后回合结束!</color></size>", 3f, ScreenLocation.Center);
                        ev.NewRole = RoleTypeId.Spectator;
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
                    if (scp5k_Sci.ins.Check(ev.Player))
                    {
                        ev.IsAllowed = true;
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
                    if (scp5k_Sci.TryGet(Goc610PID, out var item))
                    {
                        if (item.Check(ev.Player))
                        {
                            ev.IsAllowed = true;
                            return;

                        }
                    }
                    if (scp5k_Sci.TryGet(Goc610CID, out var item2))
                    {
                        if (item2.Check(ev.Player))
                        {
                            ev.IsAllowed = true;
                            return;
                        }
                    }
                    ev.IsAllowed = false;
                    if (!ev.Player.HasMessage("No!"))
                    {
                        ev.Player.AddMessage("No!", "<color=red><size=27>此物品为goc专属</size></color>", 3f, ScreenLocation.Center);
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
                    player.AddMessage("Wait", $"<color=green><size=20>还剩下没有安装的房间:{w}</size></color>", 3f, ScreenLocation.Center);
                }
                else
                {
                    player.AddMessage("Wait", $"<color=green><size=27>安装完成</size></color>", 3f, ScreenLocation.Center);

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

                    var lp = ev.Player;
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

                        if ((scp5k_Sci.TryGet(Goc610PID, out var sciRole) && sciRole.Check(ev.Player)) || (scp5k_Sci.TryGet(Goc610CID, out var sciCRole) && sciCRole.Check(ev.Player)))
                        {
                            //Log.Info("✅ 玩家拥有安装权限");


                            //var pickup = ev.Item.CreatePickup(Install_Pos,Quaternion.Euler(rotateDir),true);
                            var pickup = ev.Item.CreatePickup(Install_Pos);
                            pickup.Rigidbody.isKinematic = true;
                            pickup.PhysicsModule.Rb.isKinematic = true;
                            foreach (var item in pickup.GameObject.transform.GetComponentsInChildren<NetworkIdentity>())
                            {
                                Exiled.API.Extensions.MirrorExtensions.EditNetworkObject(item, (_) => { });
                            }

                            if (pickup != null)
                            {
                                GOCBomb.installbomb(pickup);
                                Log.Info($"炸弹成功安装在房间: {currentRoom.Name}");
                            }
                            else
                            {
                                //Log.Warn("❌ DropItem 返回 null，安装失败");
                                lp.AddMessage("no", "<color=red>丢弃物品失败，请重试</color>", 3f);
                            }
                        }
                        else

                        {
                            //Log.Info("❌ 玩家没有 Goc610PID 权限");
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
                var p = player;

                p.AddMessage("AEH_GET_HINT" + DateTime.Now.ToString(), "<size=28><color=red>你获得了绝对排斥护具,请查看Server-Specific修改按键</color></size>", 4f, ScreenLocation.Center);

                base.ShowPickedUpMessage(player);
            }
            protected override void OnPickingUp(PickingUpItemEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    if (!Plugin.MenuCache.Any(x => x.Id == Plugin.plugin.Config.SettingIds[Features.AEHKey]))
                        Plugin.MenuCache.AddRange(MenuInit());
                    Plugin.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));
                    Plugin.Register(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));

                    base.OnPickingUp(ev);
                }
            }
            void SearchingItem(SearchingPickupEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    var p = ev.Player;
                    //

                }
            }
            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                if (Check(ev.Item))
                {
                    Plugin.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));
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
                    Plugin.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]));

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
                                var p = player;

                                foreach (var items in player.Items)
                                {
                                    if (item.Check(items))
                                    {
                                        if (CoolDown.Elapsed.TotalSeconds > 105 || !CoolDown.IsRunning)
                                        {
                                            CoolDown.Restart();
                                            player.EnableEffect(Exiled.API.Enums.EffectType.Fade, 255, 45);

                                            p.AddMessage("AEH_INVESS" + DateTime.Now.ToString(), "<color=red>成功隐身</color>", 3f, ScreenLocation.Center);
                                            Timing.CallDelayed(45, () =>
                                            {
                                                p.AddMessage("AEH_INVESS_END" + DateTime.Now.ToString(), "<color=red>隐身结束</color>", 3f, ScreenLocation.Center);
                                                CoolDown.Restart();
                                            });
                                            break;
                                        }
                                        else
                                        {
                                            p.AddMessage("AEH_INVESS_COLLDOWN" + DateTime.Now.ToString(), "<color=red>冷却中</color>", 3f, ScreenLocation.Center);
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
        public static uint ScannerItemID = 5160;
        [CustomItem(ItemType.ArmorHeavy)]
        public class scp5k_Scanner : CustomArmor
        {
            public static scp5k_Scanner ins;
            public override uint Id { get; set; } = ScannerItemID;
            public override string Name { get; set; } = "扫描护具";
            public override string Description { get; set; } = "每两秒扫描150米内的所有人";
            public override float Weight { get; set; } = 10;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            //public override Vector3 Scale { get; set; } = new Vector3(2f, 2f, 2f);
            //override
            protected override void ShowPickedUpMessage(Player player)
            {
                //player.Broadcast(4, "", global::Broadcast.BroadcastFlags.Normal, true);
                var p = player;

                //p.AddMessage("AEH_GET_HINT" + DateTime.Now.ToString(), "<size=28><color=red>你获得了绝对排斥护具,请查看Server-Specific修改按键</color></size>", 4f, ScreenLocation.Center);

                base.ShowPickedUpMessage(player);
            }
            public static IEnumerator<float> Scanner(Player p)
            {
                while (p.IsAlive)
                {
                    foreach (var player in Player.Enumerable)
                    {
                        if (player != p && Vector3.Distance(player.Position, p.Position) <= 150f)
                        {
                            if (HitboxIdentity.IsEnemy(player.ReferenceHub, p.ReferenceHub))
                            {
                                new DrawableLineMessage(0.6f, Color.red, new Vector3[2] { p.CameraTransform.position + 0.2f * Vector3.down, player.Position }).SendToHubsConditionally(x => x == p.ReferenceHub);
                            }
                            else
                            {
                                new DrawableLineMessage(0.6f, Color.green, new Vector3[2] { p.CameraTransform.position + 0.2f * Vector3.down, player.Position }).SendToHubsConditionally(x => x == p.ReferenceHub);
                            }
                        }
                    }
                    yield return Timing.WaitForSeconds(2f);
                }
            }
            protected override void OnPickingUp(PickingUpItemEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    if (!CHs.ContainsKey(ev.Player))
                    {
                        var handle = Plugin.RunCoroutine(Scanner(ev.Player));
                        CHs[ev.Player] = handle;
                    }
                }
                base.OnPickingUp(ev);
            }
            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                if (Check(ev.Item))
                {
                    //Plugin.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]))
                    if (CHs.TryGetValue(ev.Player, out var handle))
                    {
                        Timing.KillCoroutines(handle);
                        CHs.Remove(ev.Player);
                    }
                }
                base.OnDroppingItem(ev);
            }
            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                }
                base.OnUpgrading(ev);
            }
            public static Dictionary<Player, CoroutineHandle> CHs = new Dictionary<Player, CoroutineHandle>();
            public override void Init()
            {
                Type = ItemType.ArmorHeavy;
                ins = this;

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