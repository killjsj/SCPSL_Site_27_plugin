using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Pickups;
using HintServiceMeow.UI.Utilities;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerRoles;
using PlayerStatsSystem;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utf8Json.Formatters;
using Utils;

namespace Next_generationSite_27.UnionP
{
    [CommandSystem.CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    class SnakeHighestCommand : ICommand
    {
        string ICommand.Command { get; } = "HighScore";

        string[] ICommand.Aliases { get; } = new[] { "HS", "snake" };

        string ICommand.Description { get; } = "查询贪吃蛇最高分";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = null;
            player = Player.Get(sender);

            var plugin = Plugin.plugin;
            if (!plugin.connect.connected)
            {
                response = "未连接mysql服务器";
                return false;
            }
            var serverHigh = plugin.connect.QueryHighest();
            if (player == null)
            {
                response = "Player not found.\n";
                if (serverHigh.highscore.HasValue)
                {
                    Log.Info(serverHigh.name);

                    if (!string.IsNullOrEmpty(serverHigh.name))
                    {
                        response += $"服务器最高分:由 {serverHigh.name} 建立,最高分:{serverHigh.highscore.Value},时间:{serverHigh.time}\n";
                    }
                    else
                    {
                        response += $"服务器最高分:由 {serverHigh.userid} 建立,最高分:{serverHigh.highscore.Value},时间:{serverHigh.time}\n";
                    }
                }
                else
                {
                    response += $"还没有服务器最高分哦\n";

                }

                return true;
            }
            response = "";
            var persHigh = plugin.connect.Query(player.UserId);

            if (!persHigh.highscore.HasValue)
            {
                response += "你还没有最高分哦\n";
            }
            else
            {
                response += $"个人最高分:{persHigh.highscore.Value},时间:{persHigh.time}\n";
            }
            if (serverHigh.highscore.HasValue)
            {
                if (!string.IsNullOrEmpty(serverHigh.name))
                {
                    response += $"服务器最高分:由 {serverHigh.name} 建立,最高分:{serverHigh.highscore.Value},时间:{serverHigh.time}\n";
                }
                else
                {
                    response += $"服务器最高分:由 {serverHigh.userid} 建立,最高分:{serverHigh.highscore.Value},时间:{serverHigh.time}\n";
                }
            }
            else
            {
                response += $"还没有服务器最高分哦\n";

            }
            return true;
        }


    }
    [CommandSystem.CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    class SnakeRankCommand : ICommand
    {
        string ICommand.Command { get; } = "SnakeRank";

        string[] ICommand.Aliases { get; } = new[] { "SnakeR", "Srank" };

        string ICommand.Description { get; } = "查询贪吃蛇排行榜";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = Plugin.plugin;
            if (!plugin.connect.connected)
            {
                response = "未连接mysql服务器";
                return false;
            }
            var serverHigh = plugin.connect.GetTopSnackScores(10);
                response = "| 名字 | 分数 | 时间 |\n";
            foreach (var s in serverHigh) {
                if (s.time.HasValue)
                {
                    if (!string.IsNullOrEmpty(s.name))
                    {
                        response += $"{s.name} {s.highscore} {s.time.Value}\n";
                    }
                    else
                    {
                        response += $"{s.userid} {s.highscore} {s.time.Value}\n";
                    }
                }
                else
                {
                    response += $"榜单结束\n";
                    break;
                }
            }
                

                return true;
            
        }


    }
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]

    class ScpChangeCommand : ICommand, IUsageProvider
    {
        public string[] Usage { get; } = new[] { "ChangeSCP", "目标scp(仅数字 如096)" };

        string ICommand.Command { get; } = "ChangeSCP";

        string[] ICommand.Aliases { get; } = new[] { "CS" };

        string ICommand.Description { get; } = "互换scp ";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Plugin.plugin.Config.EnableChangeScp)
            {
                response = "服务器未启用替换";
                return false;
            }
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Failed to find sender";
                return false;
            }
            if(arguments.Count < 1)
            {
                response = "缺少目标scp参数!";
                return false;
            }
            List<string> list = arguments.ToList();
            var target = GetRoleFromScpNumber(list[0]);
            if (target == RoleTypeId.None)
            {
                response = "无效scp!";
                return false;
            }
            List<Player> SCPList = new List<Player>();
            foreach (var item in Player.List)
            {
                if (item.IsScp)
                {
                    SCPList.Add(item);
                }
            }
            Plugin.plugin.scpChangeReqs.Add(new ScpChangeReq()
            {
                From = player,
                to = target,
            });
            foreach (var item in SCPList)
            {
                if(item.Role.Type == target)
                {
                    item.Broadcast(new Exiled.API.Features.Broadcast()
                    {
                        Content = $"<size=29><color=yellow>{player.DisplayNickname}想要和你交换scp \n控制台输入.ScpArgee同意，不同意无需理睬",Duration = 10
                    });
                }
            }
            response = "成功 等待同意中";
            return true;

        }
        RoleTypeId GetRoleFromScpNumber(string scpNumber)
        {
            switch (scpNumber)
            {
                case "049":
                    return RoleTypeId.Scp049;
                case "096":
                    return RoleTypeId.Scp096;
                case "106":
                    return RoleTypeId.Scp106;
                case "173":
                    return RoleTypeId.Scp173;
                case "3114":
                    return RoleTypeId.Scp3114;
                case "939":
                    return RoleTypeId.Scp939;
                default:
                    return RoleTypeId.None;  // 假设 None 是一个默认无效的角色ID
            }
        }


    }
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]

    class ScpChangeArgeeCommand : ICommand, IUsageProvider
    {

        string ICommand.Command { get; } = "ScpArgee";

        string[] ICommand.Aliases { get; } = new[] { "SA" };

        string ICommand.Description { get; } = "同意互换scp";
        public string[] Usage { get; } = new[] { "ScpArgee", "源scp(可选 数字 如096)" };


        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {

            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Failed to find sender";
                return false;
            }
            var ReqFound = false;
            var WaitForChange = new List<ScpChangeReq>();
            foreach (var item in Plugin.plugin.scpChangeReqs)
            {
                if (item.to == player.Role)
                {
                    ReqFound = true;
                    WaitForChange.Add(item);
                }
            }
            if (!ReqFound)
            {
                response = "没有人找你换";
                return false;
            }
            List<string> list = arguments.ToList();

            if (arguments.Count >= 1)
            {
                var target = GetRoleFromScpNumber(list[0]);
                if (target == RoleTypeId.None)
                {
                    response = "无效scp!";
                    return false;
                }
                foreach (var item in WaitForChange)
                {
                    if (item.From.Role == target)
                    {
                        var PrePos = item.From.Position;
                        var PreHealth = item.From.Health;
                        var PreShiled = item.From.HumeShield;
                        var PreRole = item.From.Role;

                        item.From.RoleManager.ServerSetRole(item.to, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
                        item.From.Position = player.Position;
                        item.From.Health = player.Health;
                        item.From.HumeShield = player.HumeShield;

                        player.RoleManager.ServerSetRole(PreRole, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
                        player.Position = PrePos;
                        player.Health = PreHealth;
                        player.HumeShield = PreShiled;
                        Plugin.plugin.scpChangeReqs.Remove(item);
                        break;
                    }
                }
            }
            else
            {

                foreach (var item in WaitForChange)
                {


                    var PrePos = item.From.Position;
                    var PreHealth = item.From.Health;
                    var PreShiled = item.From.HumeShield;
                    var PreRole = item.From.Role;

                    item.From.RoleManager.ServerSetRole(item.to, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
                    item.From.Position = player.Position;
                    item.From.Health = player.Health;
                    item.From.HumeShield = player.HumeShield;

                    player.RoleManager.ServerSetRole(PreRole, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
                    player.Position = PrePos;
                    player.Health = PreHealth;
                    player.HumeShield = PreShiled;
                    Plugin.plugin.scpChangeReqs.Remove(item);
                    break;

                }
            }
            response = "成功";
            return true;

        }
        RoleTypeId GetRoleFromScpNumber(string scpNumber)
        {
            switch (scpNumber)
            {
                case "049":
                    return RoleTypeId.Scp049;
                case "096":
                    return RoleTypeId.Scp096;
                case "106":
                    return RoleTypeId.Scp106;
                case "173":
                    return RoleTypeId.Scp173;
                case "3114":
                    return RoleTypeId.Scp3114;
                case "939":
                    return RoleTypeId.Scp939;
                default:
                    return RoleTypeId.None;  // 假设 None 是一个默认无效的角色ID
            }
        }
    }
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class KillCommand : ICommand, IUsageProvider
    {

        string ICommand.Command { get; } = "kill";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "超级自杀（？）";
        public string[] Usage { get; } = new[] { "kill" };


        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {

            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Failed to find sender";
                return false;
            }
            var dm = new PlayerStatsSystem.CustomReasonDamageHandler("你为什么要自杀");
            player.Kill(dm);
            response = "成功";
            return true;

        }
    }
    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    class ItemSizeCommand : ICommand
    {
        string ICommand.Command { get; } = "ISize";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "修改pickup大小 Isize id, x y z";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner  = Player.Get(sender);
            if (runner.KickPower < 12) {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            if (arguments.Count < 4)
            {
                response = "To execute this command provide at least 4 arguments!";
                return false;
            }

            string[] newargs;
            List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);
            if (list == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                return false;
            }

            if (newargs == null)
            {
                response = "An error occured while processing this command.";
                return false;
            }

            var x = float.Parse(newargs[0]);
            var y = float.Parse(newargs[1]);
            var z = float.Parse(newargs[2]);
            foreach (var item in list)
            {

                var player = Player.Get(item);
                if (player.CurrentItem == null) continue;
                player.CurrentItem.Scale = new UnityEngine.Vector3(x, y, z);
                var p = player.CurrentItem.CreatePickup(player.Position, player.Rotation,false);

                player.RemoveItem(player.CurrentItem);
                p.Transform.localScale = new UnityEngine.Vector3(x, y, z);
                p.Spawn();
            }
            response = "done!";
            return true;

        }


    }
}
