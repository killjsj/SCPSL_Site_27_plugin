using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
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
        public Dictionary<RoleTypeId, Hint> HowShowList = new Dictionary<RoleTypeId, Hint>()
        {
            { RoleTypeId.Scp096, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
            { RoleTypeId.Scp079, new Hint() { XCoordinate = targetX, YCoordinate = targetY ,FontSize=10 } },
            { RoleTypeId.Scp173, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
            { RoleTypeId.Scp049, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
            { RoleTypeId.Scp939, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
            { RoleTypeId.Scp106, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
            { RoleTypeId.Scp3114, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
            { RoleTypeId.Scp0492, new Hint() { XCoordinate = targetX, YCoordinate = targetY, FontSize=10 } },
        };

        public Dictionary<Player, List<Hint>> PlayerHintLists = new Dictionary<Player, List<Hint>>();

        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            Exiled.Events.Handlers.Player.Died += Died;
            Exiled.Events.Handlers.Player.Left += Left;
            //base.Init();
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
            // 如果是SCP0492，先移除其他SCP0492玩家
            if (role == RoleTypeId.Scp0492)
            {
                var existingZombie = Scp.FirstOrDefault(p => p.Role.Type == RoleTypeId.Scp0492);
                if (existingZombie != null && existingZombie != player)
                {
                    RemoveScp(existingZombie);
                }
            }

            Scp.Add(player);

            // 为新SCP创建自己的Hint列表（用于显示所有SCP信息）
            var hintList = new List<Hint>();

            // 为所有当前SCP创建Hint（包括自己）
            foreach (var scp in Scp.OrderBy(p => p.Id)) // 排序确保位置一致
            {
                var baseHint = HowShowList[scp.Role.Type];
                var hint = new Hint(baseHint)
                {
                    AutoText = Update,
                    YCoordinate = targetY + hintList.Count * 40 * 0.6f, // 动态Y坐标
                    Alignment = HintServiceMeow.Core.Enum.HintAlignment.Left,
                };
                hintList.Add(hint);
            }

            PlayerHintLists[player] = hintList;

            // 为避免屏闪，先移除所有SCP的Hint再重新添加
            RemoveAllScpHints();
            AddAllScpHints();
        }

        private void RemoveScp(Player player)
        {
            if(player.Role.Type == RoleTypeId.Scp0492)
            {
                if(Player.Enumerable.Count(x => x.Role.Type == RoleTypeId.Scp0492) > 1)
                {
                    return;
                }
            }
            Scp.Remove(player);
            PlayerHintLists.Remove(player);

            // 为避免屏闪，先移除所有SCP的Hint再重新添加
            RemoveAllScpHints();
            AddAllScpHints();
        }

        private void RemoveAllScpHints()
        {
            // 清空所有SCP的Hint
            foreach (var scp in Scp)
            {
                var display = PlayerDisplay.Get(scp);
                if (PlayerHintLists.TryGetValue(scp, out var oldHints))
                {
                    display.RemoveHint(oldHints);
                }
            }
        }

        private void AddAllScpHints()
        {
            // 重新为每个SCP创建Hint列表
            foreach (var scp in Scp)
            {
                var hintList = new List<Hint>();

                // 按顺序为所有SCP创建Hint
                int index = 0;
                foreach (var targetScp in Scp.OrderBy(p => p.Id))
                {
                    var baseHint = HowShowList[targetScp.Role.Type];
                    var hint = new Hint(baseHint)
                    {
                        AutoText = Update,
                        YCoordinate = targetY + index * 40 * 0.6f,
                        Alignment = HintServiceMeow.Core.Enum.HintAlignment.Left,
                    };
                    hintList.Add(hint);
                    index++;
                }

                PlayerHintLists[scp] = hintList;

                // 显示给当前SCP
                var display = PlayerDisplay.Get(scp);
                display.AddHint(hintList);
            }
        }

        string Update(AutoContentUpdateArg ev)
        {
            // 通过Hint.Guid找到对应的SCP
            foreach (var kvp in PlayerHintLists)
            {
                var player = kvp.Key;
                var hints = kvp.Value;

                for (int i = 0; i < hints.Count; i++)
                {
                    if (hints[i].Guid == ev.Hint.Guid)
                    {
                        var targetScp = Scp.OrderBy(p => p.Id).ElementAt(i); // 找到对应位置的SCP

                        if (targetScp.Role is Scp079Role scp079)
                        {
                            return "    " + Scp079Text
                                .Replace("{lv}", scp079.Level.ToString())
                                .Replace("{exp}", scp079.RelativeExperience.ToString());
                        }
                        else if (targetScp.Role.Type == RoleTypeId.Scp0492)
                        {
                            // 始终显示1个SCP049-2
                            return "    " + ZombieText.Replace("{count}", Player.Enumerable.Count(x=>x.Role.Type == RoleTypeId.Scp0492).ToString());
                        }
                        else
                        {
                            string scpNumber = GetScpNumber(targetScp.Role.Type);
                            return "    " + ScpText
                                .Replace("{scp}", scpNumber)
                                .Replace("{hp}", targetScp.Health.ToString("F0"))
                                .Replace("{sh}", targetScp.HumeShield.ToString("F0"));
                        }
                    }
                }
            }
            return "";
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



