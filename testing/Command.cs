using AdminToys;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using GameObjectPools;
using HintServiceMeow.UI.Utilities;
using InventorySystem;
using InventorySystem.Items;
using LabApi.Events.Arguments.PlayerEvents;
using Microsoft.Win32;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utf8Json.Formatters;
using Utils;

namespace Next_generationSite_27.UnionP 
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class TPCommand : ICommand
    {
        string ICommand.Command { get; } = "TP";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "tp x y z";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (!sender.CheckPermission(PlayerPermissions.Noclip, out response))
            {
                return false;
            }
            if (arguments.Count < 3)
            {
                response = "To execute this command provide at least 3 arguments!";
                return false;
            }

            List<string> newargs = arguments.ToList();
            response = $"done! op:{runner.Position}";

            var x = float.Parse(newargs[0]);
            var y = float.Parse(newargs[1]);
            var z = float.Parse(newargs[2]);
            runner.Position = new Vector3(x, y, z);
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class POSCommand : ICommand
    {
        string ICommand.Command { get; } = "GPs";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "gps";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            response = $"done! op:{runner.Position.ToString()}";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class ROOMCommand : ICommand
    {
        string ICommand.Command { get; } = "getroom";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "获取房间位置 getroom";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (!sender.CheckPermission(PlayerPermissions.Noclip, out response))
            {
                return false;
            }
            var r = Room.Get(runner.Position);
            response = "done!" + $"{r.Position} {r.RoomName} ";
            return true;

        }
    }
}
