using AdminToys;
using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using GameObjectPools;
using HintServiceMeow.UI.Utilities;
using InventorySystem;
using InventorySystem.Items;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utf8Json.Formatters;
using Utils;

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
                            Plugin.bomb_gun_ItemSerial.Add(itemBase.Serial);

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
}
