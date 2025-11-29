using Exiled.API.Features;
using Exiled.API.Features.Pools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP.heavy
{
    public static class CustomCassie
    {
        public static void CustomCassieMessage(this string message, string who = "", string color = "red", bool isHeld = false, bool isNoisy = false, bool isSubtitles = true)
        {
            string[] array = message.Split('\n');
            List<string> finallyList = new();
            var max = 45;
            foreach (string text in array)
            {
                var t1 = text.Replace(" ", "\u2005");
                if (t1.Length > max) {
                    for (int i = 0; i < t1.Length; i += max)
                    {
                        int len = Math.Min(max, t1.Length - i);
                        finallyList.Add(t1.Substring(i, len));
                    }
                }else
                {
                    finallyList.Add(t1);
                }
                //
            }
            foreach (var item in finallyList)
            {
                SendCustomCassieMessage(item, "", who, color, isHeld, isNoisy, isSubtitles);
            }
        }
        public static void SendCustomCassieMessage(string message, string tts = "",string who = "",string color = "red", bool isHeld = false, bool isNoisy = false, bool isSubtitles = true)
        {

            var Text = $"<voffset=100em><size=0>{(string.IsNullOrEmpty(tts)? "1" : tts)}</size><voffset=50em><indent=0%>";
            if (!string.IsNullOrEmpty(who))
            {
                Text += $"<color={color}>\u2005{who.Replace(" ", "\u2005")}</color> : ";
            }
            Text += $"{message}</indent></voffset>";
            Cassie.Message(Text, isHeld, isNoisy, isSubtitles);
        }
    }
}
