using CommandSystem;
using Exiled.API.Features;
using HintServiceMeow.Core.Extension;
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
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.SpeedBuilditem;
using static Subtitles.SubtitleCategory;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

namespace Next_generationSite_27.UnionP
{
    class Chat : BaseClass
    {
        public override void Init()
        {
            a = Timing.RunCoroutine(StaticUnityMethods_OnUpdate());
            Exiled.Events.Handlers.Server.WaitingForPlayers += WaitingForPlayers;
            //base.Init();
            Exiled.Events.Handlers.Player.Verified += Verified;
        }
        public override void Delete()
        {
            Timing.KillCoroutines(a);
            Exiled.Events.Handlers.Server.WaitingForPlayers -= WaitingForPlayers;
            Exiled.Events.Handlers.Player.Verified -= Verified;
            //base.Delete();
        }
        public static void Verified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            var player = ev.Player;
            player.AddHint(Chat.ChatHint);
            player.AddHint(Chat.AdminHint);
            player.AddHint(Chat.GroupHint);

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
        public static Hint ChatHint = new Hint()
        {
            Id = "Chat_Hint",
            XCoordinate = -100,
            YCoordinate = 200,
            FontSize = 23,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Left,
            AutoText = new HintServiceMeow.Core.Models.HintContent.AutoContent.TextUpdateHandler((_) =>
            {
                return ChatStrings;
            }),
        };
        public static Hint AdminHint = new Hint()
        {
            Id = "Admin_Hint",
            XCoordinate = 100,
            YCoordinate = 200,
            FontSize = 23,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Right,
            AutoText = new HintServiceMeow.Core.Models.HintContent.AutoContent.TextUpdateHandler((x) =>
            {
                if (x.PlayerDisplay.ReferenceHub.serverRoles.RemoteAdmin)
                    return AdminStrings;
                return "";
            }),
        };
        public static Hint GroupHint = new Hint()
        {
            Id = "Group_hint",
            XCoordinate = 0,
            YCoordinate = 200,
            FontSize = 23,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Center,
            AutoText = new HintServiceMeow.Core.Models.HintContent.AutoContent.TextUpdateHandler((x) =>
            {
                var mess = "";
                switch (x.PlayerDisplay.ReferenceHub.roleManager.CurrentRole.Team)
                {
                    case Team.SCPs:
                        mess = TeamStrings[Team.SCPs];
                        break;
                    case Team.Scientists:
                    case Team.FoundationForces:
                        mess = TeamStrings[Team.FoundationForces];

                        break;
                    case Team.ChaosInsurgency:
                    case Team.ClassD:
                        mess = TeamStrings[Team.ChaosInsurgency];

                        break;
                    case Team.Dead:
                        mess = TeamStrings[Team.Dead];

                        break;
                    case Team.OtherAlive:
                        mess = TeamStrings[Team.OtherAlive];

                        break;
                    case Team.Flamingos:
                        mess = TeamStrings[Team.Flamingos];

                        break;
                }
                return mess;
            }),
        };
        public CoroutineHandle a;
        public static ulong message_id = 0;
        public static IEnumerator<float> StaticUnityMethods_OnUpdate()
        {
            while (true)
            {
                int MaxQueueSize = 6;
                try
                {
                    //if (ChatStrings.Count > MaxQueueSize)
                    //{
                    //    // 丢弃最老的几条
                    //    while (ChatStrings.Count > MaxQueueSize)
                    //        ChatStrings.Dequeue();
                    //}
                    //if (ChatStrings.Count > 0)
                    //{
                    //    int c = 0;
                    //    foreach (var item in ChatStrings)
                    //    {
                    //        if (c > 5)
                    //        {
                    //            break;
                    //        }
                    //        foreach (var item1 in Player.Enumerable)
                    //        {
                    //            message_id++;
                    //            item1.AddMessage($"BroadcastChat_{message_id}", $"<align=left><size=23>{item}</size></align>", duration: 3f, location: ScreenLocation.MiddleLeft);
                    //            item1.SendConsoleMessage(item,"");
                    //        }
                    //        c++;

                    //    }
                    //    for (int i = 0; i < c; i++)
                    //    {
                    //        ChatStrings.Dequeue();

                    //    }
                    //}
                    if (ChatList.Count > 6)
                    {
                        while (ChatList.Count > MaxQueueSize)
                            ChatList.RemoveAt(0);
                    }
                    if (ChatList.Count > 0)
                    {
                        ChatStrings = "<align=left><size=23>";
                        int c = 0;
                        for (int i = 0; i < ChatList.Count && c <= 6; i++, c++)
                        {
                            var chatMsg = ChatList[i];
                            if (chatMsg.exp <= 0f) // new
                            {
                                chatMsg.exp = Time.time + 4f;
                                ChatList[i] = chatMsg;
                            }
                            if (chatMsg.exp <= Time.time)
                            {
                                ChatList.RemoveAt(i);
                                i--;
                                continue;
                            }
                            ChatStrings += chatMsg.text + "\n";
                        }
                        ChatStrings += "</size></align>";
                    }
                    if (AdminList.Count > 0)
                    {
                        //int c = 0;
                        //foreach (var item in AdminStrings)
                        //{
                        //    if (c > 5)
                        //    {
                        //        break;
                        //    }
                        //    foreach (var item1 in Player.Enumerable.Where(x => x.RemoteAdminAccess))
                        //    {
                        //        message_id++;
                        //        item1.AddMessage($"BroadcastChat_{message_id}", $"<color=red><align=right><size=23>{item}</size></align></color>",duration:3f, location: ScreenLocation.MiddleRight);
                        //        item1.SendConsoleMessage(item,"");
                        //    }
                        //    c++;

                        //}
                        //for (int i = 0; i < c; i++)
                        //{
                        //    AdminStrings.Dequeue();

                        //}
                        AdminStrings = "<align=right><size=23>";

                        int c = 0;
                        for (int i = 0; i < AdminList.Count && c <= 6; i++, c++)
                        {
                            var chatMsg = AdminList[i];
                            if (chatMsg.exp <= 0f) // new
                            {
                                chatMsg.exp = Time.time + 7f;
                                AdminList[i] = chatMsg;
                            }
                            if (chatMsg.exp <= Time.time)
                            {
                                AdminList.RemoveAt(i);
                                i--;
                                continue;
                            }
                            AdminStrings += "<color=red>" + chatMsg.text + "</color>\n";
                        }
                        AdminStrings += "</size></align>";

                    }
                    foreach (var item in TeamList)
                    {
                        if (item.Value.Count > 0)
                        {
                            TeamStrings[item.Key] = "<align=center><size=23>";
                            int c = 0;
                            for (int i = 0; i < item.Value.Count && c <= 6; i++, c++)
                            {
                                var chatMsg = item.Value[i];
                                if (chatMsg.exp <= 0f) // new
                                {
                                    chatMsg.exp = Time.time + 4f;
                                    item.Value[i] = chatMsg;
                                }
                                if (chatMsg.exp <= Time.time)
                                {
                                    item.Value.RemoveAt(i);
                                    i--;
                                    continue;
                                }
                                TeamStrings[item.Key] += "<color=yellow>" + chatMsg.text + "</color>\n";
                            }
                            TeamStrings[item.Key] += "</size></align>";
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Info(e.ToString());
                }
                yield return Timing.WaitForSeconds(0.2f);


            }
        }

        public static string ChatStrings = "";
        public static string AdminStrings = "";
        public static Dictionary<Team, string> TeamStrings = new Dictionary<Team, string>() {
            {Team.Dead,"" },
            {Team.FoundationForces,"" },
            {Team.Flamingos,"" },
            {Team.SCPs,"" },
            {Team.ChaosInsurgency,"" },
            {Team.OtherAlive,"" },
        };
        public struct ChatMessage
        {
            public string text;
            public float exp = 0;
            public ChatMessage(string text)
            {
                this.text = text;
            }

        }
        public static List<ChatMessage> ChatList = new();
        public static List<ChatMessage> AdminList = new();
        public static Dictionary<Team, List<ChatMessage>> TeamList = new Dictionary<Team, List<ChatMessage>>() {
            {Team.Dead,new() },
            {Team.FoundationForces,new() },
            {Team.Flamingos,new() },
            {Team.SCPs,new() },
            {Team.ChaosInsurgency,new() },
            {Team.OtherAlive,new() },
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


                message = $"{s.Nickname}💭:{message}";
                ChatList.Add(new ChatMessage(message));
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

                message = $"(反馈){s.Nickname}💭:{message}";
                AdminList.Add(new ChatMessage(message));
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
                message = $"(队伍){runner.Nickname}💭:{message}";
                switch (runner.Role.Team)
                {
                    case Team.SCPs:
                        var s = TeamList[Team.SCPs];
                        s.Add(new ChatMessage(message));
                        break;
                    case Team.Scientists:
                    case Team.FoundationForces:
                        var f = TeamList[Team.FoundationForces];
                        f.Add(new ChatMessage(message));
                        break;
                    case Team.ChaosInsurgency:
                    case Team.ClassD:
                        var c = TeamList[Team.ChaosInsurgency];
                        c.Add(new ChatMessage(message));
                        break;
                    case Team.Dead:
                        var d = TeamList[Team.Dead];
                        d.Add(new ChatMessage(message));
                        break;
                    case Team.OtherAlive:
                        var o = TeamList[Team.OtherAlive];
                        o.Add(new ChatMessage(message));
                        break;
                    case Team.Flamingos:
                        var fl = TeamList[Team.Flamingos];
                        fl.Add(new ChatMessage(message));
                        break;
                }
                response = "Done!";
                return true;
            }
        }
    }
}
