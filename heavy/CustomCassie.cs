using Cassie;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using HarmonyLib;
using Next_generationSite_27.UnionP.Buffs;
using Next_generationSite_27.UnionP.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils.NonAllocLINQ;

namespace Next_generationSite_27.UnionP.heavy
{
    public static class CustomCassie
    {
        public static void CustomCassieMessage(this string message, string who = "", string color = "red", bool isHeld = false, bool isNoisy = false, bool isSubtitles = true)
        {
            string[] array = message.Split('\n');
            var finallystring = StringBuilderPool.Pool.Get();
            var max = 45;
            var pos = 0;
            List<string> TempList = new();
            foreach (string text in array)
            {
                var t1 = text.Replace(" ", "\u2005");
                if (t1.Length > max)
                {
                    for (int i = 0; i < t1.Length; i += max)
                    {
                        int len = Math.Min(max, t1.Length - i);
                        TempList.Add(t1.Substring(i, len));
                    }
                }
                else
                {
                    TempList.Add(t1);

                }
            }
            int n = 0;
            foreach (var item in TempList)
            {
                //finallyList.Add(item);
                n++;
                finallystring.Append(item);
                if (n == TempList.Count)
                {
                    break;
                }
                finallystring.Append($"<pos={pos}em><voffset=-{n}em>");
            }
            SendCustomCassieMessage(finallystring.ToString(), "", who, color, isHeld, isNoisy, isSubtitles);

            StringBuilderPool.Pool.Return(finallystring);

        }
        public static void SendCustomCassieMessage(string message, string tts = "", string who = "", string color = "red", bool isHeld = false, bool isNoisy = false, bool isSubtitles = true)
        {
            int TotalOffest = 100;
            Log.Info("Org CustomCassie Message: " + message);

            // 匹配并累加所有形如 <coffset=...em> 或 <voffset=...em> 的数值（允许负数与小数）
            try
            {
                var matches = Regex.Matches(message, @"<\s*voffset\s*=\s*(-?\d+(?:\.\d+)?)\s*em", RegexOptions.IgnoreCase);
                double sumAbs = 0.0;
                foreach (Match m in matches)
                {
                    var g = m.Groups[1].Value;
                    if (double.TryParse(g, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                    {
                        sumAbs += Math.Abs(val);
                    }
                }
                TotalOffest += (int)Math.Round(sumAbs);

                // 2) 将文本内每个 <voffset=xem> 替换为 <voffset=(TotalOffest/2 + x)em>
                string replaced = Regex.Replace(
                    message,
                    @"<\s*voffset\s*=\s*(-?\d+(?:\.\d+)?)\s*em",
                    match =>
                    {
                        var s = match.Groups[1].Value;
                        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                            x = 0;
                        double newVal = (TotalOffest / 2.0) + x;
                        // 如果是整数则不显示小数，否则保留最多3位小数
                        return $"<voffset={Math.Floor(newVal)}em";
                    },
                    RegexOptions.IgnoreCase);
                var Text = $"<voffset={TotalOffest}em>1<voffset={TotalOffest / 2}em><indent=0%>";
                if (!string.IsNullOrEmpty(who))
                {
                    Text += $"<color={color}>\u2005{who.Replace(" ", " \u2005")}</color> : ";
                }
                Text += $"{replaced}</indent></voffset>";
                Log.SendRaw("Sending CustomCassie Message: " + Text + "", ConsoleColor.Cyan);
                foreach (var item in Player.Enumerable)
                {
                    item.SendConsoleMessage("<noparse>" + Text + "</noparse>", "white");
                }
                if (CassieIsOffline.Instance.CheckEnabled())
                {
                    var s = new CassieTtsPayload(Text, false, new Subtitles.SubtitlePart());
                    new CassieAnnouncement(s, 0f, 0f).AddToQueue();
                }
                else
                {
                    var s = new CassieTtsPayload(tts,Text, false);
                    new CassieAnnouncement(s, 0f, 0f).AddToQueue();
                }
            }
            catch (Exception ex)
            {
                // 不应阻塞主流程，记录日志以便调试
                Log.Debug($"CustomCassie: voffset: {ex}");
            }

        }
    }
}
