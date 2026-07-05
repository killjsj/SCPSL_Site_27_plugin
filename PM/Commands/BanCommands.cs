using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Utils;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class BanCommand : ICommand, IUsageProvider
    {
        public string Command => "sban";
        public string[] Aliases => new string[] { "" };
        public string Description => "封禁玩家";
        public string[] Usage => new string[] { "userId/playerID", "time", "reason" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner == null) { response = "failed to find player"; return false; }
            if (arguments.Count < 3) { response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage(); return false; }

            string[] array;
            List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
            if (list == null || list.Count <= 0)
            {
                string targetUserID = arguments.At(0);
                string text = string.Empty;
                if (array.Length > 1) text = array.Skip(1).Aggregate((string current, string n) => current + " " + n);
                long num;
                try { num = Misc.RelativeTimeToSeconds(array[0], 60); }
                catch { response = "Invalid time: " + array[0]; return false; }
                if (num < 0L) { num = 0L; array[0] = "0"; }
                if (!sender.CheckPermission(new PlayerPermissions[] { PlayerPermissions.KickingAndShortTermBanning, PlayerPermissions.BanningUpToDay, PlayerPermissions.LongTermBanning }, out response)) return false;

                ushort num2 = 0, num3 = 0;
                string text2 = string.Empty;
                try
                {
                    string combinedName = targetUserID;
                    CommandSender commandSender = sender as CommandSender;
                    ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.LogName} banned player {targetUserID}. Ban duration: {array[0]}. Reason: {(text == string.Empty ? "(none)" : text)}.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
                    PlayerManager.sql.InsertBanRecord(targetUserID, targetUserID, runner.UserId, runner.Nickname, text, DateTime.Now, end_time: DateTime.Now.AddSeconds(num), ServerStatic.ServerPort.ToString());
                    foreach (var item in Player.Enumerable)
                        if (item.UserId == targetUserID) item.Kick(text);
                    num2 += 1;
                    response = $"Done! {sender.LogName} banned player {targetUserID}. Ban duration: {array[0]}. Reason: {(text == string.Empty ? "(none)" : text)}.";
                    return true;
                }
                catch (Exception ex) { num3 += 1; text2 = "Error occured during banning: " + ex.Message + ".\n" + ex.StackTrace; }

                if (num3 == 0)
                {
                    int num4;
                    string arg = (int.TryParse(array[0], out num4) && num4 > 0) ? "Banned" : "Kicked";
                    response = $"Done! {arg} {num2} player{(num2 == 1 ? "!" : "s!")}";
                    return true;
                }
                response = $"Failed to execute the command! Failures: {num3}\nLast error log:\n{text2}";
                return false;
            }
            else
            {
                if (array == null) { response = "An error occured while processing this command.\nUsage: " + this.DisplayCommandUsage(); return false; }
                string text = string.Empty;
                if (array.Length > 1) text = array.Skip(1).Aggregate((string current, string n) => current + " " + n);
                long num;
                try { num = Misc.RelativeTimeToSeconds(array[0], 60); }
                catch { response = "Invalid time: " + array[0]; return false; }
                if (num < 0L) { num = 0L; array[0] = "0"; }
                if (!sender.CheckPermission(new PlayerPermissions[] { PlayerPermissions.KickingAndShortTermBanning, PlayerPermissions.BanningUpToDay, PlayerPermissions.LongTermBanning }, out response)) return false;

                ushort num2 = 0, num3 = 0;
                string text2 = string.Empty;
                foreach (ReferenceHub referenceHub in list)
                {
                    try
                    {
                        if (referenceHub == null) { num3 += 1; }
                        else
                        {
                            ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.LogName} banned player {referenceHub.LoggedNameFromRefHub()}. Ban duration: {array[0]}. Reason: {(text == string.Empty ? "(none)" : text)}.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
                            PlayerManager.sql.InsertBanRecord(referenceHub.authManager.UserId, referenceHub.nicknameSync.MyNick, runner.UserId, runner.Nickname, text, DateTime.Now, end_time: DateTime.Now.AddSeconds(num), ServerStatic.ServerPort.ToString());
                            BanPlayer.KickUser(referenceHub, sender, text);
                            num2 += 1;
                        }
                    }
                    catch (Exception ex) { num3 += 1; text2 = "Error occured during banning: " + ex.Message + ".\n" + ex.StackTrace; }
                }
                if (num3 == 0)
                {
                    int num4;
                    string arg = (int.TryParse(array[0], out num4) && num4 > 0) ? "Banned" : "Kicked";
                    response = $"Done! {arg} {num2} player{(num2 == 1 ? "!" : "s!")}";
                    return true;
                }
                response = $"Failed to execute the command! Failures: {num3}\nLast error log:\n{text2}";
                return false;
            }
        }
    }

    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class cbanCommand : ICommand
    {
        public string Command => "cban";
        public string[] Aliases => new string[0] { };
        public string Description => "查询封禁记录";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner == null) { response = "failed to find player"; return false; }
            if (arguments.Count == 0) { response = "空空如也"; return false; }
            response = "Done!";

            List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out _, false);
            if (list == null || list.Count <= 0)
            {
                string targetUserID = arguments.At(0);
                var Pbans = PlayerManager.sql.QueryAllBan(targetUserID);
                if (Pbans != null)
                    foreach (var arg in Pbans)
                        response += $"{arg.start_time} 到 {arg.end_time} by:{arg.issuer_name} reason:{arg.reason} \n";
                return true;
            }
            else
            {
                if (!sender.CheckPermission(new PlayerPermissions[] { PlayerPermissions.KickingAndShortTermBanning, PlayerPermissions.BanningUpToDay, PlayerPermissions.LongTermBanning }, out response))
                    return false;
                var target = list[0];
                if (target == null) { response = "Fialed To get target"; return false; }
                var Pbans = PlayerManager.sql.QueryAllBan(target.authManager.UserId);
                if (Pbans != null)
                    foreach (var arg in Pbans)
                        response += $"{arg.start_time} 到 {arg.end_time} by:{arg.issuer_name} reason:{arg.reason} \n";
                return true;
            }
        }
    }
}
