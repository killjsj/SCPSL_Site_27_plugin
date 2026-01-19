using Decals;
using DrawableLine;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Footprinting;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using Next_generationSite_27.UnionP.heavy.role;
using Org.BouncyCastle.Asn1.X509;
using PlayerRoles;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using Time = UnityEngine.Time;

namespace Next_generationSite_27.UnionP
{
    class Turret : MonoBehaviour
    {
        public static Turret Create(Vector3 pos,Vector3 rotate,Player Onwer)
        {
            var sk = new SerializableSchematic
            {
                SchematicName = "Turret",
                Position = pos,
                Rotation = rotate,
            };

            GameObject skg = sk.SpawnOrUpdateObject();
            var turret = skg.AddComponent<Turret>();
            var SO = skg.gameObject.GetComponent<SchematicObject>();
            foreach (var comp in SO.AttachedBlocks)
            {
                switch(comp.name)
                {
                    case "shotpoint":
                        turret.shotPoint = comp;
                        break;
                    case "GunY":
                        turret.GunY = comp;
                        break;
                    case "GunZ":
                        turret.GunZ = comp;
                        break;
                }
            }
            turret.turretModel = SO;
            turret.Onwer = Onwer;
            turret.init = true;
            return turret;
        }
        public Player Onwer;
        public ItemType Type;
        public SchematicObject turretModel;
        public GameObject shotPoint;
        public GameObject GunY;
        public GameObject GunZ;
        protected Ray ForwardRay
        {
            get
            {
                Transform playerCameraReference = shotPoint.transform;
                return new Ray(playerCameraReference.position, playerCameraReference.forward);
            }
        }

        public HitscanResult ResultNonAlloc = new();
        public static void SpawnDecal(Vector3 position, Vector3 startPosition, DecalPoolType type = DecalPoolType.Blood)
        {
            RelativePosition hitPoint = new RelativePosition(position);
            RelativePosition startRaycastPoint = new RelativePosition(startPosition);


        }
        public void CreateTracer(RaycastHit Info)
        {
            new DrawableLineMessage(0.6f, Color.white, new Vector3[2] { shotPoint.transform.position, Info.point }).SendToAuthenticated();

        }
        protected void Awake()
        {
            PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
        }

        // Token: 0x0600532B RID: 21291 RVA: 0x00119652 File Offset: 0x00117852
        protected void OnDestroy()
        {
            PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
        }
        private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevrole, PlayerRoleBase newrole)
        {
            if (!NetworkServer.active)
            {
                return;
            }
            if (Onwer.ReferenceHub != hub)
            {
                return;
            }
            turretModel.Destroy();
        }
        public Player Locking;
        protected Ray RandomizeRay(Ray ray, float angle)
        {
            float d = Mathf.Max(UnityEngine.Random.value, UnityEngine.Random.value);
            Vector3 a = UnityEngine.Random.insideUnitSphere * d;
            ray.direction = Quaternion.Euler(angle * a) * ray.direction;
            return ray;
        }
        protected RaycastHit ServerAppendPrescan(Ray targetRay, HitscanResult toAppend)
        {
            float maxDistance = 60;
            RaycastHit hit;
            if (!Physics.Raycast(targetRay, out hit, maxDistance, HitscanHitregModuleBase.HitregMask))
            {
                return default;
            }
            IDestructible destructible;
            if (!hit.collider.TryGetComponent<IDestructible>(out destructible))
            {
                toAppend.Obstacles.Add(new HitRayPair(targetRay, hit));
                return hit;
            }
            toAppend.Destructibles.Add(new DestructibleHitPair(destructible, hit, targetRay));
            return hit;
        }
        public float NextFire = 0f;
        private float c_firerate = 0f;
        public float firerate {get
            {
                if(c_firerate == 0f && Type.GetPickupBase() is InventorySystem.Items.Firearms.FirearmPickup firearmPickup)
                {
                    var item = firearmPickup.Template;
                    var fireM = item.Modules.FirstOrDefault(x => x is AutomaticActionModule) as AutomaticActionModule;
                    //if (fireM != null)
                    //{
                    //    c_firerate = fireM.BaseFireRate;
                    //}
                    //else
                    {
                        c_firerate = 0.5f;
                    }
                }
                return c_firerate;
            }
        }
        public bool init = false;
        public void Update()
        {
            if (!init || Onwer == null) return;
            Lock();
            if (Locking != null && GunY != null && GunZ != null)
            {
                Vector3 targetPosition = Locking.Position;

                // 推荐用炮口位置作为起点（更精准），这里用GunZ的位置
                Vector3 gunPosition = GunZ.transform.position;

                Vector3 toTarget = targetPosition - gunPosition;

                if (toTarget.sqrMagnitude < 0.01f) return; // 太近避免错误

                // ---------- 1. 控制 GunY 的 Yaw（水平转向） ----------
                Vector3 flatDirection = new Vector3(toTarget.x, 0, toTarget.z).normalized;

                Quaternion yawRotation = Quaternion.LookRotation(flatDirection, Vector3.forward);

                // 只修改 GunY 的旋转（它的子物体Cube和GunZ都会跟着转）
                GunY.transform.rotation = Quaternion.Slerp(GunY.transform.rotation, yawRotation, Time.deltaTime * 10f);

                // ---------- 2. 控制 GunZ 的 Pitch（俯仰） ----------
                float horizontalDistance = flatDirection.magnitude > 0 ? new Vector3(toTarget.x, 0, toTarget.z).magnitude : 0.01f;
                float pitchAngle = Mathf.Atan2(toTarget.y, horizontalDistance) * Mathf.Rad2Deg;

                // GunZ 是 Cube 的子物体，所以我们修改 GunZ 的 localRotation（相对于Cube）
                // 根据你之前说的“炮口沿Y轴”，默认水平时GunZ本地X=0

                // 大多数Y轴炮口模型需要负号才能向上抬
                float targetPitch = -pitchAngle;   // 如果目标在上方时炮口向下，改成 +pitchAngle

                Vector3 currentLocalAngles = GunZ.transform.localEulerAngles;
                float smoothPitch = Mathf.LerpAngle(currentLocalAngles.x, targetPitch, Time.deltaTime * 10f);

                // 只改X轴，保持Y和Z为0（防止累计误差）
                GunZ.transform.localEulerAngles = new Vector3(smoothPitch, 0, 0);
            }
            if (Locking != null && Time.time >= NextFire)
            {
                NextFire = Time.time + firerate;

                Ray baseRay = ForwardRay;
                Ray spreadRay = RandomizeRay(baseRay, 3f);

                ResultNonAlloc.Clear();
                var h = ServerAppendPrescan(spreadRay, ResultNonAlloc);

                foreach (var target in ResultNonAlloc.Destructibles)
                {
                    ServerApplyDestructibleDamage(target, ResultNonAlloc);
                    Onwer.ShowHitMarker();
                }
                CreateTracer(h);
                
            }
        }
        public float DistanceToLock = 25f;

        void Lock()
        {
            // 如果当前锁定目标有效且在范围内，保持锁定
            if (Locking != null && Locking.IsAlive &&
                Vector3.Distance(shotPoint.transform.position, Locking.Position) <= DistanceToLock)
            {
                return; // 不需要重新搜索
            }

            // 否则搜索新的最近敌人
            Locking = null;
            float closestDist = DistanceToLock;

            foreach (var player in Player.List)
            {
                if (player == null || !player.IsAlive || player == Onwer ||
                    player.Role.Team == Onwer.Role.Team ||
                    !HitboxIdentity.IsDamageable(Onwer.ReferenceHub, player.ReferenceHub))
                    continue;

                float dist = Vector3.Distance(shotPoint.transform.position, player.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    Locking = player;
                }
            }
        }
        protected virtual void ServerApplyDestructibleDamage(DestructibleHitPair target, HitscanResult result)
        {
            if (Type.GetPickupBase() is InventorySystem.Items.Firearms.FirearmPickup firearmPickup)
            {
                var item = firearmPickup.Template;
                var fireM = item.Modules.FirstOrDefault(x => x is HitscanHitregModuleBase) as HitscanHitregModuleBase;
                var damage = fireM.BaseDamage;
                    PlayerStatsSystem.AttackerDamageHandler handler = new PlayerStatsSystem.DisruptorDamageHandler(null,shotPoint.transform.forward, damage);
                handler.Attacker = Onwer.Footprint;
                IDestructible destructible = target.Destructible;
                HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
                if (hitboxIdentity != null && !hitboxIdentity.TargetHub.IsAlive())
                {
                    result.RegisterDamage(destructible, damage, handler);
                    return;
                }
                Vector3 point = target.Hit.point;
                if (!destructible.Damage(damage, handler, point))
                {
                    return;
                }
                result.RegisterDamage(destructible, damage, handler);
                CreateTracer(target.Hit);
            }
        }
    }
    [CustomItem(ItemType.Coin)]
    class TurretItem : CustomItemPlus
    {
        public static uint TurretId = 9178;
        public override uint Id { get => TurretId; set => TurretId = value; }
        public override string Name { get => "炮塔"; set { } }
        public override string Description { get => "使用后在5米内表面创建炮台"; set { } }
        public override float Weight { get =>11; set { } }
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();
        public override void Init()
        {
            base.Init();
            this.Type = ItemType.Coin;
        }
        protected override void OnUsed(Player player, Item item)
        {
            base.OnUsed(player, item);
            if(Physics.Raycast(player.CameraTransform.position + player.CameraTransform.forward * 0.2f, player.CameraTransform.forward, out RaycastHit hit, 5f))
            {
                Turret.Create(hit.point + hit.normal * 0.1f, hit.normal, player);
                return;
            }
            Turret.Create(player.CameraTransform.position + player.CameraTransform.forward * 2f, Vector3.zero, player);
        }
    }
}
