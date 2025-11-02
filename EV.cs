using AutoEvent;
using CentralAuth;
using CommandSystem.Commands.Shared;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Handlers;
using GameCore;
using Interactables.Interobjects;
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
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using MapGeneration;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using Next_generationSite_27.UnionP;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using Respawning.Objectives;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.DedicatedServer;
using UnityEngine.EventSystems;
using Utils.NonAllocLINQ;
using static Next_generationSite_27.UnionP.RoomGraph;
using Enum = System.Enum;
using KeycardItem = InventorySystem.Items.Keycards.KeycardItem;
using Log = Exiled.API.Features.Log;
using Pickup = LabApi.Features.Wrappers.Pickup;
using Player = Exiled.API.Features.Player;
using Round = Exiled.API.Features.Round;
namespace Next_generationSite_27.UnionP
{
    class EventHandle
    {

        PConfig Config => Plugin.Instance.Config;
        public Dictionary<Player, Stopwatch> BroadcastTime = new Dictionary<Player, Stopwatch>();
        public Dictionary<ushort, Player> snakepairs = new Dictionary<ushort, Player>();
        public Dictionary<Player, int> cachedHighestPairs = new Dictionary<Player, int>();
        MySQLConnect MysqlConnect = Plugin.plugin.connect;
        public (string userid, string name, int? highscore, DateTime? time) cachedHighest = (string.Empty, string.Empty, null, DateTime.MinValue);
        public Dictionary<string, List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel, bool applytoAll)>> cachedcard =
            new Dictionary<string, List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel, bool applytoAll)>>();
        public Dictionary<ushort,ItemType> cachedCards = new Dictionary<ushort, ItemType>();
        public void ChangedItem(ChangedItemEventArgs ev)
        {
            if (ev.OldItem != null)
            {
                snakepairs.Remove(ev.OldItem.Serial);
            }
            if (ev.Item == null)
            {
            }
            else if (ev.Item.IsKeycard && !cachedCards.ContainsKey(ev.Item.Serial))
            {
                if (ev.Item is Keycard oc)
                {

                    if (!cachedcard.TryGetValue(ev.Player.UserId, out var cards))
                    {
                        var res = MysqlConnect.QueryCard(ev.Player.UserId);
                        cards = new List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel, bool applytoAll)>();
                           
                        foreach (var re in res)
                        {
                            cards.Add((re.enabled, re.card, re.Text, re.holder, re.color, re.permColor, re.CardName, re.rankLevel, re.ApplytoAll));
                        }
                        cachedcard.Add(ev.Player.UserId, cards);
                    }
                    
                    foreach (var card in cards)
                    {
                        string Toc = "";
                        if (card.applytoAll)
                        {
                            
                                Toc = card.card;
                            
                        }
                        else if (ev.Item.Type == ItemType.KeycardChaosInsurgency)
                        {
                            continue;
                        }
                        else
                        {
                            switch (oc.Identifier.TypeId)
                            {
                                case ItemType.KeycardJanitor:
                                //case ItemType.KeycardO5:
                                case ItemType.KeycardContainmentEngineer:
                                case ItemType.KeycardScientist:
                                case ItemType.KeycardResearchCoordinator:
                                    {
                                        Toc = "KeycardCustomSite02";
                                        break;
                                    }
                                case ItemType.KeycardGuard:
                                    {
                                        Toc = "KeycardCustomMetalCase";
                                        break;
                                    }
                                case ItemType.KeycardMTFCaptain:
                                case ItemType.KeycardMTFPrivate:
                                case ItemType.KeycardMTFOperative:
                                    {
                                        Toc = "KeycardCustomTaskForce";
                                        break;
                                    }
                                case ItemType.KeycardFacilityManager:
                                case ItemType.KeycardZoneManager:
                                case ItemType.KeycardO5:
                                    {
                                        Toc = "KeycardCustomManagement";
                                        break;
                                    }

                            }
                        }
                        if (card.enable &&  (( !string.IsNullOrEmpty(Toc) && Toc == card.card)|| card.applytoAll))
                        {
                            {
                                KeycardItem keycardItem;
                                if (!TryParseKeycard(Toc, out keycardItem))
                                {
                                    Log.Warn($"无法获取{card.card}");
                                    return;
                                    //response += ".";
                                    //return false;
                                }
                                Color32 color = Color.cyan; // 默认颜色
                                string permColor = card.permColor; // 默认权限颜色
                                if (!string.IsNullOrEmpty(card.color))
                                {
                                    ColorUtility.TryParseHtmlString(card.color, out var color1);
                                    color = color1;
                                }
                                if ( string.IsNullOrEmpty(card.permColor))
                                {
                                   permColor = "cyan";
                                }

                                // 安全地处理可能为 null 的字符串，并替换空格
                                string displayText = !string.IsNullOrEmpty(card.text) ? card.text.Replace(" ", "_") : "Default_Text";
                                string holderName = !string.IsNullOrEmpty(card.holder) ? card.holder.Replace(" ", "_") : "Unknown_Holder";

                                foreach (DetailBase detailBase in keycardItem.Details)
                                {
                                    ICustomizableDetail customizableDetail2 = detailBase as ICustomizableDetail;
                                    if (customizableDetail2 != null)
                                    {
                                        if (customizableDetail2 is CustomItemNameDetail IN)
                                        {
                                            IN.SetArguments(new ArraySegment<object>(new object[1] { displayText }));
                                        }
                                        if (customizableDetail2 is CustomLabelDetail LD)
                                        {

                                            LD.SetArguments(new ArraySegment<object>(new object[] { displayText, color }));
                                        }
                                        if (customizableDetail2 is NametagDetail ND)
                                        {
                                            if (Toc == "KeycardCustomTaskForce")
                                            {
                                                ND.SetArguments(new ArraySegment<object>(new object[] { displayText }));
                                            }
                                            else { 
                                                ND.SetArguments(new ArraySegment<object>(new object[] { holderName }));

                                            }

                                        }
                                        if (customizableDetail2 is CustomSerialNumberDetail SND)
                                        {
                                            SND.SetArguments(new ArraySegment<object>(new object[] { holderName }));

                                        }
                                        if (customizableDetail2 is CustomWearDetail WD)
                                        {
                                            WD.SetArguments(new ArraySegment<object>(new object[] { (byte)card.RankLevel.GetValueOrDefault(2) }));

                                        }
                                        if (customizableDetail2 is CustomTintDetail TD)
                                        {
                                            TD.SetArguments(new ArraySegment<object>(new object[] { color }));
                                        }
                                        if (customizableDetail2 is CustomRankDetail RD)
                                        {
                                            RD.SetArguments(new ArraySegment<object>(new object[] { card.RankLevel.GetValueOrDefault(2) }));
                                        }
                                        if (customizableDetail2 is CustomPermsDetail PD)
                                        {

                                            var b = new KeycardLevels(oc.Base.GetPermissions(null));
                                            PD.ParseArguments(new ArraySegment<string>(new string[] { b.Containment.ToString(), b.Armory.ToString(), b.Admin.ToString(), permColor }));


                                        }
                                    }
                                }
                                Timing.CallDelayed(0.1f, () =>
                                {
                                    var i = oc.Identifier.TypeId;
                                    ev.Player.RemoveItem(oc);
                                    AddItem(ev.Player.ReferenceHub, keycardItem.ItemTypeId, i);
                                });
                                break;
                            }
                        }
                    }
                
                }
            }
        }
        public Pickup OnUpgradingPickup(ItemPickupBase pickup) {
            if (cachedCards.ContainsKey(pickup.Info.Serial))
            {
                var temp = Pickup.Create(cachedCards[pickup.Info.Serial],pickup.Position);
                temp.Spawn();
                pickup.DestroySelf();
                cachedCards.Remove(pickup.Info.Serial);
                return temp;
            }
            return null;
        }
        //public void DroppedItem(DroppedItemEventArgs ev)
        //{
        //    if (cachedCards.ContainsKey(ev.Pickup.Info.Serial))
        //    {
        //        var temp = Pickup.Create(cachedCards[ev.Pickup.Info.Serial], ev.Pickup.Position);
        //        cachedCards.Remove(ev.Pickup.Info.Serial);
        //        ev.Pickup.Destroy();
        //    }
        //}
        public void OnUpgradingInventoryItem(ReferenceHub ev) {
            Player player = Player.Get(ev);
            var Item = player.CurrentItem;
            if (Item != null)
            {
                if (cachedCards.ContainsKey(Item.Serial))
                {

                    player.RemoveHeldItem();
                    var temp = player.AddItem(cachedCards[Item.Serial]);
                    cachedCards.Add(temp.Serial, temp.Identifier.TypeId);
                    player.CurrentItem = temp;
                    cachedCards.Remove(temp.Serial);
                    cachedCards.Remove(Item.Serial);
                }
            }
        }
        private void AddItem(ReferenceHub ply, ItemType id, ItemType oc)
        {

            var x = ply.inventory.ServerAddItem(id, ItemAddReason.AdminCommand, 0, null);
            cachedCards.Add(x.ItemSerial,oc);

            Log.Info($"已给予玩家{ply.GetNickname()} 自定义卡");
            ply.inventory.ServerSelectItem(x.ItemSerial);
            if (x == null)
            {
                throw new NullReferenceException(string.Format("Could not add {0}. Inventory is full or the item is not defined.", id));
            }
        }
        private static bool TryParseKeycard(string arg, out KeycardItem keycard)
        {
            ItemType itemType;
            if (!Enum.TryParse<ItemType>(arg, true, out itemType))
            {
                keycard = null;
                return false;
            }
            return itemType.TryGetTemplate(out keycard) && keycard.Customizable;
        }
        public void InspectedKeycard(PlayerInspectedKeycardEventArgs ev)
        {
            if (ev.KeycardItem.Base is ChaosKeycardItem chaos)
            {
                if (!snakepairs.ContainsKey(chaos.ItemSerial))
                {
                    snakepairs.Add(chaos.ItemSerial, ev.Player);
                }

            }
        }
        public void OnSnakeMovementDirChanged(ushort? Nid, Vector2Int Head)
        {
            if (Nid != null)
            {

                ushort id = Nid.Value;
                if (snakepairs.TryGetValue(id, out var player))
                {

                    var SE = ChaosKeycardItem.SnakeSessions[id];
                    if (MysqlConnect.connected)
                    {
                        if (SE != null)
                        {

                            if (!cachedHighestPairs.ContainsKey(player))
                            {
                                var highscore = MysqlConnect.QuerySnake(player.UserId).highscore;
                                if (highscore != null)
                                {
                                    cachedHighestPairs.Add(player, highscore.Value);
                                }
                                else
                                {
                                    cachedHighestPairs.Add(player, 0);
                                }
                            }
                            if (!cachedHighest.highscore.HasValue)
                            {
                                cachedHighest = MysqlConnect.QueryHighest();
                                if (!cachedHighest.highscore.HasValue)
                                {
                                    cachedHighest.highscore = 0;
                                }
                            }
                            if (cachedHighestPairs[player] < SE.Score)
                            {
                                cachedHighestPairs[player] = SE.Score;
                                if (SE.Score > cachedHighest.highscore)
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast()
                                    {
                                        Content = $"<size=15>恭喜你更新服务器最高分:{cachedHighestPairs[player]}",
                                        Duration = 1
                                    });
                                    cachedHighest = (player.UserId, player.DisplayNickname, SE.Score, DateTime.Now);
                                }
                                else
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast()
                                    {
                                        Content = $"<size=15>恭喜你更新个人最高分:{cachedHighestPairs[player]}",
                                        Duration = 1
                                    });
                                }
                            }
                        }
                    }
                }

            }
        }
        public void RestartingRound()
        {
            update();
            stopBroadcast();
        }
        public void update()
        {
            var MysqlConnect = Plugin.plugin.connect;

            if (cachedHighestPairs != null)
            {
                foreach (var item in cachedHighestPairs)
                {
                    MysqlConnect.Update(item.Key.UserId, item.Key.Nickname, item.Value, DateTime.Now);
                }
            }
        }
        public EventHandle(PConfig config)
        {
        }
        public void Generated()
        {
            InventoryLimits.StandardCategoryLimits[ItemCategory.SpecialWeapon] = (sbyte)Config.MaxSpecialWeaponLimit;
            ServerConfigSynchronizer.Singleton.RefreshCategoryLimits();
            Timing.RunCoroutine(LoadConnectMeshesAsync());


        }
        private IEnumerator<float> LoadConnectMeshesAsync()
        {
            yield return Timing.WaitUntilTrue(() => SeedSynchronizer.MapGenerated);
            new RoomGraph();

        }
        public Dictionary<Player, Stopwatch> BroadcastTimers = new Dictionary<Player, Stopwatch>();
        public bool RoundEnded
        {
            get
            {

                return Round.IsEnded;
            }
        }

        public void TemplateSimulateShot(DisruptorShotEvent data, BarrelTipExtension barrelTip)
        {
            ItemIdentifier identifier = data.ItemId;
            ParticleDisruptor template = identifier.TypeId.GetTemplate<ParticleDisruptor>();
            MagazineModule magazineModule;
            if (!template.TryGetModule(out magazineModule, true))
            {
                Log.Debug(111);
                return;
            }
            magazineModule.ServerSetInstanceAmmo(identifier.SerialNumber, 6);
            magazineModule.ServerResyncData();
        }
        public void Shot(ShotEventArgs ev)
        {
            if(ev.Item.Type == ItemType.ParticleDisruptor)
            {
                ItemIdentifier identifier = ev.Item.Base.ItemId;
                ParticleDisruptor template = ev.Item.Base as ParticleDisruptor;
                MagazineModule magazineModule;
                if (!template.TryGetModule(out magazineModule, true))
                {
                    Log.Debug(111);
                    return;
                }
                magazineModule.ServerSetInstanceAmmo(identifier.SerialNumber, 6);
                magazineModule.ServerResyncData();
            }
            
        }

        // 在玩家断开连接时清理资源
        public void OnPlayerLeave(LeftEventArgs ev)
        {

            if (BroadcastTimers.ContainsKey(ev.Player))
            {
                BroadcastTimers.Remove(ev.Player);
            }
            if (RoundStart.RoundStarted || RoundEnded)
            {
                return;
            }
            if (config.RoundSelfChoose)
            {
                foreach (var item in targetRole.Values)
                {
                    item.Remove(ev.Player.ReferenceHub);
                }
            }
        }

        // 在回合结束时清理所有保护
        public void OnRoundEnd(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            Plugin.plugin.scpChangeReqs = new List<ScpChangeReq>();

            Plugin.plugin.superSCP.stop();
            Timing.KillCoroutines(new CoroutineHandle[]
            {
                            this.updateInfo
            });
            if (config.RoundEndFF)
            {
                ServerConsole.FriendlyFire = true;
                ServerConfigSynchronizer.RefreshAllConfigs();
                foreach (var item in Player.Enumerable)
                {
                    item.AddMessage("RoundEnd",config.RoundEndFFText,location:ScreenLocation.CenterTop,duration:2);
                }
            }
            // 清理计时器
            BroadcastTimers.Clear();
        }
        private System.Random random = new System.Random();

        private PConfig config => Plugin.Instance.Config;
        public Dictionary<Player, int> PlayerTicket = new Dictionary<Player, int>();
        public void RoundStarted()
        {

            testing.FlightFailed.Start();

            if (config.RoundSelfChoose)
            {
                if (targetRole == null || targetRole.Count == 0) // 更标准的空检查
                {
                    Log.Debug("No target roles to assign. Skipping RoundStarted logic.");
                    return;
                }
                Timing.CallDelayed(0.3f, delegate ()
                {
                    try
                    {
                        Log.Debug("Starting RoundStarted role assignment logic.");

                        var readyPlayers = Player.Enumerable
                            .Where(x => !SPD.Contains(x.ReferenceHub) &&
                                        x.ReferenceHub.authManager.InstanceMode != ClientInstanceMode.Unverified &&
                                        x.ReferenceHub.nicknameSync.NickSet)
                            .ToList();

                        Log.Debug($"Ready players count: {readyPlayers.Count}");
                        readyPlayers.ShuffleListSecure();
                        Dictionary<Player, RoleTypeId> initialRoles = readyPlayers.ToDictionary(p => p, p => p.Role.Type);
                        Dictionary<Player, RoleTypeId> finalRoles = new Dictionary<Player, RoleTypeId>(initialRoles);
                        Log.Debug($"notTodaySCP:\n- {string.Join("\n- ", NotTodaySCP)}");
                        List<string> nottodaySCP = new List<string>(NotTodaySCP);
                        //Log.Info(NotTodaySCP) 
                        Log.Debug($"NotTodaySCP:\n- {string.Join("\n- ", nottodaySCP)}");

                        NotTodaySCP.Clear();
                        // 为非SCP分配保留未分配玩家列表
                        List<Player> unassignedPlayers = new List<Player>(readyPlayers);

                        Log.Debug($"Initial player roles:\n- {string.Join("\n- ", initialRoles.Select(entry => $"{entry.Key.Nickname} ({entry.Key.UserId}): {entry.Value}"))}");

                        // 4. 计算各阵营名额
                        int scpSlots = 0;
                        int mtfSlots = 0;
                        int sciSlots = 0;
                        int ddSlots = 0;
                        List<RoleTypeId> scps = new List<RoleTypeId>();

                        foreach (Player player in readyPlayers)
                        {
                            Team playerTeam = RoleExtensions.GetTeam(player.Role.Type);
                            switch (playerTeam)
                            {
                                case Team.FoundationForces:
                                    mtfSlots++;
                                    break;
                                case Team.SCPs:
                                    scpSlots++;
                                    scps.Add(player.Role.Type);
                                    break;
                                case Team.Scientists:
                                    sciSlots++;
                                    break;
                                case Team.ClassD:
                                    ddSlots++;
                                    break;
                            }
                        }
                        random = new System.Random(DateTime.Now.Hour + DateTime.Now.DayOfYear + DateTime.Now.Day + DateTime.UtcNow.Hour); // 初始化随机数种子
                        var scpTargetRoles = targetRole
                            .Where(tr => RoleExtensions.GetTeam(tr.Key) == Team.SCPs && scps.Contains(tr.Key))
                            .ToList();

                        Log.Debug($"Target SCP roles to assign: {scpTargetRoles.Count}");
                        HashSet<RoleTypeId> assignedScpRoles = new HashSet<RoleTypeId>();

                        foreach (var scpTarget in scpTargetRoles)
                        {
                            RoleTypeId targetScpRole = scpTarget.Key;
                            List<ReferenceHub> preferredHubs = scpTarget.Value ?? new List<ReferenceHub>();
                            Log.Debug($"--- Processing assignment for {targetScpRole} ---");

                            // 检查此SCP角色是否已经分配过了
                            if (assignedScpRoles.Contains(targetScpRole))
                            {
                                Log.Debug($"SCP role {targetScpRole} already assigned. Skipping.");
                                continue;
                            }

                            if (scpSlots <= 0)
                            {
                                Log.Debug($"No more SCP slots available. Skipping assignment for {targetScpRole}.");
                                continue;
                            }
                            preferredHubs.ShuffleListSecure();
                            // 查找当前已经是此SCP角色的玩家
                            Player alreadyScpPlayer = readyPlayers.FirstOrDefault(p => finalRoles[p] == targetScpRole && p.IsConnected);
                            if (alreadyScpPlayer != null)
                            {
                                // 情况 1: 已有玩家是此 SCP
                                if (preferredHubs.Contains(alreadyScpPlayer.ReferenceHub) && !nottodaySCP.Contains(alreadyScpPlayer.UserId))
                                {
                                    // 1a: 该玩家在优先列表中 -> 保留角色，消耗名额
                                    Log.Debug($"Player {alreadyScpPlayer.Nickname} is already {targetScpRole} and is preferred. Keeping role.");
                                    // finalRoles[alreadyScpPlayer] = targetScpRole; // 初始值即为此，无需更改
                                    assignedScpRoles.Add(targetScpRole); // 标记为已分配
                                    NotTodaySCP.Add(alreadyScpPlayer.UserId);
                                    scpSlots--;
                                    unassignedPlayers.Remove(alreadyScpPlayer); // 从待分配列表移除
                                }
                                else
                                {
                                    // 1b: 该玩家不在优先列表中 -> 尝试用优先玩家替换
                                    bool replaced = false;
                                    if (preferredHubs.Count > 0)
                                    {
                                        // 从优先列表中查找一个未被分配的玩家来替换
                                        var eligiblePreferredPlayers = preferredHubs
                                            .Select(hub => Player.Get(hub))
                                            .Where(p => p != null && unassignedPlayers.Contains(p) && p.IsConnected)
                                            .ToList();

                                        if (eligiblePreferredPlayers.Any())
                                        {
                                            var replacementPlayer = SelectChosenSCPPlayer(eligiblePreferredPlayers, nottodaySCP);


                                            // --- 关键检查：确保 replacementPlayer 的目标角色没有被别人占用 ---
                                            RoleTypeId replacementOriginalRole = finalRoles[replacementPlayer];
                                            Player playerHoldingReplacementRole = readyPlayers.FirstOrDefault(p => finalRoles[p] == replacementOriginalRole && p != replacementPlayer);
                                            if (playerHoldingReplacementRole != null && assignedScpRoles.Contains(replacementOriginalRole))
                                            {
                                                // 如果 replacementPlayer 原本是SCP，且该SCP角色已被分配给别人，则不能进行此替换
                                                Log.Debug($"Cannot replace {alreadyScpPlayer.Nickname} with {replacementPlayer.Nickname} because {replacementPlayer.Nickname}'s original role {replacementOriginalRole} is already assigned to {playerHoldingReplacementRole.Nickname}.");
                                            }
                                            else
                                            {
                                                // 执行替换
                                                finalRoles[replacementPlayer] = targetScpRole;
                                                finalRoles[alreadyScpPlayer] = replacementOriginalRole; // 原SCP玩家获得替换者原角色
                                                assignedScpRoles.Add(targetScpRole); // 标记目标SCP为已分配
                                                                                     // 注意：replacementOriginalRole (如果也是SCP) 现在由 alreadyScpPlayer 担任，但它本身不是新分配的，所以不加到 assignedScpRoles

                                                //alreadyScpPlayer.ReferenceHub.PlayerCameraReference
                                                try
                                                {
                                                    if (!PlayerTicket.TryGetValue(alreadyScpPlayer, out int tickets)) {
                                                        tickets = 15;
                                                    };
                                                    using (ScpTicketsLoader scpTicketsLoader = new ScpTicketsLoader())
                                                    {
                                                        scpTicketsLoader.ModifyTickets(replacementPlayer.ReferenceHub, tickets);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Warn($"Error modifying tickets for {replacementPlayer.Nickname}: {ex}");
                                                }
                                        NotTodaySCP.Add(alreadyScpPlayer.UserId);
                                                unassignedPlayers.Remove(replacementPlayer); // 从待分配列表移除
                                                unassignedPlayers.Remove(alreadyScpPlayer); // 原SCP玩家也从待分配列表移除（他的角色变了）
                                                scpSlots--; // 消耗名额
                                                replaced = true;
                                                Log.Debug($"Replaced {alreadyScpPlayer.Nickname} ({alreadyScpPlayer}) with preferred player {replacementPlayer.Nickname} as {targetScpRole}.");
                                            }
                                        }
                                        else
                                        {
                                            Log.Debug($"No eligible preferred players found to replace {alreadyScpPlayer.Nickname} for {targetScpRole}.");
                                        }
                                    }

                                    if (!replaced)
                                    {
                                        // 如果没有替换发生（优先列表为空或找不到合适人选），则保留原SCP玩家的角色
                                        Log.Debug($"Keeping existing non-preferred SCP player {alreadyScpPlayer.Nickname} as {targetScpRole}.");
                                        // finalRoles[alreadyScpPlayer] = targetScpRole; // 初始值即为此，无需更改
                                        assignedScpRoles.Add(targetScpRole); // 标记为已分配
                                        NotTodaySCP.Add(alreadyScpPlayer.UserId);
                                        scpSlots--; // 消耗名额
                                        unassignedPlayers.Remove(alreadyScpPlayer); // 从待分配列表移除
                                    }
                                }
                            }
                            Log.Debug($"SCP slot processing finished for {targetScpRole}. Remaining SCP slots: {scpSlots}.");
                        }

                        // 6. 分配其他预设角色 (非SCP)
                        if (targetRole != null)
                        {
                            var nonScpTargetRoles = targetRole.Where(tr => RoleExtensions.GetTeam(tr.Key) != Team.SCPs && tr.Key != RoleTypeId.None).ToList();
                            foreach (var nonScpTarget in nonScpTargetRoles)
                            {
                                RoleTypeId targetRoleType = nonScpTarget.Key;
                                List<ReferenceHub> candidates = nonScpTarget.Value ?? new List<ReferenceHub>();

                                Log.Debug($"Processing target non-SCP role: {targetRoleType}. Candidates: {candidates.Count}.");

                                int availableSlots = 0;
                                Team targetTeam = RoleExtensions.GetTeam(targetRoleType);

                                // 根据目标阵营获取可用名额数
                                switch (targetTeam)
                                {
                                    case Team.ClassD:
                                        availableSlots = ddSlots;
                                        break;
                                    case Team.Scientists:
                                        availableSlots = sciSlots;
                                        break;
                                    case Team.FoundationForces:
                                        availableSlots = mtfSlots;
                                        break;
                                    default:
                                        availableSlots = 0; // 对于 D-Boy, Tutorial 等特殊阵营，可能需要特殊处理
                                        break;
                                }

                                if (availableSlots <= 0)
                                {
                                    Log.Debug($"Skipping {targetRoleType} assignment, no slots available for its team ({targetTeam}).");
                                    continue;
                                }

                                // 创建候选人副本以避免在迭代时修改
                                List<ReferenceHub> candidatesCopy = new List<ReferenceHub>(candidates);
                                foreach (var candidateHub in candidatesCopy)
                                {

                                    if (availableSlots <= 0)
                                    {
                                        Log.Debug($"No more slots available for team {targetTeam}. Stopping assignment for {targetRoleType}.");
                                        break; // 退出候选人循环
                                    }

                                    Player candidatePlayer = Player.Get(candidateHub);
                                    Player playerHoldingReplacementRole = readyPlayers.FirstOrDefault(p => finalRoles[p] == targetRoleType && p != candidatePlayer);
                                    // 检查候选人是否仍在待分配列表中且已连接
                                    if (playerHoldingReplacementRole == null)
                                    {
                                    }
                                    else if (candidatePlayer != null && candidatePlayer.IsConnected && unassignedPlayers.Contains(candidatePlayer))
                                    {
                                        finalRoles[playerHoldingReplacementRole] = finalRoles[candidatePlayer];
                                        finalRoles[candidatePlayer] = targetRoleType;
                                        unassignedPlayers.Remove(candidatePlayer); // 分配后移除
                                        unassignedPlayers.Remove(playerHoldingReplacementRole); // 分配后移除
                                        Log.Debug($"Assigned {targetRoleType} to {candidatePlayer.Nickname}.");

                                        // 注意：availableSlots 是局部变量，下次循环会重新根据 ddSlots/sciSlots/mtfSlots 计算
                                        // 如果需要在本次循环内精确控制，也需要在这里递减 availableSlots
                                        availableSlots--;
                                    }
                                    else
                                    {
                                        Log.Debug($"Candidate {candidatePlayer?.Nickname ?? "Unknown"} for {targetRoleType} is either null, disconnected, or already assigned.");
                                    }
                                }
                            }
                        }


                        // 7. 应用角色变更
                        Log.Debug("Applying final role changes...");
                        int appliedChanges = 0;
                        foreach (var entry in finalRoles)
                        {
                            if (entry.Value != initialRoles[entry.Key])
                            {
                                try
                                {
                                    if (entry.Key != null && entry.Key.IsConnected)
                                    {
                                        entry.Key.RoleManager.ServerSetRole(
                                            entry.Value,
                                            RoleChangeReason.RoundStart,
                                            RoleSpawnFlags.All
                                        );
                                        appliedChanges++;
                                        Log.Info($"Successfully assigned {entry.Value} to {entry.Key.Nickname}");
                                    }
                                    else
                                    {
                                        Log.Debug($"Player {entry.Key?.Nickname ?? "Unknown"} disconnected before role could be applied.");
                                    }
                                }
                                catch (Exception applyEx)
                                {
                                    Log.Error($"Failed to assign role {entry.Value} to {entry.Key?.Nickname ?? "Unknown Player"}: {applyEx}");
                                }
                            }
                        }
                        Log.Debug($"Applied {appliedChanges} role changes.");

                        // 8. 清空目标角色列表
                        Log.Debug("Clearing targetRole lists...");
                        if (targetRole != null)
                        {
                            foreach (var item in targetRole)
                            {
                                item.Value?.Clear();
                            }
                        }
                        PlayerTicket.Clear();
                        // 9. 记录最终角色状态
                        Log.Debug($"Final player roles:\n- {string.Join("\n- ", finalRoles.Select(entry => $"{entry.Key.Nickname} ({entry.Key.UserId}): {entry.Value}"))}");
                        Log.Debug("Finished RoundStarted role assignment logic.");

                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error in RoundStarted role assignment logic: {ex}");
                    }
                });
            }


            // 11. 启用 Super SCP (如果配置允许)
            updateInfo = Timing.RunCoroutine(this.UpdateInfo());
            if (Scp5k_Control.Is5kRound)
            {
                GOCBomb.init();
            }
            try
            {
                if (Player.Enumerable.Count() >= Config.EnableSuperScpCount && Config.EnableSuperScp)
                {
                    Plugin.enableSSCP = true;
                    Plugin.plugin.superSCP.start();
                    Log.Debug("Super SCP enabled and started.");
                }
            }
            catch (Exception superScpEx)
            {
                Log.Error($"Error enabling/starting Super SCP: {superScpEx}");
            }
            
        }
        public Player SelectChosenSCPPlayer(List<Player> VIPPlayerList, List<string> nottodaySCP)
        {
                VIPPlayerList.ShuffleList();

            Player chosenPlayer = VIPPlayerList.GetRandomValue();
            VIPPlayerList.RemoveAll(p => nottodaySCP.Contains(p.UserId));
            try
            {
                using (ScpTicketsLoader scpTicketsLoader = new ScpTicketsLoader())
                {
                    int num = 0;
                    foreach (Player player in VIPPlayerList)
                    {
                            int tickets = scpTicketsLoader.GetTickets(player.ReferenceHub, 10, false); // 获取该玩家的“票数”
                            Log.Debug($"Player:{player} ticket:{tickets}");
                            if (tickets >= num && !nottodaySCP.Contains(player.UserId))
                            {
                                num = tickets;
                                chosenPlayer = player; // 选择票数最高的玩家
                            }
                        
                    }
                    scpTicketsLoader.ModifyTickets(chosenPlayer.ReferenceHub, 10);

                }
            }
            catch (Exception ex)
            {
                VIPPlayerList.ShuffleList();
                chosenPlayer = VIPPlayerList[random.Next(0, VIPPlayerList.Count)]; // 使用 UnityEngine.Random

                Log.Error($"Error while loading SCP tickets: {ex}");
            }
            NotTodaySCP.Add(chosenPlayer.UserId);
            return chosenPlayer;
        }
        public List<string> NotTodaySCP = new List<string>();
        CoroutineHandle updateInfo;
        public void SentValidCommand(SentValidCommandEventArgs ev)
        {
            if (ev.Player.RemoteAdminAccess)
            {
                MysqlConnect.LogAdminPermission(ev.Player.UserId, ev.Player.DisplayNickname, Exiled.API.Features.Server.Port, ev.Query, ev.Response, group: ev.Player.Group.Name);
            }
        }
        public Dictionary<ReferenceHub, List<(EffectType, byte, float)>> effects = new Dictionary<ReferenceHub, List<(EffectType, byte, float)>>();
        public void Escaped(EscapedEventArgs ev)
        {

            Log.Info($"{ev.Player}成功撤离 时间:{ev.EscapeTime}");
            if (effects.ContainsKey(ev.Player.ReferenceHub))
            {
                foreach (var item in effects[ev.Player.ReferenceHub])
                {
                    if (ev.Player.TryGetEffect(item.Item1, out var statusEffect))
                    {
                        ev.Player.EnableEffect(statusEffect, item.Item2, item.Item3, false);
                        ev.Player.ReferenceHub.playerEffectsController.ServerSyncEffect(statusEffect);

                    }


                    Log.Info($"对{ev.Player}施加了效果:{item} Intensity:{item.Item2} 撤离");
                }
                effects.Remove(ev.Player.ReferenceHub);

            }
        }
        public void OnSpawned(SpawnedEventArgs ev)
        {
            bool flag = ev.Player.Role.Type == RoleTypeId.ClassD;
            if (flag)
            {
                KeycardJanitor(ev.Player);
            }
        }

        // Token: 0x0600000F RID: 15 RVA: 0x0000221B File Offset: 0x0000041B
        public static void KeycardJanitor(Player p)
        {
            p.AddItem(ItemType.KeycardJanitor, 1);
        }
        public void Escaping(EscapingEventArgs ev)
        {
            bool flag = ev.Player.Role.Type == RoleTypeId.FacilityGuard;
            if (effects.ContainsKey(ev.Player.ReferenceHub))
            {
                effects[ev.Player.ReferenceHub].Clear();
                foreach (var item in ev.Player.ActiveEffects)
                {
                    if (item.GetEffectType() == EffectType.Scp1344)
                    {
                        continue;
                    }
                    if (item.GetEffectType() == EffectType.Invisible)
                    {
                        continue;
                    }
                    effects[ev.Player.ReferenceHub].Add((item.GetEffectType(), item.Intensity, item.Duration));

                }
            }
            else
            {
                effects.Add(ev.Player.ReferenceHub, new List<(EffectType, byte, float)>());
                foreach (var item in ev.Player.ActiveEffects)
                {
                    if (item.GetEffectType() == EffectType.Scp1344)
                    {
                        continue;
                    }
                    if (item.GetEffectType() == EffectType.Invisible)
                    {
                        continue;
                    }
                    effects[ev.Player.ReferenceHub].Add((item.GetEffectType(), item.Intensity, item.Duration));

                }
            }
            if (flag)
            {
                if (Scp5k_Control.Is5kRound)
                {
                    ev.EscapeScenario = EscapeScenario.CustomEscape;
                    ev.NewRole = RoleTypeId.ChaosRifleman;
                    ev.IsAllowed = true;
                    return;
                } else
                {
                    ev.EscapeScenario = EscapeScenario.CustomEscape;
                    ev.NewRole = RoleTypeId.NtfSergeant;
                    ev.IsAllowed = true;
                }
            }
        }
        public void RespawningTeam(RespawningTeamEventArgs ev)
        {
            SpawnableWaveBase newW = ev.Wave.Base;
            List<ReferenceHub> players = new List<ReferenceHub>();
            Log.Info($"RespawningTeam IsMiniWave {ev.Wave.IsMiniWave}");

            if (ev.Wave.IsMiniWave)
            {
                ev.IsAllowed = false;

                switch (ev.Wave.Faction)
                {
                    case PlayerRoles.Faction.FoundationStaff:
                        {
                            newW = new NtfSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);
                            break;
                        }
                    case PlayerRoles.Faction.FoundationEnemy:
                        {
                            newW = new ChaosSpawnWave();
                            players = WaveSpawner.SpawnWave(newW);


                            break;
                        }
                } 

                    ev.Wave.Timer.SetTime(0);
                //RespawnedTeam(new RespawnedTeamEventArgs(newW, players));

            } else
            {

            }
        }
        public CoroutineHandle BroadcasterHandler;
        public List<GameObject> SPC = new List<GameObject>();
        public List<ReferenceHub> SPD = new List<ReferenceHub>();

        public bool st = false;
        public void assing()
        {
            if (config.RoundSelfChoose) { 
            st = true;
            foreach (var item in Player.Enumerable)
            {
                if (item.Role.Type != RoleTypeId.Overwatch)
                {
                    item.RoleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RoundStart);
                }
                if (SPD.Contains(item.ReferenceHub))
                {
                    item.RoleManager.ServerSetRole(RoleTypeId.Overwatch, RoleChangeReason.RoundStart);
                    NetworkServer.Destroy(item.ReferenceHub.gameObject);
                    SPD.Remove(item.ReferenceHub);

                }
            }
            foreach (ReferenceHub obj in ReferenceHub.AllHubs)
            {
                if (SPD.Contains(obj))
                {
                    //Log.Info("SPD C");
                    //Log.Info(obj);
                    NetworkServer.Destroy(obj.gameObject);
                    SPD.Remove(obj);
                }
            }
            SPD.Clear();
            foreach (GameObject p in Plugin.SOB.AttachedBlocks)
            {
                if (p.name == "SCP096P")
                {
                    SPC.Remove(p);
                    var SCP096 = p.GetComponent<coH>();
                    if (SCP096 != null)
                    {
                        SCP096.PlayerEnter -= SCP096_PlayerEnter;
                    }
                }
                if (p.name == "SCP049P")
                {
                    SPC.Remove(p);
                    var SCP049 = p.GetComponent<coH>();
                    if (SCP049 != null)
                    {
                        SCP049.PlayerEnter -= SCP049_PlayerEnter;
                    }
                }
                if (p.name == "SCP106P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP106_PlayerEnter;
                    }
                }
                if (p.name == "SCP939P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP939_PlayerEnter;
                    }
                }
                if (p.name == "SCP173P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP173_PlayerEnter;
                    }
                }
                if (p.name == "SCP079P")
                {
                    SPC.Remove(p);
                    var SCP106 = p.GetComponent<coH>();
                    if (SCP106 != null)
                    {
                        SCP106.PlayerEnter -= SCP079_PlayerEnter;
                    }
                }
            }
            Cleaned = true; 
        }
        }
        public bool Cleaned = false;
        GameObject SP;
        GameObject canvas;
        AdminToys.TextToy textToy; 
        //TeslaOvercon CurrentOvercon = null;
        //TeslaOverconRenderer TeslaOverconRenderer = null;
        public void WaitingForPlayers()
        {
            Scp330Interobject.MaxAmountPerLife = 4;
            st = false;
            StopBroadcast = false;
            Plugin.enableSSCP = false;
            cachedCards = new Dictionary<ushort, ItemType>();
            SPD = new List<ReferenceHub>();
            SPC = new List<GameObject>();
            targetRole = new Dictionary<RoleTypeId, List<ReferenceHub>>() {
            {RoleTypeId.Scientist ,new List<ReferenceHub>()},
            {RoleTypeId.Scp079 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp049 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp096 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp173 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp106 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp939 ,new List<ReferenceHub>()},
            {RoleTypeId.FacilityGuard ,new List<ReferenceHub>()},
            {RoleTypeId.ClassD ,new List<ReferenceHub>()}
        };

            SPD.Clear();
            if (config.RoundEndFF)
            {
                ServerConsole.FriendlyFire = false;
                ServerConfigSynchronizer.RefreshAllConfigs();
            }
#if DEBUG
#endif
            if (!Config.RoundSelfChoose)
            {
                goto No;
            }

            //var method = typeof(CharacterClassManager)

            GameObject.Find("StartRound").transform.localScale = Vector3.zero;

            EventSystem.current.SetSelectedGameObject(null);
            PrefabManager.RegisterPrefabs();
            var ss = new SerializableSchematic
            {
                SchematicName = "SpawnRoom",
                Position = new Vector3(0, 290, -90)
            };

            GameObject gameObject = ss.SpawnOrUpdateObject();
            Plugin.SOB = gameObject.GetComponent< SchematicObject >();
            //Log.Info($"outside {Exiled.API.Features.Room.Get(RoomType.Surface).Position}");
            foreach (GameObject p in Plugin.SOB.AttachedBlocks)
            {

                if (p != null && p.name != null)
                {
                    //Log.Info(p.name);
                    if (p.name == "DDP")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var dd = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (dd != null)
                        {
                            dd.PlayerEnter += Dd_PlayerEnter;
                        }
                    }
                    if (p.name == "SCIP")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCI = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCI != null)
                        {
                            SCI.PlayerEnter += SCI_PlayerEnter;
                        }
                    }
                    if (p.name == "GRP")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var GR = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (GR != null)
                        {
                            GR.PlayerEnter += GR_PlayerEnter;
                        }
                    }
                    if (p.name == "spawnpoint")
                    {
                        SP = p;
                    }
                    if (p.name == "roundtext")
                    {
                        var b = LabApi.Features.Wrappers.TextToy.Create(p.transform.position, p.transform.rotation, p.transform.localScale);
                        textToy = b.Base;
                    }
                    if (p.name == "SCP079-Text")
                    {
                        var b = LabApi.Features.Wrappers.TextToy.Create(p.transform.position, p.transform.rotation, p.transform.localScale);
                        b.TextFormat = "<color=red>SCP079";
                    }
                    if (p.name == "SCP096P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP096 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP096 != null)
                        {
                            SCP096.PlayerEnter += SCP096_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP049P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP049 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);


                        if (SCP049 != null)
                        {
                            SCP049.PlayerEnter += SCP049_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP106P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP106_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP939P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP939_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP173P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP173_PlayerEnter;
                        }
                    }
                    if (p.name == "SCP079P")
                    {
                        SPC.Add(p);
                        GameObject gO = new GameObject("SCPP");

                        //if (!gameObject.TryGetComponent(out BoxCollider boxCollider))
                        //{
                        //    boxCollider = gameObject.AddComponent<BoxCollider>();
                        //}
                        Vector3 position = p.transform.position;
                        Quaternion rotation = p.transform.rotation;
                        gO.transform.SetLocalPositionAndRotation(position, rotation);
                        var SCP106 = gO.AddComponent<coH>();

                        if (!gO.TryGetComponent(out BoxCollider boxCollider))
                            boxCollider = gO.AddComponent<BoxCollider>();

                        boxCollider.isTrigger = true;
                        boxCollider.size = p.transform.localScale;
                        //boxCollider.isTrigger = true; 
                        boxCollider.enabled = true; NetworkServer.UnSpawn(p); NetworkServer.Spawn(p);

                        if (SCP106 != null)
                        {
                            SCP106.PlayerEnter += SCP079_PlayerEnter;
                        }
                    }

                    if (p.name == "DD")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当DD");
                            r.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });
                    }
                    if (p.name == "SCI")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当科学");
                            r.roleManager.ServerSetRole(RoleTypeId.Scientist, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });

                    }
                    if (p.name == "SCP096D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp096, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });
                    }
                    if (p.name == "SCP049D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp049, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });

                    }
                    if (p.name == "SCP939D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp939, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });
                    }
                    if (p.name == "SCP173D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp173, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });
                    }
                    if (p.name == "SCP106D")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当SCP");
                            r.roleManager.ServerSetRole(RoleTypeId.Scp106, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });

                        });
                    }
                    if (p.name == "GR")
                    {
                        Timing.CallDelayed(1, () =>
                        {
                            var r = DummyUtils.SpawnDummy("选择当保安");
                            r.roleManager.ServerSetRole(RoleTypeId.FacilityGuard, RoleChangeReason.RoundStart);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                var pl = Player.Get(r);
                                pl.Position = p.transform.position + Vector3.up;
                                pl.Rotation = p.transform.rotation;
                                pl.Heal(99999, true);
                                pl.IsGodModeEnabled = true;
                                SPD.Add(r);
                            });
                        });
                    }
                }
                //RoundStart.singleton.NetworkTimer = -1;
                //RoundStart.RoundStartTimer.Restart();
                //Log.Info("3");
            }
            Cleaned = false;
            GC.Collect();
            Plugin.RunCoroutine(rounder());
        No:
            BroadcasterHandler = MEC.Timing.RunCoroutine(Broadcaster());

        }
        
        public Dictionary<RoleTypeId, List<ReferenceHub>> targetRole = new Dictionary<RoleTypeId, List<ReferenceHub>>() {
            {RoleTypeId.Scientist ,new List<ReferenceHub>()},
            {RoleTypeId.Scp079 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp049 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp096 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp173 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp106 ,new List<ReferenceHub>()},
            {RoleTypeId.Scp939 ,new List<ReferenceHub>()},
            {RoleTypeId.FacilityGuard ,new List<ReferenceHub>()},
            {RoleTypeId.ClassD ,new List<ReferenceHub>()}
        };
        void hp(Player player, RoleTypeId typeId)
        {
            foreach (var item in targetRole)
            {
                item.Value.Remove(player.ReferenceHub);
            }
            targetRole[typeId].Add(player.ReferenceHub);
            player.Broadcast(2, $"你选择当{typeId}", Broadcast.BroadcastFlags.Normal, true);
        }
        void GR_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose GR");
            hp(pl, RoleTypeId.FacilityGuard);
        }

        void SCP106_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCP106");
            hp(pl, RoleTypeId.Scp106);

        }
        void SCP049_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCP049");
            hp(pl, RoleTypeId.Scp049);

        }
        void SCP939_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCP939");

            hp(pl, RoleTypeId.Scp939);
        }
        void SCP079_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCP079");
            hp(pl, RoleTypeId.Scp079);

        }
        void SCP096_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCP096");
            hp(pl, RoleTypeId.Scp096);

        }
        void SCP173_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCP173");
            hp(pl, RoleTypeId.Scp173);

        }

        void SCI_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose SCI");
            hp(pl, RoleTypeId.Scientist);

        }

        void Dd_PlayerEnter(Player pl)
        {
            //Log.Info($"{pl} choose DD");
            hp(pl, RoleTypeId.ClassD);

        }

        public void stopBroadcast()
        {
            StopBroadcast = true;

            MEC.Timing.KillCoroutines(BroadcasterHandler);


        }
        public bool StopBroadcast = false;
        public int BroadcastIndex = 0;
        public int BroadcastCounter = 0;
        public IEnumerator<float> Broadcaster()
        {
            for (; ; )
            {
                BroadcastCounter++;

                if (BroadcastCounter <= Config.BroadcastWaitTime)
                {
                    yield return MEC.Timing.WaitForSeconds(1);

                }
                else
                {
                    BroadcastCounter = 0;
                    foreach (var item in Player.Enumerable)
                    {
                        item.Broadcast(new Exiled.API.Features.Broadcast()
                        {
                            Content = $"<size={Config.BroadcastSize}><color={Config.BroadcastColor}>{Config.BroadcastContext[BroadcastIndex]}</color></size>",
                            Duration = (ushort)Config.BroadcastShowTime
                        });
                    }

                    BroadcastIndex++;
                    BroadcastIndex %= Config.BroadcastContext.Count;
                }

            }
        }
        public string Waiting = "";
        public IEnumerator<float> rounder()
        {
            while (true)
            {
                Waiting = $"目前有{ReferenceHub.AllHubs.Count - Plugin.plugin.eventhandle.SPD.Count -1}个玩家\n";
                if (Round.IsLobbyLocked)
                {
                    Waiting += $"回合已锁定";
                }
                
                else if (Round.IsLobby)
                {
                    if (RoundStart.singleton.NetworkTimer == -2)
                    {
                        Waiting += $"回合不够人";

                    } else
                    {
                        Waiting += $"还有{RoundStart.singleton.NetworkTimer}秒回合开始";

                    }
                }
                else
                {
                    Waiting += $"";
                    yield break;
                }
                textToy.TextFormat = Waiting;
                yield return Timing.WaitForSeconds(1f);
            }


        }
        public IEnumerator<float> UpdateInfo()
        {
            while (Round.IsStarted)
            {
                string FirstColorHex = Config.FirstColorHex;
                string SecondColorHex =Config.SecondColorHex;
                string MainColorHex = Config.MainColorHex;
                string TextVar = Config.TextShow;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Concat(new string[]
                {
                    "<color=",
                    MainColorHex,
                    ">",
                    TextVar,
                    "</color>"
                }));
                foreach (Exiled.API.Features.Player player in Enumerable.Where<Exiled.API.Features.Player>(Exiled.API.Features.Player.Enumerable, (Exiled.API.Features.Player p) => p.Role.Team == Team.SCPs && p.IsAlive))
                {
                    sb.Append(string.Format("|<size=32><color={0}>{1} </color> :  <color={2}>{3}HP</color></size>| ", new object[]
                    {
                        FirstColorHex,
                        player.Role.Name,
                        SecondColorHex,
                        Convert.ToInt32(player.Health)
                    }));
                }
                string scpInfo = sb.ToString();
                float Showtime = Config.Showtime;
                int Showduration = Config.Showduration;
                Exiled.API.Features.Map.Broadcast((ushort)Showduration, scpInfo, global::Broadcast.BroadcastFlags.Normal, false);
                yield return Timing.WaitForSeconds(Showtime);
                FirstColorHex = null;
                SecondColorHex = null;
                MainColorHex = null;
                TextVar = null;
                sb = null;
                scpInfo = null;
            }
            yield break;
        }
        public List<ushort> x3itemid = new List<ushort>();
        public void DisruptorFiring(DisruptorFiringEventArgs ev)
        {
            if (Plugin.enableSSCP)
            {
                if (!x3itemid.Contains(ev.Pickup.Serial))
                {
                    ItemIdentifier identifier = ev.Pickup.Base.ItemId;
                    ParticleDisruptor template = identifier.TypeId.GetTemplate<ParticleDisruptor>();
                    MagazineModule magazineModule;
                    if (!template.TryGetModule(out magazineModule, true))
                    {
                        return;
                    }
                    magazineModule.ServerSetInstanceAmmo(identifier.SerialNumber, 10);
                    magazineModule.ServerResyncData();
                    x3itemid.Add(identifier.SerialNumber);
                }
            }
        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (Plugin.plugin.superSCP.PatchedPlayers.Contains(ev.Player))
            {
                Plugin.plugin.superSCP.PatchedPlayers.Remove(ev.Player);
            }
            if (config.RoundSelfChoose)
            {
                if (ev.NewRole == RoleTypeId.Overwatch)
                {
                    foreach (var item in targetRole.Values)
                    {
                        item.Remove(ev.Player.ReferenceHub);
                    }
                }
            }
        }

        public void ChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
        {
        }
        public void Joined(JoinedEventArgs ev)
        {
        }
        string ColorToHex(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        public string rainbowtime(string text)
        {
            string newtext = "";
            for (int i = 0; i < text.Length; i++)
            {
                // 在 0~1 之间均匀分布 Hue（色相）
                float hue = i / (float)Mathf.Max(1, text.Length - 1);
                Color color = Color.HSVToRGB(hue, 1f, 1f); // 饱和度和亮度最大

                // 转为 HEX 格式
                string hex = ColorToHex(color);

                newtext += $"<color={hex}>{text[i]}</color>";
            }
            return newtext;
        }
        //public static MethodInfo SendSpawnMessageMethodInfo => typeof(NetworkServer).GetMethod("SendSpawnMessage", BindingFlags.Static | BindingFlags.NonPublic);
        public void Verified(VerifiedEventArgs ev)
        {
            try
            {

                if (!Round.IsStarted)
                {
                    if (Config.RoundSelfChoose)
                    {
                        try
                        {
                            using (ScpTicketsLoader scpTicketsLoader = new ScpTicketsLoader())
                            {
                                PlayerTicket.Add(ev.Player, scpTicketsLoader.GetTickets(ev.Player.ReferenceHub,10,true));
                            }
                            var method = typeof(CharacterClassManager)
    .GetMethod("RpcRoundStarted", BindingFlags.Instance | BindingFlags.NonPublic);

                            method.Invoke(ev.Player.ReferenceHub.characterClassManager, null);

                        }
                        catch (Exception e)
                        {
                                PlayerTicket.Add(ev.Player, 10);
                            Log.Warn("Failed to get SCP tickets for player " + ev.Player.Nickname + "Exception:" + e.ToString());
                        }
                        if (SPD != null && !SPD.Contains(ev.Player.ReferenceHub))
                        {
                            ev.Player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);
                            ev.Player.Position = SP.transform.position + Vector3.up * 3;
                            Timing.CallDelayed(2f, () =>
                            {
                                if (!Round.IsStarted)
                                {
                                    if (ev.Player.Position == SP.transform.position + Vector3.up * 3)
                                    {
                                        ev.Player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
                                        ev.Player.Broadcast(3, "出现bug了!已将你传回高塔,请联系管理");
                                    }
                                }
                            });
                        }
                    }
                }
            } catch (Exception e){ 
                Log.Warn(e.ToString());
            }
            if (!cachedcard.TryGetValue(ev.Player.UserId, out var cards))
            {
                
                var res = MysqlConnect.QueryCard(ev.Player.UserId);
                        cards = new List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel, bool applytoAll)>();
                foreach (var re in res)
                {
                    cards.Add((re.enabled, re.card, re.Text, re.holder, re.color, re.permColor, re.CardName, re.rankLevel, re.ApplytoAll));
                }
                cachedcard.Add(ev.Player.UserId, cards);
            }
            else
            {
                    cachedcard.Remove(ev.Player.UserId);
                var res = MysqlConnect.QueryCard(ev.Player.UserId);
                        cards = new List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel,     bool applytoAll)>();
                foreach (var re in res)
                {
                    cards.Add((re.enabled, re.card, re.Text, re.holder, re.color, re.permColor, re.CardName, re.rankLevel, re.ApplytoAll));
                }

                cachedcard.Add(ev.Player.UserId, cards);
                
            }
            if (st)
            {
                ev.Player.RoleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
            }
            var CS = MysqlConnect.QueryCassieWelcome(ev.Player.UserId);
            if (CS.enabled)
            {
                if(string.IsNullOrEmpty(CS.welcomeText))
                {
                    if (CS.color != "rainbow"){ 
                    Exiled.API.Features.Cassie.MessageTranslated("Welcome", $"<color={CS.color}>{config.WelcomeContext.Replace("{player}", ev.Player.Nickname)}</color>", isNoisy: false, isSubtitles: true); 
                    } else 
                    {
                        Exiled.API.Features.Cassie.Message(rainbowtime(config.WelcomeContext.Replace("{player}", ev.Player.Nickname)), isNoisy: false, isSubtitles: true);
                    }
                } else
                {
                    if (CS.color != "rainbow")
                    {
                        Exiled.API.Features.Cassie.MessageTranslated("Welcome", $"<color={CS.color}>{CS.welcomeText}</color>", isNoisy: false, isSubtitles: true);
                    }
                    else
                    {
                        Exiled.API.Features.Cassie.MessageTranslated("Welcome", rainbowtime(CS.welcomeText), isNoisy: false, isSubtitles: true);
                    }

                }

            }
        }
    }
    public class coH : MonoBehaviour
    {
        public delegate void onplayerenter(Player pl);
        public event onplayerenter PlayerEnter;
        public void OnTriggerEnter(Collider other)
        {
            Player player = Player.Get(other.gameObject);
            if (player is null)
                return;
            PlayerEnter.Invoke(player);

        }
    }
}
namespace GwangjuRunningManLoader
{
    public class EventHandler
    {
        RunningMan _plugin;
        public EventHandler(RunningMan plugin)
        {
            _plugin = plugin;
        }
        public void died(DyingEventArgs ev)
        {
            if (ev.Player.Role == PlayerRoles.RoleTypeId.NtfCaptain || _plugin.Jailor.Contains(ev.Player))
            {
                return;
            }
            if (_plugin.Deaths.TryGetValue(ev.Player, out int v))
            {
                if (v == _plugin.Config.PrisonerLives - 1)
                {
                    return;
                }
                else
                {
                    _plugin.Deaths[ev.Player] += 1;
                }
            }
            else
            {
                _plugin.Deaths[ev.Player] = 1;
            }
            ev.IsAllowed = false;
            ev.Player.RoleManager.ServerSetRole(RoleTypeId.ClassD,RoleChangeReason.Respawn);
            ev.Player.GiveLoadout(_plugin.Config.PrisonerLoadouts);
            ev.Player.Position = _plugin.SpawnPoints.Where(r => r.name == "Spawnpoint").ToList().RandomItem().transform.position;
            ev.Player.Health = 100;

        }
        //public void Hit(HurtEventArgs ev)
        //{
        //    if (ev.Attacker.IsNTF && ev.Attacker.CurrentItem.Category == ItemCategory.SpecialWeapon)
        //    {
        //        ev.Attacker.RemoveItem(ev.Attacker.CurrentItem);

        //        ev.Attacker.CurrentItem = ev.Attacker.AddItem(ItemType.Jailbird);
        //    }
        //}
        public void OnChargingJailbird(ChargingJailbirdEventArgs ev)
        {
            ev.IsAllowed = false;
        }
    }
}