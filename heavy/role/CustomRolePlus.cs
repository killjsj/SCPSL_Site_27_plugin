using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.Hints;
using Next_generationSite_27.UnionP.heavy.ability;
using Next_generationSite_27.UnionP.UI;
using System;
using System.Collections.Generic;
using System.Linq;

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
            string showing = "<size=24>";
            if (p != null)
            {
                if (PlayerAbilities.TryGetValue(p, out var list))
                {
                    foreach (var item in list)
                    {
                        if (item is CoolDownAbility CDA)
                        {
                            var N = CDA.Name;
                            var Count = CDA.count;
                            var TotalCount = CDA.TotalCount;
                            var RemainTime = CDA.cooldown.Remaining;
                            var SkillRemainTime = CDA.DoneCooldown.Remaining;
                            var CustomInfo = CDA.CustomInfoToShow;
                            showing += $"{N}: <color={(Count == 0 ? "red" : "green")}>{Count}</color>/{TotalCount} {(!CDA.DoneCooldown.IsReady ? "还剩下:" : "冷却:")}{(!CDA.DoneCooldown.IsReady ? SkillRemainTime : RemainTime):F0}s {CustomInfo}\n";
                        } else if (!string.IsNullOrEmpty(item.CustomInfoToShow))
                        {

                            var N = item.Name;
                            var CustomInfo = item.CustomInfoToShow;
                            showing += $"{N}:{CustomInfo}\n";
                        }
                    }
                }
            }
            showing += "</size>";
            return showing;
        }
        public static HintServiceMeow.Core.Models.Hints.Hint AbilitiesHint = new()
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
            if (player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                if (!hSM_Hint.hud.HasHint(AbilitiesHint.Guid))
                {
                    hSM_Hint.hud.AddHint(AbilitiesHint);
                }
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
            
            if (!PlayerAbilities.TryGetValue(player, out var list))
                return;
            List<AbilityBase> ab = new();
            foreach (var ability in list)
            {
                if (ability is IRegisiterNeeded<AbilityBase> reg)
                {
                    reg.Unregister(player);

                }
                ab.Add(ability);
            }
            list.RemoveAll(x => ab.Any(y=>y.id == x.id));
            if (list.Count <= 0)
            {
                if (player.GetHUD() is HSM_hintServ hSM_Hint)
                {
                    if (!hSM_Hint.hud.HasHint(CustomRolePlus.AbilitiesHint.Guid))
                    {
                        hSM_Hint.hud.RemoveHint(AbilitiesHint);
                    }

                }
            }
        }
    }
    public abstract class CustomItemPlus : CustomItem
    {
        public List<AbilityBase> abilities = new List<AbilityBase>();
        
        public static Dictionary<Player, List<AbilityBase>> PlayerAbilities => CustomRolePlus.PlayerAbilities;
        protected override void OnAcquired(Player player, Item item, bool displayMessage)
        {
            base.OnAcquired(player, item, displayMessage);
            if (abilities == null || abilities.Count == 0)
                return;

            foreach (var item1 in player.Items)
            {
                if (item1.Serial != item.Serial)
                {
                    if (Check(item))
                    {
                        return;
                    }
                }
            }

            if (player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                if (!hSM_Hint.hud.HasHint(CustomRolePlus.AbilitiesHint.Guid))
                {
                    hSM_Hint.hud.AddHint(CustomRolePlus.AbilitiesHint);
                }
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
        
        protected override void OnDroppingItem(DroppingItemEventArgs ev)
        {
            base.OnDroppingItem(ev);
            if (Check(ev.Item))
            {
                foreach (var item in ev.Player.Items)
                {
                    if (item.Serial != ev.Item.Serial)
                    {
                        if (Check(item))
                        {
                            return;
                        }
                    }
                }
                if (!PlayerAbilities.TryGetValue(ev.Player, out var list))
                    return;

                List<AbilityBase> ab = new();
                foreach (var ability in list)
                {
                    if (ability is IRegisiterNeeded<AbilityBase> reg)
                    {
                        reg.Unregister(ev.Player);

                    }
                    ab.Add(ability);
                }
                list.RemoveAll(x => ab.Any(y => y.id == x.id));
                if (list.Count <= 0)
                {
                    if (ev.Player.GetHUD() is HSM_hintServ hSM_Hint)
                    {
                        if (!hSM_Hint.hud.HasHint(CustomRolePlus.AbilitiesHint.Guid))
                        {
                            hSM_Hint.hud.RemoveHint(CustomRolePlus.AbilitiesHint);
                        }

                    }
                }
            }
        }

    }
}
