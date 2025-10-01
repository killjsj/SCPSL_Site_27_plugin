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
using static Next_generationSite_27.UnionP.testing.FlightFailed;

namespace Next_generationSite_27.UnionP.testing
{
    class FlightFailed
    {
        public static void Start()
        {
            Log.Info("FlightFailed");
            // 在你的 Plugin.cs 或 EventHandlers.cs 初始化时
            Timing.RunCoroutine(FlightFailed.While());
            
        }
        public static IEnumerator<float> While()
        {
            while (true)
            {
                if (Round.IsLobby || Round.IsEnded)
                {
                    break;
                }
                try
                {
                    foreach (var item in BattleReqs)
                    {
                        if(item.stopwatch.Elapsed.TotalSeconds >= 30)
                        {
                            var fromLP = Player.Get(item.From.ReferenceHub);
                            fromLP.RemoveMessage("FlightReqeust");
                            var toPlayer = Player.Get(item.To);
                            if(toPlayer == null)
                            {
                                toPlayer = Player.Get(item.To_backup);
                            }
                            if (toPlayer != null)
                            {
                                toPlayer.Broadcast(3, $"<size=27>无法开始决斗 你与 {toPlayer.DisplayNickname} 长时间未响应，决斗请求已取消</size>", shouldClearPrevious: true);
                            }
                            item.From.Broadcast(3, $"<size=27>对方长时间未响应，决斗请求已取消</size>", shouldClearPrevious: true);
                            BattleReqs.Remove(item);
                        }
                    }
                    if (WaitingBattleReqs.Count > 0 && !CurrentBattling)
                    {
                        Log.Info("processing battle");
                        var battleReq = WaitingBattleReqs[0];
                        WaitingBattleReqs.RemoveAt(0);
                        if (battleReq.From != null && battleReq.To != null && !battleReq.From.IsAlive)
                        {


                            var fromLP = Player.Get(battleReq.From.ReferenceHub);
                            fromLP.RemoveMessage("FlightReqeust");
                            var toPlayer = Player.Get(battleReq.To);
                            var fromPlayer = Player.Get(battleReq.From.UserId);
                            if (toPlayer == null || toPlayer.IsAlive || battleReq.From.IsAlive)
                            {
                                if (toPlayer != null)
                                {
                                    toPlayer.Broadcast(3, $"<size=27>无法开始决斗 你与 {toPlayer.DisplayNickname} 之中有一个人活着或离开了</size>", shouldClearPrevious: true);

                                }
                                fromLP.Broadcast(3,$"<size=27>无法开始决斗 你与 {toPlayer.DisplayNickname} 之中有一个人活着或离开了</size>", shouldClearPrevious: true);
                                CurrentBattle = new CurrentBattleReq();
                                continue;
                            }
                            CurrentBattle = new CurrentBattleReq()
                            {
                                To = toPlayer,
                                From = fromPlayer,
                                Type = battleReq.Type,
                            };
                            var toLP = Player.Get(toPlayer.ReferenceHub);
                            toLP.RemoveMessage("FlightReqeust");
                            fromLP.Broadcast(5, $"<size=27>你与 {toPlayer.DisplayNickname} 的决斗开始!战斗类型:{battleReq.Type.ToString()}</size>",  shouldClearPrevious: true);
                            toLP.Broadcast(5, $"<size=27>你与 {battleReq.From.DisplayNickname} 的决斗开始!战斗类型:{battleReq.Type.ToString()}</size>",  shouldClearPrevious: true);
                            CurrentBattling = true;
                            if (battleReq.Type == BattleType.JailBird)
                            {
                                fromPlayer.Role.Set(RoleTypeId.Tutorial);
                                toPlayer.Role.Set(RoleTypeId.Tutorial);
                                fromPlayer.SetFriendlyFire(RoleTypeId.Tutorial, 1);
                                toPlayer.SetFriendlyFire(RoleTypeId.Tutorial, 1);
                                Timing.CallDelayed(0.1f, () =>
                                {
                                    fromPlayer.Health = 300;
                                    toPlayer.Health = 300;
                                    fromPlayer.AddItem(itemType: ItemType.Jailbird);
                                    fromPlayer.AddItem(itemType: ItemType.Jailbird);
                                    fromPlayer.AddItem(itemType: ItemType.Jailbird);
                                    toPlayer.AddItem(itemType: ItemType.Jailbird);
                                    toPlayer.AddItem(itemType: ItemType.Jailbird);
                                    toPlayer.AddItem(itemType: ItemType.Jailbird);
                                    fromPlayer.AddItem(itemType: ItemType.Painkillers);
                                    toPlayer.AddItem(itemType: ItemType.Painkillers);

                                });
                            }
                            else if (battleReq.Type == BattleType.Gun)
                            {
                                fromPlayer.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.UseSpawnpoint);
                                toPlayer.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.UseSpawnpoint);
                                fromPlayer.SetFriendlyFire(RoleTypeId.Tutorial, 1);
                                toPlayer.SetFriendlyFire(RoleTypeId.Tutorial, 1);
                                Timing.CallDelayed(0.2f, () =>
                                {
                                    fromPlayer.Health = 500;
                                    toPlayer.Health = 500;
                                    fromPlayer.Health = 500;
                                    toPlayer.Health = 500;
                                    fromPlayer.AddItem(itemType: ItemType.GunE11SR);
                                    fromPlayer.AddItem(itemType: ItemType.Ammo556x45);
                                    fromPlayer.AddItem(itemType: ItemType.Ammo556x45);
                                    fromPlayer.AddItem(itemType: ItemType.Ammo556x45);
                                    toPlayer.AddItem(itemType: ItemType.GunE11SR);
                                    toPlayer.AddItem(itemType: ItemType.Ammo556x45);
                                    toPlayer.AddItem(itemType: ItemType.Ammo556x45);
                                    toPlayer.AddItem(itemType: ItemType.Ammo556x45);
                                    fromPlayer.AddItem(itemType: ItemType.Medkit);
                                    toPlayer.AddItem(itemType: ItemType.Medkit);

                                });
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.Info(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1f); // 加个等待
            }
            CleanupOnRoundEnd();
            yield break;
        }

        private static void CleanupOnRoundEnd()
        {
            foreach (var handle in FailedShowHandles)
                Timing.KillCoroutines(handle);
            FailedShowHandles.Clear();

            BattleReqs.Clear();
            WaitingBattleReqs.Clear();
            CurrentBattle = new CurrentBattleReq();
            PlayerToBadge.Clear();
            CurrentBattling = false;
        }
        public static void OnDied(DyingEventArgs ev)
        {
            if (ev.Attacker != null &&CurrentBattling && (ev.Player == CurrentBattle.From || ev.Player == CurrentBattle.To))
            {
                var winner = ev.Attacker;
                var loser = ev.Player;
                var winnerLP = LabApi.Features.Wrappers.Player.Get(winner.ReferenceHub);
                var loserLP = LabApi.Features.Wrappers.Player.Get(loser.ReferenceHub);
                winnerLP.SendBroadcast($"<size=27>你赢了 {loser.DisplayNickname} 的决斗!对方将获得称号 {FlightBadgeGen(winner)}</size>", 10, shouldClearPrevious: true);
                loserLP.SendBroadcast($"<size=27>你输了 {winner.DisplayNickname} 的决斗!并获得称号 {FlightBadgeGen(winner)}</size>", 10, shouldClearPrevious: true);
                Cassie.Message($"<size=16>{winner.DisplayNickname}击败了{loser.DisplayNickname}! 并获得称号 {FlightBadgeGen(winner)}</size>", isSubtitles: true);
                if (!PlayerToBadge.ContainsKey(loser.UserId))
                {
                    PlayerToBadge.Add(loser.UserId, FlightBadgeGen(winner, false));
                }
                else
                {
                    PlayerToBadge[loser.UserId] = FlightBadgeGen(winner, false);
                }
                CurrentBattle = new CurrentBattleReq();
                CurrentBattling = false;
                winner.ClearItems();
                loser.ClearItems();
                winner.Role.Set(RoleTypeId.Spectator);
                loser.Role.Set(RoleTypeId.Spectator);
                winner.TryRemoveFriendlyFire(RoleTypeId.Tutorial);
                loser.TryRemoveFriendlyFire(RoleTypeId.Tutorial);
                FailedShowHandles.Add(Timing.RunCoroutine(FailedShow(ev.Player)));
                foreach(var i  in ev.ItemsToDrop)
                {
                    i.Destroy();
                }
                
                ev.IsAllowed = false;
            }
        }
        public static void OnHurt(HurtingEventArgs ev)
        {
            if (CurrentBattling && (ev.Player == CurrentBattle.From || ev.Player == CurrentBattle.To))
            {
                if (ev.Attacker != null)
                {
                    if (ev.Attacker != CurrentBattle.From && ev.Attacker != CurrentBattle.To)
                    {
                        ev.IsAllowed = false;
                        ev.Attacker.Broadcast(3, "不准打扰决斗", shouldClearPrevious: true);
                    }
                }
            }
        }
        public static void OnLeft(LeftEventArgs ev)
        {
            if (CurrentBattling && (ev.Player == CurrentBattle.From || ev.Player == CurrentBattle.To))
            {
                var winner = ev.Player == CurrentBattle.From ? CurrentBattle.To : CurrentBattle.From;
                var loser = ev.Player;
                var winnerLP = LabApi.Features.Wrappers.Player.Get(winner.ReferenceHub);
                Cassie.Message( $"<size=16>{loser.DisplayNickname}打不过就跑了喵! 获得称号:{FlightBadgeGen(winner)}</size>", isSubtitles:true);
                winnerLP.SendBroadcast($"<size=27>你赢了 {loser.DisplayNickname} 的决斗!对方将获得称号 {FlightBadgeGen(winner)}</size>", 10, shouldClearPrevious: true);
                if (!PlayerToBadge.ContainsKey(loser.UserId))
                {
                    PlayerToBadge.Add(loser.UserId, FlightBadgeGen(winner,false));
                }
                else
                {
                    PlayerToBadge[loser.UserId] = FlightBadgeGen(winner,false);
                }
                CurrentBattle = new CurrentBattleReq();
                CurrentBattling = false;
                winner.TryRemoveFriendlyFire(RoleTypeId.Tutorial);
                winner.Role.Set(RoleTypeId.Spectator);
            }
        }
        public static void OnVerify(VerifiedEventArgs ev)
        {
            if (PlayerToBadge.ContainsKey(ev.Player.UserId))
            {
                FailedShowHandles.Add(Timing.RunCoroutine(FailedShow(ev.Player)));
            }
        }
        public static List<CoroutineHandle> FailedShowHandles = new List<CoroutineHandle>();
        public static IEnumerator<float> FailedShow(Player player)
        {

            while (player != null && player.IsConnected && !Round.IsLobby && !Round.IsEnded)
            {
                try
                {
                    if (PlayerToBadge.TryGetValue(player.UserId, out string badgeText))
                    {
                        if (player.ReferenceHub.serverRoles.Network_myText != badgeText)
                        {
                            player.Group = player.Group.Clone();

                            player.ReferenceHub.serverRoles.SetText(badgeText);
                            player.ReferenceHub.serverRoles.Network_myText = badgeText;
                            player.ReferenceHub.serverRoles.SetColor("yellow");
                            player.ReferenceHub.serverRoles.Network_myColor = "yellow";
                        }
                    }
                    else if (player.ReferenceHub.serverRoles.Network_myText != null)
                    {
                        player.Group.BadgeText = null;
                        player.ReferenceHub.serverRoles.Network_myText = null;
                        yield break;
                    }
                }
                catch(Exception ex)
                { 
                    Log.Info(ex.ToString());
                }

                yield return Timing.WaitForSeconds(0.2f);
            }
                player.Group.BadgeText = "";
        }
        public struct BattleReq
        {
            public Player From;
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
        public static CurrentBattleReq CurrentBattle;
        public static bool CurrentBattling = false;
        public static List<BattleReq> BattleReqs = new List<BattleReq>();
        public static List<BattleReq> WaitingBattleReqs = new List<BattleReq>();
        public enum BattleType
        {
            JailBird,
            Gun,
        }
        public static string FlightBadgeGen(Player Winner,bool HTML = true)
        {
            if (HTML)
            {
                return $"<color=yellow>我是{Winner.Nickname}的猫娘喵❤❤</color>";
            }else
            {
                return $"我是{Winner.Nickname}的猫娘喵❤❤";

            }
        }
        public static Dictionary<string, string> PlayerToBadge = new Dictionary<string, string>();

    }

    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]

    class StartBattleCommand : ICommand, IUsageProvider
    {
        public string[] Usage { get; } = new[] {"目标玩家/ID", "JailBird (可填0) / Gun (可填1) (默认JailBird)" };

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
            if (FlightFailed.PlayerToBadge.ContainsKey(player.UserId))
            {
                response = "你已经是猫娘了喵 没法决斗了喵";
                return false;
            }
            var availableTarget = new List<Player>();
            foreach (var item in Player.List)
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
            if (list == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                return false;
            }
            if (list[0] == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing. list的第一个是null";
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
            var battleType = BattleType.JailBird;
            if (arguments.Count > 1)
            {
             string typeStr = arguments.At(1).ToLower();
                if (typeStr == "0" || typeStr == "jailbird") battleType = BattleType.JailBird;
                else if (typeStr == "1" || typeStr == "gun") battleType = BattleType.Gun;
                else battleType = BattleType.JailBird;
            }

            // 检查是否已有请求
            if (BattleReqs.Any(r => r.From == player && (r.To == target || r.To_backup == target.UserId) ))
            {
                response = "你已向此人发送过请求。";
                return false;
            }

            BattleReqs.Add(new BattleReq { From = player, To = target,To_backup = target.UserId, Type = battleType,stopwatch = Stopwatch.StartNew() });

            var lp = target;
            lp?.AddMessage("FlightReqeust", $"<size=27>{player.Nickname} 向你发起决斗！类型：{battleType}\n输入 acceptBattle 同意，refuseBattle 拒绝</size>", 10f);
            lp?.Broadcast(3,$"<size=27>{player.Nickname} 向你发起决斗！类型：{battleType}\n输入 acceptBattle 同意，refuseBattle 拒绝</size>");

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

            var ReqFound = false;
            FlightFailed.BattleReq battleReq = new FlightFailed.BattleReq();
            foreach (var item in FlightFailed.BattleReqs)
            {
                if (item.To == player || item.To_backup == player.UserId)
                {
                    ReqFound = true;
                    battleReq = item;
                    break;
                }
            }
            if (!ReqFound)
            {
                response = "没有人找你换";
                return false;
            }
            if (!player.IsAlive)
            {
                response = "你还活着";
                FlightFailed.BattleReqs.Remove(battleReq);
                return false;
            }
            if (!battleReq.From.IsAlive)
            {
                response = "对方还活着";
                FlightFailed.BattleReqs.Remove(battleReq);
                return false;
            }
            FlightFailed.BattleReqs.Remove(battleReq);
            FlightFailed.WaitingBattleReqs.Add(battleReq);
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
            if (list == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                return false;
            }
            if (list[0] == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing. list的第一个是null";
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
            target?.Broadcast(3,$"<size=27>{player.Nickname} 向你发起决斗！你无法拒绝因为你被强制爱了</size>");

            var battleType = BattleType.JailBird;
            if (arguments.Count > 1)
            {
                string typeStr = arguments.At(1).ToLower();
                if (typeStr == "0" || typeStr == "jailbird") battleType = BattleType.JailBird;
                else if (typeStr == "1" || typeStr == "gun") battleType = BattleType.Gun;
                else battleType = BattleType.JailBird;
            }
            FlightFailed.WaitingBattleReqs.Add(new BattleReq() { From=player,
                To=target,
                To_backup=target.UserId,
                Type = battleType

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
            foreach (var item in BattleReqs)
            {
                if (item.To == player || item.To_backup == player.UserId)
                {
                    var fromLP = Player.Get(item.From.ReferenceHub);
                    fromLP.RemoveMessage("FlightReqeust");
                    item.From.Broadcast(3, $"<size=27>玩家 {player.DisplayNickname} 拒绝了你的决斗请求!</size>", shouldClearPrevious: true);
                    break;
                }
            }
            BattleReqs.RemoveAll(x => x.To == player || x.To_backup == player.UserId);
            response = "成功";
            return true;

        }
    }
}
