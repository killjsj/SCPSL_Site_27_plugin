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
using UnityEngine;

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
        public bool AdminOverride { get => _AdminOverride; set
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
        public bool _AdminOverride = false;
        public bool AdminOverrideEnable = false;
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
            if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }
            DummyHub = Npc.Spawn("音乐播放器");
            //Plugin.plugin.eventhandle.SPD.Add(DummyHub.ReferenceHub);
            DummyHub.ReferenceHub.serverRoles.NetworkHideFromPlayerList = true;
            //Intercom.TrySetOverride(DummyHub, true);
            vpb = VoicePlayerBase.Get(DummyHub.ReferenceHub);
            vpb.BroadcastChannel = VoiceChat.VoiceChatChannel.Intercom;
        }

        private void VoicePlayerBase_OnFinishedTrack(TrackFinishedEventArgs obj)
        {
            if (obj == null) return;
            if (obj.VoicePlayerBase == vpb)
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
                    ProcessSongAsync(songId);
                }
            }
            Log.Info("exit!");

            yield break;
        }

        private async Awaitable ProcessSongAsync(long songId)
        {
            int retries = 3;
            await Awaitable.BackgroundThreadAsync();
            while (retries >= 0)
            {
                try
                {
                    Log.Info($"Loading {songId}");
                    Processing.player?.SendConsoleMessage($"歌曲加载 - 开始处理{songId} 解析中", "yellow");
                    var urlTask = api.GetSongUrl(songId, NeteaseMusicAPI.QualityLevel.STANDARD, new Dictionary<string, string>());
                    var detailTask = api.GetSongDetail(songId);
                    await Task.WhenAll(urlTask, detailTask);

                    var url = await urlTask;
                    var del = await detailTask;
                    Log.Info($"Loading {songId} - ana");
                    if (url.exception != null)
                    {
                        Log.Info($"Loading {songId} - exception {url.exception}");
                        Processing.player?.SendConsoleMessage($"歌曲加载错误 - {url.exception}", "red");
                        if (retries == 0)
                        {
                            readytonext = true;
                            if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }
                            break;
                        }
                        continue;
                    }
                    Processing.player?.SendConsoleMessage($"歌曲加载 - 解析完成", "green");
                    if (url.result.data.Count > 0)
                    {
                        var target = url.result.data[0];
                        var name = del.result.songs[0].name;

                        Log.Info($"Loading {songId} - downlaoding");
                        Processing.player?.SendConsoleMessage("歌曲加载 - 下载中", "green");
                        var p = Path.GetTempFileName();
                        var pn = Path.GetTempFileName() + ".ogg";

                        try
                        {
                            using (var c = new HttpClient())
                            {
                                var response = await c.GetAsync(target.url);
                                using (var f = File.OpenWrite(p))
                                {
                                    await response.Content.CopyToAsync(f);
                                }
                            }
                            Log.Info($"Loading {songId} - decoding");
                            Processing.player?.SendConsoleMessage("歌曲加载 - 解码中", "green");
                            NeteaseAPI.ConvertToOggMono48kHz(p, pn);
                            await Awaitable.MainThreadAsync();
                            if (!_AdminOverride && SongAble) {
                                createDummy();
                                Timing.CallDelayed(0.5f, () => // wait for dummy init
                                {
                                    vpb.BroadcastChannel = VoiceChat.VoiceChatChannel.Intercom;
                                    if (!_AdminOverride && SongAble)
                                    {
                                        DummyHub.ReferenceHub.nicknameSync.MyNick = $"正在播放 - {name}";
                                        DummyHub.ReferenceHub.nicknameSync.Network_myNickSync = $"正在播放 - {name}";
                                        vpb.Enqueue(pn, -1);
                                        vpb.Play(0);
                                        Log.Info($"Loading {songId} done!");
                                        Processing.player?.SendConsoleMessage("歌曲加载成功!", "green");
                                    }
                                    else
                                    {
                                        readytonext = true;
                                        if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }
                                        Processing.player?.SendConsoleMessage($"歌曲加载 - 处理失败 禁止点歌", "red");

                                    }
                                });

                            }
                            else
                            {
                                Processing.player?.SendConsoleMessage($"歌曲加载 - 处理失败 禁止点歌", "red");

                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"歌曲 {Processing.id} 处理失败 HttpClient: {ex}");
                            Processing.player?.SendConsoleMessage($"歌曲加载 - 处理失败 {ex}", "red");
                            if (retries == 0)
                            {
                                readytonext = true;
                                if (DummyHub != null) { DummyHub.Destroy(); DummyHub = null; }
                                break;
                            }
                        }
                        finally
                        {
                            File.Delete(p);

                        }

                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"歌曲 {Processing.id} 处理失败: {ex}");
                    if (retries == 0)
                    {
                        Processing.player?.SendConsoleMessage($"歌曲加载失败 {ex}", "red");
                        readytonext = true;
                    }
                    else
                    {
                        await Awaitable.WaitForSecondsAsync(1000);
                    }
                }
                retries--;
            }
        }
    }
}