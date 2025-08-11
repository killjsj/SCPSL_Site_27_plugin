using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using InventorySystem.Items.ThrowableProjectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace Next_generationSite_27.UnionP
{
    public class BombCollisionHandler : MonoBehaviour
    {
        private bool initialized;

        //
        // 摘要:
        //     Gets the thrower of the grenade.
        public Player Owner { get; private set; }

        //
        // 摘要:
        //     Gets the grenade itself.
        public GrenadePickup Grenade { get; private set; }
        public void Init(Player owner, GrenadePickup grenade)
        {
            Owner = owner;
            Grenade = grenade;
            initialized = true;
        }

        void OnCollisionEnter(Collision collision)
        {
            try
            {
                if (initialized)
                {
                    if (Owner == null)
                    {
                        Log.Error("Owner is null!");
                    }

                    if (Grenade == null)
                    {
                        Log.Error("Grenade is null!");
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

                    if (!(collision.collider.gameObject == Owner.GameObject) && (Player.Get(collision.gameObject) != Owner))
                    {
                        Grenade.FuseTime = 0.01f;
                            Grenade.Explode();
                            Plugin.active_g--;
                            //ExplosionGrenade.Explode(a.Footprint, Grenade.Position, Grenade, ExplosionType.Grenade);
                        
                    }
                }
            
            }
            catch (Exception arg)
            {
                Log.Error(string.Format("{0} error:\n{1}", "OnCollisionEnter", arg));
                UnityEngine.Object.Destroy(this);
            }
        }
    }
}
