using AdminToys;
using Decals;
using DrawableLine;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.Events.Handlers;
using Footprinting;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using LabApi.Features.Wrappers;
using MapGeneration.StaticHelpers;
using Mirror;
using Next_generationSite_27.UnionP.heavy.role;
using Org.BouncyCastle.Asn1.X509;
using PlayerRoles;
using PlayerStatsSystem;
using ProjectMER.Commands.Modifying.Rotation;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using RelativePositioning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using Item = Exiled.API.Features.Items.Item;
using Player = Exiled.API.Features.Player;
using Time = UnityEngine.Time;

namespace Next_generationSite_27.UnionP
{
    public class Turret : MonoBehaviour
    {
        public static Turret Create(Vector3 pos, Vector3 rotate, Player Onwer)
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
                switch (comp.name)
                {
                    case "shotpoint":
                        turret.shotPoint = comp;
                        break;
                    case "Damager":
                        turret.Damager = comp;
                        comp.AddComponent<damageReceiver>().turret = turret;
                        break;
                    case "Text":
                        turret.Text = comp.GetComponent<AdminToys.TextToy>();
                        break;
                    case "point":
                        turret.point = comp;
                        break;
                }
            }
            turret.turretModel = SO;
            turret.Onwer = Onwer;
            turret.init = true;
            turret.hp = turret.maxhp;
            try
            {
                turret.Type = ItemType.GunA7;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            SO.Rotation = Quaternion.Euler(rotate);
            return turret;
        }
        public Player Onwer;
        public ItemType Type
        {
            get => _Type;
            set { _Type = value; itemBase = CreateOnwerItem(); c_firerate = 0f;
                handler = new PlayerStatsSystem.FirearmDamageHandler(firearm: (itemBase as InventorySystem.Items.Firearms.Firearm), 0, 1);
                 ammoLeft = totalAmmo;
            }
        }
        public ItemBase itemBase { get; private set; }
        private ItemType _Type;
        public SchematicObject turretModel;
        public GameObject Damager;
        public AdminToys.TextToy Text;
        public GameObject shotPoint;
        public GameObject point;
        protected Ray ForwardRay
        {
            get
            {
                Transform playerCameraReference = shotPoint.transform;
                return new Ray(playerCameraReference.position, playerCameraReference.forward);
            }
        }

        public HitscanResult ResultNonAlloc = new();
        public void SpawnDecal(Vector3 position, Vector3 startPosition, DecalPoolType type = DecalPoolType.Blood)
        {
            RelativePosition hitPoint = new RelativePosition(position);
            RelativePosition startRaycastPoint = new RelativePosition(startPosition);
            ModularAutosyncItem autoItem = itemBase as ModularAutosyncItem;
            for (byte b = 0; b < autoItem.AllSubcomponents.Length; b++)
            {
                if (autoItem.AllSubcomponents[b] is ImpactEffectsModule)
                {
                    using (new AutosyncRpc(autoItem.ItemId, out var writer))
                    {
                        writer.WriteByte(b);
                        writer.WriteSubheader(ImpactEffectsModule.RpcType.ImpactDecal);
                        writer.WriteByte((byte)type);
                        writer.WriteRelativePosition(hitPoint);
                        writer.WriteRelativePosition(startRaycastPoint);
                        return;
                    }
                }
            }
        }
        public void SpawnTrace(RaycastHit Info, Vector3 startPosition, Team team = Team.OtherAlive)
        {
            ModularAutosyncItem autoItem = itemBase as ModularAutosyncItem;
            if (autoItem == null)
            {
                return;
            }
            RelativePosition hitPoint = new RelativePosition(Info.point);
            RelativePosition startRaycastPoint = new RelativePosition(startPosition);
            for (byte b = 0; b < autoItem.AllSubcomponents.Length; b++)
            {
                if (autoItem.AllSubcomponents[b] is ImpactEffectsModule i)
                {
                    using (new AutosyncRpc(autoItem.ItemId, out var writer))
                    {
                        writer.WriteByte(b);

                        writer.WriteSubheader(ImpactEffectsModule.RpcType.TracerDefault);
                        
                        writer.WriteRelativePosition(hitPoint);
                        writer.WriteRelativePosition(startRaycastPoint);
                        writer.WriteByte((byte)Onwer.Role.Team);
                        return;
                    }
                }
            }
        }
        public void CreateTracer(RaycastHit Info)
        {
            SpawnTrace(Info, shotPoint.transform.position, Onwer.Role.Team);
            SpawnDecal(Info.point, shotPoint.transform.position, DecalPoolType.Bullet);
            new DrawableLineMessage(0.6f, Color.white, new Vector3[2] { shotPoint.transform.position, Info.point }).SendToAuthenticated();

        }
        protected void Awake()
        {
            PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
        }
        public ItemBase CreateOnwerItem()
        {
            var itemSerial = ItemSerialGenerator.GenerateNext();
            ItemBase itemBase2 = Onwer.Inventory.CreateItemInstance(new ItemIdentifier(_Type, itemSerial), true);
            if (itemBase2 == null)
            {
                return null;
            }
            return itemBase2;
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
        RaycastHit hit;

        protected RaycastHit ServerAppendPrescan(Ray targetRay, HitscanResult toAppend)
        {
            float maxDistance = 60;
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
        public float hp = 0;
        public float maxhp = 60f;
        private float c_firerate = 0f;
        public int ammoLeft = 0;
        public float firerate {
            get
            {

                if (c_firerate == 0f && itemBase is InventorySystem.Items.Firearms.Firearm firearmPickup)
                {
                    try
                    {
                        var item = firearmPickup;
                        var fireM = item.Modules.FirstOrDefault(x => x is AutomaticActionModule) as AutomaticActionModule;
                        if (fireM != null)
                        {
                            c_firerate = 1f / fireM.BaseFireRate;
                            Log.Info($"Turret firerate set to {fireM.BaseFireRate}({c_firerate:F2}s,1 shot)");
                        }
                        else
                        {
                            c_firerate = 0.3f;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        c_firerate = 0.3f;
                    }
                }
                return c_firerate;
            }
        }
        public bool init = false;
        public void OnDamaged(float damage, PlayerStatsSystem.DamageHandlerBase handler, Vector3 pos)
        {
            hp -= damage;
            if (hp <= 0f) { 
                turretModel.Destroy();
            }
        }
        public float reloadSecond = 5f;
        public int totalAmmo = 60;
        public FullAutoRateLimiter limiter = new();
        public bool reloading = false;
        public void Update()
        {
            if (!init || Onwer == null) return;
            limiter.Update();

        }
        public void FixedUpdate()
        {
            if (!init || Onwer == null) return;
            if (Locking != null && point != null)
            {
                Vector3 direction = (Locking.Position - point.transform.position).normalized;
                direction.x = Mathf.Clamp(direction.x, -45f, 45f);
                point.transform.rotation = Quaternion.LookRotation(direction);
            }
            Text.TextFormat = $"<color={(ammoLeft <= 0 ? "red" : "green")}>{ammoLeft}/{totalAmmo} {hp}hp";

            if (limiter.Ready)
            {
                if (reloading)
                {
                    ammoLeft = totalAmmo;
                    reloading = false;
                }
                Lock();

                if (Locking != null)
                {
                    if (ammoLeft <= 0)
                    {
                        limiter.Trigger(reloadSecond);
                        reloading = true;
                        return;
                    }
                    limiter.Trigger(firerate);
                    Ray baseRay = ForwardRay;
                    baseRay = RandomizeRay(baseRay, 3f);

                    ResultNonAlloc.Clear();
                    var h = ServerAppendPrescan(baseRay, ResultNonAlloc);

                    foreach (var target in ResultNonAlloc.Destructibles)
                    {
                        ServerApplyDestructibleDamage(target, ResultNonAlloc);
                    }
                    CreateTracer(h);
                    ammoLeft--;

                }
            }
        }
        public float DistanceToLock = 15f;

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

            foreach (var player in Player.Enumerable)
            {
                if (player == null || !player.IsAlive || player == Onwer ||
                    player.Role.Team == Onwer.Role.Team ||
                    !HitboxIdentity.IsDamageable(Onwer.ReferenceHub, player.ReferenceHub))
                {
                    continue;

                }

                float dist = Vector3.Distance(shotPoint.transform.position, player.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    Locking = player;
                }
            }

        }
        PlayerStatsSystem.AttackerDamageHandler handler = new PlayerStatsSystem.FirearmDamageHandler();
        protected virtual void ServerApplyDestructibleDamage(DestructibleHitPair target, HitscanResult result)
        {
            if (itemBase == null)
            {
                itemBase = CreateOnwerItem();
            }
            //Log.Warn(itemBase is InventorySystem.Items.Firearms.Firearm);
            if (itemBase is InventorySystem.Items.Firearms.Firearm item)
            {
                try
                {
                    var fireM = item.Modules.FirstOrDefault(x => x is HitscanHitregModuleBase) as HitscanHitregModuleBase;
                    var damage = fireM.BaseDamage;
                    handler.Damage = damage;
                    //handler.Attacker = Onwer.Footprint;
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
                    Onwer.ShowHitMarker();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
    public class damageReceiver : NetworkBehaviour, IDestructible, IBlockStaticBatching
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
        public Turret turret;
        public bool Damage(float damage, PlayerStatsSystem.DamageHandlerBase handler, Vector3 pos)
        {
            turret.OnDamaged(damage, handler, pos);
            return true;
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
            Turret.Create(player.CameraTransform.position + player.CameraTransform.forward * 2f, Vector3.right, player);
        }
    }
}
