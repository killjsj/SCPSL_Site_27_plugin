using Achievements;
using AdminToys;
using AudioManagerAPI.Defaults;
using AutoEvent.Interfaces;
using CommandSystem;
using CustomRendering;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.API.Features.Waves;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.Commands.Reload;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;
using Exiled.Loader;
using InventorySystem.Items.Firearms;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Features.Audio;
using LabApi.Features.Wrappers;
using LiteNetLib;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.UI;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.DedicatedServer;
using UnityEngine.EventSystems;
using UserSettings.ServerSpecific;
using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
using static Next_generationSite_27.UnionP.heavy.Goc;
using static TMPro.TMP_InputField;
using static UnityEngine.UI.CanvasScaler;
using Player = Exiled.API.Features.Player;
using Room = Exiled.API.Features.Room;

namespace Next_generationSite_27.UnionP.Scp5k
{
    public static class MathProblemGenerator
    {
        private static readonly System.Random rand = new System.Random();

        // 生成一个随机小学混合运算题（3~4个数字，加减乘，结果0~300，无小数，允许括号）
        public static (string question, string answer) GenerateProblem()
        {
            const int MAX_RETRY = 20; // 最多重试20次
            for (int retry = 0; retry < MAX_RETRY; retry++)
            {
                int numCount = rand.Next(3, 5);
                List<int> numbers = new List<int>();
                List<char> ops = new List<char>();

                for (int i = 0; i < numCount; i++)
                {
                    numbers.Add(rand.Next(1, 101));
                }

                char[] availableOps = { '+', '-', '*' };
                for (int i = 0; i < numCount - 1; i++)
                {
                    ops.Add(availableOps[rand.Next(availableOps.Length)]);
                }

                string expr;
                if (numCount == 3)
                {
                    int bracketOption = rand.Next(0, 3);
                    switch (bracketOption)
                    {
                        case 1:
                            expr = $"({numbers[0]}{ops[0]}{numbers[1]}){ops[1]}{numbers[2]}";
                            break;
                        case 2:
                            expr = $"{numbers[0]}{ops[0]}({numbers[1]}{ops[1]}{numbers[2]})";
                            break;
                        default:
                            expr = $"{numbers[0]}{ops[0]}{numbers[1]}{ops[1]}{numbers[2]}";
                            break;
                    }
                }
                else
                {
                    int bracketOption = rand.Next(0, 5);
                    switch (bracketOption)
                    {
                        case 1:
                            expr = $"({numbers[0]}{ops[0]}{numbers[1]}){ops[1]}{numbers[2]}{ops[2]}{numbers[3]}";
                            break;
                        case 2:
                            expr = $"{numbers[0]}{ops[0]}({numbers[1]}{ops[1]}{numbers[2]}){ops[2]}{numbers[3]}";
                            break;
                        case 3:
                            expr = $"{numbers[0]}{ops[0]}{numbers[1]}{ops[1]}({numbers[2]}{ops[2]}{numbers[3]})";
                            break;
                        case 4:
                            expr = $"({numbers[0]}{ops[0]}{numbers[1]}){ops[1]}({numbers[2]}{ops[2]}{numbers[3]})";
                            break;
                        default:
                            expr = $"{numbers[0]}{ops[0]}{numbers[1]}{ops[1]}{numbers[2]}{ops[2]}{numbers[3]}";
                            break;
                    }
                }

                try
                {
                    int result = EvaluateExpression(expr);
                    if (result >= 0 && result <= 300)
                    {
                        return (expr, result.ToString());
                    }
                }
                catch
                {
                    // 解析失败也重试
                    continue;
                }
            }

            // 兜底：返回一个默认简单题
            return ("114 + 500 - 514", "100");
        }

        // 简易表达式求值器（支持括号、+ - *，整数运算）
        private static int EvaluateExpression(string expr)
        {
            // 使用递归下降解析，支持括号和优先级
            var tokens = Tokenize(expr);
            int index = 0;
            return ParseExpression(tokens, ref index);
        }

        private static List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (char.IsDigit(c))
                {
                    string num = "";
                    while (i < expr.Length && char.IsDigit(expr[i]))
                    {
                        num += expr[i];
                        i++;
                    }
                    tokens.Add(num);
                    i--; // 回退一次，因为for循环会++
                }
                else if (c == '+' || c == '-' || c == '*' || c == '(' || c == ')')
                {
                    tokens.Add(c.ToString());
                }
            }
            return tokens;
        }

        private static int ParseExpression(List<string> tokens, ref int index)
        {
            int left = ParseTerm(tokens, ref index);
            while (index < tokens.Count && (tokens[index] == "+" || tokens[index] == "-"))
            {
                string op = tokens[index++];
                int right = ParseTerm(tokens, ref index);
                if (op == "+") left += right;
                else if (op == "-") left -= right;
            }
            return left;
        }

        private static int ParseTerm(List<string> tokens, ref int index)
        {
            int left = ParseFactor(tokens, ref index);
            while (index < tokens.Count && tokens[index] == "*")
            {
                index++; // skip '*'
                int right = ParseFactor(tokens, ref index);
                left *= right;
            }
            return left;
        }

        private static int ParseFactor(List<string> tokens, ref int index)
        {
            if (tokens[index] == "(")
            {
                index++; // skip '('
                int value = ParseExpression(tokens, ref index);
                if (tokens[index] == ")") index++; // skip ')'
                return value;
            }
            else
            {
                return int.Parse(tokens[index++]);
            }
        }
    }
    [CommandHandler(typeof(ClientCommandHandler))]
    class GOCAnswer : ICommand
    {
        public string Command => "answer";

        string[] ICommand.Aliases { get; } = new[] { "ans" };

        public string Description => "回答GOC奇术炸弹问题 .answer 答案";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var runner = Exiled.API.Features.Player.Get(sender);
            if (runner == null)
            {
                response = "Failed! runner = null";
                return false;
            }
            if (arguments.Count == 0)
            {
                response = "必须要有参数!";
                return false;
            }
            if (!GOCBomb.P2B.TryGetValue(runner, out var bomb))
            {
                response = "你不在按/拆弹!";
                return false;
            }
            if (bomb.nowquestion.a == arguments.At(0))
            {
                if (!bomb.GocIntering)
                {
                    bomb.AnotAnsweredCount++;
                }
                else
                {
                    bomb.GoCAnsweredCount++;
                }
                response = "正确!";
            }
            else
            {
                response = "错误!";

            }
            bomb.nowquestion = GOCBomb.GetNextQuestion;
            return true;
        }
    }
    class GOCBomb
    {

        public static List<Room> GetFarthestRooms(int c, ZoneType z)
        {
            // Step 1: 过滤出指定 Zone 的房间
            var candidates = Room.List.Where(room => room.Zone == z).ToList();
            candidates = candidates.Where(room => room.Type != RoomType.Unknown && room.Type != RoomType.HczCornerDeep && room.Type != RoomType.HczCrossing && room.Type != RoomType.HczCrossRoomWater && room.Type != RoomType.HczCurve && room.Type != RoomType.HczStraight &&
             room.Type != RoomType.HczStraightC && room.Type != RoomType.HczStraightPipeRoom && room.Type != RoomType.HczStraightVariant && room.Type != RoomType.HczIntersection && room.Type != RoomType.HczIntersectionJunk && room.Type != RoomType.HczTesla && room.Type != RoomType.HczEzCheckpointB && room.Type != RoomType.HczEzCheckpointA
            ).ToList();
            if (candidates.Count == 0)
                return new List<Room>();

            // 如果请求的数量超过可用房间数，返回全部
            c = Math.Min(c, candidates.Count);

            List<Room> selected = new List<Room>();

            // Step 2: 选择第一个房间（可以选第一个，也可以随机）
            selected.Add(candidates[0]);
            candidates.RemoveAt(0);

            // Step 3: 贪心选择剩余 c-1 个房间
            for (int i = 1; i < c; i++)
            {
                Room farthestRoom = null;
                float maxMinDistance = -1f;

                foreach (var candidate in candidates)
                {
                    // 计算该候选房间到已选房间集合的最小距离
                    float minDistance = float.MaxValue;
                    foreach (var selectedRoom in selected)
                    {
                        float dist = Vector3.Distance(candidate.Position, selectedRoom.Position);
                        if (dist < minDistance)
                            minDistance = dist;
                    }

                    // 如果这个最小距离比当前记录的最大值还大，就选它
                    if (minDistance > maxMinDistance)
                    {
                        maxMinDistance = minDistance;
                        farthestRoom = candidate;
                    }
                }

                if (farthestRoom != null)
                {
                    selected.Add(farthestRoom);
                    candidates.Remove(farthestRoom);
                }
                else
                {
                    // 没有更多可选房间了
                    break;
                }
            }

            return selected;
        }
        public static void init()
        {
            Log.Info("GOCBOMB init");
            if (Inited) return;

            //installCount = UnityEngine.Random.Range(3,5 + 1);
            installAt = new List<Room>()
            {
                Room.Get(RoomType.Hcz049),
                Room.Get(RoomType.Hcz079),
                Room.Get(RoomType.Hcz939),
                Room.Get(RoomType.EzIntercom),
                Room.Get(RoomType.HczElevatorB),
                Room.Get(RoomType.HczHid),
                Room.Get(RoomType.HczElevatorA),
                Room.Get(RoomType.HczNuke),
                Room.Get(RoomType.Surface),
            };
            installAt.ShuffleList();
            if (installCount < installAt.Count)
                installAt.RemoveRange(installCount, installAt.Count - installCount);

            foreach (var item in installAt)
            {
                Log.Info($"炸弹要安装在:{item} {item.RoomName} {item.Position}");
            }
            QuestionCount += UnityEngine.Random.Range(-5, 6 + 1);
            int baseCount = 30 + UnityEngine.Random.Range(-5, 6); // 25~36
            int totalQuestions = baseCount * installCount * 2; // 生成双倍，增加多样性
            for (int i = 0; i < totalQuestions; i++)
            {
                Questions.Add(MathProblemGenerator.GenerateProblem());
            }
            if (!Plugin.MenuCache.Any(x => x.Id == Plugin.plugin.Config.SettingIds[Features.Scp5kGOCAnswer]))
                Plugin.MenuCache.AddRange(MenuInit());
            Inited = true;

        }

        public GOCBomb(ushort itemID)
        {
            if (!Inited) init();
            ItemID = itemID;
            nowquestion = GetNextQuestion; // 初始化第一题

        }


        // 或者在 init 时手动设置
        public static bool Inited = false;
        public static bool Played = false;
        public static int installCount = 4;
        public static int installedCount
        {
            get
            {
                return GOCBomb.installedRoom.Count(x => x.Key.installed);
            }
        }
        public static List<GOCBomb> GOCBombList = new List<GOCBomb>();
        public static List<Room> installAt = new List<Room>();
        public static Dictionary<GOCBomb, Room> installedRoom = new Dictionary<GOCBomb, Room>();
        public static Dictionary<Exiled.API.Features.Player, GOCBomb> P2B = new Dictionary<Exiled.API.Features.Player, GOCBomb>();
        public static List<(string q, string a)> Questions = new List<(string q, string a)>();
        public static int QuestionCount = 15;
        public static int QuestionPoint = -1;
        public Exiled.API.Features.Pickups.Pickup pickup;
        private static readonly object questionLock = new object();
        public SchematicObject schematicObject;
        public static GOCBomb installbomb(Exiled.API.Features.Pickups.Pickup pickup)
        {
            var sk = new SerializableSchematic
            {
                SchematicName = "GocBombPlace",
                Position = pickup.Position
            };
            GameObject skg = sk.SpawnOrUpdateObject();
            Log.Info("GOcBombPlace");
            if (skg != null)
            {
                skg.transform.parent = pickup.GameObject.transform;
                skg.transform.position = pickup.Position;
                var g = new GOCBomb(pickup.Serial);
                installedRoom.Add(g, Room.FindParentRoom(pickup.GameObject));
                GOCBombList.Add(g);
                pickup.GameObject.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                g.schematicObject = skg.GetComponent<SchematicObject>();
                foreach (var item in skg.GetComponent<SchematicObject>().AttachedBlocks)
                {
                    if (item.name == "BombInter")
                    {
                        item.GetComponent<InvisibleInteractableToy>().OnInteracted += g.OnInter;
                        break;
                    }
                }
                g.pickup = pickup;

                return g;
            }
            return null;
        }
        public static void OnPickUp(Exiled.Events.EventArgs.Player.PickingUpItemEventArgs ev)
        {
            if (ev != null)
            {
                if (ev.Pickup.Type == ItemType.Coin)
                {
                    foreach (var item in GOCBombList)
                    {
                        if (item.ItemID == ev.Pickup.Serial)
                        {
                            ev.IsAllowed = item.Uninstall(ev.Pickup);
                            break;
                        }
                    }
                }
            }
        }
        public bool Uninstall(Exiled.API.Features.Pickups.Pickup pickup)
        {
            if (installed || intering != null)
            {
                return false;
            }
            GOCBombList.Remove(this);
            installedRoom.Remove(this);
            if (schematicObject.gameObject != null)
            {
                schematicObject.Destroy();
            }
            intering = null;

            if (installedCount == 0 && Played)
            {
                if (GOCBOmb != null)
                {
                    GOCAnim.PlayEnd();
                    Played = false;
                }
            }
            return true;
        }
        public static (string q, string a) GetNextQuestion
        {
            get
            {
                if (Questions.Count == 0)
                    throw new InvalidOperationException("题库为空，请先初始化题目。");

                lock (questionLock)
                {
                    QuestionPoint = (QuestionPoint + 1) % Questions.Count;
                    if (QuestionPoint < 0) QuestionPoint += Questions.Count; // 确保非负
                    return Questions[QuestionPoint];
                }
            }
        }
        public Player intering = null;
        public int GoCAnsweredCount = 0;
        public int AnotAnsweredCount = 0;
        public ushort ItemID = 0;
        public (string q, string a) nowquestion { get => _nowquestion; set { 
                _nowquestion = value;
                if(intering != null)
                {
                    intering.SendConsoleMessage($"New Question: {value.q} = ?", "yellow");
                }
            } }
        private (string q, string a) _nowquestion;
        public bool installed = false;
        public void OnInter(ReferenceHub hub)
        {
            var p = Exiled.API.Features.Player.Get(hub);
            if (!CustomRole.TryGet(Goc610CID, out var customGocC))
            {
                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocC 请联系技术</size></color>", 3f);
                return;
            }
            if (!CustomRole.TryGet(Goc610PID, out var customGocP))
            {
                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocP 请联系技术</size></color>", 3f);
                return;
            }
            bool isGocActing = false;
            if (customGocC.Check(p) || customGocP.Check(p))
            {
                isGocActing = true;
            }
            if (intering == null)
            {
                intering = p;
                if (installed)
                {
                    if (isGocActing)
                    {
                        p.AddMessage("GocBomb", "<color=yellow><size=27>不能拆除炸弹!</size></color>");
                        return;
                    }
                    p.AddMessage("GocBomb", "<color=yellow><size=27>正在拆除炸弹</size></color>");
                    Plugin.RunCoroutine(playerCode(p, p.CurrentRoom, false));
                }
                else
                {
                    if (installAt.Contains(p.CurrentRoom))
                    {
                        if (installedRoom.Any(x => x.Key.installed && x.Value == p.CurrentRoom))
                        {

                            p.AddMessage("GocBomb", "<color=red><size=27>房间已安装炸弹</size></color>");
                            return;
                        }
                        else
                        {
                            p.AddMessage("GocBomb", "<color=yellow><size=27>正在安装炸弹</size></color>");
                            Plugin.RunCoroutine(playerCode(p, p.CurrentRoom, true));
                        }
                    }
                    else
                    {
                        p.AddMessage("GocBomb", "<color=yellow><size=27>不在这个房间!</size></color>");
                    }
                }

            }
            else
            {
                p.AddMessage("BOmb", $"<color=yellow><size=27>玩家{p.DisplayNickname}正在操作炸弹!</size></color>");
            }
        }
        public override bool Equals(object obj)
        {
            if (!(obj is GOCBomb b)) return false;
            return b.ItemID == this.ItemID;
        }
        public static List<SettingBase> MenuInit()
        {
            var settings = new List<SettingBase>();

            settings.Add(new UserTextInputSetting(
                Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer], $"回答问题 在此处输入答案:", contentType: ContentType.IntegerNumber, isServerOnly: true,
                onChanged: (player, SB) =>
                {
                    try
                    {
                        if (!GOCBomb.P2B.TryGetValue(player, out var bomb))
                        {
                            return;
                        }
                        if (bomb.intering == null || bomb.intering.ReferenceHub != player.ReferenceHub)
                        {
                            Plugin.Unregister(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer]));
                            return; // 不在互动中，忽略输入
                        }
                        var lp = Player.Get(player.ReferenceHub);
                        if (SB is UserTextInputSetting UTI)
                        {
                            if (string.IsNullOrEmpty(UTI.Text))
                            {
                                return; // 空输入忽略
                            }
                            if (bomb.nowquestion.a == UTI.Text)
                            {
                                if (!bomb.GocIntering)
                                {
                                    bomb.AnotAnsweredCount++;
                                }
                                else
                                {
                                    bomb.GoCAnsweredCount++;
                                }
                                lp.AddMessage("answer!", "<color=green>正确!</color>", 2f, ScreenLocation.Center);
                                UTI.RequestClear((x) => x == player);
                            }
                            else
                            {
                                lp.AddMessage("answer!", "<color=red>错误!</color>", 2f, ScreenLocation.Center);
                                UTI.RequestClear((x) => x == player);
                            }
                            bomb.nowquestion = GOCBomb.GetNextQuestion;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());

                    }
                }));
            return settings;
        }
        public override int GetHashCode()
        {
            return this.ItemID.GetHashCode();
        }
        public bool GocIntering = false;
        public IEnumerator<float> playerCode(Exiled.API.Features.Player player, Room runAt, bool isGoc)
        {
            if (!P2B.ContainsKey(player))
            {
                P2B[player] = this;
            }
            else
            {
                P2B[player].intering = null; // 清理旧的
                P2B[player] = this;
            }

            if (nowquestion.q == null || nowquestion.a == null)
            {
                nowquestion = GetNextQuestion; // 👈 在这里首次获取题目
            }
            GocIntering = isGoc;
            var i = Plugin.MenuCache.FirstOrDefault(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer]);
            Plugin.Register(player, new List<SettingBase>() { i });

            if (i != null && i is UserTextInputSetting u)
            {
                u.UpdateValue("", filter: p => p == player);
                u.RequestClear(p => p == player);

            }
            while (true)
            {
                if (player == null || !player.IsConnected || player.Role.IsDead)
                {
                    break;
                }

                if (intering == null)
                {
                    break;
                }
                var lp = Player.Get(player.ReferenceHub);
                if (player.CurrentRoom != runAt)
                {
                    lp.AddMessage("Runned", "<pos=20%><color=red><size=27>你已离开房间 安装进度结束</size></color></pos>", 3f, ScreenLocation.Center);
                    if (lp.HasMessage("problem"))
                    {
                        lp.RemoveMessage("problem");
                    }
                    break;
                }
                else
                {
                    if (GoCAnsweredCount == QuestionCount)
                    {
                        if (lp.HasMessage("problem"))
                        {
                            lp.RemoveMessage("problem");
                        }
                        if (!isGoc)
                        {
                            lp.AddMessage("Runned", "<pos=20%><color=green><size=27>拆除成功</size></color></pos>", 3f, ScreenLocation.Center);
                            Uninstall(pickup);
                            installedRoom.Remove(this);
                            pickup.Destroy();
                        }
                        else
                        {
                            lp.AddMessage("Runned", "<pos=20%><color=green><size=27>安装成功</size></color></pos>", 3f, ScreenLocation.Center);



                        }
                        installed = true;
                        AnotAnsweredCount = 0;
                        GoCAnsweredCount = 0;
                        if (isGoc && installedCount == 1 && !Played)
                        {
                            Exiled.API.Features.Cassie.Message("警告!GOC正在安装奇术核弹 所有人员前往阻止/拆除", isSubtitles: true);
                            Played = true;

                        }
                        if (installedCount == installCount)
                        {
                            Timing.RunCoroutine(CountDown());
                        }
                        break;
                    }
                    if (AnotAnsweredCount == QuestionCount)
                    {
                        if (lp.HasMessage("problem"))
                        {
                            lp.RemoveMessage("problem");
                        }
                        if (!isGoc)
                        {
                            lp.AddMessage("Runned", "<pos=20%><color=green><size=27>拆除成功</size></color></pos>", 3f, ScreenLocation.Center);
                            installed = false;
                            intering = null;
                            Uninstall(pickup);
                            installedRoom.Remove(this);
                            pickup.Destroy();
                            GoCAnsweredCount = 0;
                            AnotAnsweredCount = 0;
                        }

                        //Uninstall
                        break;
                    }
                    else if (!lp.HasMessage("problem"))
                    {

                        if (isGoc)
                        {
                            lp.AddMessage("problem", (p) =>
                            {
                                if (nowquestion.q == null || nowquestion.a == null)
                                {
                                    nowquestion = GetNextQuestion; // 👈 在这里首次获取题目
                                }
                                if (AnotAnsweredCount == QuestionCount)
                                {
                                    if (lp.HasMessage("problem"))
                                    {
                                        lp.RemoveMessage("problem");
                                    }
                                    return new string[] { $"" };
                                }
                                return new string[]{
                            $"<pos=45%><color=yellow><size=27>第{GoCAnsweredCount + 1}题 还剩{QuestionCount - GoCAnsweredCount - 1}题 使用 .answer 答案 或者Server-specific回答</size></color></pos>\n<pos=45%><color=green><size=27>{nowquestion.q} = ?</size></color></pos>"};

                            }, -1f, ScreenLocation.CenterTop);
                        }
                        else
                        {
                            lp.AddMessage("problem", (p) =>
                            {
                                if (nowquestion.q == null || nowquestion.a == null)
                                {
                                    nowquestion = GetNextQuestion; // 👈 在这里首次获取题目
                                }
                                if (AnotAnsweredCount == QuestionCount)
                                {
                                    if (lp.HasMessage("problem"))
                                    {
                                        lp.RemoveMessage("problem");
                                    }
                                    return new string[] { $"" };
                                }
                                return new string[]{
                            $"<pos=45%><color=yellow><size=27>第{AnotAnsweredCount + 1}题 还剩{QuestionCount - AnotAnsweredCount - 1}题 使用 .answer 答案 或者Server-specific回答</size></color></pos>\n<pos=45%><color=green><size=27>{nowquestion.q} = ?</size></color></pos>"};

                            }, -1f, ScreenLocation.CenterTop);
                        }
                    }
                    yield return Timing.WaitForSeconds(0.3f);
                }
            }
            intering = null;
            if (i != null && i is UserTextInputSetting u1)
            {
                u1.UpdateValue("", filter: p => p == player);
                u1.RequestClear(p => p == player);
            }

            Plugin.Unregister(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer]));
            P2B.Remove(player); // 👈 清理字典，避免玩家断开后仍占用内存
            yield break;
        }
        public static float countDownStart = 60f; // 倒计时总时长
        public static float countDown = 60f; // 剩余时间
        public static float countDownTick = 1f; // 每次减少的时间
        public static bool CountdownStarted = false; // 是否已开始倒计时

        // 更新GOCBomb类，加入倒计时协程
        public static IEnumerator<float> CountDown()
        {
            // 防止并发启动
            if (CountdownStarted)
                yield break;

            CountdownStarted = true; // 修正：标记为已启动
            try
            {
                // 确保每次从初始值开始
                countDown = countDownStart;

                GocSpawnable = false;
                Exiled.API.Features.Cassie.Message($"警告!GOC奇术核弹安装完成 预计在{countDown}秒后预热完成! 请务必拆除所有{GOCBomb.installCount}个炸弹", isHeld: true, isSubtitles: true);
                if (GOCBOmb == null)
                {
                    GOCAnim.Gen(new Vector3(13f, 450f, -40f));
                    Played = true;
                }

                foreach (var item in WaveManager.Waves)
                {
                    if (item is TimeBasedWave IL)
                        IL.Timer.Reset();
                }

                while (true)
                {
                    try
                    {
                        if (countDown <= 0)
                        {
                            countDown = 0;
                            foreach (var item in Player.Enumerable)
                                if (item.HasMessage("donationCount"))
                                    item.RemoveMessage("donationCount");

                            Exiled.API.Features.Cassie.Message($"警告!GOC奇术核弹预热完成 预计在40到60秒后爆炸 尽快撤离!", isSubtitles: true);
                            GOCAnim.PlayDonate();
                            break;
                        }
                        else
                        {
                            if (installedCount == 0)
                            {
                                GOCAnim.PlayEnd();
                                countDown = countDownStart;
                                break;
                            }

                            if (!CustomRole.TryGet(Goc610CID, out var customGocC))
                                Log.Info("Failed to get goc");
                            if (!CustomRole.TryGet(Goc610PID, out var customGocP))
                                Log.Info("Failed to get goc");

                            foreach (var item in Player.Enumerable)
                            {
                                bool isGocActing = false;
                                if (customGocC != null && customGocP != null)
                                    if (customGocC.Check(item) || customGocP.Check(item))
                                        isGocActing = true;

                                if (!item.HasMessage("donationCount"))
                                {
                                    if (isGocActing)
                                    {
                                        item.AddMessage("donationCount", (p) =>
                                            new string[] {
                                                $"<pos=40%><voffset=-1em%><color=red><size=27>在 {countDown.ToString("F0")}秒内保护GOC奇术核弹!</size></color></pos>\n<pos=60%><color=green><size=27>目前剩下:{installedCount}个炸弹</size></color></pos>"
                                            }, -1f, ScreenLocation.MiddleRight);
                                    }
                                    else
                                        {
                                            item.AddMessage("donationCount", (p) =>
                                                new string[] {
                                                $"<pos=40%><voffset=-1em%><color=red><size=27>在 {countDown.ToString("F0")}秒内阻止GOC奇术核弹!</size></color></pos>\n<pos=60%><color=green><size=27>目前剩下:{installedCount}个炸弹</size></color></pos>"
                                            }, -5f, ScreenLocation.MiddleRight);
                                    }
                                    }
                            }
                                    }
                                }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }

                    yield return Timing.WaitForSeconds(countDownTick);
                    countDown -= countDownTick;
                }
            }
            finally
            {
                // 强制清理，确保标志与消息被移除
                CountdownStarted = false;
                foreach (var item in Player.Enumerable)
                    if (item.HasMessage("donationCount"))
                        item.RemoveMessage("donationCount");
            }
        }

    }
}
