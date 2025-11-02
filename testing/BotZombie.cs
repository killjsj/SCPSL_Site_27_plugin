using CommandSystem.Commands.RemoteAdmin.Dummies;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using GameObjectPools;
using InventorySystem.Items.Firearms.Extensions;
using MapGeneration;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using Org.BouncyCastle.Tls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Objects;
using RelativePositioning;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Next_generationSite_27.UnionP
{
    class BetterZombie
    {
        public static BetterZombie Create(Player Owner)
        {
            var p = Npc.Spawn("Zombie", RoleTypeId.Scp0492, Owner.Position);
            p.RoleManager.ServerSetRole(RoleTypeId.Scp0492,RoleChangeReason.Died);
            p.Position = Owner.Position;
            return new BetterZombie(p, Owner);
            
        }
        public Player CurrentTarget { get; set; }
        public Npc Zombie { get; set; }
        public Player Owner { get; set; }
        public Dictionary<Player,float> Hatreds = new Dictionary<Player,float>();
        public float tick = 0.2f;
        public bool tracking = false;
        public float LockIn = 1f;
        public float DoorOpenRange = 2f;
        public float BiteRange = 2f;
        public float LockOut = 40f;
        public BetterZombie(Npc zombie, Player owner)
        {
            Zombie = zombie;
            Owner = owner;
            Timing.CallDelayed(0.05f, ()=>{
                Timing.RunCoroutine(Update());
            }
            );
        }
        //
        // 摘要:
        //     Follow a specific player.
        //
        // 参数:
        //   player:
        //     the Player to follow.
        public void Follow(Player player)
        {
            ((!Zombie.GameObject.TryGetComponent<PlayerFollower>(out var component)) ? Zombie.GameObject.AddComponent<PlayerFollower>() : component).Init(player.ReferenceHub);
        }

        //
        // 摘要:
        //     Follow a specific player.
        //
        // 参数:
        //   player:
        //     the Player to follow.
        //
        //   maxDistance:
        //     the max distance the npc will go.
        //
        //   minDistance:
        //     the min distance the npc will go.
        //
        //   speed:
        //     the speed the npc will go.
        public void Follow(Player player, float maxDistance, float minDistance, float speed = 30f)
        {
            ((!Zombie.GameObject.TryGetComponent<PlayerFollower>(out var component)) ? Zombie.GameObject.AddComponent<PlayerFollower>() : component).Init(player.ReferenceHub, maxDistance, minDistance, speed);
        }
        public IEnumerator<float> Update() {
            while (Zombie.Role.Type == RoleTypeId.Scp0492) {
                // Lock instance
                try
                {
                    if (!tracking)
                    {
                            Follow(Owner);
                        foreach (var item in Player.Enumerable.Where(x => !x.IsScp))
                        {
                            if (Vector3.Distance(item.Position, Zombie.Position) <= 10f)
                            {
                                if (Hatreds.TryGetValue(item, out var h))
                                {
                                    Hatreds[item] += tick;

                                }
                                else
                                {
                                    Hatreds[item] = tick;
                                }
                            }
                            if (Hatreds.TryGetValue(item, out var n))
                            {
                                if (n > LockIn)
                                {
                                    Hatreds[item] = LockIn;
                                    tracking = true;
                                    CurrentTarget = item;
                                    var r = Zombie.Role as Scp0492Role;
                                    Follow(item, LockOut + 10f, 1f, r.MovementSpeed);
                                }
                            }
                        }
                        if (Zombie.Health < Zombie.MaxHealth)
                        {
                            var rag = Ragdoll.List.FirstOrDefault(x => x.Room == Zombie.CurrentRoom && !x.IsConsumed);
                            if (rag != null)
                            {
                                var r = Zombie.Role as Scp0492Role;
                                Vector3 position = Zombie.Position;
                                Vector3 dir = rag.Position - position;
                                Vector3 vector = Time.deltaTime * r.MovementSpeed * dir.normalized;
                                r.FirstPersonController.FpcModule.Motor.ReceivedPosition = new RelativePosition(position + vector);
                                var Zr = Zombie.Role.Base as ZombieRole;
                                Zr.LookAtPoint(rag.Position);
                                {
                                    MethodInfo serverSendRpcMethod = typeof(KeySubroutine<ZombieRole>).GetMethod(
                                    "OnKeyDown",
                                    BindingFlags.NonPublic | BindingFlags.Instance,
                                    null,
                                    new Type[] { },
                                    null
                                    );
                                    if (serverSendRpcMethod != null)
                                    {
                                        serverSendRpcMethod.Invoke(r.ConsumeAbility, new object[] { });
                                    }

                                }
                                
                            }
                        }

                    }
                    else
                    {
                        if (CurrentTarget == null)
                        {
                            tracking = false;
                            Follow(Owner);
                            continue;
                        }
                        if (Vector3.Distance(CurrentTarget.Position, Zombie.Position) > LockOut)
                        {
                            tracking = false;
                            Hatreds.Remove(CurrentTarget);
                            CurrentTarget = null;
                            Follow(Owner);
                            continue;
                        }
                        if (Vector3.Distance(CurrentTarget.Position, Zombie.Position) <= BiteRange)
                        {
                                var Zr = Zombie.Role.Base as ZombieRole;
                                Zr.LookAtPoint(CurrentTarget.Position);
                            var r = Zombie.Role as Scp0492Role;
                            MethodInfo serverSendRpcMethod = typeof(KeySubroutine<ZombieRole>).GetMethod(
    "OnKeyDown",
    BindingFlags.NonPublic | BindingFlags.Instance,
    null,
    new Type[] { },
    null
);
                            if (serverSendRpcMethod != null)
                            {
                                serverSendRpcMethod.Invoke(r.AttackAbility, new object[] { });
                            }
                            //if (r != null && r.AttackAbility.Cooldown.IsReady)
                            //{
                            //    r.AttackAbility.Cooldown.Trigger(r.AttackCooldown);
                            //    CurrentTarget.Hurt(new Scp049DamageHandler(Zombie.ReferenceHub, r.AttackDamage, Scp049DamageHandler.AttackType.Scp0492));
                            //}
                        }
                        if (Zombie.CurrentRoom != null)
                        {
                            foreach (var item in Zombie.CurrentRoom.Doors)
                            {
                                if (!item.IsLocked && !item.IsMoving && !item.IsOpen)
                                {
                                    if (Vector3.Distance(item.Position, Zombie.Position) <= DoorOpenRange)
                                    {
                                        if (item.PermissionsPolicy.CheckPermissions(Zombie.ReferenceHub, item.Base, out _) || !item.IsKeycardDoor)
                                        {
                                            item.IsOpen = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Zombie.Destroy();
                    Log.Info(ex.ToString());
                    Log.Info(ex.StackTrace);
                    yield break;
                }
                yield return Timing.WaitForSeconds(tick);
            }
           Zombie.Destroy();
        }
    }
}
// i dont want to do this