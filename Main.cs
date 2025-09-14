using AutoEvent;
using AutoEvent.API.Enums;
using AutoEvent.Events;
using AutoEvent.Interfaces;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.RemoteAdmin.Dummies;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Pickups;
using Exiled.API.Interfaces;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.CustomRoles.Events;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Features;
using Exiled.Events.Handlers;
using HarmonyLib;
using InventorySystem.Configs;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.MicroHID;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Loader.Features.Plugins;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.Scp5k;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using ProjectMER.Features.Objects;
using RelativePositioning;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Utils.Networking;
using static HarmonyLib.Code;
using static PlayerStatsSystem.DamageHandlerBase;
using Extensions = AutoEvent.Extensions;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    public struct ScpChangeReq
    {
        public Player From;
        public RoleTypeId to;
    }
     
    
    class Plugin : Exiled.API.Features.Plugin<PConfig>
    {
        public static List<SettingBase> MenuCache = new List<SettingBase>();

        public List<ScpChangeReq> scpChangeReqs = new List<ScpChangeReq>();
        public Stopwatch RoundTime = new Stopwatch();
        public override string Name => "UnionPlugin";
        public override string Author   => "killjsj"; 
        public MySQLConnect connect = new MySQLConnect();
        public static Plugin plugin;
        public static Plugin Instance { get { return plugin; } }
        public static List<ReferenceHub> ScpPlayer = new List<ReferenceHub>();
        public EventHandle eventhandle;
        private SpawnPorject.EventHandle SPeventhandle;

        public override PluginPriority Priority => PluginPriority.Lower;
        public static Harmony harmony { get; private set; }
        // --- bomb gun ---
        public static int active_g { get; set; } = 0;
        public static int max_active_g { get; private set; }
        public static List<ushort> bomb_gun_ItemSerial { get; set; } = new List<ushort>();
        public static SchematicObject SOB;

        public BombHandle Bomb = new BombHandle();

        // --- vote ---
        [Description("0=yes,1=no")]
        public static List<List<Player>> vote_control = new List<List<Player>>();
        public static bool is_voting = false;
        public static void vote_start(string vote_name, long vote_time)
        {
            //player.Broadcast
            var c = MEC.Timing.RunCoroutine(vote_coroutine(vote_name, vote_time));
            //c.s
        }
        private static IEnumerator<float> vote_coroutine(string vote_name, long vote_time)
        {
            // 初始化包含两个空子列表的投票控制列表
            vote_control = new List<List<Player>> { new List<Player>(), new List<Player>() };
            is_voting = true;

            

            int yes = 0;
            int no = 0;

            // 检查并访问投票列表
           
            for (int i = (int)vote_time; i != 0; i--)
            {
                foreach (var item in Player.List)
                {
                    if (vote_control[0].Contains(item) || vote_control[1].Contains(item))
                    {
                        continue;
                    }


                    item.Broadcast((ushort)1.1f, "管理发起了投票:" + vote_name + " 时间:" + i.ToString() + " 在游戏控制台(`键)内输入.voteyes/.vyes同意 .voteno/.vno不同意 弃权不投票");
                }
                
                
                yield return Timing.WaitForSeconds(1);
            }
            is_voting = false;
            if (vote_control.Count > 0)
            {
                //foreach (var item in vote_control[0])
                //{
                //    yes++;
                //}
                //foreach (var item in vote_control[1])
                //{
                //    no++;
                //}
                yes = vote_control[0].Count;
                no = vote_control[1].Count;
            }
            double percentage = (yes / Math.Min(1,(yes+no))) * 100;
            Exiled.API.Features.Map.Broadcast((ushort)8f, "投票:" + vote_name + " 结果: 同意率:" + percentage.ToString("F2") + "% 同意:" + yes.ToString() + " 不同意:" + no);
            Log.Info("投票:" + vote_name + " 结果: 同意率:" + percentage.ToString("F2") + "% 同意:" + yes.ToString() + " 不同意:" + no);
            vote_control = new List<List<Player>>(); // 清空投票列表
        }
        // --- vote end ---
        // --- snake ---
        public void InspectedKeycard(PlayerInspectedKeycardEventArgs ev)
        {
            eventhandle.InspectedKeycard(ev);

        }
        // --- snake end ---
        // --- superSCP ---
        public static bool enableSSCP = false;
        public SuperSCP superSCP = new SuperSCP();
        // --- superSCP end---
        public override void OnEnabled()
        {
            plugin = this;
            MenuCache = LevelSCP.Menu();
            var connectionString = $"Server={Config.IpAddress};" +
                              $"Port={Config.Port};" +
                              $"Database={Config.Database};" +
                              $"Uid={Config.Username};" +
                              $"Pwd={Config.Password};" +
                              "SslMode=none;" +
                              "Connection Timeout=30;";
            connect.Connect(connectionString);
            eventhandle = new EventHandle(Config);
            SPeventhandle = new UnionP.SpawnPorject.EventHandle();
            Exiled.Events.Handlers.Map.Generated += eventhandle.Generated;
            Exiled.Events.Handlers.Player.Joined += eventhandle.Joined;
            Exiled.Events.Handlers.Server.RespawningTeam += eventhandle.RespawningTeam;
            Exiled.Events.Handlers.Server.WaitingForPlayers += eventhandle.WaitingForPlayers;
            Exiled.Events.Handlers.Player.Shot += eventhandle.Shot;
            Exiled.Events.Handlers.Player.Shot += SPeventhandle.Shot;
            Exiled.Events.Handlers.Player.Hurting += superSCP.Hurting;
            Exiled.Events.Handlers.Player.Died += superSCP.Died;
            Exiled.Events.Handlers.Server.RespawnedTeam += SPeventhandle.RespawnedTeam;
            Exiled.Events.Handlers.Player.ChangedItem += eventhandle.ChangedItem;
            Exiled.Events.Handlers.Player.ChangingMicroHIDState += eventhandle.ChangingMicroHIDState;
            //Exiled.Events.Handlers.P
            ChaosKeycardItem.OnSnakeMovementDirChanged += eventhandle.OnSnakeMovementDirChanged;
            //PlayerEvents.InspectedKeycard += eventhandle.InspectedKeycard;+-
            Exiled.Events.Handlers.Player.SentValidCommand += eventhandle.SentValidCommand;

            Exiled.Events.Handlers.Server.RoundStarted += eventhandle.RoundStarted;
            Exiled.Events.Handlers.Player.Verified += eventhandle.Verified;
            //Exiled.Events.Handlers.Player.DroppedItem += eventhandle.DroppedItem;
            Exiled.Events.Handlers.Server.RestartingRound += eventhandle.RestartingRound;
            Exiled.Events.Handlers.Player.ChangingRole += eventhandle.ChangingRole;
            Exiled.Events.Handlers.Player.ChangingRole += LevelSCP.ChangingRole;
            Exiled.Events.Handlers.Player.ChangingRole += superSCP.ChangingRole;
            Exiled.Events.Handlers.Player.ChangingRole += SPeventhandle.ChangingRole;
            Exiled.Events.Handlers.Server.EndingRound += eventhandle.EndingRound;
            //Exiled.Events.Handlers.Server.OnEndingRound += SPeventhandle.OnRoundEnd;

            Exiled.Events.Handlers.Player.Shot += Bomb.OnPlayerShotWeapon;
            //Exiled.Events.Handlers.Player.Shot += Bomb.OnPlayerShotWeapon;
            Exiled.Events.Handlers.Scp914.UpgradingPickup += Bomb.OnUpgradingPickup;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem += Bomb.OnUpgradingInventoryItem;

            Exiled.Events.Handlers.Player.Escaped += eventhandle.Escaped;
            Exiled.Events.Handlers.Player.Escaping += eventhandle.Escaping;

            Exiled.Events.Handlers.Player.Left += eventhandle.OnPlayerLeave;
            Exiled.Events.Handlers.Player.Left += SPeventhandle.OnPlayerLeave;

            Exiled.Events.Handlers.Player.Spawned += eventhandle.OnSpawned;
            Exiled.Events.Handlers.Scp079.GainingExperience += superSCP.GainingExperience;
            Exiled.Events.Handlers.Item.DisruptorFiring += eventhandle.DisruptorFiring;

            Exiled.Events.Handlers.Warhead.Starting += LevelSCP.Starting;
            Exiled.Events.Handlers.Warhead.Stopping += LevelSCP.Stopping;
            Exiled.Events.Handlers.Server.EndingRound += eventhandle.OnRoundEnd;
            Exiled.Events.Handlers.Server.EndingRound += SPeventhandle.OnRoundEnd;

            Exiled.Events.Handlers.Warhead.Detonating += Scp5k_Control.WarheadDetonated;
            Exiled.Events.Handlers.Server.EndingRound += Scp5k_Control.RoundEnding;
            Exiled.Events.Handlers.Server.RoundStarted += Scp5k_Control.RoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole += Scp5k_Control.ChangingRole;

            //Exiled.Events.Handlers.Player. += eventhandle.Escaping;
            CustomRole.RegisterRoles(assembly:Assembly);
            CustomItem.RegisterItems();
            max_active_g = Config.maxbomb;
            harmony = new Harmony("Killjsj.plugin.site27plugin");
            harmony.PatchAll();
            AutoEvent.AutoEvent.EventManager.RegisterInternalEvents();
            base.OnEnabled();
        }
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Map.Generated -= eventhandle.Generated;
            Exiled.Events.Handlers.Player.Joined -= eventhandle.Joined;
            Exiled.Events.Handlers.Server.RespawningTeam -= eventhandle.RespawningTeam;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= eventhandle.WaitingForPlayers;
            Exiled.Events.Handlers.Player.Shot -= eventhandle.Shot;
            //Exiled.Events.Handlers.Player.DroppedItem -= eventhandle.DroppedItem;
            Exiled.Events.Handlers.Player.Hurting -= superSCP.Hurting;
            Exiled.Events.Handlers.Player.Shot -= SPeventhandle.Shot;
            Exiled.Events.Handlers.Player.Died -= superSCP.Died;
            Exiled.Events.Handlers.Server.RespawnedTeam -= SPeventhandle.RespawnedTeam;
            Exiled.Events.Handlers.Player.ChangedItem -= eventhandle.ChangedItem;
            Exiled.Events.Handlers.Player.ChangingMicroHIDState -= eventhandle.ChangingMicroHIDState;
            //Exiled.Events.Handlers.P
            ChaosKeycardItem.OnSnakeMovementDirChanged -= eventhandle.OnSnakeMovementDirChanged;
            //PlayerEvents.InspectedKeycard -= eventhandle.InspectedKeycard;+-
            Exiled.Events.Handlers.Player.SentValidCommand -= eventhandle.SentValidCommand;

            Exiled.Events.Handlers.Server.RoundStarted -= eventhandle.RoundStarted;
            Exiled.Events.Handlers.Player.Verified -= eventhandle.Verified;
            Exiled.Events.Handlers.Scp079.GainingExperience -= superSCP.GainingExperience;
            Exiled.Events.Handlers.Server.RestartingRound -= eventhandle.RestartingRound;
            Exiled.Events.Handlers.Player.ChangingRole -= SPeventhandle.ChangingRole;
            Exiled.Events.Handlers.Player.ChangingRole -= LevelSCP.ChangingRole;
            Exiled.Events.Handlers.Player.ChangingRole -= eventhandle.ChangingRole;
            Exiled.Events.Handlers.Player.ChangingRole -= superSCP.ChangingRole;
            Exiled.Events.Handlers.Server.EndingRound -= eventhandle.EndingRound;
            Exiled.Events.Handlers.Server.EndingRound -= eventhandle.OnRoundEnd;
            Exiled.Events.Handlers.Server.EndingRound -= SPeventhandle.OnRoundEnd;

            Exiled.Events.Handlers.Player.Shot -= Bomb.OnPlayerShotWeapon;
            Exiled.Events.Handlers.Scp914.UpgradingPickup -= Bomb.OnUpgradingPickup;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem -= Bomb.OnUpgradingInventoryItem;

            Exiled.Events.Handlers.Player.Escaped -= eventhandle.Escaped;
            Exiled.Events.Handlers.Player.Escaping -= eventhandle.Escaping;
            
            Exiled.Events.Handlers.Player.Left -= eventhandle.OnPlayerLeave;
            Exiled.Events.Handlers.Player.Left -= SPeventhandle.OnPlayerLeave;


            Exiled.Events.Handlers.Item.DisruptorFiring-= eventhandle.DisruptorFiring;
            Exiled.Events.Handlers.Player.Spawned -= eventhandle.OnSpawned;

            //5k
            Exiled.Events.Handlers.Warhead.Detonating -= Scp5k_Control.WarheadDetonated;
            Exiled.Events.Handlers.Server.EndingRound -= Scp5k_Control.RoundEnding;
            Exiled.Events.Handlers.Server.RoundStarted -= Scp5k_Control.RoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole -= Scp5k_Control.ChangingRole;


            harmony.UnpatchAll();
            eventhandle.update();
            eventhandle.stopBroadcast();
            if (enableSSCP)
            {
                enableSSCP = false;
                superSCP.stop();
            }
            eventhandle = null;
            base.OnDisabled();
        }
        public override void OnReloaded()
        {
            harmony.UnpatchAll();
            harmony.PatchAll();
            base.OnReloaded();
        }
    }
    public class PConfig : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        [Description("射击后取消保护")]
        public bool NoProtectWhenShoot { get; set; } = true;

        [Description("保护时间")]
        public int protectTime { get; set; } = 30;
        [Description("进入保护字幕颜色")]
        public string InProtectColor { get; set; } = "4DFFB8";
        [Description("解除保护字幕颜色")]

        public string OutProtectColor { get; set; } = "00FFFF";
        [Description("重型武器限制数")]

        public int MaxSpecialWeaponLimit { get; set; } = 8;
        [Description("播报间隔(秒)")]
        public int BroadcastWaitTime { get; set; } = 180;
        [Description("播报时长(秒)")]
        public int BroadcastShowTime { get; set; } = 5;
        [Description("播报大小")]
        public int BroadcastSize { get; set; } = 27;
        [Description("播报颜色 具体见https://docs.unity.cn/cn/2020.3/Manual/StyledText.html#ColorTag 如果是十六进制rgb应在前面加 '#'号 如 #114514 ")]
        public string BroadcastColor { get; set; } = "yellow";
        [Description("播报文字")]
        public List<string> BroadcastContext { get; set; } = new List<string>() { "示范用1", "示范用2" };
        [Description("CASSIE欢迎文字 不要删去{player}")]
        public string WelcomeContext { get; set; } = "Welcome {player} 加入服务器";
        [Description("启用scp加强")]
        public bool EnableSuperScp { get; set; } = true;
        [Description("启用scp加强人数")]
        public int EnableSuperScpCount { get; set; } = 1;
        [Description("启用scp加强播报")]
        public string EnableSuperScpBroadcast { get; set; } = "已启用scp加强";
        [Description("启用scp替换")]
        public bool EnableChangeScp { get; set; } = true;
        [Description("最多炸弹数量")]
        public int maxbomb { get; set; } = 100;
        [Description("启用回合大厅")]
        public bool RoundSelfChoose { get; set; } = true;
        
        public float Showtime { get; set; } = 30f;

        public int Showduration { get; set; } = 5;

        public string FirstColorHex { get; set; } = "#FFFFFF";

        public string SecondColorHex { get; set; } = "#ff0000";

        public string MainColorHex { get; set; } = "#ff0000";
        public string TextShow { get; set; } = "Alive SCPs:";
        [Description("100级以上 等级奖励")]
        public bool Level { get; set; } = true;
        [Description("Button setting ids of features")]
        public Dictionary<Features, int> SettingIds { get; set; } = new Dictionary<Features, int>
        {
            { Features.LevelHeader, 114 },
            { Features.Scp079NukeKey, 514 },
            { Features.Scp5kHeader, 5000 },
            { Features.AEHKey, 5141 },
        };
        [Description("以下与5k相关 启用5k的概率(0-100)")]
        public int scp5kPercent { get; set; } = 0;

        [Description("Goc开始刷新时间(s)")]
        public int GocStartSpawnTime { get; set; } = 18 * 60;
        [Description("Goc间隔刷新时间(s)")]
        public int GocSpawnTime { get; set; } = 90;
        [Description("Goc刷新最高人数")]
        public int GocMaxCount { get; set; } = 8;
        [Description("uiu刷新时间(具体:第一波刷新时间+(该配置-UiUSpawnFloatTime+random(UiUSpawnTime*2)) 单位:s)")]
        public int UiUSpawnTime { get; set; } = 135;
        [Description("uiu刷新浮动时间(具体:第一波刷新时间+(UiUSpawnTime-UiUSpawnFloatTime+random(UiUSpawnFloatTime*2)) 单位:s)")]

        public int UiUSpawnFloatTime { get; set; } = 45;
        [Description("uiu刷新最高人数")]
        public int UiuMaxCount { get; set; } = 9;
        [Description("安德森刷新时间(具体:(AndSpawnTime-AndSpawnFloatTime+random(AndSpawnFloatTime*2)) 单位:s)")]
        public int AndSpawnTime { get; set; } = 350;
        [Description("安德森浮动刷新时间(具体:(AndSpawnTime-AndSpawnFloatTime+random(AndSpawnFloatTime*2)) 单位:s)")]

        public int AndSpawnFloatTime { get; set; } = 45;

        [Description("安德森单人生命数")]
        public int AndLives { get; set; } = 5;
        [Description("安德森最高人数")]
        public int AndMaxCount { get; set; } = 3;
        [Description("落锤开始刷新时间(s)")]
        public int HammerStartSpawnTime { get; set; } = 6 * 60;
        [Description("刷新落锤需要的数量(Scp+基金会人员 < 其他)")]
        public int HammerSpawnCount { get; set; } = 5;
        [Description("落锤最高人数")]
        public int HammerMaxCount { get; set; } = 6;
        [Description("是否启用数据库")]
        public bool IsEnableDatabase
        {
            get;
            set;
        }
        [Description("数据库连接地址")]
        public string IpAddress
        {
            get;
            set;
        }
        [Description("数据库端口")]
        public uint Port
        {
            get;
            set;
        }
        [Description("数据库用户名")]
        public string Username
        {
            get;
            set;
        }
        [Description("数据库密码")]
        public string Password
        {

            get;
            set;

        }
        [Description("数据库库名")]
        public string Database
        {
            get;
            set;
        }
    }

    public enum Features
    {
        LevelHeader,
        Scp079NukeKey,
        Scp5kHeader,
        AEHKey,
    }
    public class RunningMan : Event<GwangjuRunningManLoader.RunningManConfig, RunningManTranslation>, IEventMap, IEventSound
    {
        public override string Name { get; set; } = "光州RunningMan";
        public override string Description { get; set; } = "11111!5!";
        public override string Author { get; set; } = "killjsj";
        public override string CommandName { get; set; } = "RunningMan";
        public int totalTime = 180;
        public override bool AutoLoad { get; protected set; } = true;

        public MapInfo MapInfo { get; set; } = new MapInfo()
        {
            MapName = "RunningMan",
            Position = new Vector3(0, 0, 0),
            IsStatic = true
        };
        public SoundInfo SoundInfo { get; set; } = new SoundInfo()
        {
            SoundName = "GwangjuRunningMan.ogg",
            Volume = 10,
            Loop = true
        };
        protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;
        public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreAll;
        protected override float FrameDelayInSeconds { get; set; } = 0.5f;
        private GwangjuRunningManLoader.EventHandler _eventHandler;
        internal Dictionary<Player, int> Deaths { get; set; }
        internal List<GameObject> SpawnPoints { get; set; }
        public List<Player> Jailor = new List<Player>();

        private List<GameObject> _doors;
        protected override void RegisterEvents()
        {
            _eventHandler = new GwangjuRunningManLoader.EventHandler(this);
            Exiled.Events.Handlers.Item.ChargingJailbird += _eventHandler.OnChargingJailbird;
            Exiled.Events.Handlers.Player.Dying += _eventHandler.died;
        }

        protected override void UnregisterEvents()
        {
            Exiled.Events.Handlers.Item.ChargingJailbird -= _eventHandler.OnChargingJailbird;
            Exiled.Events.Handlers.Player.Dying -= _eventHandler.died;

            _eventHandler = null;
        }
        internal List<GameObject> medkit { get; set; }
        internal List<GameObject> gun { get; set; }

        protected override void OnStart()
        {
            Deaths = new Dictionary<Player, int>();
            SpawnPoints = new List<GameObject>();
            gun = new List<GameObject>();
            medkit = new List<GameObject>();
            Jailor = new List<Player>();
            _doors = new List<GameObject>();
            foreach (var obj in MapInfo.Map.AttachedBlocks)
            {
                switch (obj.name)
                {
                    case string str when str.Contains("Spawnpoint"): SpawnPoints.Add(obj); break;
                    case string str when str.Contains("medkit"): medkit.Add(obj); break;
                    case string str when str.Contains("gun"): gun.Add(obj); break;
                }
            }

            foreach (Player player in Player.List)
            {
                player.GiveLoadout(Config.PrisonerLoadouts);
                var p = SpawnPoints.Where(r => r.name == "Spawnpoint").ToList().RandomItem().transform;
                p.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
                player.Position = pos;
            }

            foreach (Player ply in Config.JailorRoleCount.GetPlayers(true))
            {
                ply.GiveLoadout(Config.JailorLoadouts);
                ply.AddItem(ItemType.Jailbird);
                ply.AddItem(ItemType.Jailbird);
                ply.AddItem(ItemType.Jailbird);
                ply.AddItem(ItemType.Jailbird);
                ply.AddItem(ItemType.Jailbird);
                ply.AddItem(ItemType.Jailbird);
                Jailor.Add(ply);
                var p = SpawnPoints.Where(r => r.name == "SpawnpointMtf").ToList().RandomItem().transform;
                p.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
                ply.Position = pos;
            }

        }

        protected override IEnumerator<float> BroadcastStartCountdown()
        {
            for (int time = 2; time > 0; time--)
            {
                foreach (Player player in Player.List)
                {
                    player.ClearBroadcasts();
                    if (player.HasLoadout(Config.JailorLoadouts))
                    {
                        player.Broadcast(1, Translation.Start.Replace("{name}", Name).Replace("{time}", time.ToString("00")));
                    }
                    else
                    {
                        player.Broadcast(1, Translation.StartPrisoners.Replace("{name}", Name).Replace("{time}", time.ToString("00")));
                    }
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        protected override bool IsRoundDone()
        {
            bool end = EventTime.TotalSeconds >= totalTime || Player.List.Count(r => r.Role == RoleTypeId.ClassD) == 0 || Player.List.Count(r => r.Role == RoleTypeId.NtfCaptain) == 0;
            return end;
        }

        protected override void ProcessFrame()
        {
            string dClassCount = Player.List.Count(r => r.Role == RoleTypeId.ClassD).ToString();
            string mtfCount = Player.List.Count(r => r.Role.Team == Team.FoundationForces).ToString();
            var showEventTime = TimeSpan.FromSeconds(totalTime - EventTime.TotalSeconds);
            string time = $"{showEventTime.Minutes:00}:{showEventTime.Seconds:00}";
            if (showEventTime.TotalSeconds % 90 == 0)
            {
                Pickup.CreateAndSpawn(ItemType.GunCOM18, gun.RandomItem().gameObject.transform.position);
                Pickup.CreateAndSpawn(ItemType.GunCOM18, gun.RandomItem().gameObject.transform.position);
                Pickup.CreateAndSpawn(ItemType.GunCOM18, gun.RandomItem().gameObject.transform.position);
            }
            if (EventTime.TotalSeconds % 30 == 0)
            {
                foreach (var item in medkit)
                {
                    Pickup.CreateAndSpawn(ItemType.Medkit, item.transform.position);
                }
            }
            foreach (Player player in Player.List)
            {
                player.ClearBroadcasts();
                player.Broadcast(1, Translation.Cycle.
                    Replace("{name}", Name).
                    Replace("{dclasscount}", dClassCount).
                    Replace("{mtfcount}", mtfCount).Replace("{time}", time));
            }
        }

        protected override void OnFinished()
        {
            if (EventTime.TotalSeconds >= 300 || Player.List.Count(r => r.Role == RoleTypeId.NtfCaptain) == 0)
            {
                Extensions.Broadcast(Translation.PrisonersWin.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}"), 10);
            }

            if (Player.List.Count(r => r.Role == RoleTypeId.ClassD) == 0)
            {
                Extensions.Broadcast(Translation.JailersWin.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}"), 10);
            }
        }
        protected override void OnCleanup()
        {
            base.DeSpawnMap();
            base.OnCleanup();
        }
    }
}
