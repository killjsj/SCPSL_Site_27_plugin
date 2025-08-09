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
using LabApi.Events.Arguments.PlayerEvents;
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

//namespace Next_generationSite_27.UnionP
//{
//    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
//    class CubeImageTestingCommand : ICommand
//    {
//        string ICommand.Command { get; } = "PTEST";

//        string[] ICommand.Aliases { get; } = new[] { "" };

//        string ICommand.Description { get; } = "test";

//        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
//        {
//            Player p = sender as Player;
//            if (Scp956.Singleton == null)
//            {
//               var sp =  new Scp956Pinata();
//                Scp956Pinata.Init();
//                sp.SpawnBehindTarget(p.ReferenceHub);
//            }
//            else
//            {
//                Scp956.SpawnBehindTarget(p);
//            }
//            response = "done!";
//            return true;
//        }

//    }
//}
