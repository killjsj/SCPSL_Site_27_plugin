using AdminToys;
using AutoEvent;
using AutoEvent.API;
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
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Configs;
using InventorySystem.Items;
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
using Next_generationSite_27.UnionP.heavy.ability;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.RoleAssign;
using PlayerRoles.Spectating;
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
        MySQLConnect MysqlConnect = Plugin.plugin.connect;
        public Dictionary<string, List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel, bool applytoAll)>> cachedcard =
            new Dictionary<string, List<(bool enable, string card, string text, string holder, string color, string permColor, string displayCardname, int? RankLevel, bool applytoAll)>>();
        public Dictionary<ushort,ItemType> cachedCards = new Dictionary<ushort, ItemType>();
        public void ChangedItem(ChangedItemEventArgs ev)
        {
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
        public void RestartingRound()
        {
            stopBroadcast();
            if (rder.IsRunning)
            {
                MEC.Timing.KillCoroutines(rder);
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
        }

        // 在回合结束时清理所有保护
        public void OnRoundEnd(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            Plugin.plugin.scpChangeReqs = new List<ScpChangeReq>();
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
            PassAbility.Init();
            testing.FlightFailed.Start();
                GOCBomb.init();
            
        }
        public Player SelectChosenSCPPlayer(List<Player> VIPPlayerList, List<string> nottodaySCP)
        {
            VIPPlayerList.ShuffleList();
            Player chosenPlayer = VIPPlayerList.RandomItem();
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
                    Log.Debug($"chosenPlayer:{chosenPlayer},num:{num}");
                    scpTicketsLoader.ModifyTickets(chosenPlayer.ReferenceHub, 10);

                }
            }
            catch (Exception ex)
            {
                VIPPlayerList.ShuffleList();
                chosenPlayer = VIPPlayerList[random.Next(0, VIPPlayerList.Count)];

                Log.Error($"Error while loading SCP tickets: {ex}");
                Log.Debug($"chosenPlayer:{chosenPlayer}");
            }
            NotTodaySCP.Add(chosenPlayer.UserId);
            return chosenPlayer;
        }
        public List<string> NotTodaySCP = new List<string>();
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
                st = true;
                foreach (var item in Player.Enumerable)
                {
                    if (item.Role.Type != RoleTypeId.Overwatch)
                    {
                        item.RoleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RoundStart);
                    }
                }
            
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
        public void WaitingForPlayers()
        {
            st = false;
            StopBroadcast = false;
            cachedCards = new Dictionary<ushort, ItemType>();

            textToy = LabApi.Features.Wrappers.TextToy.Create(new Vector3(42.7f, 315.5f, -32),new Quaternion(0.02f,0.7f,-0.02f,0.7f));
            textToy.Scale *= 0.5f;
            Timing.CallDelayed(0.2f, () =>
            {
                rder = MEC.Timing.RunCoroutine(rounder());
            } );
        }
        CoroutineHandle rder;
        public string Waiting = "";
        LabApi.Features.Wrappers.TextToy textToy;
        public IEnumerator<float> rounder()
        {
            while (true)
            {
                try
                {
                    Waiting = $"<color=#00FFFF><size=13>目前有{ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Host, ClientInstanceMode.Dummy)}个玩家\n";
                    if (Round.IsLobbyLocked)
                    {
                        Waiting += $"回合已锁定";
                    }

                    else if (Round.IsLobby)
                    {
                        if (RoundStart.singleton.Timer == -2)
                        {
                            Waiting += $"回合不够人";

                        }
                        else
                        {
                            Waiting += $"还有{RoundStart.singleton.Timer}秒回合开始";

                        }
                    }
                    Waiting += "</size>";
                    if (textToy != null)
                    {
                        if (!textToy.IsDestroyed)
                        {
                            textToy.TextFormat = Waiting;
                        }
                    }

                }
                catch (Exception e)
                {
                    Log.Error(e);
                    break;
                }
                yield return Timing.WaitForSeconds(1f);
            }


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
                float hue = i / (float)Mathf.Max(1, text.Length - 1);
                Color color = Color.HSVToRGB(hue, 1f, 1f);
                string hex = ColorToHex(color);

                newtext += $"<color={hex}>{text[i]}</color>";
            }
            return newtext;
        }
        //public static MethodInfo SendSpawnMessageMethodInfo => typeof(NetworkServer).GetMethod("SendSpawnMessage", BindingFlags.Static | BindingFlags.NonPublic);
        public void Verified(VerifiedEventArgs ev)
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
            if (st && Round.IsLobby)
            {
                ev.Player.RoleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
            }
            else if(Round.IsLobby)
            {
                ev.Player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);
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
            ev.Player.Ex2LabPly().GiveLoadout(_plugin.Config.PrisonerLoadouts);
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