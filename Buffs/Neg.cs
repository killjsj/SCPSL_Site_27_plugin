using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.Handlers;
using InventorySystem.Items.Autosync;
using NetworkManagerUtils.Dummies;
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
                var tim = UnityEngine.Random.Range(0.1f, 0.4f);
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
                    if (item.Role is Scp173Role role1)
                    {
                        role1.BlinkReady = true;
                    }
                }
                tim = UnityEngine.Random.Range(0.4f, 0.9f);
                c += tim;
                yield return MEC.Timing.WaitForSeconds(tim);
            }
            Map.TurnOffAllLights(0);
            foreach (var item in Player.Enumerable)
            {
                if (item.Role is Scp173Role role1)
                {
                    role1.ObserversTracker.ResetObject();
                    role1.ObserversTracker.UpdateObservers();
                }
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
                if (UnityEngine.Random.Range(0, 100) <= 5)
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
            Exiled.Events.Handlers.Player.ChangingItem += ChangingItem;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingItem -= ChangingItem;
            base.Delete();
        }
        public void ChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Item.IsFirearm && this.CheckEnabled())
            {
                var r = UnityEngine.Random.Range(0, 100);
                if (r <= 1)
                {
                    ev.IsAllowed = true;
                }
                else if (r < 99) {
                    ev.Player.RemoveItem(ev.Item);
                    ev.Item = ev.Player.AddItem(ItemType.Jailbird);
                } else
                {
                    ev.Player.RemoveItem(ev.Item);
                    ev.Item = ev.Player.AddItem(ItemType.ParticleDisruptor);
                }
            }
        }
    }

}
