using Cmdbinding;
using CommandSystem.Commands.RemoteAdmin;
using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.API.Features.Roles;
using GameCore;
using HarmonyLib;
using Hazards;
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
using PlayerRoles.PlayableScps.Scp049;
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
    [HarmonyPatch(typeof(MovementSpeedModule))]
    public static class MovementSpeedModulePatch
    {
        [HarmonyPatch("get_MovementSpeedLimit")]
        [HarmonyPrefix]
        public static bool Prefix(MovementSpeedModule __instance, ref float __result)
        {
            __result = 99999;
            return false;
        }

    }
    //[HarmonyPatch(typeof(ScpPlayerPicker))]
    //public class ScpPlayerPickerPatch
    //{
    //    private static FieldInfo scpsToSpawnField = typeof(ScpPlayerPicker).GetField("ScpsToSpawn", BindingFlags.NonPublic | BindingFlags.Static);

    //    [HarmonyPatch("GenerateList")]
    //    [HarmonyPrefix]
    //    public static bool Prefix(ScpPlayerPickerPatch __instance, ScpTicketsLoader loader, int scpsToAssign)
    //    {
    //        var ScpsToSpawn = (List<ReferenceHub>)scpsToSpawnField.GetValue(null);
    //        ScpsToSpawn.Clear();

    //        // 如果不需要分配SCP，直接返回
    //        if (scpsToAssign <= 0)
    //        {

    //            scpsToSpawnField.SetValue(null, ScpsToSpawn);

    //            return false;
    //        }
    //        List<ReferenceHub> TargetPlayer = new List<ReferenceHub>();
    //        TargetPlayer = Plugin.ScpPlayer;
    //        if (TargetPlayer.Count <= 0)
    //        {
    //            TargetPlayer = ReferenceHub.AllHubs.ToList();
    //        }
    //        else if (TargetPlayer.Count < scpsToAssign) {
    //            int reqPlayer = TargetPlayer.Count - scpsToAssign;
    //            foreach (ReferenceHub h in ReferenceHub.AllHubs) {
    //                if (!TargetPlayer.Contains(h) && RoleAssigner.CheckPlayer(h) && !ScpPlayerPicker.IsOptedOutOfScp(h)) { 
    //                    TargetPlayer.Add(h);
    //                    reqPlayer--;
    //                }
    //                if (reqPlayer <= 0) {
    //                    break;
    //                }
    //            }
    //        }
    //        // 第一阶段：选择票数最高的玩家
    //        int highestTickets = 0; // 记录当前最高票数
    //        foreach (ReferenceHub referenceHub in TargetPlayer)
    //        {
    //            // 检查玩家是否有效
    //            if (RoleAssigner.CheckPlayer(referenceHub))
    //            {
    //                // 获取玩家票数，默认10票
    //                int tickets = loader.GetTickets(referenceHub, 10, false);

    //                // 如果当前玩家票数 >= 最高票数
    //                if (tickets >= highestTickets)
    //                {
    //                    // 如果当前玩家票数更高，清空之前的候选列表
    //                    if (tickets > highestTickets)
    //                    {
    //                        ScpsToSpawn.Clear();
    //                    }

    //                    // 更新最高票数并添加到候选列表
    //                    highestTickets = tickets;
    //                    ScpsToSpawn.Add(referenceHub);
    //                }
    //            }
    //        }

    //        // 如果有多个最高票数的玩家，随机选择一个
    //        if (ScpsToSpawn.Count > 1)
    //        {
    //            ReferenceHub selectedItem = ScpsToSpawn.RandomItem<ReferenceHub>();
    //            ScpsToSpawn.Clear();
    //            ScpsToSpawn.Add(selectedItem);
    //        }

    //        // 减去已分配的SCP数量
    //        scpsToAssign -= ScpsToSpawn.Count;

    //        // 如果已经满足需求，直接返回
    //        if (scpsToAssign <= 0)
    //        {
    //            scpsToSpawnField.SetValue(null, ScpsToSpawn);

    //            return false;
    //        }

    //        // 第二阶段：使用加权随机选择剩余SCP玩家
    //        // 从对象池获取潜在SCP列表，提高性能
    //        List<ScpPlayerPicker.PotentialScp> potentialScps = NorthwoodLib.Pools.ListPool<ScpPlayerPicker.PotentialScp>.Shared.Rent();
    //        long totalWeight = 0L; // 总权重

    //        // 遍历所有玩家，为未被选中的有效玩家计算权重
    //        using (List<ReferenceHub>.Enumerator enumerator = TargetPlayer.GetEnumerator())
    //        {
    //            while (enumerator.MoveNext())
    //            {
    //                ReferenceHub player = enumerator.Current;

    //                // 排除已选中的玩家和无效玩家
    //                if (!ScpsToSpawn.Contains(player) &&
    //                    RoleAssigner.CheckPlayer(player))
    //                {
    //                    long weight = 1L; // 初始化权重
    //                    int tickets = loader.GetTickets(player, 10, false);

    //                    // 权重计算：票数的scpsToAssign次方
    //                    // 这样票数高的玩家权重会显著增加
    //                    for (int i = 0; i < scpsToAssign; i++)
    //                    {
    //                        weight *= (long)tickets;
    //                    }

    //                    // 添加到潜在SCP列表
    //                    potentialScps.Add(new ScpPlayerPicker.PotentialScp
    //                    {
    //                        Player = player,
    //                        Weight = weight
    //                    });

    //                    // 累加总权重
    //                    totalWeight += weight;
    //                }
    //            }

    //            // 开始加权随机选择循环
    //            goto Checker;
    //        }

    //    // 加权随机选择循环标签
    //    SelectionLoop:
    //        // 生成0到totalWeight之间的随机数
    //        double randomValue = (double)UnityEngine.Random.value * (double)totalWeight;

    //        // 遍历潜在SCP列表进行选择
    //        for (int j = 0; j < potentialScps.Count; j++)
    //        {
    //            ScpPlayerPicker.PotentialScp potentialScp = potentialScps[j];

    //            // 从随机值中减去当前玩家权重
    //            randomValue -= (double)potentialScp.Weight;

    //            // 如果随机值 <= 0，说明选中了这个玩家
    //            if (randomValue <= 0.0)
    //            {
    //                // 减少待分配数量
    //                scpsToAssign--;
    //                // 添加到SCP候选列表
    //                ScpsToSpawn.Add(potentialScp.Player);
    //                // 从潜在列表中移除
    //                potentialScps.RemoveAt(j);
    //                // 从总权重中减去该玩家权重
    //                totalWeight -= potentialScp.Weight;
    //                break;
    //            }
    //        }
    //    Checker:
    //        // 如果还有剩余名额，继续选择
    //        if (scpsToAssign <= 0)
    //        {
    //            // 归还对象池
    //            NorthwoodLib.Pools.ListPool<ScpPlayerPicker.PotentialScp>.Shared.Return(potentialScps);
    //            scpsToSpawnField.SetValue(null, ScpsToSpawn);
    //            return false;
    //        }

    //        // 继续选择循环
    //        goto SelectionLoop;
    //    }
    //}
}
