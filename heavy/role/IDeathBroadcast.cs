using Cassie;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using NorthwoodLib.Pools;
using PlayerRoles;
using Respawning.NamingRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP.heavy
{
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
    public class DeathBroadcast : BaseClass
    {
        public static void OnDeath(DiedEventArgs ev)
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
                        Exiled.API.Features.Cassie.MessageTranslated($"SCP {str} CONTAINEDSUCCESSFULLY BY {str2}", $"{text2} 已被{text3}重新收容。");
                    }
                }
            }
        }
        public static void AnnouncingScpTermination(AnnouncingScpTerminationEventArgs ev)
        {
            if (ev.Attacker.UniqueRole != "")
            {
                ev.IsAllowed = false;
            }

        }
        public static void ConvertSCP(RoleTypeId role, out string withoutSpace, out string withSpace)
        {
            PlayerRoleBase playerRoleBase;
            if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
            {
                withoutSpace = string.Empty;
                withSpace = string.Empty;
                return;
            }
            CassieScpTerminationAnnouncement.ConvertSCP(playerRoleBase.RoleName, out withoutSpace, out withSpace);
        }

        // Token: 0x06003CF9 RID: 15609 RVA: 0x000CA680 File Offset: 0x000C8880
        public static void ConvertSCP(string roleName, out string withoutSpace, out string withSpace)
        {
            StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
            string[] array = roleName.Split('-');
            if (array.Length < 2)
            {
                //Log.InfoError("Cassie role cannot be split by '-'. Possibly missing translation.");
                withoutSpace = "404";
                withSpace = "4 0 4";
                return;
            }
            withoutSpace = array[1];
            foreach (char value in withoutSpace)
            {
                stringBuilder.Append(value);
                stringBuilder.Append(' ');
            }
            withSpace = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
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
                ConvertSCP(result.RoleName, out withoutSpace, out withSpace);
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
                        ConvertSCP(Attacker.Role, out withoutSpace, out withSpace);
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

        public override void Init()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Player.Died += OnDeath;
            Exiled.Events.Handlers.Map.AnnouncingScpTermination += AnnouncingScpTermination;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Player.Died -= OnDeath;
            Exiled.Events.Handlers.Map.AnnouncingScpTermination -= AnnouncingScpTermination;
        }
    }
}
