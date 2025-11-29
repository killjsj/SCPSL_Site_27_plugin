using DrawableLine;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using MEC;
using PlayerRoles.Subroutines;
using ProjectMER.Features.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.heavy.ability
{
    public class TPAbility : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse1;

        public override string Name => "传送";

        public override string Des => "45m内传送";

        public override int id => 102;
        public override double Time => 45;
        public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[]
{
            "Default",
            "Hitbox",
            "Glass",
            "CCTV",
            "Door"
});

        public override bool OnTrigger()
        {
            var r = new Ray(player.CameraTransform.position + player.CameraTransform.forward * 0.8f, player.CameraTransform.forward);
            if(Physics.Raycast(r,out var raycast, 45, HitregMask.Mask))
            {
                if (raycast.collider.TryGetComponent<IDestructible>(out var destructible))
                {
                    //destructibles.Add(destructible);
                    if (destructible is HitboxIdentity HI)
                    {
                        var p = Player.Get(HI.TargetHub);
                        if (p == player)
                        {
                            return false;
                        }
                        var position = p.Position;
                        p.Position = player.Position;
                        player.Position = position;

                        return true;
                    }
                    else
                    {
                        player.Position = raycast.point + raycast.normal * 0.3f;
                        return true;
                    }

                } else
                {
                    player.Position = raycast.point + raycast.normal * 0.3f;
                    return true;
                }
            }
            return false;
        }
        internal TPAbility(Player player) : base(player)
        {
            TotalCount = 2;
        }
        public TPAbility() : base()
        {
            TotalCount = 2;
        }
    }
}
