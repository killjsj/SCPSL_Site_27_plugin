using Cassie;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Cassie;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.Handlers;
using HarmonyLib;
using InventorySystem.Items.Autosync;
using LabApi.Events.Arguments.PlayerEvents;
using Mirror;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP.heavy;
using PlayerRoles.PlayableScps.Scp079;
using Subtitles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;
using static InventorySystem.Items.Firearms.ShotEvents.ShotEventManager;
using Map = Exiled.API.Features.Map;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP.Buffs
{
    [HarmonyPatch(typeof(CassieAnnouncementDispatcher))]

    public static class CassieIsOfflinePatch
    {
        static FieldInfo a = typeof(CassieAnnouncementDispatcher).GetField("AllAnnouncements",BindingFlags.Static | BindingFlags.NonPublic);
        [HarmonyPatch(nameof(Cassie.CassieAnnouncementDispatcher.AddToQueue))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.HigherThanNormal)]

        static bool Prefix(CassieAnnouncement announcement)
        {
            if (!CassieIsOffline.Instance.CheckEnabled()) return true;
            if (announcement.Payload.SubtitleSource == CassieTtsPayload.SubtitleMode.FromTranslation)
            {
                    var s = new CassieTtsPayload("",announcement.Payload.Content, false);
                    var ca = new CassieAnnouncement(s, 0f, 0f);
                    ((List<CassieAnnouncement>)(a.GetValue(null))).AddIfNotContains(ca);
            }
            else if (announcement.Payload.SubtitleSource == CassieTtsPayload.SubtitleMode.Custom)
            {
                CassieIsOffline.Instance.ShowEACFSubtitles(announcement.Payload.Content);
            }
            return false;
        }
    }
    public class CassieIsOffline : BuffBase
    {
        public static CassieIsOffline Instance { get; private set; }

        public override BuffType Type => BuffType.Negative;
        public override bool CanEnable()
        {
            return !Scp5k.Scp5k_Control.Is5kRound;
        }
        public override string BuffName => "断开连接";
        public void ShowEACFSubtitles(string message)
        {
            CustomCassie.CustomCassieMessage(message,  "E.A.F.C", "yellow");
        }
        void RoundStarted()
        {
            if (CheckEnabled() == false)
            {
                return;
            }
            Map.IsDecontaminationEnabled = false;
            ShowEACFSubtitles("警告!检测到与Site01的连接断开,E.A.F.C 已接管设施\n安全系统 通风系统 已下线...");
        }
        void SendingCassieMessage(SendingCassieMessageEventArgs ev)
        {
            if (!CheckEnabled())
            {
                return;
            }
            //if(ev.SubtitleSource == Cassie.CassieTtsPayload.SubtitleMode.Automatic)
            //{
            //    //ShowEACFSubtitles(ev.Words);
            //    ev.IsAllowed = false;
            //    return;
            //}
            //ev.IsAllowed = false;/
        }
        void ChangedRole(PlayerChangedRoleEventArgs ev)
        {
            if (CheckEnabled()) { 
                if(ev.NewRole is PlayerRoles.PlayableScps.Scp079.Scp079Role s)
                {
                    if(s.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var a))
                    {
                        a.AccessTierIndex = 1;
                    }
                }
            }
        }
        public override void Init()
        {
            Instance = this;
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            Exiled.Events.Handlers.Cassie.SendingCassieMessage += SendingCassieMessage;
            LabApi.Events.Handlers.PlayerEvents.TriggeringTesla += PlayerEvents_TriggeringTesla;
            LabApi.Events.Handlers.PlayerEvents.ChangedRole += ChangedRole;
            LabApi.Events.Handlers.PlayerEvents.IdlingTesla += PlayerEvents_IdlingTesla;
            base.Init();
        }

        private void PlayerEvents_IdlingTesla(LabApi.Events.Arguments.PlayerEvents.PlayerIdlingTeslaEventArgs ev)
        {
            if (CheckEnabled() == false)
            {
                return;
            }
            ev.IsAllowed = false;
        }

        private void PlayerEvents_TriggeringTesla(LabApi.Events.Arguments.PlayerEvents.PlayerTriggeringTeslaEventArgs ev)
        {
            if (CheckEnabled() == false)
            {
                return;
            }
            ev.IsAllowed = false;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Exiled.Events.Handlers.Cassie.SendingCassieMessage -= SendingCassieMessage;
            LabApi.Events.Handlers.PlayerEvents.TriggeringTesla -= PlayerEvents_TriggeringTesla;
            LabApi.Events.Handlers.PlayerEvents.IdlingTesla -= PlayerEvents_IdlingTesla;
            LabApi.Events.Handlers.PlayerEvents.ChangedRole -= ChangedRole;
            base.Delete();
        }
    }
}
