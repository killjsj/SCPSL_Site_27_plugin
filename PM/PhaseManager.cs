using Exiled.API.Features;
using System;

namespace Next_generationSite_27.UnionP
{
    class PhaseManager : BaseClass
    {
        public static MySQLConnect sql => Plugin.plugin.connect;

        public override void Init() { }
        public override void Delete() { }

        public enum GamePhase
        {
            FreshStart = 0,
            FirstGlimpse = 1,
            MinorAchievement = 2,
            SteadyProgress = 3,
            BattleHardened = 4,
            SeasonedRider = 5,
            HundredBattles = 6,
            RegionalForce = 7,
            RenownedFar = 8,
            SupremeRealm = 9
        }

        public static GamePhase GetPhase(Player player)
        {
            if (player == null) return GamePhase.FreshStart;
            double hours = GetTotalHours(player);
            return HoursToPhase(hours);
        }

        public static double GetTotalHours(Player player)
        {
            if (player == null) return 0;
            var user = sql.QueryUser(player.UserId);
            var td = user.total_duration ?? TimeSpan.Zero;
            return td.TotalHours;
        }

        public static GamePhase HoursToPhase(double hours)
        {
            if (hours < 5)  return GamePhase.FreshStart;
            if (hours < 10) return GamePhase.FirstGlimpse;
            if (hours < 15) return GamePhase.MinorAchievement;
            if (hours < 20) return GamePhase.SteadyProgress;
            if (hours < 25) return GamePhase.BattleHardened;
            if (hours < 30) return GamePhase.SeasonedRider;
            if (hours < 35) return GamePhase.HundredBattles;
            if (hours < 45) return GamePhase.RegionalForce;
            if (hours < 55) return GamePhase.RenownedFar;
            return GamePhase.SupremeRealm;
        }

        public static string PhaseToName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.FreshStart => "初入茅庐",
                GamePhase.FirstGlimpse => "渐窥门径",
                GamePhase.MinorAchievement => "小有成就",
                GamePhase.SteadyProgress => "稳步前行",
                GamePhase.BattleHardened => "久经沙场",
                GamePhase.SeasonedRider => "驰骋多时",
                GamePhase.HundredBattles => "身经百战",
                GamePhase.RegionalForce => "纵横一方",
                GamePhase.RenownedFar => "威名远扬",
                GamePhase.SupremeRealm => "登峰造极",
                _ => "?"
            };
        }

        public static string PhaseToColor(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.FreshStart => "#808080",
                GamePhase.FirstGlimpse => "#FFFFFF",
                GamePhase.MinorAchievement => "#00FF00",
                GamePhase.SteadyProgress => "#00FFFF",
                GamePhase.BattleHardened => "#0099FF",
                GamePhase.SeasonedRider => "#FFAA00",
                GamePhase.HundredBattles => "#FF6600",
                GamePhase.RegionalForce => "#FF00FF",
                GamePhase.RenownedFar => "#FFD700",
                GamePhase.SupremeRealm => "#FF004D",
                _ => "#FFFFFF"
            };
        }

        public static string GetPhaseProgressString(Player player)
        {
            double hours = GetTotalHours(player);
            var phase = GetPhase(player);
            if (phase == GamePhase.SupremeRealm)
                return $"[{PhaseToName(phase)}]";

            double stageStart = phase switch
            {
                GamePhase.FreshStart => 0, GamePhase.FirstGlimpse => 5,
                GamePhase.MinorAchievement => 10, GamePhase.SteadyProgress => 15,
                GamePhase.BattleHardened => 20, GamePhase.SeasonedRider => 25,
                GamePhase.HundredBattles => 30, GamePhase.RegionalForce => 35,
                GamePhase.RenownedFar => 45, _ => 0
            };
            double stageMax = phase switch
            {
                GamePhase.FreshStart => 5, GamePhase.FirstGlimpse => 10,
                GamePhase.MinorAchievement => 15, GamePhase.SteadyProgress => 20,
                GamePhase.BattleHardened => 25, GamePhase.SeasonedRider => 30,
                GamePhase.HundredBattles => 35, GamePhase.RegionalForce => 45,
                GamePhase.RenownedFar => 55, _ => 0
            };

            return $"[{PhaseToName(phase)} 剩余{stageMax - hours:F1}小时晋级]";
        }
    }
}
