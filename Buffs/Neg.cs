using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.Handlers;
using GameCore;
using InventorySystem.Items.Autosync;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static InventorySystem.Items.Firearms.ShotEvents.ShotEventManager;
using Map = Exiled.API.Features.Map;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP.Buffs
{
    public class  Overload : BuffBase
    {
        public float NextTime = 0f;

        public override BuffType Type => BuffType.Negative;

        public override string BuffName => "电力过载";

        public IEnumerator<float> CooldownCoroutine()
        {
            yield return MEC.Timing.WaitForSeconds(0.1f);
            if (CheckEnabled() == false)
            {
                yield break;
            }
            NextTime = Time.time + UnityEngine.Random.Range(60,180);
            while (Round.InProgress)
            {
                yield return MEC.Timing.WaitForSeconds(0.1f);
                if(Time.time >= NextTime)
                {
                    MEC.Timing.RunCoroutine(StartCoroutine());
                    NextTime = Time.time + UnityEngine.Random.Range(120, 180);
                }
            }
        }
        public IEnumerator<float> StartCoroutine()
        {
            var c = 0f;
            while (c <= 60f)
            {
                yield return MEC.Timing.WaitForSeconds(0.1f);
                c += 0.1f;
                var tim = UnityEngine.Random.Range(0.5f, 4f);
                foreach (var item in Door.List)
                {
                    if(item.IsGate || item.IsElevator || item.IsElevator || item.IsKeycardDoor || item.IsPartOfCheckpoint)
                    {
                        item.IsOpen = false;
                        item.Lock(tim, DoorLockType.NoPower);
                    }
                }
                Map.TurnOffAllLights(tim);
                c += tim;
                yield return MEC.Timing.WaitForSeconds(tim);
                foreach (var item in Player.Enumerable)
                {
                    if (item.Role is Scp079Role role)
                    {
                        role.AuxManager.CurrentAux = role.AuxManager.MaxAux;
                        if (Math.Floor(c) % 2 == 0)
                        {
                            role.Experience += 1;
                        }
                    }
                }
                tim = UnityEngine.Random.Range(0.9f, 1.4f);
                c += tim;
                yield return MEC.Timing.WaitForSeconds(tim);
            }
            Map.TurnOffAllLights(0);
            foreach (var item in Player.Enumerable)
            {
            }
        }
        void RoundStarted()
        {
            if(CheckEnabled()==false)
            {
                return;
            }
            MEC.Timing.RunCoroutine(CooldownCoroutine());
        }
        public override void Init()
        {
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            base.Delete();
        }
    }
    public class NegOverridePerms : BuffBase
    {
        public override BuffType Type => BuffType.Negative;

        public override string BuffName => "权限错误";

        void OnInteractingDoor(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
        {
            if (CheckEnabled())
            {
                if (UnityEngine.Random.Range(0, 100) <= 15)
                {
                    ev.IsAllowed = false;
                    ev.CanInteract = false;
                    ev.Door.Lock(1, DoorLockType.Isolation);
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
    public class NoGunDay : BuffBase
    {
        public override BuffType Type => BuffType.Negative;

        public override string BuffName => "什么是枪";
        public override void Init()
        {
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            Exiled.Events.Handlers.Player.ChangingItem += ChangingItem;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Exiled.Events.Handlers.Player.ChangingItem -= ChangingItem;
            base.Delete();
        }
        public List<ushort> AllowSerials = new();
        public void RoundStarted()
        {
            AllowSerials.Clear();
        }
        public void ChangingItem(ChangingItemEventArgs ev)
        {
            if (this.CheckEnabled() && ev.Item != null && ev.Item.IsFirearm && ev.Item.Type != ItemType.ParticleDisruptor && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
            {
                if (AllowSerials.Contains(ev.Item.Serial))
                {
                    return;
                }
                var r = UnityEngine.Random.Range(0, 100);
                if (r <= 5)
                {
                    ev.IsAllowed = true;
                    AllowSerials.Add(ev.Item.Serial);
                }
                else if (r < 99) {
                    ev.Player.RemoveItem(ev.Item);
                        ev.Item = ev.Player.AddItem(ItemType.ParticleDisruptor);
                }
                else
                {
                    ev.Player.RemoveItem(ev.Item);
                    r = UnityEngine.Random.Range(0, 10);
                    if (r == 0)
                    {
                        ev.Item = ev.Player.AddItem(ItemType.MicroHID);

                    }
                    else
                    {
                    ev.Item = ev.Player.AddItem(ItemType.Jailbird);
                    }
                }
            }
        }
    }
    public class ZakoZako : BuffBase
    {
        public override BuffType Type => BuffType.Negative;

        public override string BuffName => ((UnityEngine.Random.Range(0,100) == 0 ? "杂鱼" : "")+"没吃饭吗?");
        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingItem;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingItem;
            base.Delete();
        }
        public void ChangingItem(ChangingRoleEventArgs ev)
        {
            if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
            {
                MEC.Timing.CallDelayed(0.3f, () =>
                {
                    if (ev.Player.Role.Type.IsScp())
                    {
                        if (ev.Player.Role.Base is IHealthbarRole h)
                        {
                            ev.Player.MaxHealth = h.MaxHealth + 1200;
                            ev.Player.Health = ev.Player.MaxHealth;
                        }
                    }
                });
            }
        }
    }
    public class CarIsComing : BuffBase
    {
        public override BuffType Type => BuffType.Negative;

        public override string BuffName => "大运来了";
        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingItem;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingItem;
            base.Delete();
        }
        public void ChangingItem(ChangingRoleEventArgs ev)
        {
            if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
            {
                MEC.Timing.CallDelayed(0.3f, () =>
                {
                    if (ev.Player.Role.Type.IsScp())
                    {
                        ev.Player.EnableEffect(EffectType.MovementBoost, 30, 0f);
                    }
                });
            }
        }
    }
    public class IAmGod : BuffBase
    {
        public override BuffType Type => BuffType.Negative;

        public override string BuffName => "我硬了";
        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingItem;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingItem;
            base.Delete();
        }
        public void ChangingItem(ChangingRoleEventArgs ev)
        {
            if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
            {
                MEC.Timing.CallDelayed(0.3f, () =>
                {
                    if (ev.Player.Role.Type.IsScp())
                    {
                        ev.Player.EnableEffect(EffectType.DamageReduction, 30, 0f);
                    }
                });
            }
        }
    }
}
