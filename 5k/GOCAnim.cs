using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Enums;
using AudioManagerAPI.Features.Static;
using AudioManagerAPI.Speakers.Extensions;
using AutoEvent.Interfaces;
using CustomRendering;
using Discord;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.Events.Commands.Reload;
using Exiled.Loader;
using MEC;
using Mirror;
using Next_generationSite_27.Features.PlayerHuds;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Next_generationSite_27.UnionP.Scp5k
{
    class GOCAnim
    {
        //public static AssetBundle ass;
        public static Animator _animator;
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
            bool jump = false;
            foreach (GameObject gameObject in SO.AttachedBlocks)
            {
                if (jump) break;
                switch (gameObject.name)
                {
                    case "GocEffect":
                        {
                            _animator = gameObject.GetComponent<Animator>();
                            jump = true;
                            break;
                        }
                }
            }
            _animator.SetBool("end", false);
            foreach (var item in Player.List)
            {
                item.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 1, 600f);
                item.EnableEffect(Exiled.API.Enums.EffectType.SoundtrackMute, 1, 600f);
            }
            jump = false;
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
            Timing.RunCoroutine(In(_animator));
            Scp5k.Scp5k_Control.GOCBOmb = skg;

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
            if (idleID != 0)
            {
                DefaultAudioManager.Instance.FadeOutAudio(idleID, 15f);
                idleID = 0;
            }
            endC = Timing.RunCoroutine(quit(_animator));
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
            Transform[] myTransforms = Scp5k.Scp5k_Control.GOCBOmb.GetComponentsInChildren<Transform>();


            foreach (var child in myTransforms)
            {
                if (child.gameObject.name == "killer")
                {
                    Log.Info("RunCoroutine");
                    Timing.RunCoroutine(OnKillerScaleChanged(child.gameObject, _animator));
                    break;
                }
            }
        }
        public static IEnumerator<float> OnKillerScaleChanged(GameObject killer, Animator an)
        {

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
                    DefaultAudioManager.Instance.FadeOutAudio(startID,2f);
                    startID = 0;
                }
                //StaticSpeakerFactory.ClearSpeakers();

                if (donateID == 0)
                {
                    donateID = DefaultAudioManager.Instance.PlayGlobalAudioWithFilter("GocDonateMusic", false, 0.6f, AudioManagerAPI.Features.Enums.AudioPriority.Max, fadeInDuration: 0f, configureSpeaker: (x) => { x.Stop(); }, queue: false);
                    Stopwatch sw = new Stopwatch();
                    sw.Restart();
                    foreach (var item in LabApi.Features.Wrappers.Player.GetAll())
                    {
                        if (!item.HasMessage("DebugMusic"))
                        {
                            item.AddMessage("DebugMusic", (p) =>
                            {
                                stateInfo = an.GetCurrentAnimatorStateInfo(0);
                                return new string[]{
                            $"<pos=45%><color=yellow><size=27>debug:本地计时:{sw.Elapsed.TotalSeconds} 动画:{stateInfo.normalizedTime}</size></color></pos>"};

                            }, 80f, Enums.ScreenLocation.Center);
                        }

                    }
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
                    }
                    catch (Exception ex)
                    {
                        Log.Info(ex.ToString());
                        error = true;
                    }
                    if (error)
                        yield break;

                    stateInfo = an.GetCurrentAnimatorStateInfo(0);
                    Log.Info("IsName");

                    // 等待动画切换到目标动画
                    while (!stateInfo.IsName("donate"))
                    {
                        yield return Timing.WaitForSeconds(0.1f);

                        stateInfo = an.GetCurrentAnimatorStateInfo(0);
                        if (Round.IsEnded)
                        {
                            Log.Info("IsEnded");
                            yield break;
                        }
                    }
                    Log.Info("normalizedTime");

                    while (stateInfo.normalizedTime < 0.98f || !stateInfo.IsName("donate"))
                    {
                        yield return Timing.WaitForSeconds(0.1f);
                        stateInfo = an.GetCurrentAnimatorStateInfo(0);
                        if (Round.IsEnded)
                            yield break;
                    }
                    Log.Info("Kill");
                    yield return Timing.WaitForSeconds(0.5f);
                    foreach (var player in Player.List)
                    {
                        player.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 6, 10f);
                    }
                    Scp5k_Control.GocNuke = true;
                    Warhead.Shake();
                    foreach (var player in Player.List)
                    {
                        player.Kill("Goc奇术");
                    }
                    an.SetBool("donate", false);
                    donating = false;
                    
                    
                    yield break;
                }

                yield return Timing.WaitForSeconds(0.3f);
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
            Scp5k.Scp5k_Control.GOCBOmb.GetComponent<SchematicObject>().Destroy();


            Scp5k.Scp5k_Control.GOCBOmb = null;

        }
    }
}
