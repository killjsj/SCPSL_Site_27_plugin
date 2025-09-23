using AdminToys;
using AutoEvent.Interfaces;
using CommandSystem;
using CustomRendering;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.Commands.Reload;
using Exiled.Events.EventArgs.Player;
using Exiled.Loader;
using InventorySystem.Items.Firearms;
using LabApi.Features.Wrappers;
using LiteNetLib;
using MEC;
using Mirror;
using Next_generationSite_27.Features.PlayerHuds;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.DedicatedServer;
using UnityEngine.EventSystems;
using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
using static TMPro.TMP_InputField;
using static UnityEngine.UI.CanvasScaler;
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
                bomb.AnsweredCount++;
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
             room.Type != RoomType.HczStraightC && room.Type != RoomType.HczStraightPipeRoom && room.Type != RoomType.HczStraightVariant && room.Type != RoomType.HczIntersection && room.Type != RoomType.HczIntersectionJunk && room.Type != RoomType.HczTesla
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
            installAt = GetFarthestRooms(installCount, ZoneType.HeavyContainment);
            foreach (var item in installAt)
            {
                Log.Info($"炸弹要安装在:{item} {item.RoomName} {item.Position}");
            }
            //QuestionCount += UnityEngine.Random.Range(-5, 6 + 1);
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
        public static int installCount = 2;
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
        public static int QuestionCount = 1;
        public static int QuestionPoint = -1;
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
            schematicObject.Destroy();
            intering = null;

            if (installedCount == 0 && Played)
            {
                if (Scp5k_Control.GOCBOmb != null)
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
        public LabApi.Features.Wrappers.Player intering = null;
        public int AnsweredCount = 0;
        public ushort ItemID = 0;
        public (string q, string a) nowquestion;
        public bool installed = false;
        public void OnInter(ReferenceHub hub)
        {
            var p = LabApi.Features.Wrappers.Player.Get(hub);
            if (!CustomRole.TryGet(Scp5k_Control.GocCID, out var customGocC))
            {
                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocC 请联系技术</size></color>", 3f);
                return;
            }
            if (!CustomRole.TryGet(Scp5k_Control.GocPID, out var customGocP))
            {
                p.AddMessage("Failed", "<color=red><size=27>未获取角色:GocP 请联系技术</size></color>", 3f);
                return;
            }
            var ep = Exiled.API.Features.Player.Get(hub);
            bool isGocActing = false;
            if (customGocC.Check(ep) || customGocP.Check(ep))
            {
                isGocActing = true;
            }
            if (intering == null)
            {
                if (installed)
                {
                    if (isGocActing)
                    {
                        p.AddMessage("GocBomb", "<color=yellow><size=27>不能拆除炸弹!</size></color>");
                        return;
                    }
                    intering = p;
                    p.AddMessage("GocBomb", "<color=yellow><size=27>正在拆除炸弹</size></color>");
                    Timing.RunCoroutine(playerCode(ep, ep.CurrentRoom));
                }
                else
                {
                    if (installAt.Contains(ep.CurrentRoom))
                    {
                        if (installedRoom.Any(x => x.Key.installed && x.Value == ep.CurrentRoom))
                        {

                            p.AddMessage("GocBomb", "<color=red><size=27>房间已安装炸弹</size></color>");
                            return;
                        }
                        else
                        {
                            intering = p;
                            p.AddMessage("GocBomb", "<color=yellow><size=27>正在安装炸弹</size></color>");
                            Timing.RunCoroutine(playerCode(ep, ep.CurrentRoom));
                        }
                    }
                    else
                    {
                        p.AddMessage("GocBomb", "<color=yellow><size=27>不在这个房间!</size></color>");
                    }
                }

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
                Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer], $"回答问题 在此处输入答案:", contentType: ContentType.IntegerNumber,
                onChanged: (player, SB) =>
                {
                    try
                    {
                        if (!GOCBomb.P2B.TryGetValue(player, out var bomb))
                        {
                            return;
                        }
                        var lp = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);
                        if (SB is UserTextInputSetting UTI)
                        {
                            if (bomb.nowquestion.a == UTI.Text)
                            {
                                bomb.AnsweredCount++;
                                lp.AddMessage("answer!", "<color=green>正确!</color>", 3f, Enums.ScreenLocation.CenterBottom);
                                UTI.RequestClear((x) => x == player);
                            }
                            else
                            {
                                lp.AddMessage("answer!", "<color=red>错误!</color>", 3f, Enums.ScreenLocation.CenterBottom);

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
        public IEnumerator<float> playerCode(Exiled.API.Features.Player player, Room runAt)
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
            SettingBase.Register(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer]));

            while (!player.Role.IsDead)
            {
                if (intering == null)
                {
                    break;
                }
                var lp = LabApi.Features.Wrappers.Player.Get(player.ReferenceHub);
                if (player.CurrentRoom != runAt)
                {
                    lp.AddMessage("Runned", "<pos=20%><color=red><size=27>你已离开房间 安装进度结束</size></color></pos>", 3f, Enums.ScreenLocation.CenterBottom);
                    break;
                }
                else
                {
                    if (AnsweredCount == QuestionCount)
                    {
                        if (installed)
                        {
                            lp.AddMessage("Runned", "<pos=20%><color=green><size=27>拆除成功</size></color></pos>", 3f, Enums.ScreenLocation.CenterBottom);
                            installedRoom.Remove(this);
                        }
                        else
                        {
                            lp.AddMessage("Runned", "<pos=20%><color=green><size=27>安装成功</size></color></pos>", 3f, Enums.ScreenLocation.CenterBottom);


                            if (installedCount == 1 && !Played)
                            {
                                Exiled.API.Features.Cassie.Message("警告!GOC正在安装奇术核弹 所有人员前往阻止/拆除", isSubtitles: true);
                                Played = true;

                            }
                        }
                        installed = !installed;
                        AnsweredCount = 0;
                        break;
                    }
                    if (!lp.HasMessage("problem"))
                    {
                        lp.AddMessage("problem", (p) =>
                        {
                            if (nowquestion.q == null || nowquestion.a == null)
                            {
                                nowquestion = GetNextQuestion; // 👈 在这里首次获取题目
                            }
                            return new string[]{
                            $"<pos=45%><color=yellow><size=27>第{AnsweredCount + 1}题 还剩{QuestionCount - AnsweredCount - 1}题 使用 .answer 答案 或者Server-specific回答</size></color></pos>\n<pos=45%><color=green><size=27>{nowquestion.q} = ?</size></color></pos>"};

                        }, 4f, Enums.ScreenLocation.CenterTop);
                    }
                    yield return Timing.WaitForSeconds(0.3f);
                }
            }
            intering = null;
            SettingBase.Unregister(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kGOCAnswer]));

            P2B.Remove(player); // 👈 清理字典，避免玩家断开后仍占用内存
            yield break;
        }
        public static float countDown = 100;
        public static float countDownTick = 0.2f;
        public static IEnumerator<float> CountDown()
        {
                            Scp5k_Control.GocSpawnable = false;
            Exiled.API.Features.Cassie.Message($"警告!GOC奇术核弹安装完成 预计在{countDown}秒后爆炸! 请务必拆除所有 {GOCBomb.installCount} 个炸弹", isSubtitles: true);
            if (Scp5k.Scp5k_Control.GOCBOmb == null)
            {
                GOCAnim.Gen(new Vector3(13f, 360f, -40f));
                //Exiled.API.Features.Cassie.Message("警告!GOC正在安装奇术核弹 所有人员前往阻止/拆除", isSubtitles: true);
                Played = true;
            }
            if (!CustomRole.TryGet(Scp5k_Control.GocCID, out var customGocC))
            {
                Log.Info("Failed to get goc");

            }
            if (!CustomRole.TryGet(Scp5k_Control.GocPID, out var customGocP))
            {
                Log.Info("Failed to get goc");
            }

            while (true)
            {
                try
                {
                    if (countDownTick <= 0)
                    {
                        countDownTick = 0; break;
                    }
                    else
                    {
                        if(installedCount == 0)
                        {

                            Exiled.API.Features.Cassie.Message($"GOC奇术核弹拆除完毕 终结所有GOC人员", isSubtitles: true);
                            GOCAnim.PlayEnd();
                            countDown = 100;
                            yield break;
                        }
                        foreach (var item in LabApi.Features.Wrappers.Player.GetAll())
                        {
                            var ep = Exiled.API.Features.Player.Get(item);
                            bool isGocActing = false;
                            if (customGocC != null && customGocP != null)
                            {
                                if (customGocC.Check(ep) || customGocP.Check(ep))
                                {
                                    isGocActing = true;
                                }
                            }
                            if (isGocActing)
                            {
                                if (!item.HasMessage("donationCount"))
                                {
                                    item.AddMessage("donationCount", (p) =>
                                    {
                                        return new string[]{
                            $"<voffset=-1em%><color=red><size=27>在 {countDown.ToString("F0")}内保护GOC奇术核弹!</size></color></pos>\n<pos=45%><color=green><size=27>目前剩下:{installedCount}</size></color></pos>"};

                                    }, 5f, Enums.ScreenLocation.CenterTop);
                                }
                            }
                            else
                            {
                                if (!item.HasMessage("donationCount"))
                                {
                                    item.AddMessage("donationCount", (p) =>
                                    {
                                        return new string[]{
                            $"<voffset=-1em%><color=red><size=27>在 {countDown.ToString("F0")}内阻止GOC奇术核弹!</size></color></pos>\n<pos=45%><color=green><size=27>目前剩下:{installedCount}</size></color></pos>"};

                                    }, 5f, Enums.ScreenLocation.CenterTop);
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
            foreach (var item in LabApi.Features.Wrappers.Player.GetAll())
            {
                var ep = Exiled.API.Features.Player.Get(item);
                bool isGocActing = false;
                if (customGocC != null && customGocP != null)
                {
                    if (customGocC.Check(ep) || customGocP.Check(ep))
                    {
                        isGocActing = true;
                    }
                }
                if (item.HasMessage("donationCount"))
                {
                    item.RemoveMessage("donationCount");
                }
                    
            }
            Exiled.API.Features.Cassie.Message($"警告!GOC奇术核弹预热完成 预计在40到20秒后爆炸 尽快撤离!", isSubtitles: true);
            GOCAnim.PlayDonate();
        }

    }
}
