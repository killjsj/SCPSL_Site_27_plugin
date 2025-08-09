using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class ra_VoteControl_start : ICommand, IUsageProvider
    {
        public static long RelativeTimeToSeconds(string time, int defaultFactor = 1)
        {
            // 允许输入负值

            if (long.TryParse(time, out var result))
            {
                return result * defaultFactor;
            }
            if (time[0] == '-' && long.TryParse(time.Substring(1), out result))
            {
                return 0;
            }
            // 检查字符串是否有效且长度大于1
            if (time.Length < 2)
            {
                throw new Exception($"{time} is not a valid time.");
            }

            // 处理负数情况


            // 对剩余部分进行转换
            if (!long.TryParse(time.Substring(0, time.Length - 1), out result))
            {
                throw new Exception($"{time} is not a valid time.");
            }

            // 根据时间单位进行转换
            switch (time[time.Length - 1])
            {
                case 'S':
                case 's':
                    return result;
                case 'm':
                    return result * 60;
                case 'M':
                    return result * 60;
                default:
                    throw new Exception($"{time} is not a valid time.");
            }
        }
        public string Command { get; } = "start_vote";

        public string[] Aliases { get; } = { "startv" };

        public string Description { get; } = "start vote(vote time Must  short than 1h)";
        public string[] Usage { get; } = new string[] { "vote time", "vote name1", "vote name2....." };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.is_voting) {
                response = "Error! is voting now!";
                return false;
            }
            List<string> arg = arguments.ToList();
            if (arg.Count <= 1)
            {
                response = "Error! arg must more  than 1";
                return false;
            }
            {
                var vote_name = string.Join(" ", arg.Skip(1)); ;
                var vote_time = RelativeTimeToSeconds(arg[0]);
                //sender
                //PlayerCommandSender
                if (!(sender is PlayerCommandSender playerCommandSender))
                {
                    response = "You must be in-game to use this command!";
                    return false;
                }
                //var refhub =                ;
                //var dis = new Display(refhub);
                Log.Info("started vote:"+vote_name+" time:"+vote_time);
                Plugin.vote_start(vote_name,vote_time);
                response = "vote started!";
                return true;
            }
        }
    }
    
    
}
