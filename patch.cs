using AutoEvent.Events;
using CentralAuth;
using Cmdbinding;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CustomPlayerEffects;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Footprinting;
using GameCore;
using Google.Protobuf.WellKnownTypes;
using HarmonyLib;
using Hazards;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Armor;
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
using Next_generationSite_27.UnionP.Scp5k;
using NorthwoodLib.Pools;
using Org.BouncyCastle.Pkix;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.RoleAssign;
using PlayerRoles.Subroutines;
using PlayerRoles.Visibility;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using RelativePositioning;
using Respawning.Waves;
using Scp914;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UserSettings.ServerSpecific;
using Utils.Networking;
using VoiceChat;
using ZstdSharp;
using static HarmonyLib.AccessTools;
using static PlayerStatsSystem.DamageHandlerBase;
using Intercom = PlayerRoles.Voice.Intercom;
using IVoiceRole = PlayerRoles.Voice.IVoiceRole;
using Log = Exiled.API.Features.Log;
using Object = UnityEngine.Object;
using Type = System.Type;
namespace Next_generationSite_27.UnionP
{
    [HarmonyPatch(typeof(PlayerEvents))]
    public class PlayerEventsUpdatePatch
    {
        [HarmonyPatch("OnInspectedKeycard")]
        [HarmonyPrefix]
        public static bool Prefix(PlayerInspectedKeycardEventArgs ev)
        {
            Plugin.plugin.InspectedKeycard(ev); // 暂时性解决方案
            return true;
        }
    }
    [HarmonyPatch(typeof(ItemPickupBase))]
    public class ItemPickupBasePatch
    {
        [HarmonyPatch("DestroySelf")]
        [HarmonyPrefix]
        public static bool Prefix(ItemPickupBase __instance)
        {
            if(__instance == null)
            {
                return false;
            }
            if(__instance.GetInstanceID()== 0)
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(BanCommand))]
    public class BanCommandPatch
    {
        [HarmonyPatch("Execute")]
        [HarmonyPrefix]
        public static bool Prefix(ArraySegment<string> arguments, ICommandSender sender, ref string response, ref bool __result)
        {
            __result = new Next_generationSite_27.UnionP.PlayerManager.BanCommand().Execute(arguments, sender, out response);
            return false;
        }
    }
    [HarmonyPatch(typeof(BodyArmorUtils))]
    public class BodyArmorUtilsPatch
    {
        [HarmonyPatch("RemoveEverythingExceedingLimits")]
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch]
    public class DoorPermissionsPolicyPatch
    {
        private static readonly Type[] TargetMethodArgs = new Type[]
        {
        typeof(ReferenceHub),
        typeof(IDoorPermissionRequester),
        typeof(PermissionUsed).MakeByRefType()
        };

        private static MethodBase TargetMethod()
        {
            return typeof(DoorPermissionsPolicy)
                .GetMethod("CheckPermissions", TargetMethodArgs);
        }

        [HarmonyPostfix]
        public static void Postfix(
            ReferenceHub hub,
            IDoorPermissionRequester requester,
            out PermissionUsed callback,
            ref bool __result)
        {
            // 必须先赋值 out 参数
            callback = null;

            // 如果已有权限，不再检查
            if (__result)
                return;

            // 获取玩家
            var player = Player.Get(hub);
            if (player == null) return;

            // 检查玩家物品
            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (item is IDoorPermissionProvider dp)
                {
                    if (requester.PermissionsPolicy.CheckPermissions(dp, requester, out PermissionUsed tempCallback))
                    {
                        __result = true;
                        callback = tempCallback;
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility.ServerProcessCmd))]
    public class Scp049AttackPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = new List<CodeInstruction>(instructions);

            // 查找 ldc.r8 1.5 并替换为 1.1（如果启用 SSCP）
            for (int i = 0; i < newInstructions.Count; i++)
            {
                if (newInstructions[i].opcode == OpCodes.Ldc_R8 && (double)newInstructions[i].operand == 1.5)
                {
                    // 只有当 enableSSCP 为 true 时才修改冷却时间
                    newInstructions[i] = new CodeInstruction(OpCodes.Call, typeof(Scp049AttackPatch).GetMethod(nameof(GetCooldown), BindingFlags.Static | BindingFlags.NonPublic));
                    break;
                }
            }

            return newInstructions;
        }
        private static double GetCooldown()
        {
            return Plugin.enableSSCP ? 1.1 : 1.5;
        }
    }
    [HarmonyPatch(typeof(Scp173BlinkTimer))]
    public static class Scp173BlinkTimerPatch
    {
        [HarmonyPatch("get_TotalCooldownServer")]
        [HarmonyPrefix]
        public static bool Prefix(Scp173BlinkTimer __instance, ref float __result)
        {
            if (Plugin.enableSSCP)
            {
                var role = __instance.Role as PlayerRoles.PlayableScps.Scp173.Scp173Role;
                role.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out var _breakneckSpeedsAbility);
                __result = (3 - 0.6f) * (_breakneckSpeedsAbility.IsActive ? 0.5f : 1f);
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(NtfSpawnWave))]
    public static class NtfSpawnWavePatch
    {
        [HarmonyPatch("PopulateQueue")]
        [HarmonyPostfix]
        public static void Postfix(Queue<RoleTypeId> queueToFill, int playersToSpawn)
        {
            queueToFill.Enqueue(RoleTypeId.NtfCaptain);
        }
    }
    [HarmonyPatch(typeof(HitboxIdentity))]
    public static class HitboxIdentityPatch
    {
        [HarmonyPatch("IsDamageable", typeof(ReferenceHub), typeof(ReferenceHub))]
        [HarmonyPrefix]
        public static bool Prefix(ReferenceHub attacker, ReferenceHub victim, ref bool __result)
        {
            if (Plugin.CurrentFFManager != null)
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
            if (Plugin.CurrentFFManager != null)
            {
                __result = IsEnemy(attacker, victim);  // 跳过原始方法执行}
                return false;
            }
            return true;
        }

        public static bool IsEnemy(ReferenceHub attacker, ReferenceHub victim)
        {
            if (Plugin.CurrentFFManager == null)
                return IsEnemy(attacker.GetTeam(), victim.GetTeam());
            if (attacker == Server.Host.ReferenceHub)
                return true;
            if ((victim.isServer || victim == Server.Host.ReferenceHub) && !victim.IsDummy)
            {
                //ffMultiplier = -1f;
                return false;
            }
            // 获取攻击者和受害者的 Player 对象
            var a = Player.Get(attacker);
            var v = Player.Get(victim);

            // 安全检查：如果玩家对象不存在，返回 false（无法造成伤害）
            if (a == null || v == null)
                return true;
            // Always allow damage from Server.Host

            return Plugin.CurrentFFManager.IsDamaging(a, v);
        }
        public static bool IsEnemy(Team attackerTeam, Team victimTeam)
        {
            return attackerTeam != Team.Dead && victimTeam != Team.Dead && (attackerTeam != Team.SCPs || victimTeam != Team.SCPs) && attackerTeam.GetFaction() != victimTeam.GetFaction();
        }
    }
    [HarmonyPatch(typeof(AttackerDamageHandler))]
    public static class AttackerDamageHandlerPatch
    {
        [HarmonyPatch("get_IgnoreFriendlyFireDetector")]
        [HarmonyPrefix]
        public static bool get_IgnoreFriendlyFireDetectorPrefix(ref bool __result)
        {
            __result = true;
            return false;
        }
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
            if ((victimHub.isServer || victimHub == Server.Host.ReferenceHub) && !victimHub.IsDummy)
            {
                ffMultiplier = -1f;
                return false;
            }
            if (Plugin.CurrentFFManager != null)
            {

                try
                {
                    Player attacker = Player.Get(attackerFootprint.Hub);
                    Player victim = Player.Get(victimHub);
                    var FF = Plugin.CurrentFFManager.GetFF(attacker, victim);
                    Log.Info($"FF between {attacker.Nickname} and {victim.Nickname} is {FF}");
                    if (FF != -1)
                    {
                        ffMultiplier = FF;
                        return FF >= 0f;
                    }
                }
                catch (Exception ex)
                {
                    //Log.Error($"CheckFriendlyFirePlayerRules failed to handle friendly fire because: {ex}");
                }
            }
            // Default to NW logic
            return HitboxIdentityPatch.IsEnemy(attackerFootprint.Hub, victimHub);
        }

        [HarmonyPatch("ProcessDamage")]
        [HarmonyPrefix]
        public static bool ProcessDamagePrefix(AttackerDamageHandler __instance, ReferenceHub ply)
        {
            // 仅在 5k 轮次生效
            if (Plugin.CurrentFFManager == null)
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
            if (CheckFriendlyFirePlayerRules(__instance.Attacker, ply, out float ffMultiplier))
            {
                //Log.Info($"ffMultiplier:{ffMultiplier}");
                //Log.Info($"__instance.Damage:{__instance.Damage}");
                __instance.Damage *= ffMultiplier;
                //Log.Info($"__instance.Damage *  f:{__instance.Damage}");
                return false;
            }
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
            //    .GetProperty("ForceFullFriendlyFire", BindingFlags.NonPublic | BindingFlags.nav);
            //forceFfProp?.SetValue(__instance, true, null);

            // ✅ 完全处理完毕，跳过原版 ProcessDamage
            return false;
        }
    }
    [HarmonyPatch(typeof(FpcStateProcessor))]
    public static class FpcStateProcessorPatch
    {
        [HarmonyPatch("get_ServerUseRate")]
        [HarmonyPrefix]
        public static bool Prefix(FpcStateProcessor __instance, ref float __result)
        {

            var f1 = typeof(FpcStateProcessor)
                .GetProperty("Hub", BindingFlags.NonPublic | BindingFlags.Instance);
            ReferenceHub h = (ReferenceHub)f1.GetValue(__instance);
            if ( h != null)
            {
                if (h.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp939)
                {
                    return true;
                }
                if (h.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp106)
                {
                    return true;
                }
            }
            __result = 0;
                return false;  // 跳过原始方法执行}
        }
    }
    //[HarmonyPatch(typeof(FirstPersonMovementModule))]
    //public static class FirstPersonMovementModulePatch
    //{
    //    private static readonly Type ExcludedType = typeof(MovementSpeedModule);

    //    [HarmonyPatch("VelocityForState")]
    //    [HarmonyPrefix]
    //    public static bool Prefix(FirstPersonMovementModule __instance, PlayerMovementState state, bool applyCrouch, ref float __result)
    //    {
    //        try
    //        {
    //            // --- Null Checks ---
    //            if (__instance == null)
    //            {
    //                 Log.Warn("Prefix: __instance is null");
    //                return true; // Let the original method handle it or prevent errors
    //            }
    //            ReferenceHub hub = __instance.Motor.Hub;
    //            if (hub == null)
    //            {
    //                 Log.Warn("Prefix: hub is null2");
    //                // Cannot proceed without hub, let original method handle it or return default
    //                // Returning true calls the original method.
    //                return true;
    //            }

    //            if (hub.inventory == null)
    //            {
    //                 Log.Warn("Prefix: hub.inventory is null");
    //                return true;
    //            }

    //            if (hub.playerEffectsController == null)
    //            {
    //                 Log.Warn("Prefix: hub.playerEffectsController is null");
    //                return true;
    //            }
    //            // --- End Null Checks ---

    //            float num = 0f;

    //            switch (state)
    //            {
    //                case PlayerMovementState.Crouching:
    //                    num = __instance.CrouchSpeed;
    //                    break;
    //                case PlayerMovementState.Sneaking:
    //                    num = __instance.SneakSpeed;
    //                    break;
    //                case PlayerMovementState.Walking:
    //                    num = __instance.WalkSpeed;
    //                    break;
    //                case PlayerMovementState.Sprinting:
    //                    num = __instance.SprintSpeed;
    //                    break;
    //            }

    //            if (applyCrouch)
    //            {
    //                // Ensure StateProcessor is not null if CrouchPercent is accessed
    //                if (__instance.StateProcessor != null)
    //                {
    //                    num = Mathf.Lerp(num, __instance.CrouchSpeed, __instance.StateProcessor.CrouchPercent);
    //                }
    //                else
    //                {
    //                     Log.Warn("Prefix: __instance.StateProcessor is null");
    //                    // Handle or use default CrouchPercent logic if needed
    //                }

    //            }

    //            num *= hub.inventory.MovementSpeedMultiplier;
    //            float num2 = hub.inventory.MovementSpeedLimit;

    //            // Check EffectsLength for potential issues, though loop handles i < length
    //            // if (hub.playerEffectsController.EffectsLength < 0) { ... }

    //            for (int i = 0; i < hub.playerEffectsController.EffectsLength; i++)
    //            {
    //                var effect = hub.playerEffectsController.AllEffects[i]; // Get the effect first
    //                if (effect == null) continue; // Safety check for the effect object itself

    //                IMovementSpeedModifier movementSpeedModifier = effect as IMovementSpeedModifier;

    //                // --- Key Fix: Check for null BEFORE accessing members ---
    //                if (movementSpeedModifier == null)
    //                {
    //                    // Not an IMovementSpeedModifier, skip
    //                    continue;
    //                }

    //                // Now it's safe to check the type
    //                //Log.Info(movementSpeedModifier.GetType());

    //                if (movementSpeedModifier.GetType() == ExcludedType)
    //                {
    //                    Log.Info(213);
    //                    continue; // Skip this specific modifier type
    //                }

    //                // Now check if the modifier is active
    //                if (movementSpeedModifier.MovementModifierActive) // This was previously causing the error if modifier was null
    //                {
    //                    // Apply the modifier's constraints and multipliers
    //                    num2 = Mathf.Min(num2, movementSpeedModifier.MovementSpeedLimit);
    //                    num *= movementSpeedModifier.MovementSpeedMultiplier;
    //                }
    //                // --- End Key Fix ---
    //            }

    //            __result = Mathf.Min(num, num2);
    //            return false; // Skip the original method
    //        }
    //        catch (Exception e)
    //        {
    //            // Log.Warn should ideally be replaced with your game's/mod's logging system
    //             Log.Warn($"[FirstPersonMovementModulePatch] Exception in Prefix: {e.Message}");
    //             Log.Warn($"[FirstPersonMovementModulePatch] Exception in Prefix: {e.Source}");
    //             Log.Warn($"[FirstPersonMovementModulePatch] Exception in Prefix: {e.ToString()}");
    //            // Log.Warn(e.StackTrace);
    //            // instance case of an unexpected error in the patch, it's often safer to let the original method run
    //            return true;
    //        }
    //    }

    //}
    //[HarmonyPatch(typeof(AutosyncModifiersCombiner))]
    //static class AutosyncModifiersCombinerPatch
    //{
    //    private static float MinValue<T>(T[] arr, float startMin, Func<T, float> selector, Func<T, bool> validator = null)
    //    {
    //        float num = startMin;
    //        foreach (T arg in arr)
    //        {
    //            if (validator == null || validator(arg))
    //            {
    //                num = Mathf.Min(num, selector(arg));
    //            }
    //        }
    //        return num;
    //    }
    //    private static float CombineMultiplier<T>(T[] arr, Func<T, float> selector, Func<T, bool> validator = null)
    //    {
    //        float num = 1f;
    //        foreach (T arg in arr)
    //        {
    //            if (validator == null || validator(arg))
    //            {
    //                num *= selector(arg);
    //            }
    //        }
    //        return num;
    //    }
    //    private static readonly Type ExcludedType = typeof(MovementSpeedModule);
    //    [HarmonyPrefix]
    //    [HarmonyPatch("get_MovementSpeedLimit")]

    //    public static bool Prefix(AutosyncModifiersCombiner __instance, ref float __result)
    //    {
    //        try
    //        {
    //            // 使用 Traverse 获取私有字段 _movementSpeedModifiers
    //            var modifiers = (IMovementSpeedModifier[])hubField.GetValue(__instance);

    //            if (hubField != null)
    //            {
    //                __result = MinValue<IMovementSpeedModifier>(modifiers, float.MaxValue, (IMovementSpeedModifier x) =>
    //                {
    //                    if (x.GetType() == ExcludedType)
    //                    {
    //                        Log.Debug(x.MovementSpeedLimit);
    //                        return 114514;
    //                    }
    //                    return x.MovementSpeedLimit;
    //                }, (IMovementSpeedModifier x) => x.MovementModifierActive);
    //                //Log.Debug(__result);
    //                return false;
    //            } else
    //            {
    //                return true;
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            // 记录异常（可选）
    //            Log.Warn("Failed to filter movement modifiers: " + e);
    //            return true;
    //        }
    //    }
    //    static readonly System.Reflection.FieldInfo hubField =
    //        AccessTools.Field(typeof(AutosyncModifiersCombiner), "_movementSpeedModifiers");
    //    [HarmonyPrefix]
    //    [HarmonyPatch("get_MovementSpeedMultiplier")]

    //    public static bool MPrefix(AutosyncModifiersCombiner __instance, ref float __result)
    //    {
    //        try
    //        {
    //            var modifiers = (IMovementSpeedModifier[])hubField.GetValue(__instance);
    //            __result = CombineMultiplier<IMovementSpeedModifier>(modifiers, (IMovementSpeedModifier x) => {
    //                if (x.GetType() == ExcludedType)

    //                {
    //                    return 1;
    //                }
    //                return x.MovementSpeedMultiplier;
    //            }, (IMovementSpeedModifier x) => x.MovementModifierActive);
    //            //Log.Debug(__result);
    //            return false;
    //        }
    //        catch (Exception e)
    //        {
    //            // 记录异常（可选）
    //            Log.Warn("Failed to filter movement modifiers: " + e);
    //            return true;
    //        }
    //    }
    //}
    [HarmonyPatch(typeof(InventorySystem.Items.Usables.Scp207))]
    public static class Scp207Patch
    {
        [HarmonyPatch("OnEffectsActivated")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                // 查找 "ldc.i4.4" 指令（加载整数 4）
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    // 替换为 "ldc.i4 255"
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)127);
                    break; // 只改第一个匹配（通常就是上限判断）
                }
            }

            return codes;
        }
    }
    [HarmonyPatch(typeof(Scp914Upgrader))]
    public static class S914Patch
    {
        [HarmonyPatch("ProcessPlayer")]
        [HarmonyPrefix]
        public static bool PlPrefix(ReferenceHub ply, bool upgradeInventory, bool heldOnly, Scp914KnobSetting setting)
        {
            Plugin.plugin.eventhandle.OnUpgradingInventoryItem(ply);

            return true;
        }
        [HarmonyPatch("ProcessPickup")]
        [HarmonyPrefix]
        public static bool PrPrefix(ref ItemPickupBase pickup, bool upgradeDropped, Scp914KnobSetting setting)
        {
            var p = Plugin.plugin.eventhandle.OnUpgradingPickup(pickup);
            if (p != null) {
                pickup = p.Base;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(ReferenceHub))]
    public static class MReferenceHubPatch
    {
        [HarmonyPatch("GetPlayerCount", new Type[] {
        typeof(ClientInstanceMode),
        typeof(ClientInstanceMode),
        typeof(ClientInstanceMode)
    })]
        [HarmonyPrefix]
        public static bool Prefix(
            ClientInstanceMode allowedState,
            ClientInstanceMode allowedState2,
            ClientInstanceMode allowedState3,
            ref int __result)
        {
            int num = 0;
            foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
            {
                if (allowedState == referenceHub.Mode ||
                    allowedState2 == referenceHub.Mode ||
                    allowedState3 == referenceHub.Mode)
                {
                    num++;
                }
            }

            __result = num - Plugin.plugin.eventhandle.SPD.Count;
            return false;
        }
    }
    [HarmonyPatch(typeof(CharacterClassManager))]
    public static class CharacterClassManagerPatch
    {
        [HarmonyPatch("ForceRoundStart")]
        [HarmonyPrefix]
        public static bool Prefix()
        {
            Plugin.plugin.eventhandle.assing();
            return true;
        }
    }
    [HarmonyPatch(typeof(DisruptorHitregModule))]
    public static class DisruptorHitregModulePatch
    {
        [HarmonyPatch("TemplateSimulateShot")]
        [HarmonyPrefix]
        public static bool Prefix(DisruptorShotEvent data, BarrelTipExtension barrelTip)
        {
            Plugin.plugin.eventhandle.TemplateSimulateShot(data, barrelTip);
            return true;
        }
    }
    //[HarmonyPatch(typeof(Scp096AudioPlayer))]
    //public static class Scp096AudioPlayerPatch
    //{
    //    [HarmonyPatch("SetAudioState")]
    //    [HarmonyPrefix]
    //    public static bool Prefix(Scp096AudioPlayer __instance)
    //    {
    //        if (!Round.IsStarted)
    //        {
    //            if (Plugin.plugin.eventhandle.SPD.Count((x) => x.PlayerId == __instance.Owner.PlayerId) >= 1)
    //            {
    //                return false;
    //            }
    //        }
    //        return true;
    //    }
    //}
    //[HarmonyPatch(typeof(Scp173AudioPlayer))]
    //public static class Scp173AudioPlayerPatch
    //{
    //    [HarmonyPatch("SetAudioState")]
    //    [HarmonyPrefix]
    //    public static bool Prefix(Scp173AudioPlayer __instance)
    //    {
    //        if (!Round.IsStarted)
    //        {
    //            if (__instance.Role.TryGetOwner(out var owner))
    //            {
    //                if (Plugin.plugin.eventhandle.SPD.Count((x) => x.PlayerId == owner.PlayerId) >= 1)
    //                {
    //                    return false;
    //                }
    //            }
    //        }
    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(Exiled.Events.EventArgs.Player.ChangingNicknameEventArgs))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Exiled.API.Features.Player), typeof(string) })]
    public static class PatchChangingNicknameCtor
    {
        static bool Prefix(ref Exiled.API.Features.Player player, ref string newName)
        {
            if (player == null)
            {
                return false; // 跳过构造函数
            }

            return true; // 继续构造
        }
    }
    [HarmonyPatch(typeof(EmergencyDoorRelease))]
    public static class EmergencyDoorReleasePatch
    {
        [HarmonyPatch("ServerInteract")]
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(FpcServerPositionDistributor))]
    public static class FpcServerPositionDistributorPatch
    {
        [HarmonyPatch("GetVisibleRole")]
        [HarmonyPrefix]
        public static bool Prefix(ReferenceHub receiver, ReferenceHub target,ref RoleTypeId __result)
        {
            RoleTypeId result = target.GetRoleId();
            if (target.isLocalPlayer || receiver.isLocalPlayer)
            {
                __result = result;
                return false;
            }
            IObfuscatedRole obfuscatedRole = target.roleManager.CurrentRole as IObfuscatedRole;
            if (obfuscatedRole != null)
            {
                result = obfuscatedRole.GetRoleForUser(receiver);
            }
            if (receiver == target)
            {
                __result = result;
                return false;
            }
            bool visable = false;
            ICustomVisibilityRole customVisibilityRole = receiver.roleManager.CurrentRole as ICustomVisibilityRole;
            if (customVisibilityRole != null)
            {
                visable = customVisibilityRole.VisibilityController.ValidateVisibility(target);
            }
            bool perm = PermissionsHandler.IsPermitted(receiver.serverRoles.Permissions, PlayerPermissions.GameplayData);
            bool IsSpec = receiver.GetRoleId() == RoleTypeId.Spectator;
            if (target.GetTeam() == Team.SCPs)
            {
                __result = result;
                return false;
            }
            if (IsCommunicatingGlobally(target))
            {
                __result = result;
                return false;
            }
            if (visable && perm && !IsSpec)
            {
            } else
            {
                result = RoleTypeId.Spectator;
            }
            __result = result;
            return false;
        }
        private static bool IsCommunicatingGlobally(ReferenceHub hub)
        {
            return true;
        }
    }

    //    [HarmonyPatch(typeof(RoleAssigner))]
    //public static class RoleAssignerPatch
    //{

    //    //[HarmonyPatch("CheckPlayer")]
    //    //[HarmonyPrefix]
    //    //public static bool CheckPlayerPrefix(ReferenceHub hub,ref bool __result)
    //    //{
    //    //    if (Exiled.API.Features.Round.IsStarted) { return true; }
    //    //    //Log.Info(Plugin.plugin.eventhandle.SPD.Count);
    //    //    //foreach (var item in ReferenceHub.AllHubs)
    //    //    //{
    //    //        if (Plugin.plugin.eventhandle.SPD.Contains(hub))
    //    //        {
    //    //            //if (item == hub)
    //    //            {
    //    //                __result = false;
    //    //                //Plugin.plugin.eventhandle.SPD.Remove(item);
    //    //                //NetworkServer.Destroy(item.gameObject);
    //    //                return false;
    //    //            }
    //    //        }
    //    //    //}

    //    //    return true;
    //    //}

    //}
}
