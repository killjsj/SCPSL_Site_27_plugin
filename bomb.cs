using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.ThrowableProjectiles;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]

    public class test_bg : ICommand, IUsageProvider
    {
        public string Command { get; } = "givebb";


        public string[] Aliases { get; } = new string[1] { "givebb" };


        public string Description { get; } = "give a bomb gun";


        public string[] Usage { get; } = new string[1] { "%player%" };

        private static ItemBase AddItem(ReferenceHub ply, ICommandSender sender, ItemType id)
        {
            ItemBase itemBase = ply.inventory.ServerAddItem(id, ItemAddReason.AdminCommand, 0);
            ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.LogName} gave {id} to {ply.LoggedNameFromRefHub()}.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging);
            if (itemBase == null)
            {
                throw new NullReferenceException($"Could not add {id}. Inventory is full or the item is not defined.");
            }
            return itemBase;
        }
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
            {
                return false;
            }

            if (arguments.Count >= 1)
            {
                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);

                ItemType id = ItemType.GunCOM15;

                int num = 0;
                int num2 = 0;
                string arg = string.Empty;
                if (list != null)
                {
                    foreach (ReferenceHub item in list)
                    {
                        try
                        {
                            Player player = Player.Get(item);
                            ItemBase itemBase = AddItem(item, sender, id);
                            Plugin.bomb_gun_ItemSerial.Add(itemBase.ItemSerial);

                        }
                        catch (Exception ex)
                        {
                            num++;
                            arg = ex.Message;
                            continue;
                        }

                        num2++;
                    }
                }

                response = ((num == 0) ? string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!") : $"Failed to execute the command! Failures: {num}\nLast error log:\n{arg}");
                return true;
            }

            response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
            return false;
        }


        private bool HasPerms(RoleTypeId targetRole, bool self, ICommandSender sender, out string response)
        {
            switch (targetRole)
            {
                case RoleTypeId.Spectator:
                    if (self)
                    {
                        return sender.CheckPermission(new PlayerPermissions[4]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassToSpectator,
                        PlayerPermissions.ForceclassSelf,
                        PlayerPermissions.Overwatch
                        }, out response);
                    }

                    return sender.CheckPermission(new PlayerPermissions[2]
                    {
                    PlayerPermissions.ForceclassWithoutRestrictions,
                    PlayerPermissions.ForceclassToSpectator
                    }, out response);
                case RoleTypeId.Overwatch:
                    if (self)
                    {
                        return sender.CheckPermission(PlayerPermissions.Overwatch, out response);
                    }

                    if (sender.CheckPermission(PlayerPermissions.Overwatch, out response))
                    {
                        return sender.CheckPermission(new PlayerPermissions[2]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassToSpectator
                        }, out response);
                    }

                    return false;
                default:
                    if (self)
                    {
                        return sender.CheckPermission(new PlayerPermissions[2]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassSelf
                        }, out response);
                    }

                    return sender.CheckPermission(PlayerPermissions.ForceclassWithoutRestrictions, out response);
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]

    public class test_bgplus : ICommand, IUsageProvider
    {
        public string Command { get; } = "givebbplus";


        public string[] Aliases { get; } = new string[1] { "givebbplus" };


        public string Description { get; } = "give a bomb gun plus version";


        public string[] Usage { get; } = new string[1] { "%player%" };

        private static ItemBase AddItem(ReferenceHub ply, ICommandSender sender, ItemType id)
        {
            ItemBase itemBase = ply.inventory.ServerAddItem(id, ItemAddReason.AdminCommand, 0);
            ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.LogName} gave {id} to {ply.LoggedNameFromRefHub()}.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging);
            if (itemBase == null)
            {
                throw new NullReferenceException($"Could not add {id}. Inventory is full or the item is not defined.");
            }
            return itemBase;
        }
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
            {
                return false;
            }

            if (arguments.Count >= 1)
            {
                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);

                ItemType id = ItemType.GunCom45;

                int num = 0;
                int num2 = 0;
                string arg = string.Empty;
                if (list != null)
                {
                    foreach (ReferenceHub item in list)
                    {
                        try
                        {
                            Player player = Player.Get(item);
                            ItemBase itemBase = AddItem(item, sender, id);
                            Plugin.bomb_gun_ItemSerial.Add(itemBase.ItemSerial);

                        }
                        catch (Exception ex)
                        {
                            num++;
                            arg = ex.Message;
                            continue;
                        }

                        num2++;
                    }
                }

                response = ((num == 0) ? string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!") : $"Failed to execute the command! Failures: {num}\nLast error log:\n{arg}");
                return true;
            }

            response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
            return false;
        }


        private bool HasPerms(RoleTypeId targetRole, bool self, ICommandSender sender, out string response)
        {
            switch (targetRole)
            {
                case RoleTypeId.Spectator:
                    if (self)
                    {
                        return sender.CheckPermission(new PlayerPermissions[4]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassToSpectator,
                        PlayerPermissions.ForceclassSelf,
                        PlayerPermissions.Overwatch
                        }, out response);
                    }

                    return sender.CheckPermission(new PlayerPermissions[2]
                    {
                    PlayerPermissions.ForceclassWithoutRestrictions,
                    PlayerPermissions.ForceclassToSpectator
                    }, out response);
                case RoleTypeId.Overwatch:
                    if (self)
                    {
                        return sender.CheckPermission(PlayerPermissions.Overwatch, out response);
                    }

                    if (sender.CheckPermission(PlayerPermissions.Overwatch, out response))
                    {
                        return sender.CheckPermission(new PlayerPermissions[2]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassToSpectator
                        }, out response);
                    }

                    return false;
                default:
                    if (self)
                    {
                        return sender.CheckPermission(new PlayerPermissions[2]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassSelf
                        }, out response);
                    }

                    return sender.CheckPermission(PlayerPermissions.ForceclassWithoutRestrictions, out response);
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]

    public class set_bg : ICommand, IUsageProvider
    {
        public string Command { get; } = "setbg";


        public string[] Aliases { get; } = new string[1] { "setbg" };


        public string Description { get; } = "set the gun to bomb gun if you hand it";


        public string[] Usage { get; } = new string[1] { "%player%" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
            {
                return false;
            }

            if (arguments.Count >= 1)
            {
                string[] newargs;
                List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out newargs);

                int num = 0;
                int num2 = 0;
                string arg = string.Empty;
                if (list != null)
                {
                    foreach (ReferenceHub item in list)
                    {
                        try
                        {
                            Player player = Player.Get(item);
                            var itemBase = player.CurrentItem;
                            if (itemBase == null)
                            {
                                throw new NullReferenceException($"Could not set the gun. item is not defined.");
                            }
                            if (!itemBase.IsWeapon)
                            {
                                throw new NullReferenceException($"Could not set the gun. item is not a gun.");
                            }
                            BombHandle.RegisterAGun(itemBase);

                        }
                        catch (Exception ex)
                        {
                            num++;
                            arg = ex.Message;
                            continue;
                        }

                        num2++;
                    }
                }

                response = ((num == 0) ? string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!") : $"Failed to execute the command! Failures: {num}\nLast error log:\n{arg}");
                return true;
            }

            response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
            return false;
        }


        private bool HasPerms(RoleTypeId targetRole, bool self, ICommandSender sender, out string response)
        {
            switch (targetRole)
            {
                case RoleTypeId.Spectator:
                    if (self)
                    {
                        return sender.CheckPermission(new PlayerPermissions[4]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassToSpectator,
                        PlayerPermissions.ForceclassSelf,
                        PlayerPermissions.Overwatch
                        }, out response);
                    }

                    return sender.CheckPermission(new PlayerPermissions[2]
                    {
                    PlayerPermissions.ForceclassWithoutRestrictions,
                    PlayerPermissions.ForceclassToSpectator
                    }, out response);
                case RoleTypeId.Overwatch:
                    if (self)
                    {
                        return sender.CheckPermission(PlayerPermissions.Overwatch, out response);
                    }

                    if (sender.CheckPermission(PlayerPermissions.Overwatch, out response))
                    {
                        return sender.CheckPermission(new PlayerPermissions[2]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassToSpectator
                        }, out response);
                    }

                    return false;
                default:
                    if (self)
                    {
                        return sender.CheckPermission(new PlayerPermissions[2]
                        {
                        PlayerPermissions.ForceclassWithoutRestrictions,
                        PlayerPermissions.ForceclassSelf
                        }, out response);
                    }

                    return sender.CheckPermission(PlayerPermissions.ForceclassWithoutRestrictions, out response);
            }
        }
    }
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
                if(Plugin.active_g >= Plugin.max_active_g + 200)
                {
                    plr.ShowHint($"Server Max Grenade");
                    return;
                }
                var p = plr.CameraTransform.position + plr.CameraTransform.forward * UnityEngine.Random.value;
                // 直接创建手雷物品并生成Pickup
                var grenadeItem = Item.Create(ItemType.GrenadeHE);
                var grenadePickup = grenadeItem.CreatePickup(p, Quaternion.identity, true) as GrenadePickup;
                if (grenadePickup != null && grenadePickup.Rigidbody != null)
                {
                    grenadePickup.Rigidbody.AddForce(plr.CameraTransform.forward * 25, ForceMode.Impulse);
                    Timing.RunCoroutine(GCoroutine(plr, grenadePickup));
                    Plugin.active_g++;
                }
                Plugin.active_g++;
            }
        }
        public static void RegisterAGun(Pickup gun)
        {

            if (!Plugin.bomb_gun_ItemSerial.Contains(gun.Serial))
            {

                Plugin.bomb_gun_ItemSerial.Add(gun.Serial);
            }

        }
        public static void RegisterAGun(Item gun)
        {

            if (!Plugin.bomb_gun_ItemSerial.Contains(gun.Serial))
            {

                Plugin.bomb_gun_ItemSerial.Add(gun.Serial);
            }

        }

        private IEnumerator<float> GCoroutine(Exiled.API.Features.Player plr,GrenadePickup grenadePickup)
        {
            //yield return Timing.WaitForSeconds(0.1f);
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
