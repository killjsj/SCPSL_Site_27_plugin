using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp049;
using Exiled.Events.EventArgs.Scp0492;
using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.EventArgs.Scp939;
using Exiled.Events.EventArgs.Warhead;
using HarmonyLib;
using Hazards;
using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Org.BouncyCastle.Bcpg.Sig;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using RelativePositioning;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;
using Player = Exiled.API.Features.Player;
using Random = UnityEngine.Random;
using Scp079Role = Exiled.API.Features.Roles.Scp079Role;
using Warhead = Exiled.API.Features.Warhead;

namespace Next_generationSite_27.UnionP
{
    class LevelSCP
    {
        public static IEnumerator<float> ClearPower(Scp079Role sr)
        {
            float wt = 18f;
            float i = 0f;
            while (sr != null)
            {
                i += 0.2f;
                if (i >= wt)
                {
                    yield break;
                }
                sr.Energy = 0;
                yield return Timing.WaitForSeconds(0.2f);
            }
        }
        public static List<SettingBase> Menu()
        {
            List<SettingBase> settings = new List<SettingBase>();
            if (Plugin.Instance.Config.Level)
            {
                settings.Add(new HeaderSetting(Plugin.Instance.Config.SettingIds[Features.LevelHeader], "等级插件"));

                settings.Add(new ButtonSetting(Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey], "一键开关核", "开核", 0.2f, "(Scp079 设施等级为5 且 游戏等级大于211级)\n使用后消耗全部电力开关核，使用后18秒内不会回复电力值",
                    onChanged: (player, SB) =>
                    { try
                        {

                            var PU = Plugin.Instance.connect.QueryUser(player.UserId);
                            var lv = PU.level;
                            if (player.Role is Scp079Role SR)
                            {
                                if (SR.Level == 5)
                                {
                                    if (lv >= 211)
                                    {
                                        if (SR.Energy >= SR.MaxEnergy - 2)
                                        {

                                            if (AlphaWarheadController.Singleton.IsLocked)
                                            {
                                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>核弹已锁定!</color>", 3), true);
                                                return;
                                            }
                                            if (AlphaWarheadController.Singleton.Info.InProgress)
                                            {
                                                SR.Energy = 0;
                                                Timing.RunCoroutine(ClearPower(SR));
                                                AlphaWarheadController.Singleton.CancelDetonation(player.ReferenceHub);
                                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>核弹已取消!</color>", 3), true);
                                                return;
                                            }
                                            else
                                            {
                                                if (!AlphaWarheadNukesitePanel.Singleton.Networkenabled)
                                                {
                                                    player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>拉杆未拉下!</color>", 3), true);
                                                    return;
                                                }
                                                if (!AlphaWarheadActivationPanel.IsUnlocked)
                                                {
                                                    player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>地表未开盖!</color>", 3), true);
                                                    return;
                                                }
                                                SR.Energy = 0;
                                                Timing.RunCoroutine(ClearPower(SR));
                                                AlphaWarheadController.Singleton.InstantPrepare();
                                                AlphaWarheadController.Singleton.StartDetonation(false,false,player.ReferenceHub);
                                                AlphaWarheadController.Singleton.IsLocked = false;
                                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>核弹已开始!</color>", 3), true);
                                                return;
                                            }

                                        }
                                        else
                                        {
                                            player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>电力不足！</color>", 3), true);
                                        }
                                    }
                                    else
                                    {
                                        player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>你没到211级！</color>", 3), true);
                                    }
                                }
                                else
                                {
                                    player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>设施等级不足5级！</color>", 3), true);
                                }
                            }
                            else
                            {
                                player.Broadcast(new Exiled.API.Features.Broadcast("<color=red>你不是SCP079！</color>", 3), true);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());

                        }
                    }));
            }
            return settings;
        }
        public static void Stopping(StoppingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
            {
                Timing.CallDelayed(0.2f, () =>
                {
                    var nuke = Plugin.MenuCache.Find(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]) as ButtonSetting;
                    var Text = Exiled.API.Features.Warhead.IsInProgress ? "关核" : "开核";
                    nuke.UpdateSetting(Text, 0.2f, filter: (p) => p.Role.Type == RoleTypeId.Scp079);

                });
            }
        }
        public static void Starting(StartingEventArgs ev)
        {
            if (Plugin.Instance.Config.Level)
            {
                Timing.CallDelayed(0.2f, () =>
                {
                    var nuke = Plugin.MenuCache.Find(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]) as ButtonSetting;
                    var Text = Exiled.API.Features.Warhead.IsInProgress ? "关核" : "开核";
                    nuke.UpdateSetting(Text, 0.2f, filter: (p) => p.Role.Type == RoleTypeId.Scp079);
                });
            }

        }
        public static void ChangingRole(ChangingRoleEventArgs ev)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                if (Plugin.Instance.Config.Level)
                {
                    SettingBase.Unregister(ev.Player, Plugin.MenuCache.Where((a) => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]));
                    var CandyList = Enum.GetValues(typeof(CandyKindID))
        .Cast<CandyKindID>()
        .Where(x => x != CandyKindID.None)
        //.Select(x => (int)x)
        .ToList();
                    var player = ev.Player;
                    var PU = Plugin.Instance.connect.QueryUser(player.UserId);
                    var lv = PU.level;
                    var bufList = new List<EffectType>()
            {
                EffectType.MovementBoost,// 20
                EffectType.DamageReduction,// 50
                EffectType.SilentWalk, // 7
            };

                    Random.InitState(lv + PU.experience + DateTime.UtcNow.Second + DateTime.UtcNow.Minute * 60 + DateTime.UtcNow.DayOfYear + DateTime.Now.Hour);
                    if (lv >= 101)
                    {
                        switch (player.RoleManager.CurrentRole.RoleTypeId)
                        {
                            case RoleTypeId.ClassD:
                            case RoleTypeId.Scientist:
                                {
                                    //Log.Debug($"ClassD/Scientist {player} Level 101,processing");
                                    if (Random.Range(0, 100) >= 50)
                                    {
                                        //Log.Debug($"ClassD/Scientist {player} Level 101,AddItem fl");
                                        player.AddItem(ItemType.Flashlight, 1);
                                    }
                                    if (Random.Range(0, 100) >= 50)
                                    {
                                        //Log.Debug($"ClassD/Scientist {player} Level 101,AddItem Radio");
                                        player.AddItem(ItemType.Radio, 1);
                                    }
                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                    player.ReferenceHub.GrantCandy(CandyList.RandomItem(), InventorySystem.Items.ItemAddReason.StartingItem);
                                    break;
                                }
                            case RoleTypeId.FacilityGuard:
                                {
                                    //Log.Debug($"FacilityGuard {player} Level 101,processing");
                                    int extraHealth = Math.Min((lv - 101) / 2, 15);
                                    player.MaxHealth += extraHealth;
                                    player.EnableEffect(EffectType.MovementBoost, 24, 12f, false);
                                    break;
                                }
                        }
                    }
                    if (lv >= 131)
                    {
                        switch (player.RoleManager.CurrentRole.RoleTypeId)
                        {
                            case RoleTypeId.Scp049:
                                {
                                    //Log.Debug($"049 {player} Level 131,processing");
                                    int extraHealth = Math.Min((lv - 131) * 4, 180);
                                    int extraShield = Math.Min((lv - 131) * 2, 90);
                                    player.MaxHealth += extraHealth;
                                    IHumeShieldedRole healthbarRole = player.RoleManager.CurrentRole as IHumeShieldedRole;
                                    player.MaxHumeShield = healthbarRole.HumeShieldModule.HsMax + extraShield;
                                    //player.ArtificialHealth = player.MaxArtificialHealth;
                                    break;
                                }
                            case RoleTypeId.Scp0492:
                                {
                                    //Log.Debug($"Scp0492 {player} Level 131,processing");
                                    int extraHealth = Math.Min((lv - 131) * 4, 180);
                                    player.MaxHealth += extraHealth;
                                    break;
                                }
                            case RoleTypeId.ClassD:
                            case RoleTypeId.Scientist:
                                {
                                    //Log.Debug($"ClassD/Scientist {player} Level 131,processing");
                                    if (Random.Range(0, 100) >= 30)
                                    {
                                        player.AddItem(ItemType.SCP500, 1);
                                    }
                                    player.AddItem(ItemType.Medkit, 2);
                                    break;
                                }
                        }
                    }
                    if (lv >= 176)
                    {
                        switch (player.RoleManager.CurrentRole.RoleTypeId)
                        {
                            case RoleTypeId.Scp173:
                                {
                                    //Log.Debug($"173 {player} Level 176,processing");
                                    int extraHealth = Math.Min((lv - 176) * 6, 210);
                                    player.MaxHealth += extraHealth;
                                    break;
                                }
                            case RoleTypeId.NtfCaptain:
                            case RoleTypeId.NtfPrivate:
                            case RoleTypeId.NtfSpecialist:
                            case RoleTypeId.NtfSergeant:
                                {
                                    //Log.Debug($"NTF {player} Level 176,processing");
                                    int extraHealth = 0;
                                    if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfCaptain) extraHealth = 12;
                                    else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfPrivate) extraHealth = 7;
                                    else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfSpecialist) extraHealth = 9;
                                    else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.NtfSergeant) extraHealth = 9;
                                    player.MaxHealth += extraHealth;
                                    player.AddItem(ItemType.Painkillers, 1);
                                    break;
                                }
                            case RoleTypeId.ChaosConscript:
                            case RoleTypeId.ChaosMarauder:
                            case RoleTypeId.ChaosRepressor:
                            case RoleTypeId.ChaosRifleman:
                                {
                                    //Log.Debug($"Chaos {player} Level 176,processing");
                                    int extraHealth = 0;
                                    if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosRepressor) extraHealth = 12;
                                    else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosRifleman) extraHealth = 7;
                                    else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosMarauder) extraHealth = 9;
                                    else if (player.RoleManager.CurrentRole.RoleTypeId == RoleTypeId.ChaosConscript) extraHealth = 9;
                                    player.MaxHealth += extraHealth;
                                    player.AddItem(ItemType.Painkillers, 1);
                                    break;
                                }
                        }
                    }
                    if (lv >= 211)
                    {
                        //Log.Debug(player.RoleManager.CurrentRole.RoleTypeId.ToString() + $" {player} Level 211,processing");

                        switch (player.RoleManager.CurrentRole.RoleTypeId)
                        {
                            case RoleTypeId.FacilityGuard:
                                {

                                    switch (bufList.RandomItem())
                                    {
                                        case EffectType.MovementBoost:
                                            player.EnableEffect(EffectType.MovementBoost, 20, 99999f, false);
                                            Log.Debug($"{player} get MovementBoost");
                                            break;
                                        case EffectType.DamageReduction:
                                            Log.Debug($"{player} get DamageReduction");
                                            player.EnableEffect(EffectType.DamageReduction, 50, 99999f, false);
                                            break;
                                        case EffectType.SilentWalk:
                                            Log.Debug($"{player} get SilentWalk");
                                            player.EnableEffect(EffectType.SilentWalk, 7, 99999f, false);
                                            break;
                                    }
                                    break;
                                }
                            case RoleTypeId.Scp106:
                                {

                                    break;
                                }
                            case RoleTypeId.Scp079:
                                {
                                    var r = player.Role as Scp079Role;

                                    FieldInfo field = typeof(Scp079DoorLockChanger).GetField("_lockCostPerSec", BindingFlags.NonPublic | BindingFlags.Instance);
                                    float original = (float)field.GetValue(r.DoorLockChanger);
                                    float adjusted = PU.level >= 220 ? original * 0.75f : original;
                                    field.SetValue(r.DoorLockChanger, adjusted);
                                    SettingBase.Unregister(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey]));
                                    
                                    SettingBase.Register(player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.Scp079NukeKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.LevelHeader]));
                                    break;
                                }
                        }
                    }
                    if (lv >= 241)
                    {
                        //Log.Debug(player.RoleManager.CurrentRole.RoleTypeId.ToString() + $" {player} Level 241,processing");
                        switch (player.RoleManager.CurrentRole.RoleTypeId)
                        {
                            case RoleTypeId.FacilityGuard:
                                {
                                    if (Random.Range(0, 100) < 35)
                                    {
                                        player.AddItem(ItemType.SCP2176, 1);
                                        Log.Debug($"{player} get SCP2176");
                                    }
                                    if (Random.Range(0, 100) < 2)
                                    {
                                        Log.Debug($"{player} get SurfaceAccessPass");
                                        player.AddItem(ItemType.SurfaceAccessPass, 1);
                                    }
                                    break;
                                }
                            case RoleTypeId.Scp096:
                            case RoleTypeId.Scp939:
                                {
                                    int extraShield = Math.Min((lv - 241) * 7, 210);
                                    IHumeShieldedRole healthbarRole = player.RoleManager.CurrentRole as IHumeShieldedRole;
                                    player.MaxHumeShield = healthbarRole.HumeShieldModule.HsMax + extraShield;
                                    break;
                                }
                        }
                    }
                    if (lv >= 270)
                    {
                        //Log.Debug(player.RoleManager.CurrentRole.RoleTypeId.ToString() + $" {player} Level 270,processing");
                        switch (player.RoleManager.CurrentRole.RoleTypeId)
                        {
                            case RoleTypeId.ClassD:
                            case RoleTypeId.Scientist:
                                {
                                    if (Random.Range(0, 100) < 15)
                                    {
                                        player.AddItem(ItemType.SCP207, 1);
                                        Log.Debug($"{player} get SCP207");
                                        if (Random.Range(0, 100) < 2)
                                        {
                                            Log.Debug($"{player} get double SCP207");
                                            player.AddItem(ItemType.SCP207, 1);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            });

        }
        //[HarmonyPatch(typeof(Scp106HumeShieldController))]
        //class OLDMANSPatch
        //{
        //    [HarmonyPatch("get_HsMax")]
        //    [HarmonyPostfix]
        //    public static void Postfix(Scp106HumeShieldController __instance, ref float __result)
        //    {
        //        var p = Player.Get(__instance.Owner);
        //        var PU = Plugin.Instance.connect.QueryUser(p.UserId);
        //        var lv = PU.level;
        //        if (lv >= 211 && Plugin.Instance.Config.Level)
        //        {
        //            int extraShield = Math.Min((lv - 211) * 5, 150);
        //            __result += extraShield;
        //        }
        //    }
        //}
        //[HarmonyPatch(typeof(DynamicHumeShieldController), nameof(DynamicHumeShieldController.HsMax), MethodType.Getter)]
        //public class Scp106HsMaxPatch
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(DynamicHumeShieldController __instance, ref float __result)
        //    {
        //        // 判断是否是 SCP-106
        //        if (!(__instance is Scp106HumeShieldController))
        //            return;

        //        var hub = __instance.Owner;
        //        var player = Player.Get(hub);
        //        if (player == null)
        //            return;

        //        var plugin = Plugin.Instance;
        //        if (plugin == null || !plugin.Config.Level)
        //            return;

        //        var pu = plugin.connect.QueryUser(player.UserId);

        //        int lv = pu.level;
        //        if (lv >= 211)
        //        {
        //            int extraShield = Math.Min((lv - 211) * 5, 150);
        //            __result += extraShield;
        //        }
        //    }
        //}
    }
}
