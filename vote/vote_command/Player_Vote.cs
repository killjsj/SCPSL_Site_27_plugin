using CommandSystem;
using Exiled.API.Features;
using Next_generationSite_27.UnionP;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP { 
    [CommandHandler(typeof(ClientCommandHandler))]
    class vote_yes : ICommand
    {
        public string Command { get; } = "yes";

        public string[] Aliases { get; } = { "y" ,"vyes"};

        public string Description { get; } = "agree vote";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Plugin.is_voting)
            {
                response = "Error!no vote now!";
                return false;
            }
            Player player = Player.Get(sender);
            foreach (Player p in Plugin.vote_control[0]) {
                if (p == player) {
                    response = "Error!"+sender.LogName + " is already vote yes";
                    return false;
                }
            }
            Plugin.vote_control[0].Add(player);
            Plugin.vote_control[1].Remove(player);
            response = "voted!";
            return true;
        }
    }
    [CommandHandler(typeof(ClientCommandHandler))]
    class vote_no : ICommand
    {
        public string Command { get; } = "no";

        public string[] Aliases { get; } = { "n" ,"vno"};

        public string Description { get; } = "disagree vote";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Plugin.is_voting)
            {
                response = "Error!no vote now!";
                return false;
            }
            Player player = Player.Get(sender);
            foreach (Player p in Plugin.vote_control[0])
            {
                if (p == player)
                {
                    response = "Error!" + sender.LogName + " is already vote yes";
                    return false;
                }
            }
            Plugin.vote_control[1].Add(player);
            Plugin.vote_control[0].Remove(player);
            response = "voted!";
            return true;
        }
    }


}
