using Exiled.API.Features;
using Hints;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using MEC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static HintServiceMeow.Core.Models.HintContent.AutoContent;

namespace Next_generationSite_27.UnionP.UI
{
    class HSM_hintServ : UI.IHintShowHUD
    {
        public static Dictionary<PlayerDisplay, HSM_hintServ> PDH = new Dictionary<PlayerDisplay, HSM_hintServ>();
        public Dictionary<AbstractHint, (Stopwatch, float)> Dur = new Dictionary<AbstractHint, (Stopwatch, float)>();
        public PlayerDisplay hud;
        static public IHintShowHUD GetPlayerHUD(Player player)
        {
            if (PDH.ContainsKey(PlayerDisplay.Get(player)))
            {
                return PDH[PlayerDisplay.Get(player)];
            }
            return new HSM_hintServ(player);
        }
        static public bool GetPlayerHUD(Player player, out IHintShowHUD hud)
        {
            if (PDH.ContainsKey(PlayerDisplay.Get(player)))
            {
                hud = PDH[PlayerDisplay.Get(player)];
            }
            hud = new HSM_hintServ(player);
            return true;
        }
        public IHintShowHUD GetHUD(Player player)
        {
            if (PDH.ContainsKey(PlayerDisplay.Get(player)))
            {
                return PDH[PlayerDisplay.Get(player)];
            }
            return new HSM_hintServ(player);
        }
        public HSM_hintServ(Player player)
        {
            this.hud = PlayerDisplay.Get(player);
            coroutineHandle = Timing.RunCoroutine(Updater());
        }
        public CoroutineHandle coroutineHandle;
        public IEnumerator<float> Updater()
        {
            while (true)
            {
                if (hud == null || hud.ReferenceHub == null)
                {
                    yield break;
                }
                foreach (var item in Dur)
                {
                    if (item.Value.Item1.Elapsed.TotalSeconds > item.Value.Item2 && item.Value.Item2 > 0)
                    {
                        hud.RemoveHint(item.Key);
                        Timing.CallDelayed(0.01f, () =>
                        {
                            Dur.Remove(item.Key);
                        });
                    }
                }
                yield return Timing.WaitForSeconds(0.4f);
            }
        }
        ~HSM_hintServ()
        {
            this.hud = null;
            Timing.KillCoroutines(coroutineHandle);
            foreach (var item in Dur)
            {
                this.hud.RemoveHint(item.Key);
            }
        }
        public void AddMessage(string id, Func<Player, string[]> getter, float duration = 5, ScreenLocation location = ScreenLocation.Center)
        {
            var a = ParsePos(location);
            var targetX = a.targetX;
            var targetY = a.targetY;
            var h = new HintServiceMeow.Core.Models.Hints.DynamicHint()
            {
                Id = id,
                AutoText = new TextUpdateHandler((x) =>
                {
                    string r = "";
                    foreach (var i in getter.Invoke(Player.Get(x.PlayerDisplay.ReferenceHub)))
                    {
                        r += i + "\n";
                    }
                    return r;
                }),
                TargetX = targetX,
                TargetY = targetY,
                TopBoundary = targetY,
                LeftBoundary = targetX,
                BottomBoundary = targetY + 200,
                RightBoundary = targetX + 100,
                Strategy = HintServiceMeow.Core.Enum.DynamicHintStrategy.StayInPosition,
            };
            if (duration != 0)
            {
                Dur.Add(h, (Stopwatch.StartNew(), duration));
            }
            else
            {
                Dur.Add(h, (new Stopwatch(), duration));

            }
            hud.AddHint(h);
        }

        bool IHintShowHUD.HasMessage(string messageID)
        {
            return hud.HasHint(messageID);
        }

        void IHintShowHUD.RemoveMessage(string id)
        {
            List<AbstractHint> wait = new List<AbstractHint>();
            foreach (AbstractHint item in from predicate in Dur.Keys
                                          where predicate.Id == id
                                          select predicate)
            {
                wait.Add(item);
            }
            hud.RemoveHint(id);
            foreach (var item in wait)
            {
                Dur.Remove(item);

            }
        }

        public void AddMessage(string id, string c, float duration = 5, ScreenLocation location = ScreenLocation.Center)
        {
            var a = ParsePos(location);
            var targetX = a.targetX;
            var targetY = a.targetY;
            var h = new HintServiceMeow.Core.Models.Hints.DynamicHint()
            {
                Id = id,
                AutoText = new TextUpdateHandler((x) => c),
                TargetX = targetX,
                TargetY = targetY,
                TopBoundary = targetY,
                BottomBoundary = targetY + 200,
                RightBoundary = targetX + 100,
                LeftBoundary = targetX,
                Strategy = HintServiceMeow.Core.Enum.DynamicHintStrategy.StayInPosition,
                SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Normal

            };
            if (duration != 0)
            {
                Dur.Add(h, (Stopwatch.StartNew(), duration));
            }
            else
            {
                Dur.Add(h, (new Stopwatch(), duration));

            }
            hud.AddHint(h);
        }
        public static (float targetX, float targetY) ParsePos(ScreenLocation location) {
            var targetX = 0;
            var targetY = 0;
            switch (location)
            {
                case ScreenLocation.Top:
                    targetX = -100;
                    targetY = 200;
                    break;
                case ScreenLocation.CenterTop:
                    targetY = 100;
                    targetX = 0;
                    break;
                case ScreenLocation.Center:
                    targetY = 600;
                    targetX = 50;
                    break;
                case ScreenLocation.MiddleLeft:
                    targetY = 200;
                    targetX = -100;
                    break;
                case ScreenLocation.Middle:
                    targetY = 200;
                    targetX = 0;
                    break;
                case ScreenLocation.Scp914:
                    targetY = 200;
                    targetX = 0;
                    break;
                case ScreenLocation.MiddleRight:
                    targetY = 200;
                    targetX = 100;
                    break;
                case ScreenLocation.CenterBottom:
                    targetY = 1000;
                    targetX = 0;
                    break;
                case ScreenLocation.ReversedForPlayerLVShow:
                    targetY = 1070;
                    targetX = 0;
                    break;
                case ScreenLocation.NtfSpawn:
                    targetX = 150;
                    targetY = 100;
                    break;
                case ScreenLocation.ChaosSpawn:
                    targetX = 900;
                    targetY = 100;
                    break;
                case ScreenLocation.Bottom:
                    targetY = 1070;
                    targetX = -100;
                    break;
                default:
                case ScreenLocation.Custom:
                    targetX = 0;
                    targetY = 0;
                    break;
            }
            return (targetX, targetY);
        }
    }
    
}
