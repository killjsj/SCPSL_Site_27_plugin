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
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp079;
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
            Exiled.Events.Handlers.Player.Died += PlayerManager.Died;
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
            base.Init();
        }
        public static CoroutineHandle rec;
        public override void Delete()
        {

            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            Exiled.Events.Handlers.Player.Shot -= Shot;
            Plugin.MenuCache.RemoveAll(x => x.Id == Plugin.Instance.Config.SettingIds[Features.LevelHeader] || x.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]);
            Plugin.MenuCache.RemoveAll(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk]);
            Exiled.Events.Handlers.Warhead.Starting -= PlayerManager.Starting;
            Exiled.Events.Handlers.Player.DroppingAmmo -= PlayerManager.DroppedAmmo;
            Exiled.Events.Handlers.Player.Verified -= PlayerManager.Verified;
            Exiled.Events.Handlers.Player.PreAuthenticating -= PlayerManager.PreAuthenticating;
            Exiled.Events.Handlers.Server.RestartingRound -= PlayerManager.RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= PlayerManager.WaitingForPlayers;
            Exiled.Events.Handlers.Player.Hurting -= PlayerManager.Hurting;
            Exiled.Events.Handlers.Player.Left -= PlayerManager.Left;
            Exiled.Events.Handlers.Warhead.Stopping -= PlayerManager.Stopping;
            Exiled.Events.Handlers.Player.Died -= PlayerManager.Died;
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
            } else if (!Room.TryGetRoomAtPosition( ev.PlayerDisplay.ReferenceHub.GetPosition(),out var r))
            {
                    ev.PlayerDisplay.RemoveHint(ev.Hint);
                return "";
            }
            else if(r.Base.Name != MapGeneration.RoomName.Lcz914)
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
                    if (GetLevel(item) > 10)
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
                if (GetLevel(item) > 10)
                {
                    AddExp(item, 5, reason: AddExpReason.RoundEnd);
                    if (ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.Anomalies || (Scp5k_Control.Is5kRound && ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces))
                    {
                        if (item.Role.Type.IsScp())
                        {
                            AddExp(item, 15, reason: AddExpReason.ScpWin);
                        }
                    }
                    if (ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces ||
                        ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.ChaosInsurgency)
                    {
                        if (item.Role.Type.IsHuman())
                        {
                            AddExp(item, 15, reason: AddExpReason.HumanWin);
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
            if (Scp5k_Control.Is5kRound) {
                ev.Player.Role.Set(RoleTypeId.ChaosConscript);
            }
        }
        public static void Died(DiedEventArgs ev)
        {
            if (ev.Attacker != null)
            {
                if (!ev.Attacker.IsScp)
                {
                    if (!ev.TargetOldRole.IsScp())
                    {
                        AddExp(ev.Attacker, 5, true, AddExpReason.PeopleKillPeoPle);
                    }
                    else
                    {
                        if (ev.TargetOldRole == RoleTypeId.Scp0492)
                        {
                            AddExp(ev.Attacker, 15, true, AddExpReason.KillZombie);
                        }
                        else
                        {
                            AddExp(ev.Attacker, 40, false, AddExpReason.killScp);

                        }

                    }
                }
                else
                {
                    AddExp(ev.Attacker, 5, true, AddExpReason.ScpKillPeoPle);

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
            TodayTimeCache.Clear();
            foreach (var item in TodayTimer.Values)
            {
                item.Stop();
            }
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
            if (Plugin.Instance.Config.Level)
            {

                settings.Add(new HeaderSetting(Plugin.Instance.Config.SettingIds[Features.LevelHeader], "等级插件"));

                settings.Add(new ButtonSetting(Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey], "一键开关核", "开核", 0.2f, "(Scp079 设施等级为5 且 游戏等级大于211级)\n使用后消耗全部电力开关核，使用后18秒内不会回复电力值",
                    onChanged: (player, SB) =>
                    {
                        try
                        {

                            var PU = Plugin.Instance.connect.QueryUser(player.UserId);
                            var lv = PU.level;
                            if (player.Role is Scp079Role SR)
                            {
                                if (SR.Level == 5)
                                {
                                    if (lv >= 211)
                                    {
                                        if (SR.Energy >= SR.MaxEnergy - 2)
                                        {

                                            if (AlphaWarheadController.Singleton.IsLocked)
                                            {
                                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>核弹已锁定!</color>", 3), true);
                                                return;
                                            }
                                            if (AlphaWarheadController.Singleton.Info.InProgress)
                                            {
                                                SR.Energy = 0;
                                                Plugin.RunCoroutine(ClearPower(SR));
                                                AlphaWarheadController.Singleton.CancelDetonation(player.ReferenceHub);
                                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>核弹已取消!</color>", 3), true);
                                                return;
                                            }
                                            else
                                            {
                                                if (!AlphaWarheadNukesitePanel.Singleton.Networkenabled)
                                                {
                                                    player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>拉杆未拉下!</color>", 3), true);
                                                    return;
                                                }
                                                if (!AlphaWarheadActivationPanel.IsUnlocked)
                                                {
                                                    player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>地表未开盖!</color>", 3), true);
                                                    return;
                                                }
                                                SR.Energy = 0;
                                                Plugin.RunCoroutine(ClearPower(SR));
                                                AlphaWarheadController.Singleton.InstantPrepare();
                                                AlphaWarheadController.Singleton.StartDetonation(false, false, player.ReferenceHub);
                                                AlphaWarheadController.Singleton.IsLocked = false;
                                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>核弹已开始!</color>", 3), true);
                                                return;
                                            }

                                        }
                                        else
                                        {
                                            player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>电力不足！</color>", 3), true);
                                        }
                                    }
                                    else
                                    {
                                        player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>你没到211级！</color>", 3), true);
                                    }
                                }
                                else
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>设施等级不足5级！</color>", 3), true);
                                }
                            }
                            else
                            {
                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>你不是SCP079！</color>", 3), true);
                            }
                        }
                        catch (Exception ex)
                        {
                            Exiled.API.Features.Log.Error(ex.ToString());

                        }
                    }));
            }
            return settings;
        }
        public static void Stopping(StoppingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
            {
                Timing.CallDelayed(0.2f, () =>
                {
                    var nuke = Plugin.MenuCache.Find(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]) as ButtonSetting;
                    var Text = Exiled.API.Features.Warhead.IsInProgress ? "关核" : "开核";
                    nuke.UpdateSetting(Text, 0.2f, filter: (p) => p.Role.Type == RoleTypeId.Scp079);

                });
            }
        }
        public static void Starting(StartingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
            {
                Timing.CallDelayed(0.2f, () =>
                {
                    var nuke = Plugin.MenuCache.Find(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]) as ButtonSetting;
                    var Text = Exiled.API.Features.Warhead.IsInProgress ? "关核" : "开核";
                    nuke.UpdateSetting(Text, 0.2f, filter: (p) => p.Role.Type == RoleTypeId.Scp079);
                });
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


            

            
                Timing.CallDelayed(0.4f, () =>
            {
                try
                {
                    if (ev.Player == null) return;
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
                        if (Plugin.GetPlayerRegistered(ev.Player).Any(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]))
                        {
                            //if (menuItems != null && menuItems.Count > 0)
                            {
                                Plugin.Unregister(ev.Player, Plugin.MenuCache.Where((a) => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]));

                            }
                        }
                        var CandyList = new List<CandyKindID>()
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
        var player = ev.Player;
                        //var PU = Plugin.nav.connect.QueryUser(player.UserId);
                        var level = GetLevel(player);
                        var bufList = new List<EffectType>()
            {
                EffectType.MovementBoost,// 20
                EffectType.DamageReduction,// 50
                EffectType.SilentWalk, // 7
            };

                        Random.InitState(level + DateTime.UtcNow.Second + DateTime.UtcNow.Minute * 60 + DateTime.UtcNow.DayOfYear + DateTime.Now.Hour);
                        if (level >= 1 && level <= 10)
                        {
                            if (player.Role.Type == RoleTypeId.ClassD)
                            {
                                if (Random.Range(0, 100) < 50)
                                    player.AddItem(ItemType.KeycardJanitor, 1);
                            }
                            player.AddItem(ItemType.Painkillers, 2);
                        }

                        // ====== 11-20级 ======
                        if (level >= 11 && level <= 20)
                        {
                            if (player.Role.Type == RoleTypeId.ClassD)
                                player.AddItem(ItemType.KeycardJanitor, 1);

                            player.AddItem(ItemType.Medkit, 1);
                        }

                        // ====== 21-30级 ======
                        if (level >= 21)
                        {
                            if (player.Role.Type == RoleTypeId.ClassD)
                                player.AddItem(ItemType.KeycardJanitor, 1);

                            player.AddItem(ItemType.Medkit, 1);

                            if (player.Role.Type == RoleTypeId.ClassD || player.Role.Type == RoleTypeId.Scientist)
                            {
                                player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                            }
                        }//dd:2

                        // ====== 41-50级 ======
                        if (level >= 41)
                        {
                            if (player.Role.Type == RoleTypeId.ClassD || player.Role.Type == RoleTypeId.Scientist)
                            {
                                player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                            }
                            if (player.Role.Type == RoleTypeId.Scientist && Random.Range(0, 100) < 25)
                                player.AddItem(ItemType.KeycardResearchCoordinator, 1); // 黄卡
                        }

                        // ====== 51-60级 ======
                        if (level >= 51)
                        {
                            if (player.Role.Type == RoleTypeId.FacilityGuard && Random.Range(0, 100) < 40)
                            {
                                player.AddItem(ItemType.KeycardResearchCoordinator, 1); // 黄卡

                            }
                            if (level >= 61)
                            {
                                if (player.Role.Type == RoleTypeId.FacilityGuard)
                                {
                                    if (Random.Range(0, 100) < 75)
                                        player.AddItem(ItemType.KeycardScientist, 1);

                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                }

                            }
                            if (level >= 71)
                            {
                                if (player.Role.Type == RoleTypeId.FacilityGuard)
                                {
                                    player.AddItem(ItemType.KeycardScientist, 1);
                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                }
                            }
                            if (player.Role.Type == RoleTypeId.ClassD || player.Role.Type == RoleTypeId.Scientist)
                            {
                                player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                            }
                        }

                        // ====== 81-90级 ======
                        if (level >= 81)
                        {
                            if (player.Role.Type == RoleTypeId.ClassD || player.Role.Type == RoleTypeId.Scientist)

                            {
                                player.AddItem(ItemType.ArmorLight, 1);
                            }//3
                        }

                        // ====== 91-99级 ======
                        if (level >= 91 && level <= 99)
                        {

                            if (player.Role.Type == RoleTypeId.FacilityGuard)
                            {
                                // 安保人员替换为轻甲
                                player.AddItem(ItemType.ArmorLight, 1);
                            }
                        }
                        if (Scp5k_Control.Is5kRound) return;
                        if (level >= 101)
                        {
                            switch (player.RoleManager.CurrentRole.RoleTypeId)
                            {
                                case RoleTypeId.ClassD:
                                case RoleTypeId.Scientist:
                                    {
                                        //Log.Debug($"ClassD/Scientist {player} Level 101,processing");
                                        if (Random.Range(0, 100) >= 50)
                                        {
                                            //Log.Debug($"ClassD/Scientist {player} Level 101,AddItem fl");
                                            player.AddItem(ItemType.Flashlight, 1);
                                        }
                                        else if(Random.Range(0, 100) < 45)
                                        {
                                            player.AddItem(ItemType.KeycardZoneManager, 1); // 绿卡

                                        }
                                        player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                        player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                        player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                        break;
                                    }
                                case RoleTypeId.FacilityGuard:
                                    {
                                        //Log.Debug($"FacilityGuard {player} Level 101,processing");
                                        int extraHealth = Math.Min((level - 101) / 2, 15);
                                        player.MaxHealth += extraHealth;
                                        player.EnableEffect(EffectType.MovementBoost, 24, 12f, false);
                                        break;
                                    }
                            }
                        }

                        if (level >= 131)
                        {
                            switch (player.RoleManager.CurrentRole.RoleTypeId)
                            {
                                case RoleTypeId.Scp049:
                                    {
                                        //Log.Debug($"049 {player} Level 131,processing");
                                        int extraHealth = Math.Min((level - 131) * 4, 180);
                                        int extraShield = Math.Min((level - 131) * 2, 90);
                                        player.MaxHealth += extraHealth;
                                        IHumeShieldedRole healthbarRole = player.RoleManager.CurrentRole as IHumeShieldedRole;
                                        player.MaxHumeShield = healthbarRole.HumeShieldModule.HsMax + extraShield;
                                        //player.ArtificialHealth = player.MaxArtificialHealth;
                                        break;
                                    }
                                case RoleTypeId.Scp0492:
                                    {
                                        //Log.Debug($"Scp0492 {player} Level 131,processing");
                                        int extraHealth = Math.Min((level - 131) * 4, 180);
                                        player.MaxHealth += extraHealth;
                                        break;
                                    }
                                case RoleTypeId.ClassD:
                                case RoleTypeId.Scientist:
                                    {
                                        //Log.Debug($"ClassD/Scientist {player} Level 131,processing");
                                        if (Random.Range(0, 100) >= 30)
                                        {
                                            player.AddItem(ItemType.SCP500, 1);
                                        }
                                        else
                                        {
                                            player.AddItem(ItemType.Medkit, 2);
                                        }
                                        break;
                                    }
                            }
                        }
                        if (level >= 176)
                        {
                            switch (player.RoleManager.CurrentRole.RoleTypeId)
                            {
                                case RoleTypeId.Scp173:
                                    {
                                        //Log.Debug($"173 {player} Level 176,processing");
                                        int extraHealth = Math.Min((level - 176) * 6, 210);
                                        player.MaxHealth += extraHealth;
                                        break;
                                    }
                                case RoleTypeId.NtfCaptain:
                                case RoleTypeId.NtfPrivate:
                                case RoleTypeId.NtfSpecialist:
                                case RoleTypeId.NtfSergeant:
                                    {
                                        //Log.Debug($"NTF {player} Level 176,processing");
                                        int extraHealth = 0;
                                        if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfCaptain) extraHealth = 12;
                                        else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfPrivate) extraHealth = 7;
                                        else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfSpecialist) extraHealth = 9;
                                        else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfSergeant) extraHealth = 9;
                                        player.MaxHealth += extraHealth;
                                        player.AddItem(ItemType.Painkillers, 1);
                                        break;
                                    }
                                case RoleTypeId.ChaosConscript:
                                case RoleTypeId.ChaosMarauder:
                                case RoleTypeId.ChaosRepressor:
                                case RoleTypeId.ChaosRifleman:
                                    {
                                        //Log.Debug($"Chaos {player} Level 176,processing");
                                        int extraHealth = 0;
                                        if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosRepressor) extraHealth = 12;
                                        else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosRifleman) extraHealth = 7;
                                        else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosMarauder) extraHealth = 9;
                                        else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosConscript) extraHealth = 9;
                                        player.MaxHealth += extraHealth;
                                        player.AddItem(ItemType.Painkillers, 1);
                                        break;
                                    }
                            }
                        }
                        if (level >= 211)
                        {
                            //Log.Debug(player.RoleManager.CurrentRole.RoleTypeId.ToString() + $" {player} Level 211,processing");

                            switch (player.RoleManager.CurrentRole.RoleTypeId)
                            {
                                case RoleTypeId.FacilityGuard:
                                    {

                                        switch (bufList.RandomItem())
                                        {
                                            case EffectType.MovementBoost:
                                                player.EnableEffect(EffectType.MovementBoost, 20, 99999f, false);
                                                Log.Debug($"{player} get MovementBoost");
                                                break;
                                            case EffectType.DamageReduction:
                                                Log.Debug($"{player} get DamageReduction");
                                                player.EnableEffect(EffectType.DamageReduction, 50, 99999f, false);
                                                break;
                                            case EffectType.SilentWalk:
                                                Log.Debug($"{player} get SilentWalk");
                                                player.EnableEffect(EffectType.SilentWalk, 7, 99999f, false);
                                                break;
                                        }
                                        break;
                                    }
                                case RoleTypeId.Scp106:
                                    {

                                        break;
                                    }
                                case RoleTypeId.Scp079:
                                    {
                                        var r = player.Role as Scp079Role;

                                        FieldInfo field = typeof(Scp079DoorLockChanger).GetField("_lockCostPerSec", BindingFlags.NonPublic | BindingFlags.Instance);
                                        float original = (float)field.GetValue(r.DoorLockChanger);
                                        float adjusted = level >= 220 ? original * 0.75f : original;
                                        field.SetValue(r.DoorLockChanger, adjusted);
                                        Plugin.Unregister(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]));
                                        Plugin.Register(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.LevelHeader]));
                                        break;
                                    }
                            }
                        }
                        if (level >= 241)
                        {
                            //Log.Debug(player.RoleManager.CurrentRole.RoleTypeId.ToString() + $" {player} Level 241,processing");
                            switch (player.RoleManager.CurrentRole.RoleTypeId)
                            {
                                case RoleTypeId.FacilityGuard:
                                    {
                                        if (Random.Range(0, 100) < 35)
                                        {
                                            player.AddItem(ItemType.SCP2176, 1);
                                            Log.Debug($"{player} get SCP2176");
                                        }
                                        if (Random.Range(0, 100) < 2)
                                        {
                                            Log.Debug($"{player} get SurfaceAccessPass");
                                            player.AddItem(ItemType.SurfaceAccessPass, 1);
                                        }
                                        break;
                                    }
                                case RoleTypeId.Scp096:
                                case RoleTypeId.Scp939:
                                    {
                                        int extraShield = Math.Min((level - 241) * 7, 210);
                                        IHumeShieldedRole healthbarRole = player.RoleManager.CurrentRole as IHumeShieldedRole;
                                        player.MaxHumeShield = healthbarRole.HumeShieldModule.HsMax + extraShield;
                                        break;
                                    }
                            }
                        }
                        if (level >= 270)
                        {
                            //Log.Debug(player.RoleManager.CurrentRole.RoleTypeId.ToString() + $" {player} Level 270,processing");
                            switch (player.RoleManager.CurrentRole.RoleTypeId)
                            {
                                case RoleTypeId.ClassD:
                                case RoleTypeId.Scientist:
                                    {
                                        if (Random.Range(0, 100) < 15)
                                        {
                                            player.AddItem(ItemType.SCP207, 1);
                                            Log.Debug($"{player} get SCP207");
                                            if (Random.Range(0, 100) < 2)
                                            {
                                                Log.Debug($"{player} get double SCP207");
                                                player.AddItem(ItemType.SCP207, 1);
                                            }
                                        }
                                        break;
                                    }
                            }
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

            // 经验奖励
            if (GetLevel(player) > 10)
            {
                AddExp(player, 10, false, AddExpReason.GuardEscaped);
            }
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
                if (badgeData.color.Contains( "rainbow"))
                {
                    if (!rainbowC.ContainsKey(player))
                    {
                        rainbowC[player] = Timing.RunCoroutine(rainbowTime(player,colors));
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
            int level = GetLevel(player);
            string expectedName = $"Lv.{level} | {player.Nickname}";

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
        public static IEnumerator<float> rainbowTime(Player player,List<string> colors)
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

            string upLine = "";
            string downLine = "";
            if (player.Role is SpectatorRole SR)
            {
                if (SR.SpectatedPlayer != null)
                {
                    if (SpecList.ContainsKey(SR.SpectatedPlayer))
                    {
                        SpecCount = SpecList[SR.SpectatedPlayer].Count;
                    }
                    upLine = $"<align=center><size=25><color=green>Lv.{GetLevel(SR.SpectatedPlayer)}</color>  |  <color=green>{GetExperience(SR.SpectatedPlayer)}/{GetExpToNextLevel(GetLevel(SR.SpectatedPlayer))}</color>  |  称号: <color=white>{(string.IsNullOrEmpty(SR.SpectatedPlayer.RankName) ? "无" : SR.SpectatedPlayer.RankName)}</color></size></align>";
                    if (SR.SpectatedPlayer.UniqueRole != "")
                    {
                        var showing = "";
                        CustomRole role = CustomRole.Get(SR.SpectatedPlayer.UniqueRole);
                            showing = role.Name;
                        
                        downLine = $"<align=center><size=25><color=green>UID:{GetUid(SR.SpectatedPlayer)}</color> | <color=yellow>{SR.SpectatedPlayer.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {GetTodayTimer(SR.SpectatedPlayer).Hours.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Minutes.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Seconds.ToString("D2")}</color> | <color=yellow>扮演: {showing}</color>  | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                    } else
                    {
                        downLine = $"<align=center><size=25><color=green>UID:{GetUid(SR.SpectatedPlayer)}</color> | <color=yellow>{SR.SpectatedPlayer.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {GetTodayTimer(SR.SpectatedPlayer).Hours.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Minutes.ToString("D2")}:{GetTodayTimer(SR.SpectatedPlayer).Seconds.ToString("D2")}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                    }
                    if (Misc.TryParseColor(SR.SpectatedPlayer.RankColor, out var color))
                    {
                        upLine = $"<align=center><size=25><color=green>Lv.{GetLevel(SR.SpectatedPlayer)}</color>  |  <color=green>{GetExperience(SR.SpectatedPlayer)}/{GetExpToNextLevel(GetLevel(SR.SpectatedPlayer))}</color>  |  称号: <color={color.ToHex()}>{(string.IsNullOrEmpty(SR.SpectatedPlayer.RankName) ? "无" : SR.SpectatedPlayer.RankName)}</color></size></align>";
                    }

                }
                else
                {
                    upLine = $"<align=center><size=25><color=green>Lv.{GetLevel(player)}</color>  |  <color=green>{GetExperience(player)}/{GetExpToNextLevel(GetLevel(player))}</color>  |  称号: <color=white>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)}</color></size></align>";
                    downLine = $"<align=center><size=25><color=green>UID:{GetUid(player)}</color> | <color=yellow>尊敬的 {player.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {p.Hours.ToString("D2")}:{p.Minutes.ToString("D2")}:{p.Seconds.ToString("D2")}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                    if (Misc.TryParseColor(player.RankColor, out var color))
                    {
                        upLine = $"<align=center><size=25><color=green>Lv.{GetLevel(player)}</color>  |  <color=green>{GetExperience(player)}/{GetExpToNextLevel(GetLevel(player))}</color>  |  称号: <color={color.ToHex()}>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)} </color></size></align></width>";
                    }
                }

            }
            else
            {
                if (SpecList.ContainsKey(player))
                {
                    SpecCount = SpecList[player].Count;
                }
                upLine = $"<align=center><size=25><color=green>Lv.{GetLevel(player)}</color>  |  <color=green>{GetExperience(player)}/{GetExpToNextLevel(GetLevel(player))}</color>  |  称号: <color=white>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)}</color></size></align>";
                downLine = $"<align=center><size=25><color=green>UID:{GetUid(player)}</color> | <color=yellow>尊敬的 {player.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {p.Hours.ToString("D2")}:{p.Minutes.ToString("D2")}:{p.Seconds.ToString("D2")}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
                if (Misc.TryParseColor(player.RankColor, out var color))
                {
                    upLine = $"<align=center><size=25><color=green>Lv.{GetLevel(player)}</color>  |  <color=green>{GetExperience(player)}/{GetExpToNextLevel(GetLevel(player))}</color>  |  称号: <color={color.ToHex()}>{(string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName)} </color></size></align>";
                }
                if (player.UniqueRole != "")
                {
                    var showing = "";
                    CustomRole role = CustomRole.Get(player.UniqueRole);
                        showing = role.Name;
                        downLine = $"<align=center><size=25><color=green>UID:{GetUid(player)}</color> | <color=yellow>尊敬的 {player.Nickname} {GetGreetingWord()}</color>| <color=#00ffffff>今日时长: {p.Hours.ToString("D2")}:{p.Minutes.ToString("D2")}:{p.Seconds.ToString("D2")}</color> | <color=yellow>你是: {showing}</color> | <color=#add8e6ff>观众:{SpecCount}</color></size>";
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

            sql.Update(ev.Player.UserId, ev.Player.Nickname, last_time: DateTime.Now,ip:ev.Player.IPAddress);

            
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
                            badges[ev.Player.UserId] = (item.player_name, text, colors, item.expiration_date,item.is_permanent,item.notes);
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

        public static Dictionary<string, (string player_name, string badge, List<string> color, DateTime expiration_date, bool is_permanent, string notes)> badges = new Dictionary<string, (string player_name, string badge, List< string> color, DateTime expiration_date, bool is_permanent, string notes)>();
        public static void AddLevel(Player player, int level)
        {
            var pU = sql.QueryUser(player.UserId);
            SetLevel(player, pU.level + level);
        }
        public static Dictionary<Player, int> levelCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> expCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> UidCache = new Dictionary<Player, int>();
        public static Dictionary<Player, Stopwatch> TodayTimer = new Dictionary<Player, Stopwatch>();
        public static Dictionary<Player, TimeSpan> TodayTimeCache = new Dictionary<Player, TimeSpan>();
        public static int GetLevel(Player player)
        {
            if (levelCache.ContainsKey(player))
            {

                return levelCache[player];
            }
            var l = sql.QueryUser(player.UserId).level;
            levelCache[player] = l;
            return l;

        }
        public static TimeSpan GetTodayTimer(Player player)
        {
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
            if (expCache.ContainsKey(player))
            {
                return expCache[player];
            }
            var l = sql.QueryUser(player.UserId).experience;
            expCache[player] = l;
            return l;

        }
        public static void SetLevel(Player player, int level)
        {
            levelCache[player] = level;

            sql.Update(player.UserId, level: level);
        }
        public static void AddExp(Player player, int exp, bool igronMul = false, AddExpReason reason = AddExpReason.Custom, string CustomReasonStr = "")
        {
            if (player == null || !player.IsConnected) return;
            if (exp <= 0 || global_experience_multiplier <= 0)
            {
                return;
            }
            var pU = sql.QueryUser(player.UserId);
            int currentLevel = GetLevel(player);
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
            // 逐步升级，直到无法升级或达到100级
            while (true)
            {
                int expToNextLevel = GetExpToNextLevel(currentLevel);

                if (totalExp >= expToNextLevel)
                {
                    currentLevel++;
                    totalExp -= expToNextLevel; // 扣除升级消耗的经验

                    // 可选：广播升级消息
                    //player.ShowHint($"<size=20><b>🎉 恭喜升级！</b> 您已升到 <color=yellow>等级 {currentLevel}</color>！</size>", 5);
                    player.AddMessage("LevelUpdated", $"<align=center><color=yellow><size=22>👏 Lv{pU.level}->Lv{currentLevel}</size></color></color>", 3f, ScreenLocation.CenterBottom);

                    Log.Info($"{player.Nickname} 升级到了 {currentLevel} 级！");
                }
                else
                {
                    break; // 经验不足，停止升级
                }
            }

            // 保存最终等级和剩余经验
            SetLevel(player, level: currentLevel);
            SetExp(player, totalExp);
            if (OnLevelUp != null)
            {
                OnLevelUp(player, pU.level, currentLevel);
            }
        }
        public delegate void onlevelup(Player player, int level, int currentLevel);
        public static event onlevelup OnLevelUp;
        /// <summary>
        /// 获取从当前等级升到下一级所需的经验值
        /// </summary>
        /// <param name="currentLevel">当前等级（1-99）</param>
        /// <returns>升级所需经验；若 >=100 返回 0</returns>
        /// <summary>
        /// 获取从当前等级升到下一级所需的经验值（兼容 C# 7.3）
        /// </summary>
        /// <param name="currentLevel">当前等级（1-99）</param>
        /// <returns>升级所需经验；若 >=100 返回 0</returns>
        public static int GetExpToNextLevel(int currentLevel)
        {
            if (currentLevel < 1) return 100;

            if (currentLevel >= 1 && currentLevel <= 10)
                return 100;
            else if (currentLevel >= 11 && currentLevel <= 20)
                return 250;
            else if (currentLevel >= 21 && currentLevel <= 30)
                return 350;
            else if (currentLevel >= 31 && currentLevel <= 40)
                return 500;
            else if (currentLevel >= 41 && currentLevel <= 50)
                return 750;
            else if (currentLevel >= 51 && currentLevel <= 60)
                return 1000;
            else if (currentLevel >= 61 && currentLevel <= 70)
                return 1400;
            else if (currentLevel >= 71 && currentLevel <= 80)
                return 1800;
            else if (currentLevel >= 81 && currentLevel <= 90)
                return 2100;
            else if (currentLevel >= 91 && currentLevel <= 99)
                return 2500;
            else
                return 10000;
        }
        public static void SetExp(Player player, int exp)
        {
            if (player == null) return;
            if (exp < 0) exp = 0;
            expCache[player] = exp;
            sql.Update(player.UserId, experience: exp);
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
                                    if(item.UserId == targetUserID)
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
                    if(target == null)
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
                    response = "done";

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

