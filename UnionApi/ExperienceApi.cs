using Exiled.API.Features;
using System;

namespace UnionApi
{
    public static class ExperienceApi
    {
        private static Func<Player, int> _getExp;
        private static Action<Player, int> _setExp;
        private static Action<Player, int, bool, AddExpReason, string> _addExp;
        private static Func<Player, ExpTier> _getLevel;
        private static Func<Player, int> _getPoint;
        private static Action<Player, int> _setPoint;
        private static Action<Player, int> _addPoint;
        private static Func<Player, int> _getUid;
        private static Func<ExpTier, int> _expToNextLevel;
        private static Func<ExpTier, string> _levelToName;
        private static Func<Player, RoundStatistics> _getOrCreateStats;

        public static event OnExpUpHandler OnExpUp;
        public static event OnLevelUpHandler OnLevelUp;
        public static event OnPointChangedHandler OnPointChanged;

        public static void Register(
            Func<Player, int> getExp,
            Action<Player, int> setExp,
            Action<Player, int, bool, AddExpReason, string> addExp,
            Func<Player, ExpTier> getLevel,
            Func<Player, int> getPoint,
            Action<Player, int> setPoint,
            Action<Player, int> addPoint,
            Func<Player, int> getUid,
            Func<ExpTier, int> expToNextLevel,
            Func<ExpTier, string> levelToName,
            Func<Player, RoundStatistics> getOrCreateStats)
        {
            _getExp = getExp;
            _setExp = setExp;
            _addExp = addExp;
            _getLevel = getLevel;
            _getPoint = getPoint;
            _setPoint = setPoint;
            _addPoint = addPoint;
            _getUid = getUid;
            _expToNextLevel = expToNextLevel;
            _levelToName = levelToName;
            _getOrCreateStats = getOrCreateStats;
        }

        public static int GetExperience(Player player)
        {
            if (_getExp == null) throw new InvalidOperationException("UnionApi not initialized. Is UnionPlugin loaded?");
            return _getExp(player);
        }

        public static void SetExp(Player player, int exp)
        {
            if (_setExp == null) throw new InvalidOperationException("UnionApi not initialized.");
            _setExp(player, exp);
        }

        public static void AddExp(Player player, int exp, bool ignoreMultiplier = false, AddExpReason reason = AddExpReason.Custom, string customReason = "")
        {
            if (_addExp == null) throw new InvalidOperationException("UnionApi not initialized.");
            _addExp(player, exp, ignoreMultiplier, reason, customReason);
        }

        public static ExpTier GetLevel(Player player)
        {
            if (_getLevel == null) throw new InvalidOperationException("UnionApi not initialized.");
            return _getLevel(player);
        }

        public static int GetPoint(Player player)
        {
            if (_getPoint == null) throw new InvalidOperationException("UnionApi not initialized.");
            return _getPoint(player);
        }

        public static void SetPoint(Player player, int point)
        {
            if (_setPoint == null) throw new InvalidOperationException("UnionApi not initialized.");
            _setPoint(player, point);
        }

        public static void AddPoint(Player player, int point)
        {
            if (_addPoint == null) throw new InvalidOperationException("UnionApi not initialized.");
            _addPoint(player, point);
        }

        public static int GetUid(Player player)
        {
            if (_getUid == null) throw new InvalidOperationException("UnionApi not initialized.");
            return _getUid(player);
        }

        public static int ExpToNextLevel(ExpTier currentLevel)
        {
            if (_expToNextLevel == null) throw new InvalidOperationException("UnionApi not initialized.");
            return _expToNextLevel(currentLevel);
        }

        public static string LevelToName(ExpTier currentLevel)
        {
            if (_levelToName == null) throw new InvalidOperationException("UnionApi not initialized.");
            return _levelToName(currentLevel);
        }

        public static RoundStatistics GetOrCreateStats(Player player)
        {
            if (_getOrCreateStats == null) throw new InvalidOperationException("UnionApi not initialized.");
            return _getOrCreateStats(player);
        }

        public static void InvokeOnExpUp(Player player, int newExp)
        {
            OnExpUp?.Invoke(player, newExp);
        }

        public static void InvokeOnLevelUp(Player player, ExpTier newLevel)
        {
            OnLevelUp?.Invoke(player, newLevel);
        }

        public static void InvokeOnPointChanged(Player player, int newPoints)
        {
            OnPointChanged?.Invoke(player, newPoints);
        }
    }
}
