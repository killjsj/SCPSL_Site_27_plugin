using AdminToys;
using AutoEvent;
using AutoEvent.Commands;
using CentralAuth;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.RemoteAdmin.Dummies;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Features;
using Exiled.Events.Handlers;
using GameCore;
using InventorySystem.Configs;
using InventorySystem.Items.Keycards;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using LiteNetLib;
using MEC;
using Mirror;
using Mysqlx.Notice;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Utils.NonAllocLINQ;
using Log = Exiled.API.Features.Log;
using Object = UnityEngine.Object;
using Player = Exiled.API.Features.Player;
using Exiled.API.Extensions;

using Round = Exiled.API.Features.Round;
namespace Next_generationSite_27.UnionP
{
    class EventHandle
    {

        PConfig Config = null;
        public Dictionary<Player, CoroutineHandle> cs = new Dictionary<Player, CoroutineHandle>();
        public Dictionary<Player, Stopwatch> BroadcastTime = new Dictionary<Player, Stopwatch>();
        public Dictionary<ushort, Player> snakepairs = new Dictionary<ushort, Player>();
        public Dictionary<Player, int> cachedHighestPairs = new Dictionary<Player, int>();
        MySQLConnect MysqlConnect = Plugin.plugin.connect;
        public (string userid, string name, int? highscore, DateTime? time) cachedHighest = (string.Empty, string.Empty, null, DateTime.MinValue);
        public void ChangedItem(ChangedItemEventArgs ev)
        {
            if (ev.OldItem != null)
            {
                snakepairs.Remove(ev.OldItem.Serial);
            }

        }
        public void InspectedKeycard(PlayerInspectedKeycardEventArgs ev)
        {
            if (ev.KeycardItem.Base is ChaosKeycardItem chaos)
            {
                if (!snakepairs.ContainsKey(chaos.ItemSerial))
                {
                    snakepairs.Add(chaos.ItemSerial, ev.Player);
                }

            }
        }
        public void OnSnakeMovementDirChanged(ushort? Nid, Vector2Int Head)
        {
            if (Nid != null)
            {

                ushort id = Nid.Value;
                if (snakepairs.TryGetValue(id, out var player))
                {

                    var SE = ChaosKeycardItem.SnakeSessions[id];
                    if (MysqlConnect.connected)
                    {
                        if (SE != null)
                        {

                            if (!cachedHighestPairs.ContainsKey(player))
                            {
                                var highscore = MysqlConnect.Query(player.UserId).highscore;
                                if (highscore != null)
                                {
                                    cachedHighestPairs.Add(player, highscore.Value);
                                }
                                else
                                {
                                    cachedHighestPairs.Add(player, 0);
                                }
                            }
                            if (!cachedHighest.highscore.HasValue)
                            {
                                cachedHighest = MysqlConnect.QueryHighest();
                                if (!cachedHighest.highscore.HasValue)
                                {
                                    cachedHighest.highscore = 0;
                                }
                            }
                            if (cachedHighestPairs[player] < SE.Score)
                            {
                                cachedHighestPairs[player] = SE.Score;
                                if (SE.Score > cachedHighest.highscore)
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast()
                                    {
                                        Content = $"<size=15>恭喜你更新服务器最高分:{cachedHighestPairs[player]}",
                                        Duration = 1
                                    });
                                    cachedHighest = (player.UserId, player.DisplayNickname, SE.Score, DateTime.Now);
                                }
                                else
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast()
                                    {
                                        Content = $"<size=15>恭喜你更新个人最高分:{cachedHighestPairs[player]}",
                                        Duration = 1
                                    });
                                }
                            }
                        }
                    }
                }

            }
        }
        public void RestartingRound()
        {
            update();
            stopBroadcast();
        }
        public void update()
        {
            var MysqlConnect = Plugin.plugin.connect;

            if (cachedHighestPairs != null)
            {
                foreach (var item in cachedHighestPairs)
                {
                    MysqlConnect.Update(item.Key.UserId, item.Key.Nickname, item.Value, DateTime.Now);
                }
            }
        }
        public EventHandle(PConfig config)
        {
            Config = config;
        }
        public void Generated()
        {
            InventoryLimits.StandardCategoryLimits[ItemCategory.SpecialWeapon] = (sbyte)Config.MaxSpecialWeaponLimit;
            ServerConfigSynchronizer.Singleton.RefreshCategoryLimits();
        }
        // 添加字段来跟踪受保护的玩家
        public Dictionary<Player, CoroutineHandle> ProtectionCoroutines = new Dictionary<Player, CoroutineHandle>();
        public Dictionary<Player, Stopwatch> BroadcastTimers = new Dictionary<Player, Stopwatch>();
        public bool RoundEnded
        {
            get
            {

                return Round.IsEnded;
            }
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (RoundEnded) return;

            if (ev.Wave is NtfMiniWave || ev.Wave is NtfSpawnWave ||
                ev.Wave is ChaosMiniWave || ev.Wave is ChaosSpawnWave)
            {
                foreach (var player in ev.Players)
                {
                    ApplySpawnProtection(player);
                }
            }
        }

        private void ApplySpawnProtection(Player player)
        {
            try
            {
                // 先清理旧的保护
                if (ProtectionCoroutines.ContainsKey(player))
                {
                    Timing.KillCoroutines(ProtectionCoroutines[player]);
                    ProtectionCoroutines.Remove(player);
                }

                // 应用保护效果
                player.EnableEffect(EffectType.SpawnProtected, SpawnProtected.SpawnDuration);

                // 启动保护文本更新协程
                var coroutine = ProtectTextUpdate(player);
                var handle = MEC.Timing.RunCoroutine(coroutine);

                ProtectionCoroutines[player] = handle;

                Log.Info($"[出生保护] 为玩家 {player.Nickname} 应用保护，持续 {SpawnProtected.SpawnDuration} 秒");
            }
            catch (Exception ex)
            {
                Log.Error($"[出生保护] 为玩家 {player.Nickname} 应用保护时出错: {ex.Message}");
            }
        }

        public IEnumerator<float> ProtectTextUpdate(Player player) // AI太好用了你们知道吗
        {

            // 检查玩家有效性
            if (player == null || !player.IsConnected)
                yield break;

            var spawnProtectedEffect = player.GetEffect(EffectType.SpawnProtected);
            if (spawnProtectedEffect == null)
                yield break;
            //Log.Info(spawnProtectedEffect.TimeLeft);
            while (spawnProtectedEffect.TimeLeft > 0)
            {
                try
                {
                    // 检查玩家是否仍然有效
                    if (player == null || !player.IsConnected || !player.IsAlive)
                    {
                        if (!player.IsAlive)
                        {
                            player.DisableEffect(EffectType.SpawnProtected);

                        }
                        break;
                    }
                    var remainingTime = spawnProtectedEffect.TimeLeft;
                    var text = $"<size=27><color=#{Config.InProtectColor}>🔰刷新保护剩余 {remainingTime:F0} 秒🔰\n开枪将取消保护</color></size>";

                    player.Broadcast(new Exiled.API.Features.Broadcast()
                    {
                        Content = text,
                        Duration = 1
                    }, true);
                }
                catch (Exception ex)
                {
                    Log.Error($"[出生保护] 文本更新出错: {ex.Message}");
                }

                yield return MEC.Timing.WaitForSeconds(1f);
            }

            // 保护结束，清理字典
            if (player != null && ProtectionCoroutines.ContainsKey(player))
            {
                ProtectionCoroutines.Remove(player);
            }

            Log.Info($"[出生保护] 玩家 {player?.Nickname ?? "Unknown"} 保护结束");
        }


        public void Shot(ShotEventArgs ev)
        {
            if (ev.Player.GetEffect<SpawnProtected>() != null)
            {
                var a = ev.Player.GetEffect<SpawnProtected>();
                if (Config.NoProtectWhenShoot && (ProtectionCoroutines.ContainsKey(ev.Player)))
                {
                    try
                    {
                        // 移除保护效果
                        ev.Player.GetEffect<SpawnProtected>().TimeLeft = 0;
                        ev.Player.DisableEffect(EffectType.SpawnProtected);
                        a.ServerDisable();

                        // 停止保护协程
                        if (ProtectionCoroutines.TryGetValue(ev.Player, out var handle))
                        {
                            Timing.KillCoroutines(handle);
                            ProtectionCoroutines.Remove(ev.Player);
                        }

                        // 显示取消保护提示
                        ShowProtectionCancelledMessage(ev.Player);
                        Log.Info($"[出生保护] 玩家 {ev.Player.Nickname} 因开枪取消保护");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[出生保护] 取消保护时出错: {ex.Message}");
                    }
                }
                else
                {
                    if (a.Intensity != 0)
                    {
                        Log.Info($"[出生保护] 玩家 {ev.Player.Nickname} 因开枪取消保护");
                        a.ServerDisable();
                    }
                }
            }
        }

        private void ShowProtectionCancelledMessage(Player player)
        {
            var text = $"<size=27><color=#{Config.OutProtectColor}>保护已取消 - 因开枪</color></size>";
            player.Broadcast(new Exiled.API.Features.Broadcast()
            {
                Content = text,
                Duration = 3
            }, true);
            if (ProtectionCoroutines.ContainsKey(player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[player]);
                ProtectionCoroutines.Remove(player);
            }
        }

        // 在玩家断开连接时清理资源
        public void OnPlayerLeave(LeftEventArgs ev)
        {
            if (ProtectionCoroutines.ContainsKey(ev.Player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[ev.Player]);
                ProtectionCoroutines.Remove(ev.Player);
            }

            if (BroadcastTimers.ContainsKey(ev.Player))
            {
                BroadcastTimers.Remove(ev.Player);
            }
            if (RoundStart.RoundStarted || RoundEnded)
            {
                return;
            }
            foreach (var item in targetRole.Values)
            {
                item.Remove(ev.Player.ReferenceHub);
            }
        }

        // 在回合结束时清理所有保护
        public void OnRoundEnd(RoundEndedEventArgs ev)
        {
            // 清理所有保护协程
            foreach (var handle in ProtectionCoroutines.Values)
            {
                Timing.KillCoroutines(handle);
            }
            ProtectionCoroutines.Clear();
            Plugin.plugin.scpChangeReqs = new List<ScpChangeReq>();

            Plugin.plugin.superSCP.stop();

            // 清理计时器
            BroadcastTimers.Clear();
        }
        private readonly System.Random random = new System.Random();

        private readonly PConfig config = Plugin.Instance.Config;
        public void RoundStarted()
        {
            // assing(); // 假设此方法存在

            if (targetRole == null || targetRole.Count == 0) // 更标准的空检查
            {
                Log.Debug("No target roles to assign. Skipping RoundStarted logic.");
                return;
            }

            Timing.CallDelayed(0.1f, delegate ()
            {
                try
                {
                    Log.Debug("Starting RoundStarted role assignment logic.");

                    var readyPlayers = Player.List
                        .Where(x => !x.ReferenceHub.IsDummy &&
                                    x.Connection.GetType() == typeof(NetworkConnectionToClient) &&
                                    x.ReferenceHub.authManager.InstanceMode != ClientInstanceMode.Unverified &&
                                    x.ReferenceHub.nicknameSync.NickSet)
                        .ToList();

                    Log.Debug($"Ready players count: {readyPlayers.Count}");

                    Dictionary<Player, RoleTypeId> initialRoles = readyPlayers.ToDictionary(p => p, p => p.Role.Type);
                    Dictionary<Player, RoleTypeId> finalRoles = new Dictionary<Player, RoleTypeId>(initialRoles);

                    // SCP替换不需要严格区分"未分配"，我们直接操作所有玩家
                    // 但为了非SCP分配，保留 unassignedPlayers
                    List<Player> unassignedPlayers = new List<Player>(readyPlayers);

                    Log.Debug($"Initial player roles:\n- {string.Join("\n- ", initialRoles.Select(entry => $"{entry.Key.Nickname} ({entry.Key.UserId}): {entry.Value}"))}");

                    // 4. 计算各阵营名额 (关键修复：提前计算)
                    int scpSlots = 0;
                    int mtfSlots = 0;
                    int sciSlots = 0;
                    int ddSlots = 0;
                    List<RoleTypeId> scps = new List<RoleTypeId>();
                    foreach (Player player in readyPlayers)
                    {
                        Team playerTeam = RoleExtensions.GetTeam(player.Role.Type);
                        switch (playerTeam)
                        {
                            case Team.FoundationForces:
                                mtfSlots++;
                                break;
                            case Team.SCPs:
                                scpSlots++;
                                scps.Add(player.Role.Type);
                                break;
                            case Team.Scientists:
                                sciSlots++;
                                break;
                            case Team.ClassD:
                                ddSlots++;
                                break;
                        }
                    }
                    var scpTargetRoles = targetRole
            .Where(tr => RoleExtensions.GetTeam(tr.Key) == Team.SCPs)
            .ToList();

                    Log.Debug($"Target SCP roles to assign: {scpTargetRoles.Count}");

                    foreach (var scpTarget in scpTargetRoles)
                    {
                        RoleTypeId targetScpRole = scpTarget.Key;
                        List<ReferenceHub> preferredHubs = scpTarget.Value ?? new List<ReferenceHub>();

                        Log.Debug($"--- Processing assignment for {targetScpRole} ---");

                        if (scpSlots <= 0)
                        {
                            Log.Debug($"No more SCP slots available. Skipping assignment for {targetScpRole}.");
                            continue;
                        }
                        Player alreadyScpPlayer = readyPlayers.FirstOrDefault(p => p.Role == targetScpRole && p.IsConnected);
                        if (alreadyScpPlayer != null)
                        {
                            // 检查目标角色是否已分配给其他玩家
                            if (finalRoles.Values.Contains(targetScpRole) && finalRoles[alreadyScpPlayer] != targetScpRole)
                            {
                                Log.Warn($"Player {alreadyScpPlayer.Nickname} is already assigned to a different role. Skipping replacement.");
                                continue;
                            }

                            if (preferredHubs.Contains(alreadyScpPlayer.ReferenceHub))
                            {
                                finalRoles[alreadyScpPlayer] = targetScpRole;
                                scpSlots--;
                            }
                            else if (scps.Contains(targetScpRole))
                            {
                                if (preferredHubs.Count > 0)
                                {
                                    var lucker = preferredHubs.RandomItem();
                                    var p = Player.Get(lucker);
                                    if (p != null && unassignedPlayers.Contains(p))
                                    {
                                        finalRoles[p] = targetScpRole;
                                        finalRoles[alreadyScpPlayer] = p.Role.Type;
                                        unassignedPlayers.Remove(alreadyScpPlayer);
                                        scpSlots--;
                                    }
                                    else
                                    {
                                        Log.Warn($"Candidate {p?.Nickname} is not in unassignedPlayers. Skipping replacement.");
                                    }
                                }
                                else
                                {
                                    // preferredHubs 为空时，仅保留已有玩家角色，不进行分配
                                    finalRoles[alreadyScpPlayer] = targetScpRole;
                                    scpSlots--;
                                }
                            }
                            Log.Debug($"SCP slot used. Remaining SCP slots: {scpSlots}.");
                        }
                        else
                        {
                            if (scps.Contains(targetScpRole))
                            {
                                if (preferredHubs.Count > 0)
                                {
                                    var lucker = preferredHubs.RandomItem();
                                    var p = Player.Get(lucker);
                                    if (p != null && unassignedPlayers.Contains(p))
                                    {
                                        finalRoles[p] = targetScpRole;
                                        unassignedPlayers.Remove(p);
                                        scpSlots--;
                                    }
                                    else
                                    {
                                        Log.Warn($"Candidate {p?.Nickname} is not in unassignedPlayers. Skipping assignment.");
                                    }
                                }
                                else
                                {
                                    Log.Warn($"No candidates for {targetScpRole}. Skipping assignment.");
                                }
                            }
                        }
                    }

                    // 6. 分配其他预设角色 (非SCP) - 保持原有逻辑或根据需要调整
                    if (targetRole != null)
                    {
                        var nonScpTargetRoles = targetRole.Where(tr => RoleExtensions.GetTeam(tr.Key) != Team.SCPs && tr.Key != RoleTypeId.None).ToList();
                        foreach (var nonScpTarget in nonScpTargetRoles)
                        {
                            RoleTypeId targetRoleType = nonScpTarget.Key;
                            List<ReferenceHub> candidates = nonScpTarget.Value ?? new List<ReferenceHub>();

                            Log.Debug($"Processing target non-SCP role: {targetRoleType}. Candidates: {candidates.Count}.");

                            int availableSlots = 0;
                            Team targetTeam = RoleExtensions.GetTeam(targetRoleType);
                            switch (targetTeam)
                            {
                                case Team.ClassD:
                                    availableSlots = ddSlots;
                                    break;
                                case Team.Scientists:
                                    availableSlots = sciSlots;
                                    break;
                                case Team.FoundationForces:
                                    availableSlots = mtfSlots;
                                    break;
                            }

                            if (availableSlots <= 0)
                            {
                                Log.Debug($"Skipping {targetRoleType} assignment, no slots available for its team.");
                                continue;
                            }

                            // 创建候选人副本以避免在迭代时修改
                            List<ReferenceHub> candidatesCopy = new List<ReferenceHub>(candidates);
                            foreach (var candidateHub in candidatesCopy)
                            {
                                // 检查名额
                                switch (targetTeam)
                                {
                                    case Team.ClassD:
                                        if (ddSlots <= 0) continue;
                                        break;
                                    case Team.Scientists:
                                        if (sciSlots <= 0) continue;
                                        break;
                                    case Team.FoundationForces:
                                        if (mtfSlots <= 0) continue;
                                        break;
                                }

                                Player candidatePlayer = Player.Get(candidateHub);
                                // 关键修改：检查候选人是否仍在待分配列表中
                                if (candidatePlayer != null && unassignedPlayers.Contains(candidatePlayer))
                                {
                                    finalRoles[candidatePlayer] = targetRoleType;
                                    unassignedPlayers.Remove(candidatePlayer); // 分配后移除
                                    Log.Debug($"Assigned {targetRoleType} to {candidatePlayer.Nickname}.");

                                    switch (targetTeam)
                                    {
                                        case Team.ClassD:
                                            ddSlots--;
                                            break;
                                        case Team.Scientists:
                                            sciSlots--;
                                            break;
                                        case Team.FoundationForces:
                                            mtfSlots--;
                                            break;
                                    }
                                }
                                else
                                {
                                    Log.Debug($"Candidate {candidatePlayer?.Nickname ?? "Unknown"} for {targetRoleType} is either null, disconnected, or already assigned.");
                                }
                            }
                        }
                    }


                    // 7. 应用角色变更 (保持不变)
                    Log.Debug("Applying final role changes...");
                    int appliedChanges = 0;
                    foreach (var entry in finalRoles)
                    {
                        if (entry.Value != initialRoles[entry.Key])
                        {
                            try
                            {
                                if (entry.Key != null && entry.Key.IsConnected)
                                {
                                    entry.Key.RoleManager.ServerSetRole(
                                        entry.Value,
                                        RoleChangeReason.RoundStart,
                                        RoleSpawnFlags.All
                                    );
                                    appliedChanges++;
                                    Log.Info($"Successfully assigned {entry.Value} to {entry.Key.Nickname}");
                                }
                                else
                                {
                                    Log.Debug($"Player {entry.Key?.Nickname ?? "Unknown"} disconnected before role could be applied.");
                                }
                            }
                            catch (Exception applyEx)
                            {
                                Log.Error($"Failed to assign role {entry.Value} to {entry.Key?.Nickname ?? "Unknown Player"}: {applyEx}");
                            }
                        }
                    }
                    Log.Debug($"Applied {appliedChanges} role changes.");

                    // 8. 清空目标角色列表 (保持不变)
                    Log.Debug("Clearing targetRole lists...");
                    if (targetRole != null)
                    {
                        foreach (var item in targetRole)
                        {
                            item.Value?.Clear();
                        }
                    }

                    // 9. 记录最终角色状态 (保持不变)
                    Log.Debug($"Final player roles:\n- {string.Join("\n- ", finalRoles.Select(entry => $"{entry.Key.Nickname} ({entry.Key.UserId}): {entry.Value}"))}");
                    Log.Debug("Finished RoundStarted role assignment logic.");
                }
                catch (Exception ex)
                {
                    Log.Error($"Critical error in RoundStarted delegate: {ex}");
                }
            });


            // 11. 启用 Super SCP (如果配置允许)
            try
            {
                if (Player.List.Count() >= Config.EnableSuperScpCount && Config.EnableSuperScp)
                {
                    Plugin.enableSSCP = true;
                    Plugin.plugin.superSCP.start();
                    Log.Debug("Super SCP enabled and started.");
                }
            }
            catch (Exception superScpEx)
            {
                Log.Error($"Error enabling/starting Super SCP: {superScpEx}");
            }
            
        }
        bool IsRoleAvailable(RoleTypeId role, Dictionary<Player, RoleTypeId> roles)
        {
            // 非唯一角色（如Class-D）总是可用
            if (role != RoleTypeId.Scp049 &&
                role != RoleTypeId.Scp079 &&
                role != RoleTypeId.Scp096 &&
                role != RoleTypeId.Scp106 &&
                role != RoleTypeId.Scp173 &&
                role != RoleTypeId.Scp939)
            {
                return true;
            }

            // 唯一角色检查：确保服务器中不存在该角色
            return !roles.Values.Any(r => r == role);
        }
        public void SentValidCommand(SentValidCommandEventArgs ev)
        {
            if (ev.Player.RemoteAdminAccess)
            {
                MysqlConnect.LogAdminPermission(ev.Player.UserId, ev.Player.DisplayNickname, Exiled.API.Features.Server.Port, ev.Query, ev.Response, group: ev.Player.Group.Name);
            }
        }
        public Dictionary<ReferenceHub, List<(EffectType, byte, float)>> effects = new Dictionary<ReferenceHub, List<(EffectType, byte, float)>>();
        public void Escaped(EscapedEventArgs ev)
        {

            Log.Info($"{ev.Player}成功撤离 时间:{ev.EscapeTime}");
            if (effects.ContainsKey(ev.Player.ReferenceHub))
            {
                foreach (var item in effects[ev.Player.ReferenceHub])
                {
                    if (ev.Player.TryGetEffect(item.Item1, out var statusEffect))
                    {
                        ev.Player.EnableEffect(statusEffect, item.Item2, item.Item3, false);
                        ev.Player.ReferenceHub.playerEffectsController.ServerSyncEffect(statusEffect);

                    }


                    Log.Info($"对{ev.Player}施加了效果:{item} Intensity:{item.Item2} 撤离");
                }
                effects.Remove(ev.Player.ReferenceHub);

            }
        }
        public void OnSpawned(SpawnedEventArgs ev)
        {
            bool flag = ev.Player.Role.Type == RoleTypeId.ClassD;
            if (flag)
            {
                KeycardJanitor(ev.Player);
            }
        }

        // Token: 0x0600000F RID: 15 RVA: 0x0000221B File Offset: 0x0000041B
        public static void KeycardJanitor(Player p)
        {
            p.AddItem(ItemType.KeycardJanitor, 1);
        }
        public void Escaping(EscapingEventArgs ev)
        {
            bool flag = ev.Player.Role.Type == RoleTypeId.FacilityGuard;
            if (flag)
            {
                ev.EscapeScenario = EscapeScenario.CustomEscape;
                ev.NewRole = RoleTypeId.NtfSergeant;
                ev.IsAllowed = true;
            }
            if (effects.ContainsKey(ev.Player.ReferenceHub))
            {
                effects[ev.Player.ReferenceHub].Clear();
                foreach (var item in ev.Player.ActiveEffects)
                {
                    if (item.GetEffectType() == EffectType.Scp1344)
                    {
                        continue;
                    }
                    if (item.GetEffectType() == EffectType.Invisible)
                    {
                        continue;
                    }
                    effects[ev.Player.ReferenceHub].Add((item.GetEffectType(), item.Intensity, item.Duration));

                }
            }
            else
            {
                effects.Add(ev.Player.ReferenceHub, new List<(EffectType, byte, float)>());
                foreach (var item in ev.Player.ActiveEffects)
                {
                    if (item.GetEffectType() == EffectType.Scp1344)
                    {
                        continue;
                    }
                    if (item.GetEffectType() == EffectType.Invisible)
                    {
                        continue;
                    }
                    effects[ev.Player.ReferenceHub].Add((item.GetEffectType(), item.Intensity, item.Duration));

                }
            }
        }
        public void RespawningTeam(RespawningTeamEventArgs ev)
        {
            SpawnableWaveBase newW = ev.Wave.Base;
            List<ReferenceHub> players = new List<ReferenceHub>();
            Log.Info($"RespawningTeam IsMiniWave {ev.Wave.IsMiniWave}");

            if (ev.Wave.IsMiniWave)
            {
                ev.IsAllowed = false;

                switch (ev.Wave.Faction)
                {
                    case PlayerRoles.Faction.FoundationStaff:
                        {
                            newW = new NtfSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);
                            break;
                        }
                    case PlayerRoles.Faction.FoundationEnemy:
                        {
                            newW = new ChaosSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);


                            break;
                        }
                }

                ev.Wave.Timer.SetTime(0);
                RespawnedTeam(new RespawnedTeamEventArgs(newW, players));

            }
        }
        public CoroutineHandle BroadcasterHandler;
        public List<GameObject> SPC = new List<GameObject>();
        public List<ReferenceHub> SPD = new List<ReferenceHub>();
        public bool st = false;
        public void assing()
        {
            st = true;
            foreach (var item in Player.List)
            {
                if (item.Role.Type != RoleTypeId.Overwatch)
                {
                    item.RoleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RoundStart);
                }
                if (SPD.Contains(item.ReferenceHub))
                {
                    item.RoleManager.ServerSetRole(RoleTypeId.Overwatch, RoleChangeReason.RoundStart);
                    NetworkServer.Destroy(item.ReferenceHub.gameObject);
                    SPD.Remove(item.ReferenceHub);

                }
            }
            foreach (ReferenceHub obj in ReferenceHub.AllHubs)
            {
                if (SPD.Contains(obj))
                {
                    //Log.Info("SPD C");
                    //Log.Info(obj);
                    NetworkServer.Destroy(obj.gameObject);
                    SPD.Remove(obj);
                }
            }
            SPD.Clear();
            foreach (GameObject p in Plugin.SOB.AttachedBlocks)
            {
                if (p.name == "SCP096P")
                {
                    SPC.Remove(p);
                    var SCP096 = p.GetComponent<coH>();
                    if (SCP096 != null)
                    {
                        SCP096.PlayerEnter -= SCP096_PlayerEnter;
                    }
                }
                if (p.name == "SCP049P")
                {
                    SPC.Remove(p);
                    var SCP049 = p.GetComponent<coH>();
                    if (SCP049 != null)
                    {
                        SCP049.PlayerEnter -= SCP049_PlayerEnter;
                    }
                }
                if (p.name == "SCP106P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP106_PlayerEnter;
                    }
                }
                if (p.name == "SCP939P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP939_PlayerEnter;
                    }
                }
                if (p.name == "SCP173P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP173_PlayerEnter;
                    }
                }
                if (p.name == "SCP079P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP079_PlayerEnter;
                    }
                }
            }
            Cleaned = true;
        }
        public bool Cleaned = false;
        GameObject SP;
        AdminToys.TextToy textToy;
        public void WaitingForPlayers()
        {
            st = false;
            StopBroadcast = false;
            Plugin.enableSSCP = false;
            SPD.Clear();

            SPD = new List<ReferenceHub>();
            SPC = new List<GameObject>();
            targetRole = new Dictionary<RoleTypeId, List<ReferenceHub>>() {
            {RoleTypeId.Scientist ,new List<ReferenceHub>()},
            {RoleTypeId.Scp079 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp049 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp096 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp173 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp106 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp939 ,new List<ReferenceHub>()},
            {RoleTypeId.FacilityGuard ,new List<ReferenceHub>()},
            {RoleTypeId.ClassD ,new List<ReferenceHub>()}
        };
            if (!Config.RoundSelfChoose)
            {
                goto No;
            }
            var g = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            if (g != null)
            {
                foreach (Canvas c in g)
                {
                    if (c.name == "Player Canvas")
                    {
                        var t = c.gameObject.transform.Find("StartRound");
                        if (t != null)
                        {
                            t.gameObject.SetActive(false);
                        }
                    }
                }

            }


            PrefabManager.RegisterPrefabs();
            var ss = new SerializableSchematic
            {
                SchematicName = "SpawnRoom",
                Position = new Vector3(0, 290, -90)
            };

            GameObject gameObject = ss.SpawnOrUpdateObject();
            Plugin.SOB = gameObject.GetComponent<SchematicObject>();
            //Log.Info($"outside {Exiled.API.Features.Room.Get(RoomType.Surface).Position}");
            foreach (GameObject p in Plugin.SOB.AttachedBlocks)
            {

                if (p != null && p.name != null)
                {
                    //Log.Info(p.name);
                    if (p.name == "DDP")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var dd = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (dd != null)
                        {
                            dd.PlayerEnter += Dd_PlayerEnter;
                        }
                    }
                    if (p.name == "SCIP")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCI = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCI != null)
                        {
                            SCI.PlayerEnter += SCI_PlayerEnter;
                        }
                    }
                    if (p.name == "GRP")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var GR = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (GR != null)
                        {
                            GR.PlayerEnter += GR_PlayerEnter;
                        }
                    }
                    if (p.name == "spawnpoint")
                    {
                        SP = p;
                    }
                    if (p.name == "roundtext")
                    {
                        var b = LabApi.Features.Wrappers.TextToy.Create(p.transform.position, p.transform.rotation, p.transform.localScale);
                        textToy = b.Base;
                    }
                    if (p.name == "SCP079-Text")
                    {
                        var b = LabApi.Features.Wrappers.TextToy.Create(p.transform.position, p.transform.rotation, p.transform.localScale);
                        b.TextFormat = "<color=red>SCP079";
                    }
                    if (p.name == "SCP096P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP096 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP096 != null)
                        {
                            SCP096.PlayerEnter += SCP096_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP049P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP049 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);


                        if (SCP049 != null)
                        {
                            SCP049.PlayerEnter += SCP049_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP106P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP106_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP939P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP939_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP173P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP173_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP079P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP079_PlayerEnter;
                        }
                    }

                    if (p.name == "DD")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当DD");
                            r.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });
                    }
                    if (p.name == "SCI")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当科学");
                            r.roleManager.ServerSetRole(RoleTypeId.Scientist, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });

                    }
                    if (p.name == "SCP096D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp096, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });
                    }
                    if (p.name == "SCP049D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp049, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });

                    }
                    if (p.name == "SCP939D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp939, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });
                    }
                    if (p.name == "SCP173D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp173, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });
                    }
                    if (p.name == "SCP106D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp106, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });
                    }
                    if (p.name == "GR")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当保安");
                            r.roleManager.ServerSetRole(RoleTypeId.FacilityGuard, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });
                    }
                }
                //RoundStart.singleton.NetworkTimer = -1;
                //RoundStart.RoundStartTimer.Restart();
                //Log.Info("3");
            }
            Cleaned = false;
            GC.Collect();
            MEC.Timing.RunCoroutine(rounder());
        No:
            BroadcasterHandler = MEC.Timing.RunCoroutine(Broadcaster());

        }
        public Dictionary<RoleTypeId, List<ReferenceHub>> targetRole = new Dictionary<RoleTypeId, List<ReferenceHub>>() {
            {RoleTypeId.Scientist ,new List<ReferenceHub>()},
            {RoleTypeId.Scp079 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp049 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp096 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp173 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp106 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp939 ,new List<ReferenceHub>()},
            {RoleTypeId.FacilityGuard ,new List<ReferenceHub>()},
            {RoleTypeId.ClassD ,new List<ReferenceHub>()}
        };
        void hp(Player player, RoleTypeId typeId)
        {
            foreach (var item in targetRole)
            {
                item.Value.Remove(player.ReferenceHub);
            }
            targetRole[typeId].Add(player.ReferenceHub);
            player.Broadcast(2, $"你选择当{typeId}", Broadcast.BroadcastFlags.Normal, true);
        }
        void GR_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose GR");
            hp(pl, RoleTypeId.FacilityGuard);
        }

        void SCP106_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCP106");
            hp(pl, RoleTypeId.Scp106);

        }
        void SCP049_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCP049");
            hp(pl, RoleTypeId.Scp049);

        }
        void SCP939_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCP939");

            hp(pl, RoleTypeId.Scp939);
        }
        void SCP079_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCP079");
            hp(pl, RoleTypeId.Scp079);

        }
        void SCP096_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCP096");
            hp(pl, RoleTypeId.Scp096);

        }
        void SCP173_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCP173");
            hp(pl, RoleTypeId.Scp173);

        }

        void SCI_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose SCI");
            hp(pl, RoleTypeId.Scientist);

        }

        void Dd_PlayerEnter(Player pl)
        {
            Log.Info($"{pl} choose DD");
            hp(pl, RoleTypeId.ClassD);

        }

        public void stopBroadcast()
        {
            StopBroadcast = true;

            MEC.Timing.KillCoroutines(BroadcasterHandler);


        }
        public bool StopBroadcast = false;
        public int BroadcastIndex = 0;
        public int BroadcastCounter = 0;
        public IEnumerator<float> Broadcaster()
        {
            for (; ; )
            {
                BroadcastCounter++;

                if (BroadcastCounter <= Config.BroadcastWaitTime)
                {
                    yield return MEC.Timing.WaitForSeconds(1);

                }
                else
                {
                    BroadcastCounter = 0;
                    foreach (var item in Player.List)
                    {
                        item.Broadcast(new Exiled.API.Features.Broadcast()
                        {
                            Content = $"<size={Config.BroadcastSize}><color={Config.BroadcastColor}>{Config.BroadcastContext[BroadcastIndex]}</color></size>",
                            Duration = (ushort)Config.BroadcastShowTime
                        });
                    }

                    BroadcastIndex++;
                    BroadcastIndex %= Config.BroadcastContext.Count;
                }

            }
        }
        public string Waiting = "";
        public IEnumerator<float> rounder()
        {
            while (true)
            {
                if (Round.IsLobbyLocked)
                {
                    Waiting = $"回合已锁定";
                }
                
                else if (Round.IsLobby)
                {
                    Waiting = $"还有{RoundStart.singleton.NetworkTimer}秒回合开始";
                    if (RoundStart.singleton.NetworkTimer == -2)
                    {
                        Waiting = $"回合不够人";

                    }
                }
                else
                {
                    Waiting = $"回合已开始{Round.ElapsedTime.TotalSeconds}秒";
                }
                textToy.TextFormat = Waiting;
                yield return Timing.WaitForSeconds(1f);
            }


        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (cs.TryGetValue(ev.Player, out var CH))
            {
                ev.Player.DisableEffect(EffectType.SpawnProtected);
                MEC.Timing.KillCoroutines(CH);
                cs.Remove(ev.Player);
            }
            if (Plugin.plugin.superSCP.PatchedPlayers.Contains(ev.Player))
            {
                Plugin.plugin.superSCP.PatchedPlayers.Remove(ev.Player);
            }
            if (ev.NewRole == RoleTypeId.Overwatch)
            {
                foreach (var item in targetRole.Values)
                {
                    item.Remove(ev.Player.ReferenceHub);
                }
            }
        }
        public void EndingRound(EndingRoundEventArgs ev)
        {

            foreach (var item in cs)
            {
                item.Key.DisableEffect(EffectType.SpawnProtected);
                MEC.Timing.KillCoroutines(item.Value);
                cs.Remove(item.Key);
            }
        }

        public void ChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
        {
        }
        public void Joined(JoinedEventArgs ev)
        {
        }
        public void Verified(VerifiedEventArgs ev)
        {
            if (!Round.IsStarted)
            {
                if (!SPD.Contains(ev.Player.ReferenceHub))
                {
                    ev.Player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);
                    ev.Player.Position = SP.transform.position + Vector3.up * 3;
                    Timing.CallDelayed(2f, () =>
                    {
                        if (!Round.IsStarted)
                        {
                            if (ev.Player.Position == SP.transform.position + Vector3.up * 3)
                            {
                                ev.Player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);
                                ev.Player.Broadcast(3, "出现bug了!已将你传回高塔,请联系管理");
                            }
                        }
                    });
                }
            }
            if (st)
            {
                ev.Player.RoleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
            }
        }
    }
    public class coH : MonoBehaviour
    {
        public delegate void onplayerenter(Player pl);
        public event onplayerenter PlayerEnter;
        public void OnTriggerEnter(Collider other)
        {
            Player player = Player.Get(other.gameObject);
            if (player is null)
                return;
            PlayerEnter.Invoke(player);

        }
    }
}
namespace GwangjuRunningManLoader
{
    public class EventHandler
    {
        RunningMan _plugin;
        public EventHandler(RunningMan plugin)
        {
            _plugin = plugin;
        }
        public void died(DiedEventArgs ev)
        {
            if (ev.TargetOldRole == PlayerRoles.RoleTypeId.NtfCaptain || _plugin.Jailor.Contains(ev.Player))
            {
                return;
            }
            if (_plugin.Deaths.TryGetValue(ev.Player, out int v))
            {
                if (v == _plugin.Config.PrisonerLives - 1)
                {
                    return;
                }
                else
                {
                    _plugin.Deaths[ev.Player] += 1;
                }
            }
            else
            {
                _plugin.Deaths[ev.Player] = 1;
            }
            ev.Player.GiveLoadout(_plugin.Config.PrisonerLoadouts);
            ev.Player.Position = _plugin.SpawnPoints.Where(r => r.name == "Spawnpoint").ToList().RandomItem().transform.position;
            ev.Player.Health = 100;

        }
        //public void Hit(HurtEventArgs ev)
        //{
        //    if (ev.Attacker.IsNTF && ev.Attacker.CurrentItem.Category == ItemCategory.SpecialWeapon)
        //    {
        //        ev.Attacker.RemoveItem(ev.Attacker.CurrentItem);

        //        ev.Attacker.CurrentItem = ev.Attacker.AddItem(ItemType.Jailbird);
        //    }
        //}
        public void OnChargingJailbird(ChargingJailbirdEventArgs ev)
        {
            ev.IsAllowed = false;
        }
    }
}