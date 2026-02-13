using CommandSystem;
using CustomPlayerEffects;
using CustomRendering;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using GameCore;
using InventorySystem.Items.Usables.Scp244.Hypothermia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Next_generationSite_27.UnionP.Buffs
{
    public class Blizzard_Combined : BuffBase
    {
        public override BuffType Type => BuffType.Mixed;
        public override string BuffName => "暴风雪";

        public static Blizzard_Combined Instance { get; private set; }

        internal Dictionary<Int64, DateTime> LastHeat;

        internal Dictionary<Int64, bool> InBlizzard;

        internal CancellationTokenSource ctsBlz;

        internal readonly object _dictLock = new object();

        public override void Init()
        {

            Instance = this;

            ctsBlz = null;

            LastHeat = new Dictionary<Int64, DateTime>();
            InBlizzard = new Dictionary<Int64, bool>();

            Exiled.Events.Handlers.Player.UsedItem += OnUsingItem;
            Exiled.Events.Handlers.Player.Joined += OnPlayerJoined;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Player.Died += OnPlayerDied;
            Exiled.Events.Handlers.Warhead.Detonated += Detonated;
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;


            ctsBlz = new CancellationTokenSource();


            base.Init();
        }

        public override void Delete()
        {

            ctsBlz?.Cancel();
            ctsBlz?.Dispose();
            ctsBlz = null;

            LastHeat.Clear();
            InBlizzard.Clear();

            Exiled.Events.Handlers.Player.UsedItem -= OnUsingItem;
            Exiled.Events.Handlers.Player.Joined -= OnPlayerJoined;
            Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
            Exiled.Events.Handlers.Player.Died -= OnPlayerDied;
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Exiled.Events.Handlers.Warhead.Detonated -= Detonated;
            Exiled.Events.Handlers.Server.RespawnedTeam -= RespawnedTeam;

            Instance = null;

            base.Delete();

        }
        public void Detonated()
        {
            if (!CheckEnabled()) return;
            if(ctsBlz.IsCancellationRequested) return;
            foreach (var item in Player.Enumerable.Where(x=>x.IsAlive))
            {
                LastHeat[item.Id] = DateTime.UtcNow.AddSeconds(90);
                item.Broadcast(3, "你有90秒温暖保护 速战速决!");
            }
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            try
            {
                if (!CheckEnabled()) return;
                if (ctsBlz.IsCancellationRequested) return;
                foreach (var item in ev.Players)
                {
                    LastHeat[item.Id] = DateTime.UtcNow.AddSeconds(10);
                    item.Broadcast(3, "你有10秒温暖保护 快去地下!");
                }
            }
            catch (Exception ex) { 
                Log.Error(ex);
            }
        }
        public void RoundStarted()
        {

            if(!CheckEnabled()) return;
            _ = MEC.Timing.RunCoroutine(Bliz(ctsBlz.Token));
            Exiled.API.Features.Cassie.MessageTranslated("", "警告 检测到地表温度极具下降 预计持续10分钟! (使用药包进行保暖)");
        }
        private void OnUsingItem(UsedItemEventArgs ev)
        {
            if(!CheckEnabled()) return;
            if (ev.Item.Type == ItemType.Medkit)
            {

                lock (_dictLock)
                {

                    if (LastHeat.ContainsKey(ev.Player.Id))
                    {
                        LastHeat[ev.Player.Id] = DateTime.UtcNow;
                    }
                    else
                    {
                        LastHeat.Add(ev.Player.Id, DateTime.UtcNow);
                    }

                }

            }
        }

        private void OnPlayerJoined(JoinedEventArgs ev)
        {
            if(!CheckEnabled()) return;
            lock (_dictLock)
            {

                if (!LastHeat.ContainsKey(ev.Player.Id))
                {
                    LastHeat.Add(ev.Player.Id, DateTime.MinValue);
                }
                if (!InBlizzard.ContainsKey(ev.Player.Id))
                {
                    InBlizzard.Add(ev.Player.Id, false);
                }

            }
        }

        private void OnPlayerLeft(LeftEventArgs ev)
        {

            if(!CheckEnabled()) return;
            lock (_dictLock)
            {

                if (LastHeat.ContainsKey(ev.Player.Id))
                {
                    LastHeat.Remove(ev.Player.Id);
                }
                if (InBlizzard.ContainsKey(ev.Player.Id))
                {
                    InBlizzard.Remove(ev.Player.Id);
                }

            }
        }

        private void OnPlayerDied(DiedEventArgs ev)
        {
            if(!CheckEnabled()) return;
            Player player = ev.Player;

            lock (_dictLock)
            {
                if (LastHeat.ContainsKey(ev.Player.Id))
                {
                    LastHeat[player.Id] = DateTime.MinValue;
                }
                if (InBlizzard.ContainsKey(ev.Player.Id))
                {
                    InBlizzard[player.Id] = false;
                }
            }

        }

        // This is where config ends and commandhandler begins,
        // I hate codes all mushed up together but GEEZ, I ain't the one who wrote this thing...............

        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        [CommandHandler(typeof(GameConsoleCommandHandler))]

        public class BlizzardCommand : ICommand
        {
            public string Command => "Weather.Blizzard";
            public string[] Aliases => new string[] { "bliz" };
            public string Description => "Toggles the blizzard effect.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {

                string executer = "Remote/Console";

                Player player = Player.Get(sender); if (player != null) executer = player.Nickname;

                if (player != null || sender.CheckPermission(PlayerPermissions.FacilityManagement) == false)
                {
                    response = "You do not have permission to use this command.";

                    return false;
                }

                try
                {

                    string fArg = arguments.FirstOrDefault()?.ToLower();

                    if (fArg == "on")
                    {

                        bool BlizzardCanRun = (Instance.ctsBlz == null) || (Instance.ctsBlz.Token.IsCancellationRequested);

                        if (BlizzardCanRun)
                        {

                            Instance.ctsBlz?.Cancel();
                            Instance.ctsBlz?.Dispose();
                            Instance.ctsBlz = null;


                            Instance.ctsBlz = new CancellationTokenSource();
                            MEC.Timing.RunCoroutine(Blizzard_Combined.Instance.Bliz(Instance.ctsBlz.Token));


                            response = "Blizzard effect has been toggled, it'll probably last for 10m if not manually shutted.";

                        }
                        else
                        {
                            response = "Blizzard effect is already running.";

                        }

                    }
                    else if (fArg == "off")
                    {

                        bool BlizzardIsRunning = (Instance.ctsBlz != null) && (!Instance.ctsBlz.Token.IsCancellationRequested);

                        if (BlizzardIsRunning)
                        {

                            Blizzard_Combined.Instance.BlizEnd();

                            Instance.ctsBlz?.Cancel();
                            Instance.ctsBlz?.Dispose();
                            Instance.ctsBlz = null;



                            response = "Blizzard effect has been manually shutted.";

                        }

                        else
                        {
                            response = "Blizzard effect is not running.";
                        }

                    }
                    else
                    {
                        response = "Invalid argument. Use 'on' to start the blizzard effect or 'off' to stop it.";
                    }

                    return true;

                }
                catch (Exception e)
                {

                    response = $"An error occurred while executing blizzard: {e.Message} ";

                    Log.Info($"[Blizzard] An error occurred while {executer} was executing blizzard: {e}");

                    return false;

                }
            }

        }

        private IEnumerator<float> Bliz(CancellationToken Token)
        {

            DateTime StartTime = DateTime.UtcNow;
            while (!Token.IsCancellationRequested)
            {
                if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {

                    try
                    {


                        if (DateTime.UtcNow - StartTime > TimeSpan.FromMinutes(10)) ctsBlz.Cancel();

                        foreach (Player player in Player.List)
                        {

                            if (player.IsDead) continue;

                            lock (_dictLock)
                            {

                                bool IsBlizzarded = false;
                                InBlizzard.TryGetValue(player.Id, out IsBlizzarded);
                                DateTime LHeat = DateTime.MinValue;
                                LastHeat.TryGetValue(player.Id, out LHeat);

                                if (IsBlizzarded == false && InZone(player))
                                {

                                    SpawnFog(player);

                                    IsBlizzarded = InBlizzard[player.Id] = true;
                                }
                                else if (IsBlizzarded == true && InZone(player) == false)
                                {

                                    DisableBliz(player);

                                    IsBlizzarded = InBlizzard[player.Id] = false;

                                }

                                if (IsBlizzarded && DateTime.UtcNow - LHeat >= TimeSpan.FromSeconds(45)) player.EnableEffect<Hypothermia>(90, 9999f, false);
                                else if (IsBlizzarded && DateTime.UtcNow - LHeat < TimeSpan.FromSeconds(45) && player.TryGetEffect<Hypothermia>(out var garb)) player.DisableEffect<Hypothermia>();

                            }

                        }


                    }

                    catch (OperationCanceledException)
                    {

                        BlizEnd();

                        Log.Info($"[Blizzard] Blizzard effect has ended.");
                    }

                    catch (Exception e)
                    {

                        Log.Info($"[Blizzard] An error occurred in the blizzard effect handler: {e}");
                    }
                }
                yield return MEC.Timing.WaitForSeconds(0.5f);
            }
            Exiled.API.Features.Cassie.MessageTranslated("", "检测到地表温度回升");

        }
        private void SpawnFog(Player player)
        {

            player.EnableEffect<FogControl>();

            FogControl FogEffect = player.GetEffect<FogControl>();

            if (FogEffect == null)
            {

                Log.Warn($"Failed to get FogControl effect for player {player.Nickname}");

                return;

            }

            FogEffect.SetFogType(FogType.Scp244);

            FogEffect.Intensity = 255;

        }
        private void DisableBliz(Player player)
        {

            player.DisableEffect<FogControl>();
            player.DisableEffect<Hypothermia>();


        }
        internal bool InZone(Player player)
        {

            float Ypos = player.Position.y;

            if (Ypos >= 270f && Ypos <= 310f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void BlizEnd()
        {

            lock (_dictLock)
            {

                foreach (var player in Player.List.ToList())
                {

                    if (InBlizzard[player.Id]) DisableBliz(player);

                    InBlizzard[player.Id] = false;
                    LastHeat[player.Id] = DateTime.MinValue;
                }

            }

        }


    }
}