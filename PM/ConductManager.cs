using Exiled.API.Features;
using System;
using System.Collections.Generic;

namespace Next_generationSite_27.UnionP
{
    class ConductManager : BaseClass
    {
        public static MySQLConnect sql => Plugin.plugin.connect;
        public static Dictionary<string, int> violationsCache = new Dictionary<string, int>();

        public override void Init() { }
        public override void Delete() { violationsCache.Clear(); }

        public enum ConductTier
        {
            Outstanding = 0,
            Acceptable = 1,
            Ordinary = 2,
            Lax = 3,
            Negative = 4,
            Worst = 5
        }

        public static ConductTier GetConduct(Player player)
        {
            if (player == null) return ConductTier.Outstanding;
            int v = GetViolationCount(player);
            return ViolationsToTier(v);
        }

        public static ConductTier ViolationsToTier(int violations)
        {
            if (violations <= 0) return ConductTier.Outstanding;
            if (violations == 1) return ConductTier.Acceptable;
            if (violations == 2) return ConductTier.Ordinary;
            if (violations == 3) return ConductTier.Lax;
            if (violations == 4) return ConductTier.Negative;
            return ConductTier.Worst;
        }

        public static int GetViolationCount(Player player)
        {
            if (player == null) return 0;
            var uid = player.UserId;
            if (violationsCache.TryGetValue(uid, out int cached))
                return cached;
            int count = sql.CountUserViolations(uid);
            violationsCache[uid] = count;
            return count;
        }

        public static string ConductToName(ConductTier tier)
        {
            return tier switch
            {
                ConductTier.Outstanding => "出众",
                ConductTier.Acceptable => "尚可",
                ConductTier.Ordinary => "寻常",
                ConductTier.Lax => "散漫",
                ConductTier.Negative => "消极",
                ConductTier.Worst => "恶劣",
                _ => "?"
            };
        }

        public static string ConductToColor(ConductTier tier)
        {
            return tier switch
            {
                ConductTier.Outstanding => "#00FF00",
                ConductTier.Acceptable => "#66FF66",
                ConductTier.Ordinary => "#FFFF00",
                ConductTier.Lax => "#FFAA00",
                ConductTier.Negative => "#FF6600",
                ConductTier.Worst => "#FF0000",
                _ => "#FFFFFF"
            };
        }

        public static string GetConductPhaseString(int violations)
        {
            var tier = ViolationsToTier(violations);
            if (tier == ConductTier.Worst)
                return $"已达上限: {ConductToName(tier)}";

            int nextThreshold = (int)tier + 1;
            int remaining = nextThreshold - violations;
            return $"{tier + 1}/{6}阶段 距下一级还需 {remaining} 违规";
        }
    }
}
