using System;
using System.Collections.Generic;

namespace UnionApi
{
    public enum ExpTier
    {
        Small,
        Medium,
        Large,
        Pot,
        Shao,
        Eat,
        EatPlus,
        Robot,
    }

    public enum AddExpReason
    {
        Custom,
        DayLogin,
        PeopleKillPeoPle,
        ScpKillPeoPle,
        KillZombie,
        killScp,
        DDSCIEscaped,
        GuardEscaped,
        CuffedPeopleEscaped,
        RoundEnd,
        ScpWin,
        HumanWin,
        RaAdded,
        Scp079Gener
    }

    public enum ConductTier
    {
        Outstanding = 0,
        Acceptable = 1,
        Ordinary = 2,
        Lax = 3,
        Negative = 4,
        Worst = 5
    }

    public class RoundStatistics
    {
        public int Kills { get; set; } = 0;
        public int Escapes { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int Points { get; set; } = 0;
        public int AssistWaves { get; set; } = 0;
    }

    public delegate void OnExpUpHandler(Exiled.API.Features.Player player, int newExp);
    public delegate void OnLevelUpHandler(Exiled.API.Features.Player player, ExpTier newLevel);
    public delegate void OnPointChangedHandler(Exiled.API.Features.Player player, int newPoints);
}
