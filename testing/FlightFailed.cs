using AdminToys;
using CommandSystem;
using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using RemoteAdmin.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Next_generationSite_27.UnionP.testing
{
    class FlightFailed
    {
        // --- 状态字段 ---
        public struct BattleReq
        {
            public Player From;
            public string From_backup;
            public Player To;
            public string To_backup;
            public BattleType Type;
            public Stopwatch stopwatch;
        }
        public struct CurrentBattleReq
        {
            public Player From;
            public Player To;
            public BattleType Type;
        }

        public enum BattleType
        {
            JailBird,
            Gun,
        }

        public static CurrentBattleReq CurrentBattle = new CurrentBattleReq();
        public static bool CurrentBattling = false;
        public static List<BattleReq> BattleReqs = new List<BattleReq>();
        public static List<BattleReq> WaitingBattleReqs = new List<BattleReq>();
        public static Dictionary<string, string> PlayerToBadge = new Dictionary<string, string>();
        public static List<CoroutineHandle> FailedShowHandles = new List<CoroutineHandle>();

        private const string REQUEST_KEY = "FlightRequest";

        // --- 启动入口（在 Plugin 或 EventHandlers 初始化时调用） ---
        public static void Start()
        {
            Log.Info("[FlightFailed] Starting FlightFailed module.");
            Timing.RunCoroutine(While());
        }

        // --- 主协程循环 ---
        public static IEnumerator<float> While()
        {
            Log.Info("[FlightFailed] While coroutine started.");
            while (Round.IsStarted)
            {
                try
                {
                    // 1) 清理超时的请求（倒序删除）
                    for (int i = BattleReqs.Count - 1; i >= 0; i--)
                    {
                        var req = BattleReqs[i];
                        try
                        {
                            if (req.stopwatch != null && req.stopwatch.Elapsed.TotalSeconds >= 30)
                            {
                                CancelRequest(req, "请求超时，决斗已取消。");
                                BattleReqs.RemoveAt(i);
                            }
                        }
                        catch (Exception exReq)
                        {
                            Log.Warn($"[FlightFailed] 清理请求时发生异常: {exReq}");
                            // 安全移除以避免死循环
                            BattleReqs.RemoveAt(i);
                        }
                    }

                    // 2) 处理等待队列（只有当没有正在战斗时）
                    if (!tempFlightFlag && WaitingBattleReqs.Count > 0)
                    {
                        var next = WaitingBattleReqs[0];
                        WaitingBattleReqs.RemoveAt(0);
                        TryStartBattle(next);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[FlightFailed] 主循环异常: {ex}");
                }

                yield return Timing.WaitForSeconds(1f);
            }

            // 回合结束后的清理
            CleanupOnRoundEnd();
            Log.Info("[FlightFailed] While coroutine ended (round ended).");
            yield break;
        }

        private static void CleanupOnRoundEnd()
        {
            Log.Info("[FlightFailed] Cleaning up on round end.");
            try
            {
                foreach (var handle in FailedShowHandles)
                    Timing.KillCoroutines(handle);
            }
            catch (Exception ex) { Log.Warn($"[FlightFailed] Cleanup FailedShowHandles exception: {ex}"); }

            FailedShowHandles.Clear();
            BattleReqs.Clear();
            WaitingBattleReqs.Clear();
            CurrentBattle = new CurrentBattleReq();
            PlayerToBadge.Clear();
            CurrentBattling = false; tempFlightFlag = false;
        }
        
        // --- 请求取消帮助函数 ---
        private static void CancelRequest(BattleReq req, string reason)
        {
            try
            {
                var from = req.From ?? (string.IsNullOrEmpty(req.From_backup) ? null : Player.Get(req.From_backup));
                var to = req.To ?? (string.IsNullOrEmpty(req.To_backup) ? null : Player.Get(req.To_backup));
                from?.RemoveMessage(REQUEST_KEY);
                to?.RemoveMessage(REQUEST_KEY);
                from?.Broadcast(3, $"<size=20>{reason}</size>", shouldClearPrevious: true);
                to?.Broadcast(3, $"<size=20>{reason}</size>", shouldClearPrevious: true);
                Log.Info($"[FlightFailed] CancelRequest: {from?.Nickname ?? req.From_backup} <-> {to?.Nickname ?? req.To_backup}: {reason}");
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] CancelRequest exception: {ex}");
            }
        }
        public static bool tempFlightFlag = false;
        // --- 尝试启动战斗 ---
        private static void TryStartBattle(BattleReq req)
        {
            try
            {
                var from = req.From ?? (string.IsNullOrEmpty(req.From_backup) ? null : Player.Get(req.From_backup));
                var to = req.To ?? (string.IsNullOrEmpty(req.To_backup) ? null : Player.Get(req.To_backup));

                if (from == null || to == null)
                {
                    Log.Info("[FlightFailed] TryStartBattle: one side is null, cancelling.");
                    CancelRequest(req, "无法开始决斗：玩家不存在或已离开。");
                    return;
                }

                // 只有双方都不在线或都处于不可复活（本插件语义是双方必须都死亡/不可操作）才可开始
                if (from.IsAlive || to.IsAlive)
                {
                    Log.Info("[FlightFailed] TryStartBattle: one of players is still alive — cannot start.");
                    from.Broadcast(3, "<size=20>无法开始决斗：双方中有人还活着。</size>", shouldClearPrevious: true);
                    to.Broadcast(3, "<size=20>无法开始决斗：双方中有人还活着。</size>", shouldClearPrevious: true);
                    return;
                }
                tempFlightFlag = true;
                // 成功设置当前战斗
                CurrentBattle = new CurrentBattleReq { From = from, To = to, Type = req.Type };

                from.RemoveMessage(REQUEST_KEY);
                to.RemoveMessage(REQUEST_KEY);

                from.Broadcast(5, $"<size=25>你与 {to.DisplayNickname} 的决斗开始! 战斗类型: {req.Type}</size>", shouldClearPrevious: true);
                to.Broadcast(5, $"<size=25>你与 {from.DisplayNickname} 的决斗开始! 战斗类型: {req.Type}</size>", shouldClearPrevious: true);

                SetupBattleEnvironment(from, to, req.Type);
                Log.Info($"[FlightFailed] Battle started: {from.Nickname} vs {to.Nickname}, Type={req.Type}");
            }
            catch (Exception ex)
            {
                Log.Error($"[FlightFailed] TryStartBattle exception: {ex}");
                CurrentBattle = new CurrentBattleReq();
            CurrentBattling = false; tempFlightFlag = false;
            }
        }

        // --- 给对战双方设置环境与物品 ---
        private static void SetupBattleEnvironment(Player from, Player to, BattleType type)
        {
            try
            {
                // 统一设为 Tutorial（简化重生点）
                from.Role.Set(RoleTypeId.Tutorial);
                to.Role.Set(RoleTypeId.Tutorial);

                // 延迟少许执行以避免立即冲突
                Timing.CallDelayed(0.1f, () =>
                {
                    try
                    {
                        CurrentBattling = true;
                        if (type == BattleType.Gun)
                        {
                            from.Health = 500;
                            to.Health = 500;
                        }
                        else
                        {
                            from.Health = 300;
                            to.Health = 300;
                        }

                        from.SetFriendlyFire(RoleTypeId.Tutorial, 1);
                        to.SetFriendlyFire(RoleTypeId.Tutorial, 1);

                        // 发放物品
                        switch (type)
                        {
                            case BattleType.JailBird:
                                GiveItems(from, ItemType.Jailbird, 3);
                                GiveItems(to, ItemType.Jailbird, 3);
                                break;
                            case BattleType.Gun:
                                from.AddItem(itemType: ItemType.GunE11SR);
                                from.AddItem(itemType: ItemType.Ammo556x45);
                                from.AddItem(itemType: ItemType.Ammo556x45);
                                from.AddItem(itemType: ItemType.Ammo556x45);
                                to.AddItem(itemType: ItemType.GunE11SR);
                                to.AddItem(itemType: ItemType.Ammo556x45);
                                to.AddItem(itemType: ItemType.Ammo556x45);
                                to.AddItem(itemType: ItemType.Ammo556x45);
                                break;
                        }

                        from.AddItem(itemType: ItemType.Medkit);
                        to.AddItem(itemType: ItemType.Medkit);

                        Log.Info($"[FlightFailed] SetupBattleEnvironment done for {from.Nickname} and {to.Nickname}");
                    }
                    catch (Exception exInner)
                    {
                        Log.Warn($"[FlightFailed] SetupBattleEnvironment inner exception: {exInner}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] SetupBattleEnvironment exception: {ex}");
            }
        }

        private static void GiveItems(Player player, ItemType type, int count)
        {
            try
            {
                for (int i = 0; i < count; i++)
                    player.AddItem(itemType: type);
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] GiveItems exception for {player?.Nickname}: {ex}");
            }
        }

        // --- 结束战斗并赋予称号（胜者/败者处理） ---
        private static void EndBattle(Player winner, Player loser)
        {
            try
            {
                if (winner == null || loser == null)
                {
                    Log.Warn("[FlightFailed] EndBattle called with null players.");
                    CurrentBattle = new CurrentBattleReq();
                    CurrentBattling = false;
                    CurrentBattling = false; tempFlightFlag = false;

                    return;
                }

                var winnerLP = LabApi.Features.Wrappers.Player.Get(winner.ReferenceHub);
                var loserLP = LabApi.Features.Wrappers.Player.Get(loser.ReferenceHub);

                var badge = FlightBadgeGen(winner);
                winnerLP?.SendBroadcast($"<size=27>你赢了 {loser.DisplayNickname} 的决斗! 对方将获得称号 {badge}</size>", 10, shouldClearPrevious: true);
                loserLP?.SendBroadcast($"<size=27>你输了 {winner.DisplayNickname} 的决斗! 并获得称号 {badge}</size>", 10, shouldClearPrevious: true);
                Cassie.Message($"<size=16>{winner.DisplayNickname} 击败了 {loser.DisplayNickname}! 并获得称号 {badge}</size>", isSubtitles: true);

                // 记录 loser 的称号（显示为失败者获得称号）
                PlayerToBadge[loser.UserId] = FlightBadgeGen(winner, false);

                // 清理
                CurrentBattle = new CurrentBattleReq();
            CurrentBattling = false; tempFlightFlag = false;
                CurrentBattling = false;

                try { winner.ClearItems(); } catch { }
                try { loser.ClearItems(); } catch { }

                try { winner.Role.Set(RoleTypeId.Spectator); } catch { }
                try { loser.Role.Set(RoleTypeId.Spectator); } catch { }

                try { winner.TryRemoveFriendlyFire(RoleTypeId.Tutorial); } catch { }
                try { loser.TryRemoveFriendlyFire(RoleTypeId.Tutorial); } catch { }

                // 启动显示称号的协程以确保称号在网络端同步
                FailedShowHandles.Add(Timing.RunCoroutine(FailedShow(loser)));
            }
            catch (Exception ex)
            {
                Log.Error($"[FlightFailed] EndBattle exception: {ex}");
                CurrentBattle = new CurrentBattleReq();
            CurrentBattling = false; tempFlightFlag = false;
                CurrentBattling = false;
            }
        }

        // --- 事件处理 --- 
        public static void OnDied(DyingEventArgs ev)
        {
            try
            {
                if (!CurrentBattling) return;
                if (ev.Player == null || ev.Attacker == null) return;

                // 仅在参与者之间击杀才处理
                if (ev.Player == CurrentBattle.From || ev.Player == CurrentBattle.To)
                {
                    Player winner = ev.Attacker;
                    Player loser = ev.Player;

                    // 阻止掉常规掉落
                    foreach (var item in ev.ItemsToDrop)
                    {
                        try { item.Destroy(); } catch { }
                    }

                    ev.IsAllowed = false;

                    EndBattle(winner, loser);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] OnDied exception: {ex}");
            }
        }

        public static void OnHurt(HurtingEventArgs ev)
        {
            try
            {
                if (!CurrentBattling) return;
                if (ev.Player == null) return;

                // 只允许决斗双方互殴
                var from = CurrentBattle.From;
                var to = CurrentBattle.To;
                if (ev.Attacker != null && ev.Attacker != from && ev.Attacker != to)
                {
                    ev.IsAllowed = false;
                    ev.Attacker.Broadcast(3, "不准打扰决斗", shouldClearPrevious: true);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] OnHurt exception: {ex}");
            }
        }

        public static void OnLeft(LeftEventArgs ev)
        {
            try
            {
                if (!CurrentBattling) return;
                if (ev.Player == null) return;

                if (ev.Player == CurrentBattle.From || ev.Player == CurrentBattle.To)
                {
                    var loser = ev.Player;
                    var winner = loser == CurrentBattle.From ? CurrentBattle.To : CurrentBattle.From;
                    var winnerLP = LabApi.Features.Wrappers.Player.Get(winner.ReferenceHub);

                    Cassie.Message($"<size=16>{loser.DisplayNickname} 打不过就跑了! 获得称号: {FlightBadgeGen(winner)}</size>", isSubtitles: true);
                    winnerLP?.SendBroadcast($"<size=27>你赢了 {loser.DisplayNickname} 的决斗! 对方将获得称号 {FlightBadgeGen(winner)}</size>", 10, shouldClearPrevious: true);

                    if (!PlayerToBadge.ContainsKey(loser.UserId)) PlayerToBadge.Add(loser.UserId, FlightBadgeGen(winner, false));
                    else PlayerToBadge[loser.UserId] = FlightBadgeGen(winner, false);

                    CurrentBattle = new CurrentBattleReq();
            CurrentBattling = false; tempFlightFlag = false;
                    CurrentBattling = false;

                    try { winner.TryRemoveFriendlyFire(RoleTypeId.Tutorial); } catch { }
                    try { winner.Role.Set(RoleTypeId.Spectator); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] OnLeft exception: {ex}");
            }
        }

        public static void OnChangingRole(ChangingRoleEventArgs ev)
        {
            try
            {
                if (!CurrentBattling) return;
                if (ev.Player == null) return;

                if (ev.Player == CurrentBattle.From || ev.Player == CurrentBattle.To)
                {
                    var loser = ev.Player;
                    var winner = loser == CurrentBattle.From ? CurrentBattle.To : CurrentBattle.From;
                    var winnerLP = LabApi.Features.Wrappers.Player.Get(winner.ReferenceHub);

                    string reason = ev.Reason.ToString() ?? "切换角色";



                    winnerLP?.SendBroadcast($"<size=27>{loser.DisplayNickname} 因 {reason} 切换了角色，决斗取消</size>", 5, shouldClearPrevious: true);
                    loser.Broadcast(5, $"<size=27>{loser.DisplayNickname} 因 {reason} 切换了角色，决斗取消</size>", shouldClearPrevious: true);

                    CurrentBattle = new CurrentBattleReq();
                    CurrentBattling = false;
                    CurrentBattling = false; tempFlightFlag = false;

                    try { winner.TryRemoveFriendlyFire(RoleTypeId.Tutorial); } catch { }
                    try { loser.TryRemoveFriendlyFire(RoleTypeId.Tutorial); } catch { }

                    try { winner.Role.Set(RoleTypeId.Spectator); } catch { }
                    try { loser.Role.Set(RoleTypeId.Spectator); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] OnChangingRole exception: {ex}");
            }
        }

        public static void OnVerify(VerifiedEventArgs ev)
        {
            try
            {
                if (ev.Player == null) return;
                if (PlayerToBadge.ContainsKey(ev.Player.UserId))
                {
                    FailedShowHandles.Add(Timing.RunCoroutine(FailedShow(ev.Player)));
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[FlightFailed] OnVerify exception: {ex}");
            }
        }

        // --- 显示称号的协程（降低频率，避免不断写网络变量） ---
        public static IEnumerator<float> FailedShow(Player player)
        {
            if (player == null) yield break;

            // 初始一次性设置（局部 try）
            if (PlayerToBadge.TryGetValue(player.UserId, out string badgeText))
            {
                try
                {
                    player.ReferenceHub.serverRoles.SetText(badgeText);
                    player.ReferenceHub.serverRoles.Network_myText = badgeText;
                    player.ReferenceHub.serverRoles.SetColor("yellow");
                    player.ReferenceHub.serverRoles.Network_myColor = "yellow";
                }
                catch (Exception ex)
                {
                    Log.Warn($"[FlightFailed] FailedShow initial set exception: {ex}");
                }
            }

            while (player != null && player.IsConnected && Round.IsStarted)
            {
                try
                {
                    if (PlayerToBadge.TryGetValue(player.UserId, out badgeText))
                    {
                        string current = player.ReferenceHub?.serverRoles?.Network_myText;
                        if (current == null || !current.Contains(badgeText))
                        {
                            try
                            {
                                player.Group = player.Group.Clone();
                                player.ReferenceHub.serverRoles.SetText(badgeText);
                                player.ReferenceHub.serverRoles.Network_myText = badgeText;
                                player.ReferenceHub.serverRoles.SetColor("yellow");
                                player.ReferenceHub.serverRoles.Network_myColor = "yellow";
                            }
                            catch (Exception exInner)
                            {
                                Log.Warn($"[FlightFailed] FailedShow setText exception: {exInner}");
                            }
                        }
                    }
                    else
                    {
                        // 没有要显示的称号了 -> 移除并结束协程
                        try
                        {
                            player.Group.BadgeText = null;
                            player.ReferenceHub.serverRoles.Network_myText = null;
                        }
                        catch { }
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[FlightFailed] FailedShow loop exception: {ex}");
                }

                // yield 在 try/catch 外部（或说不被含有 catch 的 try 包含）
                yield return Timing.WaitForSeconds(2f);
            }
        }


        // --- 称号生成器 ---
        public static string FlightBadgeGen(Player winner, bool HTML = true)
        {
            if (winner == null) return "猫娘喵";
            if (HTML)
            {
                return $"<color=yellow>我是{winner.Nickname}的猫娘喵❤❤</color>";
            }
            else
            {
                return $"我是{winner.Nickname}的猫娘喵❤❤";
            }
        }
    }

    // ---------------- Command classes (保留原来命令，但修正请求 key 名称等) ----------------

    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class StartBattleCommand : ICommand, IUsageProvider
    {
        public string[] Usage { get; } = new[] { "目标玩家/ID", "JailBird (可填0) / Gun (可填1) (默认JailBird)" };

        string ICommand.Command { get; } = "startBattle";
        string[] ICommand.Aliases { get; } = new[] { "startB" };
        string ICommand.Description { get; } = "还有战败play";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null || player.IsAlive)
            {
                response = "你不能发起决斗。";
                return false;
            }
            if (!Round.IsStarted)
            {
                response = "回合未开始。";
                return false;
            }
            if (FlightFailed.PlayerToBadge.ContainsKey(player.UserId))
            {
                response = "你已经是猫娘了喵 没法决斗了喵";
                return false;
            }

            var availableTarget = new List<Player>();
            foreach (var item in Player.Enumerable)
            {
                if (!item.IsAlive && !FlightFailed.PlayerToBadge.ContainsKey(item.UserId) && !FlightFailed.BattleReqs.Any(x => x.From == item || x.To == item || x.To_backup == item.UserId))
                {
                    availableTarget.Add(item);
                }
            }

            if (arguments.Count < 1)
            {
                response = "缺少目标参数! 以下为合法目标";
                foreach (var item in availableTarget)
                {
                    response += $"\n{item.DisplayNickname} ID:{item.Id}";
                }
                return false;
            }

            string[] newargs;
            List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);
            if (list == null || list.Count == 0 || list[0] == null)
            {
                response = "目标处理失败。";
                return false;
            }

            var target = Player.Get(list[0]);
            if (target == null || target.IsAlive)
            {
                response = "目标玩家不存在或活着。";
                return false;
            }
            if (target == player)
            {
                response = "不能向自己发起决斗。";
                return false;
            }

            var battleType = FlightFailed.BattleType.JailBird;
            if (arguments.Count > 1)
            {
                string typeStr = arguments.At(1).ToLower();
                if (typeStr == "0" || typeStr == "jailbird") battleType = FlightFailed.BattleType.JailBird;
                else if (typeStr == "1" || typeStr == "gun") battleType = FlightFailed.BattleType.Gun;
            }

            // 检查是否已有请求
            if (FlightFailed.BattleReqs.Any(r => r.From == player && (r.To == target || r.To_backup == target.UserId)))
            {
                response = "你已向此人发送过请求。";
                return false;
            }

            FlightFailed.BattleReqs.Add(new FlightFailed.BattleReq
            {
                From = player,
                From_backup = player.UserId,
                To = target,
                To_backup = target.UserId,
                Type = battleType,
                stopwatch = Stopwatch.StartNew()
            });

            var lp = target;
            lp?.AddMessage("FlightRequest", $"<size=27>{player.Nickname} 向你发起决斗！类型：{battleType}\n输入 acceptBattle 同意，refuseBattle 拒绝</size>", 10f);
            lp?.Broadcast(3, $"<size=27>{player.Nickname} 向你发起决斗！类型：{battleType}\n输入 acceptBattle 同意，refuseBattle 拒绝</size>");

            response = $"请求已发送。类型{battleType}";
            return true;
        }
    }

    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class BattleArgeeCommand : ICommand, IUsageProvider
    {
        string ICommand.Command { get; } = "acceptBattle";
        string[] ICommand.Aliases { get; } = new[] { "AB" };
        string ICommand.Description { get; } = "同意决斗";
        public string[] Usage { get; } = new[] { "ID" };

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Failed to find sender";
                return false;
            }
            if (!Round.IsStarted)
            {
                response = "回合未开始";
                return false;
            }

            var found = false;
            FlightFailed.BattleReq foundReq = new FlightFailed.BattleReq();
            for (int i = 0; i < FlightFailed.BattleReqs.Count; i++)
            {
                var item = FlightFailed.BattleReqs[i];
                if (item.To == player || item.To_backup == player.UserId)
                {
                    found = true;
                    foundReq = item;
                    break;
                }
            }

            if (!found)
            {
                response = "没有人找你决斗";
                return false;
            }

            if (player.IsAlive)
            {
                response = "你还活着";
                FlightFailed.BattleReqs.Remove(foundReq);
                return false;
            }
            if (foundReq.From != null && foundReq.From.IsAlive)
            {
                response = "对方还活着";
                FlightFailed.BattleReqs.Remove(foundReq);
                return false;
            }

            FlightFailed.BattleReqs.Remove(foundReq);
            FlightFailed.WaitingBattleReqs.Add(foundReq);
            response = "成功";
            return true;
        }
    }

    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    class RABattleGroupCommand : ICommand, IUsageProvider
    {
        string ICommand.Command { get; } = "ForceBattle";
        string[] ICommand.Aliases { get; } = new[] { "fB" };
        string ICommand.Description { get; } = "强制决斗";
        public string[] Usage { get; } = new[] { "ID" };

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Failed to find sender";
                return false;
            }

            string[] newargs;
            List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);
            if (list == null || list.Count == 0 || list[0] == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                return false;
            }

            var target = Player.Get(list[0]);
            if (target == null || target.IsAlive)
            {
                response = "目标玩家不存在或活着。";
                return false;
            }
            if (target == player)
            {
                response = "不能向自己发起决斗。";
                return false;
            }

            target?.Broadcast(3, $"<size=27>{player.Nickname} 向你发起决斗！你无法拒绝因为你被强制爱了</size>");

            var battleType = FlightFailed.BattleType.JailBird;
            if (arguments.Count > 1)
            {
                string typeStr = arguments.At(1).ToLower();
                if (typeStr == "0" || typeStr == "jailbird") battleType = FlightFailed.BattleType.JailBird;
                else if (typeStr == "1" || typeStr == "gun") battleType = FlightFailed.BattleType.Gun;
            }

            FlightFailed.WaitingBattleReqs.Add(new FlightFailed.BattleReq()
            {
                From = player,
                To = target,
                To_backup = target.UserId,
                Type = battleType,
                stopwatch = Stopwatch.StartNew()
            });

            response = "成功";
            return true;
        }
    }

    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class BattleRefuseCommand : ICommand, IUsageProvider
    {
        string ICommand.Command { get; } = "refuseBattle";
        string[] ICommand.Aliases { get; } = new[] { "RB" };
        string ICommand.Description { get; } = "拒绝决斗";
        public string[] Usage { get; } = new[] { "ID" };

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Failed to find sender";
                return false;
            }

            // 找到所有发给我的请求并移除
            for (int i = FlightFailed.BattleReqs.Count - 1; i >= 0; i--)
            {
                var item = FlightFailed.BattleReqs[i];
                if (item.To == player || item.To_backup == player.UserId)
                {
                    var fromLP = Player.Get(item.From?.ReferenceHub);
                    fromLP?.RemoveMessage("FlightRequest");
                    item.From?.Broadcast(3, $"<size=27>玩家 {player.DisplayNickname} 拒绝了你的决斗请求!</size>", shouldClearPrevious: true);
                }
            }
            FlightFailed.BattleReqs.RemoveAll(x => x.To == player || x.To_backup == player.UserId);
            response = "成功";
            return true;
        }
    }
}
