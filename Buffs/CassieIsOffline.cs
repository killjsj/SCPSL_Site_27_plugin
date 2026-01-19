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
using static InventorySystem.Items.Firearms.ShotEvents.ShotEventManager;
using Map = Exiled.API.Features.Map;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP.Buffs
{
    [HarmonyLib.HarmonyPatch(typeof(NetworkUtils))]
    public static class CassieIsOfflinePatch
    {
        public static List<SubtitleType> AllowTypes = new List<SubtitleType>() { 
            SubtitleType.Custom,
            SubtitleType.AlphaWarheadCancelled,
            SubtitleType.AlphaWarheadEngage,
            SubtitleType.AlphaWarheadResumed,
            SubtitleType.DeadMansSwitch,
        };
        [HarmonyPatch(nameof(NetworkUtils.SendToHubsConditionally))]
        [HarmonyPrefix]
        static bool Prefix<T>(T msg, Func<ReferenceHub, bool> predicate, int channelId) where T : struct, NetworkMessage
        {
            if(!CassieIsOffline.Instance.CheckEnabled()) return true;
            if (msg is SubtitleMessage subtitle) {
                foreach (var item in subtitle.SubtitleParts)
                {
                    if (!AllowTypes.Contains(item.Subtitle))
                    {
                        return false;
                    }
                }
            }
            return true; 
        }
    }
    public class CassieIsOffline : BuffBase
    {
        public static CassieIsOffline Instance { get; private set; }

        public override BuffType Type => BuffType.Negative;

        public override string BuffName => "断开连接";
        void ShowEACFSubtitles(string message)
        {
            CustomCassie.SendCustomCassieMessage(message, "", "E.A.F.C", "yellow");
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
            if (CheckEnabled() == false)
            {
                return;
            }
            if(ev.SubtitleSource == Cassie.CassieTtsPayload.SubtitleMode.Custom)
            {
                ShowEACFSubtitles(ev.Words);
                return;
            }
            ev.IsAllowed = false;
        }
        void ChangedRole(PlayerChangedRoleEventArgs ev)
        {
            if (CheckEnabled()) { 
                if(ev.NewRole is PlayerRoles.PlayableScps.Scp079.Scp079Role s)
                {
                    if(s.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var a))
                    {
                        a.TotalExp = 1;
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
