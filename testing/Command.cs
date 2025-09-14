using AdminToys;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using Exiled.CustomRoles.API.Features;
using GameObjectPools;
using InventorySystem;
using InventorySystem.Items;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp079Events;
using MEC;
using Microsoft.Win32;
using Mirror;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.Enums;
using Next_generationSite_27.UnionP.Scp5k;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    class MessageTestCommand : ICommand
    {
        string ICommand.Command { get; } = "MT";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "MT messageid text time loc";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (arguments.Count < 4)
            {
                response = "To execute this command provide at least 4 arguments!" + string.Join("\n- ", Enum.GetNames(typeof(ScreenLocation)));
                return false;
            }

            List<string> newargs = arguments.ToList();
            response = $"done! op:{runner.Position}";

            var messid = newargs[0];
            var text = newargs[1];
            var time = float.Parse(newargs[2]);
            var loc = newargs[3];
            var p = LabApi.Features.Wrappers.Player.Get(runner.ReferenceHub);
            Next_generationSite_27.Features.PlayerHuds.PlayerHud.TryGet(p, out var hud);
            
            hud.AddMessage(new Next_generationSite_27.Features.PlayerHuds.Messages.TextMessage(messid, text, time, (ScreenLocation)Enum.Parse(typeof(ScreenLocation), loc)));
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
            response = $"done! op:{runner.Position.ToString()} {runner.CurrentRoom} {runner.CurrentRoom.RoomName} {runner.CurrentRoom.Identifier.Shape}";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class FakePingCommand : ICommand
    {
        string ICommand.Command { get; } = "FP";

        string[] ICommand.Aliases { get; } = new[] { "FakePing" };

        string ICommand.Description { get; } = "A fake ping";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            var r = DummyUtils.SpawnDummy("temp 079");
            var p = Player.Get(r);
            r.roleManager.ServerSetRole(RoleTypeId.Scp079, RoleChangeReason.RemoteAdmin);
            Timing.CallDelayed(0.4f, () =>
            {
                try
                {
                    var rol = p.Role as Exiled.API.Features.Roles.Scp079Role;

                    foreach (var item in Player.List.Where((x) => !x.IsScp))
                    {
                        //PingAbility._syncPos = new RelativePosition(item.Position);
                        //PingAbility._syncNormal = item.Position;
                        //PingAbility._syncProcessorIndex = (byte)PingType.Human;

                        //PingAbility.ServerSendRpc(x => ServerCheckReceiver(x, PingAbility._syncPos.Position, (int)PingType.Human));


                        //PingAbility._rateLimiter.RegisterInput();
                        if (item.Items.Count((x) => x.Type == ItemType.MicroHID) >= 1)
                        {
                            Ping(rol, item.Position, PingType.MicroHid, false);

                        }
                        else
                        {
                            Ping(rol, item.Position, PingType.Human, false);
                        }
                        //rol.Ping(item.Position, PingType.Human, false);
                    }

                    NetworkServer.Destroy(r.gameObject);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            response = $"done! op:{runner.Position.ToString()}";
            return true;

        }
        public void Ping(Exiled.API.Features.Roles.Scp079Role role, Vector3 position, PingType pingType = PingType.Default, bool consumeEnergy = true)
        {
            if (!role.SubroutineModule.TryGetSubroutine<Scp079PingAbility>(out var PingAbility))
            {
                Log.Error("Scp079PingAbility subroutine not found in Scp079Role::ctor");
                return;
            }

            // 使用反射获取私有字段信息
            var type = typeof(Scp079PingAbility);

            // 获取三个私有字段的 FieldInfo
            FieldInfo syncProcessorIndexField = type.GetField("_syncProcessorIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo syncPosField = type.GetField("_syncPos", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo syncNormalField = type.GetField("_syncNormal", BindingFlags.NonPublic | BindingFlags.Instance);

            // 检查字段是否存在（防止游戏更新后字段名变化）
            if (syncProcessorIndexField == null || syncPosField == null || syncNormalField == null)
            {
                Log.Error("Failed to get one or more private fields from Scp079PingAbility. Field names may have changed.");
                return;
            }

            // ✅ 使用 SetValue 强制修改私有字段
            syncProcessorIndexField.SetValue(PingAbility, (byte)pingType);
            syncPosField.SetValue(PingAbility, new RelativePosition(position));
            syncNormalField.SetValue(PingAbility, position); // 或 Vector3.up 等方向

            MethodInfo serverSendRpcMethod = typeof(Scp079PingAbility).GetMethod(
    "ServerSendRpc",
    BindingFlags.NonPublic | BindingFlags.Instance,
    null,
    new Type[] { typeof(Func<ReferenceHub, bool>) },
    null
);
            if (serverSendRpcMethod != null)
            {
                // 构造委托参数
                Func<ReferenceHub, bool> condition = x => ServerCheckReceiver(x, ((RelativePosition)syncPosField.GetValue(PingAbility)).Position, (int)pingType);
                serverSendRpcMethod.Invoke(PingAbility, new object[] { condition });
            }
            else
            {
                Log.Error("Failed to find ServerSendRpc method via reflection.");
            }

            //PingAbility._rateLimiter.RegisterInput();
        }
        public bool ServerCheckReceiver(ReferenceHub hub, Vector3 point, int processorIndex)
        {
            PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
            if (!(currentRole is FpcStandardScp fpcStandardScp))
            {

                return true;
            }

            float range = PingProcessors[processorIndex].Range;
            float num = range * range;
            return (fpcStandardScp.FpcModule.Position - point).sqrMagnitude < num;
        }
        public static readonly IPingProcessor[] PingProcessors = new IPingProcessor[7]
{
        new GeneratorPingProcessor(),
        new ProjectilePingProcessor(),
        new MicroHidPingProcesssor(),
        new HumanPingProcessor(),
        new ElevatorPingProcessor(),
        new DoorPingProcessor(),
        new DefaultPingProcessor()
};
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class Scp5kstartCommand : ICommand
    {
        string ICommand.Command { get; } = "5k";

        string[] ICommand.Aliases { get; } = new[] { "Scp5000" };

        string ICommand.Description { get; } = "!!! 使用后将立刻重启服务器并启动5k 由于进行测试(有bug) 谨慎使用";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            Scp5k_Control.Is5kRound = true;
            Round.Restart();
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class Scp5kroleCommand : ICommand
    {
        string ICommand.Command { get; } = "5kRole";

        string[] ICommand.Aliases { get; } = new[] { "Scp5000Role" };

        string ICommand.Description { get; } = "5kRole PlayerID GOC/UIU/BOT/Doc";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            if (arguments.Count < 2)
            {
                response = "To execute this command provide at least 2 arguments!";
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
                response = "An error occured while processing this command.(0)";
                return false;
            }
            if (newargs.Length == 0)
            {
                response = "An error occured while processing this command.(1)";
                return false;
            }
            switch (newargs[0].ToUpper())
            {
                case "GOC":
                    {
                        foreach (var item in list) {
                            if (CustomRole.TryGet(30, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
                case "UIU":
                    {
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(32, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
                case "BOT":
                    {
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(Scp5k_Control.botID, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
                case "DOC":
                    {
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(Scp5k.Scp5k_Control.SciID, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
            }
            response = $"done!";
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
