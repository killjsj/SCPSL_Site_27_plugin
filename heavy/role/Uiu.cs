using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using MapGeneration;
using MEC;
using Next_generationSite_27.UnionP.heavy.role;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Uiu : BaseClass
    {

        public static uint UiuCID = 32;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_C : CustomRolePlus, IDeathBroadcaster
        {
            public string CassieBroadcast => "U I U";

            public string ShowingToPlayer => "UIU";
            public static scp5k_Uiu_C ins;
            public override uint Id { get; set; } = UiuCID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "UIU 突袭小组 队员";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "UIU 突袭小组 队员";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }

            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "与安德森机器人合作 调查基金会为什么毁灭人类\n下载完资料后撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 110;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU 突袭小组 队员</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往机房下载资料 下载完资料后撤离</color></size>", 4);
                //p.AddMessage("messID", "你是 反基金会 团队 消灭一切基金会团队的成员", 2f, ScreenLocation.Center));
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.GunCrossvec),
                string.Format("{0}", ItemType.GrenadeHE)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();

                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += start;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= start;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            void OnChangingRole(ChangingRoleEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if (CH.TryGetValue(ev.Player.UserId, out var CH1))
                    {
                        if (CH1.IsRunning)
                        {
                            var hud = HSM_hintServ.GetPlayerHUD(ev.Player);
                            Timing.KillCoroutines(CH1);
                            if (hud.HasMessage("UIUdownloading" + ev.Player.Nickname))
                            {
                                hud.RemoveMessage("UIUdownloading" + ev.Player.Nickname);
                            }
                        }
                    }
                }
            }

            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        CH[player.UserId] = Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(0, 302, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });

            }
        }
        public static uint UiuPID = 28;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Uiu_P : CustomRolePlus, IDeathBroadcaster
        {
            public string CassieBroadcast => "U I U";

            public string ShowingToPlayer => "UIU";
            public static scp5k_Uiu_P ins;

            public override uint Id { get; set; } = UiuPID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "UIU 突袭小组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "UIU 突袭小组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "与安德森机器人合作 调查基金会为什么毁灭人类\n下载完资料后撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 135;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是UIU 突袭小组 队长</color></size>\n<size=30><color=yellow>调查基金会为什么毁灭人类\n前往机房下载资料 下载完资料后撤离</color></size>", 4);


                //foreach (var item in FFMul)
                //{
                //    SetFriendlyFire(item);
                //}
                this.IgnoreSpawnSystem = true;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.GunE11SR),
                string.Format("{0}", ItemType.GrenadeHE)

            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();

                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += start;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= start;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            void OnChangingRole(ChangingRoleEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if (CH.TryGetValue(ev.Player.UserId, out var CH1))
                    {
                        if (CH1.IsRunning)
                        {
                            Timing.KillCoroutines(CH1);
                        }
                    }
                }
            }
            protected override void RoleAdded(Player player)

            {
                base.RoleAdded(player);
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        CH[player.UserId] = Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(0, 302, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 1);
                    }
                });
            }
        }
        public static bool UiuEscaped
        {
            get { return _UiuEscaped; }
            set
            {
                if (value && !_UiuEscaped)
                {
                    OnUIUEscaped();
                }
                _UiuEscaped = value;
            }
        }
        public static List<Player> diedPlayer
        {
            get
            {
                var t = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.Spectator).ToList();
                t.ShuffleList();
                return t;
            }
        }
        public static PConfig config => Plugin.Instance.Config;
        public static void TrySpawnUiu(List<Player> candidates)
        {
            if (candidates.Count == 0) return;

            int count = Math.Min(config.UiuMaxCount, candidates.Count);
            var spawnList = candidates.Take(count).ToList();

            if (CustomRole.TryGet(28, out var uiuRole))
            {
                if (CustomRole.TryGet(32, out var pioneer))
                    pioneer.AddRole(spawnList[0]);

                foreach (var p in spawnList)
                    uiuRole.AddRole(p);

                Exiled.API.Features.Cassie.MessageTranslated(
                    "Security alert . Substantial U I U activity detected . Security personnel , proceed with standard protocols",
                    "安保警戒，侦测到UIU的活动。安保人员请继续执行标准协议。阻止下载资料"
                );
            }
        }
        public static void OnUIUEscaped()
        {
            foreach (var s in Player.Enumerable)
            {
                s.AddMessage("UIU_ESCAPED" + DateTime.Now.ToString(), "<size=40><color=red>UIU已撤离</color></size>");
            }
            var w = WaveManager.Waves.FirstOrDefault(x => x is ChaosSpawnWave) as ChaosSpawnWave;
            w.RespawnTokens += 1;
            FactionInfluenceManager.Add(Faction.FoundationEnemy, FactionInfluenceManager.Get(Faction.FoundationEnemy));
            w.Timer.AddTime(60);
        }
        public static float UiuDownloadTick
        {
            get
            {
                if (CustomRole.TryGet(UiuCID, out var role) && CustomRole.TryGet(UiuPID, out var Prole))
                {
                    var count = Prole.TrackedPlayers.Count + role.TrackedPlayers.Count;
                    if (count > 0)
                    {
                        // 目标：6人 → 90秒 → 每秒总进度 = 100/90
                        // 每人每秒贡献 = (100/90) / 6
                        return (100f / 90f) / 5f * count;
                    }
                }
                return 0.2f; // 默认极慢速度（无人时）
            }
        }
        //public static bool Enabled = false;

        public static bool UiuSpawned = false;
        public static bool uiu_broadcasted = false;
        public static bool _UiuEscaped = false;
        public static float UiuDownloadTime = 0;
        public static int UiuInServerRoom = 0;
        public static void OnRoundStart()
        {
            UiuInServerRoom = 0;
            _UiuEscaped = false;
            uiu_broadcasted = false; 
            UiuSpawned = false;
            UiuDownloadTime = 0; UiuDownloadBroadcasted = false;
            Plugin.RunCoroutine(UiuBackendUpdate());
            

        }
        public static IEnumerator<float> UiuBackendUpdate()
        {
            bool Downloaded = false;
            while (true)
            {
                try
                {
                    if (UiuDownloadTime >= 100f)
                    {
                        Downloaded = true;
                        if (!uiu_broadcasted)
                        {
                            Exiled.API.Features.Cassie.MessageTranslated("Security alert . U I U down load d . Security personnel , proceed with standard protocols",
                                                     "安保警戒，侦测到机房资料下载完成。安保人员请继续执行标准协议。阻止uiu撤离");
                            uiu_broadcasted = true;
                        }
                        UiuDownloadTime = 100f;
                    }
                    UiuInServerRoom = 0;
                    foreach (var item in scp5k_Uiu_C.ins.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                UiuInServerRoom += 1;
                            }
                        }
                    }
                    foreach (var item in scp5k_Uiu_P.ins.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                UiuInServerRoom += 1;
                            }
                        }
                    }
                    if (!Downloaded)
                    {
                        if (UiuInServerRoom > 0)
                        {
                            UiuDownloadTime += UiuDownloadTick * 0.2f * UiuInServerRoom;
                            if (UiuDownloadTime >= 30f && !UiuDownloadBroadcasted)
                            {
                                Exiled.API.Features.Cassie.MessageTranslated("Security alert . U I U down load activity detected . Security personnel , proceed with standard protocols",
                                                         "安保警戒，侦测到UIU的下载活动。安保人员请继续执行标准协议。前往机房");
                                UiuDownloadBroadcasted = true;
                            }
                            else if (UiuDownloadTime <= 20f)
                            {
                                UiuDownloadBroadcasted = false;
                            }
                        }
                        else
                        {
                            if (UiuDownloadTime > 0f)
                                UiuDownloadTime -= UiuDownloadTick * 0.2f;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.2f);
            }
        }
        public static Dictionary<string, CoroutineHandle> CH = new Dictionary<string, CoroutineHandle>();

        public static bool UiuDownloadBroadcasted = false;
        //static bool IEnableAble.Enabled { get ; set; }

        public static IEnumerator<float> UiuPlayerUpdate(Player player)
        {
            bool Downloaded = false;
            var p = player;
            while (true)
            {
                try
                {
                    if (player == null)
                    {
                        yield break;
                    }
                    var hud = HSM_hintServ.GetPlayerHUD(player);
                    if (hud == null)
                    {
                        yield break;
                    }

                    if (!scp5k_Uiu_C.ins.Check(player) && !scp5k_Uiu_P.ins.Check(player))
                    {
                        break;
                    }
                    if (!Downloaded)
                    {
                        if (player.CurrentRoom != null && player.CurrentRoom.RoomName != RoomName.Unnamed)

                        {

                            if (player.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                if (UiuDownloadTime >= 100f)
                                {
                                    Downloaded = true;
                                    if (hud.HasMessage("UIUdownloading"))
                                        hud.RemoveMessage("UIUdownloading");
                                    hud.AddMessage("UIU_DOWNLOAD_DONE" + DateTime.Now.ToString(),
                                                   "<size=30><color=red>你已成功下载资料,请尽快撤离!</color></size>",
                                                   4f, ScreenLocation.CenterBottom);


                                }
                                else
                                {
                                    float remainTime = 100;

                                    if (!hud.HasMessage("UIUdownloading"))
                                    {
                                        hud.AddMessage(
                                            "UIUdownloading",
                                            (x) =>
                                            {
                                                if (UiuDownloadTime >= 100f)
                                                {
                                                    if (hud.HasMessage("UIUdownloading"))
                                                        hud.RemoveMessage("UIUdownloading");
                                                    return Array.Empty<string>();
                                                }
                                                remainTime = UiuDownloadTick > 0f ? (100f - UiuDownloadTime) / UiuDownloadTick * UiuInServerRoom : float.PositiveInfinity;

                                                return new string[]
                                                {
                        $"<size=30><color=red>你正在下载资料,请勿离开电脑房! 已下载: {UiuDownloadTime:F1}% 预计下载结束: {remainTime:F1} 秒</color></size>"
                                                };
                                            },
                                            -1f,
                                            ScreenLocation.CenterBottom
                                        );
                                    }
                                }

                            }
                            else
                            {

                                if (hud.HasMessage("UIUdownloading"))
                                {
                                    hud.RemoveMessage("UIUdownloading");
                                }
                            }
                        }
                        else
                        {
                            if (hud.HasMessage("UIUdownloading"))
                            {
                                hud.RemoveMessage("UIUdownloading");
                            }
                        }
                    }
                    else
                    {
                        if (Escape.CanEscape(player.ReferenceHub, out var role, out var zone))
                        {
                            if (hud.HasMessage("UIUdownloading" + player.Nickname))
                            {
                                hud.RemoveMessage("UIUdownloading" + player.Nickname);
                            }
                            hud.AddMessage("UIU_ESCAPED" + DateTime.Now.ToString(), "<size=30><color=yellow>你作为uiu成功撤离</color></size>", 4f, ScreenLocation.Center);
                            UiuEscaped = true;
                            player.Role.Set(RoleTypeId.Spectator, reason: SpawnReason.Respawn);
                            yield break;
                        }

                    }
                }


                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.2f);

            }
            if (p.HasMessage("UIUdownloading" + player.Nickname))
            {
                p.RemoveMessage("UIUdownloading" + player.Nickname);
            }
            //Log.Debug("Out!");

        }

        public override void Init()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;

        }

        public override void Delete()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStart;

        }
    }
}
