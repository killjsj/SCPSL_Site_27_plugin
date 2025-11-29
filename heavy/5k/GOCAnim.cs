using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.Commands.Reload;
using Exiled.Events.EventArgs.Player;
using Exiled.Loader;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.heavy;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using Subtitles;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using static Next_generationSite_27.UnionP.heavy.Goc;


namespace Next_generationSite_27.UnionP.Scp5k
{
    class GOCAnim
    {
        //public static AssetBundle ass;
        public static Animator _animator;
        public static GameObject camera;
        public static byte startID = 0;
        public static byte idleID = 0;
        public static byte donateID = 0;
        public static bool donating = false;
        //public static AnimationClip donate;
        //public static AnimationClip start;
        public static void Load()
        {
            DefaultAudioManager.RegisterAudio("GocNukeIdle", () =>
                    File.OpenRead($"{Paths.Configs}\\Plugins\\union_plugin\\GocNukeIdle.wav"));
            DefaultAudioManager.RegisterAudio("GocNukeStart", () =>
                File.OpenRead($"{Paths.Configs}\\Plugins\\union_plugin\\GocNukeStart.wav"));
            DefaultAudioManager.RegisterAudio("GocDonateMusic", () =>
                File.OpenRead($"{Paths.Configs}\\Plugins\\union_plugin\\GocNukeDonate.wav"));
            //ass = AssetBundle.LoadFromFile($"{Paths.Configs}\\Plugins\\union_plugin\\gocanim");
            //Log.Info($"{Paths.Configs}\\Plugins\\union_plugin\\gocanim");
            //idle = ass.LoadAsset<AnimationClip>("donate");
            //donate = ass.LoadAsset<AnimationClip>("idle");
            //start = ass.LoadAsset<AnimationClip>("start");
        }
        public static void Gen(Vector3 pos)
        {

            var sk = new SerializableSchematic
            {
                SchematicName = "GocBomb",
                Position = pos
            };

            Log.Info($"5kEffect Loaed!");
            GameObject skg = sk.SpawnOrUpdateObject();
            Log.Info($"5kEffect spawned at:{skg.transform.position}");
            var SO = skg.gameObject.GetComponent<SchematicObject>();
            //bool jump = false;
            foreach (GameObject gameObject in SO.AttachedBlocks)
            {
                //if (jump) break;
                switch (gameObject.name)
                {
                    case "GocEffect":
                        {
                            _animator = gameObject.GetComponent<Animator>();
                            //jump = true;
                            break;
                        }
                    case "camera":
                        {
                            camera = gameObject;
                            //jump = true;
                            break;
                        }
                }
            }
            foreach (var item in Room.List)
            {
                item.Color = Color.red;

            }
            _animator.SetBool("end", false);
            foreach (var item in Player.Enumerable)
            {
                item.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 1, 600f);
                item.EnableEffect(Exiled.API.Enums.EffectType.SoundtrackMute, 1, 600f);
            }
            //jump = false;
            startID = DefaultAudioManager.Instance.PlayGlobalAudioWithFilter(
"GocNukeStart", loop: false,
volume: 0.8f,
priority: AudioPriority.High,
configureSpeaker: null,
queue: true,
fadeInDuration: 1f,
persistent: false,
lifespan: 0f,
autoCleanup: false);
            Plugin.RunCoroutine(In(_animator));
            GOCBOmb = skg;

        }
        public static IEnumerator<float> In(Animator an)
        {

            if (Round.IsEnded)
            {
                Log.Info("IsEnded");

                yield break;
            }
            AnimatorStateInfo stateInfo = an.GetCurrentAnimatorStateInfo(0);
            Log.Info("IsName");

            // 等待动画切换到目标动画
            while (!stateInfo.IsName("idle"))
            {
                yield return Timing.WaitForSeconds(0.1f);
                stateInfo = an.GetCurrentAnimatorStateInfo(0);
                if (Round.IsEnded)
                {
                    Log.Info("IsEnded");
                    yield break;
                }
            }
            if (startID != 0)
            {
                DefaultAudioManager.Instance.FadeOutAudio(startID, 2f);
                startID = 0;
            }
            if (idleID == 0)
            {
                idleID = DefaultAudioManager.Instance.PlayGlobalAudioWithFilter(
            "GocNukeIdle", loop: true,
volume: 0.8f,
priority: AudioPriority.High,
configureSpeaker: null,

queue: true,
fadeInDuration: 1.5f,
persistent: false,
lifespan: 0f,
autoCleanup: false);
            }


        }
        [Server]

        public static void PlayIdle()
        {
            _animator.Play("idle");
        }
        static CoroutineHandle endC;
        [Server]

        public static void PlayEnd()
        {
            _animator.SetBool("end", true);
            foreach (var item in Room.List)
            {
                item.ResetColor();
            }
            Exiled.API.Features.Cassie.Message($"GOC奇术核弹拆除完毕 终结所有GOC人员", isSubtitles: true);

            if (idleID != 0)
            {
                DefaultAudioManager.Instance.FadeOutAudio(idleID, 15f);
                idleID = 0;
            }
            endC = Plugin.RunCoroutine(quit(_animator));
        }
        [Server]

        public static void StopEnd()
        {
            _animator.SetBool("end", false);
            if (idleID != 0)
            {
                DefaultAudioManager.Instance.FadeInAudio(idleID, 1.5f);
            }
            else
            {
                idleID = DefaultAudioManager.Instance.PlayGlobalAudioWithFilter(
"GocNukeIdle", loop: true,
volume: 0.8f,
priority: AudioPriority.High,
configureSpeaker: null,
queue: false,
fadeInDuration: 1f,
persistent: false,
lifespan: 0f,
autoCleanup: false);
            }
            if (endC != null && endC.IsRunning)
            {
                Timing.KillCoroutines(endC);
            }
        }

        [Server]
        public static void PlayDonate()
        {
            if (donating) return;
            donating = true;
            _animator.SetBool("donate", true);
            Log.Info("donate:true");
            //FogController.DisableFogType(FogType.Outside,999f);
            Transform[] myTransforms = GOCBOmb.GetComponentsInChildren<Transform>();


            foreach (var child in myTransforms)
            {
                if (child.gameObject.name == "killer")
                {
                    Log.Info("RunCoroutine");
                    Plugin.RunCoroutine(OnKillerScaleChanged(child.gameObject, _animator));
                    break;
                }
            }
        }
        public static void OnchangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.IsAllowed)
            {
                if (ev.NewRole.IsAlive())
                {
                    if (donating)
                    {
                        Timing.CallDelayed(0.2f, () =>
                        {
                            if (ev.Player.Role.Base is IFpcRole i)
                            {
                                i.FpcModule.Motor.GravityController.Gravity = Vector3.zero;
                            }
                            ev.Player.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 1, 600f);
                            ev.Player.EnableEffect(Exiled.API.Enums.EffectType.SoundtrackMute, 1, 600f);
                            ev.Player.EnableEffect(Exiled.API.Enums.EffectType.Fade, 255, 45f);
                            Timing.RunCoroutine(CamUpdater(ev.Player), segment: Segment.LateUpdate);
                        });
                    }
                }
            }
        }
        public static IEnumerator<float> CamUpdater(Player player)
        {
            var lastEuler = camera.transform.eulerAngles;

            var start = player.InfoArea;
            var startPos = player.Position;
                    FakePlayerPos.SendFakePlayerPos(player, startPos);
            while (donating)
            {
                try
                {
                    if (player == null)
                        break;
                    if (!player.IsAlive)
                        break;
                    player.CurrentItem = null;
                    player.InfoArea = 0;
                    Vector3 currentEuler = camera.transform.eulerAngles;


                    // 处理 pitch（上下）
                    float pitch = currentEuler.x;
                    if (pitch > 180f) pitch -= 360f;
                    pitch = -Mathf.Clamp(pitch, -90f, 90f);

                    // 处理 yaw（左右）
                    float yaw = currentEuler.y;
                    if (yaw < 0f) yaw += 360f;
                    if (yaw > 360f) yaw -= 360f;

                    Vector2 rotation = new Vector2(pitch, yaw);

                    // 发送到服务器
                    player.ReferenceHub.TryOverrideRotation(rotation);
                    player.ReferenceHub.TryOverridePosition(camera.transform.position);

                    lastEuler = currentEuler;
                    //Log.Debug($"[CamUpdater] {player.Nickname} pitch={pitch:F2} yaw={yaw:F2} raw={currentEuler}");
                }
                catch (Exception e)
                {
                    Log.Warn(e);
                }

                yield return Timing.WaitForSeconds(0.02f);
            }
            player.InfoArea = start;
                    FakePlayerPos.RemoveSendFakePlayerPos(player);
        }

        public static IEnumerator<float> OnKillerScaleChanged(GameObject killer, Animator an)
        {
            Dictionary<Transform, Transform> playerToTransfrom = new Dictionary<Transform, Transform>();
            if (killer == null)
            {
                Log.Info("killer == null");
                yield break;
            }

            if (Round.IsEnded)
            {
                Log.Info("IsEnded");

                yield break;
            }

            AnimatorStateInfo stateInfo = an.GetCurrentAnimatorStateInfo(0);
            while (!stateInfo.IsName("donate"))
            {
                stateInfo = an.GetCurrentAnimatorStateInfo(0);
                yield return Timing.WaitForSeconds(0.06f);
                if (Round.IsEnded)
                {
                    Log.Info("IsEnded");
                    yield break;
                }
            }
            AnimatorClipInfo[] clipInfo = an.GetCurrentAnimatorClipInfo(0);
            string currentClipName = clipInfo[0].clip.name;
            Log.Info("IsName donate");
            try
            {
                if (idleID != 0)
                {
                    DefaultAudioManager.Instance.FadeOutAudio(idleID, 2f);
                    idleID = 0;
                }
                if (startID != 0)
                {
                    DefaultAudioManager.Instance.FadeOutAudio(startID, 2f);
                    startID = 0;
                }
                try
                {
                    //StaticSpeakerFactory.ClearSpeakers();
                    foreach (var item in Player.Enumerable.Where(x => x.IsAlive))
                    {

                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex);
                }
                if (donateID == 0)
                {
                    donateID = DefaultAudioManager.Instance.PlayGlobalAudioWithFilter("GocDonateMusic", false, 0.6f, AudioManagerAPI.Features.Enums.AudioPriority.Max, fadeInDuration: 0f, configureSpeaker: (x) => { x.Stop(); }, queue: false);
                    Stopwatch sw = new Stopwatch();
                    sw.Restart();
                    //foreach (var item in LabApi.Features.Wrappers.Player.GetAll())
                    //{
                    //    if (!item.HasMessage("DebugMusic"))
                    //    {
                    //        item.AddMessage("DebugMusic", (p) =>
                    //        {
                    //            stateInfo = an.GetCurrentAnimatorStateInfo(0);
                    //            return new string[]{
                    //        $"<pos=45%><color=yellow><size=27>debug:本地计时:{sw.Elapsed.TotalSeconds} 动画:{stateInfo.normalizedTime}</size></color></pos>"};

                    //        }, 80f, Enums.ScreenLocation.Center);
                    //    }

                    //}
                }
                try
                {
                    //StaticSpeakerFactory.ClearSpeakers();
                    foreach (var item in Player.Enumerable)
                    {
                        if (item.CameraTransform != null)
                        {
                            {
                                if (item.Role.Base is IFpcRole i)
                                {
                                    i.FpcModule.Motor.GravityController.Gravity = Vector3.zero;
                                }
                                item.EnableEffect(Exiled.API.Enums.EffectType.Fade, 255, 45f);
                                Timing.RunCoroutine(CamUpdater(item), segment: Segment.LateUpdate);
                            }
                        }
                    }
                }
                catch (Exception ex1)
                {
                    Log.Warn(ex1);
                }
            }
            catch (Exception ex)
            {
                Log.Info(ex.ToString());

            }
            while (true)
            {
                if (killer.transform.localScale.y > 50)
                {
                    bool error = false;
                    try
                    {
                        var toy = killer.GetComponent<AdminToys.PrimitiveObjectToy>();
                        if (toy != null)
                            toy.NetworkPrimitiveFlags = AdminToys.PrimitiveFlags.Visible;

                        if (an == null)
                        {
                            Log.Info("an == null");
                            yield break;
                        }
                        var cam = LabApi.Features.Wrappers.CameraToy.Create(toy.transform);
                        Plugin.RunCoroutine(Scp079CameraUpdate(cam));
                    }
                    catch (Exception ex)
                    {
                        Log.Info(ex.ToString());
                        error = true;
                    }
                    if (error)
                        yield break;
                    foreach (var player in Player.Enumerable)
                    {
                        player.EnableEffect(Exiled.API.Enums.EffectType.Flashed, 1, 2f);
                    }
                    stateInfo = an.GetCurrentAnimatorStateInfo(0);
                    Log.Info("normalizedTime");

                    while (stateInfo.normalizedTime < 0.99f || !stateInfo.IsName("donate"))
                    {
                        yield return Timing.WaitForSeconds(0.02f);
                        stateInfo = an.GetCurrentAnimatorStateInfo(0);
                        if (Round.IsEnded)
                            yield break;
                    }
                    Log.Info("Kill");
                    try
                    {
                        //StaticSpeakerFactory.ClearSpeakers();
                        foreach (var item in Player.Enumerable)
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex);
                    }
                    Warhead.Shake();
                    foreach (var player in Player.Enumerable)
                    {
                        player.DisableEffect(Exiled.API.Enums.EffectType.Flashed);
                        player.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 6, 10f);
                    }
                    GocNuke = true;
                    foreach (var player in Player.Enumerable)
                    {
                        if (player.Role.Base is IFpcRole i)
                        {
                            i.FpcModule.Motor.GravityController.Gravity = FpcGravityController.DefaultGravity;
                        }
                            var d = new CustomReasonDamageHandler("goc奇术炸弹", -1f, "");
                                                
                        player.ReferenceHub.playerStats.DealDamage(d);

                    }
                    an.SetBool("donate", false);
                    donating = false;


                    yield break;
                }

                yield return Timing.WaitForSeconds(0.3f);
            }
        }

        private static IEnumerator<float> Scp079CameraUpdate(LabApi.Features.Wrappers.CameraToy cam)
        {
            var c = Exiled.API.Features.Camera.Get(cam.Camera.Base);
            
            while (donating)
            {
                foreach (var item in Player.Enumerable.Where(x=>x.Role.Type == RoleTypeId.Scp079))
                {
                    if(item.Role is Scp079Role scp079 && scp079.Camera != c)
                    {
                        scp079.Camera = c;
                    }
                }
                yield return Timing.WaitForSeconds(0.02f);
            }
        }

        public static IEnumerator<float> quit(Animator an)
        {

            if (Round.IsEnded)
            {
                Log.Info("IsEnded");

                yield break;
            }
            AnimatorStateInfo stateInfo = an.GetCurrentAnimatorStateInfo(0);
            Log.Info("IsName");

            // 等待动画切换到目标动画
            while (!stateInfo.IsName("end"))
            {
                yield return Timing.WaitForOneFrame;
                stateInfo = an.GetCurrentAnimatorStateInfo(0);
                if (Round.IsEnded)
                {
                    Log.Info("IsEnded");
                    yield break;
                }
            }
            Log.Info("normalizedTime");

            while (stateInfo.normalizedTime < 0.95f)
            {
                yield return Timing.WaitForOneFrame;
                stateInfo = an.GetCurrentAnimatorStateInfo(0);
                if (Round.IsEnded)
                    yield break;
            }
            GOCBOmb.GetComponent<SchematicObject>().Destroy();


            GOCBOmb = null;

        }
    }
}
