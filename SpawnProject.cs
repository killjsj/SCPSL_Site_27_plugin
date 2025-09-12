using AdminToys;
using AutoEvent;
using AutoEvent.Commands;
using CentralAuth;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.RemoteAdmin.Dummies;
using CommandSystem.Commands.RemoteAdmin.Inventory;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Features;
using Exiled.Events.Handlers;
using Exiled.Loader;
using GameCore;
using Google.Protobuf.WellKnownTypes;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using LiteNetLib;
using MEC;
using Mirror;
using Mysqlx.Notice;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP;
using Org.BouncyCastle.Asn1.Ocsp;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using PlayerRoles.RoleAssign;
using ProjectMER.Commands.Utility;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using Query;
using Respawning.Objectives;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.DedicatedServer;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Windows;
using Utils.NonAllocLINQ;
using Enum = System.Enum;
using KeycardItem = InventorySystem.Items.Keycards.KeycardItem;
using Log = Exiled.API.Features.Log;
using Object = UnityEngine.Object;
using Pickup = LabApi.Features.Wrappers.Pickup;
using Player = Exiled.API.Features.Player;
using Round = Exiled.API.Features.Round;
namespace Next_generationSite_27.UnionP.SpawnPorject
{
    class EventHandle
    {

        PConfig Config => Plugin.Instance.Config;
        public Dictionary<Player, CoroutineHandle> ProtectionCoroutines = new Dictionary<Player, CoroutineHandle>();
        public bool RoundEnded
        {
            get
            {

                return Round.IsEnded;
            }
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (RoundEnded) return;

            if (ev.Wave is NtfMiniWave || ev.Wave is NtfSpawnWave ||
                ev.Wave is ChaosMiniWave || ev.Wave is ChaosSpawnWave)
            {
                foreach (var player in ev.Players)
                {
                    ApplySpawnProtection(player);
                }
            }
        }

        private void ApplySpawnProtection(Player player)
        {
            try
            {
                // 先清理旧的保护
                if (ProtectionCoroutines.ContainsKey(player))
                {
                    Timing.KillCoroutines(ProtectionCoroutines[player]);
                    ProtectionCoroutines.Remove(player);
                }

                // 应用保护效果
                player.EnableEffect(EffectType.SpawnProtected, SpawnProtected.SpawnDuration);

                // 启动保护文本更新协程
                var coroutine = ProtectTextUpdate(player);
                var handle = MEC.Timing.RunCoroutine(coroutine);

                ProtectionCoroutines[player] = handle;

                Log.Info($"[出生保护] 为玩家 {player.Nickname} 应用保护，持续 {SpawnProtected.SpawnDuration} 秒");
            }
            catch (Exception ex)
            {
                Log.Error($"[出生保护] 为玩家 {player.Nickname} 应用保护时出错: {ex.Message}");
            }
        }

        public IEnumerator<float> ProtectTextUpdate(Player player) // AI太好用了你们知道吗
        {

            // 检查玩家有效性
            if (player == null || !player.IsConnected)
                yield break;

            var spawnProtectedEffect = player.GetEffect(EffectType.SpawnProtected);
            if (spawnProtectedEffect == null)
                yield break;
            //Log.Info(spawnProtectedEffect.TimeLeft);
            while (spawnProtectedEffect.TimeLeft > 0)
            {
                try
                {
                    // 检查玩家是否仍然有效
                    if (player == null || !player.IsConnected || !player.IsAlive)
                    {
                        if (!player.IsAlive)
                        {
                            player.DisableEffect(EffectType.SpawnProtected);

                        }
                        break;
                    }
                    var remainingTime = spawnProtectedEffect.TimeLeft;
                    var text = $"<size=27><color=#{Config.InProtectColor}>🔰刷新保护剩余 {remainingTime:F0} 秒🔰\n开枪将取消保护</color></size>";

                    player.Broadcast(new Exiled.API.Features.Broadcast()
                    {
                        Content = text,
                        Duration = 1
                    }, true);
                }
                catch (Exception ex)
                {
                    Log.Error($"[出生保护] 文本更新出错: {ex.Message}");
                }

                yield return MEC.Timing.WaitForSeconds(1f);
            }

            // 保护结束，清理字典
            if (player != null && ProtectionCoroutines.ContainsKey(player))
            {
                ProtectionCoroutines.Remove(player);
            }

            Log.Info($"[出生保护] 玩家 {player?.Nickname ?? "Unknown"} 保护结束");
        }
        public void Shot(ShotEventArgs ev)
        {
            if (ev.Player.GetEffect<SpawnProtected>() != null)
            {
                var a = ev.Player.GetEffect<SpawnProtected>();
                if (Config.NoProtectWhenShoot && (ProtectionCoroutines.ContainsKey(ev.Player)))
                {
                    try
                    {
                        // 移除保护效果
                        ev.Player.GetEffect<SpawnProtected>().TimeLeft = 0;
                        ev.Player.DisableEffect(EffectType.SpawnProtected);
                        a.ServerDisable();

                        // 停止保护协程
                        if (ProtectionCoroutines.TryGetValue(ev.Player, out var handle))
                        {
                            Timing.KillCoroutines(handle);
                            ProtectionCoroutines.Remove(ev.Player);
                        }

                        // 显示取消保护提示
                        ShowProtectionCancelledMessage(ev.Player);
                        Log.Info($"[出生保护] 玩家 {ev.Player.Nickname} 因开枪取消保护");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[出生保护] 取消保护时出错: {ex.Message}");
                    }
                }
                else
                {
                    if (a.Intensity != 0)
                    {
                        Log.Info($"[出生保护] 玩家 {ev.Player.Nickname} 因开枪取消保护");
                        a.ServerDisable();
                    }
                }
            }
        }

        private void ShowProtectionCancelledMessage(Player player)
        {
            var text = $"<size=27><color=#{Config.OutProtectColor}>保护已取消 - 因开枪</color></size>";
            player.Broadcast(new Exiled.API.Features.Broadcast()
            {
                Content = text,
                Duration = 3
            }, true);
            if (ProtectionCoroutines.ContainsKey(player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[player]);
                ProtectionCoroutines.Remove(player);
            }
        }

        // 在玩家断开连接时清理资源
        public void OnPlayerLeave(LeftEventArgs ev)
        {
            if (ProtectionCoroutines.ContainsKey(ev.Player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[ev.Player]);
                ProtectionCoroutines.Remove(ev.Player);
            }
            if (RoundStart.RoundStarted || RoundEnded)
            {
                return;
            }
        }

        // 在回合结束时清理所有保护
        public void OnRoundEnd(EndingRoundEventArgs ev)
        {
            // 清理所有保护协程
            foreach (var handle in ProtectionCoroutines.Values)
            {
                Timing.KillCoroutines(handle);
            }
            ProtectionCoroutines.Clear();
        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            ev.Player.DisableEffect(EffectType.SpawnProtected);
            if (ProtectionCoroutines.ContainsKey(ev.Player))
            {
                Timing.KillCoroutines(ProtectionCoroutines[ev.Player]);
                ProtectionCoroutines.Remove(ev.Player);
            }
        }
    }
}