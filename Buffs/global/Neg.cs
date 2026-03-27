using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp049;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.Handlers;
using GameCore;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Autosync;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using ProjectMER.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static InventorySystem.Items.Firearms.ShotEvents.ShotEventManager;
using Map = Exiled.API.Features.Map;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP.Buffs
{
    public class Nice049 : GlobalBuffBase
    {
        public float NextTime = 0f;
        public override string Description => "Scp049救人能力增强 僵尸攻击增强";

        public override BuffType Type => BuffType.Negative;
        public static Nice049 ins;
        public override string BuffName => "医者仁心";
        public override bool CanEnable()
        {
            return Player.Enumerable.Any(x => x.Role.Type == RoleTypeId.Scp049);
        }
        [HarmonyPatch(nameof(Scp049ResurrectAbility))]
        public static class S049RecallPatch
        {
            [HarmonyPatch(typeof(Scp049ResurrectAbility), "get_Duration")]
            [HarmonyPrefix]
            public static bool GetDurationPrefix(ref float __result)
            {
                if (Nice049.ins != null && Nice049.ins.CheckEnabled())
                {
                    __result = 3f;
                    return false; // 跳过原始方法
                }
                return true; // 执行原始方法
            }

            [HarmonyPatch(typeof(Scp049ResurrectAbility), "get_RangeSqr")]
            [HarmonyPrefix]
            public static bool GetRangeSqrPrefix(ref float __result)
            {
                if (Nice049.ins != null && Nice049.ins.CheckEnabled())
                {
                    __result = 6f;
                    return false;
                }
                return true;
            }
        }
        public static class S0492AttackPatch
        {
            [HarmonyPatch(typeof(ZombieAttackAbility), nameof(ZombieAttackAbility.BaseCooldown), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool BaseCooldownGetterPrefix(ref float __result)
            {
                if (Nice049.ins != null && Nice049.ins.CheckEnabled())
                {
                    __result = 0.8f;
                    return false;
                }
                return true;
            }

            // 如果你想修改伤害值，也可以添加
            [HarmonyPatch(typeof(ZombieAttackAbility), nameof(ZombieAttackAbility.DamageAmount), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool DamageAmountPrefix(ref float __result)
            {
                if (Nice049.ins != null && Nice049.ins.CheckEnabled())
                {
                    __result = 50f; // 根据需要调整
                    return false;
                }
                return true;
            }
        }
        public override void Init()
        {
            ins = this;
            base.Init();
        }
        public override void Delete()
        {
            ins = null;
            base.Delete();
        }
    }
    public class SpeedUped : GlobalBuffBase
    {
        public float NextTime = 0f;
        public override string Description => "106增强";

        public override BuffType Type => BuffType.Negative;
        public static SpeedUped ins;
        public override string BuffName => "地下小人";
        public override bool CanEnable()
        {
            return Player.Enumerable.Any(x => x.Role.Type == RoleTypeId.Scp106);
        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            Timing.CallDelayed(1.2f, () => {
                if(ev.Player.Role is Scp106Role s)
                {
                    s.CaptureCooldown = 1f;
                    s.StaminaRegenMultiplier = 1.5f;
                    s.ResetStamina();
                }
            });
        }
        public override void Init()
        {
            ins = this;
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            base.Init();
        }
        public override void Delete()
        {
            ins = null;
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            base.Delete();
        }
    }
}
