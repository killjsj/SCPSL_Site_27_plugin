using AutoEvent.Events;
using CentralAuth;
using Cmdbinding;
using CommandSystem.Commands.RemoteAdmin;
using CustomPlayerEffects;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.API.Features.Roles;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Footprinting;
using GameCore;
using Google.Protobuf.WellKnownTypes;
using HarmonyLib;
using Hazards;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using MapGeneration.Holidays;
using Mirror;
using MySqlX.XDevAPI;
using NorthwoodLib.Pools;
using Org.BouncyCastle.Math.EC.Multiplier;
using Org.BouncyCastle.Pkix;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using PlayerRoles.RoleAssign;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using Scp914;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using YamlDotNet.Core.Tokens;
using static HarmonyLib.AccessTools;
using static PlayerStatsSystem.DamageHandlerBase;
using Log = Exiled.API.Features.Log;
using Type = System.Type;
namespace Next_generationSite_27.UnionP.Scp5k
{
    [HarmonyPatch(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility.ServerProcessCmd))]
    public class Scp049AttackPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var found = false;
            for (int i = 0; i < codes.Count - 2; i++)
            {
                // 查找 this.Cooldown.Trigger(1.5) 的指令序列
                if (
                    codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Ldfld &&
                    codes[i + 2].opcode == OpCodes.Ldc_R8 && (double)codes[i + 2].operand == 1.5 &&
                    codes[i + 3].opcode == OpCodes.Callvirt
                )
                {
                    // 替换 ldc.r8 1.5 为 Scp5k_Control.Is5kRound ? 0.7 : 1.5
                    codes[i + 2] = new CodeInstruction(OpCodes.Call, typeof(Scp049AttackPatch).GetMethod(nameof(GetCooldown), BindingFlags.Static | BindingFlags.NonPublic));
                    found = true;
                    break;
                }
            }
            if (!found)
                Log.Warn("[SCP-049 Attack Patch] 未找到冷却时间指令，未做任何修改。");
            return codes;
        }

        // 用于 Transpiler 的静态方法
        private static double GetCooldown()
        {
            return Scp5k_Control.Is5kRound ? 0.7 : 1.5;
        }
    }
    [HarmonyPatch(typeof(CustomPlayerEffects.CardiacArrest), "ServerUpdate")]
    public class CardiacArrestPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var found = false;
            for (int i = 0; i < codes.Count - 2; i++)
            {
                // 查找 8f 的伤害数值并替换
                if (
                    codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Ldfld &&
                    codes[i + 2].opcode == OpCodes.Ldc_R4 && (float)codes[i + 2].operand == 8f
                )
                {
                    // 替换 ldc.r4 8 为 CardiacArrestPatch.GetDamage()
                    codes[i + 2] = new CodeInstruction(OpCodes.Call, typeof(CardiacArrestPatch).GetMethod(nameof(GetDamage), BindingFlags.Static | BindingFlags.NonPublic));
                    found = true;
                    break;
                }
            }
            if (!found)
                Log.Warn("[CardiacArrest Patch] 未找到伤害指令，未做任何修改。");
            return codes;
        }

        // 用于 Transpiler 的静态方法
        private static float GetDamage()
        {
            return Scp5k_Control.Is5kRound ? 16f : 8f;
        }
    }


    [HarmonyPatch(typeof(PlayerStats))]
    public static class PlayerStatsPatch
    {
        [HarmonyPatch("DealDamage")]
        [HarmonyPrefix]
        public static bool Prefix(PlayerStats __instance, DamageHandlerBase handler, ref bool __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                var hub = typeof(PlayerStats)
                    .GetField("_hub", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(__instance) as ReferenceHub;
                if(hub != null)
                {
                    if (handler is WarheadDamageHandler attackerHandler)
                    {
                        if(hub.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp079)
                        {
                            __result = false;
                            return false;  // 跳过原始方法执行}

                        }
                    }
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(ZombieAttackAbility))]
    public static class ZombieAttackAbilityPatch
    {
        [HarmonyPatch("get_DamageAmount")]
        [HarmonyPrefix]
        public static bool Prefix(ZombieAttackAbility __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = Math.Min(40 + ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy), 90f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
        [HarmonyPatch("get_BaseCooldown")]
        [HarmonyPrefix]
        public static bool BaseCDPrefix(ZombieAttackAbility __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = Math.Max(1.3f - ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy) * 0.05f, 0.5f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Scp3114Slap))]
    public static class Scp3114SlapPatch
    {
        [HarmonyPatch("get_DamageAmount")]
        [HarmonyPrefix]
        public static bool Prefix(Scp3114Slap __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = Math.Min(15 + ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy), 50f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
        [HarmonyPatch("get_BaseCooldown")]
        [HarmonyPrefix]
        public static bool BaseCDPrefix(Scp3114Slap __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = Math.Max(0.5f - ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy) * 0.01f, 0.2f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
        [HarmonyPatch("get_AttackDelay")]
        [HarmonyPrefix]
        public static bool AttackDelayPrefix(Scp3114Slap __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = 0f;
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Scp939ClawAbility))]
    public static class Scp939ClawAbilityPatch
    {
        [HarmonyPatch("get_DamageAmount")]
        [HarmonyPrefix]
        public static bool Prefix(Scp939ClawAbility __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = Math.Min(40 + ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy), 90f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
        [HarmonyPatch("get_BaseCooldown")]
        [HarmonyPrefix]
        public static bool BaseCDPrefix(Scp939ClawAbility __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                __result = Math.Max(0.8f - ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy) * 0.05f, 0.4f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Hitmarker))]
    public static class HitmarkerPatch
    {
        [HarmonyPatch("CheckHitmarkerPerms")]
        [HarmonyPrefix]
        public static bool Prefix(AttackerDamageHandler adh, ReferenceHub victim, ref bool __result)
        {
            if (Plugin.CurrentFFManager != null)
            {
                if (Plugin.CurrentFFManager.IsDamaging(Player.Get(adh.Attacker), Player.Get(victim)))
                {
                    __result = true;
                    return false;  // 跳过原始方法执行}
                }
                else
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
