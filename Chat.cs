using CommandSystem;
using Exiled.API.Features;
using HintServiceMeow.Core.Models.Hints;
using InventorySystem.Items.Firearms.Modules.Scp127;
using MEC;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

namespace Next_generationSite_27.UnionP
{
    class Chat : BaseClass
    {
        public override void Init()
        {
            a = Timing.RunCoroutine(StaticUnityMethods_OnUpdate());
            Exiled.Events.Handlers.Server.WaitingForPlayers += WaitingForPlayers;
            base.Init();
        }
        public override void Delete()
        {
            Timing.KillCoroutines(a);
            Exiled.Events.Handlers.Server.WaitingForPlayers -= WaitingForPlayers;
            base.Delete();
        }
        public static void WaitingForPlayers()
        {
            message_id = 0;
        }
        //public static Dictionary<Hint, bool> hints = new Dictionary<Hint, bool>(5) {
        //    {
        //        new Hint()
        //        {
        //            Id = "BroadcastChat_0",XCoordinate = -100,YCoordinate = 200
        //    },false},
        //    {
        //        new Hint()
        //        {
        //            Id = "BroadcastChat_1",XCoordinate = -100,YCoordinate = 250
        //    },false},
        //    {
        //        new Hint()
        //        {
        //            Id = "BroadcastChat_2",XCoordinate = -100,YCoordinate = 300
        //    },false},
        //    {
        //        new Hint()
        //        {
        //            Id = "BroadcastChat_3",XCoordinate = -100,YCoordinate = 350
        //    },false},
        //    {
        //        new Hint()
        //        {
        //            Id = "BroadcastChat_4",XCoordinate = -100,YCoordinate = 400
        //    },false},
        //};
        public CoroutineHandle a;
        public static ulong message_id = 0;
        public static IEnumerator<float> StaticUnityMethods_OnUpdate()
        {
            while (true)
            {
                int MaxQueueSize = 20;
                try
                {
                    if (ChatStrings.Count > MaxQueueSize)
                    {
                        // 丢弃最老的几条
                        while (ChatStrings.Count > MaxQueueSize)
                            ChatStrings.Dequeue();
                    }
                    if (ChatStrings.Count > 0)
                    {
                        int c = 0;
                        foreach (var item in ChatStrings)
                        {
                            if (c > 5)
                            {
                                break;
                            }
                            foreach (var item1 in Player.List)
                            {
                                message_id++;
                                item1.AddMessage($"BroadcastChat_{message_id}", $"<align=left><size=23>{item}</size></align>", duration: 3f, location: ScreenLocation.MiddleLeft);
                            }
                            c++;

                        }
                        for (int i = 0; i < c; i++)
                        {
                            ChatStrings.Dequeue();

                        }
                    }
                    if (AdminStrings.Count > 0)
                    {
                        int c = 0;
                        foreach (var item in AdminStrings)
                        {
                            if (c > 5)
                            {
                                break;
                            }
                            foreach (var item1 in Player.List.Where(x => x.RemoteAdminAccess))
                            {
                                message_id++;
                                item1.AddMessage($"BroadcastChat_{message_id}", $"<color=red><align=right><size=23>{item}</size></align></color>",duration:3f, location: ScreenLocation.MiddleRight);
                            }
                            c++;

                        }
                        for (int i = 0; i < c; i++)
                        {
                            AdminStrings.Dequeue();

                        }
                    }
                    Dictionary<Team,int> changed = new Dictionary<Team, int>();
                    bool changedFlag = false;
                    foreach (var item in TeamStrings)
                    {
                        int c = 0;
                        if (item.Value.Count > 0)
                        {
                            foreach (var item1 in item.Value)
                            {
                                if (c > 5)
                                {
                                    break;
                                }
                                foreach (var item2 in Player.List.Where(x =>
                            {
                                return x.Role.Team == item.Key || (x.Role.Team == Team.ClassD && item.Key == Team.ChaosInsurgency) || (x.Role.Team == Team.Scientists && item.Key == Team.FoundationForces);
                            }))
                                {
                                    message_id++;
                                    item2.AddMessage($"BroadcastChat_{message_id}", $"<color=yellow><align=center><size=23>{item1}</size></align></color>", duration: 3f, location: ScreenLocation.Middle);
                                }
                                c++;
                            }
                            changedFlag = true;

                            changed.Add(item.Key, c);


                        }
                    }
                    if (changedFlag)
                    {
                        foreach (var item in changed)
                        {
                            for (int i = 0; i < item.Value; i++)
                            {
                                TeamStrings[item.Key].Dequeue();

                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Info(e.ToString());
                }
                yield return Timing.WaitForSeconds(1f);


            }
        }

        public static Queue<string> ChatStrings = new Queue<string>();
        public static Queue<string> AdminStrings = new Queue<string>();
        public static Dictionary<Team, Queue<string>> TeamStrings = new Dictionary<Team, Queue<string>>() {
            {Team.Dead,new Queue<string>() },
            {Team.FoundationForces,new Queue<string>() },
            {Team.Flamingos,new Queue<string>() },
            {Team.SCPs,new Queue<string>() },
            {Team.ChaosInsurgency,new Queue<string>() },
            {Team.OtherAlive,new Queue<string>() },
        };

        [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
        public class BroadcastChat : ICommand
        {
            public string Command => "bc";

            public string[] Aliases => new string[1] { "cc" };

            public string Description => "公屏聊天";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var s = Player.Get(sender);
                if (s == null)
                {
                    response = "failed to find player";
                    return false;
                }
                if (arguments.Count == 0)
                {
                    response = "空空如也";
                    return false;
                }

                string message = string.Join(" ", arguments.ToArray());


                message = $"{s.Nickname}:{message}";
                ChatStrings.Enqueue(message);
                response = "Done!";
                return true;
            }
        }
        [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
        public class AdminChat : ICommand
        {
            public string Command => "ac";

            public string[] Aliases => new string[0] { };

            public string Description => "管理聊天";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var s = Player.Get(sender);
                if (s == null)
                {
                    response = "failed to find player";
                    return false;
                }
                if (arguments.Count == 0)
                {
                    response = "空空如也";
                    return false;
                }

                string message = string.Join(" ", arguments.ToArray());

                message = $"(反馈){s.Nickname}:{message}";
                AdminStrings.Enqueue(message);
                response = "Done!";
                return true;
            }
        }
        [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
        public class TeamChat : ICommand
        {
            public string Command => "c";

            public string[] Aliases => new string[0] { };

            public string Description => "队伍聊天";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var runner = Player.Get(sender);
                if (runner == null)
                {
                    response = "failed to find player";
                    return false;
                }
                if (arguments.Count == 0)
                {
                    response = "空空如也";
                    return false;
                }

                string message = string.Join(" ", arguments.ToArray());

                if (string.IsNullOrEmpty(message))
                {
                    response = "消息不能为空";
                    return false;
                }
                message = $"(队伍){runner.Nickname}:{message}";
                switch (runner.Role.Team)
                {
                    case Team.SCPs:
                        var s = TeamStrings[Team.SCPs];
                        s.Enqueue(message);
                        break;
                    case Team.Scientists:
                    case Team.FoundationForces:
                        var f = TeamStrings[Team.FoundationForces];
                        f.Enqueue(message);
                        break;
                    case Team.ChaosInsurgency:
                    case Team.ClassD:
                        var c = TeamStrings[Team.ChaosInsurgency];
                        c.Enqueue(message);
                        break;
                    case Team.Dead:
                        var d = TeamStrings[Team.Dead];
                        d.Enqueue(message);
                        break;
                    case Team.OtherAlive:
                        var o = TeamStrings[Team.OtherAlive];
                        o.Enqueue(message);
                        break;
                    case Team.Flamingos:
                        var fl = TeamStrings[Team.Flamingos];
                        fl.Enqueue(message);
                        break;
                }
                response = "Done!";
                return true;
            }
        }
    }
}
