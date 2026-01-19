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
    public class ChangeEffect
    {
        public string DisplayName => "ChangeEffect";

        public bool CanBeDisplayed => true;
        public RoleTypeId LastType { get; private set; }
        public RoleTypeId TargetType { get; private set; } = RoleTypeId.None;

        private RoleTypeId LastTargetType { get; set; } = RoleTypeId.None;

        private Player _player;
        public Player _LaBplayer => _player;
        public int TimeLeft = 0;
        public static ChangeEffect Enabled(ReferenceHub Hub,int time)
        {
            var a = new ChangeEffect();

            a.TimeLeft = time;
            a._player = Player.Get(Hub);
            a.LastType = Hub.roleManager.CurrentRole.RoleTypeId; // ✅ 正确记录原始角色

            if (a._player != null && Scp5k.Scp5k_Control.ColorChangerRole.PlayerToRole.TryGetValue(a._player, out var role))
            {
                a._player.ChangeAppearance(role);
                a.TargetType = role;
                a.LastTargetType = role;
            Timing.RunCoroutine(a.OnTick());
            }
            return a;
        }

        public IEnumerator<float> OnTick()
        {
            while (_player != null) {
                try
                {
                    if (!_LaBplayer.HasMessage("ChangeEffect"))
                    {
                        _LaBplayer.AddMessage("ChangeEffect", (x) =>
                        {
                            if (TargetType == RoleTypeId.None)
                            {
                                return new string[]
                                {
                            $""
                                };
                            }
                            return new string[]
                            {
                            $"<pos=40%><voffset=-1em%><color=yellow><size=27>变身剩余时间: {TimeLeft.ToString("F0")} 目前外表:{Scp5k.Scp5k_Control.ColorChangerRole.RoleTrans[TargetType]}</size></color></pos></voffset>"
                            };
                        }, this.TimeLeft, ScreenLocation.Center);
                    }
                    if (_player == null || TargetType == LastTargetType)
                        yield break;
                    if (TargetType == RoleTypeId.None)
                    {
                        _player.ChangeAppearance(LastType); // 恢复原始外观
                        LastTargetType = TargetType = LastType;
                        this.DisableEffect();
                        yield break;
                    }
                    // 只在目标变更时更新
                    _player.ChangeAppearance(TargetType);
                    LastTargetType = TargetType;
                }catch (Exception ex)
                {
                    Log.Warn(ex);
                }
                if(TimeLeft <= 0)
                {
                    this.DisableEffect();
                    yield break;
                }
                TimeLeft -= 1;
                yield return Timing.WaitForSeconds(0.9f);
            }
        }

        public void DisableEffect()
        {
            if (_player == null)
                return;

            // 延迟执行，避免角色切换冲突
            Timing.CallDelayed(0.1f, () =>
            {
                // 再次检查
                if (_player?.ReferenceHub?.roleManager == null)
                    return;

                try
                {
                    _player.ChangeAppearance(LastType);
                }
                catch (Exception e)
                {
                    Log.Debug($"[ChangeEffect] 恢复外观失败: {e}");
                }
            });

            // HUD 清理
            if (_LaBplayer != null)
            {
                if (_LaBplayer.HasMessage("ChangeEffect"))
                    _LaBplayer.RemoveMessage("ChangeEffect");

                _LaBplayer.AddMessage("ChangeEffectEnd", "<pos=40%><voffset=-1em%><color=yellow><size=27>打回原型</size></color></pos></voffset>", 2f, ScreenLocation.Center);
            }
        }

        public void ChangeTarget(RoleTypeId target)
        {
            TargetType = target;
            _player.ChangeAppearance(target); // 恢复原始外观

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
