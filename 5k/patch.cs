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
            return Scp5k_Control.Is5kRound ? 16f : 8f; // 举例：5k回合伤害减半
        }
    }

    [HarmonyPatch(typeof(HitboxIdentity))]
    public static class HitboxIdentityPatch
    {
        [HarmonyPatch("IsDamageable", typeof(ReferenceHub), typeof(ReferenceHub))]
        [HarmonyPrefix]
        public static bool Prefix(ReferenceHub attacker, ReferenceHub victim, ref bool __result)
        {
            if (Scp5k_Control.Is5kRound)
            {



                __result = ((ServerConfigSynchronizer.Singleton.MainBoolsSync & 1) == 1 || IsEnemy(attacker, victim));
                Log.Info(__result);

                return false;  // 跳过原始方法执行}
            }
            return true;
        }

        [HarmonyPatch("IsEnemy", typeof(ReferenceHub), typeof(ReferenceHub))]
        [HarmonyPrefix]
        public static bool IsEnemyPrefix(ReferenceHub attacker, ReferenceHub victim, ref bool __result)
        {
            if (Scp5k_Control.Is5kRound)
            {




                __result = IsEnemy(attacker, victim);  // 跳过原始方法执行}
                return false;
            }
            return true;
        }

        public static bool IsEnemy(ReferenceHub attacker, ReferenceHub victim)
        {
            // 获取攻击者和受害者的 Player 对象
            var a = Player.Get(attacker);
            var v = Player.Get(victim);

            // 安全检查：如果玩家对象不存在，返回 false（无法造成伤害）
            if (a == null || v == null)
                return false;

            // 尝试获取 Goc 角色 (仅用于判定，Uiu 和 Bot 不再需要)
            if (!CustomRole.TryGet(Scp5k_Control.GocCID, out var customGocC) ||
                !CustomRole.TryGet(Scp5k_Control.GocPID, out var customGocP))
            {
                // 如果无法获取 Goc 角色定义，则回退到默认的队伍判定
                return HitboxIdentity.IsEnemy(attacker.GetTeam(), victim.GetTeam());
            }

            // 判定攻击者是否为 Goc
            bool isAttackerGoc = customGocC.Check(a) || customGocP.Check(a) ||
                                 a.UniqueRole == customGocC.Name || a.UniqueRole == customGocP.Name;

            // 判定受害者是否为 Goc
            bool isVictimGoc = customGocC.Check(v) || customGocP.Check(v) ||
                               v.UniqueRole == customGocC.Name || v.UniqueRole == customGocP.Name;

            // 核心逻辑：
            if (isAttackerGoc)
            {
                return !isVictimGoc;
            }
            else if (isVictimGoc)
            {
                return true;
            }
            else if(a.LeadingTeam == Exiled.API.Enums.LeadingTeam.Anomalies && v.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces)
            {
                return false;
            }
            else if(a.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces && v.LeadingTeam == Exiled.API.Enums.LeadingTeam.Anomalies)
            {
                return false;
            }
            else
            {
                return IsEnemy(attacker.GetTeam(), victim.GetTeam());
            }
        }
        public static bool IsEnemy(Team attackerTeam, Team victimTeam)
        {
            return attackerTeam != Team.Dead && victimTeam != Team.Dead && (attackerTeam != Team.SCPs || victimTeam != Team.SCPs) && attackerTeam.GetFaction() != victimTeam.GetFaction();
        }
    }
    [HarmonyPatch(typeof(AttackerDamageHandler))]
    public static class AttackerDamageHandlerPatch
    {
        private static bool DisableSpawnProtect(ReferenceHub attacker, ReferenceHub target)
        {
            return attacker != null &&
                   SpawnProtected.CanShoot &&
                   SpawnProtected.CheckPlayer(attacker) &&
                   attacker != target;
        }
        public static bool CheckFriendlyFirePlayerRules(Footprint attackerFootprint, ReferenceHub victimHub, out float ffMultiplier)
        {
            ffMultiplier = 1f;

            // Return false, no custom friendly fire allowed, default to NW logic for FF. No point in processing if FF is enabled across the board.
            if (Server.FriendlyFire)
                return HitboxIdentity.IsDamageable(attackerFootprint.Role, victimHub.roleManager.CurrentRole.RoleTypeId);


            // Always allow damage from Server.Host
            if (attackerFootprint.Hub == Server.Host.ReferenceHub)
                return true;

            // Only check friendlyFire if the FootPrint hasn't changed (Fix for Grenade not dealing damage because it's from a dead player)
            if (!attackerFootprint.CompareLife(new Footprint(attackerFootprint.Hub)))
                return HitboxIdentity.IsDamageable(attackerFootprint.Role, victimHub.roleManager.CurrentRole.RoleTypeId);

            // Check if attackerFootprint.Hub or victimHub is null and log debug information
            if (attackerFootprint.Hub is null || victimHub is null)
            {
                Log.Debug($"CheckFriendlyFirePlayerRules, Attacker hub null: {attackerFootprint.Hub is null}, Victim hub null: {victimHub is null}");
                return true;
            }

            try
            {
                Player attacker = Player.Get(attackerFootprint.Hub);
                Player victim = Player.Get(victimHub);
                // 尝试获取 Goc 角色 (仅用于判定，Uiu 和 Bot 不再需要)
                if (!CustomRole.TryGet(Scp5k_Control.GocCID, out var customGocC) ||
                    !CustomRole.TryGet(Scp5k_Control.GocPID, out var customGocP) ||
                    !CustomRole.TryGet(Scp5k_Control.GocSpyID, out var customGocSpy))
                {
                    goto cont;
                }

                // 判定攻击者是否为 Goc
                bool isAttackerGoc = customGocC.Check(attacker) || customGocP.Check(attacker) ||
                                     attacker.UniqueRole == customGocC.Name || attacker.UniqueRole == customGocP.Name ||
                                     attacker.UniqueRole == customGocSpy.Name || attacker.UniqueRole == customGocP.Name;

                // 判定受害者是否为 Goc
                bool isVictimGoc = customGocC.Check(victim) || customGocP.Check(victim) ||
                                   victim.UniqueRole == customGocC.Name || victim.UniqueRole == customGocP.Name ||
                                   victim.UniqueRole == customGocSpy.Name || victim.UniqueRole == customGocP.Name;
                if (isAttackerGoc)
                {
                    if(!isVictimGoc)
                    {
                        ffMultiplier = 1;
                        return true;
                    }
                    else
                    {
                        ffMultiplier = 0;
                        return true;
                    }
                }
                else if (isVictimGoc)
                {
                    ffMultiplier = 1;
                    return true;
                }
            cont:
                if (attacker is null || victim is null)
                {
                    Log.Debug($"CheckFriendlyFirePlayerRules, Attacker null: {attacker is null}, Victim null: {victim is null}");
                    return true;
                }

                if (attacker == victim)
                {
                    Log.Debug("CheckFriendlyFirePlayerRules, Attacker player was equal to Victim, likely suicide");
                    return true;
                }

                Log.Debug($"CheckFriendlyFirePlayerRules, Attacker role {attacker.Role} and Victim {victim.Role}");

                // Check victim's UniqueRole for custom FF multiplier
                if (!string.IsNullOrEmpty(victim.UniqueRole) &&
                    victim.CustomRoleFriendlyFireMultiplier.TryGetValue(victim.UniqueRole, out Dictionary<RoleTypeId, float> victimPairedData) &&
                    victimPairedData.TryGetValue(attacker.Role, out ffMultiplier))
                {
                    return true;
                }

                // Check attacker's UniqueRole for custom FF multiplier
                if (!string.IsNullOrEmpty(attacker.UniqueRole) &&
                    attacker.CustomRoleFriendlyFireMultiplier.TryGetValue(attacker.UniqueRole, out Dictionary<RoleTypeId, float> attackerPairedData) &&
                    attackerPairedData.TryGetValue(victim.Role, out ffMultiplier))
                {
                    return true;
                }

                // Default FF logic for SCP or other roles without unique roles
                if (!attacker.FriendlyFireMultiplier.IsEmpty() &&
                    attacker.FriendlyFireMultiplier.TryGetValue(victim.Role, out ffMultiplier))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CheckFriendlyFirePlayerRules failed to handle friendly fire because: {ex}");
            }

            // Default to NW logic
            return HitboxIdentity.IsDamageable(attackerFootprint.Role, victimHub.roleManager.CurrentRole.RoleTypeId);
        }
    
        [HarmonyPatch("ProcessDamage")]
        [HarmonyPrefix]
        public static bool Prefix(AttackerDamageHandler __instance, ReferenceHub ply)
        {
            // 仅在 5k 轮次生效
            if (!Scp5k_Control.Is5kRound)
                return true; // 非5k轮次：走原版逻辑（但你可能想 return false？见下方说明）

            // 安全检查
            if (__instance.Attacker.Hub == null || ply == null)
            {
                return true;
            }

            var attacker = __instance.Attacker.Hub;
            var victim = ply;

            // 出生保护：攻击者受保护 → 无法造成伤害
            if (DisableSpawnProtect(attacker, victim))
            {
                return true;

            }

            // 判断是否为“敌人”（使用你的自定义 IsEnemy）
            bool isEnemy = HitboxIdentityPatch.IsEnemy(attacker, victim);
            if (!isEnemy)
            {
                return true;
            }
            // 设置 IsFriendlyFire：只有非敌人（友军）才设为 true
            var isFriendlyFireProp = __instance.GetType()
                .GetProperty("IsFriendlyFire", BindingFlags.NonPublic | BindingFlags.Instance);
            isFriendlyFireProp?.SetValue(__instance, false, null);

            // 如果是友军（非敌人），直接归零伤害（禁用友伤）
            if(CheckFriendlyFirePlayerRules(__instance.Attacker,ply,out float ffMultiplier))
            {
                __instance.Damage *= ffMultiplier;
                return false;
            }

            // 是敌人 → 允许伤害，继续应用伤害修饰器（如护甲、减伤等）
            StatusEffectBase[] allEffects = victim.playerEffectsController.AllEffects;
            for (int i = 0; i < allEffects.Length; i++)
            {
                if (allEffects[i] is IDamageModifierEffect modEffect && modEffect.DamageModifierActive)
                {
                    __instance.Damage *= modEffect.GetDamageModifier(__instance.Damage, __instance, __instance.Hitbox);
                }
            }

            // 可选：强制启用“完全友伤”逻辑（虽然这里已是敌人，通常不需要）
            // 但如果你的某些系统依赖 ForceFullFriendlyFire，可以设为 true
            //var forceFfProp = __instance.GetType()
            //    .GetProperty("ForceFullFriendlyFire", BindingFlags.NonPublic | BindingFlags.Instance);
            //forceFfProp?.SetValue(__instance, true, null);

            // ✅ 完全处理完毕，跳过原版 ProcessDamage
            return false;
        }
    }
    [HarmonyPatch(typeof(Scp173BlinkTimer))]
    public static class Scp173BlinkTimerPatch
    {
        [HarmonyPatch("get_TotalCooldownServer")]
        [HarmonyPrefix]
        public static bool Prefix(Scp173BlinkTimer __instance, ref float __result)
        {
            if (Scp5k_Control.Is5kRound)
            {
                var role = __instance.Role as PlayerRoles.PlayableScps.Scp173.Scp173Role;
                role.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out var _breakneckSpeedsAbility);
                __result = (3 - Math.Max(ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy) * 0.1f, 1.5f)) * (_breakneckSpeedsAbility.IsActive ? 0.5f : 1f);
                return false;  // 跳过原始方法执行}
            }
            return true;
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
}
