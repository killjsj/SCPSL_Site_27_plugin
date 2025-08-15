using AutoEvent.Events;
using CentralAuth;
using Cmdbinding;
using CommandSystem.Commands.RemoteAdmin;
using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using GameCore;
using HarmonyLib;
using Hazards;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
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
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.RoleAssign;
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
using Log = Exiled.API.Features.Log;
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
    [HarmonyPatch(typeof(AutosyncModifiersCombiner))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(ModularAutosyncItem) })]
    static class AutosyncModifiersCombinerPatch
    {
        private static readonly Type ExcludedType = typeof(MovementSpeedModule);
        [HarmonyPostfix]
        static void Postfix(AutosyncModifiersCombiner __instance)
        {
            try
            {
                // 使用 Traverse 获取私有字段 _movementSpeedModifiers
                var field = Traverse.Create(__instance).Field("_movementSpeedModifiers");
                var modifiers = field.GetValue<IMovementSpeedModifier[]>();

                if (modifiers == null || modifiers.Length == 0) return;

                // 过滤掉 MovementSpeedModule
                var filtered = Array.FindAll(modifiers, m => m.GetType() != ExcludedType);

                // 写回字段
                field.SetValue(filtered);
            }
            catch (Exception e)
            {
                // 记录异常（可选）
                // Plugin.Instance.Log.Warn("Failed to filter movement modifiers: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(MicroHIDItem))]
    public static class MicroHIDItemPatch
    {
        [HarmonyPatch("get_Weight")]
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            __result = 0;
            return false;
        }
    }
    [HarmonyPatch(typeof(MovementSpeedModule))]
    public static class MovementSpeedModulePatch
    {
        static readonly System.Reflection.FieldInfo hubField =
            AccessTools.Field(typeof(MovementSpeedModule), "_movementSpeedLimit");
        [HarmonyPatch("get_MovementSpeedLimit")]
        [HarmonyPostfix]
        public static void HarmonyPostfix(MovementSpeedModule __instance, ref float __result)
        {
            hubField.SetValue(__instance, 255);
            __result = 255;
        }
        [HarmonyPatch("get_MovementSpeedLimit")]
        [HarmonyPrefix]
        public static bool HarmonyPrefix(MovementSpeedModule __instance, ref float __result)
        {
            __result = 255;
            return false;
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

    [HarmonyPatch(typeof(NicknameSync), nameof(NicknameSync.Network_displayName), MethodType.Setter)]
    public static class Patch_Network_displayName
    {
        static readonly System.Reflection.FieldInfo hubField =
            AccessTools.Field(typeof(NicknameSync), "_hub"); // 获取私有字段 _hub

        static bool Prefix(NicknameSync __instance, string value)
        {
            // 通过反射读取 _hub
            var hub = hubField.GetValue(__instance);
            if (hub == null)
            {
                Debug.LogWarning("[Patch] NicknameSync._hub 为 null，跳过 DisplayName 设置");
                return false; // 跳过原方法
            }

            // 这里可以用 hub 初始化你的 PlayerData
            var player = Exiled.API.Features.Player.Get((ReferenceHub)hub);
            if (player == null)
            {
                Debug.LogWarning($"[Patch] 无法通过 hub 获取 Player，name={value}");
                return false; // 跳过原方法
            }

            return true; // 继续原方法
        }
    }
    [HarmonyPatch(typeof(Exiled.Events.EventArgs.Player.ChangingNicknameEventArgs))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Exiled.API.Features.Player), typeof(string) })]
    public static class PatchChangingNicknameCtor
    {
        static bool Prefix(ref Exiled.API.Features.Player player, ref string newName)
        {
            if (player == null)
            {
                UnityEngine.Debug.LogError("[Patch] ChangingNicknameEventArgs 构造时 Player 为 null，跳过构造函数");
                return false; // 跳过构造函数
            }

            return true; // 继续构造
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

    [HarmonyPatch(typeof(ScpPlayerPicker))]
    public class ScpPlayerPickerPatch
    {
        private static FieldInfo scpsToSpawnField = typeof(ScpPlayerPicker).GetField("ScpsToSpawn", BindingFlags.NonPublic | BindingFlags.Static);

        [HarmonyPatch("GenerateList")]
        [HarmonyPrefix]
        public static bool Prefix(ScpPlayerPickerPatch __instance, ScpTicketsLoader loader, int scpsToAssign)
        {
            var ScpsToSpawn = (List<ReferenceHub>)scpsToSpawnField.GetValue(null);
                ScpsToSpawn.Clear();
            //    Log.Info("start");
            //    // 如果不需要分配SCP，直接返回
            //    if (scpsToAssign <= 0)
            //    {

            //        scpsToSpawnField.SetValue(null, ScpsToSpawn);

            //        return false;
            //    }
            //    List<ReferenceHub> TargetPlayer = new List<ReferenceHub>();
            //    TargetPlayer = Plugin.plugin.eventhandle.targetRole[RoleTypeId.Scp939];
            //    if (TargetPlayer.Count <= 0)
            //    {
            //        TargetPlayer = ReferenceHub.AllHubs.ToList();
            //    }
            //    else if (TargetPlayer.Count < scpsToAssign)
            //    {
            //        int reqPlayer = TargetPlayer.Count - scpsToAssign;
            //        foreach (ReferenceHub h in ReferenceHub.AllHubs)
            //        {
            //            if (!TargetPlayer.Contains(h) && RoleAssigner.CheckPlayer(h) && !ScpPlayerPicker.IsOptedOutOfScp(h))
            //            {
            //                TargetPlayer.Add(h);
            //                reqPlayer--;
            //            }
            //            if (reqPlayer <= 0)
            //            {
            //                break;
            //            }
            //        }
            //    }
            //    Log.Info("p1 start");

            //    // 第一阶段：选择票数最高的玩家
            //    int highestTickets = 0; // 记录当前最高票数
            //    foreach (ReferenceHub referenceHub in TargetPlayer)
            //    {
            //        // 检查玩家是否有效
            //        if (RoleAssigner.CheckPlayer(referenceHub))
            //        {
            //            // 获取玩家票数，默认10票
            //            int tickets = loader.GetTickets(referenceHub, 10, false);

            //            // 如果当前玩家票数 >= 最高票数
            //            if (tickets >= highestTickets)
            //            {
            //                // 如果当前玩家票数更高，清空之前的候选列表
            //                if (tickets > highestTickets)
            //                {
            //                    ScpsToSpawn.Clear();
            //                }

            //                // 更新最高票数并添加到候选列表
            //                highestTickets = tickets;
            //                ScpsToSpawn.Add(referenceHub);
            //            }
            //        }
            //    }

            //    // 如果有多个最高票数的玩家，随机选择一个
            //    if (ScpsToSpawn.Count > 1)
            //    {
            //        ReferenceHub selectedItem = ScpsToSpawn.RandomItem<ReferenceHub>();
            //        ScpsToSpawn.Clear();
            //        ScpsToSpawn.Add(selectedItem);
            //    }

            //    // 减去已分配的SCP数量
            //    scpsToAssign -= ScpsToSpawn.Count;

            //    // 如果已经满足需求，直接返回
            //    if (scpsToAssign <= 0)
            //    {
            //        scpsToSpawnField.SetValue(null, ScpsToSpawn);

            //        return false;
            //    }
            //    Log.Info("p2 start");

            //    // 第二阶段：使用加权随机选择剩余SCP玩家
            //    // 从对象池获取潜在SCP列表，提高性能
            //    List<ScpPlayerPicker.PotentialScp> potentialScps = NorthwoodLib.Pools.ListPool<ScpPlayerPicker.PotentialScp>.Shared.Rent();
            //    long totalWeight = 0L; // 总权重

            //    // 遍历所有玩家，为未被选中的有效玩家计算权重
            //    using (List<ReferenceHub>.Enumerator enumerator = TargetPlayer.GetEnumerator())
            //    {
            //        while (enumerator.MoveNext())
            //        {
            //            ReferenceHub player = enumerator.Current;

            //            // 排除已选中的玩家和无效玩家
            //            if (!ScpsToSpawn.Contains(player) &&
            //                RoleAssigner.CheckPlayer(player))
            //            {
            //                long weight = 1L; // 初始化权重
            //                int tickets = loader.GetTickets(player, 10, false);

            //                // 权重计算：票数的scpsToAssign次方
            //                // 这样票数高的玩家权重会显著增加
            //                for (int i = 0; i < scpsToAssign; i++)
            //                {
            //                    weight *= (long)tickets;
            //                }

            //                // 添加到潜在SCP列表
            //                potentialScps.Add(new ScpPlayerPicker.PotentialScp
            //                {
            //                    Player = player,
            //                    Weight = weight
            //                });

            //                // 累加总权重
            //                totalWeight += weight;
            //            }
            //        }
            //        Log.Info("p3 start");

            //        // 开始加权随机选择循环
            //        goto Checker;
            //    }

            //// 加权随机选择循环标签
            //SelectionLoop:
            //    // 生成0到totalWeight之间的随机数
            //    double randomValue = (double)UnityEngine.Random.value * (double)totalWeight;

            //    // 遍历潜在SCP列表进行选择
            //    for (int j = 0; j < potentialScps.Count; j++)
            //    {
            //        ScpPlayerPicker.PotentialScp potentialScp = potentialScps[j];

            //        // 从随机值中减去当前玩家权重
            //        randomValue -= (double)potentialScp.Weight;

            //        // 如果随机值 <= 0，说明选中了这个玩家
            //        if (randomValue <= 0.0)
            //        {
            //            // 减少待分配数量
            //            scpsToAssign--;
            //            // 添加到SCP候选列表
            //            ScpsToSpawn.Add(potentialScp.Player);
            //            // 从潜在列表中移除
            //            potentialScps.RemoveAt(j);
            //            // 从总权重中减去该玩家权重
            //            totalWeight -= potentialScp.Weight;
            //            break;
            //        }
            //    }
            //Checker:
            //    // 如果还有剩余名额，继续选择
            //    if (scpsToAssign <= 0)
            //    {
            //        // 归还对象池
            //        NorthwoodLib.Pools.ListPool<ScpPlayerPicker.PotentialScp>.Shared.Return(potentialScps);
            //        scpsToSpawnField.SetValue(null, ScpsToSpawn);
            //        Log.Info("end");

            //        return false;
            //    }

            //    // 继续选择循环
            //    goto SelectionLoop;
            return true;
        }
    }
}
