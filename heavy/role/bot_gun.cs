using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.heavy.ability;
using Next_generationSite_27.UnionP.heavy.role;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.Scannner;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Bot_GUN
    {
        public static uint bot_gun_id = 122;
        [CustomItem(ItemType.GunRevolver)]
        public class bot_gun : CustomWeapon
        {
            public static bot_gun ins { get; private set; }
            public override uint Id { get; set; } = bot_gun_id;
            public override string Name { get; set; } = "手炮";
            public override string Description { get; set; } = "一次射3发，60s3次";
            public override float Weight { get; set; } = 1;
            public override SpawnProperties SpawnProperties { get; set; } = new() { Limit = 0 };
            public static Dictionary<ushort, AbilityCooldown> CoolDowns = new();
            public static Dictionary<ushort, CoroutineHandle> CoolDownsDeceter = new();
            public override byte ClipSize { get; set; } = 3;
            protected override void OnReloading(ReloadingWeaponEventArgs ev)
            {
                ev.IsAllowed = false;
                base.OnReloading(ev);
            }
            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                ev.IsAllowed = false;
                base.OnDroppingItem(ev);
            }
            public static IEnumerator<float> Refresher(Item item)
            {
                if (item is Firearm fr)
                {
                    var ser = item.Serial;
                    if (!CoolDowns.ContainsKey(ser))
                    {
                        yield break;
                    }
                    var cd = CoolDowns[ser];
                    while (true)
                    {
                        if (fr.MagazineAmmo < 3)
                        {
                            if (cd.IsReady)
                            {
                                cd.Trigger(30);
                                fr.MagazineAmmo += 1;
                            }
                        }
                        yield return Timing.WaitForSeconds(0.3f);
                    }
                }
            }
            protected override void OnShot(ShotEventArgs ev)
            {
                if (Plugin.active_g >= Plugin.max_active_g + 200)
                {
                    ev.Player.ShowHint($"Server Max Grenade");
                    return;
                }
                var p = ev.Player.CameraTransform.position + ev.Player.CameraTransform.forward * UnityEngine.Random.Range(0.1f, 0.5f);
                // 直接创建手雷物品并生成Pickup
                for (int i = 0; i < 3; i++)
                {
                    var grenadeItem = Item.Create(ItemType.GrenadeHE);
                    var grenadePickup = grenadeItem.CreatePickup(p, Quaternion.identity, true) as GrenadePickup;
                    if (grenadePickup != null && grenadePickup.Rigidbody != null)
                    {
                        grenadePickup.Rigidbody.AddForce(ev.Player.CameraTransform.forward * 35, ForceMode.Impulse);
                        grenadePickup.GameObject.AddComponent<BombCollisionHandler>().Init(ev.Player, grenadePickup);
                        Plugin.active_g++;
                    }
                    Plugin.active_g++;
                }
                if (CoolDowns.ContainsKey(ev.Item.Serial))
                {
                    var cd = CoolDowns[ev.Item.Serial];
                    if (cd.IsReady) cd.Trigger(30);
                }
                else
                {
                    var cd = new AbilityCooldown();
                    CoolDowns[ev.Item.Serial] = cd;
                    if (cd.IsReady) cd.Trigger(30);
                }
                base.OnShot(ev);
            }
            public override void Init()
            {
                ins = this;
                base.Init();
            }
        }
    }
}
