using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.heavy.role;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using Respawning;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.Scannner;
using static Next_generationSite_27.UnionP.heavy.SpeedBuilditem;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Omega1 : BaseClass
    {
        public static uint O1PID = 59;
        [CustomRole(RoleTypeId.NtfCaptain)]
        public class scp5k_Omega1_P : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Omega1_P instance { get; private set; }
            public override uint Id { get; set; } = O1PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Omega-1 小队 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Omega-1 小队 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Omega 1";

            public string ShowingToPlayer => "Omega-1";

            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfCaptain;
                MaxHealth = 70;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 队长</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                //string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFCaptain),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunFRMG0)
            };
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += start;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= start;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }

            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        player.EnableEffect(EffectType.Fade, 255, 0f);

                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint O1SID = 58;
        [CustomRole(RoleTypeId.NtfSergeant)]
        public class scp5k_Omega1_S : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Omega1_S instance { get; private set; }
            public override uint Id { get; set; } = O1SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Omega-1 小队 奇术师";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Omega-1 小队 奇术师";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Omega 1";

            public string ShowingToPlayer => "Omega-1";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 50;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 奇术师</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunE11SR)
            };
                MenuInit();
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += start;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= start;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }

            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        player.EnableEffect(EffectType.Fade, 255, 0f);
                        Plugin.Register(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
            protected override void RoleRemoved(Player player)
            {
                Plugin.Unregister(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));

                base.RoleRemoved(player);
            }
            public static void MenuInit()
            {
                var m = new List<SettingBase>() {
                    new ButtonSetting(Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce], "修改引力", "", 0.5f, "使周围50m内所有人受到随机引力改变并给队友上伤害抗性", onChanged: (player, sb) =>
                    {
                        foreach (var p in player.CurrentRoom.Players)
                        {
                            //if (Vector3.Distance(player.Position,p.Position) <= 50f)
                            {
                                if (!HitboxIdentity.IsEnemy(p.ReferenceHub,player.ReferenceHub))
                                {
                                    p.EnableEffect(EffectType.DamageReduction, 20, 30f);
                                }
                                if(p.Role is FpcRole fpcRole)
                                {
                                    Vector3 targetG = new Vector3(
                                        UnityEngine.Random.Range(-10f,10f),
                                        UnityEngine.Random.Range(-10f,10f),
                                        UnityEngine.Random.Range(-10f,10f)
                                        );
                                    fpcRole.Gravity += targetG;
                                    Timing.CallDelayed(30f, () =>
                                    {
                                        fpcRole.Gravity -= targetG;
                                    });
                                }
                            }
                        }
                    })
                };
                Plugin.MenuCache.AddRange(m);
            }
        }
        public static uint O1NID = 60;
        [CustomRole(RoleTypeId.NtfPrivate)]
        public class scp5k_Omega1_N : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Omega1_N instance { get; private set; }
            public override uint Id { get; set; } = O1NID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Omega-1 小队 队员";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Omega-1 小队 队员";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Omega 1";

            public string ShowingToPlayer => "Omega-1";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 60;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 队员</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunCrossvec)
            };
                //MenuInit();
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += start;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= start;
                //Exiled.Events.Handlers.Player.Verified -= OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating -= OnDecontaminating;
            }

            protected override void RoleAdded(Player player)

            {
                Timing.CallDelayed(0.4f, () =>
                {
                    if (player != null)
                    {
                        player.EnableEffect(EffectType.Fade, 255, 0f);
                        //Plugin.Register(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }
        public static void TrySpawnO1(List<Player> candidates, bool forced = false, bool imm = false)
        {
            int chaos = 0, scp = 0;

            foreach (var h in ReferenceHub.AllHubs)
            {
                var r = h.roleManager.CurrentRole.RoleTypeId;
                if (!r.IsAlive()) continue;
                if (r.IsScp() || r.IsNtf()) scp++; else chaos++;
            }

            if (chaos - scp > config.HammerSpawnCount || forced)
            {
                Log.Info("O1 wave triggered");
                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                if (w != null)
                {
                    if (w.RespawnTokens > 0)
                        w.RespawnTokens--;

                    w.Timer.Reset();
                    if (!imm) Exiled.API.Features.Respawn.SummonNtfChopper();
                    Timing.RunCoroutine(O1SpawnCoroutine(w, candidates, imm));
                }
            }
        }
        private static IEnumerator<float> O1SpawnCoroutine(NtfSpawnWave w, List<Player> spawntarget, bool imm = false)
        {
            if (!imm)
            {
                if (w != null)
                {
                    yield return Timing.WaitForSeconds(w.AnimationDuration);
                }
            }
            var HammerWave = new List<Player>(spawntarget.Take(Math.Min(config.HammerMaxCount, spawntarget.Count - 1)));
            if (HammerWave.Count == 0)
            {
                yield break;
            }
            spawntarget.RemoveRange(0, Math.Min(config.HammerMaxCount, spawntarget.Count - 1));
            scp5k_Omega1_P.instance.AddRole(HammerWave[0]);
            spawntarget.RemoveRange(0, 1);
            foreach (var item in HammerWave)
            {
                if (UnityEngine.Random.Range(0, 100) < 40)
                {
                    scp5k_Omega1_S.instance.AddRole(item);
                }
                else
                {
                    scp5k_Omega1_N.instance.AddRole(item);
                }


            }
            if (true)
            {
                Exiled.API.Features.Cassie.MessageTranslated("Mobile Task Force Unit Omega 1 has entered the facility", "机动特遣队Omega-1小队已进入设施。");
                //HammerSpawnedBroadcast = true;
            }

            yield break;
        }
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
        public static void OnRoundStart()
        {
                   //HammerSpawned = false;
        }
       
    }
}
