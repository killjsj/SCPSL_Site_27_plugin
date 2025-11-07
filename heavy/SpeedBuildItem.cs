using Decals;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Footprinting;
using InventorySystem.Items.Firearms.Modules;
using MapGeneration;
using MapGeneration.StaticHelpers;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using Respawning;
using Respawning.Waves;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.Scannner;
using Object = UnityEngine.Object;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class SpeedBuilditem : BaseClass
    {

        public static uint SpeedBuildItemID = 5096;
        [CustomItem(ItemType.GrenadeFlash)]
        public class SpeedBuildItem : CustomItem
        {

            public static CustomItem instance { get; private set; }
            public override uint Id { get; set; } = SpeedBuildItemID;
            public override string Name { get; set; } = "速凝掩体";
            public override string Description { get; set; }
            public override Vector3 Scale { get => new Vector3(0.5f, 0.5f, 0.5f); set => base.Scale = value; }
            public override float Weight { get; set; } = 1f;
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
            {
            };
            public override void Destroy()
            {
                IUnsubscribeEvents();
                base.Destroy();
            }
            public override void Init()
            {
                ISubscribeEvents();
                instance = this;
                base.Init();
            }
            protected void ISubscribeEvents()
            {
                Exiled.Events.Handlers.Player.ThrownProjectile += OnDroppedItem;
            }
            protected void IUnsubscribeEvents()
            {
                Exiled.Events.Handlers.Player.ThrownProjectile -= OnDroppedItem;
            }
            public void OnDroppedItem(ThrownProjectileEventArgs ev)
            {
                if (this.Check(ev.Pickup))
                {
                    ev.Pickup.Base.gameObject.AddComponent<Builder>().init(ev.Pickup, ev.Player.Rotation, ev.Player);
                }
            }
        }

        class Builder : MonoBehaviour
        {
            public Pickup pickup = null;
            public Quaternion playerRotation;
            public Player Owner = null;
            void OnCollisionEnter(Collision collision)
            {
                if (Owner == null)
                {
                    Log.Error("Owner is null!");
                }

                if (pickup == null)
                {
                    Log.Error("pickup is null!");
                }

                if (collision == null)
                {
                    Log.Error("wat");
                }

                if (!collision.collider)
                {
                    Log.Error("water");
                }

                if (collision.collider.gameObject == null)
                {
                    Log.Error("pepehm");
                }

                if (!(collision.collider.gameObject == Owner.GameObject) && (Player.Get(collision.gameObject) != Owner) && !Spawned)
                {
                    Spawned = true;
                    Vector3 wallNormal = collision.contacts[0].normal;
                    Quaternion bunkerRotation = CalculateBunkerRotation(wallNormal, playerRotation);

                    CreateBunker(collision.contacts[0].point, bunkerRotation);
                    pickup.Destroy();
                    Destroy(this);
                }
            }
            public bool Spawned = false;
            public void init(Pickup Pickup, Quaternion rotation, Player player)
            {
                playerRotation = rotation;
                pickup = Pickup;
                Owner = player;
            }

            private Quaternion CalculateBunkerRotation(Vector3 wallNormal, Quaternion playerRot)
            {
                Vector3 playerForward = playerRot * Vector3.forward;
                Vector3 playerRight = playerRot * Vector3.right;
                Vector3 projectedForward = Vector3.ProjectOnPlane(playerForward, wallNormal).normalized;
                Vector3 projectedRight = Vector3.ProjectOnPlane(playerRight, wallNormal).normalized;
                if (projectedForward.magnitude < 0.1f)
                {
                    projectedForward = Vector3.ProjectOnPlane(Vector3.forward, wallNormal).normalized;
                    projectedRight = Vector3.ProjectOnPlane(Vector3.right, wallNormal).normalized;
                }

                // 创建旋转：向前方向是投影后的玩家方向，向上方向是墙面法线
                return Quaternion.LookRotation(projectedForward, wallNormal);
            }
        }



        public static void CreateBunker(Vector3 pos, Quaternion rot)
        {
            Primitive p = Primitive.Get(Object.Instantiate(Primitive.Prefab));
            p.Position = pos;
            p.Base.NetworkPrimitiveType = PrimitiveType.Cube;
            p.Rotation = rot;
            p.Scale = new Vector3(2.3f, 2.3f, 0.3f);
            p.Color = Color.gray;
            p.Collidable = true;
            p.Visible = true;
            foreach (var item in p.GameObject.GetComponentsInChildren<Transform>())
            {
                item.gameObject.layer = LayerMask.GetMask("Glass");
            }
            p.GameObject.layer = LayerMask.GetMask("Glass");
            p.Spawn();

            var BW = p.GameObject.AddComponent<bunker>();
            BW.Health = 200;
        }
        public static RaycastHit CreateRaycastHit(Vector3 from, Vector3 to)
        {
            RaycastHit hit;
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);

            if (Physics.Raycast(from, direction, out hit, distance))
            {
                return hit;
            }

            // 如果没有命中，可以手动设置一些值
            hit.point = to;
            hit.normal = Vector3.up;
            hit.distance = distance;
            // 其他字段需要根据实际情况设置

            return hit;
        }
        public class bunker : NetworkBehaviour, IDestructible, IBlockStaticBatching
        {
            public uint NetworkId
            {
                get
                {
                    return base.netId;
                }
            }

            // Token: 0x17000016 RID: 22
            // (get) Token: 0x06000043 RID: 67 RVA: 0x00002B88 File Offset: 0x00000D88
            public Vector3 CenterOfMass
            {
                get
                {
                    return base.transform.position;
                }
            }
            private void ServerSendImpactDecal(RaycastHit hit, Vector3 origin, DecalPoolType decalType, ImpactEffectsModule impactEffectsModule)
            {
                typeof(ImpactEffectsModule).GetMethod("ServerSendImpactDecal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(impactEffectsModule, new object[] { hit, origin, decalType });
            }
            public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
            {
                AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
                if (attackerDamageHandler == null)
                {
                    this.ServerDamageWindow(damage);
                    return true;
                }
                if (!this.CheckDamagePerms(attackerDamageHandler.Attacker.Role))
                {
                    return false;
                }
                this.LastAttacker = attackerDamageHandler.Attacker;
                Player attacker = Player.Get(attackerDamageHandler.Attacker);
                this.ServerDamageWindow(damage);
                if ((handler is MicroHidDamageHandler)) return true;
                if (attackerDamageHandler.Attacker.Hub.inventory.CurItem != null)
                {
                    if (attacker.CurrentItem is Exiled.API.Features.Items.Firearm firearm)
                    {
                        {
                            if (firearm.HitscanHitregModule is HitscanHitregModuleBase hitscan)
                            {
                                var mod = (ImpactEffectsModule)typeof(HitscanHitregModuleBase).GetField("_impactEffectsModule", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(hitscan);
                                if (mod != null)
                                {
                                    var r = CreateRaycastHit(attackerDamageHandler.Attacker.Hub.GetPosition(), pos);
                                    ServerSendImpactDecal(r, attacker.Position, DecalPoolType.Bullet, mod);
                                }
                            }
                        }
                    }
                }
                return true;
            }
            private void Update()
            {
                if (!this.IsBroken || this._prevStatus)
                {
                    return;
                }
                base.StartCoroutine(this.BreakWindow());
                this._prevStatus = true;
            }

            // Token: 0x0600004B RID: 75 RVA: 0x00002D1B File Offset: 0x00000F1B
            private IEnumerator BreakWindow()
            {
                GameObject.Destroy(base.gameObject);
                yield break;
            }

            // Token: 0x0600004C RID: 76 RVA: 0x00002D2C File Offset: 0x00000F2C
            private bool CheckDamagePerms(RoleTypeId roleType)
            {
                PlayerRoleBase playerRoleBase;
                return !this._preventScpDamage || (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(roleType, out playerRoleBase) && playerRoleBase.Team > Team.SCPs);
            }

            // Token: 0x0600004D RID: 77 RVA: 0x00002D58 File Offset: 0x00000F58
            [ServerCallback]
            private void ServerDamageWindow(float damage)
            {
                if (!NetworkServer.active)
                {
                    return;
                }
                this.Health -= damage;
                if (this.Health <= 0f)
                {
                    this.NetworkIsBroken = true;
                }
            }
            public bool NetworkIsBroken
            {
                get
                {
                    return this.IsBroken;
                }
                set
                {
                    this.IsBroken = value;
                }
            }
            public Footprint LastAttacker;
            public float Health = 30f;
            public bool IsBroken;
            private bool _preventScpDamage = false;
            private bool _prevStatus;
        }

        public override void Init()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStart;
            //throw new NotImplementedException();
        }
        public static void OnRoundStart()
        {
        }
       
    }
}
