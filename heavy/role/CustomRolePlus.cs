using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.Hints;
using Next_generationSite_27.UnionP.heavy.ability;
using Next_generationSite_27.UnionP.UI;
using System;
using System.Collections.Generic;

namespace Next_generationSite_27.UnionP.heavy.role
{
    public abstract class CustomRolePlus : CustomRole
    {
        // 所有玩家共享的能力模板
        public List<AbilityBase> abilities = new List<AbilityBase>();

        // 每个玩家自己的技能实例
        public static Dictionary<Player, List<AbilityBase>> PlayerAbilities = new Dictionary<Player, List<AbilityBase>>();
        public static string abilitiesShower(AutoContentUpdateArg ev)
        {
            var p = Player.Get(ev.PlayerDisplay.ReferenceHub);
            string showing = "<align=right><size=16>";
            if (p != null)
            {
                if (PlayerAbilities.TryGetValue(p, out var list))
                {
                    foreach (var item in list)
                    {
                        if(item is CoolDownAbility CDA)
                        {
                            var N = CDA.Name;
                            var Count = CDA.count;
                            var TotalCount = CDA.TotalCount;
                            var RemainTime = CDA.cooldown.Remaining;
                            var SkillRemainTime = CDA.DoneCooldown.Remaining;
                            showing += $"{N}: <color={(Count == 0 ? "red" : "green")}>{Count}</color>/{TotalCount} {(!CDA.DoneCooldown.IsReady ? "还剩下:" : "冷却:")}{(!CDA.DoneCooldown.IsReady ? SkillRemainTime : RemainTime):F0}s\n";
                        }
                    }
                }
            }
            showing += "</size></align>";
            return showing;
        }
        public HintServiceMeow.Core.Models.Hints.Hint AbilitiesHint = new()
        {
            AutoText = abilitiesShower,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Right,
            YCoordinate = 400,
        };
        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);

            if (abilities == null || abilities.Count == 0)
                return;
            if(player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                hSM_Hint.hud.AddHint(AbilitiesHint);
            }
            foreach (var template in abilities)
            {
                // 复制或注册单独实例
                AbilityBase instance = template;

                if (template is IRegisiterNeeded<AbilityBase> reg)
                {
                    instance = reg.Register(player);
                }

                if (!PlayerAbilities.TryGetValue(player, out var list))
                {
                    list = new List<AbilityBase>();
                    PlayerAbilities[player] = list;
                }

                list.Add(instance);
            }
        }

        protected override void RoleRemoved(Player player)
        {
            base.RoleRemoved(player);
            if (player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                hSM_Hint.hud.RemoveHint(AbilitiesHint);
            }
            if (!PlayerAbilities.TryGetValue(player, out var list))
                return;

            foreach (var ability in list)
            {
                if (ability is IRegisiterNeeded<AbilityBase> reg)
                {
                    reg.Unregister(player);
                }
            }

            PlayerAbilities.Remove(player);
        }
    }
}
