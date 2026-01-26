using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Interface;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Utilities;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Log = Exiled.API.Features.Log;

namespace Next_generationSite_27.UnionP
{
    public class ScpHpShow : BaseClass
    {
        public static float targetX = 930;
        public static float targetY = 800;
        public string ScpText = "<color=red><size=18>SCP{scp}:<color=green>血量 {hp} <color=purple>护盾 {sh}</size></color>";
        public string Scp079Text = "<color=red><size=18>SCP079:<color=green>LV {lv} <color=yellow>Exp {exp}</size></color>";
        public string ZombieText = "<color=red><size=18>SCP049-2:<color=green>{count}个</size></color>";

        public List<Player> Scp = new List<Player>();
        public Hint shower;

        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            Exiled.Events.Handlers.Player.Died += Died;
            Exiled.Events.Handlers.Player.Left += Left;
            shower = new Hint() { 
                XCoordinate = targetX,
                YCoordinate = targetY,
                AutoText = new HintServiceMeow.Core.Models.HintContent.AutoContent.TextUpdateHandler(Update),
                FontSize = 10
            };
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            Exiled.Events.Handlers.Player.Died -= Died;
            Exiled.Events.Handlers.Player.Left -= Left;
            //base.Delete();
        }

        void Died(DiedEventArgs ev)
        {
            if (Scp.Contains(ev.Player))
            {
                RemoveScp(ev.Player);
            }
        }

        void Left(LeftEventArgs ev)
        {
            if (Scp.Contains(ev.Player))
            {
                RemoveScp(ev.Player);
            }
        }

        void ChangingRole(ChangingRoleEventArgs ev)
        {
            // 玩家离开SCP角色时清理
            if (Scp.Contains(ev.Player))
            {
                RemoveScp(ev.Player);
            }
            Timing.CallDelayed(0.2f, () =>
            {
                if (IsScpRole(ev.NewRole))
                {
                    AddScp(ev.Player, ev.NewRole);
                }
            });
        }

        private void AddScp(Player player, RoleTypeId role)
        {
            Scp.Add(player);
            player.AddHint(shower);
        }

        private void RemoveScp(Player player)
        {
            Scp.Remove(player);
            player.RemoveHint(shower);
        }
        string Update(AutoContentUpdateArg ev)
        {
            string show = "";
            var ZombieCount = 0;
            foreach (var item in Scp)
            {
                if (item.Role == RoleTypeId.Scp0492)
                {
                    ZombieCount += 1;
                }
                else if (item.Role == RoleTypeId.Scp079)
                {
                    var scp079 = (Scp079Role)item.Role;
                    show += Scp079Text.Replace("{lv}", scp079.Level.ToString())
                        .Replace("{exp}", scp079.RelativeExperience.ToString()) + "\n";
                }
                else
                {
                    var hp = item.Health;
                    var sh = item.HumeShield;
                    show += ScpText.Replace("{scp}", GetScpNumber(item.Role))
                        .Replace("{hp}", ((int)hp).ToString())
                        .Replace("{sh}", sh.ToString()) + "\n";
                }
            }
            show += ZombieText.Replace("{count}", ZombieCount.ToString());
            return show;
        }

        bool IsScpRole(RoleTypeId role)
        {
            return role == RoleTypeId.Scp173 || role == RoleTypeId.Scp106 || role == RoleTypeId.Scp049 ||
                   role == RoleTypeId.Scp079 || role == RoleTypeId.Scp096 || role == RoleTypeId.Scp0492 ||
                   role == RoleTypeId.Scp939 || role == RoleTypeId.Scp3114;
        }

        string GetScpNumber(RoleTypeId role)
        {
            return role switch
            {
                RoleTypeId.Scp049 => "049",
                RoleTypeId.Scp079 => "079",
                RoleTypeId.Scp096 => "096",
                RoleTypeId.Scp106 => "106",
                RoleTypeId.Scp173 => "173",
                RoleTypeId.Scp3114 => "3114",
                RoleTypeId.Scp939 => "939",
                _ => "???"
            };
        }
    }
}



