using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.ShotEvents;
using MEC;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    public class BombHandle
    {


        public float maxDistance = 2000f; // 最大检测距离
        public float lookAccurate = 300f; // 精准度
        public float lookAngle = 100f; // 扇形角度

        public void  MapGenerated () {
            //var n = Locker.Get(getl).ToList();
            //foreach (var item in n)
            //{
            //    foreach (var item2 in item.Base.Chambers)
            //    {
            //        foreach(var item3 in item2.Content)
            //        {
            //            if(item3.ItemId.TypeId != ItemType.GunFRMG0 &&
            //                item3.ItemId.TypeId != ItemType.GunE11SR) { continue; }
            //            Plugin.bomb_gun_ItemSerial.Add(item3.Info.Serial);
            //        }
            //    }
            //}
            //Vector3 sp = new Vector3();
            //foreach(var b in Room.List)
            //{
            //    if(b.RoomName == MapGeneration.RoomName.Hcz106)
            //    {
            //        foreach(var b1 in b.Doors)
            //        {
            //            if (b1.IsKeycardDoor == true)
            //            {
            //                sp = b1.Position + b1.Rotation * Vector3.forward * 2 + Vector3.up * 2;
            //                break;
            //            }
            //        }
            //    }
            //}
            //Pickup Pi;
            //if (UnityEngine.Random.value == 0.01f) // 1% 概率
            //{
            //    Pi = Item.Create(ItemType.GunCom45).CreatePickup(sp, Quaternion.identity, true);

            //}
            //else { 
            //    Pi = Item.Create(ItemType.GunCOM15).CreatePickup(sp, Quaternion.identity, true);

            //}
            //Plugin.bomb_gun_ItemSerial.Add(Pi.Serial);
        }
        //public bool getl(Locker n)
        //{
        //    if (n == null)
        //    {
        //        return false;
        //    }
        //    if (n.Room.RoomName == MapGeneration.RoomName.Hcz079)
        //    {
        //        return true;
        //    }
        //    if (n.Room.RoomName == MapGeneration.RoomName.Hcz049)
        //    {
        //        return true;
        //    }

        //    return false;

        //}
        public void OnPlayerShotWeapon(ShotEventArgs ev)
        {
            var plr = ev.Player;
            var gun = ev.Firearm;

            //Log.Info($"Player {plr.Nickname} ({plr.UserId}) shot {gun.Base.ItemId}");
            // Bomb gun
            if (Plugin.bomb_gun_ItemSerial.Contains(gun.Serial))
            {
                if(Plugin.active_g >= Plugin.max_active_g)
                {
                    plr.ShowHint($"Server Max Grenade");
                    return;
                }
                var p = plr.CameraTransform.position + plr.CameraTransform.forward * UnityEngine.Random.value;
                GrenadePickup grenadePickup = (GrenadePickup)Pickup.CreateAndSpawn<GrenadePickup>(ItemType.GrenadeHE, p, plr.CameraTransform.rotation, plr);
                grenadePickup.Rigidbody.AddForce(plr.CameraTransform.forward * 25, ForceMode.Impulse);
                Timing.RunCoroutine(GCoroutine(plr,grenadePickup));
                Plugin.active_g++;
            }
        }

        private IEnumerator<float> GCoroutine(Exiled.API.Features.Player plr,GrenadePickup grenadePickup)
        {
            yield return Timing.WaitForSeconds(0.5f);
            if(grenadePickup == null)
            {
                yield break;
            }
            grenadePickup.GameObject.AddComponent<BombCollisionHandler>().Init(plr, grenadePickup);

        }

        public void OnUpgradingPickup(UpgradingPickupEventArgs args)
        {
            if (args.Pickup != null)
            {
                foreach (var item in Plugin.bomb_gun_ItemSerial)
                {
                    if (args.Pickup.Serial == item)
                    {
                        args.IsAllowed = false;
                    }
                }
            }
        }

        internal void OnUpgradingInventoryItem(UpgradingInventoryItemEventArgs args)
        {
            if (args.Item != null)
            {
                foreach (var item in Plugin.bomb_gun_ItemSerial)
                {
                    if (args.Item.Serial == item)
                    {
                        args.IsAllowed = false;

                    }
                }
            }
        }
    }
}
