using AudioApi;
using CommandSystem;
using Exiled.API.Features;
using GameCore;
using HarmonyLib;
using InventorySystem.Items.Pickups;
using MEC;
using Mirror;
using NeteaseMusicAPI; // 假设命名空间
using Respawning.Waves;
using Scp914;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP
{
    public struct SongReq
    {
        public SongReq(string id, Player player) { this.id = id; this.player = player; }
        public string id;
        public Player player;

        public override bool Equals(object obj) => obj is SongReq other && other.id == id && other.player == player;
        public override int GetHashCode() => id.GetHashCode();
    }
    [HarmonyPatch(typeof(WaveSpawner))]
    public static class WaveSpawnerPatch
    {
        [HarmonyPatch("CanBeSpawned")]
        [HarmonyPrefix]
        public static bool Prefix(ReferenceHub player, ref bool __result)
        {
            if(LobbySOng.Ins.DummyHub.ReferenceHub == player)
            {
                __result = false;
                return false;
            }
            return true;

        }
    }
    [CommandHandler(typeof(ClientCommandHandler))]
    class OrderSongCommand : ICommand
    {
        public string Command => "orderSong";

        public string[] Aliases => new string[] { "os" };

        public string Description => "大厅点歌(限网易云 只能在回合未开始或管理手动允许使用) 用法:os id(网易云歌曲id)";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "点歌需要id!";
                return false;
            }
            if (LobbySOng.Ins.AdminOverride && !LobbySOng.Ins.AdminOverrideEnable)
            {
                response = "管理禁止点歌!";
                return false;
            }
            if (Round.InProgress && !LobbySOng.Ins.AdminOverrideEnable)
            {
                response = "回合已开始 禁止点歌!";
                return false;
            }
            var sr = new SongReq(arguments.Array[1], Player.Get(sender));
            LobbySOng.Ins.WaitForProcess.Enqueue(sr);
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class DisOrderSongCommand : ICommand
    {
        public string Command => "DisOrderSOng";

        public string[] Aliases => new string[] { "Dos" };

        public string Description => "限制大厅点歌 （toggle）用法:dos";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            LobbySOng.Ins.AdminOverride = !LobbySOng.Ins.AdminOverride;
            response = "Done!" + (LobbySOng.Ins.AdminOverride ? "已禁止" : "已同意");
            return true;
        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class EnOrderSongCommand : ICommand
    {
        public string Command => "EnOrderSOng";

        public string[] Aliases => new string[] { "Eos" };

        public string Description => "强制允许大厅点歌 （toggle）用法:eos";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            LobbySOng.Ins.AdminOverrideEnable = !LobbySOng.Ins.AdminOverrideEnable;
            response = "Done!" + (!LobbySOng.Ins.AdminOverrideEnable ? "已禁止" : "已同意");
            return true;
        }
    }

    class LobbySOng : BaseClass
    {
        public static LobbySOng Ins { get; private set; }
        public override void Delete()
        {
            //Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= WaitingForPlayers;
            if (r.IsRunning) Timing.KillCoroutines(r);
            if (DummyHub != null) DummyHub.Destroy();
            Ins = null;
            VoicePlayerBase.OnFinishedTrack -= VoicePlayerBase_OnFinishedTrack;
        }

        public override void Init()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += WaitingForPlayers;
            //Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            Ins = this;
            VoicePlayerBase.OnFinishedTrack += VoicePlayerBase_OnFinishedTrack;
        }
        public void RoundStarted()
        {
            if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }

                        readytonext = true;
        }
        public bool AdminOverride { get => _AdminOverride;set
            {
                if (_AdminOverride != value)
                {
                    if (value)
                    {
                        if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }
                        readytonext = true;
                    }
                }
                _AdminOverride = value;
            }
        }
        public bool _AdminOverride =false;
        public bool AdminOverrideEnable =false;
        bool SongAble => Round.IsLobby || AdminOverrideEnable;
        public readonly ConcurrentQueue<SongReq> WaitForProcess = new();
        SongReq Processing;
        CoroutineHandle r;
        readonly NeteaseAPI api = new();
        public Npc DummyHub;
        VoicePlayerBase vpb;
        bool readytonext = true;

        void WaitingForPlayers()
        {
            //createDummy();
            if (r.IsRunning) Timing.KillCoroutines(r);
            r = Timing.RunCoroutine(Processer());
            _AdminOverride = false;
            AdminOverrideEnable = false;
        }
        void createDummy()
        {
            if (DummyHub != null){ DummyHub.Destroy();DummyHub = null; }
            DummyHub = Npc.Spawn("音乐播放器");
            //Plugin.plugin.eventhandle.SPD.Add(DummyHub.ReferenceHub);
            DummyHub.ReferenceHub.serverRoles.NetworkHideFromPlayerList = true;
            //Intercom.TrySetOverride(DummyHub, true);
            vpb = VoicePlayerBase.Get(DummyHub.ReferenceHub);
            vpb.BroadcastChannel = VoiceChat.VoiceChatChannel.Intercom;
        }

        private void VoicePlayerBase_OnFinishedTrack(TrackFinishedEventArgs obj)
        {
            if(obj == null) return;
            if(obj.VoicePlayerBase == vpb)
            {
                readytonext = true;
                if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }

                File.Delete(obj.Track);
            }
        }

        IEnumerator<float> Processer()
        {
            Log.Info("start!");
            while (true)
            {
                yield return Timing.WaitForSeconds(0.4f);
                if (SongAble && readytonext && WaitForProcess.TryDequeue(out Processing))
                {
                    if (!long.TryParse(Processing.id, out long songId)) { /* 错误处理 */ continue; }
                    readytonext = false;

                    // 用 Task.Run offload 到线程池（比 new Thread 更好，自动管理）
                    Task.Run(async () => await ProcessSongAsync(songId  ));
                }
            }
            Log.Info("exit!");

            yield break;
        }

        private async Task ProcessSongAsync(long songId)
        {
            int retries = 3;
            while (retries-- > 0)
            {
                try
                {
                    Log.Info($"Loading {songId}");
                    Processing.player?.SendConsoleMessage($"歌曲加载 - 开始处理{songId} 解析中", "yellow");  // Same assumption as above
                    var urlTask = api.GetSongUrl(songId, QualityLevel.STANDARD, new Dictionary<string, string>());
                    var detailTask = api.GetSongDetail(songId);
                    await Task.WhenAll(urlTask, detailTask);

                    var url = await urlTask;  // Use await instead of .Result
                    var del = await detailTask;
                    Log.Info($"Loading {songId} - ana");
                    if( url.exception != null)
                    {
                        Log.Info($"Loading {songId} - exception {url.exception}");
                        Processing.player?.SendConsoleMessage($"歌曲加载错误 - {url.exception}", "red");  // Same assumption as above

                        continue;
                    }
                    Processing.player?.SendConsoleMessage($"歌曲加载 - 解析完成", "green");  // Same assumption as above
                    if (url.result.data.Count > 0)
                    {
                        var target = url.result.data[0];
                        var name = del.result.songs[0].name;

                        Log.Info($"Loading {songId} - downlaoding");
                        Processing.player?.SendConsoleMessage("歌曲加载 - 下载中", "green");  // Same assumption as above

                        var p = Path.GetTempFileName();
                        var pn = Path.GetTempFileName() + ".ogg";

                        try
                        {
                            using (var c = new HttpClient())
                            {
                                var response = await c.GetAsync(target.url);  // Async Get
                                using (var f = File.OpenWrite(p))
                                {
                                    await response.Content.CopyToAsync(f);  // Async Copy
                                }
                            }
                            Log.Info($"Loading {songId} - decoding");
                            Processing.player?.SendConsoleMessage("歌曲加载 - 解码中", "green");  // Same assumption as above

                            NeteaseAPI.ConvertToOggMono48kHz(p, pn);  // Assuming this is sync; if async, await it
                            Timing.CallDelayed(0f, () =>
                            {
                                createDummy();
                                Timing.CallDelayed(0.5f, () =>
                                {
                                    //DummyHub.RoleManager.ServerSetRole(PlayerRoles.RoleTypeId.Overwatch,PlayerRoles.RoleChangeReason.None);
                                    vpb.BroadcastChannel = VoiceChat.VoiceChatChannel.Intercom;
                                    if (!_AdminOverride && SongAble)
                                    {


                                        DummyHub.ReferenceHub.nicknameSync.MyNick = $"正在播放 - {name}";
                                        DummyHub.ReferenceHub.nicknameSync.Network_myNickSync = $"正在播放 - {name}";
                                        vpb.Enqueue(pn, -1);
                                        vpb.Play(0);
                                        Log.Info($"Loading {songId} done!");
                                        Processing.player?.SendConsoleMessage("歌曲加载成功!", "green");  // Same assumption as above
                                    } else
                                    {
                                        readytonext = true;
                                        if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }
                                        Processing.player?.SendConsoleMessage($"歌曲加载 - 处理失败 禁止点歌", "red");  // Same assumption as above

                                    }
                                });
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"歌曲 {Processing.id} 处理失败 HttpClient: {ex}");
                            Processing.player?.SendConsoleMessage($"歌曲加载 - 处理失败 {ex}", "red");  // Same assumption as above
                            if(retries == 0)
                            {
                                readytonext = true;
                                if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }

                            }
                        }
                        finally
                        {
                            File.Delete(p);
                            // File.Delete(pn);  // Uncomment if VoicePlayerBase copies the file internally and doesn't need it after enqueue
                        }

                        break;  // Success, exit retry loop
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"歌曲 {Processing.id} 处理失败: {ex}");
                    if (retries == 0)
                    {
                        Processing.player?.SendConsoleMessage($"歌曲加载失败 {ex}", "red");  // Same assumption as above
                        readytonext = true;
                    }
                    else
                    {
                        await Task.Delay(1000);  // Delay before retry (1 second)
                    }
                }
            }
        }
    }
}