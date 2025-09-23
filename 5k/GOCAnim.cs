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
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        //public static AnimationClip donate;
        //public static AnimationClip start;
        public static void Load()
        {
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
            }
            jump = false;
            Scp5k.Scp5k_Control.GOCBOmb = skg;
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
            endC = Timing.RunCoroutine(quit(_animator));
        }
        [Server]

        public static void StopEnd()
        {
            _animator.SetBool("end", false);
            if (endC != null && endC.IsRunning) { 
                Timing.KillCoroutines(endC);
            }
        }

        [Server]
        public static void PlayDonate()
        {
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
            while (true)
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

                if (killer.transform.localScale.y > 8)
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

                    AnimatorStateInfo stateInfo = an.GetCurrentAnimatorStateInfo(0);
                    Log.Info("IsName");

                    // 等待动画切换到目标动画
                    while (!stateInfo.IsName("donate"))
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
                stateInfo = an.GetCurrentAnimatorStateInfo(0);
                        yield return Timing.WaitForOneFrame;
                        if (Round.IsEnded)
                            yield break;
                    }
                    Log.Info("Kill");
                    foreach (var player in Player.List)
                    {
                        player.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 6, 10f);
                    }
                    yield return Timing.WaitForSeconds(0.5f);
                    Scp5k_Control.GocNuke = true;
                    Warhead.Detonate();
                    foreach (var player in Player.List)
                    {
                        player.Kill("Goc奇术");
                    }
                    an.SetBool("donate", false);
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
