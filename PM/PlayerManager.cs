using AutoEvent;
using AutoEvent.Commands;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using HintServiceMeow.Core.Models.Arguments;
using InventorySystem;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using Mysqlx.Expr;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using ProjectMER.Commands.Utility;
using Respawning;
using Respawning.Waves;
using Scp914;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UserSettings.ServerSpecific;
using Utils;
using Utils.NonAllocLINQ;
using VoiceChat.Codec;
using VoiceChat.Networking;
using static HintServiceMeow.Core.Models.HintContent.AutoContent;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;
using Random = UnityEngine.Random;
using Scp079Role = Exiled.API.Features.Roles.Scp079Role;
using SpectatorRole = Exiled.API.Features.Roles.SpectatorRole;

namespace Next_generationSite_27.UnionP
{

    class PlayerManager : BaseClass
    {
        public static double global_experience_multiplier = 1;
        public static MySQLConnect sql => Plugin.plugin.connect;
        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            Exiled.Events.Handlers.Player.Shot += Shot;
            Exiled.Events.Handlers.Scp079.GainingExperience += GainingExperience;
            Exiled.Events.Handlers.Warhead.Starting += PlayerManager.Starting;
            Exiled.Events.Handlers.Warhead.Stopping += PlayerManager.Stopping;
            Exiled.Events.Handlers.Player.Verified += PlayerManager.Verified;
            Exiled.Events.Handlers.Player.PreAuthenticating += PlayerManager.PreAuthenticating;
            Exiled.Events.Handlers.Player.DroppingAmmo += PlayerManager.DroppedAmmo;
            Exiled.Events.Handlers.Server.RestartingRound += PlayerManager.RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers += PlayerManager.WaitingForPlayers;
            Exiled.Events.Handlers.Player.Left += PlayerManager.Left;
            Exiled.Events.Handlers.Warhead.Stopping += PlayerManager.Stopping;
            Exiled.Events.Handlers.Player.Hurting += PlayerManager.Hurting;
            Exiled.Events.Handlers.Player.Dying += PlayerManager.Dying;
            Exiled.Events.Handlers.Player.Escaped += Escaping;
            Exiled.Events.Handlers.Server.RoundEnded += RoundEnded;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += EnteringPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += EscapingPocketDimension;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension += FailingEscapePocketDimension;
            Exiled.Events.Handlers.Scp914.ChangingKnobSetting += ChangingKnobSetting;
            Exiled.Events.Handlers.Player.VoiceChatting += VoiceChatting;
            Exiled.Events.Handlers.Scp914.Activating += Activating;
            Exiled.Events.Handlers.Map.GeneratorActivating += GeneratorActivating;
            rec = Timing.RunCoroutine(RefreshAllPlayers(), segment: Segment.FixedUpdate);

            Plugin.MenuCache.AddRange(Menu());
            //base.Init();
        }
        public static CoroutineHandle rec;
        public override void Delete()
        {

            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            Exiled.Events.Handlers.Player.Shot -= Shot;
            Plugin.MenuCache.RemoveAll(x => x.Id == Plugin.Instance.Config.SettingIds[Features.LevelHeader] || x.Id == Plugin.Instance.Config.SettingIds[Features.ScpDestroyYanTiButton]);
            Plugin.MenuCache.RemoveAll(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk]);
            Exiled.Events.Handlers.Warhead.Starting -= PlayerManager.Starting;
            Exiled.Events.Handlers.Scp079.GainingExperience -= GainingExperience;
            Exiled.Events.Handlers.Player.DroppingAmmo -= PlayerManager.DroppedAmmo;
            Exiled.Events.Handlers.Player.Verified -= PlayerManager.Verified;
            Exiled.Events.Handlers.Player.PreAuthenticating -= PlayerManager.PreAuthenticating;
            Exiled.Events.Handlers.Server.RestartingRound -= PlayerManager.RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= PlayerManager.WaitingForPlayers;
            Exiled.Events.Handlers.Player.Hurting -= PlayerManager.Hurting;
            Exiled.Events.Handlers.Player.Left -= PlayerManager.Left;
            Exiled.Events.Handlers.Warhead.Stopping -= PlayerManager.Stopping;
            Exiled.Events.Handlers.Player.Dying -= PlayerManager.Dying;
            Exiled.Events.Handlers.Server.RoundEnded -= RoundEnded;
            Exiled.Events.Handlers.Player.Escaped -= Escaping;
            Exiled.Events.Handlers.Scp914.ChangingKnobSetting -= ChangingKnobSetting;
            Exiled.Events.Handlers.Scp914.Activating -= Activating;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= EnteringPocketDimension;
            Exiled.Events.Handlers.Player.VoiceChatting -= VoiceChatting;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= EscapingPocketDimension;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension -= FailingEscapePocketDimension;
            Exiled.Events.Handlers.Map.GeneratorActivating -= GeneratorActivating;
            //StaticUnityMethods.OnUpdate -= RefreshAllPlayers;
            Timing.KillCoroutines(rec);
            //base.Delete();
        }

        public static Dictionary<Player, Player> Scp106CatchPlayers = new Dictionary<Player, Player>();
        public static void EnteringPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (ev.Player != null && ev.Scp106 != null)
            {
                Scp106CatchPlayers[ev.Player] = ev.Scp106;
            }
        }
        public static void EscapingPocketDimension(EscapingPocketDimensionEventArgs ev)
        {
            if (ev.Player != null && Scp106CatchPlayers.ContainsKey(ev.Player))
            {
                Scp106CatchPlayers.Remove(ev.Player);
            }
        }
        public static void FailingEscapePocketDimension(FailingEscapePocketDimensionEventArgs ev)
        {
            if (ev.Player != null && Scp106CatchPlayers.ContainsKey(ev.Player))
            {
                AddExp(Scp106CatchPlayers[ev.Player], 5, true, AddExpReason.ScpKillPeoPle);

                Scp106CatchPlayers.Remove(ev.Player);
            }
        }
        //public static 
        public static Hint Scp914Hint = new Hint()
        {
            AutoText = Scp914Updater,
            YCoordinate = 200,
            XCoordinate = 0,
            SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Normal
        };
        public static List<string> Keep = new List<string>();
        public static string Scp914Updater(AutoContentUpdateArg ev)
        {
            if (ev.PlayerDisplay.ReferenceHub == null)
            {
                ev.PlayerDisplay.RemoveHint(ev.Hint);
                return "";
            }
            else if (!Room.TryGetRoomAtPosition(ev.PlayerDisplay.ReferenceHub.GetPosition(), out var r))
            {
                ev.PlayerDisplay.RemoveHint(ev.Hint);
                return "";
            }
            else if (r.Base.Name != MapGeneration.RoomName.Lcz914)
            {
                ev.PlayerDisplay.RemoveHint(ev.Hint);
                return "";

            }
            int MaxQueueSize = 6;
            if (Scp914q.Count > MaxQueueSize)
            {
                // 丢弃最老的几条
                while (Scp914q.Count > MaxQueueSize)
                    Scp914q.Dequeue();
            }
            ev.DefaultUpdateDelay = TimeSpan.FromSeconds(0.6);
            ev.NextUpdateDelay = TimeSpan.FromSeconds(0.6);
            string t = "";

            var p = Player.Get(ev.PlayerDisplay.ReferenceHub);
            if (p != null && p.CurrentRoom != null)
            {
                if (p.CurrentRoom.Type != RoomType.Lcz914)
                {
                    ev.PlayerDisplay.RemoveHint(ev.Hint);
                    return "";
                }
            }
            else
            {
                return "";
            }

            if (Scp914q.Count > 0)
            {
                while (Scp914q.Count != 0)
                {
                    var k = Scp914q.Dequeue();
                    string transstr = "";
                    switch (k.knob)
                    {
                        case Scp914.Scp914KnobSetting.Rough:
                            transstr = "超粗";
                            break;
                        case Scp914.Scp914KnobSetting.Coarse:
                            transstr = "粗加";
                            break;
                        case Scp914.Scp914KnobSetting.OneToOne:
                            transstr = "1:1";
                            break;
                        case Scp914.Scp914KnobSetting.Fine:
                            transstr = "精加";
                            break;
                        case Scp914.Scp914KnobSetting.VeryFine:
                            transstr = "超精";
                            break;
                        default:
                            break;
                    }
                    if (k.act)
                    {
                        t += $"<size=22><color=green>{k.p.Nickname}</color> 激活了914 模式:<color=yellow>{transstr}</color></size>\n";

                        //Keep.Add(t);
                    }
                    else
                    {
                        t += $"<size=22><color=green>{k.p.Nickname}</color> 修改914模式到 <color=yellow>{transstr}</color></size>\n";
                        //Keep.Add(t);
                    }
                }
            }
            return t;
        }


        public static Queue<(Player p, Scp914KnobSetting knob, bool act)> Scp914q = new Queue<(Player p, Scp914KnobSetting knob, bool act)>();
        public static void ChangingKnobSetting(ChangingKnobSettingEventArgs ev)
        {
            Scp914q.Enqueue((ev.Player, ev.KnobSetting, false));

            // 安全遍历所有玩家，并检查 CurrentRoom 是否存在
            foreach (var player in Player.Enumerable.Where(player => player.CurrentRoom?.RoomName != MapGeneration.RoomName.Lcz914))
            {
                try
                {

                    // 获取 HUD 组件
                    var hudComponent = player.GetHUD() as HSM_hintServ;
                    if (hudComponent == null || hudComponent.hud == null)
                        continue;

                    // 防止重复添加
                    if (!hudComponent.hud.HasHint("Scp914KnobChanged"))
                    {
                        hudComponent.hud.AddHint(Scp914Hint);
                    }
                }
                catch (Exception ex)
                {
                    // 可选：记录异常，避免影响其他玩家
                    Log.Warn($"Failed to show SCP-914 hint to player {player.Nickname}: {ex.Message}");
                }
            }
        }
        public static void Activating(ActivatingEventArgs ev)
        {
            // 先入队（原逻辑）
            Scp914q.Enqueue((ev.Player, ev.KnobSetting, true));

            // 安全遍历所有玩家，并检查 CurrentRoom 是否存在
            foreach (var player in Player.Enumerable)
            {
                try
                {
                    if (player == null || player.CurrentRoom == null) continue;
                    // 检查玩家是否在 LCZ-914 房间
                    if (player.CurrentRoom?.RoomName != MapGeneration.RoomName.Lcz914)
                        continue;

                    // 获取 HUD 组件
                    var hudComponent = player.GetHUD() as HSM_hintServ;
                    if (hudComponent == null || hudComponent.hud == null)
                        continue;

                    // 防止重复添加
                    if (!hudComponent.hud.HasHint("Scp914KnobChanged"))
                    {
                        hudComponent.hud.AddHint(Scp914Hint);
                    }
                }
                catch (Exception ex)
                {
                    // 可选：记录异常，避免影响其他玩家
                    Log.Warn($"Failed to show SCP-914 hint to player {player.Nickname}: {ex.Message}");
                }
            }
        }
        public static void GeneratorActivating(GeneratorActivatingEventArgs ev)
        {
            if (ev.Generator.LastActivator != null)
            {
                foreach (var item in Player.Enumerable.Where(x => x.Role.Team == ev.Generator.LastActivator.Role.Team))
                {
                    {
                        AddExp(item, 15, true, AddExpReason.Scp079Gener);
                    }
                }
            }
        }
        public static void RoundEnded(RoundEndedEventArgs ev)
        {
            foreach (var item in Player.Enumerable)
            {
                {
                    AddExp(item, 5, reason: AddExpReason.RoundEnd);
                    if (ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.Anomalies || (Scp5k_Control.Is5kRound && ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces))
                    {
                        if (item.Role.Type.IsScp())
                        {
                            AddExp(item, 10, reason: AddExpReason.ScpWin);
                        }
                    }
                    if (ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces ||
                        ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.ChaosInsurgency)
                    {
                        if (item.Role.Type.IsHuman())
                        {
                            AddExp(item, 10, reason: AddExpReason.HumanWin);
                        }
                    }
                }
            }
        }
        public static void Escaping(EscapedEventArgs ev)
        {
            //if (ev.IsAllowed)
            {
                AddExp(ev.Player, 25, reason: AddExpReason.DDSCIEscaped);
                if (ev.Player.IsCuffed)
                {
                    AddExp(ev.Player.Cuffer, 15, false, AddExpReason.CuffedPeopleEscaped);
                }
            }
            if (Scp5k_Control.Is5kRound)
            {
                ev.Player.Role.Set(RoleTypeId.ChaosConscript);
            }
        }
        public static void GainingExperience(GainingExperienceEventArgs ev)
        {
            if (ev.Player != null)
            {
                if (ev.GainType == Scp079HudTranslation.ExpGainTerminationDirect)
                {
                    AddExp(ev.Player, 20, true, AddExpReason.ScpKillPeoPle);
                }
                if (ev.GainType == Scp079HudTranslation.ExpGainTerminationAssist)
                {
                    AddExp(ev.Player, 5, true, AddExpReason.ScpKillPeoPle);
                }
            }
        }
        public static void Dying(DyingEventArgs ev)
        {
            if (ev.Attacker != null)
            {
                bool isHandGunKill = (ev.DamageHandler.Base is FirearmDamageHandler f && (f.WeaponType == ItemType.GunCOM15 || f.WeaponType == ItemType.GunCOM18));
                bool x3orJailBirdKill096 = (ev.Player.Role.Type == RoleTypeId.Scp096 && (ev.DamageHandler.Base is JailbirdDamageHandler || ev.DamageHandler.Base is DisruptorDamageHandler));
                bool GunKilledRage096 = (ev.Player.Role is Scp096Role s && s.RageState == PlayerRoles.PlayableScps.Scp096.Scp096RageState.Enraged && (ev.DamageHandler.Base is FirearmDamageHandler));
                if (!ev.Attacker.IsScp)
                {
                    if (!ev.Player.Role.Type.IsScp())
                    {
                        int exp = 10;
                        exp = exp + 10 * ev.Player.GetEffect(EffectType.Scp207).Intensity
                            + 20 * ev.Player.GetEffect(EffectType.Scp1344).Intensity
                            + (isHandGunKill ? 5 : 0)
                            + 10 * ev.Player.GetEffect(EffectType.Scp1853).Intensity;
                        AddExp(ev.Attacker, exp, true, AddExpReason.PeopleKillPeoPle);

                    }
                    else
                    {
                        int exp = (isHandGunKill ? 5 : 0) + (x3orJailBirdKill096 ? 50 : 0) + (GunKilledRage096 ? 100 : 0);
                        if (ev.Player.Role.Type == RoleTypeId.Scp0492)
                        {
                            AddExp(ev.Attacker, exp + 20, true, AddExpReason.KillZombie);
                        }
                        else
                        {
                            AddExp(ev.Attacker, exp + 50, false, AddExpReason.killScp);

                        }

                    }
                }
                else
                {
                    int exp = 10;
                    exp = exp + 10 * ev.Player.GetEffect(EffectType.Scp207).Intensity
    + 20 * ev.Player.GetEffect(EffectType.Scp1344).Intensity
    + 10 * ev.Player.GetEffect(EffectType.Scp1853).Intensity;
                    AddExp(ev.Attacker, exp, true, AddExpReason.ScpKillPeoPle);

                }
            }
        }
        public static void Hurting(HurtingEventArgs ev)
        {
            if (ev.DamageHandler.Type == DamageType.Scp207 || ev.DamageHandler.Type == DamageType.Poison)
            {
                if (ev.DamageHandler.Type != DamageType.Poison)
                {
                    ev.Player.DisableEffect(EffectType.Poisoned);
                }
                ev.IsAllowed = false;
            }
        }
        public static void WaitingForPlayers()
        {

        }
        public static void Left(LeftEventArgs ev)
        {
            ScpToSpeaker.Remove(ev.Player);
            sql.Update(ev.Player.UserId, name: ev.Player.Nickname, today_duration: GetTodayTimer(ev.Player));
        }
        public static void RestartingRound()
        {

            expCache.Clear();
            levelCache.Clear();
            foreach (var item in TodayTimer)
            {
                item.Value.Stop();
                sql.Update(item.Key.UserId, name: item.Key.Nickname, today_duration: GetTodayTimer(item.Key));
            }
            TodayTimeCache.Clear();
            foreach (var item in PointCache)
            {
                sql.Update(item.Key.UserId,point:item.Value);
            }
            PointCache.Clear();
            TodayTimer.Clear();
            SpecList.Clear();
        }
        public static void Shot(ShotEventArgs ev)
        {
            foreach (var i in Enum.GetValues(typeof(AmmoType)))
            {
                ev.Player.SetAmmo((AmmoType)i, ev.Player.GetAmmoLimit((AmmoType)i));
            }
        }
        public static IEnumerator<float> ClearPower(Scp079Role sr)
        {
            float wt = 18f;
            float i = 0f;
            while (sr != null)
            {
                i += 0.2f;
                if (i >= wt)
                {
                    yield break;
                }
                sr.Energy = 0;
                yield return Timing.WaitForSeconds(0.2f);
            }
        }
        public static List<Player> TalkTohumanScp = new List<Player>();
        public static Dictionary<Player, AdminToys.SpeakerToy> ScpToSpeaker = new Dictionary<Player, AdminToys.SpeakerToy>();
        private static AdminToys.SpeakerToy _speakerPrefab;

        private static AdminToys.SpeakerToy GetSpeakerPrefab()
        {
            if (_speakerPrefab != null)
                return _speakerPrefab;

            foreach (var prefab in NetworkClient.prefabs.Values)
            {
                if (prefab.TryGetComponent(out AdminToys.SpeakerToy toy))
                {
                    _speakerPrefab = toy;
                    break;
                }
            }
            return _speakerPrefab;
        }
        public static void VoiceChatting(VoiceChattingEventArgs ev)
        {
            if (ev.Player.IsScp)
            {
                if (TalkTohumanScp.Contains(ev.Player))
                {
                    var id = (byte)(120 + ev.Player.Id);
                    if (!ScpToSpeaker.TryGetValue(ev.Player, out var sp))
                    {
                        var prefab = GetSpeakerPrefab();
                        if (prefab == null)
                            return;

                        var newInstance = GameObject.Instantiate(prefab, ev.Player.Position, Quaternion.identity);
                        newInstance.NetworkControllerId = id;
                        newInstance.NetworkVolume = 1f;
                        newInstance.IsSpatial = false;
                        newInstance.MinDistance = 0f;
                        newInstance.MaxDistance = 20f;
                        newInstance.transform.parent = ev.Player.Transform;

                        NetworkServer.Spawn(newInstance.gameObject);

                        ScpToSpeaker.Add(ev.Player, newInstance);
                        sp = newInstance;
                    }

                    sp.transform.position = ev.Player.Position;
                    sp.MaxDistance = 20f;
                    sp.MinDistance = 0f;

                    var vm = new AudioMessage()
                    {
                        ControllerId = id,
                        Data = ev.VoiceMessage.Data,
                        DataLength = ev.VoiceMessage.DataLength,
                    };

                    foreach (var hub in ReferenceHub.AllHubs.Where(x =>
                        x.roleManager.CurrentRole is FpcStandardRoleBase i &&
                        Vector3.Distance(i.CameraPosition, ev.Player.Position) <= 20 && x != ev.Player.ReferenceHub && x.roleManager.CurrentRole.Team != Team.SCPs))

                    {
                        hub.connectionToClient.Send(vm, 0);
                    }

                    //var vm = ev.VoiceMessage;
                    //vm.Channel = VoiceChat.VoiceChatChannel.Proximity;
                    //foreach (var item in ReferenceHub.AllHubs.Where(x =>
                    //    {
                    //        if (x.roleManager.CurrentRole is FpcStandardRoleBase i)
                    //        {
                    //            return Vector3.Distance(i.CameraPosition, ev.Player.Position) <= 20;
                    //        }
                    //        return false;
                    //    }))
                    //{

                    //    item.connectionToClient.Send<VoiceMessage>(vm, 0);
                    //}

                }
            }
        }
        public static List<SettingBase> Menu()
        {
            List<SettingBase> settings = new List<SettingBase>();
            settings.Add(new KeybindSetting(
   Plugin.Instance.Config.SettingIds[Features.ScpTalk], "SCP切换语音频道", KeyCode.V, false, false, "与人类沟通",
   onChanged: (player, SB) =>
   {
       try
       {
           if (SB is KeybindSetting ks)
           {

               if (ks.IsPressed)
               {
                   if (player != null)
                   {
                       if (player.IsScp)
                       {
                           if (!TalkTohumanScp.Contains(player))
                           {
                               TalkTohumanScp.Add(player);
                               player.AddMessage($"EnableScpToHumanTalk-{DateTime.Now.ToString()}", "<voffset=-1em><size=29><b>已启用scp对人类讲话</b></size></voffset>", duration: 3f);

                           }
                           else
                           {
                               TalkTohumanScp.Remove(player);
                               player.AddMessage($"DisableScpToHumanTalk-{DateTime.Now.ToString()}", "<voffset=-1em><size=29><b>已禁用scp对人类讲话</b></size></voffset>", duration: 3f);

                           }
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
        public static void Stopping(StoppingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
            {
                Timing.CallDelayed(0.2f, () =>
                {
                });
            }
        }
        public static void Starting(StartingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
            {
            }

        }
        public static string GetGreetingWord()
        {
            var t = DateTime.Now;
            int h = t.Hour;
            if (h >= 6 && h <= 11)
            {
                return "早上好";
            }
            else if (h >= 11 && h <= 14)
            {
                return "中午好";
            }
            else if (h >= 15 && h <= 17)
            {
                return "下午好";
            }
            else if (h >= 18 && h <= 23)
            {
                return "晚上好";
            }
            else if (h >= 24 && h <= 5)
            {
                return "夜深了";
            }
            else
            {
                return "";
            }
        }
        public static List<CandyKindID> CandyList = new List<CandyKindID>()
    {
            CandyKindID.Rainbow,
            CandyKindID.Rainbow,
            CandyKindID.Rainbow,
            CandyKindID.Rainbow,
            CandyKindID.Rainbow,
            CandyKindID.Rainbow,
             CandyKindID.Yellow,
             CandyKindID.Yellow,
             CandyKindID.Yellow,
             CandyKindID.Yellow,
             CandyKindID.Yellow,
             CandyKindID.Yellow,
             CandyKindID.Purple,
             CandyKindID.Purple,
             CandyKindID.Purple,
             CandyKindID.Purple,
             CandyKindID.Red,
             CandyKindID.Red,
             CandyKindID.Red,
             CandyKindID.Green,
             CandyKindID.Green,
             CandyKindID.Green,
             CandyKindID.Blue,
             CandyKindID.Blue,
             CandyKindID.Blue,
             CandyKindID.Pink,
             CandyKindID.Pink,
    };
        public static void ChangingRole(ChangingRoleEventArgs ev)
        {
            //Log.Info($"{ev.Player} changing role");
            ScpToSpeaker.Remove(ev.Player);
            var keysToUpdate = new List<Player>();
            foreach (var entry in SpecList.ToList())
            {
                if (entry.Value.Contains(ev.Player))
                {
                    keysToUpdate.Add(entry.Key);
                }
            }

            foreach (var key in keysToUpdate)
            {
                SpecList[key].Remove(ev.Player);

                // 如果这个目标没有观众了，可以顺手移除 key
                if (SpecList[key].Count == 0)
                    SpecList.Remove(key);
            }

            TalkTohumanScp.Remove(ev.Player);
            var h = HSM_hintServ.GetPlayerHUD(ev.Player) as HSM_hintServ;
            if (ev.NewRole == RoleTypeId.Spectator || ev.NewRole == RoleTypeId.Overwatch)
            {
                h.hud.AddHint(ChaosSpawnHint);
                h.hud.AddHint(NtfSpawnHint);
                h.hud.AddHint(SpawnHint);
            }
            else
            {
                h.hud.RemoveHint(SpawnHint);
                h.hud.RemoveHint(NtfSpawnHint);
                h.hud.RemoveHint(ChaosSpawnHint);
            }
            if (h != null)
            {
                if (h.hud.HasHint("Scp914KnobChanged"))
                {

                    h.hud.RemoveHint(Scp914Hint);
                }

            }


            var menuItems = Plugin.MenuCache?
.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk])
?.ToList();
            if (true)
            {
                if (menuItems != null && menuItems.Count > 0)
                {
                    if (Plugin.GetPlayerRegistered(ev.Player).Any(x => menuItems.Contains(x)))
                    {
                        Plugin.Unregister(ev.Player, menuItems);
                    }
                }
                else
                {
                    Log.Debug($"[ChangingRole] 未找到 SettingIds[{Features.ScpTalk}] 对应菜单项。");
                }
            }




            if (ev.Reason != SpawnReason.RoundStart) return;
            Timing.CallDelayed(0.4f, () =>
        {
            try
            {
                if (ev.Player == null) return;
                foreach (var i in Enum.GetValues(typeof(AmmoType)))
                {
                    ev.Player.SetAmmo((AmmoType)i, ev.Player.GetAmmoLimit((AmmoType)i));
                }
                if (ev.Player.IsScp)
                {

                    if (menuItems != null)
                    {
                        if (!Plugin.GetPlayerRegistered(ev.Player).Any(a => a.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk]))
                        {
                            Plugin.Register(ev.Player, menuItems);
                        }

                    }
                }


                    if (Plugin.Instance.Config.Level)
                    {
                        if (AutoEvent.AutoEvent.EventManager.CurrentEvent != null)
                        {
                            return;
                        }

                        CandyList.ShuffleList();
                        var player = ev.Player;
                    /*E段每人10%一个糖， D每人15%一颗
糖，10%概率给手电筒，C段25%一颗
糖，10%给手电，20%给对讲机；B段40%
给一个糖，15%概率给第二课糖，15%给手
电，15%给对讲，8%给闪光，7%给手雷。
A段50%概率给一颗糖，15%给第二颗糖
20%概率手电，20%对讲机，0.5%保安枪，
2%com15，2%com17，0.5%左轮。5%给轻
甲，4%战术甲，1%重甲。S段50概率给第
一颗糖，20%给第二颗糖，10%给轻甲，5%
给战术甲，5%给重甲，3%保安枪，
2%com15，3%com17，5%左轮，0.01%给
e11*/
                    if (player.Role.Type == RoleTypeId.ClassD || player.Role.Type == RoleTypeId.Scientist)
                    {
                        if (UnityEngine.Random.Range(0, 4) <= 1)
                        {
                            player.AddItem(ItemType.KeycardScientist);

                        }
                        else
                        if (UnityEngine.Random.Range(0, 5) <= 1)
                        {
                            player.AddItem(ItemType.KeycardZoneManager);
                        }
                        else
                        {
                            player.AddItem(ItemType.KeycardJanitor);
                        }
                        player.AddItem(ItemType.Medkit);

                    }
                    switch (GetLevel(player))
                    {
                        case ExpTier.Small:
                            if(UnityEngine.Random.Range(0,100)<=10)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            break;
                        case ExpTier.Medium:
                            if (UnityEngine.Random.Range(0, 100) <= 15)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 10)
                            {
                                player.AddItem(ItemType.Flashlight);
                            }
                            break;
                        case ExpTier.Large:
                            if (UnityEngine.Random.Range(0, 100) <= 25)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 10)
                            {
                                player.AddItem(ItemType.Flashlight);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 20)
                            {
                                player.AddItem(ItemType.Radio);
                            }
                            break;
                        case ExpTier.Pot:
                            if (UnityEngine.Random.Range(0, 100) <= 40)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 15)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 15)
                            {
                                player.AddItem(ItemType.Flashlight);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 15)
                            {
                                player.AddItem(ItemType.Radio);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 8)
                            {
                                player.AddItem(ItemType.GrenadeFlash);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 7)
                            {
                                player.AddItem(ItemType.GrenadeHE);
                            }
                            break;
                        case ExpTier.Shao:
                            if (UnityEngine.Random.Range(0, 100) <= 50)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 15)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 20)
                            {
                                player.AddItem(ItemType.Flashlight);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 20)
                            {
                                player.AddItem(ItemType.Radio);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 2)
                            {
                                player.AddItem(ItemType.GunCOM15);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 2)
                            {
                                player.AddItem(ItemType.GunCOM18);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 1)
                            {
                                player.AddItem(ItemType.GunFSP9);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 1)
                            {
                                player.AddItem(ItemType.GunRevolver);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 5)
                            {
                                player.AddItem(ItemType.ArmorLight);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 4)
                            {
                                player.AddItem(ItemType.ArmorCombat);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 1)
                            {
                                player.AddItem(ItemType.ArmorHeavy);
                            }
                            break;
                        case ExpTier.Eat:
                        case ExpTier.EatPlus:
                            if (UnityEngine.Random.Range(0, 100) <= 50)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 20)
                            {
                                var candy = CandyList.RandomItem();
                                player.TryAddCandy(candy);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 20)
                            {
                                player.AddItem(ItemType.Flashlight);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 20)
                            {
                                player.AddItem(ItemType.Radio);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 2)
                            {
                                player.AddItem(ItemType.GunCOM15);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 2)
                            {
                                player.AddItem(ItemType.GunCOM18);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 3)
                            {
                                player.AddItem(ItemType.GunFSP9);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 5)
                            {
                                player.AddItem(ItemType.GunRevolver);
                            }
                            if (UnityEngine.Random.Range(0, 100) <= 10)
                            {
                                player.AddItem(ItemType.ArmorLight);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 5)
                            {
                                player.AddItem(ItemType.ArmorCombat);
                            }
                            else if (UnityEngine.Random.Range(0, 100) <= 5)
                            {
                                player.AddItem(ItemType.ArmorHeavy);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn(e.ToString());
            }
        });

        }
        public static Dictionary<Player, CoroutineHandle> rainbowC = new Dictionary<Player, CoroutineHandle>();
        public static Dictionary<Player, (Stopwatch stand, double lastTime, Vector3 lastPos)> ScpStandHP = new Dictionary<Player, (Stopwatch stand, double lastTime, Vector3 lastPos)>();
        public static void DroppedAmmo(DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }
        public static IEnumerator<float> RefreshAllPlayers()
        {
            while (true)
            {
                foreach (var player in Player.Enumerable)
                {
                    if (player == null)
                        continue;

                    try
                    {
                        // 缓存常用属性
                        var hub = player.ReferenceHub;
                        var role = player.Role;
                        var roleType = role?.Type ?? RoleTypeId.None;
                        var room = player.CurrentRoom;
                        var hsm = HSM_hintServ.GetPlayerHUD(player) as HSM_hintServ;
                        var hud = hsm?.hud;

                        // ==================== 914 区域提示清理 ====================
                        try
                        {
                            if (room == null || room.Type != RoomType.Lcz914)
                            {
                                var a = player.GetHUD() as HSM_hintServ;
                                a?.hud?.RemoveHint(Scp914Hint);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warn($"[HUD914] {player.Nickname}: {e.Message}");
                        }

                        // ==================== Guard逃生逻辑 ====================
                        if (roleType == RoleTypeId.FacilityGuard)
                            HandleGuardEscape(player, hub);

                        // ==================== 第二层 HUD 检查 ====================
                        if (hud != null && (room == null || room.Type != RoomType.Lcz914))
                        {
                            if (hud.HasHint("Scp914KnobChanged"))
                                hud.RemoveHint(Scp914Hint);
                        }

                        // ==================== 徽章同步 ====================
                        HandleBadgeSync(player, hub);

                        // ==================== 观察者追踪 ====================
                        if (role is SpectatorRole spectatorRole)
                        {
                            HandleSpectatorTracking(player, spectatorRole);
                        }
                        else if (role is OverwatchRole overwatch)
                        {
                            HandleSpectatorTracking(player, overwatch);
                        }
                        else
                        {
                            // ✅ 加空判断
                            if (hud != null)
                            {
                                hud.RemoveHint(SpawnHint);
                                hud.RemoveHint(NtfSpawnHint);
                                hud.RemoveHint(ChaosSpawnHint);
                            }
                        }

                        // ==================== SCP 自动回血 ====================
                        if (roleType.IsScp() && role?.Base is IFpcRole fpcRole)
                            HandleScpStandHeal(player, fpcRole);

                        // ==================== 更新名字显示 ====================
                        UpdatePlayerDisplayName(player);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[RefreshAllPlayers] {player?.Nickname ?? "Unknown"}: {e.GetType().Name} - {e.Message}");
                    }
                }

                yield return Timing.WaitForSeconds(0.25f);
            }
        }

        private static void HandleGuardEscape(Player player, ReferenceHub hub)
        {
            if (!Escape.CanEscape(hub, out var role, out var zone))
                return;

            var humanRole = role as PlayerRoles.HumanRole;
            var newRole = Scp5k_Control.Is5kRound ? RoleTypeId.ChaosRifleman : RoleTypeId.NtfSergeant;
            var currentRoleType = hub.roleManager.CurrentRole.RoleTypeId;
            var escapeScenario = Escape.EscapeScenarioType.Scientist;

            var args = new PlayerEscapingEventArgs(hub, currentRoleType, newRole, escapeScenario, zone);
            PlayerEvents.OnEscaping(args);

            if (!args.IsAllowed || args.EscapeScenario == Escape.EscapeScenarioType.None)
                return;

            // 发送逃脱消息
            hub.connectionToClient.Send<Escape.EscapeMessage>(new Escape.EscapeMessage
            {
                ScenarioId = (byte)args.EscapeScenario,
                EscapeTime = (ushort)Mathf.CeilToInt(hub.roleManager.CurrentRole.ActiveTime)
            }, 0);

            // 设置新角色
            hub.roleManager.ServerSetRole(args.NewRole, RoleChangeReason.Escaped, RoleSpawnFlags.All, null);

            // 触发逃脱后事件
            PlayerEvents.OnEscaped(new PlayerEscapedEventArgs(hub, currentRoleType, args.NewRole, args.EscapeScenario, zone));
            AddExp(player, 10, false, AddExpReason.GuardEscaped);

        }

        private static void HandleBadgeSync(Player player, ReferenceHub hub)
        {
            if (!badges.TryGetValue(player.UserId, out var badgeData))
                return;

            if (testing.FlightFailed.PlayerToBadge.ContainsKey(player.UserId))
                return;
            if (hub.serverRoles.Network_myText == null)
            {
                player.RankName = badgeData.badge;

            }
            // 同步徽章文本
            if (!hub.serverRoles.Network_myText.Contains(badgeData.badge))
            {
                player.RankName = badgeData.badge;
            }

            // 同步颜色
            //if (hub.serverRoles.Network_myColor != badgeData.color)
            {
                if (badgeData.color.Contains("rainbow"))
                {
                    if (!rainbowC.ContainsKey(player))
                    {
                        rainbowC[player] = Timing.RunCoroutine(rainbowTime(player, colors));
                    }
                    else if (!rainbowC[player].IsRunning)
                    {
                        rainbowC[player] = Timing.RunCoroutine(rainbowTime(player, colors));
                    }
                }
                else
                {
                    rainbowC[player] = Timing.RunCoroutine(rainbowTime(player, badgeData.color));

                }
            }
        }

        private static void HandleSpectatorTracking(Player player, Exiled.API.Features.Roles.SpectatorRole spectatorRole)
        {
            if (player == null || !player.IsConnected)
                return;

            var target = spectatorRole?.SpectatedPlayer;

            // 先清理掉所有无效 key（目标玩家已下线）
            foreach (var kv in SpecList.Keys.ToList())
            {
                if (kv == null || !kv.IsConnected)
                {
                    SpecList.Remove(kv);
                }
            }

            // 从其他目标的观察者列表里移除自己
            var keysToUpdate = new List<Player>();
            foreach (var entry in SpecList.ToList())
            {
                if (entry.Value.Contains(player))
                {
                    keysToUpdate.Add(entry.Key);
                }
            }

            foreach (var key in keysToUpdate)
            {
                SpecList[key].Remove(player);

                // 如果这个目标没有观众了，可以顺手移除 key
                if (SpecList[key].Count == 0)
                    SpecList.Remove(key);
            }

            // 如果没有正在观战的目标，就不再添加
            if (target == null || !target.IsConnected)
                return;

            // 确保字典有对应 key
            if (!SpecList.ContainsKey(target))
            {
                SpecList[target] = new List<Player>();
            }

            // 加入目标的观众列表
            if (!SpecList[target].Contains(player))
            {
                SpecList[target].Add(player);
            }
        }


        private static void HandleScpStandHeal(Player player, IFpcRole fpcRole)
        {
            if (!Plugin.Instance.Config.ScpStandAddHP)
                return;

            double interval = 1.0; // 每秒回血检查一次

            if (!ScpStandHP.TryGetValue(player, out var data))
                ScpStandHP[player] = (Stopwatch.StartNew(), 0.0, player.Position);

            var (stopwatch, lastHealTime, lastPos) = ScpStandHP[player];
            double elapsed = stopwatch.Elapsed.TotalSeconds;

            // 判断是否移动（允许0.05米以内的浮动）
            if (Vector3.Distance(player.Position, lastPos) < 0.5f)
            {
                // 站够指定时间开始回血
                if (elapsed >= Plugin.Instance.Config.ScpStandAddHPTime)
                {
                    if (elapsed - lastHealTime >= interval)
                    {
                        player.Heal(Plugin.Instance.Config.ScpStandAddHPCount);
                        ScpStandHP[player] = (stopwatch, elapsed, player.Position);

                        Log.Debug($"[ScpStandHeal] {player.Nickname} healed {Plugin.Instance.Config.ScpStandAddHPCount} HP");
                    }
                }
            }
            else
            {
                // 移动了，重置计时
                stopwatch.Restart();
                ScpStandHP[player] = (stopwatch, 0.0, player.Position);
            }
        }


        private static void UpdatePlayerDisplayName(Player player)
        {
            string level = LevelToName(GetLevel(player));
            string expectedName = $"{level} | {player.Nickname}";

            // 避免 Contains 引起误判（比如名字里有 Lv.x）
            if (!player.DisplayNickname.Contains(expectedName))
            {
                player.DisplayNickname = expectedName;
            }
        }
        public static List<string> colors = new List<string>()
        {
            "red",
            "green",
            "yellow",
            "cyan",
            "magenta",
            //"gray",
        };
        public static Hint NtfSpawnHint = new Hint()
        {
            Id = "NtfSpawnHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                if (x.PlayerDisplay.ReferenceHub != null)
                {
                    var p = Player.Get(x.PlayerDisplay.ReferenceHub);
                    if (p != null)
                    {
                        if (p.IsAlive)
                        {
                            return "";
                        }
                    }
                }
                string r = "";
                foreach (var i in PlayerHudSpawnNtfShow(Player.Get(x.PlayerDisplay.ReferenceHub)))
                {
                    r += i + "\n";
                }
                return r;
            }),
            XCoordinate = 150,
            YCoordinate = 100
        };
        public static Hint ChaosSpawnHint = new Hint()
        {
            Id = "ChaosSpawnHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                string r = "";
                foreach (var i in PlayerHudSpawnChaosShow(Player.Get(x.PlayerDisplay.ReferenceHub)))
                {
                    r += i + "\n";
                }
                return r;
            }),
            XCoordinate = 0,
            YCoordinate = 100
        };
        public static Hint SpawnHint = new Hint()
        {
            Id = "SpawnHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                string r = "";
                foreach (var i in PlayerHudSpawnHintShow(Player.Get(x.PlayerDisplay.ReferenceHub)))
                {
                    r += i + "\n";
                }
                return r;
            }),
            XCoordinate = 0,
            YCoordinate = 190
        };
        public static IEnumerator<float> rainbowTime(Player player, List<string> colors)
        {
            if (player == null)
            {
                yield break;
            }
            while (true)
            {
                if (player == null)
                {
                    break;
                }
                foreach (var color in colors)
                {
                    if (player == null)
                    {
                        break;
                    }
                    player.RankColor = color;
                    yield return Timing.WaitForSeconds(1.5f);
                }
                yield return Timing.WaitForSeconds(1.5f);
            }
        }
        public static Dictionary<Player, List<Player>> SpecList = new Dictionary<Player, List<Player>>();
        public static string[] PlayerHudLVShow(Player player)
        {
            var p = GetTodayTimer(player);
            int SpecCount = 0;
            short totalTick = ServerStatic.ServerTickrate;
            double currentTick = Math.Round((double)(1f / Time.smoothDeltaTime));
            string upLine = "";
            string downLine = "";
            if (player.Role is SpectatorRole SR)
            {
                if (SR.SpectatedPlayer != null) // 在观战
                {
                    if (SpecList.ContainsKey(SR.SpectatedPlayer))
                    {
                        SpecCount = SpecList[SR.SpectatedPlayer].Count;
                    }
                    upLine = $"<align=center><size=25><color=green>{LevelToName(GetLevel(SR.SpectatedPlayer))}</color>  |  <color=green>{GetExperience(SR.SpectatedPlayer)}/{ExpToNextLevel(GetLevel(SR.SpectatedPlayer))}</color>  |  <color=green>薯条:{GetPoint(SR.SpectatedPlayer)}</color> | 称号: <color=white>{(string.IsNullOrEmpty(SR.SpectatedPlayer.RankName) ? "无" : SR.SpectatedPlayer.RankName)}</color></size></align>";
                    if (SR.SpectatedPlayer.UniqueRole != "")
                    {
                        var showing = "";
                        CustomRole role = CustomRole.Get(SR.SpectatedPlayer.UniqueRole);
                        showing = role.Name;

                        downLine = $"<align=center><size=25><color=green>UID:{GetUid(SR.SpectatedPlayer)}</color> | <color=yellow>{SR.SpectatedPlayer.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {GetTodayTimer(SR.SpectatedPlayer).Hours.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Minutes.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Seconds.ToString("D2")}</color> | <color=yellow>扮演: {showing}</color>  |  <color=#FFD700>TPS: {currentTick}/{totalTick}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                    }
                    else
                    {
                        downLine = $"<align=center><size=25><color=green>UID:{GetUid(SR.SpectatedPlayer)}</color> | <color=yellow>{SR.SpectatedPlayer.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {GetTodayTimer(SR.SpectatedPlayer).Hours.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Minutes.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Seconds.ToString("D2")}</color> |  <color=#FFD700>TPS: {currentTick}/{totalTick}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                    }
                    if (Misc.TryParseColor(SR.SpectatedPlayer.RankColor, out var color))
                    {
                        upLine = $"<align=center><size=25><color=green>{LevelToName(GetLevel(SR.SpectatedPlayer))}</color>  |  <color=green>{GetExperience(SR.SpectatedPlayer)}/{ExpToNextLevel(GetLevel(SR.SpectatedPlayer))}</color>  |  <color=green>薯条:{GetPoint(SR.SpectatedPlayer)}</color> | 称号: <color={color.ToHex()}>{(string.IsNullOrEmpty(SR.SpectatedPlayer.RankName) ? "无" : SR.SpectatedPlayer.RankName)}</color></size></align>";
                    }

                }
                else
                {
                    upLine = $"<align=center><size=25><color=green>{LevelToName(GetLevel(SR.SpectatedPlayer))}</color>  |  <color=green>{GetExperience(player)}/{ExpToNextLevel(GetLevel(player))}</color> |  称号: <color=white>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)}</color></size></align>";
                    downLine = $"<align=center><size=25><color=green>UID:{GetUid(player)}</color> | <color=yellow>尊敬的 {player.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {p.Hours.ToString("D2")}:{p.Minutes.ToString("D2")}:{p.Seconds.ToString("D2")}</color> |  <color=#FFD700>TPS: {currentTick}/{totalTick}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                    if (Misc.TryParseColor(player.RankColor, out var color))
                    {
                        upLine = $"<align=center><size=25><color=green>{LevelToName(GetLevel(SR.SpectatedPlayer))}</color>  |  <color=green>{GetExperience(player)}/{ExpToNextLevel(GetLevel(player))}</color> |  称号: <color={color.ToHex()}>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)} </color></size></align></width>";
                    }
                }

            }
            else
            {
                if (SpecList.ContainsKey(player))
                {
                    SpecCount = SpecList[player].Count;
                }
                upLine = $"<align=center><size=25><color=green>Lv.{LevelToName(GetLevel(player))}</color>  |  <color=green>{GetExperience(player)}/{ExpToNextLevel(GetLevel(player))}</color>  | <color=green>薯条:{GetPoint(player)}</color> | 称号: <color=white>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)}</color></size></align>";
                downLine = $"<align=center><size=25><color=green>UID:{GetUid(player)}</color> | <color=yellow>尊敬的 {player.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {p.Hours.ToString("D2")}:{p.Minutes.ToString("D2")}:{p.Seconds.ToString("D2")}</color> |  <color=#FFD700>TPS: {currentTick}/{totalTick}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                if (Misc.TryParseColor(player.RankColor, out var color))
                {
                    upLine = $"<align=center><size=25><color=green>{LevelToName(GetLevel(player))}</color>  |  <color=green>{GetExperience(player)}/{ExpToNextLevel(GetLevel(player))}</color>  |  <color=green>薯条:{GetPoint(player)}</color> | 称号: <color={color.ToHex()}>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)} </color></size></align>";
                }
                if (player.UniqueRole != "")
                {
                    var showing = "";
                    CustomRole role = CustomRole.Get(player.UniqueRole);
                    showing = role.Name;
                    downLine = $"<align=center><size=25><color=green>UID:{GetUid(player)}</color> | <color=yellow>尊敬的 {player.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {p.Hours.ToString("D2")}:{p.Minutes.ToString("D2")}:{p.Seconds.ToString("D2")}</color> |  <color=#FFD700>TPS: {currentTick}/{totalTick}</color> | <color=yellow>你是: {showing}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                }
            }

            return new string[] { upLine, downLine };
        }
        public static string[] PlayerHudSpawnNtfShow(Player player)
        {
            string upLine = "";
            string downLine = "";
            if (player == null)
            {
                //if (player.IsAlive)
                {
                    return new string[] { };
                }
            }
            else if (player != null)
            {
                if (player.IsAlive)
                {
                    return new string[] { };
                }
            }
            if (player.Role is SpectatorRole)
            {
                var NtfBig = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                var NtfSmall = WaveManager.Waves.FirstOrDefault(x => x is NtfMiniWave) as NtfMiniWave;

                if (NtfBig != null)
                {
                    double timeLeftBig = Math.Max(0, NtfBig.Timer.TimeLeft);
                    var timeSpanBig = TimeSpan.FromSeconds(timeLeftBig);
                    upLine = $"<align=left><size=25><color=#0000ffff>🚁九尾狐: {timeSpanBig:mm\\:ss}</color></size></align>";
                }

                if (NtfSmall != null)
                {
                    double timeLeftSmall = Math.Max(0, NtfSmall.Timer.TimeLeft);
                    var timeSpanSmall = TimeSpan.FromSeconds(timeLeftSmall);
                    downLine = $"<align=left><size=25><color=#0000ffff>🚁九尾狐增援：{timeSpanSmall:mm\\:ss}</color></size></align>";
                }
            }

            return new string[] { upLine, downLine };
        }
        public static string[] PlayerHudSpawnChaosShow(Player player)
        {
            string upLine = "";
            string downLine = "";
            if (player == null)
            {
                //if (player.IsAlive)
                {
                    return new string[] { };
                }
            }
            else if (player != null)
            {
                if (player.IsAlive)
                {
                    return new string[] { };
                }
            }
            if (player.Role is SpectatorRole)
            {
                var ChaosBig = WaveManager.Waves.FirstOrDefault(x => x is ChaosSpawnWave) as ChaosSpawnWave;
                var ChaosSmall = WaveManager.Waves.FirstOrDefault(x => x is ChaosMiniWave) as ChaosMiniWave;

                // 分别处理大部队
                if (ChaosBig != null)
                {
                    // 确保 TimeLeft 非负
                    double timeLeftBig = Math.Max(0, ChaosBig.Timer.TimeLeft);
                    var timeSpanBig = TimeSpan.FromSeconds(timeLeftBig);
                    upLine = $"<margin=8em><align=right><size=25><color=#008000ff>🚗混沌: {timeSpanBig:mm\\:ss}</color></size></align></margin>";
                }

                // 分别处理增援
                if (ChaosSmall != null)
                {
                    // 确保 TimeLeft 非负
                    double timeLeftSmall = Math.Max(0, ChaosSmall.Timer.TimeLeft);
                    var timeSpanSmall = TimeSpan.FromSeconds(timeLeftSmall);
                    downLine = $"<margin=8em><align=right><size=25><color=#008000ff>🚗混沌增援：{timeSpanSmall:mm\\:ss}</color></size></align></margin>";
                }
            }

            return new string[] { upLine, downLine };
        }
        public static Stopwatch WaveCalc = new Stopwatch();
        public static string[] PlayerHudSpawnHintShow(Player player)
        {
            string upLine = "";
            if (player == null)
            {
                //if (player.IsAlive)
                {
                    return new string[] { };
                }
            }
            else if (player != null)
            {
                if (player.IsAlive)
                {
                    return new string[] { };
                }
            }

            if (player.Role is SpectatorRole)
            {
                var ChaosBig = WaveManager.Waves.FirstOrDefault(x => x is ChaosSpawnWave) as ChaosSpawnWave;
                var NtfBig = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                var NtfSmall = WaveManager.Waves.FirstOrDefault(x => x is NtfMiniWave) as NtfMiniWave;
                var ChaosSmall = WaveManager.Waves.FirstOrDefault(x => x is ChaosMiniWave) as ChaosMiniWave;
                if (ChaosSmall.IsAnimationPlaying || NtfBig.IsAnimationPlaying || NtfSmall.IsAnimationPlaying || ChaosBig.IsAnimationPlaying)
                {
                    if (!WaveCalc.IsRunning)
                    {
                        WaveCalc.Restart();
                    }
                }
                else
                {
                    WaveCalc.Stop();
                }
                if (ChaosBig.IsAnimationPlaying)
                {
                    var LeftTime = ChaosBig.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                    upLine = $"<size=22><color=#ffffc0cb><b>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#008000ff>🚗混沌</color></size>";
                }
                if (NtfBig.IsAnimationPlaying)
                {
                    var LeftTime = NtfBig.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                    upLine = $"<size=22><color=#ffffc0cb>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#0000ffff>🚁九尾狐</color></size>";
                }

                if (ChaosSmall.IsAnimationPlaying)
                {
                    var LeftTime = ChaosSmall.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                    upLine = $"<size=22><color=#ffffc0cb>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#008000ff>🚗混沌增援</color></size>";
                }
                if (NtfSmall.IsAnimationPlaying)
                {
                    var LeftTime = NtfSmall.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                    upLine = $"<size=22><color=#ffffc0cb>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#0000ffff>🚁九尾狐增援</color></size>";
                }

            }

            return new string[] { upLine };
        }
        public static void PreAuthenticating(PreAuthenticatingEventArgs ev)
        {
            var Pban = sql.QueryBan(ev.UserId);
            if (Pban != null)
            {
                Log.Info($"Pban {Pban}");
                bool thisServer = false;
                if (Pban?.port != "0")
                {
                    thisServer = Pban.Value.port == ServerStatic.ServerPort.ToString();
                }
                else
                {
                    thisServer = true;
                }
                if (thisServer)
                {
                    ev.Reject($"{Pban.Value.name}, 你在{Pban.Value.start_time.ToString("yyyy-MM-dd HH:mm:ss")}被封禁至{Pban.Value.end_time.ToString("yyyy-MM-dd HH:mm:ss")}\n原因:{Pban.Value.reason}\n处理人：{Pban.Value.issuer_name} \n如有疑问，请进群询问QQ：{Plugin.Instance.Config.QQgroup}", true);
                    Log.Info($"Kicking {ev.UserId} due find data in ban");
                    return;
                }
            }
        }
        public static void Verified(VerifiedEventArgs ev)
        {

            sql.Update(ev.Player.UserId, ev.Player.Nickname, last_time: DateTime.Now, ip: ev.Player.IPAddress);


            var PU = Plugin.Instance.connect.QueryUser(ev.Player.UserId);
            if (PU.uid == 0)
            {
                return;
            }
            if (PU.last_time.HasValue)
            {
                if (PU.last_time?.DayOfYear != DateTime.Now.DayOfYear || PU.last_time?.Year != DateTime.Now.Year)
                {
                    sql.Update(ev.Player.UserId, ev.Player.Nickname, today_duration: new TimeSpan(0));
                    AddExp(ev.Player, 25, true, AddExpReason.DayLogin);

                }

            }
            var PA = sql.QueryAdmin(userid: ev.Player.UserId);
            (string player_name, string port, string permissions, DateTime expiration_date, bool is_permanent, string notes)? target = null;
            if (PA != null)
            {

                if (PA.Count > 0)
                {
                    foreach (var item in PA)
                    {
                        if (item.port == ServerStatic.ServerPort.ToString() || item.port == "0")
                        {

                            target = item;
                            break;
                        }
                    }
                    if (target != null)
                    {
                        //ev.Player.Group = 
                        Log.Info($"get group:{target.Value.permissions} due AdminSystem");
                        var UserGroup = ServerStatic.PermissionsHandler.GetGroup(target.Value.permissions);
                        if (UserGroup != null)
                        {

                            if (ev.Player.Group == null || (ev.Player.Group != null && ev.Player.Group.KickPower < UserGroup.KickPower))
                            {
                                Log.Info($"player {ev.Player} get group:{UserGroup.Name} due AdminSystem");
                                ev.Player.Group = UserGroup.Clone();
                                ev.Player.RankName = $"({UserGroup.Name})";
                            }
                        }
                        else
                        {
                            Log.Info($"failed to get group! target:{target.Value.permissions}");
                        }
                    }
                }
            }
            var PB = sql.QueryBadge(userid: ev.Player.UserId);
            if (PB != null)
            {
                if (PB.Count > 0)
                {
                    foreach (var item in PB)
                    {
                        if (item.is_permanent || item.expiration_date <= DateTime.Now)
                        {
                            var text = item.badge;
                            if (target != null && target.HasValue)
                            {
                                Log.Info($"get group:{target.Value.permissions} due badgeSystem");
                                var UserGroup = ServerStatic.PermissionsHandler.GetGroup(target.Value.permissions);
                                if (UserGroup != null)
                                {
                                    text += $"({UserGroup.Name})";

                                }
                                else
                                {
                                    Log.Info($"failed to get group! target:{target.Value.permissions}");
                                }
                            }

                            List<string> colors = new List<string>();
                            item.color.Split(',').ForEach(c => colors.Add(c));
                            badges[ev.Player.UserId] = (item.player_name, text, colors, item.expiration_date, item.is_permanent, item.notes);
                            break;
                        }
                    }
                }
            }
            ev.Player.AddMessage("Always_InfoShow", PlayerHudLVShow, -1, ScreenLocation.ReversedForPlayerLVShow);

        }
        public enum AddExpReason
        {
            Custom,
            DayLogin,
            PeopleKillPeoPle,
            ScpKillPeoPle,
            KillZombie,
            killScp,
            DDSCIEscaped,
            GuardEscaped,
            CuffedPeopleEscaped,
            RoundEnd,
            ScpWin,
            HumanWin,
            RaAdded,
            Scp079Gener
        }

        public static Dictionary<string, (string player_name, string badge, List<string> color, DateTime expiration_date, bool is_permanent, string notes)> badges = new Dictionary<string, (string player_name, string badge, List<string> color, DateTime expiration_date, bool is_permanent, string notes)>();
        public static Dictionary<Player, int> levelCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> expCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> UidCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> PointCache = new Dictionary<Player, int>();
        public static Dictionary<Player, Stopwatch> TodayTimer = new Dictionary<Player, Stopwatch>();
        public static Dictionary<Player, TimeSpan> TodayTimeCache = new Dictionary<Player, TimeSpan>();
        public static ExpTier GetLevel(Player player)
        {
            return ExpToLevel(GetExperience(player));

        }
        public static int GetPoint(Player player)
        {
            if (player == null) return 0;
            if (PointCache.ContainsKey(player))
            {
                return PointCache[player];
            }
            var l = sql.QueryUser(player.UserId).point;
            PointCache[player] = l;
            return l;

        }
        public static TimeSpan GetTodayTimer(Player player)
        {
            if (player == null) return default;
            if (TodayTimer.ContainsKey(player))
            {
                if (TodayTimeCache.ContainsKey(player))
                {
                    return TodayTimer[player].Elapsed + TodayTimeCache[player];
                }
                else
                {
                    return TodayTimer[player].Elapsed;
                }
            }
            else
            {
                TodayTimer[player] = Stopwatch.StartNew();
                if (TodayTimeCache.ContainsKey(player))
                {
                    return TodayTimer[player].Elapsed + TodayTimeCache[player];
                }
                else
                {
                    var l = sql.QueryUser(player.UserId).today_duration;
                    if (l.HasValue)
                    {
                        TodayTimeCache[player] = l.Value;
                        return TodayTimer[player].Elapsed + l.Value;
                    }
                    else
                    {
                        return TimeSpan.Zero;
                    }
                }

            }

            //return l;

        }
        public static int GetUid(Player player)
        {
            if (player == null) return 0;
            if (player.IsNPC) return -1;
            if (UidCache.ContainsKey(player))
            {
                return UidCache[player];
            }
            var l = sql.QueryUser(player.UserId).uid;
            UidCache[player] = l;
            return l;

        }
        public static int GetExperience(Player player)
        {
            if (player == null) return 0;
            if (player.IsNPC) return -1;

            if (expCache.ContainsKey(player))
            {
                return expCache[player];
            }
            var l = sql.QueryUser(player.UserId).experience;
            expCache[player] = l;
            return l;

        }
        public static void AddExp(Player player, int exp, bool igronMul = false, AddExpReason reason = AddExpReason.Custom, string CustomReasonStr = "")
        {
            if (player == null || !player.IsConnected) return;
            if (global_experience_multiplier <= 0)
            {
                return;
            }
            if (player.IsNPC) return;

            AddPoint(player, exp);

            var pU = sql.QueryUser(player.UserId);
            int currentExp = GetExperience(player);
            int totalExp = (int)(currentExp + exp);
            // 玩家当前总经验 + 新增经验
            double experience_multiplier = Math.Max((double)1, pU.experience_multiplier.Value);
            if (!igronMul)
            {
                totalExp = (int)(currentExp + exp * experience_multiplier * global_experience_multiplier);
                player.AddMessage("ExpUpdated", $"<color=green><size=23>🔔获得经验:{(exp * experience_multiplier * global_experience_multiplier).ToString("F0")}</size></color>", 3f, ScreenLocation.CenterBottom);
            }
            else
            {
                player.AddMessage("ExpUpdated", $"<color=green><size=23>🔔获得经验:{(exp).ToString("F0")}</size></color>", 3f, ScreenLocation.CenterBottom);
            }

            string reasonStr = "";
            switch (reason)
            {
                default:
                case AddExpReason.Custom:
                    reasonStr = CustomReasonStr;
                    break;
                case AddExpReason.DayLogin:
                    reasonStr = "今日登录";
                    break;
                case AddExpReason.PeopleKillPeoPle:
                    reasonStr = "击杀人类";
                    break;
                case AddExpReason.ScpKillPeoPle:
                    reasonStr = "击杀人类";
                    break;
                case AddExpReason.KillZombie:
                    reasonStr = "击杀Scp049-2";
                    break;
                case AddExpReason.killScp:
                    reasonStr = "击杀SCP";
                    break;
                case AddExpReason.DDSCIEscaped:
                    reasonStr = "逃跑成功";
                    break;
                case AddExpReason.GuardEscaped:
                    reasonStr = "下班";
                    break;
                case AddExpReason.CuffedPeopleEscaped:
                    reasonStr = "捆绑的中立单位撤离";
                    break;
                case AddExpReason.RoundEnd:
                    reasonStr = "回合结束";
                    break;
                case AddExpReason.ScpWin:
                    reasonStr = "Scp获胜";
                    break;
                case AddExpReason.HumanWin:
                    reasonStr = "人类获胜";
                    break;
                case AddExpReason.RaAdded:
                    reasonStr = "管理指令";
                    break;
                case AddExpReason.Scp079Gener:
                    reasonStr = "阵营启动发电机";
                    break;
            }
            if (igronMul)
            {
                player.SendConsoleMessage($"你获得{(exp)} = {exp} * 1 * 1 原因:{reasonStr}", "grenn");
            }
            else
            {
                player.SendConsoleMessage($"你获得{(exp * experience_multiplier * global_experience_multiplier)} = {exp} * {pU.experience_multiplier} * {global_experience_multiplier} 原因:{reasonStr}", "grenn");
            }

            SetExp(player, totalExp);
            if (OnExpUp != null)
            {
                OnExpUp(player, totalExp);
            }
        }
        public delegate void onexpup(Player player, int NewExp);
        public static event onexpup OnExpUp;
        /*0-100为小份薯条，100-300为中份薯
条，300-800为大份薯条，800-1500为炸
锅，1500-3000为漏勺，3000-10000为吃薯
条，全服前三单服给称号*/
        public enum ExpTier
        {
            Small,//小份薯条
            Medium,//中份薯条
            Large,//大份薯条
            Pot,//炸锅
            Shao,//漏勺
            Eat,//吃薯条
            EatPlus,//吃薯条(>10000)
            Robot, // dummy专属 -1
        }
        public static ExpTier ExpToLevel(int currentExp)
        {
            if(currentExp == -1)
            {
                return ExpTier.Robot;
            }
            if (currentExp <= 100)
            {
                return ExpTier.Small;
            }
            else if (currentExp <= 300)
            {
                return ExpTier.Medium;
            }
            else if (currentExp <= 800)
            {
                return ExpTier.Large;
            }
            else if (currentExp <= 1500)
            {
                return ExpTier.Pot;
            }
            else if (currentExp <= 3000)
            {
                return ExpTier.Shao;
            }
            else if (currentExp <= 10000)
            {
                return ExpTier.Eat;
            }
            else
            {
                return ExpTier.EatPlus;
            }

        }
        public static int ExpToNextLevel(ExpTier currentLevel)
        {
            switch (currentLevel)
            {
                case ExpTier.Small:
                    return 100;
                    break;
                case ExpTier.Medium:
                    return 300;
                    break;
                case ExpTier.Large:
                    return 800;
                    break;
                case ExpTier.Pot:
                    return 1500;
                    break;
                case ExpTier.Shao:
                    return 3000;
                    break;
                case ExpTier.Eat:
                    return 10000;
                    break;
                case ExpTier.EatPlus:
                    return 0;
                case ExpTier.Robot:
                    return 0;
                default:
                    return 10000;
                    break;
            }
        }
        public static string LevelToName(ExpTier currentLevel)
        {
            switch (currentLevel)
            {
                case ExpTier.Small:
                    return "小份薯条";
                    break;
                case ExpTier.Medium:
                    return "中份薯条";
                    break;
                case ExpTier.Large:
                    return "大份薯条";
                    break;
                case ExpTier.Pot:
                    return "炸锅";
                    break;
                case ExpTier.Shao:
                    return "漏勺";
                    break;
                case ExpTier.EatPlus:
                case ExpTier.Eat:
                    return "吃薯条";
                case ExpTier.Robot:
                    return "人机";
                default:
                    return "?";
            }
        }
        public static void SetExp(Player player, int exp)
        {
            if (player == null) return;
            if (player.IsNPC) return;
            if (exp < 0) exp = 0;
            expCache[player] = exp;
            sql.Update(player.UserId, experience: exp);
        }
        public static void SetPoint(Player player, int point)
        {
            if (player == null) return;
            if (point < 0) point = 0;
            sql.Update(player.UserId, point: point);
            PointCache[player] = point;
        }
        public static void AddPoint(Player player, int point)
        {
            if (player == null) return;
            SetPoint(player, point: GetPoint(player) + point);
        }
        [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class BanCommand : ICommand, IUsageProvider
        {
            public string Command => "sban";

            public string[] Aliases => new string[] { "" };

            public string Description => "封禁玩家";

            public string[] Usage => new string[] { "userId/playerID", "time", "reason" };

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var runner = Player.Get(sender);
                if (runner == null)
                {
                    response = "failed to find player";
                    return false;
                }
                if (arguments.Count < 3)
                {
                    response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
                    return false;
                }
                string[] array;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
                if (list == null || list.Count <= 0)
                {
                    string targetUserID = arguments.At(0);
                    string text = string.Empty;
                    if (array.Length > 1)
                    {
                        text = array.Skip(1).Aggregate((string current, string n) => current + " " + n);
                    }
                    long num;
                    try
                    {
                        num = Misc.RelativeTimeToSeconds(array[0], 60);
                    }
                    catch
                    {
                        response = "Invalid time: " + array[0];
                        return false;
                    }
                    if (num < 0L)
                    {
                        num = 0L;
                        array[0] = "0";
                    }
                    if (!sender.CheckPermission(new PlayerPermissions[]
                    {
                PlayerPermissions.KickingAndShortTermBanning,
                PlayerPermissions.BanningUpToDay,
                PlayerPermissions.LongTermBanning
                    }, out response))
                    {
                        return false;
                    }
                    ushort num2 = 0;
                    ushort num3 = 0;
                    string text2 = string.Empty;
                    {
                        try
                        {


                            {
                                string combinedName = targetUserID;
                                CommandSender commandSender = sender as CommandSender;
                                ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[]
                                {
                            sender.LogName,
                            " banned player ",
                            targetUserID,
                            ". Ban duration: ",
                            array[0],
                            ". Reason: ",
                            (text == string.Empty) ? "(none)" : text,
                            "."
                                }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
                                sql.InsertBanRecord(targetUserID, targetUserID, runner.UserId, runner.Nickname, text, DateTime.Now, end_time: DateTime.Now.AddSeconds(num), ServerStatic.ServerPort.ToString());
                                foreach (var item in Player.Enumerable)
                                {
                                    if (item.UserId == targetUserID)
                                    {
                                        item.Kick(text);
                                    }
                                }

                                num2 += 1;
                                response = "Done! " + string.Concat(new string[]
                                {
                            sender.LogName,
                            " banned player ",
                            targetUserID,
                            ". Ban duration: ",
                            array[0],
                            ". Reason: ",
                            (text == string.Empty) ? "(none)" : text,
                            "."
                                });
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            num3 += 1;
                            Log.Debug(ex);
                            text2 = "Error occured during banning: " + ex.Message + ".\n" + ex.StackTrace;
                        }
                    }
                    if (num3 == 0)
                    {
                        string arg = "Banned";
                        int num4;
                        if (int.TryParse(array[0], out num4))
                        {
                            arg = ((num4 > 0) ? "Banned" : "Kicked");
                        }
                        response = string.Format("Done! {0} {1} player{2}", arg, num2, (num2 == 1) ? "!" : "s!");
                        return true;
                    }
                    response = string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n{1}", num3, text2);
                    return false;
                }
                else
                {
                    if (array == null)
                    {
                        response = "An error occured while processing this command.\nUsage: " + this.DisplayCommandUsage();
                        return false;
                    }
                    string text = string.Empty;
                    if (array.Length > 1)
                    {
                        text = array.Skip(1).Aggregate((string current, string n) => current + " " + n);
                    }
                    long num;
                    try
                    {
                        num = Misc.RelativeTimeToSeconds(array[0], 60);
                    }
                    catch
                    {
                        response = "Invalid time: " + array[0];
                        return false;
                    }
                    if (num < 0L)
                    {
                        num = 0L;
                        array[0] = "0";
                    }
                    if (!sender.CheckPermission(new PlayerPermissions[]
                    {
                PlayerPermissions.KickingAndShortTermBanning,
                PlayerPermissions.BanningUpToDay,
                PlayerPermissions.LongTermBanning
                    }, out response))
                    {
                        return false;
                    }
                    ushort num2 = 0;
                    ushort num3 = 0;
                    string text2 = string.Empty;
                    foreach (ReferenceHub referenceHub in list)
                    {
                        try
                        {
                            if (referenceHub == null)
                            {
                                num3 += 1;
                            }
                            else
                            {
                                string combinedName = referenceHub.nicknameSync.CombinedName;
                                CommandSender commandSender = sender as CommandSender;
                                ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[]
                                {
                            sender.LogName,
                            " banned player ",
                            referenceHub.LoggedNameFromRefHub(),
                            ". Ban duration: ",
                            array[0],
                            ". Reason: ",
                            (text == string.Empty) ? "(none)" : text,
                            "."
                                }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
                                sql.InsertBanRecord(referenceHub.authManager.UserId, referenceHub.nicknameSync.MyNick, runner.UserId, runner.Nickname, text, DateTime.Now, end_time: DateTime.Now.AddSeconds(num), ServerStatic.ServerPort.ToString());
                                BanPlayer.KickUser(referenceHub, sender, text);

                                num2 += 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            num3 += 1;
                            Log.Debug(ex);
                            text2 = "Error occured during banning: " + ex.Message + ".\n" + ex.StackTrace;
                        }
                    }
                    if (num3 == 0)
                    {
                        string arg = "Banned";
                        int num4;
                        if (int.TryParse(array[0], out num4))
                        {
                            arg = ((num4 > 0) ? "Banned" : "Kicked");
                        }
                        response = string.Format("Done! {0} {1} player{2}", arg, num2, (num2 == 1) ? "!" : "s!");
                        return true;
                    }
                    response = string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n{1}", num3, text2);
                    return false;
                }
            }
        }
        [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class cbanCommand : ICommand
        {
            public string Command => "cban";

            public string[] Aliases => new string[0] { };

            public string Description => "查询封禁记录";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var runner = Player.Get(sender);
                if (runner == null)
                {
                    response = "failed to find player";
                    return false;
                }
                if (arguments.Count == 0)
                {
                    response = "空空如也";
                    return false;
                }
                response = "Done!";
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out _, false);
                if (list == null || list.Count <= 0)
                {
                    string targetUserID = arguments.At(0);
                    string text = string.Empty;
                    var Pbans = sql.QueryAllBan(targetUserID);

                    if (Pbans != null)
                    {
                        foreach (var arg in Pbans)
                        {
                            response += $"{arg.start_time} 到 {arg.end_time} by:{arg.issuer_name} reason:{arg.reason} \n";
                        }
                    }
                    return true;
                }
                else
                {
                    //if (array == null)
                    if (!sender.CheckPermission(new PlayerPermissions[]
                    {
                PlayerPermissions.KickingAndShortTermBanning,
                PlayerPermissions.BanningUpToDay,
                PlayerPermissions.LongTermBanning
                    }, out response))
                    {
                        return false;
                    }
                    var target = list[0];
                    if (target == null)
                    {
                        response = "Fialed To get target";
                        return false;
                    }
                    var Pbans = sql.QueryAllBan(target.authManager.UserId);

                    if (Pbans != null)
                    {
                        foreach (var arg in Pbans)
                        {
                            response += $"{arg.start_time} 到 {arg.end_time} by:{arg.issuer_name} reason:{arg.reason} \n";
                        }
                    }
                    return true;
                }
                return true;
            }
        }
        [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class ExpxCommand : ICommand
        {
            public string Command => "expx";

            public string[] Aliases => new string[0] { };

            public string Description => "修改倍率";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var s = Player.Get(sender);
                if (s == null)
                {
                    response = "failed to find player";
                    return false;
                }
                if (s.KickPower < 244)
                {
                    response = "KickPower 小于 244 !";
                    return false;
                }
                if (arguments.Count == 0)
                {
                    response = "空空如也";
                    return false;
                }
                double g = double.Parse(arguments.At(0));
                global_experience_multiplier = g;
                response = "Done!";
                return true;
            }
        }
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        class AddExpCommand : ICommand, IUsageProvider
        {
            public string[] Usage { get; } = new[] { "playerID", "exp" };

            string ICommand.Command { get; } = "AddPlayerExp";

            string[] ICommand.Aliases { get; } = new[] { "" };

            string ICommand.Description { get; } = "添加经验";

            bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var runner = Player.Get(sender);
                if (runner.KickPower < 244)
                {
                    response = "KickPower 小于 244 !";
                    return false;
                }
                List<ReferenceHub> list = new List<ReferenceHub>();
                if (arguments.Count >= 2)
                {
                    list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out var newargs);
                    if (list == null)
                    {
                        response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                        return false;
                    }
                    var exp = int.Parse(newargs[0]);
                    foreach (var item in list)
                    {
                        AddExp(Player.Get(item), exp, true, reason: AddExpReason.RaAdded);
                    }
                    response = $"done added {exp}!";

                    return true;
                }
                else
                {
                    response = "To execute this command provide at least 2 arguments!";
                    return false;
                }


            }
        }
    }
}

