using CustomPlayerEffects;
using CustomRendering;
using DrawableLine;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Roles;
using MEC;
using PlayerRoles.Subroutines;
using ProjectMER.Features.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;

namespace Next_generationSite_27.UnionP.heavy.ability
{
    public class Scp079Ability1 : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse3;

        public override string Name => "房间放毒";

        public override string Des => "房间放毒7秒";

        public override int id => 106;
        public override double Time => 20;
        public override float WaitForDoneTime => 7;
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
            if (player.Role is Scp079Role role)
            {
                if (role.Energy >= 150)
                {
                    role.Energy -= 150;
                    var r = new Ray(role.Camera.Position + role.Camera.Transform.forward * 0.8f, role.Camera.Transform.forward);
                    if (Physics.Raycast(r, out var raycast, 45, HitregMask.Mask))
                    {
                        var o = Room.Get(raycast.point);
                        if (o && o.Type != RoomType.Surface)
                        {
                            o.LockDown(7);
                            foreach (var item in o.Players)
                            {
                                if (!item.IsScp) { 
                                    item.EnableEffect(EffectType.Decontaminating, 7f);
                                    item.EnableEffect<FogControl>(7f);
                                    item.GetEffect<FogControl>().SetFogType(FogType.Decontamination);
                                }
                            }

                        }
                        else return false;
                    }
                    return true;
                }
                else return false;
            }
            return false;
        }
        internal Scp079Ability1(Player player) : base(player)
        {
            TotalCount = 1;
        }
        public Scp079Ability1() : base()
        {
            TotalCount = 1;
        }
    }
    public class DebuggersAbility1 : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse3;

        public override string Name => "房间放毒";

        public override string Des => "房间放毒7秒";

        public override int id => 106;
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
                        item.EnableEffect<FogControl>(7f);

                        item.GetEffect<FogControl>().SetFogType(FogType.Decontamination);
                    }
                }
                else return false;
            }
            return true;
        }
        internal DebuggersAbility1(Player player) : base(player)
        {
            TotalCount = 1;
        }
        public DebuggersAbility1() : base()
        {
            TotalCount = 1;
        }
    }
    public class DebuggersAbility2 : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse2;

        public override string Name => "房间锁门";

        public override string Des => "房间放毒7秒";

        public override int id => 104;
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
        internal DebuggersAbility2(Player player) : base(player)
        {
            TotalCount = 1;
        }
        public DebuggersAbility2() : base()
        {
            TotalCount = 1;
        }
    }
    public class DebuggersAbility3 : PassAbility, ITiming
    {
        //public override KeyCode KeyCode => KeyCode.Mouse2;

        public override string Name => "范围扫描";

        public override string Des => "扫描周围35m内的敌人";

        public override int id => 105;
        //public override double Time => 75;
        //public override float WaitForDoneTime => 12;
        public static AbilityCooldown cd = new();
        public override AbilityBase Register(Player player)
        {
            var a = new DebuggersAbility3(player);
            a.InternalRegister(player);
            return a;
        }

        float ITiming.CoolDownRemaining { get => cd.Remaining; set => cd.Remaining = value; }
        float ITiming.DoneRemaining { get => 0; set { } }

        bool ITiming.Done => true;

        public override void OnCheck(Player player)
        {
            //base.OnCheck(player);
            if (cd.IsReady)
            {
                cd.Trigger(2.5);
                foreach (var p in Player.Enumerable)
                {
                    if (player != p && Vector3.Distance(player.Position, p.Position) <= 35f)
                    {
                        if (HitboxIdentity.IsEnemy(player.ReferenceHub, p.ReferenceHub))
                        {
                            new DrawableLineMessage(0.5f, Color.red * new Color(1, 1, 1, 1-(Vector3.Distance(player.Position, p.Position) / 150 )+ 0.01f), new Vector3[2] { p.CameraTransform.position + 0.2f * Vector3.down, player.Position }).SendToHubsConditionally(x => x == player.ReferenceHub);
                        }
                    }
                }
            }
        }
        internal DebuggersAbility3(Player player) : base(player)
        {
            //TotalCount = 1;
        }
        public DebuggersAbility3() : base()
        {
            //TotalCount = 1;
        }
    }
}
