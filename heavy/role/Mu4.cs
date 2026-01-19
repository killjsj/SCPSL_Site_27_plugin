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
using Next_generationSite_27.UnionP.heavy.ability;
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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.Mu4;
using static Next_generationSite_27.UnionP.heavy.Scannner;
using static Next_generationSite_27.UnionP.heavy.SpeedBuilditem;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Mu4 : BaseClass // Non-5k todo
    {
        public static uint Mu4PID = 65;
        [CustomRole(RoleTypeId.NtfCaptain)]
        public class scp5k_Mu4_P : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Mu4_P instance { get; private set; }
            public override uint Id { get; set; } = Mu4PID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Mu-4 小队 队长";
            public override string Description { get => Scp5k_Control.Is5kRound ? "连接Scp079" : "断开Scp-079"; set { } } 
            public override string CustomInfo { get; set; } = "Mu-4 小队 队长";
            public override Exiled.API.Features.Broadcast Broadcast { get => new Exiled.API.Features.Broadcast($"<size=40><color=red>你是 Mu-4 小队 队长</color></size>\n<size=30><color=yellow>{Description}</color></size>", 4); set { } }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Mu 4";

            public string ShowingToPlayer => "Mu-4";

            public override void Init()
            {
                this.Role = RoleTypeId.NtfCaptain;
                MaxHealth = 130;
                this.IgnoreSpawnSystem = true;
                instance = this;
                abilities.Add(new DebuggersAbility1());
                abilities.Add(new DebuggersAbility2());
                //abilities.Add(new TestAbility1());
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
                        CH[player.UserId] = Plugin.RunCoroutine(Mu4PlayerUpdate(player));
                    if (player != null)
                    {
                        player.EnableEffect(EffectType.MovementBoost, 40, 0f);

                    }
                });

                base.RoleAdded(player);
            }
        }
        public static uint Mu4SID = 66;
        [CustomRole(RoleTypeId.NtfSergeant)]
        public class scp5k_Mu4_S : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Mu4_S instance { get; private set; }
            public override uint Id { get; set; } = Mu4SID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Mu-4 小队 重装";
            public override string Description { get => Scp5k_Control.Is5kRound ? "连接Scp079" : "断开Scp-079"; set { } }
            public override string CustomInfo { get; set; } = "Mu-4 小队 重装";
            public override Exiled.API.Features.Broadcast Broadcast { get => new Exiled.API.Features.Broadcast($"<size=40><color=red>你是 Mu-4 小队 重装</color></size>\n<size=30><color=yellow>{Description}</color></size>", 4); set { } }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Mu 4";

            public string ShowingToPlayer => "Mu-4";
            public override void Init()
            {
                //Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 120;
                //Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 奇术师</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
                this.IgnoreSpawnSystem = true;
                instance = this;
                //abilities.Add(new DebuggersAbility1());
                abilities.Add(new DebuggersAbility2());
                abilities.Add(new DebuggersAbility3());
                this.Inventory = new List<string>()
            {
                string.Format("{0}", ItemType.ArmorHeavy),
                string.Format("{0}", ItemType.Medkit),
                string.Format("{0}", ItemType.Jailbird),
                string.Format("{0}", ItemType.KeycardMTFOperative),
                string.Format("{0}", ItemType.SCP207),
                string.Format("{0}", ItemType.GunE11SR)
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
                        CH[player.UserId] = Plugin.RunCoroutine(Mu4PlayerUpdate(player));
                    if (player != null)
                    {
                        player.EnableEffect(EffectType.MovementBoost, 60, 0f);
                        player.EnableEffect(EffectType.SilentWalk, 40, 0f);
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
            protected override void RoleRemoved(Player player)
            {
                //Plugin.Unregister(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));

                base.RoleRemoved(player);
            }
        }
        public static uint Mu4NID = 67;
        [CustomRole(RoleTypeId.NtfPrivate)]
        public class scp5k_Mu4_N : CustomRolePlus, IDeathBroadcaster
        {

            public static scp5k_Mu4_N instance { get; private set; }
            public override uint Id { get; set; } = Mu4NID;
            public override int MaxHealth { get; set; }
            public override string Name { get; set; } = "Mu-4 小队 队员";
            public override string Description { get => Scp5k_Control.Is5kRound ? "连接Scp079" : "断开Scp-079"; set { } }
            public override string CustomInfo { get; set; } = "Mu-4 小队 队员";
            public override Exiled.API.Features.Broadcast Broadcast { get => new Exiled.API.Features.Broadcast($"<size=40><color=red>你是 Mu-4 小队 队员</color></size>\n<size=30><color=yellow>{Description}</color></size>", 4); set { } }
            public override RoleTypeId Role { get => base.Role; set => base.Role = value; }
            //public override Vector3 Scale { get => new Vector3(1.5f, 1, 1.5f); set => base.Scale = value; }
            public override List<string> Inventory { get => base.Inventory; set => base.Inventory = value; }
            public string CassieBroadcast => "Mu 4";

            public string ShowingToPlayer => "Mu-4";
            public override void Init()
            {
                //Description = "帮助基金会消灭全部人类";
                this.Role = RoleTypeId.NtfSergeant;
                MaxHealth = 100;
                //Broadcast = new Exiled.API.Features.Broadcast("<size=40><color=red>你是 Omega-1 小队 队员</color></size>\n<size=30><color=yellow>帮助基金会消灭全部人类</color></size>", 4);
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
                        CH[player.UserId] = Plugin.RunCoroutine(Mu4PlayerUpdate(player));
                    if (player != null)
                    {
                        //player.EnableEffect(EffectType.Fade, 255, 0f);
                        //Plugin.Register(player, Plugin.MenuCache.Where(x => x.Id == Plugin.Instance.Config.SettingIds[Features.Omega1ChangeGForce]));
                        player.EnableEffect(EffectType.MovementBoost, 30, 0f);
                        player.EnableEffect(EffectType.SilentWalk, 60, 0f);
                        SpeedBuildItem.instance.Give(player);
                    }
                });

                base.RoleAdded(player);
            }
        }
        public static void TrySpawnMu4(List<Player> candidates, bool forced = false, bool imm = false)
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
                Log.Info("Mu4 wave triggered");
                var w = WaveManager.Waves.FirstOrDefault(x => x is NtfSpawnWave) as NtfSpawnWave;
                if (w != null)
                {
                    if (w.RespawnTokens > 0)
                        w.RespawnTokens--;

                    w.Timer.Reset();
                    if (!imm) Exiled.API.Features.Respawn.SummonNtfChopper();
                    Timing.RunCoroutine(Mu4SpawnCoroutine(w, candidates, imm));
                }
            }
        }
        private static IEnumerator<float> Mu4SpawnCoroutine(NtfSpawnWave w, List<Player> spawntarget, bool imm = false)
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
            scp5k_Mu4_P.instance.AddRole(HammerWave[0]);
            spawntarget.RemoveRange(0, 1);
            foreach (var item in HammerWave)
            {
                if (UnityEngine.Random.Range(0, 100) < 40)
                {
                    scp5k_Mu4_S.instance.AddRole(item);
                }
                else
                {
                    scp5k_Mu4_N.instance.AddRole(item);
                }


            }
            if (true)
            {
                Exiled.API.Features.Cassie.MessageTranslated("Mobile Task Force Unit Mu 4 has entered the facility", "机动特遣队Mu-4小队已进入设施。");
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
        public static float Mu4InstallTick
        {
            get
            {
                {
                    var count = scp5k_Mu4_N.instance.TrackedPlayers.Count + scp5k_Mu4_P.instance.TrackedPlayers.Count + scp5k_Mu4_S.instance.TrackedPlayers.Count;
                    if (count > 0)
                    {
                        // 目标：6人 → 90秒 → 每秒总进度 = 100/90
                        // 每人每秒贡献 = (100/90) / 6
                        return (100f / 50f) / 5f * count;
                    }
                }
                return 0.2f; // 默认极慢速度（无人时）
            }
        }
        //public static bool Enabled = false;

        public static bool Mu4Spawned = false;
        public static bool Mu4_broadcasted = false;
        public static bool _Mu4Escaped = false;
        public static float Mu4InstallTime = 0;
        public static int Mu4InServerRoom = 0;
        public static void OnRoundStart()
        {
            Mu4InServerRoom = 0;
            _Mu4Escaped = false;
            Mu4_broadcasted = false;
            Mu4Spawned = false;
            Mu4InstallTime = 0;
            Plugin.RunCoroutine(Mu4BackendUpdate());


        }
        public static IEnumerator<float> Mu4BackendUpdate()
        {
            bool Downloaded = false;
            while (true)
            {
                try
                {
                    if (Mu4InstallTime >= 100f)
                    {
                        Downloaded = true;
                                    Plugin.RunCoroutine(Scp079Spawner());
                        if (!Mu4_broadcasted)
                        {
                            Mu4_broadcasted = true;
                        }
                        Mu4InstallTime = 100f;
                    }
                    Mu4InServerRoom = 0;
                    foreach (var item in scp5k_Mu4_N.instance.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                Mu4InServerRoom += 1;
                            }
                        }
                    }
                    foreach (var item in scp5k_Mu4_S.instance.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                Mu4InServerRoom += 1;
                            }
                        }
                    }
                    foreach (var item in scp5k_Mu4_P.instance.TrackedPlayers)
                    {
                        if (item.CurrentRoom != null)
                        {
                            if (item.CurrentRoom.Type == RoomType.HczServerRoom)
                            {
                                Mu4InServerRoom += 1;
                            }
                        }
                    }
                    if (!Downloaded)
                    {
                        if (Mu4InServerRoom > 0)
                        {
                            Mu4InstallTime += Mu4InstallTick * 0.2f * Mu4InServerRoom;
                        }
                        else
                        {
                            if (Mu4InstallTime > 0f)
                                Mu4InstallTime -= Mu4InstallTick * 0.2f;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.2f);
            }
        }
        public static Dictionary<string, CoroutineHandle> CH = new Dictionary<string, CoroutineHandle>();

        //public static bool Mu4DownloadBroadcasted = false;
        //static bool IEnableAble.Enabled { get ; set; }

        public static uint Scp079BetterID = 68;
        [CustomRole(RoleTypeId.Scp079)]
        public class Scp079Better : CustomRolePlus
        {
            public static Scp079Better ins {  get; set; }
            public override uint Id { get; set; } = Scp079BetterID;
            public override int MaxHealth { get; set; } = 100;
            public override string Name { get; set; } = "Scp-079";
            public override string Description { get; set; } = "Scp-079";
            public override string CustomInfo { get; set; } = "";
            public override void Init()
            {
                ins = this;
                Role = RoleTypeId.Scp079;
                abilities.Add(new Scp079Ability1());
                base.Init();
            }
        }
        public static IEnumerator<float> Scp079Spawner()
        {
            while (true)
            {
                var d = diedPlayer;
                    if (d.Count > 0)
                    {
                        var Luck = d.RandomItem();
                        Scp079Better.ins.AddRole(Luck);
                        Timing.CallDelayed(0.4f, () =>
                        {
                            if (Luck.Role is Scp079Role role)
                            {
                                role.Level = 5;
                                role.MaxEnergy += 200;

                            }
                        });
                    }
                
                yield return Timing.WaitForSeconds(0.3f);
            }
        }
        public static IEnumerator<float> Mu4PlayerUpdate(Player player)
        {
            bool Downloaded = false;
            var p = player;
            while (true)
            {
                try
                {
                    if (player == null)
                    {
                        yield break;
                    }
                    var hud = HSM_hintServ.GetPlayerHUD(player);
                    if (hud == null)
                    {
                        yield break;
                    }

                    if (!Mu4.scp5k_Mu4_N.instance.Check(player) && !scp5k_Mu4_P.instance.Check(player) && !scp5k_Mu4_N.instance.Check(player))
                    {
                        break;
                    }
                    if (!Downloaded)
                    {
                        if (player.CurrentRoom != null && player.CurrentRoom.RoomName != RoomName.Unnamed)

                        {

                            if (player.CurrentRoom.Type == RoomType.Hcz079)
                            {
                                if (Mu4InstallTime >= 100f)
                                {
                                    Downloaded = true;
                                    if (hud.HasMessage("Mu4downloading"))
                                        hud.RemoveMessage("Mu4downloading");
                                    hud.AddMessage("Mu4_DOWNLOAD_DONE" + DateTime.Now.ToString(),
                                                   "<size=30><color=red>你已成功加载Scp-079,请尽快撤离!</color></size>",
                                                   4f, ScreenLocation.CenterBottom);

                                }
                                else
                                {
                                    float remainTime = 100;

                                    if (!hud.HasMessage("Mu4downloading"))
                                    {
                                        hud.AddMessage(
                                            "Mu4downloading",
                                            (x) =>
                                            {
                                                if (Mu4InstallTime >= 100f)
                                                {
                                                    if (hud.HasMessage("Mu4downloading"))
                                                        hud.RemoveMessage("Mu4downloading");
                                                    return Array.Empty<string>();
                                                }
                                                remainTime = Mu4InstallTick > 0f ? (100f - Mu4InstallTime) / Mu4InstallTick * Mu4InServerRoom : float.PositiveInfinity;

                                                return new string[]
                                                {
                        $"<size=30><color=red>你正在安装Scp-079,请勿离开电脑房! 进度: {Mu4InstallTime:F1}% 预计结束: {remainTime:F1} 秒</color></size>"
                                                };
                                            },
                                            -1f,
                                            ScreenLocation.CenterBottom
                                        );
                                    }
                                }

                            }
                            else
                            {

                                if (hud.HasMessage("Mu4downloading"))
                                {
                                    hud.RemoveMessage("Mu4downloading");
                                }
                            }
                        }
                        else
                        {
                            if (hud.HasMessage("Mu4downloading"))
                            {
                                hud.RemoveMessage("Mu4downloading");
                            }
                        }
                    }
                    else
                    {

                    }
                }


                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.2f);

            }
            if (p.HasMessage("Mu4downloading" + player.Nickname))
            {
                p.RemoveMessage("Mu4downloading" + player.Nickname);
            }
            //Log.Debug("Out!");

        }

    }
}
