using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp049;
using Exiled.Events.EventArgs.Scp0492;
using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.EventArgs.Scp939;
using HarmonyLib;
using Hazards;
using InventorySystem.Items.Firearms;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP
{
    class SuperSCP
    {
        //public void TriggeringBloodlust(TriggeringBloodlustEventArgs ev)
        //{
        //    ev.Scp0492.
        //}
        public void PlacedAmnesticCloud(PlacedAmnesticCloudEventArgs ev)
        {
            if (Plugin.enableSSCP)
            {
                ev.Scp939.AmnesticCloudCooldown *= 0.6f;
            }
        }
        public void Clawed(ClawedEventArgs ev)
        {
            if (Plugin.enableSSCP)
            {
                ev.Scp939.AttackCooldown -= 0.2f;
            }
        }
        public List<Player> PatchedPlayers = new List<Player>();
        public void GainingExperience(GainingExperienceEventArgs ev)
        {
            if (Plugin.enableSSCP)
            {
                //ev.Amount *= 2;
            }
        }
        public IEnumerator<float> update()
        {
            Log.Info("SuperSCP Start!");
            Cassie.Message(Plugin.plugin.Config.EnableSuperScpBroadcast,isSubtitles:true);
            for (; ; )
            {
                if (!Plugin.enableSSCP) { break; }

                foreach (var item in Player.Enumerable.Where(x => x.IsScp))
                {
                    if (!PatchedPlayers.Contains(item))
                    {
                        switch (item.Role.Type)
                        {
                            case RoleTypeId.Scp0492:
                                {
                                    var role = item.Role as Scp0492Role;
                                    role.Velocity = role.Velocity * 1.05f;
                                    role.WalkingSpeed *= 1.05f;
                                    break;
                                }
                            case RoleTypeId.Scp049:
                                {
                                    var role = item.Role as Exiled.API.Features.Roles.Scp049Role;
                                    //role.AttackAbility.OnServerHit += AttackAbility_OnServerHit;
                                    break;
                                }
                            case RoleTypeId.Scp173:
                                {
                                    var role = item.Role as Exiled.API.Features.Roles.Scp173Role;
                                    role.SprintingSpeed *= 1.4f;
                                    break;
                                }
                        }
                        Log.Info($"Patched Player:{item}");
                        PatchedPlayers.Add(item);
                    }
                    if (item.Role is Scp0492Role scp0492Role)
                    {
                        if (AnyTargets(item.ReferenceHub, item.ReferenceHub.PlayerCameraReference))
                        {
                            item.EnableEffect(EffectType.DamageReduction, 40, 1);
                        }
                        else
                        {
                            item.DisableEffect(Exiled.API.Enums.EffectType.DamageReduction);
                        }
                    }
                    else if (item.Role is Scp079Role SR)
                    {
                        //if (SR.Level == 5)
                        //{
                        //    SR.Energy = SR.MaxEnergy;
                        //}
                    }
                }
                yield return MEC.Timing.WaitForSeconds(0.2f);
            }
        }
        bool AnyTargets(ReferenceHub owner, Transform camera)
        {
            foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
            {
                if (referenceHub.IsHuman() && !referenceHub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
                {
                    IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
                    if (fpcRole != null && VisionInformation.GetVisionInformation(owner, camera, fpcRole.FpcModule.Position, fpcRole.FpcModule.CharacterControllerSettings.Radius,45, true, true, 0, false).IsLooking)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //public void AttackAbility_OnServerHit(ReferenceHub hub)
        //{
        //    var player = Player.Get(hub);
        //    var role = player.Role as Scp049Role;
        //    Log.Info($"role.AttackAbility.Cooldown.Remaining {role.AttackAbility.Cooldown.Remaining}");
        //}

        public CoroutineHandle coroutineHandle;
        public void start()
        {
            try
            {
                // 启动协程
                if (coroutineHandle.IsValid)
                {
                    MEC.Timing.KillCoroutines(coroutineHandle);
                }
                coroutineHandle = MEC.Timing.RunCoroutine(update());

                // 确保 Plugin.harmony 实例存在
                if (Plugin.harmony == null)
                {
                    Log.Error("[SuperSCP] Harmony 实例为 null");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SuperSCP] start() 方法出错: {ex.Message}");
                Log.Error($"[SuperSCP] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        public void stop()
        {
            MEC.Timing.KillCoroutines(coroutineHandle);
            Plugin.enableSSCP = false;
            try
            {
                // 启动协程
                if (coroutineHandle.IsValid)
                {
                    MEC.Timing.KillCoroutines(coroutineHandle);
                }
                // 确保 Plugin.harmony 实例存在
                if (Plugin.harmony == null)
                {
                    Log.Error("[SuperSCP] Harmony 实例为 null");
                    return;
                }
                //Log.Info("[SuperSCP] 成功应用所有 Harmony 补丁");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SuperSCP] start() 方法出错: {ex.Message}");
                Log.Error($"[SuperSCP] 堆栈跟踪: {ex.StackTrace}");
            }
        }


        public Dictionary<Player, CoroutineHandle> cs = new Dictionary<Player, CoroutineHandle>();
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (PatchedPlayers.Contains(ev.Player))
            {
                PatchedPlayers.Remove(ev.Player);
            }
            if (cs.ContainsKey(ev.Player))
            {
                Timing.KillCoroutines(cs[ev.Player]);
                cs.Remove(ev.Player);
            }

        }
        public void Died(DiedEventArgs ev)
        {
            if (Plugin.enableSSCP)
            {
                if (ev.DamageHandler.Base is Scp096DamageHandler DH)
                {
                    var player = Player.Get(DH.Attacker);
                    player.Heal(35);
                    player.HumeShield = Math.Min(player.MaxHumeShield, player.HumeShield + 30);
                }
            }
        }
        public static void OnHurting(HurtingEventArgs ev)
        {

        }

        // Token: 0x0600000E RID: 14 RVA: 0x000021E8 File Offset: 0x000003E8


        // Token: 0x06000010 RID: 16 RVA: 0x00002228 File Offset: 0x00000428
        public void Hurting(HurtingEventArgs ev)
        {

            bool flag = ev.DamageHandler == null;
            if (!flag)
            {
                bool flag2 = ev.DamageHandler.Type == DamageType.Scp207;
                if (flag2)
                {
                    ev.IsAllowed = false;
                }
                bool flag3 = ev.DamageHandler.Type == DamageType.Poison;
                if (flag3)
                {
                    ev.IsAllowed = false;
                }
            }
            if (Plugin.enableSSCP)
            {
                if (ev.DamageHandler.Base is Scp939DamageHandler Doghandler)
                {
                    Doghandler.Damage += 5;
                    ev.DamageHandler = new Exiled.API.Features.DamageHandlers.CustomDamageHandler(ev.Player, Doghandler);
                }
                else if (ev.DamageHandler.Base is JailbirdDamageHandler Jbhandler && ev.Player.Role is Scp0492Role)
                {

                    Jbhandler.Damage /= 2;
                    ev.DamageHandler = new Exiled.API.Features.DamageHandlers.CustomDamageHandler(ev.Player, Jbhandler);

                }
                else if (ev.Player.Role is Scp096Role role && role.RageState != PlayerRoles.PlayableScps.Scp096.Scp096RageState.Enraged)
                {
                    if (ev.DamageHandler.Base is MicroHidDamageHandler HIDhandler)
                    {
                        HIDhandler.Damage *= 0.6f;
                        ev.DamageHandler = new Exiled.API.Features.DamageHandlers.CustomDamageHandler(ev.Player, HIDhandler);

                    }
                    if (ev.DamageHandler.Base is JailbirdDamageHandler Jbhandler1)
                    {
                        Jbhandler1.Damage *= 0.6f;

                        ev.DamageHandler = new Exiled.API.Features.DamageHandlers.CustomDamageHandler(ev.Player, Jbhandler1);

                    }
                    if (ev.DamageHandler.Base is FirearmDamageHandler FDhandler)
                    {
                        if (FDhandler.Firearm.ItemTypeId == ItemType.ParticleDisruptor)
                        {
                            ev.Amount *= 0.6f;
                            ev.DamageHandler = new Exiled.API.Features.DamageHandlers.CustomDamageHandler(ev.Player, FDhandler);
                        }

                    }

                }
                else if (ev.Player.Role.Type == RoleTypeId.Scp173)
                {
                    ev.DamageHandler.Damage = Math.Min(ev.DamageHandler.Damage, 225);

                }
            }
        }
    }
}
