using Exiled.API.Extensions;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP
{
    class Niggers : CustomWeapon
    {
        public override uint Id { get { return 0411; } set { } }
        public override string Name { get => "鞭子"; set { } }
        public override string Description { get => "Niggers"; set { } }
        public override float Weight { get => 3f; set { } }

        public override SpawnProperties SpawnProperties { get => new SpawnProperties(); set { } }
        public override float Damage { get; set; } = 2;


        /// <summary>
        /// Gets or sets a value indicating whether to allow friendly fire with this weapon on FF-enabled servers.
        /// </summary>
        public override bool FriendlyFire { get; set; }
        public override void Init()
        {
            Type = ItemType.Jailbird;
            FriendlyFire = true;
            base.Init();
        }
        //public override
    }
}
