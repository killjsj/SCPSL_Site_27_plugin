using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.heavy.role;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.JS_L1;
using static Next_generationSite_27.UnionP.heavy.Scannner;
using static Next_generationSite_27.UnionP.heavy.SpeedBuilditem;
using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Goc : BaseClass
    {
        public static void WarheadDetonated(Exiled.Events.EventArgs.Warhead.DetonatingEventArgs ev)
        {
            if (Enabled && Nuke_GOC_Spawned && LastChangedWarheadIsGoc && !Player.Enumerable.Any(x => x.IsScp))
            {
                heavy.Goc.Nuke_GOC_WinCon = true;
                Round.EndRound(true);
            }
            GocSpawnable = false;
        }
        public static void ChangingLeverStatus(Exiled.Events.EventArgs.Warhead.ChangingLeverStatusEventArgs ev)
        {
            if (Enabled)
            {
                if (ev.Player != null)
                {
                    if (ev.CurrentState && ev.IsAllowed && !Warhead.IsInProgress)
                    {
                        if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                        {
                            return;
                        }
                        if (ev.Player.UniqueRole == GocNukeP.Name || ev.Player.UniqueRole == GocNukeC.Name)
                        {
                            Cassie.MessageTranslated("", "GOC正在强制启动核弹!所有人前去关闭!");
                            LastChangedWarheadIsGoc = true;
                            Warhead.Start(false, trigger: ev.Player);
                        }
                    }
                    else if (Warhead.IsInProgress && !ev.CurrentState && !LastChangedWarheadIsGoc)
                    {

                        if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                        {
                            Warhead.Stop(ev.Player);
                            return;
                        }
                        if (ev.Player.UniqueRole == GocNukeP.Name || ev.Player.UniqueRole == GocNukeC.Name)
                        {
                            ev.Player.AddMessage($"FailedToControlWarhead_{ev.Player.Nickname}", "<size=27><color=red>无法控制核弹! 核弹已启用!</color></size>", 2f, ScreenLocation.Scp914);
                            ev.IsAllowed = false;
                        }
                        else
                        {
                            LastChangedWarheadIsGoc = false;
                            Warhead.Stop(ev.Player);
                        }
                    }
                }
            }
        }

        public static uint Goc2CID = 47;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_2_C : CustomRolePlus, IDeathBroadcaster
        {
            public static scp5k_Goc_2_C ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc2CID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术1组 特工";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术1组 特工";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "使用奇术核弹毁灭站点";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 100;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术1组 特工</color></size>\n<size=30><color=yellow>使用奇术核弹毁灭站点</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        string.Format("{0}", ItemType.ArmorCombat),
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardChaosInsurgency),
        string.Format("{0}", ItemType.SCP207),
        string.Format("{0}", ItemType.GunLogicer)
    };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }

            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in GOCFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    var g = CustomItem.Get(GocBombItemId);
                    if (g != null)
                    {
                        g.Give(player);
                    }
                    SpeedBuildItem.instance.Give(player, false);
                });

                base.RoleAdded(player);
            }
        }
        public static uint Goc2PID = 48;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_2_P : CustomRolePlus,IDeathBroadcaster
        {
            public static scp5k_Goc_2_P ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc2PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术1组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术1组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启奇术核弹毁灭站点";

                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 140;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术1组 队长</color></size>\n<size=30><color=yellow>开启奇术核弹毁灭站点</color></size>", 4);
                ins = this;

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        string.Format("{0}", ItemType.ArmorHeavy),
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardChaosInsurgency),
        string.Format("{0}", ItemType.SCP207),
        string.Format("{0}", ItemType.GunFRMG0)
    };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in GOCFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    var g = CustomItem.Get(GocBombItemId);
                    if (g != null)
                    {
                        g.Give(player);
                    }
                    SpeedBuildItem.instance.Give(player, false);
                });
                base.RoleAdded(player);
            }
        }
        public static uint GocBombItemId = 5514;
        [CustomItem(ItemType.Coin)]
        public class scp5k_GocBomb : CustomItemPlus
        {
            public override uint Id { get; set; } = GocBombItemId;
            public override string Name { get; set; } = "Goc奇术发生器";
            public override string Description { get => $"要安放在{GOCBomb.installCount}个互相离得最远的重收房间"; set { } }

            public override float Weight { get; set; } = 25;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            public override Vector3 Scale { get; set; } = new Vector3(5f, 5f, 5f);
            protected override void OnOwnerChangingRole(OwnerChangingRoleEventArgs ev)
            {
                foreach (var item in ev.Player.Items)
                {
                    if (Check(item))
                    {
                        ev.Player.DropItem(item);
                        break;
                    }
                }
                base.OnOwnerChangingRole(ev);
            }

            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                    base.OnUpgrading(ev);
                }
            }
            void SearchingItem(SearchingPickupEventArgs ev)
            {
                if (Check(ev.Pickup))
                {

                }
            }
            protected override void ShowSelectedMessage(Player player)
            {
                string w = "";
                if (GOCBomb.installAt.Where(x => !GOCBomb.installedRoom.Any(y => y.Value == x)).Count() != 0)
                {
                    foreach (var item in GOCBomb.installAt.Where(x => !GOCBomb.installedRoom.Any(y => y.Value == x)))
                    {
                        w += $"{item.Type} ";
                    }
                    player.AddMessage("Wait", $"<color=green><size=20>还剩下没有安装的房间:{w}</size></color>", 3f, ScreenLocation.Center);
                }
                else
                {
                    player.AddMessage("Wait", $"<color=green><size=27>安装完成</size></color>", 3f, ScreenLocation.Center);

                }

            }
            static CachedLayerMask RoomDetectionMask = new CachedLayerMask(new string[]
{
            "Default",
            "InvisibleCollider",
            "Fence",
            "Glass","Door",
            "CCTV"
});
            public void Flip(FlippingCoinEventArgs ev)
            {

                try
                {
                    //Log.Info($">>> OnUsedItem 被触发！玩家: {ev.Player.Nickname}，物品类型: {ev.Item.Type}");

                    var GocC4 = CustomItem.Get(GocBombItemId);
                    if (GocC4 == null)
                    {
                        Log.Error("❌ GocC4 自定义物品未找到！GocBombItemId = " + GocBombItemId);
                        return;
                    }

                    if (!GocC4.Check(ev.Item))
                    {
                        //Log.Info($"❌ 当前物品不是 GocC4，类型为: {ev.Item.Type}");
                        return;
                    }
                    //Log.Info("✅ GocC4 检查通过");

                    var lp = ev.Player;
                    if (lp == null)
                    {
                        //Log.Error("❌ 无法获取 LabApi Player Wrapper");
                        return;
                    }
                    Vector3 Install_Pos = ev.Player.Position;
                    //Vector3 rotateDir = ev.Player.CameraTransform.forward;
                    //if (Physics.Raycast(ev.Player.CameraTransform.position, rotateDir, out RaycastHit hitInfo, 10f, RoomDetectionMask.Mask))
                    //{
                    //    rotateDir = hitInfo.normal;
                    //    Install_Pos = hitInfo.point + (hitInfo.normal * 0.1f); // 沿法线方向偏移0.1单位,防止嵌入墙内
                    //}
                    //else
                    //{
                    //    lp.AddMessage("no", "<color=red>请对着墙安装</color>", 3f);
                    //    return;
                    //}
                    var currentRoom = Room.Get(Install_Pos);
                    if (currentRoom == null)
                    {
                        //Log.Warn("玩家当前房间为 null");
                        lp.AddMessage("no", "<color=red>无法获取房间</color>", 3f);
                        return;
                    }

                    if (GOCBomb.installAt.Contains(currentRoom))
                    {
                        //Log.Info($"✅ 玩家在允许安装的房间: {currentRoom.Name}");

                        if (GOCBomb.installedRoom.Any(x => x.Value == currentRoom))
                        {
                            //Log.Info("❌ 房间已安装过炸弹");/
                            lp.AddMessage("NO!", "<color=red><size=27>该房间已安装!</size></color>", 3f);
                            return;
                        }

                        if ((scp5k_Sci.TryGet(Goc610PID, out var sciRole) && sciRole.Check(ev.Player)) || (scp5k_Sci.TryGet(Goc610CID, out var sciCRole) && sciCRole.Check(ev.Player)))
                        {
                            //Log.Info("✅ 玩家拥有安装权限");


                            //var pickup = ev.Item.CreatePickup(Install_Pos,Quaternion.Euler(rotateDir),true);
                            var pickup = ev.Item.CreatePickup(Install_Pos);
                            pickup.Rigidbody.isKinematic = true;
                            pickup.PhysicsModule.Rb.isKinematic = true;
                            foreach (var item in pickup.GameObject.transform.GetComponentsInChildren<NetworkIdentity>())
                            {
                                Exiled.API.Extensions.MirrorExtensions.EditNetworkObject(item, (_) => { });
                            }

                            if (pickup != null)
                            {
                                GOCBomb.installbomb(pickup);
                                Log.Info($"炸弹成功安装在房间: {currentRoom.Name}");
                            }
                            else
                            {
                                //Log.Warn("❌ DropItem 返回 null，安装失败");
                                lp.AddMessage("no", "<color=red>丢弃物品失败，请重试</color>", 3f);
                            }
                        }
                        else

                        {
                            //Log.Info("❌ 玩家没有 Goc610PID 权限");
                            lp.AddMessage("NO!", "<color=red>你没有权限安装此炸弹</color>", 3f);
                        }
                    }
                    else
                    {
                        //Log.Info($"❌ 当前房间不允许安装: {currentRoom.Name}，允许的房间: {string.Join(", ", GOCBomb.installAt.Select(r => r?.Name ?? "null"))}");

                        lp.AddMessage("NO!", "<color=red><size=27>不在该房间安装!</size></color>", 3f);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("OnUsedItem 发生异常: " + ex);
                }
            }

            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                if (Check(ev.Item))
                {
                    ev.IsAllowed = false;
                    var p = ev.Item.CreatePickup(ev.Player.Position, ev.Player.Rotation, true);
                    p.Scale = this.Scale;
                    ev.Player.RemoveItem(ev.Item);
                }
                base.OnDroppingItem(ev);
            }
            protected override void SubscribeEvents()
            {
                Exiled.Events.Handlers.Player.SearchingPickup += SearchingItem;

                Exiled.Events.Handlers.Player.FlippingCoin += Flip;
                base.SubscribeEvents();
            }



            protected override void UnsubscribeEvents()
            {

                Exiled.Events.Handlers.Player.SearchingPickup -= SearchingItem;
                Exiled.Events.Handlers.Player.FlippingCoin -= Flip;
                base.UnsubscribeEvents();
            }
            public override void Init()
            {
                base.Init();
            }
        }

        public static uint GocNukeCID = 42;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_nuke_C : CustomRolePlus, IDeathBroadcaster
        {
            public static scp5k_Goc_nuke_C ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = GocNukeCID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 消灭1组 特工";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 消灭1组 特工";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "开启核弹并消灭所有SCP撤离";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 110;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 消灭1组 特工</color></size>\n<size=30><color=yellow>开启核弹并消灭所有SCP撤离</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        string.Format("{0}", ItemType.ArmorCombat),
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardChaosInsurgency),
        string.Format("{0}", ItemType.SCP207),
        string.Format("{0}", ItemType.GunLogicer)
    };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    Nuke_GOC_Spawned = true;
                });

                base.RoleAdded(player);
            }
        }
        public static uint GocNukePID = 41;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_nuke_P : CustomRolePlus, IDeathBroadcaster
        {
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public static scp5k_Goc_nuke_P ins;
            public override uint Id { get; set; } = GocNukePID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 消灭1组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 消灭1组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启核弹并消灭所有SCP撤离";
                ins = this;
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 150;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 消灭1组 队长</color></size>\n<size=30><color=yellow>开启核弹并消灭所有SCP撤离</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        string.Format("{0}", ItemType.ArmorHeavy),
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardChaosInsurgency),
        string.Format("{0}", ItemType.SCP207),
        string.Format("{0}", ItemType.GunFRMG0)
    };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                    }
                    GocSpawned = true;
                    Nuke_GOC_Spawned = true;
                });
                base.RoleAdded(player);
            }
        }
        public static uint GocNukeScanID = 57;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_nuke_scan : CustomRolePlus, IDeathBroadcaster
        {
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public static scp5k_Goc_nuke_scan ins;
            public override uint Id { get; set; } = GocNukeScanID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 消灭1组 扫描专员";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 消灭1组 扫描专员";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "消灭所有SCP撤离";
                ins = this;
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 100;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 消灭1组 扫描专员</color></size>\n<size=30><color=yellow>消灭所有SCP撤离</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        //string.Format("{0}", ItemType.ArmorHeavy),
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardChaosInsurgency),
        string.Format("{0}", ItemType.SCP207),
        //string.Format("{0}", ItemType.GunFRMG0)
    };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                    }
                    MagicGun1_JS_L1.ins.Give(player, false);
                    scanner.ins.Give(player, false);
                    GocSpawned = true;
                    Nuke_GOC_Spawned = true;
                });
                base.RoleAdded(player);
            }
        }
        public static uint GocSpyID = 33;
        public static GameObject _GOCBOmb;
        public static GameObject GOCBOmb
        {
            set
            {
                _GOCBOmb = value;
                //Scp5k.GOCAnim.Playstart(_GOCBOmb.gameObject);
            }
            get { return _GOCBOmb; }
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
        public static void TrySpawnGoc(List<Player> candidates, bool Is610Time = false)
        {
            int count = Math.Min(config.GocMaxCount, candidates.Count);
            var wave = candidates.Take(count).ToList();

            uint cid = Spawn_Nuke_GOC ? GocNukeCID : Goc2CID;
            uint pid = Spawn_Nuke_GOC ? GocNukePID : Goc2PID;
            cid = Is610Time ? Goc610CID : cid;
            pid = Is610Time ? Goc610PID : cid;
            if (CustomRole.TryGet(cid, out var role))
            {
                if (CustomRole.TryGet(pid, out var pioneer))
                    pioneer.AddRole(wave[0]);

                foreach (var p in wave)
                {
                    if (UnityEngine.Random.Range(0, 100) >= 60)
                    {
                        role.AddRole(p);
                    }
                    else
                    {
                        scp5k_Goc_nuke_scan.ins.AddRole(p);
                    }
                }

                Cassie.MessageTranslated(
                    "Security alert . Substantial G O C activity detected . Security personnel , proceed with standard protocols , Protect the warhead",
                    "安保警戒，侦测到大量GOC的活动。安保人员请继续执行标准协议，保护核弹。"
                );
            }
        }
        public static void TrySpawnGocSmall(List<Player> candidates, bool Is610Time = false)
        {
            uint cid = Spawn_Nuke_GOC ? GocNukeCID : Goc2CID;
            uint pid = Spawn_Nuke_GOC ? GocNukePID : Goc2PID;
            cid = Is610Time ? Goc610CID : cid;
            pid = Is610Time ? Goc610PID : cid;
            if (!CustomRole.TryGet(cid, out var role)) return;

            int need = config.GocMaxCount - role.TrackedPlayers.Count;
            if (need <= 0) return;

            var wave = candidates.Take(need).ToList();
            if (CustomRole.TryGet(pid, out var pioneer))
                pioneer.AddRole(wave[0]);

            foreach (var p in wave)
            {
                if (UnityEngine.Random.Range(0, 100) >= 60)
                {
                    role.AddRole(p);
                }
                else
                {
                    scp5k_Goc_nuke_scan.ins.AddRole(p);
                }
            }

            Cassie.MessageTranslated(
                "Attention security personnel , G O C spotted at Gate A . Protect the warhead",
                "安保人员请注意，已在A大门处发现GOC，保护核弹。"
            );
        }
        public static bool Enabled = false;


        public static bool Spawn_Nuke_GOC = false;
        public static bool Nuke_GOC_WinCon = false;
        public static int GocKilledScp = 0;
        public static bool Goc_Spy_broadcasted = false;
        public static bool GocNuke = false;
        public static bool GocSpawnable = true;
        public static bool GocSpawnedOnce = false;
        public static bool GocSpawned = false;
        public static bool LastChangedWarheadIsGoc = false;
        public static bool Nuke_GOC_Spawned = false;
        public override void Init()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnd;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;
            Exiled.Events.Handlers.Warhead.Detonating += WarheadDetonated;
            Exiled.Events.Handlers.Warhead.ChangingLeverStatus += ChangingLeverStatus;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStart;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnd;
            Exiled.Events.Handlers.Warhead.Detonating -= WarheadDetonated;
            Exiled.Events.Handlers.Warhead.ChangingLeverStatus -= ChangingLeverStatus;
            //throw new NotImplementedException();
        }
        public static Stopwatch GocTimer = new Stopwatch();

        public static void OnRoundStart()
        {
            GocTimer.Restart();
            Spawn_Nuke_GOC = false;
            Nuke_GOC_WinCon = false;
            Goc_Spy_broadcasted = false;
            GocNuke = false;
            GocKilledScp = 0;
            GocSpawnable = true;
            GocSpawnedOnce = false;
            GocSpawned = false;
            LastChangedWarheadIsGoc = false;
            Nuke_GOC_Spawned = false;
        }
        public static void OnRoundEnd(RoundEndedEventArgs ev)
        {
            GocTimer.Restart(); Enabled = false;
            Spawn_Nuke_GOC = false;
            Nuke_GOC_WinCon = false;
            Goc_Spy_broadcasted = false;
            GocNuke = false;
            GocKilledScp = 0;
            GocSpawnable = true;
            GocSpawnedOnce = false;
            GocSpawned = false;
            LastChangedWarheadIsGoc = false;
            Nuke_GOC_Spawned = false;
        }

        [CustomRole(RoleTypeId.ClassD)]
        public class scp5k_Goc_spy : CustomRolePlus
        {
            public static scp5k_Goc_spy ins;
            public override uint Id { get; set; } = GocSpyID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 间谍";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
            {
                Limit = 1,
            };
            public override void Init()
            {
                ins = this;
                Description = "你是Goc间谍\n前往广播室呼叫阵营";

                this.Role = RoleTypeId.ClassD;
                MaxHealth = 100;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc间谍</color></size>\n<size=30><color=yellow>前往广播室呼叫阵营</color></size>", 4);

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
    {
        string.Format("{0}", ItemType.Medkit),
        string.Format("{0}", ItemType.Painkillers),
        string.Format("{0}", ItemType.KeycardGuard),
        string.Format("{0}", ItemType.Coin),
        string.Format("{0}", ItemType.GunCOM18)
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
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                Exiled.Events.Handlers.Player.FlippingCoin += Flip;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void Flip(FlippingCoinEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    if (diedPlayer.Count == 0)
                    {

                        ev.Player.AddMessage("spy_failed_no_body_died" + DateTime.Now.ToString(), "<size=30><color=green>失败 没有GOC在待命!</color></size>", 3f, ScreenLocation.Center);
                        return;
                    }
                    if (ev.Player.CurrentRoom != null && ev.Player.CurrentRoom.RoomName == RoomName.EzIntercom && !Goc_Spy_broadcasted && GocSpawnable)
                    {
                        Log.Info("goc");
                        var tempdiedPlayer = diedPlayer;
                        tempdiedPlayer.ShuffleList();
                        var GocWave = new List<Player>(Math.Min(config.GocMaxCount, tempdiedPlayer.Count - 1));
                        GocWave.AddRange(tempdiedPlayer.Take(Math.Min(config.GocMaxCount, tempdiedPlayer.Count - 1)));
                        tempdiedPlayer.RemoveRange(0, Math.Min(config.GocMaxCount, tempdiedPlayer.Count - 1));
                        if (Spawn_Nuke_GOC)
                        {
                            if (CustomRole.TryGet(GocNukeCID, out var role) && GocWave.Count > 0)
                            {
                                if (CustomRole.TryGet(GocNukePID, out var Prole))
                                {
                                    Prole.AddRole(GocWave[0]);
                                }
                                tempdiedPlayer.RemoveRange(0, 1);
                                foreach (var item in GocWave)
                                {
                                    role.AddRole(item);
                                }
                            }
                        }
                        else
                        {
                            if (CustomRole.TryGet(Goc610CID, out var role) && GocWave.Count > 0)
                            {
                                if (CustomRole.TryGet(Goc610PID, out var Prole))
                                {
                                    Prole.AddRole(GocWave[0]);
                                }
                                tempdiedPlayer.RemoveRange(0, 1);
                                foreach (var item in GocWave)
                                {
                                    role.AddRole(item);
                                }
                            }
                            GocSpawned = true;
                        }
                        if (Goc_Spy_broadcasted)
                        {
                            Cassie.MessageTranslated("Security alert . Substantial G o c activity detected . Security personnel ,  proceed with standard protocols , Protect the warhead ", "安保警戒，侦测到大量GOC的活动。安保人员请继续执行标准协议，保护核弹。");
                            ev.Player.AddMessage("Spy_goc_spawned" + DateTime.Now.ToString(), "<size=30><color=green>你成功呼叫支援!</color></size>", 3f, ScreenLocation.Center);
                        }
                        else
                        {
                            ev.Player.AddMessage("spy_failed_no_body" + DateTime.Now.ToString(), "<size=30><color=green>失败!</color></size>", 3f, ScreenLocation.Center);

                        }
                    }
                    else
                    {
                        var p = ev.Player;
                        p.AddMessage("Spy_Failed_not_in_intercom" + DateTime.Now.ToString(), "<size=30><color=red>你必须在广播室使用硬币呼叫阵营!</color></size>", 3f, ScreenLocation.Center);
                    }
                }
            }
            protected override void UnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.FlippingCoin -= Flip;
                base.UnsubscribeEvents();
            }
            protected override void RoleAdded(Player player)

            {
                player.InfoArea = PlayerInfoArea.Nickname | PlayerInfoArea.Badge | PlayerInfoArea.Role | PlayerInfoArea.UnitName;
                base.RoleAdded(player);
            }
        }
        public static uint Goc610CID = 30;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_610_C : CustomRolePlus, IDeathBroadcaster
        {
            public static scp5k_Goc_610_C ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc610CID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术2组 特工";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术2组 特工";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                Description = "使用奇术核弹毁灭站点";
                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 120;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术2组 特工</color></size>\n<size=30><color=yellow>使用奇术核弹毁灭站点</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunLogicer)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }

            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in GOCFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    var g = CustomItem.Get(GocBombItemId);
                    if (g != null)
                    {
                        g.Give(player);
                    }
                    SpeedBuildItem.instance.Give(player, false);
                });

                base.RoleAdded(player);
            }
        }
        public static uint Goc610PID = 31;
        [CustomRole(RoleTypeId.Tutorial)]
        public class scp5k_Goc_610_P : CustomRolePlus, IDeathBroadcaster
        {
            public static scp5k_Goc_610_P ins;
            public string CassieBroadcast => "G O C";

            public string ShowingToPlayer => "GOC";
            public override uint Id { get; set; } = Goc610PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Goc 奇术2组 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Goc 奇术2组 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                Description = "开启奇术核弹毁灭站点";

                this.Role = RoleTypeId.Tutorial;
                MaxHealth = 160;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是Goc 奇术2组 队长</color></size>\n<size=30><color=yellow>开启奇术核弹毁灭站点</color></size>", 4);
                ins = this;

                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunFRMG0)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>

                {
                    if (player != null)
                    {
                        //MEC.Plugin.RunCoroutine(UiuPlayerUpdate(player));
                        player.Position = new Vector3(16, 292, -41);
                        foreach (var item in GOCFF)
                        {
                            player.SetFriendlyFire(item);

                        }
                        player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 0);
                        player.SetCustomRoleFriendlyFire("Goc_P", RoleTypeId.Tutorial, 0);
                    }
                    GocSpawned = true;
                    var g = CustomItem.Get(GocBombItemId);
                    if (g != null)
                    {
                        g.Give(player);
                    }
                    SpeedBuildItem.instance.Give(player, false);
                });
                base.RoleAdded(player);
            }
        }

        public static bool Nuke_GOC_COunt
        {
            get
            {
                if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                {
                    return false;
                }
                return Player.Enumerable.Any(x =>
                {
                    return x.UniqueRole == GocNukeP.Name || x.UniqueRole == GocNukeC.Name;
                });
            }
        }
        public static int Nuke_GOC_count
        {
            get
            {
                if (!(CustomRole.TryGet(GocNukeCID, out var GocNukeC) && CustomRole.TryGet(GocNukePID, out var GocNukeP)))
                {
                    return 0;
                }
                return Player.Enumerable.Count(x =>
                {
                    return x.UniqueRole == GocNukeP.Name || x.UniqueRole == GocNukeC.Name;
                });
            }
        }

        
    }
}
