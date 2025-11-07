using Decals;
using DrawableLine;
using Exiled.API.Enums;
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
using InventorySystem.Items.Firearms.Attachments;
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
using Utils.Networking;
using static Next_generationSite_27.UnionP.heavy.Scannner;
using Object = UnityEngine.Object;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class JS_L1 : BaseClass
    {
        public static uint MagicGun1_JS_L1_ID = 5807;
        [CustomItem(ItemType.GunA7)]
        public class MagicGun1_JS_L1 : CustomWeapon
        {
            public static MagicGun1_JS_L1 ins;
            public override uint Id { get; set; } = MagicGun1_JS_L1_ID;
            public override string Name { get; set; } = "JS-L1";
            public override string Description { get; set; } = "对沿途目标照成流血+减速伤害";
            public override float Weight { get; set; } = 10;
            public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();
            public override Vector3 Scale { get => base.Scale; set => base.Scale = value; }
            public override AttachmentName[] Attachments { get => base.Attachments; set => base.Attachments = value; }
            public override ItemType Type { get => base.Type; set => base.Type = value; }
            public override float Damage { get => base.Damage; set => base.Damage = value; }
            public override byte ClipSize { get => base.ClipSize; set => base.ClipSize = value; }

            public override void Init()
            {
                ins = this;
                Type = ItemType.GunA7;
                Damage = 1;
                ClipSize = 50;
                base.Init();
                //DrawableLine.DrawableLines.IsDebugModeEnabled = true;
            }

            protected override void OnHurting(HurtingEventArgs ev)
            {
                base.OnHurting(ev);
            }

            protected override void OnOwnerEscaping(OwnerEscapingEventArgs ev)
            {
                base.OnOwnerEscaping(ev);
            }
            public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[]
{
            "Default",
            "Hitbox",
            "Glass",
            "CCTV",
            "Door"
});
            protected override void OnShooting(ShootingEventArgs ev)
            {
                var r = new Ray(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward);
                var raycasts = Physics.SphereCastAll(r, 0.2f, 120, HitregMask.Mask);
                var l = raycasts.ToList();
                l.Sort((RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
                IDestructible destructible;
                foreach (var raycast in l)
                {
                    if (raycast.collider.TryGetComponent<IDestructible>(out destructible))
                    {
                        //destructibles.Add(destructible);
                        if (destructible is HitboxIdentity HI)
                        {
                            var p = Player.Get(HI.TargetHub);
                            if (p == ev.Player)
                            {
                                continue;
                            }
                            if (HitboxIdentity.IsEnemy(HI.TargetHub, ev.Player.ReferenceHub))
                            {
                                p.EnableEffect(EffectType.Blurred, 10f);
                                if (p.IsScp)
                                {
                                    p.EnableEffect(EffectType.Slowness, 15, 10);
                                    p.EnableEffect(EffectType.Bleeding, 15, 20);
                                    p.Hurt(new FirearmDamageHandler(ev.Firearm.Base, 5, 1));
                                }
                                else
                                {
                                    p.Hurt(new FirearmDamageHandler(ev.Firearm.Base, 0.07f, 1));

                                }
                                new DrawableLineMessage(0.6f, Color.red, new Vector3[2] { ev.Player.CameraTransform.position + 0.2f * Vector3.down, raycast.point }).SendToAuthenticated();
                            }
                            else
                            {
                                p.AddRegeneration(0.1f, 10);
                                //p.Hurt(new FirearmDamageHandler(ev.Firearm.Base, 5, 1));
                                new DrawableLineMessage(0.6f, Color.green, new Vector3[2] { ev.Player.CameraTransform.position + 0.2f * Vector3.down, raycast.point }).SendToAuthenticated();
                            }

                        }
                        else
                        {
                            destructible.Damage(10, new FirearmDamageHandler(ev.Firearm.Base, 8, 1), raycast.point);
                            new DrawableLineMessage(0.6f, Color.yellow, new Vector3[2] { ev.Player.CameraTransform.position + 0.2f * Vector3.down, raycast.point }).SendToAuthenticated();

                        }
                    }
                }
                int num = UnityEngine.Random.Range(5, 7);
                Vector3[] array = new Vector3[num];
                Vector3 vector = ev.Player.CameraTransform.position + 0.2f * Vector3.down;
                for (int i = 0; i < num; i++)
                {
                    if (i != 0)
                    {
                        if (i % 5 == 0)
                        {
                            vector += ev.Player.CameraTransform.forward * GetRandomDistance(false);
                        }
                        else
                        {
                            Vector3 a = ev.Player.CameraTransform.forward * GetRandomDistance(false);
                            Vector3 b = a;
                            if (i % 5 == 4)
                            {
                                b = ev.Player.CameraTransform.up * -0.5f * GetRandomDistance(true);
                            }
                            else if (i % 5 == 3)
                            {
                                b = ev.Player.CameraTransform.up * 0.5f * GetRandomDistance(true);
                            }
                            else if (i % 5 == 2)
                            {
                                //b = ev.Player.CameraTransform.up * -1 * GetRandomDistance(true);
                                b = ev.Player.CameraTransform.right * 0.5f * GetRandomDistance(true);
                            }
                            else if (i % 5 == 1)
                            {

                                b = ev.Player.CameraTransform.right * -0.5f * GetRandomDistance(true);
                            }
                            vector += a + b;
                        }
                    }
                    array[i] = vector;
                }
                new DrawableLineMessage(0.14f, Color.blue, array).SendToAuthenticated();

                base.OnShooting(ev);
            }

            private static float GetRandomDistance(bool allowNegativeValues = true)
            {
                bool flag = allowNegativeValues && UnityEngine.Random.Range(0, 4) % 2 == 0;
                float num = UnityEngine.Random.Range(0.4f, 0.2f);
                if (!flag)
                {
                    return num;
                }
                return -num;
            }
            protected override void OnShot(ShotEventArgs ev)
            {

                base.OnShot(ev);
            }

            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();
            }

            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
            }
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
