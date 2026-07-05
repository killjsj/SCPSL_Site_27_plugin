using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Utilities;
using Interactables.Interobjects;
using MEC;
using Next_generationSite_27.UnionP.testing;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using Scp914;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Utils;
using static HintServiceMeow.Core.Models.HintContent.AutoContent;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    class PlayerHUDManager : BaseClass
    {
        public override void Init()
        {
            Exiled.Events.Handlers.Player.InteractingElevator += InteractingElevator;
            Exiled.Events.Handlers.Scp914.ChangingKnobSetting += ChangingKnobSetting;
            Exiled.Events.Handlers.Scp914.Activating += Activating;
            Exiled.Events.Handlers.Map.AnnouncingNtfEntrance += AnnouncingNtfEntrance;
            Exiled.Events.Handlers.Map.AnnouncingChaosEntrance += AnnouncingChaosEntrance;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Player.InteractingElevator -= InteractingElevator;
            Exiled.Events.Handlers.Scp914.ChangingKnobSetting -= ChangingKnobSetting;
            Exiled.Events.Handlers.Scp914.Activating -= Activating;
            Exiled.Events.Handlers.Map.AnnouncingNtfEntrance -= AnnouncingNtfEntrance;
            Exiled.Events.Handlers.Map.AnnouncingChaosEntrance -= AnnouncingChaosEntrance;
        }

        public static int doc = 0;
        public static int ntf = 0;
        public static int gruad = 0;
        public static int chaos = 0;
        public static int dd = 0;

        public static Hint ElevatorHint = new Hint()
        {
            Id = "ElevatorHint",
            AutoText = ElevatorHintUpdater,
            YCoordinate = 300,
            XCoordinate = 600,
            SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Fast
        };

        public static string ElevatorHintUpdater(AutoContentUpdateArg ev)
        {
            if (ev.PlayerDisplay.ReferenceHub == null) return "";

            string r = "";
            bool hasContent = false;

            foreach (var item in ElevatorInteractions.ToArray().Where(x => Vector3.Distance(x.InteractAt, ev.PlayerDisplay.ReferenceHub.transform.position) <= 9f))
            {
                if (Time.time - item.InteractTime <= 2f)
                {
                    if (!hasContent) { r = "<size=22><color=#FFFF00>"; hasContent = true; }
                    r += $"{item.Interactor.Nickname}启用电梯\n";
                }
                else { ElevatorInteractions.Remove(item); }
            }

            if (hasContent) r += "</color></size>";
            return r;
        }

        public struct ElevatorInteractInfo
        {
            public Vector3 InteractAt;
            public Player Interactor;
            public float InteractTime;
        }

        public static List<ElevatorInteractInfo> ElevatorInteractions = new();

        public static void InteractingElevator(InteractingElevatorEventArgs ev)
        {
            Lift l = ev.Lift;
            Player p = ev.Player;
            if (ev.IsAllowed && l != null && p != null && l.Status == ElevatorChamber.ElevatorSequence.Ready && ev.Elevator.IsReadyForUserInput)
            {
                ElevatorInteractions.RemoveAll(x => x.Interactor == p && (Time.time - x.InteractTime) < 0.3f);
                ElevatorInteractions.Add(new ElevatorInteractInfo()
                {
                    InteractAt = p.Position,
                    Interactor = p,
                    InteractTime = Time.time
                });
            }
        }

        public static Hint Scp914Hint = new Hint()
        {
            AutoText = Scp914Updater,
            YCoordinate = 200,
            XCoordinate = 0,
            SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Normal
        };

        public static List<string> Keep = new List<string>();

        public static string Scp914Updater(AutoContentUpdateArg ev)
        {
            if (ev.PlayerDisplay.ReferenceHub == null) { ev.PlayerDisplay.RemoveHint(ev.Hint); return ""; }
            else if (!LabApi.Features.Wrappers.Room.TryGetRoomAtPosition(ev.PlayerDisplay.ReferenceHub.transform.position, out var r)) { ev.PlayerDisplay.RemoveHint(ev.Hint); return ""; }
            else if (r.Base.Name != MapGeneration.RoomName.Lcz914) { ev.PlayerDisplay.RemoveHint(ev.Hint); return ""; }

            int MaxQueueSize = 6;
            if (Scp914q.Count > MaxQueueSize)
            {
                while (Scp914q.Count > MaxQueueSize)
                    Scp914q.Dequeue();
            }
            ev.DefaultUpdateDelay = TimeSpan.FromSeconds(0.6);
            ev.NextUpdateDelay = TimeSpan.FromSeconds(0.6);
            string t = "";

            var p = Player.Get(ev.PlayerDisplay.ReferenceHub);
            if (p != null && p.CurrentRoom != null)
            {
                if (p.CurrentRoom.Type != RoomType.Lcz914) { ev.PlayerDisplay.RemoveHint(ev.Hint); return ""; }
            }
            else { return ""; }

            if (Scp914q.Count > 0)
            {
                while (Scp914q.Count != 0)
                {
                    var k = Scp914q.Dequeue();
                    string transstr = k.knob switch
                    {
                        Scp914KnobSetting.Rough => "超粗",
                        Scp914KnobSetting.Coarse => "粗加",
                        Scp914KnobSetting.OneToOne => "1:1",
                        Scp914KnobSetting.Fine => "精加",
                        Scp914KnobSetting.VeryFine => "超精",
                        _ => ""
                    };
                    if (k.act)
                        t += $"<size=22><color=green>{k.p.Nickname}</color> 激活了914 模式:<color=yellow>{transstr}</color></size>\n";
                    else
                        t += $"<size=22><color=green>{k.p.Nickname}</color> 修改914模式到 <color=yellow>{transstr}</color></size>\n";
                }
            }
            return t;
        }

        public static Queue<(Player p, Scp914KnobSetting knob, bool act)> Scp914q = new Queue<(Player p, Scp914KnobSetting knob, bool act)>();

        public static void ChangingKnobSetting(ChangingKnobSettingEventArgs ev)
        {
            Scp914q.Enqueue((ev.Player, ev.KnobSetting, false));

            foreach (var player in Player.Enumerable.Where(player => player.CurrentRoom?.RoomName != MapGeneration.RoomName.Lcz914))
            {
                try
                {
                    var hudComponent = player.GetHUD() as HSM_hintServ;
                    if (hudComponent == null || hudComponent.hud == null) continue;
                    if (!hudComponent.hud.HasHint("Scp914KnobChanged"))
                        hudComponent.hud.AddHint(Scp914Hint);
                }
                catch (Exception ex) { Log.Warn($"Failed to show SCP-914 hint to player {player.Nickname}: {ex.Message}"); }
            }
        }

        public static void Activating(ActivatingEventArgs ev)
        {
            Scp914q.Enqueue((ev.Player, ev.KnobSetting, true));

            foreach (var player in Player.Enumerable)
            {
                try
                {
                    if (player == null || player.CurrentRoom == null) continue;
                    if (player.CurrentRoom?.RoomName != MapGeneration.RoomName.Lcz914) continue;
                    var hudComponent = player.GetHUD() as HSM_hintServ;
                    if (hudComponent == null || hudComponent.hud == null) continue;
                    if (!hudComponent.hud.HasHint("Scp914KnobChanged"))
                        hudComponent.hud.AddHint(Scp914Hint);
                }
                catch (Exception ex) { Log.Warn($"Failed to show SCP-914 hint to player {player.Nickname}: {ex.Message}"); }
            }
        }

        public static Hint RoleHint = new Hint()
        {
            Id = "RoleHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                return RoleShow(Player.Get(x.PlayerDisplay.ReferenceHub));
            }),
            XCoordinate = 360,
            YCoordinate = 750,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Center
        };

        public static string RoleShow(Player player)
        {
            string v = "<align=right><size=19><b>";
            if (player != null)
            {
                if (!player.IsScp)
                {
                    if (player.Role.Team == Team.FoundationForces || player.Role.Team == Team.Scientists)
                        v += $"<color=#00FFFF> {doc}:博士数量 </color>\n <color=#808080> {gruad}:保安数量 </color> \n <color=#0000FF> {ntf}:九尾数量 </color>";
                    else if (player.Role.Team == Team.ChaosInsurgency || player.Role.Team == Team.ClassD)
                        v += $"<color=yellow> {dd}:dd数量 </color>\n <color=#009900> {chaos}:混沌数量 </color>";
                }
            }
            v += "</b></size></align>";
            return v;
        }

        public static Hint NtfSpawnHint = new Hint()
        {
            Id = "NtfSpawnHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                if (x.PlayerDisplay.ReferenceHub != null)
                {
                    var p = Player.Get(x.PlayerDisplay.ReferenceHub);
                    if (p != null && p.IsAlive) return "";
                }
                string r = "";
                foreach (var i in PlayerHudSpawnNtfShow(Player.Get(x.PlayerDisplay.ReferenceHub)))
                    r += i + "\n";
                return r;
            }),
            XCoordinate = 150,
            YCoordinate = 100
        };

        public static Hint ChaosSpawnHint = new Hint()
        {
            Id = "ChaosSpawnHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                string r = "";
                foreach (var i in PlayerHudSpawnChaosShow(Player.Get(x.PlayerDisplay.ReferenceHub)))
                    r += i + "\n";
                return r;
            }),
            XCoordinate = 0,
            YCoordinate = 100
        };

        public static Hint SpawnHint = new Hint()
        {
            Id = "SpawnHUD",
            AutoText = new TextUpdateHandler((x) =>
            {
                string r = "";
                foreach (var i in PlayerHudSpawnHintShow(Player.Get(x.PlayerDisplay.ReferenceHub)))
                    r += i + "\n";
                return r;
            }),
            XCoordinate = 0,
            YCoordinate = 190
        };

        public static Hint ImageHint = new Hint()
        {
            AutoText = ImageUpdater,
            YCoordinate = 1040,
            XCoordinate = 85,
            SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Slow,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Left
        };

        public static string ImageUpdater(AutoContentUpdateArg ev)
        {
            string outT = "";
            image.getFrame("C:\\Users\\kill\\AppData\\Roaming\\EXILED\\Plugins\\IcomMediaDisplay\\te\\0.png", ref outT);
            return outT;
        }

        public struct ScoreChange
        {
            public Player Player;
            public int Amount;
            public string Reason;
            public float Time;
        }

        public static List<ScoreChange> ScoreQueue = new List<ScoreChange>();

        public static void AddScoreChange(Player player, int amount, string reason)
        {
            ScoreQueue.Add(new ScoreChange { Player = player, Amount = amount, Reason = reason, Time = Time.time });
        }

        public static Hint ScoreHint = new Hint()
        {
            Id = "ScoreHint",
            AutoText = ScoreUpdater,
            YCoordinate = 700,
            XCoordinate = 0,
            SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Fast,
            Alignment = HintServiceMeow.Core.Enum.HintAlignment.Center
        };

        public static string ScoreUpdater(AutoContentUpdateArg ev)
        {
            if (ev.PlayerDisplay.ReferenceHub == null) return "";
            var p = Player.Get(ev.PlayerDisplay.ReferenceHub);
            if (p == null) return "";

            ScoreQueue.RemoveAll(x => Time.time - x.Time > 1f || x.Player == null);

            var mine = ScoreQueue.Where(x => x.Player == p).ToList();
            if (mine.Count == 0) return "";

            var latest = mine.Last();
            string color = latest.Amount > 0 ? "#00FF00" : "#FF4444";
            string sign = latest.Amount > 0 ? "+" : "";

            ScoreQueue.RemoveAll(x => x.Player == p);
            return $"<size=24><color={color}>{sign}{latest.Amount} 积分 ({latest.Reason})</color></size>";
        }

        public static int ntfWave = 0;
        public static int ChaosCount = 0;

        public static void AnnouncingNtfEntrance(AnnouncingNtfEntranceEventArgs ev) => ntfWave += 1;

        public static void AnnouncingChaosEntrance(AnnouncingChaosEntranceEventArgs ev) => ChaosCount += 1;

        public static int GetWaveCount(Player player)
        {
            try
            {
                if (player.IsNTF) return ntfWave;
                else return ChaosCount;
            }
            catch { return 0; }
        }

        public static Stopwatch WaveCalc = new Stopwatch();

        public static string[] PlayerHudLVShow(Player player)
        {
            if (player == null) return new string[] { "", "" };

            int spectatorCount = 0;
            short totalTick = ServerStatic.ServerTickrate;
            double currentTick = Math.Round(1f / Time.smoothDeltaTime);

            Player targetPlayer = player;
            if (player.Role is SpectatorRole spectatorRole && spectatorRole.SpectatedPlayer != null)
                targetPlayer = spectatorRole.SpectatedPlayer;

            if (PlayerStateManager.SpecList.ContainsKey(targetPlayer))
                spectatorCount = PlayerStateManager.SpecList[targetPlayer].Count;

            var roundStats = PlayerManager.GetOrCreateStats(targetPlayer);

            string upLine = BuildFirstLine(targetPlayer, player.Role is SpectatorRole);
            string downLine = BuildSecondLine(targetPlayer, roundStats, spectatorCount, currentTick, totalTick, player.Role is SpectatorRole);

            if (!targetPlayer.IsAlive && player.Role is SpectatorRole)
                return new string[] { upLine, downLine };

            return new string[] { upLine, downLine };
        }

        private static string BuildFirstLine(Player player, bool isSpec)
        {
            if (player == null) return "";

            string roleTeam = GetTeamName(player);
            string teamColor = GetTeamColor(player);

            var conduct = ConductManager.GetConduct(player);
            string conductName = ConductManager.ConductToName(conduct);
            string conductColor = ConductManager.ConductToColor(conduct);

            var phase = PhaseManager.GetPhase(player);
            string phaseName = PhaseManager.PhaseToName(phase);
            string phaseColor = PhaseManager.PhaseToColor(phase);
            string phaseProgress = PhaseManager.GetPhaseProgressString(player);

            string rankColor = "#FFFFFF";
            if (!string.IsNullOrEmpty(player.RankColor) && Misc.TryParseColor(player.RankColor, out var color))
                rankColor = color.ToHex();

            string rankName = string.IsNullOrEmpty(player.RankName) ? "无" : player.RankName;

            return $"<align=center><size=21>" +
                   $"<color=#FFFF00>{(isSpec ? "玩家:" : "欢迎回来:")} {player.Nickname}</color> | " +
                   $"<color={conductColor}>品行: {conductName}</color> | " +
                   $"<color={phaseColor}>阶段: {phaseProgress}</color> | " +
                   $"<color={teamColor}>阵营: {roleTeam}</color>" +
                   $"</size></align>";
        }

        private static string BuildSecondLine(Player player, PlayerManager.RoundStatistics stats, int spectatorCount, double currentTick, short totalTick, bool isSpec)
        {
            if (player == null || stats == null) return "";

            var todayDuration = ExperienceManager.GetTodayTimer(player);
            string durationStr = $"{todayDuration.Hours:D2}:{todayDuration.Minutes:D2}:{todayDuration.Seconds:D2}";

            int assistWaves = GetWaveCount(player);

            return $"<align=center><size=22>" +
                   $"<color=#FFD700>本场得分:{stats.Points}</color> | " +
                   $"<color=#00FF00>击杀:{stats.Kills}🔫</color> | " +
                   $"<color=#FF0000>死亡:{stats.Deaths}💀</color> | " +
                   (isSpec ? "" : $"<color=#FF00FF>总时长:{durationStr}</color> | ") +
                   ((player.LeadingTeam == LeadingTeam.FacilityForces || player.LeadingTeam == LeadingTeam.ChaosInsurgency) && !isSpec ? $"<color=#FFA500>支援:{assistWaves}波</color> | " : "") +
                   $"<color=#FFD700>TPS:{currentTick}/{totalTick}</color> | " +
                   $"<color=#87CEEB>观众:{spectatorCount}</color>" +
                   $"</size></align>";
        }

        private static string GetTeamName(Player player)
        {
            if (player == null || player.Role == null) return "未知";
            return player.Role.Team switch
            {
                Team.FoundationForces => "基金会",
                Team.ChaosInsurgency => "混沌",
                Team.Scientists => "基金会",
                Team.ClassD => "混沌",
                Team.OtherAlive => "教程人员",
                Team.SCPs => "SCP",
                _ => "死人"
            };
        }

        private static string GetTeamColor(Player player)
        {
            if (player == null || player.Role == null) return "white";
            return player.Role.Team switch
            {
                Team.FoundationForces => "#0000FF",
                Team.ChaosInsurgency => "#00AA00",
                Team.Scientists => "#0000FF",
                Team.OtherAlive => "#FF00FF",
                Team.ClassD => "#00AA00",
                Team.SCPs => "#FF0000",
                _ => "#FFFFFF"
            };
        }

        public static string GetGreetingWord()
        {
            var t = DateTime.Now;
            int h = t.Hour;
            if (h >= 6 && h <= 11) return "早上好";
            else if (h >= 11 && h <= 14) return "中午好";
            else if (h >= 15 && h <= 17) return "下午好";
            else if (h >= 18 && h <= 23) return "晚上好";
            else if (h >= 24 && h <= 5) return "夜深了";
            else return "";
        }

        public static string[] PlayerHudSpawnNtfShow(Player player)
        {
            string upLine = "";
            string downLine = "";
            if (player == null) return new string[] { };
            else if (player != null && player.IsAlive) return new string[] { };

            if (!(player.Role is SpectatorRole)) return new string[] { upLine, downLine };

            var NtfBig = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
            var NtfSmall = WaveManager.Waves.FirstOrDefault(x => x is NtfMiniWave) as NtfMiniWave;

            if (NtfBig != null)
            {
                double timeLeftBig = Math.Max(0, NtfBig.Timer.TimeLeft);
                var timeSpanBig = TimeSpan.FromSeconds(timeLeftBig);
                upLine = $"<align=left><size=25><color=#0000ffff>🚁九尾狐: {timeSpanBig:mm\\:ss}</color></size></align>";
            }
            if (NtfSmall != null)
            {
                double timeLeftSmall = Math.Max(0, NtfSmall.Timer.TimeLeft);
                var timeSpanSmall = TimeSpan.FromSeconds(timeLeftSmall);
                downLine = $"<align=left><size=25><color=#0000ffff>🚁九尾狐增援：{timeSpanSmall:mm\\:ss}</color></size></align>";
            }

            return new string[] { upLine, downLine };
        }

        public static string[] PlayerHudSpawnChaosShow(Player player)
        {
            string upLine = "";
            string downLine = "";
            if (player == null) return new string[] { };
            else if (player != null && player.IsAlive) return new string[] { };

            if (!(player.Role is SpectatorRole)) return new string[] { upLine, downLine };

            var ChaosBig = WaveManager.Waves.FirstOrDefault(x => x is ChaosSpawnWave) as ChaosSpawnWave;
            var ChaosSmall = WaveManager.Waves.FirstOrDefault(x => x is ChaosMiniWave) as ChaosMiniWave;

            if (ChaosBig != null)
            {
                double timeLeftBig = Math.Max(0, ChaosBig.Timer.TimeLeft);
                var timeSpanBig = TimeSpan.FromSeconds(timeLeftBig);
                upLine = $"<margin=8em><align=right><size=25><color=#008000ff>🚗混沌: {timeSpanBig:mm\\:ss}</color></size></align></margin>";
            }
            if (ChaosSmall != null)
            {
                double timeLeftSmall = Math.Max(0, ChaosSmall.Timer.TimeLeft);
                var timeSpanSmall = TimeSpan.FromSeconds(timeLeftSmall);
                downLine = $"<margin=8em><align=right><size=25><color=#008000ff>🚗混沌增援：{timeSpanSmall:mm\\:ss}</color></size></align></margin>";
            }

            return new string[] { upLine, downLine };
        }

        public static string[] PlayerHudSpawnHintShow(Player player)
        {
            string upLine = "";
            if (player == null) return new string[] { };
            else if (player != null && player.IsAlive) return new string[] { };

            if (!(player.Role is SpectatorRole)) return new string[] { upLine };

            var ChaosBig = WaveManager.Waves.FirstOrDefault(x => x is ChaosSpawnWave) as ChaosSpawnWave;
            var NtfBig = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
            var NtfSmall = WaveManager.Waves.FirstOrDefault(x => x is NtfMiniWave) as NtfMiniWave;
            var ChaosSmall = WaveManager.Waves.FirstOrDefault(x => x is ChaosMiniWave) as ChaosMiniWave;

            if (ChaosSmall.IsAnimationPlaying || NtfBig.IsAnimationPlaying || NtfSmall.IsAnimationPlaying || ChaosBig.IsAnimationPlaying)
            {
                if (!WaveCalc.IsRunning) WaveCalc.Restart();
            }
            else { WaveCalc.Stop(); }

            if (ChaosBig.IsAnimationPlaying)
            {
                var LeftTime = ChaosBig.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                upLine = $"<size=22><color=#ffffc0cb><b>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#008000ff>🚗混沌</color></size>";
            }
            if (NtfBig.IsAnimationPlaying)
            {
                var LeftTime = NtfBig.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                upLine = $"<size=22><color=#ffffc0cb>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#0000ffff>🚁九尾狐</color></size>";
            }
            if (ChaosSmall.IsAnimationPlaying)
            {
                var LeftTime = ChaosSmall.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                upLine = $"<size=22><color=#ffffc0cb>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#008000ff>🚗混沌增援</color></size>";
            }
            if (NtfSmall.IsAnimationPlaying)
            {
                var LeftTime = NtfSmall.AnimationDuration - WaveCalc.Elapsed.TotalSeconds;
                upLine = $"<size=22><color=#ffffc0cb>你将在{LeftTime.ToString("F0")}秒后复活为:</b></color><color=#0000ffff>🚁九尾狐增援</color></size>";
            }

            return new string[] { upLine };
        }
    }
}
