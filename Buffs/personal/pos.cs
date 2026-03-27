using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using GameCore;
using MEC;
using Next_generationSite_27.UnionP.heavy;
using Next_generationSite_27.UnionP.Turret;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.Buffs
{
    public class OverridePermsPos : PersonalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string BuffName => "权限错误";

        public override string Description => "10%概率无权限开门";

        void OnInteractingDoor(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
        {
            if (CheckEnabled(ev.Player))
            {
                if (ev.Door.RequiredPermissions != Interactables.Interobjects.DoorUtils.DoorPermissionFlags.None &&UnityEngine.Random.Range(0, 100) < 10)
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
    public class SuperHot : PersonalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string Description => "根据你的短时间输出提高你的各项体能";
        public override string BuffName => "彻底疯狂";

        public Dictionary<Player, (float LastTIme, float amount)> damageRecode = new();
        IEnumerator<float> CooldownCoroutine()
        {
            yield return MEC.Timing.WaitForSeconds(0.1f);
            while (Round.InProgress)
            {
                yield return MEC.Timing.WaitForSeconds(0.5f);
                try
                {
                    foreach (var item in damageRecode.ToList())
                    {
                        if (item.Value.amount < 0)
                        {
                            damageRecode.Remove(item.Key);
                            continue;
                        }
                        else if (item.Value.LastTIme + 5f < Time.time)
                        {
                            damageRecode[item.Key] = (item.Value.LastTIme, item.Value.amount - 5);
                        }
                        //Log.Info($"item.Key{item.Key} 1:{item.Value.LastTIme} 2:{item.Value.amount}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
        void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if(ev.Attacker == null || ev.Player == null)
            {
                return;
            }
            if (CheckEnabled(ev.Player))
            {

                //ev.Amount *= 1.5f;
                if (!damageRecode.ContainsKey(ev.Attacker))
                {
                    damageRecode[ev.Attacker] =  (Time.time, (int)ev.Amount);
                }
                else
                {
                    damageRecode[ev.Attacker] = (Time.time, damageRecode[ev.Attacker].amount + (int)ev.Amount * (ev.Player.Role.Type.IsScp() ? 0.5f : 1f));
                }

                if (damageRecode[ev.Attacker].amount >= 500)
                {
                    EnableSuperHot(ev.Attacker);
                    damageRecode[ev.Attacker] = (Time.time, damageRecode[ev.Attacker].amount - 250);
                }
            }
            if (startedSH.Contains(ev.Player)) {
                if ((ev.DamageHandler.Type == DamageType.Scp207 || ev.DamageHandler.Type == DamageType.Poison))
                {
                    ev.Player.DisableEffect(EffectType.Poisoned);
                    ev.IsAllowed = false;
                }
            }
        }
        void OnShot(Exiled.Events.EventArgs.Player.ShotEventArgs ev)
        {
            if (CheckEnabled(ev.Player))
            {
                if (startedSH.Contains(ev.Player))
                {
                    if (ev.Item.IsFirearm && ev.Item.Type != ItemType.ParticleDisruptor)
                    {
                        Firearm firearm = (Firearm)ev.Item;
                        if(firearm.MagazineAmmo % 3 == 0)
                            firearm.MagazineAmmo += 1;
                    }
                }
            }
        }
        public List<Player> startedSH = new();
        void EnableSuperHot(Player player)
        {
            if (startedSH.Contains(player)) return;
            var orI207 = player.GetEffect(Exiled.API.Enums.EffectType.Scp207).Intensity;
            var orB207 = player.GetEffect(Exiled.API.Enums.EffectType.Scp207).IsEnabled;
            var orIScp1853 = player.GetEffect(Exiled.API.Enums.EffectType.Scp1853).IsEnabled;
            player.EnableEffect(Exiled.API.Enums.EffectType.Scp207, 2, 12f);
            player.EnableEffect(Exiled.API.Enums.EffectType.MovementBoost, 20, 12f);
            player.EnableEffect(Exiled.API.Enums.EffectType.Scp1853, 2, 12f);
            startedSH.Add(player);
            MEC.Timing.CallDelayed(12f, () =>
            {
                if (orB207)
                {
                    player.EnableEffect(Exiled.API.Enums.EffectType.Scp207, orI207, 0);
                    //Log.Info(1);
                }else
                {
                    player.DisableEffect(Exiled.API.Enums.EffectType.Scp207);
                    //Log.Info(3);
                }
                if (orIScp1853)
                {
                    player.EnableEffect(Exiled.API.Enums.EffectType.Scp1853, 1, 0);
                    //Log.Info(2);
                }
                else
                {
                    player.DisableEffect(Exiled.API.Enums.EffectType.Scp1853);
                    //Log.Info(4);
                }
                player.DisableEffect(Exiled.API.Enums.EffectType.MovementBoost);
                startedSH.Remove(player);
                            damageRecode.Remove(player);
                    player.DisableEffect(EffectType.Poisoned);
            });
        }
        void RoundStarted()
        {
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

    public class StarOfScp : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "基金会之星";
        public override string Description => "开局给一位博士发3x";

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

        public void RoundStarted()
        {
            MEC.Timing.CallDelayed(1.2f, () =>
            {
                if (CheckEnabled())
                {
                    MEC.Timing.CallDelayed(1f, () =>
                    {

                        var luck = Player.Enumerable.Where(x => x.Role.Type == PlayerRoles.RoleTypeId.Scientist).GetRandomValue();
                        if (luck != null)
                        {
                            luck.AddItem(ItemType.ParticleDisruptor);
                        }
                    });

                }
            });
        }
    }
    public class GRunningMan : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "光州跑男";

        public override string Description => "开局给一个保安刷囚鸟 给混沌刷增益性道具";
        public override void Init()
        {
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;

            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;

            base.Delete();
        }
        public void RoundStarted()
        {
            Timing.CallDelayed(1.4f, () => { 
            if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
            {
                var p = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.FacilityGuard).GetRandomValue();
                if (p != null)
                {
                    p.AddItem(ItemType.Jailbird);
                }

            } });
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (CheckEnabled())
            {
                foreach (var item in ev.Players)
                {
                    if (item.Role.Type == PlayerRoles.RoleTypeId.ChaosMarauder)
                    {
                        item.AddItem(ItemType.SCP1509);
                    }
                    if (item.Role.Type == PlayerRoles.RoleTypeId.ChaosRepressor)
                    {
                        item.TryAddCandy(InventorySystem.Items.Usables.Scp330.CandyKindID.Black);
                    }
                }
            }
        }
    }
    public class StrongerHuman : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "科技这一块";
        public override string Description => "给九尾刷强力内战道具";

        public override void Init()
        {
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;

            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;

            base.Delete();
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            try
            {
                if (CheckEnabled())
                {
                    foreach (var item in ev.Players)
                    {
                        if (item.Role.Type == PlayerRoles.RoleTypeId.NtfCaptain || item.Role.Type == PlayerRoles.RoleTypeId.ChaosRepressor || item.Role.Type == PlayerRoles.RoleTypeId.NtfSergeant)
                        {
                            TurretItem.Instance.Give(item);
                        }
                        if (item.Role.Type == PlayerRoles.RoleTypeId.NtfPrivate)
                        {
                            SpeedBuilditem.SpeedBuildItem.instance.Give(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
    public class SuperHuman : PersonalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string Description => "当你是最后3个存活人类时 伤害增加25%";
        public override string BuffName => "孤注一掷";
        void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            Timing.CallDelayed(1.2f, () =>
            {
                if (CheckEnabled(ev.Player) && ev.Attacker != null)
                {
                    var c = Player.Enumerable.Count(x => x != null && x.IsHuman && x != ev.Attacker && HitboxIdentity.IsEnemy(x.ReferenceHub, ev.Attacker.ReferenceHub));
                    if (c <= 3)
                    {
                        ev.Amount *= 1.25f;

                    }
                }
            });
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
    public class Scanner : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string Description => "定时扫描SCP位置并公开";
        public override string BuffName => "定时扫描";
        void start()
        {
            Timing.CallDelayed(1.2f, () =>
            {
                if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {
                    MEC.Timing.RunCoroutine(ScannerCoroutine());
                }
            });

        }
        public IEnumerator<float> ScannerCoroutine()
        {
            while (Round.InProgress)
            {
                yield return MEC.Timing.WaitForSeconds(120f);
                if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {
                    Exiled.API.Features.Cassie.MessageTranslated("", "正在扫描设施, 预计10s后完成....");
                    yield return MEC.Timing.WaitForSeconds(10f + UnityEngine.Random.Range(-2, 3));
                    StringBuilder SB = new StringBuilder();
                    SB.AppendLine("<color=yellow>扫描结果:</color>");
                    int chaos = 0;
                    foreach (var item in Player.Enumerable)
                    {
                        if (item.Role.Type.IsScp())
                        {
                            SB.AppendLine($"<color=red>{item.Role.Name}</color> <color=green>位置:</color> {item.CurrentRoom.RoomToString()}</color>");
                        }
                        if (item.Role.Type.IsChaos())
                        {
                            chaos++;
                        }
                    }
                    SB.AppendLine($"<color=yellow>混沌总数: {chaos}</color>");
                    foreach (var item in Player.Enumerable)
                    {
                        item.AddMessage("", SB.ToString(), 6f, ScreenLocation.Top);
                    }
                }
            }

        }
        public override void Init()
        {
            Exiled.Events.Handlers.Server.RoundStarted += start;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= start;
            base.Delete();
        }
    }
    public class NoWhereToHide : PersonalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string Description => "定时扫描人类位置(无Scp1344效果时)";
        public override string BuffName => "无处可藏";
        public IEnumerator<float> ScannerCoroutine(Player player)
        {
            while (Round.InProgress)
            {
                yield return MEC.Timing.WaitForSeconds(180f);
                if (this.CheckEnabled(player) && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {
                    if (!player.GetEffect(EffectType.Scp1344).IsEnabled)
                    {
                        player.EnableEffect(EffectType.Scp1344, 25f);
                    }
                }
                else
                {
                    yield break;
                }
            }

        }
        public override void Init()
        {
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            base.Delete();
        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            MEC.Timing.CallDelayed(1.4f, () =>
            {
                if (this.CheckEnabled(ev.Player) && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {
                    {
                        if (ev.Player.Role.Type.IsAlive())
                        {
                            MEC.Timing.RunCoroutine(ScannerCoroutine(ev.Player));

                        }
                    }
                }
            });
        }
    }

}
