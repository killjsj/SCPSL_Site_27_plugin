using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.heavy
{
    public class BombGun : BaseClass
    {
        public override void Delete()
        {
            //throw new NotImplementedException();
        }

        public override void Init()
        {
            //throw new NotImplementedException();
        }
        public static uint BombgunItemID = 5808;
        [CustomItem(ItemType.GunRevolver)]
        public class bomb_gun : CustomWeapon
        {
            public override uint Id { get; set; } = BombgunItemID;
            public override string Name { get; set; } = "榴弹枪";
            public override float Damage { get; set; } = 0;
            public override string Description { get; set; } = "";
            public override float Weight { get; set; } = 2;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            public override Vector3 Scale { get; set; } = new Vector3(2f, 2f, 2f);
            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                    base.OnUpgrading(ev);
                }
            }
            protected override void OnAcquired(Player player, Item item, bool displayMessage)
            {
                if (Check(item))
                {
                    BombHandle.RegisterAGun(item);
                }
                base.OnAcquired(player, item, displayMessage);
            }
            public override void Init()
            {
                base.Init();
            }
        }
    }
}
