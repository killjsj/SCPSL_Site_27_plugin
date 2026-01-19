using Exiled.API.Extensions;
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
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Nu7 : BaseClass
    {
        public static uint Nu7PID = 44;
        [CustomRole(RoleTypeId.NtfCaptain)]
        public class scp5k_Nu7_P : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Nu7_P instance { get; private set; }
            public override uint Id { get; set; } = Nu7PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Nu-7 小队 队长";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Nu-7 小队 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }

            public string CassieBroadcast => "NU 7";

            public string ShowingToPlayer => "Nu-7";

            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfCaptain;
                MaxHealth = 150;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Nu-7 小队 队长</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.Jailbird),
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
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
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

                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint Nu7SID = 45;
        [CustomRole(RoleTypeId.NtfSergeant)]
        public class scp5k_Nu7_S : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Nu7_S instance { get; private set; }
            public override uint Id { get; set; } = Nu7SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Nu-7 小队 重装";
            public override string Description { get; set; }
            public override string CustomInfo { get; set; } = "Nu-7 小队 重装";
            public override Exiled.API.Features.Broadcast Broadcast { get => base.Broadcast; set => base.Broadcast = value; }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "NU 7";

            public string ShowingToPlayer => "Nu-7";
            public override void Init()
            {
                Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 135;
                Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Nu-7 小队 重装</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
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
                base.Init();
            }
            // 电脑:
            //     EzOfficeLarge,
            //     EzOfficeSmall, 
            public void SubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying += OnDying;
                //Exiled.Events.Handlers.Player.Hurting += OnHurting;
                //Exiled.Events.Handlers.Player.Verified += OnVerified;
                //Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
                //Exiled.Events.Handlers.Map.Decontaminating += OnDecontaminating;
            }
            public void UnSubEvent()
            {
                //Exiled.Events.Handlers.Player.Dying -= OnDying;
                //Exiled.Events.Handlers.Player.Hurting -= OnHurting;
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
                        //SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }
        //public static bool Enabled = false;
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
                   HammerSpawned = false;
         HammerSpawnedBroadcast = false;
        }
        public static void TrySpawnHammer(List<Player> candidates, bool imm = false)
        {
            //if (chaos - scp > config.HammerSpawnCount || imm)
            {
                Log.Info("Hammer wave triggered");
                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                if (w != null)
                {
                    if (w.RespawnTokens > 0)
                        w.RespawnTokens--;

                    w.Timer.Reset();
                    if (!imm) Exiled.API.Features.Respawn.SummonNtfChopper();
                    Timing.RunCoroutine(HammerSpawnCoroutine(w, candidates, imm));
                }
            }
        }

        public static bool HammerSpawned = false;
        public static bool HammerSpawnedBroadcast = false;

        public static IEnumerator<float> HammerSpawnCoroutine(NtfSpawnWave w, List<Player> spawntarget, bool imm = false)
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
            scp5k_Nu7_P.instance.AddRole(HammerWave[0]);
            spawntarget.RemoveRange(0, 1);
            foreach (var item in HammerWave)
            {
                if (UnityEngine.Random.Range(0, 100) < 40)
                {
                    scp5k_Nu7_S.instance.AddRole(item);
                }
                else
                {
                    scp5k_Nu7_S.instance.AddRole(item);
                }


            }
            HammerSpawned = true;
            if (!HammerSpawnedBroadcast)
            {
                Exiled.API.Features.Cassie.MessageTranslated("Mobile Task Force Unit Nu 7 has entered the facility", "机动特遣队Nu-7小队已进入设施。");
                //HammerSpawnedBroadcast = true;
            }

            yield break;
        }
    }
}
