using AdminToys;
using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Static;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Discord;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.CustomStats;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
//using System.Media;
using GameObjectPools;
using InventorySystem;
using InventorySystem.Items;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
//using LabApi.Features.Wrappers;
using LightContainmentZoneDecontamination;
using MapGeneration;
using MEC;
using Microsoft.Win32;
using Mirror;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP.heavy;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using Org.BouncyCastle.Asn1.X509;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using ProjectMER.Commands.ToolGunLike;
using ProjectMER.Features;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using RelativePositioning;
using RemoteAdmin;
using Respawning;
using Respawning.NamingRules;
using Subtitles;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.DedicatedServer;
using UnityEngine.EventSystems;
using Utf8Json.Formatters;
using Utils;
using Utils.Networking;
using VoiceChat.Codec;
using VoiceChat.Networking;
using static HintServiceMeow.Core.Models.HintContent.AutoContent;
using static LightContainmentZoneDecontamination.DecontaminationController;
using static Next_generationSite_27.UnionP.heavy.Goc;
using static Next_generationSite_27.UnionP.heavy.Nu7;
using static Next_generationSite_27.UnionP.heavy.SpeedBuilditem;
using static Next_generationSite_27.UnionP.heavy.Uiu;
using static Next_generationSite_27.UnionP.RoomGraph;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;

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
    class WhipCommand : ICommand
    {
        string ICommand.Command { get; } = "whip";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "Whip playerID";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
            {
                return false;
            }
            Player Owner = null;
            if (arguments.Count < 1)
            {
                Owner = runner;
                CustomItem.Get(WhipS.WhipId).Give(Player.Get(Owner));
            }
            else
            {

                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);
                if (list == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                    return false;
                }
                if (list[0] == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.2";
                    return false;
                }
                foreach (var item in list)
                {
                    CustomItem.Get(WhipS.WhipId).Give(Player.Get(item));
                }
            }
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class MessageTest2Command : ICommand
    {
        string ICommand.Command { get; } = "MT2";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "MT2 text locX locY";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (arguments.Count < 3)
            {
                response = "To execute this command provide at least 4 arguments!" + string.Join("\n- ", Enum.GetNames(typeof(ScreenLocation)));
                return false;
            }

            List<string> newargs = arguments.ToList();
            response = $"done! op:{runner.Position}";

            var text = newargs[0];
            var locX = float.Parse(newargs[1]);
            var locY = float.Parse(newargs[2]);
            var p = LabApi.Features.Wrappers.Player.Get(runner.ReferenceHub);
            HSM_hintServ.GetPlayerHUD(p, out var hud);
            if (hud is HSM_hintServ hsm)
            {

                hsm.hud.AddHint(new HintServiceMeow.Core.Models.Hints.Hint()
                {
                    Text = text,
                    XCoordinate = locX,
                    YCoordinate = locY,


                });
            }
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
            HSM_hintServ.GetPlayerHUD(p, out var hud);

            hud.AddMessage(messid, text, time, (ScreenLocation)Enum.Parse(typeof(ScreenLocation), loc, true));
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class RoleSyncMessageTestCommand : ICommand
    {
        string ICommand.Command { get; } = "RSMT";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "RSMT Target";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            var targetRole = RoleTypeId.None;
            List<ReferenceHub> list = new List<ReferenceHub>();
            if (arguments.Count >= 2)
            {
                targetRole = (RoleTypeId)Enum.Parse(typeof(RoleTypeId), arguments.At(0), true);
                list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 1, out var _);
            }
            else
            {
                list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out var _);
                targetRole = RoleTypeId.Scp106;
            }
            if (list == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                return false;
            }
            if (list[0] == null)
            {
                response = "An unexpected problem has occurred during PlayerId/Name array processing.2";
                return false;
            }
            RoleTypeId roleTypeId = targetRole;
            ChangeAppearance(Player.Get(list[0]), roleTypeId, new List<Player>() { runner });
            response = "done";

            return true;

        }
        public static void ChangeAppearance(Player player, RoleTypeId type, IEnumerable<Player> playersToAffect, bool skipJump = false, byte unitId = 0)
        {
            if (!player.IsConnected || !RoleExtensions.TryGetRoleBase(type, out PlayerRoleBase roleBase))
                return;

            bool isRisky = PlayerRolesUtils.GetTeam(type) is Team.Dead || player.IsDead;



            NetworkWriterPooled writer = NetworkWriterPool.Get();
            if (roleBase is PlayerRoles.HumanRole HR)
            {
                UnitNamingRule unitNamingRule;

                if (NamingRulesManager.TryGetNamingRule(HR.Team, out unitNamingRule))
                {
                    writer.WriteByte(HR.UnitNameId);
                }
            }
            if (roleBase is ZombieRole)
            {
                if (!(player.Role.Base is ZombieRole))
                    isRisky = true;

                writer.WriteUShort((ushort)Mathf.Clamp(Mathf.CeilToInt(player.MaxHealth), ushort.MinValue, ushort.MaxValue));
                writer.WriteBool(true);
            }

            if (roleBase is FpcStandardRoleBase fpc)
            {
                if (!(player.Role.Base is FpcStandardRoleBase playerfpc))
                    isRisky = true;
                else
                    fpc = playerfpc;

                ushort value = 0;
                fpc?.FpcModule.MouseLook.GetSyncValues(0, out value, out ushort _);
                writer.WriteRelativePosition(player.RelativePosition);
                writer.WriteUShort(value);
            }
            foreach (Player target in playersToAffect)
            {
                if (target != player || !isRisky)
                    target.ReferenceHub.connectionToClient.Send<RoleSyncInfo>(new RoleSyncInfo(player.ReferenceHub, type, target.ReferenceHub, writer), 0);
                else
                    Log.Error($"Prevent Seld-Desync of {player.Nickname} with {type}");
            }

            NetworkWriterPool.Return(writer);

            // To counter a bug that makes the player invisible until they move after changing their appearance, we will teleport them upwards slightly to force a new position update for all clients.
            if (!skipJump)
                player.Position += Vector3.up * 0.25f;
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
            response = $"done! op:{runner.Position.ToString()} {runner.CurrentRoom} {runner.CurrentRoom.RoomName} {runner.CurrentRoom.Identifier.Shape} \nNearestRooms:";
            foreach (var item in runner.CurrentRoom.NearestRooms)
            {
                response += $"  {item} \n";
            }
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
            Timing.CallDelayed(0.2f, () =>
            {
                try
                {
                    var rol = p.Role as Exiled.API.Features.Roles.Scp079Role;

                    foreach (var item in Player.Enumerable.Where((x) => !x.IsScp))
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
    new Type[] { },
    null
);
            if (serverSendRpcMethod != null)
            {
                // 构造委托参数
                Func<ReferenceHub, bool> condition = x => ServerCheckReceiver(x, ((RelativePosition)syncPosField.GetValue(PingAbility)).Position, (int)pingType);
                serverSendRpcMethod.Invoke(PingAbility, new object[] { });
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
    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //class StartRoundTest1Command : ICommand
    //{
    //    string ICommand.Command { get; } = "SRT";

    //    string[] ICommand.Aliases { get; } = new[] { "" };

    //    string ICommand.Description { get; } = "!!! Debug Command";

    //    bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        var runner = Player.Get(sender);
    //        if (runner != null)
    //        {
    //            if (runner.KickPower < 12)
    //            {
    //                response = "你没权 （player.KickPower < 12）";
    //                return false;
    //            }
    //        }
    //        foreach (var item in ReferenceHub.AllHubs)
    //        {
    //            //item.characterClassManager.RpcRoundStarted();

    //        }
    //        RoundStart.singleton.NetworkTimer = -1;
    //        response = $"done!";
    //        return true;

    //    }
    //}
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class Scp5kstartCommand : ICommand
    {
        string ICommand.Command { get; } = "5k";

        string[] ICommand.Aliases { get; } = new[] { "Scp5000" };

        string ICommand.Description { get; } = "!!! 使用后将立刻重启服务器并启动5k 由于进行测试(有bug) 谨慎使用";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner != null)
            {
                if (runner.KickPower < 12)
                {
                    response = "你没权 （player.KickPower < 12）";
                    return false;
                }
            }
            Scp5k_Control.IsForce5kRound = true;
            Round.Restart();
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class EffectTestCommand : ICommand
    {
        string ICommand.Command { get; } = "EfT";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 使用后将给你添加一个测试buff 由于进行测试(有bug) 谨慎使用 EfT playerId(可选)";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            Player Owner = null;
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            if (arguments.Count < 1)
            {
                Owner = runner;
            }
            else
            {

                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);
                if (list == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                    return false;
                }
                if (list[0] == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.2";
                    return false;
                }
                Owner = Player.Get(list[0]);
            }
            var a = EffectHelper.AddStatusEffectRuntime<Next_generationSite_27.UnionP.TestEffect>(Owner.ReferenceHub.playerEffectsController);
            Owner.EnableEffect(a, 30f);
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class Scp079RedButtonCommand : ICommand
    {
        string ICommand.Command { get; } = "S079T";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 由于进行测试(有bug) 谨慎使用";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            //Player Owner = null;
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            Scp079NotificationManager.AddNotification(new Scp079AccentedNotification("Test,Never gonna giv you up", "#00a2ff", '$'));
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class CassieTestCommand : ICommand
    {
        string ICommand.Command { get; } = "CAT";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 由于进行测试";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var p = Player.Get(sender);
            Scp5k_Control.StartSubtitles();
            response = $"done!";
            return true;

        }
        
    }
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class ReplyCommand : ICommand
    {
        string ICommand.Command { get; } = "reply";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "just a test";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Timing.RunCoroutine(test1(sender));
            response = $"test 1";
            return true;

        }
        IEnumerator<float> test1(ICommandSender sender)
        {
            if (sender is CommandSender CS)
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        CS.RaReply($"RaReply test3 {i}", true, true, "test3_overrideDisplay");
                        CS.RaReply($"RaReply test3 {i} Null", true, true, "");
                        // 替换此行：
                        // CS.RaReply("\b"*18+$"RaReply#test3.1 {i} Null", true, true, "");

                        // 修正为如下（使用 new string('\b', 18) 生成 18 个退格符）:
                        CS.RaReply(new string('\b', 18) + $"RaReply#test3.1 {i} Null", true, true, "");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("test3:");
                        Log.Error(ex.ToString());
                    }
                    try
                    {
                        CS.Print($"Print test4 {i}");
                        CS.Print($"Print Color test4 {i}", ConsoleColor.Green);
                        CS.Print($"Print Color RgbColor test4 {i}", ConsoleColor.Green, Color.cyan);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("test4:");
                        Log.Error(ex.ToString());
                    }
                    try
                    {
                        CS.Respond($"Respond test5 {CS.Available()}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("test5:");
                        Exiled.API.Features.Log.Error(ex.ToString());
                    }
                    yield return Timing.WaitForSeconds(0.5f);

                }
            }
        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class Reply1Command : ICommand
    {
        string ICommand.Command { get; } = "repl1y";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "just a test";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Timing.RunCoroutine(test1(sender));
            response = $"test 1";
            return true;

        }
        IEnumerator<float> test1(ICommandSender sender)
        {
            if (sender is CommandSender CS)
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        CS.RaReply(new string('\f', 1) + $"RaReply#test3.1 {i} Null", true, true, "");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("test3:");
                        Log.Error(ex.ToString());
                    }
                    yield return Timing.WaitForSeconds(0.5f);

                }
            }
        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class EndRoundCommand : ICommand
    {
        string ICommand.Command { get; } = "EndRound";

        string[] ICommand.Aliases { get; } = new[] { "ENR" };

        string ICommand.Description { get; } = "!!! 使用后将立刻结束回合";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            Round.EndRound(true);
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class BotZombieCommand : ICommand
    {
        string ICommand.Command { get; } = "BotZombie";

        string[] ICommand.Aliases { get; } = new[] { "BZZ" };

        string ICommand.Description { get; } = "!!! 使用后产生一个机器小僵尸 bzz [PlayerId(主人 可选)]";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            Player Owner = null;
            if (arguments.Count < 1)
            {
                Owner = runner;
            }
            else
            {

                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);
                if (list == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                    return false;
                }
                if (list[0] == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.2";
                    return false;
                }
                Owner = Player.Get(list[0]);
            }
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            BetterZombie.Create(Owner);
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class CreateCommand : ICommand
    {
        string ICommand.Command { get; } = "createAwindow";

        string[] ICommand.Aliases { get; } = new[] { "CAW" };

        string ICommand.Description { get; } = "!!! 使用后产生方块";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            var p = Primitive.Create(PrimitiveType.Cube, runner.Position, null, null, true);
            p.Collidable = true;
            p.Visible = true;
            var BW = p.GameObject.AddComponent<bunker>();
            BW.Health = 100;
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class ChangeAppearceCommand : ICommand
    {
        string ICommand.Command { get; } = "CAP";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 修改外貌 CAP RoleTypeID [PlayerId(可选)]";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var target = Player.Get(sender);
            RoleTypeId targetRole = RoleTypeId.None;
            if (target.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            if (arguments.Count < 2)
            {
                targetRole = (RoleTypeId)Enum.Parse(typeof(RoleTypeId), arguments.At(0), true);
            }
            else
            {

                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 1, out newargs);
                if (list == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.";
                    return false;
                }
                if (list[0] == null)
                {
                    response = "An unexpected problem has occurred during PlayerId/Name array processing.2";
                    return false;
                }
                targetRole = (RoleTypeId)Enum.Parse(typeof(RoleTypeId), arguments.At(0), true);
                target = Player.Get(list[0]);
            }
            target.ChangeAppearance(targetRole);
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class FinderRoomCommand : ICommand
    {
        string ICommand.Command { get; } = "pathroom";
        string[] ICommand.Aliases { get; } = new[] { "" };
        string ICommand.Description { get; } = "!!! 使用后产生很多教程角色寻路";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            response = "";
            if (arguments.Count < 1)
            {
                foreach (var arg in Enum.GetNames(typeof(RoomType)))
                {
                    response += arg + " \n";
                }
                return false;
            }

            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }

            if (!Enum.TryParse<RoomType>(arguments.First(), true, out var r))
            {
                response = "Failed to parse!";
                return false;
            }

            if (runner.CurrentRoom == null)
            {
                response = "Player has no current room.";
                return false;
            }

            var targetRoomId = RoomIdentifier.AllRoomIdentifiers.FirstOrDefault(x => Room.Get(x).Type == r);
            if (targetRoomId == null)
            {
                response = "Target room not found.";
                return false;
            }
            var nav = RoomGraph.InternalNav;
            var re = nav.GetPathRooms(runner.CurrentRoom, Room.Get(targetRoomId));

            // 修复：检查路径是否存在
            if (re == null || re.Count == 0)
            {
                response = "No path found.";
                return false;
            }

            foreach (var item in re)
            {
                Npc npc = new Npc(DummyUtils.SpawnDummy("name"));
                Timing.CallDelayed(0.5f, delegate
                {
                    npc.Role.Set(RoleTypeId.Tutorial);
                    npc.Position = item.Position + Vector3.up * 2f;
                    npc.Health = npc.MaxHealth;
                });
                Player.Dictionary.Add(npc.GameObject, npc);
                response += $"   spawned at {item.Position}\n";
            }
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class FinderPosCommand : ICommand
    {
        string ICommand.Command { get; } = "pathPos";
        string[] ICommand.Aliases { get; } = new[] { "" };
        string ICommand.Description { get; } = "!!! 使用后产生很多教程角色寻路";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            response = "";
            if (arguments.Count < 3)
            {
                response = "No pos";
                return false;
            }

            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }

            var x = float.Parse(arguments.ElementAt(0));
            var y = float.Parse(arguments.ElementAt(1));
            var z = float.Parse(arguments.ElementAt(2));
            //var pathfinding = Pathfinding.nav;

            //// 获取路径点
            //var re = pathfinding.GetPathPoints(
            //    target.Position, new Vector3(x, y, z)
            //);
            var nav = RoomGraph.InternalNav;
            var re = nav.FindPath(runner.Position, runner.CurrentRoom, new Vector3(x, y, z), Room.Get(new Vector3(x, y, z)));
            // 修复：检查路径是否存在
            if (re == null || re.Count == 0)
            {
                response = $"No path found.Runner:{runner.CurrentRoom}";

                return false;
            }
            int i = 0;
            foreach (var item in re)
            {
                Npc npc = new Npc(DummyUtils.SpawnDummy($"{i}"));
                Timing.CallDelayed(0.5f, delegate
                {
                    npc.Role.Set(RoleTypeId.Tutorial);
                    npc.Position = item + Vector3.up * 2f;
                    npc.Health = npc.MaxHealth;
                });
                Player.Dictionary.Add(npc.GameObject, npc);
                response += $"  {i} spawned at {item}\n";
                i++;
            }
            return true;
        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class PlaceGocBombEffectCommand : ICommand
    {
        string ICommand.Command { get; } = "PGOCBE";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 使用后将生成GOC奇数核弹特性（原地） 由于进行测试(有bug) 谨慎使用";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }

            Scp5k.GOCAnim.Gen(new Vector3(13f, 450f, -40f));
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class PlayGocBombEffectIdleCommand : ICommand
    {
        string ICommand.Command { get; } = "PGOCBEI";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 使用后将使现在的GOC奇数核弹播放动画 由于进行测试(有bug) 谨慎使用";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            Scp5k.GOCAnim.PlayIdle();
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class PlayGocBombEffectDonateCommand : ICommand
    {
        string ICommand.Command { get; } = "PGOCBED";

        string[] ICommand.Aliases { get; } = new[] { "" };

        string ICommand.Description { get; } = "!!! 使用后将使现在的GOC奇数核弹播放动画 由于进行测试(有bug) 谨慎使用";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Player.Get(sender);
            if (runner.KickPower < 12)
            {
                response = "你没权 （player.KickPower < 12）";
                return false;
            }
            Scp5k.GOCAnim.PlayDonate();
            response = $"done!";
            return true;

        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class Scp5kroleCommand : ICommand
    {
        string ICommand.Command { get; } = "5kRole";

        string[] ICommand.Aliases { get; } = new[] { "Scp5000Role" };

        string ICommand.Description { get; } = "5kRole PlayerID GOC/UIU/BOT/Doc/GOCSPY/Changer/NukeGOC/NU7";

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
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(Goc610PID, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
                case "NUKEGOC":
                    {
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(GocNukePID, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
                case "GOCSPY":
                    {
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(GocSpyID, out var Prole))
                            {
                                Player player = Player.Get(item);
                                Prole.AddRole(player);
                            }
                        }
                        break;
                    }
                case "CHANGER":
                    {
                        foreach (var item in list)
                        {

                            Player player = Player.Get(item);
                            Scp5k.Scp5k_Control.ColorChangerRole.instance.AddRole(player);

                        }
                        break;
                    }
                case "NU7":
                    {
                        foreach (var item in list)
                        {

                            Player player = Player.Get(item);
                            scp5k_Nu7_P.instance.AddRole(player);

                        }
                        break;
                    }
                case "UIU":
                    {
                        foreach (var item in list)
                        {
                            if (CustomRole.TryGet(UiuPID, out var Prole))
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
                            if (CustomRole.TryGet(bot.botID, out var Prole))
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
