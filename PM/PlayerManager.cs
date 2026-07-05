using AutoEvent;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Utilities;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using ProjectMER.Features;
using Respawning.Waves;
using Scp914;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;
using Random = UnityEngine.Random;
using Scp079Role = Exiled.API.Features.Roles.Scp079Role;
using Scp096Role = Exiled.API.Features.Roles.Scp096Role;
using SpectatorRole = Exiled.API.Features.Roles.SpectatorRole;

namespace Next_generationSite_27.UnionP
{
    class PlayerManager : BaseClass
    {
        public static MySQLConnect sql => Plugin.plugin.connect;

        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            Exiled.Events.Handlers.Scp079.GainingExperience += GainingExperience;
            Exiled.Events.Handlers.Warhead.Starting += Starting;
            Exiled.Events.Handlers.Warhead.Stopping += Stopping;
            Exiled.Events.Handlers.Player.Verified += Verified;
            Exiled.Events.Handlers.Player.PreAuthenticating += PreAuthenticating;
            Exiled.Events.Handlers.Server.RestartingRound += RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers += WaitingForPlayers;
            Exiled.Events.Handlers.Player.Left += Left;
            Exiled.Events.Handlers.Player.Dying += Dying;
            Exiled.Events.Handlers.Player.Hurting += Hurting;
            Exiled.Events.Handlers.Player.Escaped += Escaping;
            Exiled.Events.Handlers.Server.RoundEnded += RoundEnded;
            Exiled.Events.Handlers.Map.GeneratorActivating += GeneratorActivating;

            rec = Timing.RunCoroutine(RefreshAllPlayers(), segment: Segment.FixedUpdate);
        }

        public static CoroutineHandle rec;

        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            Exiled.Events.Handlers.Scp079.GainingExperience -= GainingExperience;
            Exiled.Events.Handlers.Warhead.Starting -= Starting;
            Exiled.Events.Handlers.Warhead.Stopping -= Stopping;
            Exiled.Events.Handlers.Player.Verified -= Verified;
            Exiled.Events.Handlers.Player.PreAuthenticating -= PreAuthenticating;
            Exiled.Events.Handlers.Server.RestartingRound -= RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= WaitingForPlayers;
            Exiled.Events.Handlers.Player.Left -= Left;
            Exiled.Events.Handlers.Player.Dying -= Dying;
            Exiled.Events.Handlers.Player.Hurting -= Hurting;
            Exiled.Events.Handlers.Player.Escaped -= Escaping;
            Exiled.Events.Handlers.Server.RoundEnded -= RoundEnded;
            Exiled.Events.Handlers.Map.GeneratorActivating -= GeneratorActivating;

            Timing.KillCoroutines(rec);
        }

        public static void Hurting(HurtingEventArgs ev)
        {
            if (ev.DamageHandler.Type == DamageType.Scp207 || ev.DamageHandler.Type == DamageType.Poison)
                ev.Amount *= 0.5f;
        }

        public static void GeneratorActivating(GeneratorActivatingEventArgs ev)
        {
            if (ev.Generator.LastActivator != null)
            {
                foreach (var item in Player.Enumerable.Where(x => x.Role.Team == ev.Generator.LastActivator.Role.Team))
                    ExperienceManager.AddExp(item, 15, true, ExperienceManager.AddExpReason.Scp079Gener);
            }
        }

        public static void RoundEnded(RoundEndedEventArgs ev)
        {
            foreach (var item in Player.Enumerable)
            {
                var stats = GetOrCreateStats(item);
                if (stats != null)
                {
                    if (item.IsAlive)
                        stats.Points++;
                    ExperienceManager.AddPoint(item, stats.Points);
                }

                ExperienceManager.AddExp(item, 5, reason: ExperienceManager.AddExpReason.RoundEnd);
                if (ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.Anomalies)
                {
                    if (item.Role.Type.IsScp())
                        ExperienceManager.AddExp(item, 10, reason: ExperienceManager.AddExpReason.ScpWin);
                }
                if (ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.FacilityForces ||
                    ev.LeadingTeam == Exiled.API.Enums.LeadingTeam.ChaosInsurgency)
                {
                    if (item.Role.Type.IsHuman())
                        ExperienceManager.AddExp(item, 10, reason: ExperienceManager.AddExpReason.HumanWin);
                }
            }
        }

        public static void Escaping(EscapedEventArgs ev)
        {
            var escapeStats = GetOrCreateStats(ev.Player);
            if (escapeStats != null)
            {
                escapeStats.Escapes++;
                escapeStats.Points++;
                PlayerHUDManager.AddScoreChange(ev.Player, 1, "逃离");
            }

            ExperienceManager.AddExp(ev.Player, 25, reason: ExperienceManager.AddExpReason.DDSCIEscaped);
            if (ev.Player.IsCuffed)
                ExperienceManager.AddExp(ev.Player.Cuffer, 15, false, ExperienceManager.AddExpReason.CuffedPeopleEscaped);
        }

        public static void GainingExperience(GainingExperienceEventArgs ev)
        {
            if (ev.Player != null)
            {
                if (ev.GainType == Scp079HudTranslation.ExpGainTerminationDirect)
                    ExperienceManager.AddExp(ev.Player, 20, true, ExperienceManager.AddExpReason.ScpKillPeoPle);
                if (ev.GainType == Scp079HudTranslation.ExpGainTerminationAssist)
                    ExperienceManager.AddExp(ev.Player, 5, true, ExperienceManager.AddExpReason.ScpKillPeoPle);
            }
        }

        public static void Dying(DyingEventArgs ev)
        {
            var diedStats = GetOrCreateStats(ev.Player);
            if (diedStats != null)
            {
                diedStats.Deaths++;
                diedStats.Points--;
                PlayerHUDManager.AddScoreChange(ev.Player, -1, "死亡");
            }

            if (ev.Attacker != null)
            {
                var attackerStats = GetOrCreateStats(ev.Attacker);
                if (attackerStats != null)
                {
                    attackerStats.Kills++;
                    attackerStats.Points++;
                    PlayerHUDManager.AddScoreChange(ev.Attacker, 1, "击杀");
                }

                bool isHandGunKill = (ev.DamageHandler.Base is FirearmDamageHandler f && (f.WeaponType == ItemType.GunCOM15 || f.WeaponType == ItemType.GunCOM18));
                bool x3orJailBirdKill096 = (ev.Player.Role.Type == RoleTypeId.Scp096 && (ev.DamageHandler.Base is JailbirdDamageHandler || ev.DamageHandler.Base is DisruptorDamageHandler));
                bool GunKilledRage096 = (ev.Player.Role is Scp096Role s && s.RageState == PlayerRoles.PlayableScps.Scp096.Scp096RageState.Enraged && (ev.DamageHandler.Base is FirearmDamageHandler));
                if (!ev.Attacker.IsScp)
                {
                    if (!ev.Player.Role.Type.IsScp())
                    {
                        int exp = 10;
                        exp = exp + 10 * ev.Player.GetEffect(EffectType.Scp207).Intensity
                            + 20 * ev.Player.GetEffect(EffectType.Scp1344).Intensity
                            + (isHandGunKill ? 5 : 0)
                            + 10 * ev.Player.GetEffect(EffectType.Scp1853).Intensity;
                        ExperienceManager.AddExp(ev.Attacker, exp, true, ExperienceManager.AddExpReason.PeopleKillPeoPle);
                    }
                    else
                    {
                        int exp = (isHandGunKill ? 5 : 0) + (x3orJailBirdKill096 ? 50 : 0) + (GunKilledRage096 ? 100 : 0);
                        if (ev.Player.Role.Type == RoleTypeId.Scp0492)
                            ExperienceManager.AddExp(ev.Attacker, exp + 20, true, ExperienceManager.AddExpReason.KillZombie);
                        else
                            ExperienceManager.AddExp(ev.Attacker, exp + 50, false, ExperienceManager.AddExpReason.killScp);
                    }
                }
                else
                {
                    int exp = 10;
                    exp = exp + 10 * ev.Player.GetEffect(EffectType.Scp207).Intensity
                        + 20 * ev.Player.GetEffect(EffectType.Scp1344).Intensity
                        + 10 * ev.Player.GetEffect(EffectType.Scp1853).Intensity;
                    ExperienceManager.AddExp(ev.Attacker, exp, true, ExperienceManager.AddExpReason.ScpKillPeoPle);
                }
            }
        }

        public static void WaitingForPlayers()
        {
            Scp330Interobject.MaxAmountPerLife = 4;
            if (Plugin.Instance.Config.RoundEndFF)
            {
                ServerConsole.FriendlyFire = false;
                ServerConfigSynchronizer.RefreshAllConfigs();
            }
            GameObject.Find("StartRound").transform.localScale = Vector3.zero;
            EventSystem.current.SetSelectedGameObject(null);
            PrefabManager.RegisterPrefabs();
        }

        public static void Left(LeftEventArgs ev)
        {
            ScpVoiceManager.CleanupPlayer(ev.Player);
            var sessionTime = ExperienceManager.GetTodayTimer(ev.Player);
            var user = sql.QueryUser(ev.Player.UserId);
            var newTotal = (user.total_duration ?? TimeSpan.Zero) + sessionTime;
            sql.Update(ev.Player.UserId, name: ev.Player.Nickname, today_duration: sessionTime, total_duration: newTotal);
        }

        public static void RestartingRound()
        {
            ExperienceManager.expCache.Clear();
            ExperienceManager.levelCache.Clear();
            foreach (var item in ExperienceManager.TodayTimer)
            {
                item.Value.Stop();
                var sessionTime = ExperienceManager.GetTodayTimer(item.Key);
                var user = sql.QueryUser(item.Key.UserId);
                var newTotal = (user.total_duration ?? TimeSpan.Zero) + sessionTime;
                sql.Update(item.Key.UserId, name: item.Key.Nickname, today_duration: sessionTime, total_duration: newTotal);
            }
            ExperienceManager.TodayTimeCache.Clear();
            foreach (var item in ExperienceManager.PointCache)
                sql.Update(item.Key.UserId, point: item.Value);
            ExperienceManager.PointCache.Clear();
            ExperienceManager.TodayTimer.Clear();
            PlayerStateManager.SpecList.Clear();
            RoundStats.Clear();
        }

        public static void Stopping(StoppingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
                Timing.CallDelayed(0.2f, () => { });
        }

        public static void Starting(StartingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level) { }
        }

        public static IEnumerator<float> ClearPower(Scp079Role sr)
        {
            float wt = 18f;
            float i = 0f;
            while (sr != null)
            {
                i += 0.2f;
                if (i >= wt) yield break;
                sr.Energy = 0;
                yield return Timing.WaitForSeconds(0.2f);
            }
        }

        public static List<CandyKindID> CandyList = new List<CandyKindID>()
        {
            CandyKindID.Rainbow, CandyKindID.Rainbow, CandyKindID.Rainbow, CandyKindID.Rainbow, CandyKindID.Rainbow, CandyKindID.Rainbow,
            CandyKindID.Yellow, CandyKindID.Yellow, CandyKindID.Yellow, CandyKindID.Yellow, CandyKindID.Yellow, CandyKindID.Yellow,
            CandyKindID.Purple, CandyKindID.Purple, CandyKindID.Purple, CandyKindID.Purple,
            CandyKindID.Red, CandyKindID.Red, CandyKindID.Red,
            CandyKindID.Green, CandyKindID.Green, CandyKindID.Green,
            CandyKindID.Blue, CandyKindID.Blue, CandyKindID.Blue,
            CandyKindID.Pink, CandyKindID.Pink,
        };

        public static void ChangingRole(ChangingRoleEventArgs ev)
        {
            ScpVoiceManager.CleanupPlayer(ev.Player);
            PlayerStateManager.RemoveFromSpectatorLists(ev.Player);

            var h = HSM_hintServ.GetPlayerHUD(ev.Player) as HSM_hintServ;
            if (ev.NewRole == RoleTypeId.Spectator || ev.NewRole == RoleTypeId.Overwatch)
            {
                h.hud.AddHint(PlayerHUDManager.ChaosSpawnHint);
                h.hud.AddHint(PlayerHUDManager.NtfSpawnHint);
                h.hud.AddHint(PlayerHUDManager.SpawnHint);
            }
            else
            {
                h.hud.RemoveHint(PlayerHUDManager.SpawnHint);
                h.hud.RemoveHint(PlayerHUDManager.NtfSpawnHint);
                h.hud.RemoveHint(PlayerHUDManager.ChaosSpawnHint);
            }
            if (h != null)
            {
                if (h.hud.HasHint("Scp914KnobChanged"))
                    h.hud.RemoveHint(PlayerHUDManager.Scp914Hint);
            }

            var menuItems = Plugin.MenuCache?
                .Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk])
                ?.ToList();
            if (menuItems != null && menuItems.Count > 0)
            {
                if (Plugin.GetPlayerRegistered(ev.Player).Any(x => menuItems.Contains(x)))
                    Plugin.Unregister(ev.Player, menuItems);
            }
            else { Log.Debug($"[ChangingRole] 未找到 SettingIds[{Features.ScpTalk}] 对应菜单项。"); }

            if (ev.Reason != SpawnReason.RoundStart) return;

            Timing.CallDelayed(0.4f, () =>
            {
                try
                {
                    if (ev.Player == null) return;
                    foreach (var i in Enum.GetValues(typeof(AmmoType)))
                    {
                        int newAmmo = (int)Math.Floor(ev.Player.GetAmmo((AmmoType)i) * 1.5f);
                        if (newAmmo > ushort.MaxValue) newAmmo = ushort.MaxValue;
                        ev.Player.SetAmmo((AmmoType)i, (ushort)newAmmo);
                    }
                    if (ev.Player.IsScp)
                    {
                        if (menuItems != null)
                        {
                            if (!Plugin.GetPlayerRegistered(ev.Player).Any(a => a.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk]))
                                Plugin.Register(ev.Player, menuItems);
                        }
                    }

                    if (Plugin.Instance.Config.Level)
                    {
                        if (AutoEvent.API.EventManager.CurrentEvent != null) return;

                        CandyList.ShuffleList();
                        var player = ev.Player;

                        if (player.Role.Type == RoleTypeId.ClassD || player.Role.Type == RoleTypeId.Scientist)
                        {
                            if (Random.Range(0, 4) <= 1) player.AddItem(ItemType.KeycardScientist);
                            else if (Random.Range(0, 5) <= 1) player.AddItem(ItemType.KeycardZoneManager);
                            else player.AddItem(ItemType.KeycardJanitor);
                            player.AddItem(ItemType.Medkit);
                        }
                        switch (ExperienceManager.GetLevel(player))
                        {
                            case ExperienceManager.ExpTier.Small:
                                if (Random.Range(0, 100) <= 10) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                break;
                            case ExperienceManager.ExpTier.Medium:
                                if (Random.Range(0, 100) <= 15) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 10) player.AddItem(ItemType.Flashlight);
                                break;
                            case ExperienceManager.ExpTier.Large:
                                if (Random.Range(0, 100) <= 25) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 10) player.AddItem(ItemType.Flashlight);
                                if (Random.Range(0, 100) <= 20) player.AddItem(ItemType.Radio);
                                break;
                            case ExperienceManager.ExpTier.Pot:
                                if (Random.Range(0, 100) <= 40) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 15) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 15) player.AddItem(ItemType.Flashlight);
                                if (Random.Range(0, 100) <= 15) player.AddItem(ItemType.Radio);
                                if (Random.Range(0, 100) <= 8) player.AddItem(ItemType.GrenadeFlash);
                                else if (Random.Range(0, 100) <= 7) player.AddItem(ItemType.GrenadeHE);
                                break;
                            case ExperienceManager.ExpTier.Shao:
                                if (Random.Range(0, 100) <= 50) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 15) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 20) player.AddItem(ItemType.Flashlight);
                                if (Random.Range(0, 100) <= 20) player.AddItem(ItemType.Radio);
                                if (Random.Range(0, 100) <= 2) player.AddItem(ItemType.GunCOM15);
                                else if (Random.Range(0, 100) <= 2) player.AddItem(ItemType.GunCOM18);
                                else if (Random.Range(0, 100) <= 1) player.AddItem(ItemType.GunFSP9);
                                if (Random.Range(0, 100) <= 1) player.AddItem(ItemType.GunRevolver);
                                if (Random.Range(0, 100) <= 5) player.AddItem(ItemType.ArmorLight);
                                else if (Random.Range(0, 100) <= 4) player.AddItem(ItemType.ArmorCombat);
                                else if (Random.Range(0, 100) <= 1) player.AddItem(ItemType.ArmorHeavy);
                                break;
                            case ExperienceManager.ExpTier.Eat:
                            case ExperienceManager.ExpTier.EatPlus:
                                if (Random.Range(0, 100) <= 50) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 20) { var candy = CandyList.RandomItem(); player.TryAddCandy(candy); }
                                if (Random.Range(0, 100) <= 20) player.AddItem(ItemType.Flashlight);
                                if (Random.Range(0, 100) <= 20) player.AddItem(ItemType.Radio);
                                if (Random.Range(0, 100) <= 2) player.AddItem(ItemType.GunCOM15);
                                else if (Random.Range(0, 100) <= 2) player.AddItem(ItemType.GunCOM18);
                                else if (Random.Range(0, 100) <= 3) player.AddItem(ItemType.GunFSP9);
                                if (Random.Range(0, 100) <= 5) player.AddItem(ItemType.GunRevolver);
                                if (Random.Range(0, 100) <= 10) player.AddItem(ItemType.ArmorLight);
                                else if (Random.Range(0, 100) <= 5) player.AddItem(ItemType.ArmorCombat);
                                else if (Random.Range(0, 100) <= 5) player.AddItem(ItemType.ArmorHeavy);
                                break;
                        }
                    }
                }
                catch (Exception e) { Log.Warn(e.ToString()); }
            });
        }

        public static IEnumerator<float> RefreshAllPlayers()
        {
            while (true)
            {
                PlayerHUDManager.ntf = 0;
                PlayerHUDManager.doc = 0;
                PlayerHUDManager.dd = 0;
                PlayerHUDManager.gruad = 0;
                PlayerHUDManager.chaos = 0;

                foreach (var player in Player.Enumerable)
                {
                    if (player == null) continue;

                    switch (player.Role.Type)
                    {
                        case RoleTypeId.NtfCaptain:
                        case RoleTypeId.NtfSpecialist:
                        case RoleTypeId.NtfPrivate:
                        case RoleTypeId.NtfSergeant:
                            PlayerHUDManager.ntf++;
                            break;
                        case RoleTypeId.Scientist:
                            PlayerHUDManager.doc++;
                            break;
                        case RoleTypeId.FacilityGuard:
                            PlayerHUDManager.gruad++;
                            break;
                        case RoleTypeId.ChaosRifleman:
                        case RoleTypeId.ChaosConscript:
                        case RoleTypeId.ChaosMarauder:
                        case RoleTypeId.ChaosRepressor:
                            PlayerHUDManager.chaos++;
                            break;
                        case RoleTypeId.ClassD:
                            PlayerHUDManager.dd++;
                            break;
                    }

                    try
                    {
                        var hub = player.ReferenceHub;
                        var role = player.Role;
                        var room = player.CurrentRoom;
                        var hsm = HSM_hintServ.GetPlayerHUD(player) as HSM_hintServ;
                        var hud = hsm?.hud;

                        try
                        {
                            if (room == null || room.Type != RoomType.Lcz914)
                            {
                                var a = player.GetHUD() as HSM_hintServ;
                                a?.hud?.RemoveHint(PlayerHUDManager.Scp914Hint);
                            }
                        }
                        catch (Exception e) { Log.Warn($"[HUD914] {player.Nickname}: {e.Message}"); }

                        HandleEscape(player, hub);

                        if (hud != null && (room == null || room.Type != RoomType.Lcz914))
                        {
                            if (hud.HasHint("Scp914KnobChanged"))
                                hud.RemoveHint(PlayerHUDManager.Scp914Hint);
                        }

                        PlayerStateManager.HandleBadgeSync(player, hub);

                        if (role is SpectatorRole spectatorRole)
                            PlayerStateManager.HandleSpectatorTracking(player, spectatorRole);
                        else if (role is OverwatchRole overwatch)
                            PlayerStateManager.HandleSpectatorTracking(player, overwatch);
                        else
                        {
                            if (hud != null)
                            {
                                hud.RemoveHint(PlayerHUDManager.SpawnHint);
                                hud.RemoveHint(PlayerHUDManager.NtfSpawnHint);
                                hud.RemoveHint(PlayerHUDManager.ChaosSpawnHint);
                            }
                        }

                        try { PlayerStateManager.HandleScpStandHeal(player); }
                        catch (Exception e) { Log.Error($"[scpheal] {player?.Nickname ?? "Unknown"}: {e.GetType().Name} - {e.Message}"); }

                        PlayerStateManager.UpdatePlayerDisplayName(player);
                    }
                    catch (Exception e) { Log.Error($"[RefreshAllPlayers] {player?.Nickname ?? "Unknown"}: {e.GetType().Name} - {e.Message}"); }
                }

                yield return Timing.WaitForSeconds(0.25f);
            }
        }

        private static void HandleEscape(Player player, ReferenceHub hub)
        {
            if (!Escape.CanEscape(hub, out var role, out var zone)) return;

            var humanRole = role as PlayerRoles.HumanRole;
            if (humanRole != null)
            {
                RoleTypeId newRole = RoleTypeId.Spectator;
                if (!player.IsCuffed)
                {
                    if (player.Role.Type == RoleTypeId.FacilityGuard)
                        newRole = RoleTypeId.NtfSergeant;
                }
                else
                {
                    if (player.Role.Team == Team.FoundationForces)
                        newRole = RoleTypeId.ChaosConscript;
                    else if (player.Role.Team == Team.ChaosInsurgency)
                        newRole = RoleTypeId.NtfPrivate;
                }
                var currentRoleType = hub.roleManager.CurrentRole.RoleTypeId;
                var escapeScenario = Escape.EscapeScenarioType.Scientist;

                var args = new PlayerEscapingEventArgs(hub, currentRoleType, newRole, escapeScenario, zone);
                PlayerEvents.OnEscaping(args);

                if (!args.IsAllowed || args.EscapeScenario == Escape.EscapeScenarioType.None) return;

                hub.connectionToClient.Send<Escape.EscapeMessage>(new Escape.EscapeMessage
                {
                    ScenarioId = (byte)args.EscapeScenario,
                    EscapeTime = (ushort)Mathf.CeilToInt(hub.roleManager.CurrentRole.ActiveTime)
                }, 0);

                hub.roleManager.ServerSetRole(args.NewRole, RoleChangeReason.Escaped, RoleSpawnFlags.All, null);

                PlayerEvents.OnEscaped(new PlayerEscapedEventArgs(hub, currentRoleType, args.NewRole, args.EscapeScenario, zone));
                ExperienceManager.AddExp(player, 10, false, ExperienceManager.AddExpReason.GuardEscaped);
            }
        }

        public static void PreAuthenticating(PreAuthenticatingEventArgs ev)
        {
            var Pban = sql.QueryBan(ev.UserId);
            if (Pban != null)
            {
                Log.Info($"Pban {Pban}");
                bool thisServer = Pban?.port != "0" ? Pban.Value.port == ServerStatic.ServerPort.ToString() : true;
                if (thisServer)
                {
                    ev.Reject($"{Pban.Value.name}, 你在{Pban.Value.start_time.ToString("yyyy-MM-dd HH:mm:ss")}被封禁至{Pban.Value.end_time.ToString("yyyy-MM-dd HH:mm:ss")}\n原因:{Pban.Value.reason}\n处理人：{Pban.Value.issuer_name} \n如有疑问，请进群询问QQ：{Plugin.Instance.Config.QQgroup}", true);
                    Log.Info($"Kicking {ev.UserId} due find data in ban");
                    return;
                }
            }
        }

        public static void Verified(VerifiedEventArgs ev)
        {
            sql.Update(ev.Player.UserId, ev.Player.Nickname, last_time: DateTime.Now, ip: ev.Player.IPAddress);

            var PU = Plugin.Instance.connect.QueryUser(ev.Player.UserId);
            if (PU.uid == 0) return;

            if (PU.last_time.HasValue)
            {
                if (PU.last_time?.DayOfYear != DateTime.Now.DayOfYear || PU.last_time?.Year != DateTime.Now.Year)
                {
                    sql.Update(ev.Player.UserId, ev.Player.Nickname, today_duration: new TimeSpan(0));
                    ExperienceManager.AddExp(ev.Player, 25, true, ExperienceManager.AddExpReason.DayLogin);
                }
            }

            var PA = sql.QueryAdmin(userid: ev.Player.UserId);
            (string player_name, string port, string permissions, DateTime expiration_date, bool is_permanent, string notes)? target = null;
            if (PA != null && PA.Count > 0)
            {
                foreach (var item in PA)
                {
                    if (item.port == ServerStatic.ServerPort.ToString() || item.port == "0")
                    { target = item; break; }
                }
                if (target != null)
                {
                    Log.Info($"get group:{target.Value.permissions} due AdminSystem");
                    var UserGroup = ServerStatic.PermissionsHandler.GetGroup(target.Value.permissions);
                    if (UserGroup != null)
                    {
                        if (ev.Player.Group == null || (ev.Player.Group != null && ev.Player.Group.KickPower < UserGroup.KickPower))
                        {
                            Log.Info($"player {ev.Player} get group:{UserGroup.Name} due AdminSystem");
                            ev.Player.Group = UserGroup.Clone();
                            ev.Player.RankName = $"({UserGroup.Name})";
                        }
                    }
                    else { Log.Info($"failed to get group! target:{target.Value.permissions}"); }
                }
            }

            var PB = sql.QueryBadge(userid: ev.Player.UserId);
            if (PB != null && PB.Count > 0)
            {
                foreach (var item in PB)
                {
                    if (item.is_permanent || item.expiration_date <= DateTime.Now)
                    {
                        var text = item.badge;
                        if (target != null && target.HasValue)
                        {
                            Log.Info($"get group:{target.Value.permissions} due badgeSystem");
                            var UserGroup = ServerStatic.PermissionsHandler.GetGroup(target.Value.permissions);
                            if (UserGroup != null) text += $"({UserGroup.Name})";
                            else Log.Info($"failed to get group! target:{target.Value.permissions}");
                        }

                        List<string> colors = new List<string>();
                        item.color.Split(',').ForEach(c => colors.Add(c));
                        PlayerStateManager.badges[ev.Player.UserId] = (item.player_name, text, colors, item.expiration_date, item.is_permanent, item.notes);
                        break;
                    }
                }
            }

            ev.Player.AddMessage("Always_InfoShow", PlayerHUDManager.PlayerHudLVShow, -1, ScreenLocation.ReversedForPlayerLVShow);
            ev.Player.GetPlayerDisplay().AddHint(PlayerHUDManager.RoleHint);
            ev.Player.GetPlayerDisplay().AddHint(PlayerHUDManager.ElevatorHint);
            ev.Player.GetPlayerDisplay().AddHint(PlayerHUDManager.ScoreHint);
        }

        public class RoundStatistics
        {
            public int Kills { get; set; } = 0;
            public int Escapes { get; set; } = 0;
            public int Deaths { get; set; } = 0;
            public int Points { get; set; } = 0;
            public int AssistWaves { get; set; } = 0;
        }

        public static Dictionary<Player, RoundStatistics> RoundStats = new();

        public static RoundStatistics GetOrCreateStats(Player player)
        {
            if (player == null) return null;
            if (!RoundStats.ContainsKey(player))
                RoundStats[player] = new RoundStatistics();
            return RoundStats[player];
        }
    }
}
