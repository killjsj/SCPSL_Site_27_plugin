using Exiled.API.Features;
using System;

namespace UnionApi
{
    public static class PlayerAdminApi
    {
        private static Action<Player, string> _setBadge;
        private static Action<Player, string> _setDisplayName;
        private static Action<Player, string> _setAdminGroup;
        private static Action<Player> _clearBadge;

        public static void Register(
            Action<Player, string> setBadge,
            Action<Player, string> setDisplayName,
            Action<Player, string> setAdminGroup,
            Action<Player> clearBadge)
        {
            _setBadge = setBadge;
            _setDisplayName = setDisplayName;
            _setAdminGroup = setAdminGroup;
            _clearBadge = clearBadge;
        }

        public static void SetBadge(Player player, string badgeText, string colorCSV = "white")
        {
            if (_setBadge == null) throw new InvalidOperationException("UnionApi not initialized. Is UnionPlugin loaded?");
            _setBadge(player, badgeText);
        }

        public static void SetDisplayName(Player player, string displayName)
        {
            if (_setDisplayName == null) throw new InvalidOperationException("UnionApi not initialized.");
            _setDisplayName(player, displayName);
        }

        public static void SetAdminGroup(Player player, string groupName)
        {
            if (_setAdminGroup == null) throw new InvalidOperationException("UnionApi not initialized.");
            _setAdminGroup(player, groupName);
        }

        public static void ClearBadge(Player player)
        {
            if (_clearBadge == null) throw new InvalidOperationException("UnionApi not initialized.");
            _clearBadge(player);
        }
    }
}
