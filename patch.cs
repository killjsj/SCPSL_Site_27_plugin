using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.API.Features.Roles;
using HarmonyLib;
using Hazards;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using MapGeneration.Holidays;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using static HarmonyLib.AccessTools;
using static PlayerStatsSystem.DamageHandlerBase;
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
                    if (Next_generationSite_27.UnionP.Plugin.enableSSCP)
                    {
                        newInstructions[i].operand = 1.1;
                        Log.Info("[SCP-049 Attack Patch] 冷却时间已设置为 1.1 秒");
                    }
                    else
                    {
                        newInstructions[i].operand = 1.5;
                        Log.Info("[SCP-049 Attack Patch] 保持默认冷却时间 1.5 秒");
                    }
                    break;
                }
            }

            return newInstructions;
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
                __result = (3 - 0.4f) * (_breakneckSpeedsAbility.IsActive ? 0.5f : 1f); ; // 直接设置返回值为 0
                return false;  // 跳过原始方法执行}
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MovementSpeedModule))]
    public static class MovementSpeedModulePatch
    {
        [HarmonyPatch("get_MovementModifierActive")]
        [HarmonyPrefix]
        public static bool Prefix(MovementSpeedModule __instance, ref bool __result)
        {
            __result = false;
            return false;
        }
    
    }
    [HarmonyPatch(typeof(Scp173TantrumAbility), nameof(Scp173TantrumAbility.ServerProcessCmd))]
    public class Scp173TantrumPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = new List<CodeInstruction>(instructions);
            if (Plugin.enableSSCP)
            {
                try
                {
                    // 查找并移除观察者检查逻辑
                    // 查找第一个条件检查（RemainingSustainPercent > 0）
                    for (int i = 0; i < newInstructions.Count - 10; i++)
                    {
                        // 查找 ldarg.0 后面跟着 ldfld _blinkTimer 的指令
                        if (newInstructions[i].opcode == OpCodes.Ldarg_0 &&
                            i + 1 < newInstructions.Count &&
                            newInstructions[i + 1].opcode == OpCodes.Ldfld &&
                            newInstructions[i + 1].operand.ToString().Contains("_blinkTimer"))
                        {
                            // 查找对应的 ret 指令（条件跳转的目标）
                            for (int j = i + 5; j < newInstructions.Count; j++)
                            {
                                if (newInstructions[j].opcode == OpCodes.Ret)
                                {
                                    // 移除从检查开始到 ret 之间的所有指令
                                    int removeCount = j - i + 1;
                                    newInstructions.RemoveRange(i, removeCount);
                                    Log.Info("[SCP-173 Tantrum Patch] 成功移除观察者检查逻辑");
                                    return newInstructions;
                                }
                            }
                        }
                    }

                    Log.Warn("[SCP-173 Tantrum Patch] 未找到观察者检查逻辑");
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[SCP-173 Tantrum Patch] Transpiler 出错: {ex.Message}");
                }
            }
            return newInstructions;
        }
    }
}
