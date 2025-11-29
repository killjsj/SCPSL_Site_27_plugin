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
    public abstract class ItemCoolDownAbility : ItemAbilityBase, ICounted, ITiming
    {
        public virtual double Time { get; } = 30;              // 主冷却时间
        public virtual float WaitForDoneTime { get; } = 0;     // 动作执行时间（短暂冷却）
        public virtual int TotalCount { get; set; } = 1;       // 最大次数
        public int count { get; set; } = 1;           // 当前剩余次数

        public AbilityCooldown cooldown = new AbilityCooldown();     // 主冷却
        public AbilityCooldown DoneCooldown = new AbilityCooldown(); // 执行动作间隔冷却
        public virtual float CoolDownRemaining { get => cooldown.Remaining; set => cooldown.Remaining = value; }
        public virtual float DoneRemaining { get => DoneCooldown.Remaining; set => DoneCooldown.Remaining = value; }
        public virtual bool Done { get => DoneCooldown.IsReady; }

        internal ItemCoolDownAbility(ushort serial) : base(serial)
        {
        }

        public ItemCoolDownAbility() { }

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
            if (cooldown.IsReady) cooldown.Trigger(WaitForDoneTime);
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
                    if (count < TotalCount) cooldown.Trigger(Time);
                }

                yield return Timing.WaitForSeconds(0.3f);
            }
        }

        public abstract bool OnTrigger();


        public virtual void InternalRegistier()
        {
            Plugin.RunCoroutine(cooldownReset());
        }
    }

    public abstract class ItemKeyAbility : ItemCoolDownAbility, IRegisiterNeeded<ItemAbilityBase>
    {
        //public abstract void OnTrigger(Player player);
        public SettingBase setting = null;
        public Player player;
        ItemAbilityBase IRegisiterNeeded<ItemAbilityBase>.Register(Player player)
        {
            var a = (ItemKeyAbility)Activator.CreateInstance(this.GetType(), player);
            a.InternalRegister(player);
            return a;
        }
        public abstract KeyCode KeyCode { get; }
        //public string Des;
        public ItemKeyAbility() : base()
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
        public ItemKeyAbility(ushort serial) : base(serial)
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
        public static Dictionary<Player, List<ItemKeyAbility>> activeAbilities = new();
        public void InternalRegister(Player player)
        {
            Plugin.Register(player, setting);
            if (!activeAbilities.ContainsKey(player))
            {
                activeAbilities.Add(player, new List<ItemKeyAbility>() { this });
            }
            else
            {
                activeAbilities[player].Add(this);
            }
            base.InternalRegistier();
        }
        public virtual void Unregister(Player player)
        {

        }
    }
}
