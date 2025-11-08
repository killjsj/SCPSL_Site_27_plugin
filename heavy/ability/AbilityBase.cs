using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using MEC;
using PlayerRoles.Subroutines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.heavy.ability
{
    public interface IRegisiterNeeded<T>
    {
        public T Register(Player player);
        public void Unregister(Player player);
    }
    public abstract class AbilityBase
    {
        public abstract string Name { get; }
        public abstract string Des { get; }
        public abstract int id { get; }
        public AbilityBase() { }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is AbilityBase AB)
            {
                return AB.id == id;
            }

            return false;
        }
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }
    public abstract class CoolDownAbility : AbilityBase, IRegisiterNeeded<AbilityBase>
    {
        public virtual double Time { get; } = 30;              // 主冷却时间
        public virtual float WaitForDoneTime { get; } = 0;     // 动作执行时间（短暂冷却）
        public virtual int TotalCount { get; set; } = 1;       // 最大次数
        public int count { get; protected set; } = 1;           // 当前剩余次数

        public AbilityCooldown cooldown = new AbilityCooldown();     // 主冷却
        public AbilityCooldown DoneCooldown = new AbilityCooldown(); // 执行动作间隔冷却

        public virtual Player player { get; set; }

        internal CoolDownAbility(Player layer)
        {
            player = layer;
        }

        public CoolDownAbility() { }

        /// <summary>
        /// 尝试触发技能
        /// </summary>
        public void OnTriggerInternal(Player player)
        {
            // 无次数或冷却中直接跳过
            if (count <= 0 || !DoneCooldown.IsReady)
                return;

            // 执行技能逻辑
            if (!OnTrigger())
                return; // 技能失败（例如条件不满足）

            count--;

            // 启动动作结束等待
            if(cooldown.IsReady)cooldown.Trigger(WaitForDoneTime);
            DoneCooldown.Trigger(WaitForDoneTime);
            Plugin.RunCoroutine(cooldownStart());
        }
        public IEnumerator<float> cooldownStart()
        {
            while (true)
            {
                if (DoneCooldown.IsReady) break;
                yield return Timing.WaitForSeconds(0.2f);
            }
            if (cooldown.IsReady) cooldown.Trigger(Time);


        }
        /// <summary>
        /// 自动恢复次数协程
        /// </summary>
        public IEnumerator<float> cooldownReset()
        {
            while (true)
            {
                // 主冷却结束 → 恢复一次使用次数
                if (cooldown.IsReady && count < TotalCount)
                {
                    count++;
                    // 重新启动下一次恢复冷却
                    if(count < TotalCount) cooldown.Trigger(Time);
                }

                yield return Timing.WaitForSeconds(0.3f);
            }
        }

        public abstract bool OnTrigger();

        public abstract AbilityBase Register(Player player);

        public virtual void InternalRegistier()
        {
            Plugin.RunCoroutine(cooldownReset());
        }

        public virtual void Unregister(Player player)
        {
            // 可在这里停止协程、释放资源
        }
    }

    public abstract class KeyAbility : CoolDownAbility, IRegisiterNeeded<AbilityBase>
    {
        //public abstract void OnTrigger(Player player);
        public SettingBase setting = null;
        public abstract KeyCode KeyCode { get; }
        //public string Des;
        public KeyAbility() : base()
        {
            setting = null;
            if (Plugin.MenuCache.Any(x => x.Id == id))
            {
                setting = Plugin.MenuCache.FirstOrDefault(x => x.Id == id);
            }
            else
            {
                setting = new KeybindSetting(id, Name, KeyCode, true, hintDescription: Des, onChanged: (player, SB) =>
                {
                    if (SB is KeybindSetting kbs)
                    {
                        if (kbs.IsPressed)
                        {
                            if (activeAbilities.TryGetValue(player, out var ability))
                            {
                                if (ability != null)
                                {
                                    var a = ability.First(x => x.id == kbs.Id);
                                    if (a != null)
                                    {
                                        a.OnTriggerInternal(player);
                                    }
                                }
                            }
                        }
                    }
                });
                Plugin.MenuCache.Add(setting);
            }
        }
        public KeyAbility(Player player) : base(player)
        {
            setting = null;
            if (Plugin.MenuCache.Any(x => x.Id == id))
            {
                setting = Plugin.MenuCache.FirstOrDefault(x => x.Id == id);
            }
            else
            {
                setting = new KeybindSetting(id, Name, KeyCode, true, hintDescription: Des, onChanged: (player, SB) =>
                {
                    if (SB is KeybindSetting kbs)
                    {
                        if (kbs.IsPressed)
                        {
                            if (activeAbilities.TryGetValue(player, out var ability))
                            {
                                if (ability != null)
                                {
                                    var a = ability.First(x => x.id == kbs.Id);
                                    if (a != null)
                                    {
                                        a.OnTriggerInternal(player);
                                    }
                                }
                            }
                        }
                    }
                });
                Plugin.MenuCache.Add(setting);
            }
        }
        public static Dictionary<Player, List<KeyAbility>> activeAbilities = new Dictionary<Player, List<KeyAbility>>();

        public override void Unregister(Player player)
        {
            Plugin.Unregister(player, setting);
            if (!activeAbilities.ContainsKey(player))
            {
                //activeAbilities.Remove(player);
            }
            else
            {
                activeAbilities[player].Remove(this);
            }
        }
        public void InternalRegister(Player player)
        {
            Plugin.Register(player, setting);
            if (!activeAbilities.ContainsKey(player))
            {
                activeAbilities.Add(player, new List<KeyAbility>() { this });
            }
            else
            {
                activeAbilities[player].Add(this);
            }
            base.InternalRegistier();
        }
    }
    public abstract class PassAbility : AbilityBase, IRegisiterNeeded<AbilityBase>
    {
        //public static bool Inited = false;
        public static void Init()
        {
            //if (!Inited)
            {
                Plugin.RunCoroutine(ReFresher());
            }
        }
        public Player player;
        public static Dictionary<Player, List<PassAbility>> activeAbilities = new Dictionary<Player, List<PassAbility>>();
        public static IEnumerator<float> ReFresher()
        {
            while (true)
            {
                foreach (var kv in activeAbilities)
                {
                    foreach (var ability in kv.Value)
                    {
                        try
                        {
                            ability.OnCheck(kv.Key);
                        }
                        catch (Exception ex)
                        {
                            Log.Warn(ex);
                        }
                    }
                    yield return Timing.WaitForOneFrame;

                }

                yield return Timing.WaitForSeconds(0.3f);
            }
            //Inited = false;
        }
        public abstract void OnCheck(Player player);
        public void Unregister(Player player)
        {
            if (!activeAbilities.ContainsKey(player))
            {
                //activeAbilities.Remove(player);
            }
            else
            {
                activeAbilities[player].Remove(this);
            }

        }
        public abstract AbilityBase Register(Player player);
        public void InternalRegister(Player player)
        {
            if (!activeAbilities.ContainsKey(player))
            {
                activeAbilities.Add(player, new List<PassAbility>() { this });
            }
            else
            {
                activeAbilities[player].Add(this);
            }
        }
        internal PassAbility(Player layer)
        {
            player = layer;
        }
        public PassAbility()
        {
            
        }
    }
}
