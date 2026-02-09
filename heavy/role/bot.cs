using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.heavy.ability;
using Next_generationSite_27.UnionP.heavy.role;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.Scannner;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class bot : BaseClass
    {
        public static List<Player> diedPlayer
        {
            get
            {
                var t = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.Spectator).ToList();
                t.ShuffleList();
                return t;
            }
        }
        public static PConfig config => Plugin.Instance.Config;

        public static uint botID = 29;

        [CustomRole(RoleTypeId.Tutorial)]

        public class scp5k_Bot : CustomRolePlus, IDeathBroadcaster
        {
            public static scp5k_Bot ins;
            public string CassieBroadcast => "And Saw";

            public string ShowingToPlayer => "安德森机器人";
            public override uint Id { get; set; } = botID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "安德森机器人";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "安德森机器人";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public override void Init()
            {
                ins = this;
                abilities.Add(new TPAbility());
                abilities.Add(new NoSoundMove());
                Description = "与反scp基金会势力合作";
                this.Role = RoleTypeId.Tutorial;
                this.Gravity = new UnityEngine.Vector3(0, -14f, 0);
                MaxHealth = 140;
                this.DisplayCustomItemMessages = false;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是安德森机器人</color></size>\n<size=30><color=yellow>与反scp基金会势力合作</color></size>", 4);
                this.IgnoreSpawnSystem = true;

                this.Inventory = new List<string>()
            {
                //string.Format("{0}", ItemType.ArmorCombat),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Painkillers),
                string.Format("{0}", ItemType.KeycardChaosInsurgency),
                string.Format("{0}", ItemType.ParticleDisruptor),
                string.Format("{0}", ItemType.GunE11SR),
                Bot_GUN.bot_gun.ins.Name,
                //scanner.ins.Name
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            protected override void SubscribeEvents()
            {
                base.SubscribeEvents();
                Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += start;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            protected override void UnsubscribeEvents()
            {
                base.UnsubscribeEvents();
                Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= start;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }
            public int totalLives = 0;
            public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
            {
                if (Check(ev.Player))
                {
                    //var p = LabApi.Features.Wrappers.Player.Get(Player.ReferenceHub);
                    var p = ev.Player;
                    if (ev.Player.Role.Type == RoleTypeId.Tutorial && Check(ev.Player))
                    {
                        if (totalLives > 0)
                        {
                            ev.Player.EnableEffect(type: Exiled.API.Enums.EffectType.Flashed, 0.1f);
                            totalLives = totalLives - 1;
                            ev.IsAllowed = false;
                            ev.Player.Health = ev.Player.MaxHealth;
                            ev.Player.Position = Room.Get(Exiled.API.Enums.RoomType.EzGateA).Position + new UnityEngine.Vector3(0, 3f, 0);
                            ev.Player.ClearItems();
                            foreach (string itemName in Inventory)
                            {
                                TryAddItem(ev.Player, itemName);
                            }
                            p.AddMessage("", $"<color=red><size=30>你还有 {totalLives} 次复活机会</size></color>", 1.5f, ScreenLocation.Center);
                        }
                        else
                        {
                            RemoveRole(ev.Player);

                        }
                    }


                }
            }
            protected override void RoleRemoved(Player player)
            {
                if (!(player.ReferenceHub.roleManager.CurrentRole is IFpcRole fpcRole))
                {
                    return;
                }
                fpcRole.FpcModule.Motor.GravityController.Gravity = FpcGravityController.DefaultGravity;
                base.RoleRemoved(player);
            }
            protected override void RoleAdded(Player player)
            {


                Timing.CallDelayed(0.2f, () =>
                {
                    if (player != null)
                    {
                        totalLives += config.AndLives;
                        player.Position = Room.Get(Exiled.API.Enums.RoomType.EzGateA).Position + new UnityEngine.Vector3(0, 3f, 0);
                        //player.SetCustomRoleFriendlyFire("Goc_C", RoleTypeId.Tutorial, 1);
                        scanner.ins.Give(player, false);
                        //Plugin.RunCoroutine(UiuPlayerUpdate(player));
                    }
                });
                base.RoleAdded(player);
            }

            //public IEnumerator<float> UiuPlayerUpdate(Player player)
            //{
            //    while (player.IsAlive && player.Role.Type == RoleTypeId.Tutorial && Check(player))
            //    {
            //        yield return Timing.WaitForSeconds(0.3f);
            //    }
            //}
            public override void AddRole(Player player)
            {

                base.AddRole(player);
            }
        }
        public static bool Enabled = false;

        public override void Init()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStart;
            //throw new NotImplementedException();
        }
        public static Stopwatch AndTimer = new Stopwatch();

        public static void TrySpawnAndBots(List<Player> candidates)
        {
            if (!CustomRole.TryGet(botID, out var role)) return;

            if (role.TrackedPlayers.Count < config.AndMaxCount &&
                (AndTimer.Elapsed.TotalSeconds >= 220 || !AndTimer.IsRunning) &&
                AndRefreshCount <= config.AndRefreshMaxCount)
            {
                AndTimer.Restart();
                int need = config.AndMaxCount - role.TrackedPlayers.Count;
                var bots = candidates.Take(need).ToList();

                foreach (var p in bots)
                    role.AddRole(p);

                AndRefreshCount++;
                Exiled.API.Features.Cassie.MessageTranslated(
                    "Attention security personnel , And saw spotted at Gate A",
                    "安保人员请注意，已在A大门处发现安德森机器人"
                );
            }
        }
        public static int AndRefreshCount { get; private set; } = 0;
        public static void OnRoundStart()
        {
                AndTimer.Restart();
            AndRefreshCount = 0;

        }

    }
}
