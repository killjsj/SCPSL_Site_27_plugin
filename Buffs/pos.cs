using Exiled.API.Features;
using Exiled.API.Features.Items;
using GameCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.Buffs
{
    public class OverridePerms : BuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string BuffName => "权限错误";

        void OnInteractingDoor(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
        {
            if (CheckEnabled())
            {
                if (ev.Door.RequiredPermissions != Interactables.Interobjects.DoorUtils.DoorPermissionFlags.None &&UnityEngine.Random.Range(0, 100) < 5)
                {
                    ev.IsAllowed = true;
                    ev.CanInteract = true;
                }
            }
        }
        public override void Init()
        {
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            base.Delete();
        }
    }
    public class SuperHot : BuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string BuffName => "彻底疯狂";

        public Dictionary<Player, (float LastTIme, int amount)> damageRecode = new();
        IEnumerator<float> CooldownCoroutine()
        {
                yield return MEC.Timing.WaitForSeconds(0.1f);
            while (Round.InProgress)
            {
                    yield return MEC.Timing.WaitForSeconds(0.5f);
                foreach (var item in damageRecode)
                {
                    if (item.Value.amount < 0)
                    {
                        damageRecode[item.Key] = (item.Value.LastTIme, 0);
                        continue;
                    }
                    else if (item.Value.LastTIme + 5f < Time.time)
                    {
                        damageRecode[item.Key] = (Time.time, item.Value.amount - 5);
                    }
                }
            }
        }
        void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if(ev.Attacker == null || ev.Player == null)
            {
                return;
            }
            if (CheckEnabled())
            {

                //ev.Amount *= 1.5f;
                if (!damageRecode.ContainsKey(ev.Attacker))
                {
                    damageRecode[ev.Attacker] =  (Time.time, (int)ev.Amount);
                }
                else
                {
                    damageRecode[ev.Attacker] = (Time.time, damageRecode[ev.Attacker].amount + (int)ev.Amount);
                }
                if (damageRecode[ev.Attacker].amount >= 250)
                {
                    EnableSuperHot(ev.Attacker);
                    damageRecode[ev.Attacker] = (Time.time, damageRecode[ev.Attacker].amount - 250);
                }
            }
        }
        void OnShot(Exiled.Events.EventArgs.Player.ShotEventArgs ev)
        {
            if (CheckEnabled())
            {
                if (startedSH.Contains(ev.Player))
                {
                    if (ev.Item.IsFirearm)
                    {
                        Firearm firearm = (Firearm)ev.Item;
                        firearm.MagazineAmmo += 2;
                    }
                }
            }
        }
        public List<Player> startedSH = new();
        void EnableSuperHot(Player player)
        {
            var orI207 = player.GetEffect(Exiled.API.Enums.EffectType.Scp207).Intensity;
            var orIScp1853 = player.GetEffect(Exiled.API.Enums.EffectType.Scp1853).Intensity;
            player.EnableEffect(Exiled.API.Enums.EffectType.Scp207, 2, 12f);
            player.EnableEffect(Exiled.API.Enums.EffectType.Scp1853, 2, 12f);
            startedSH.Add(player);
            MEC.Timing.CallDelayed(12f, () =>
            {
                player.DisableEffect(Exiled.API.Enums.EffectType.Scp207);
                if (orI207 > 0)
                {
                    player.EnableEffect(Exiled.API.Enums.EffectType.Scp207, orI207, 0);
                }
                if (orIScp1853 > 0)
                {
                    player.EnableEffect(Exiled.API.Enums.EffectType.Scp1853, orIScp1853, 0);
                }
                startedSH.Remove(player);
            });
        }
        void RoundStarted()
        {
            if (CheckEnabled() == false)
            {
                return;
            }
            damageRecode.Clear();
            MEC.Timing.RunCoroutine(CooldownCoroutine());
        }
        public override void Init()
        {
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.Shot -= OnShot;
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            base.Delete();
        }
    }


    public class SuperHuman : BuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string BuffName => "孤注一掷";
        void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (CheckEnabled())
            {
                var c = Player.Enumerable.Count(x=> x!=null && x.IsHuman && x != ev.Attacker && HitboxIdentity.IsEnemy(x.ReferenceHub,ev.Attacker.ReferenceHub));
                if(c <= 3)
                {
                    ev.Amount *= 1.25f;

                }
            }
        }
        public override void Init()
        {
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            base.Delete();
        }
    }

}
