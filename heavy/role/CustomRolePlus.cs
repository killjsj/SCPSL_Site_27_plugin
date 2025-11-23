using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.Hints;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Events.Handlers;
using MonoMod.Utils;
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
        public static string abilitiesShower(AutoContentUpdateArg ev)
        {
            var p = Player.Get(ev.PlayerDisplay.ReferenceHub);
            string showing = "<size=24>";
            if (p != null)
            {
                if (AbilityBase.PlayerAbilities.TryGetValue(p, out var list))
                {
                    foreach (var item in list)
                    {
                        var N = item.Name;
                        var CustomInfo = item.CustomInfoToShow;
                        bool show = false;
                        string nS = $"{N}: ";
                        if (item is ICounted CDA)
                        {
                            var Count = CDA.count;
                            var TotalCount = CDA.TotalCount;
                            nS += $"<color={(Count == 0 ? "red" : "green")}>{Count}</color>/{TotalCount} ";
                            show = true;
                        }
                        if(item is ITiming timing)
                        {
                            var RemainTime = timing.CoolDownRemaining;
                            var SkillRemainTime = timing.DoneRemaining;
                            nS += $"{(!timing.Done ? "还剩下:" : "冷却:")}{(!timing.Done ? SkillRemainTime : RemainTime):F0}s ";
                            show = true;
                        }
                        if (!string.IsNullOrEmpty(item.CustomInfoToShow))
                        {
                            show = true;
                            nS += $"{CustomInfo}";
                        }
                        nS += "\n";
                        if (show)
                        {
                            showing += nS;
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
        public static HintServiceMeow.Core.Models.Hints.Hint RoleHint = new()
        {
            AutoText = RoleShower,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Right,
            YCoordinate = 700,
        };

        public static string RoleShower(AutoContentUpdateArg ev)
        {
            var p = Player.Get(ev.PlayerDisplay.ReferenceHub);
            string showing = "<size=24>";
            if (p != null)
            {
                if (!string.IsNullOrEmpty(p.UniqueRole))
                {
                    var r = CustomRole.Get(p.UniqueRole);
                    showing += $"你是: {p.UniqueRole}\n{r.Description}\n";
                }
                if (p.CurrentItem != null) {
                    var c = p.CurrentItem.GetItemSCustom();
                    if (c != null) { 
                    showing += $"{c.Name}:{c.Description}";

                    }
                }
            }
            showing += "</size>";
            return showing;
        }
        protected override void ShowMessage(Player player)
        {
            //base.ShowMessage(player);
            if (player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                if (!hSM_Hint.hud.HasHint(RoleHint.Guid))
                {
                    hSM_Hint.hud.AddHint(RoleHint);
                }
            }
        }
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

                if (!AbilityBase.PlayerAbilities.TryGetValue(player, out var list))
                {
                    list = new List<AbilityBase>();
                    AbilityBase.PlayerAbilities[player] = list;
                }

                list.Add(instance);
            }
        }
        
        protected override void RoleRemoved(Player player)
        {
            base.RoleRemoved(player);
            
            if (!AbilityBase.PlayerAbilities.TryGetValue(player, out var list))
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
    public static class CustomItemEx
    {
        public static CustomItemPlus GetItemSCustom(this Item item)
        {
            if (item == null) return null;
            if (CustomItemPlus.ItemMapping.TryGetValue(item, out var map))
            {
                return map;
            }
            return null;
        }
    }
    public abstract class CustomItemPlus : CustomItem
    {
        public List<ItemAbilityBase> abilities = new List<ItemAbilityBase>();
        
        public static Dictionary<Item, CustomItemPlus> ItemMapping = new();
        public static Dictionary<Player, List<AbilityBase>> PlayerAbilities => AbilityBase.PlayerAbilities;
        public static Dictionary<ushort, List<ItemAbilityBase>> ItemAbilities => ItemAbilityBase.ItemABs;
        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            ItemPickupBase.OnPickupDestroyed += ItemPickupBase_OnPickupDestroyed;
            //ItemPickupBase.OnPickupDestroyed += ItemPickupBase_OnPickupDestroyed;
            InventoryExtensions.OnItemRemoved += ItemBase_OnItemRemoved;
            PlayerEvents.PickingUpItem += PlayerEvents_PickingUpItem;
        }
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            ItemPickupBase.OnPickupDestroyed -= ItemPickupBase_OnPickupDestroyed;
            //ItemPickupBase.OnPickupDestroyed += ItemPickupBase_OnPickupDestroyed;
            InventoryExtensions.OnItemRemoved -= ItemBase_OnItemRemoved;
            PlayerEvents.PickingUpItem -= PlayerEvents_PickingUpItem;
        }
        private static List<ushort> PickedItem = new();
        private void PlayerEvents_PickingUpItem(LabApi.Events.Arguments.PlayerEvents.PlayerPickingUpItemEventArgs ev)
        {
            PickedItem.Add(ev.Pickup.Serial);
        }

        private void ItemBase_OnItemRemoved(ReferenceHub hub, ItemBase it, ItemPickupBase itb)
        {
            if(itb != null) return;
            OnDestroyedInternal(it.ItemId.SerialNumber,hub);

        }
        protected override void ShowSelectedMessage(Player player)
        {
            //base.ShowMessage(player);
            if (player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                if (!hSM_Hint.hud.HasHint(CustomRolePlus.RoleHint.Guid))
                {
                    hSM_Hint.hud.AddHint(CustomRolePlus.RoleHint);
                }
            }
        }
        private void ItemPickupBase_OnPickupDestroyed(ItemPickupBase obj)
        {
            if (PickedItem.Contains(obj.ItemId.SerialNumber))
            {
                PickedItem.Remove(obj.ItemId.SerialNumber);
                return;
            }
            OnDestroyedInternal(obj.ItemId.SerialNumber);
        }
        protected virtual void OnDestroyedInternal(ushort serial, ReferenceHub referenceHub = null)
        {
            if (TrackedSerials.Contains(serial))
            {
                var player = Player.Get(referenceHub);

                if (!ItemAbilities.TryGetValue(serial, out var list1))
                    return;

                List<ItemAbilityBase> ab = new();
                foreach (var ability in list1)
                {
                    if (ability is IitemRegisiterNeeded<ItemAbilityBase> reg1)
                    {
                        reg1.Unregister(serial);

                    }
                    ab.Add(ability);
                }
                list1.RemoveAll(x => ab.Any(y => y.id == x.id));
                if (player != null) {
                    if (!PlayerAbilities.TryGetValue(player, out var list))
                        return;
                    foreach (var ability in list.Where(x=>ab.Contains(x))) {
                        if (ability is IRegisiterNeeded<ItemAbilityBase> reg1)
                        {
                            reg1.Unregister(player);
                        }
                    }
                    list.RemoveAll(x => ab.Any(y => y.id == x.id));

                }
                ItemAbilities.Remove(serial);
                OnDestroyed(serial, player);
                TrackedSerials.Remove(serial);
                
            }
            
        }
        protected virtual void OnDestroyed(ushort serial, Player player = null) { }
        protected override void OnAcquired(Player player, Item item, bool displayMessage)
        {
            base.OnAcquired(player, item, displayMessage);
            ItemMapping[item] = this;

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
            //item.Destroy
            if (player.GetHUD() is HSM_hintServ hSM_Hint)
            {
                if (!hSM_Hint.hud.HasHint(CustomRolePlus.AbilitiesHint.Guid))
                {
                    hSM_Hint.hud.AddHint(CustomRolePlus.AbilitiesHint);
                }
            }
            if (!ItemAbilities.TryGetValue(item.Serial, out var list))
            {
                list = new List<ItemAbilityBase>();
                ItemAbilities[item.Serial] = list;
                foreach (var template in abilities)
                {
                    // 复制或注册单独实例
                    ItemAbilityBase instance = template;


                    if (template is IitemRegisiterNeeded<ItemAbilityBase> reg1)
                    {
                        instance = reg1.Register(item.Serial);
                    }
                    else if (template is IRegisiterNeeded<ItemAbilityBase> reg)
                    {
                        instance = reg.Register(player);
                    }

                    list.Add(instance);
                }
            }
            PlayerAbilities[player].AddRange(list);



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
                if (!ItemAbilities.TryGetValue(ev.Item.Serial, out var list1))
                    return;

                List<ItemAbilityBase> ab = new();
                foreach (var ability in list1)
                {
                    if (ability is IRegisiterNeeded<ItemAbilityBase> reg)
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
