using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Features;
using MEC;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using UnionApi;
using Utils;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    class ExperienceManager : BaseClass
    {
        public static MySQLConnect sql => Plugin.plugin.connect;
        public static double global_experience_multiplier = 1;

        public static Dictionary<Player, int> expCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> levelCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> UidCache = new Dictionary<Player, int>();
        public static Dictionary<Player, int> PointCache = new Dictionary<Player, int>();
        public static Dictionary<Player, Stopwatch> TodayTimer = new Dictionary<Player, Stopwatch>();
        public static Dictionary<Player, TimeSpan> TodayTimeCache = new Dictionary<Player, TimeSpan>();

        public delegate void OnExpUpDelegate(Player player, int NewExp);
        public static event OnExpUpDelegate OnExpUp;

        public override void Init() { }
        public override void Delete() { }

        public static ExpTier GetLevel(Player player)
        {
            return ExpToLevel(GetExperience(player));
        }

        public static int GetExperience(Player player)
        {
            if (player == null) return 0;
            if (player.IsNPC) return -1;
            if (expCache.ContainsKey(player))
                return expCache[player];
            var l = sql.QueryUser(player.UserId).experience;
            expCache[player] = l;
            return l;
        }

        public static void SetExp(Player player, int exp)
        {
            if (player == null) return;
            if (player.IsNPC) return;
            if (exp < 0) exp = 0;
            expCache[player] = exp;
            sql.Update(player.UserId, experience: exp);
        }

        public static void AddExp(Player player, int exp, bool igronMul = false, AddExpReason reason = AddExpReason.Custom, string CustomReasonStr = "")
        {
            if (player == null || !player.IsConnected) return;
            if (global_experience_multiplier <= 0) return;
            if (player.IsNPC) return;

            var pU = sql.QueryUser(player.UserId);
            int currentExp = GetExperience(player);
            int totalExp;
            double experience_multiplier = Math.Max((double)1, pU.experience_multiplier.Value);
            if (!igronMul)
            {
                totalExp = (int)(currentExp + exp * experience_multiplier * global_experience_multiplier);
                player.AddMessage("ExpUpdated", $"<color=green><size=23>🔔获得经验:{(exp * experience_multiplier * global_experience_multiplier).ToString("F0")}</size></color>", 3f, ScreenLocation.CenterBottom);
            }
            else
            {
                totalExp = currentExp + exp;
                player.AddMessage("ExpUpdated", $"<color=green><size=23>🔔获得经验:{exp.ToString("F0")}</size></color>", 3f, ScreenLocation.CenterBottom);
            }

            string reasonStr = AddExpReasonToString(reason, CustomReasonStr);
            if (igronMul)
                player.SendConsoleMessage($"你获得{(exp)} = {exp} * 1 * 1 原因:{reasonStr}", "grenn");
            else
                player.SendConsoleMessage($"你获得{(exp * experience_multiplier * global_experience_multiplier)} = {exp} * {pU.experience_multiplier} * {global_experience_multiplier} 原因:{reasonStr}", "grenn");

            SetExp(player, totalExp);
            OnExpUp?.Invoke(player, totalExp);
            UnionApi.ExperienceApi.InvokeOnExpUp(player, totalExp);
        }

        private static string AddExpReasonToString(AddExpReason reason, string customStr)
        {
            return reason switch
            {
                AddExpReason.DayLogin => "今日登录",
                AddExpReason.PeopleKillPeoPle => "击杀人类",
                AddExpReason.ScpKillPeoPle => "击杀人类",
                AddExpReason.KillZombie => "击杀Scp049-2",
                AddExpReason.killScp => "击杀SCP",
                AddExpReason.DDSCIEscaped => "逃跑成功",
                AddExpReason.GuardEscaped => "下班",
                AddExpReason.CuffedPeopleEscaped => "捆绑的中立单位撤离",
                AddExpReason.RoundEnd => "回合结束",
                AddExpReason.ScpWin => "Scp获胜",
                AddExpReason.HumanWin => "人类获胜",
                AddExpReason.RaAdded => "管理指令",
                AddExpReason.Scp079Gener => "阵营启动发电机",
                _ => customStr
            };
        }

        public static int GetPoint(Player player)
        {
            if (player == null) return 0;
            if (PointCache.ContainsKey(player))
                return PointCache[player];
            var l = sql.QueryUser(player.UserId).point;
            PointCache[player] = l;
            return l;
        }

        public static void SetPoint(Player player, int point)
        {
            if (player == null) return;
            if (point < 0) point = 0;
            sql.Update(player.UserId, point: point);
            PointCache[player] = point;

            var stats = PlayerManager.GetOrCreateStats(player);
            if (stats != null)
                stats.Points = point;
        }

        public static void AddPoint(Player player, int point)
        {
            if (player == null) return;
            SetPoint(player, point: GetPoint(player) + point);
        }

        public static int GetUid(Player player)
        {
            if (player == null) return 0;
            if (player.IsNPC) return -1;
            if (UidCache.ContainsKey(player))
                return UidCache[player];
            var l = sql.QueryUser(player.UserId).uid;
            UidCache[player] = l;
            return l;
        }

        public static TimeSpan GetTodayTimer(Player player)
        {
            if (player == null) return default;
            if (TodayTimer.ContainsKey(player))
            {
                if (TodayTimeCache.ContainsKey(player))
                    return TodayTimer[player].Elapsed + TodayTimeCache[player];
                else
                    return TodayTimer[player].Elapsed;
            }
            else
            {
                TodayTimer[player] = Stopwatch.StartNew();
                if (TodayTimeCache.ContainsKey(player))
                    return TodayTimer[player].Elapsed + TodayTimeCache[player];
                else
                {
                    var l = sql.QueryUser(player.UserId).today_duration;
                    if (l.HasValue)
                    {
                        TodayTimeCache[player] = l.Value;
                        return TodayTimer[player].Elapsed + l.Value;
                    }
                    else
                        return TimeSpan.Zero;
                }
            }
        }

        public enum ExpTier
        {
            Small,
            Medium,
            Large,
            Pot,
            Shao,
            Eat,
            EatPlus,
            Robot,
        }

        public static ExpTier ExpToLevel(int currentExp)
        {
            if (currentExp == -1) return ExpTier.Robot;
            if (currentExp <= 100) return ExpTier.Small;
            else if (currentExp <= 300) return ExpTier.Medium;
            else if (currentExp <= 800) return ExpTier.Large;
            else if (currentExp <= 1500) return ExpTier.Pot;
            else if (currentExp <= 3000) return ExpTier.Shao;
            else if (currentExp <= 10000) return ExpTier.Eat;
            else return ExpTier.EatPlus;
        }

        public static int ExpToNextLevel(ExpTier currentLevel)
        {
            return currentLevel switch
            {
                ExpTier.Small => 100,
                ExpTier.Medium => 300,
                ExpTier.Large => 800,
                ExpTier.Pot => 1500,
                ExpTier.Shao => 3000,
                ExpTier.Eat => 10000,
                ExpTier.EatPlus or ExpTier.Robot => 0,
                _ => 10000,
            };
        }

        public static string LevelToName(ExpTier currentLevel)
        {
            return currentLevel switch
            {
                ExpTier.Small => "小份薯条",
                ExpTier.Medium => "中份薯条",
                ExpTier.Large => "大份薯条",
                ExpTier.Pot => "炸锅",
                ExpTier.Shao => "漏勺",
                ExpTier.EatPlus or ExpTier.Eat => "吃薯条",
                ExpTier.Robot => "人机",
                _ => "?",
            };
        }

        public enum AddExpReason
        {
            Custom,
            DayLogin,
            PeopleKillPeoPle,
            ScpKillPeoPle,
            KillZombie,
            killScp,
            DDSCIEscaped,
            GuardEscaped,
            CuffedPeopleEscaped,
            RoundEnd,
            ScpWin,
            HumanWin,
            RaAdded,
            Scp079Gener
        }

        [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class ExpxCommand : ICommand
        {
            public string Command => "expx";
            public string[] Aliases => new string[0] { };
            public string Description => "修改倍率";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var s = Player.Get(sender);
                if (s == null) { response = "failed to find player"; return false; }
                if (s.KickPower < 244) { response = "KickPower 小于 244 !"; return false; }
                if (arguments.Count == 0) { response = "空空如也"; return false; }
                double g = double.Parse(arguments.At(0));
                global_experience_multiplier = g;
                response = "Done!";
                return true;
            }
        }

        [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
        class AddExpCommand : ICommand, IUsageProvider
        {
            public string[] Usage { get; } = new[] { "playerID", "exp" };
            string ICommand.Command { get; } = "AddPlayerExp";
            string[] ICommand.Aliases { get; } = new[] { "" };
            string ICommand.Description { get; } = "添加经验";

            bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var runner = Player.Get(sender);
                if (runner.KickPower < 244) { response = "KickPower 小于 244 !"; return false; }
                List<ReferenceHub> list;
                if (arguments.Count >= 2)
                {
                    list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out var newargs);
                    if (list == null) { response = "An unexpected problem has occurred during PlayerId/Name array processing."; return false; }
                    var exp = int.Parse(newargs[0]);
                    foreach (var item in list)
                        AddExp(Player.Get(item), exp, true, reason: AddExpReason.RaAdded);
                    response = $"done added {exp}!";
                    return true;
                }
                else { response = "To execute this command provide at least 2 arguments!"; return false; }
            }
        }
    }
}
