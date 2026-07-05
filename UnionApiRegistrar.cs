using UnionApi;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    static class UnionApiRegistrar
    {
        public static void Initialize()
        {
            ExperienceApi.Register(
                getExp: ExperienceManager.GetExperience,
                setExp: ExperienceManager.SetExp,
                addExp: (p, e, ig, r, s) => ExperienceManager.AddExp(p, e, ig, (ExperienceManager.AddExpReason)(int)r, s),
                getLevel: p => (ExpTier)(int)ExperienceManager.GetLevel(p),
                getPoint: ExperienceManager.GetPoint,
                setPoint: ExperienceManager.SetPoint,
                addPoint: ExperienceManager.AddPoint,
                getUid: ExperienceManager.GetUid,
                expToNextLevel: t => ExperienceManager.ExpToNextLevel((ExperienceManager.ExpTier)(int)t),
                levelToName: t => ExperienceManager.LevelToName((ExperienceManager.ExpTier)(int)t),
                getOrCreateStats: p => { var s = PlayerManager.GetOrCreateStats(p); return s == null ? null : new RoundStatistics { Kills = s.Kills, Escapes = s.Escapes, Deaths = s.Deaths, Points = s.Points, AssistWaves = s.AssistWaves }; }
            );

            PlayerAdminApi.Register(
                setBadge: (p, text) => { if (p != null) p.RankName = text; },
                setDisplayName: (p, name) => { if (p != null) p.DisplayNickname = name; },
                setAdminGroup: (p, groupName) =>
                {
                    if (p == null) return;
                    var g = ServerStatic.PermissionsHandler.GetGroup(groupName);
                    if (g != null) p.Group = g.Clone();
                },
                clearBadge: p => { if (p != null) p.RankName = ""; }
            );
        }
    }
}
