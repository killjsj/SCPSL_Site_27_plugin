using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using Next_generationSite_27.UnionP.testing;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;
using Random = UnityEngine.Random;

namespace Next_generationSite_27.UnionP
{
    class PlayerStateManager : BaseClass
    {
        public override void Init()
        {
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.EnteringPocketDimension += EnteringPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension += EscapingPocketDimension;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension += FailingEscapePocketDimension;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.EnteringPocketDimension -= EnteringPocketDimension;
            Exiled.Events.Handlers.Player.EscapingPocketDimension -= EscapingPocketDimension;
            Exiled.Events.Handlers.Player.FailingEscapePocketDimension -= FailingEscapePocketDimension;
        }

        public static Dictionary<string, (string player_name, string badge, List<string> color, DateTime expiration_date, bool is_permanent, string notes)> badges = new();
        public static Dictionary<Player, List<Player>> SpecList = new();
        public static Dictionary<Player, CoroutineHandle> rainbowC = new();
        public static Dictionary<Player, (Stopwatch stand, double lastTime, Vector3 lastPos)> ScpStandHP = new();

        public static List<string> colors = new List<string>() { "red", "green", "yellow", "cyan", "magenta" };

        public static Dictionary<Player, Player> Scp106CatchPlayers = new();

        public static void OnInteractingDoor(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
        {
            if (ev.Door.IsMoving) return;
            if (ev.Door.IsNonInteractable) return;
            if (ev.Door.IsLocked) return;

            foreach (var item in ev.Player.Items)
            {
                if (item is Keycard k)
                {
                    if (k.Base is IDoorPermissionProvider doorPermissionProvider2)
                    {
                        if (ev.Door.Base.CheckPermissions(doorPermissionProvider2, out var _))
                            ev.IsAllowed = true;
                    }
                }
            }
        }

        public static void EnteringPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (ev.Player != null && ev.Scp106 != null)
                Scp106CatchPlayers[ev.Player] = ev.Scp106;
        }

        public static void EscapingPocketDimension(EscapingPocketDimensionEventArgs ev)
        {
            if (ev.Player != null && Scp106CatchPlayers.ContainsKey(ev.Player))
                Scp106CatchPlayers.Remove(ev.Player);
        }

        public static void FailingEscapePocketDimension(FailingEscapePocketDimensionEventArgs ev)
        {
            if (ev.Player != null && Scp106CatchPlayers.ContainsKey(ev.Player))
            {
                ExperienceManager.AddExp(Scp106CatchPlayers[ev.Player], 5, true, ExperienceManager.AddExpReason.ScpKillPeoPle);
                Scp106CatchPlayers.Remove(ev.Player);
            }
        }

        public static void HandleBadgeSync(Player player, ReferenceHub hub)
        {
            if (!badges.TryGetValue(player.UserId, out var badgeData)) return;
            if (FlightFailed.PlayerToBadge.ContainsKey(player.UserId)) return;
            if (hub.serverRoles.Network_myText == null)
                player.RankName = badgeData.badge;

            if (!hub.serverRoles.Network_myText.Contains(badgeData.badge))
                player.RankName = badgeData.badge;

            if (badgeData.color.Contains("rainbow"))
            {
                if (!rainbowC.ContainsKey(player))
                    rainbowC[player] = Timing.RunCoroutine(RainbowTimeCoroutine(player, colors));
                else if (!rainbowC[player].IsRunning)
                    rainbowC[player] = Timing.RunCoroutine(RainbowTimeCoroutine(player, colors));
            }
            else
            {
                rainbowC[player] = Timing.RunCoroutine(RainbowTimeCoroutine(player, badgeData.color));
            }
        }

        public static void HandleSpectatorTracking(Player player, SpectatorRole spectatorRole)
        {
            if (player == null || !player.IsConnected) return;

            var target = spectatorRole?.SpectatedPlayer;

            foreach (var kv in SpecList.Keys.ToList())
            {
                if (kv == null || !kv.IsConnected)
                    SpecList.Remove(kv);
            }

            var keysToUpdate = new List<Player>();
            foreach (var entry in SpecList.ToList())
            {
                if (entry.Value.Contains(player))
                    keysToUpdate.Add(entry.Key);
            }

            foreach (var key in keysToUpdate)
            {
                SpecList[key].Remove(player);
                if (SpecList[key].Count == 0)
                    SpecList.Remove(key);
            }

            if (target == null || !target.IsConnected) return;

            if (!SpecList.ContainsKey(target))
                SpecList[target] = new List<Player>();

            if (!SpecList[target].Contains(player))
                SpecList[target].Add(player);
        }

        public static void HandleSpectatorTracking(Player player, OverwatchRole overwatch)
        {
            HandleSpectatorTracking(player, overwatch as SpectatorRole);
        }

        public static void RemoveFromSpectatorLists(Player player)
        {
            var keysToUpdate = new List<Player>();
            foreach (var entry in SpecList.ToList())
            {
                if (entry.Value.Contains(player))
                    keysToUpdate.Add(entry.Key);
            }
            foreach (var key in keysToUpdate)
            {
                SpecList[key].Remove(player);
                if (SpecList[key].Count == 0)
                    SpecList.Remove(key);
            }
        }

        public static void HandleScpStandHeal(Player player)
        {
            if (!(player.Role?.Base is IFpcRole fpcRole) || !player.IsScp) return;

            double interval = 1.0;

            if (!ScpStandHP.TryGetValue(player, out var data))
                ScpStandHP[player] = (Stopwatch.StartNew(), 0.0, player.Position);

            var (stopwatch, lastHealTime, lastPos) = ScpStandHP[player];
            double elapsed = stopwatch.Elapsed.TotalSeconds;

            if (Vector3.Distance(player.Position, lastPos) < 0.5f)
            {
                if (elapsed >= Plugin.Instance.Config.ScpStandAddHPTime)
                {
                    if (elapsed - lastHealTime >= interval)
                    {
                        player.Heal(player.Role.Type.IsScp() ? Plugin.Instance.Config.ScpStandAddHPCount * 2 : Plugin.Instance.Config.ScpStandAddHPCount * 2);
                        ScpStandHP[player] = (stopwatch, elapsed, player.Position);
                    }
                }
            }
            else
            {
                stopwatch.Restart();
                ScpStandHP[player] = (stopwatch, 0.0, player.Position);
            }
        }

        public static void UpdatePlayerDisplayName(Player player)
        {
            string level = ExperienceManager.LevelToName(ExperienceManager.GetLevel(player));
            string expectedName = $"{level} | {player.Nickname}";
            if (!player.DisplayNickname.Contains(expectedName))
                player.DisplayNickname = expectedName;
        }

        public static IEnumerator<float> RainbowTimeCoroutine(Player player, List<string> colorsList)
        {
            if (player == null) yield break;
            while (player != null)
            {
                foreach (var color in colorsList)
                {
                    if (player == null) break;
                    player.RankColor = color;
                    yield return Timing.WaitForSeconds(1.5f);
                }
                if (player == null) break;
                yield return Timing.WaitForSeconds(1.5f);
            }
        }

        public static IEnumerator<float> RainbowTimeCoroutine(Player player, string singleColor)
        {
            if (player == null) yield break;
            player.RankColor = singleColor;
            yield break;
        }
    }
}
