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
    public class SkynetAbility1 : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse3;

        public override string Name => "房间放毒";

        public override string Des => "房间放毒7秒";

        public override int id => 102;
        public override double Time => 120;
        public override float WaitForDoneTime => 7;
        public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[]
{
            "Default",
            "Hitbox",
            "Glass",
            "CCTV",
            "Door"
});
        public override AbilityBase Register(Player player)
        {
            var a = new SkynetAbility1(player);
            a.InternalRegister(player);
            return a;
        }
        public override bool OnTrigger()
        {
            var r = new Ray(player.CameraTransform.position + player.CameraTransform.forward * 0.8f, player.CameraTransform.forward);
            if (Physics.Raycast(r, out var raycast, 45, HitregMask.Mask))
            {
                var o = Room.Get(raycast.point);
                if (o && o.Type != RoomType.Surface)
                {
                    o.LockDown(7);
                    foreach (var item in o.Players)
                    {
                        item.EnableEffect(EffectType.Decontaminating, 7f);
                    }
                }
                else return false;
            }
            return true;
        }
        internal SkynetAbility1(Player player) : base(player)
        {
            TotalCount = 1;
        }
        public SkynetAbility1() : base()
        {
            TotalCount = 1;
        }
    }
    public class SkynetAbility2 : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse2;

        public override string Name => "房间锁门";

        public override string Des => "房间放毒7秒";

        public override int id => 102;
        public override double Time => 75;
        public override float WaitForDoneTime => 12;
        public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[]
{
            "Default",
            "Hitbox",
            "Glass",
            "CCTV",
            "Door"
});
        public override AbilityBase Register(Player player)
        {
            var a = new SkynetAbility2(player);
            a.InternalRegister(player);
            return a;
        }
        public override bool OnTrigger()
        {
            var r = new Ray(player.CameraTransform.position + player.CameraTransform.forward * 0.8f, player.CameraTransform.forward);
            if (Physics.Raycast(r, out var raycast, 45, HitregMask.Mask))
            {
                var o = Room.Get(raycast.point);
                if (o)
                {
                    o.Blackout(12);
                    o.LockDown(12);

                }
                else return false;
            }
            return true;
        }
        internal SkynetAbility2(Player player) : base(player)
        {
            TotalCount = 1;
        }
        public SkynetAbility2() : base()
        {
            TotalCount = 1;
        }
    }
    public class SkynetAbility3 : PassAbility
    {
        //public override KeyCode KeyCode => KeyCode.Mouse2;

        public override string Name => "房间锁门";

        public override string Des => "房间放毒7秒";

        public override int id => 102;
        //public override double Time => 75;
        //public override float WaitForDoneTime => 12;
        public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[]
{
            "Default",
            "Hitbox",
            "Glass",
            "CCTV",
            "Door"
});
        public override AbilityBase Register(Player player)
        {
            var a = new SkynetAbility3(player);
            a.InternalRegister(player);
            return a;
        }
        public override void OnCheck(Player player)
        {
        }
        internal SkynetAbility3(Player player) : base(player)
        {
            //TotalCount = 1;
        }
        public SkynetAbility3() : base()
        {
            //TotalCount = 1;
        }
    }
}
