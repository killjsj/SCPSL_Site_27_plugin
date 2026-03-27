using CustomPlayerEffects;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.UI;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using RemoteAdmin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP
{
    class TestEffect : TickingEffectBase, ISpectatorDataPlayerEffect
    {
        public string DisplayName => "TestingEffect";

        public bool CanBeDisplayed => true;

        public override void OnTick()
        {
            var p = Player.Get(Hub);
            if (p != null)
            {
                p.Health += 1;
                if (!p.HasMessage("testEffect"))
                {
                    p.AddMessage("testEffect", (x) =>
                    {
                        return new string[]
                        {
                            $"TestEffect: instance:{this.Intensity} Dur:{this.TimeLeft}"
                        };
                    });
                }
            }
        }

        bool ISpectatorDataPlayerEffect.GetSpectatorText(out string display)
        {
            display = DisplayName;
            return true;
        }
    }
    class EffectHelper
    {
        public static T AddStatusEffectRuntime<T>(PlayerEffectsController manager) where T : StatusEffectBase
        {
            if (manager.AllEffects.Any(x => x is T))
                return manager.AllEffects.First(x => x is T) as T;
            var h = new UnityEngine.GameObject($"EffectsHolder_{typeof(T).Name}");
            Log.Info($"Adding effect holder {typeof(T).Name}");
            h.transform.parent = manager.transform;
            var nT = h.AddComponent<T>();


            var list = new List<StatusEffectBase>();
            list.AddRange(manager.AllEffects);
            list.Add(nT);

            var f1 = typeof(PlayerEffectsController)
                .GetProperty("AllEffects", BindingFlags.Public | BindingFlags.Instance);

            if (f1 != null)
            {
                f1.SetValue(manager, list.ToArray());
                var f2 = typeof(PlayerEffectsController)
                .GetProperty("EffectsLength", BindingFlags.Public | BindingFlags.Instance);
                if (f2 != null)
                {
                    f2.SetValue(manager, list.Count);

                    var effectsByType = typeof(PlayerEffectsController)
                    .GetField("_effectsByType", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(manager) as Dictionary<Type, StatusEffectBase>;
                    if (effectsByType != null)
                    {
                        effectsByType?.Add(typeof(T), nT);

                        var syncList = typeof(PlayerEffectsController)
                            .GetField("_syncEffectsIntensity", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.GetValue(manager) as SyncList<byte>;
                        if (syncList != null)
                        {
                            syncList?.Add(0);
                        }
                        else
                        {
                            Log.Error("Failed to get _syncEffectsIntensity field");
                        }
                    }
                    else
                    {
                        Log.Error("Failed to get _effectsByType field");
                    }
                }
                else
                {
                    Log.Error("Failed to get EffectsLength field");
                }

            }
            else
            {
                Log.Error("Failed to get AllEffects property");
            }
            return nT;
        }
    }
}
