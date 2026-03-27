using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs.Player;
using MEC;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.Buffs.personal
{
    public class Scp207NoDamage : PersonalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string BuffName => "可乐无毒";

        public override string Description => "不承受Scp207的伤害";
        public void Hurting(HurtingEventArgs ev)
        {
            if ((ev.DamageHandler.Type == DamageType.Scp207 || ev.DamageHandler.Type == DamageType.Poison) && this.CheckEnabled(ev.Player))
            {
                ev.Player.DisableEffect(EffectType.Poisoned);
                               ev.IsAllowed = false;
            }
        }
        public override void Init()
        {
            Exiled.Events.Handlers.Player.Hurting += Hurting;
            base.Init();

        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Player.Hurting -= Hurting;
            base.Delete();
        }
    }
    public class ScpHeal : PersonalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string BuffName => "呼吸回血";
        public static ScpHeal ins;
        public override string Description => "站立回血（无论scp还是人类）";
        public static Dictionary<Player, (Stopwatch stand, double lastTime, Vector3 lastPos)> ScpStandHP = new Dictionary<Player, (Stopwatch stand, double lastTime, Vector3 lastPos)>();
        private void HandleScpStandHeal(Player player, IFpcRole fpcRole)
        {

            double interval = 1.0; // 每秒回血检查一次

            if (!ScpStandHP.TryGetValue(player, out var data))
                ScpStandHP[player] = (Stopwatch.StartNew(), 0.0, player.Position);

            var (stopwatch, lastHealTime, lastPos) = ScpStandHP[player];
            double elapsed = stopwatch.Elapsed.TotalSeconds;

            // 判断是否移动（允许0.05米以内的浮动）
            if (Vector3.Distance(player.Position, lastPos) < 0.5f)
            {
                // 站够指定时间开始回血
                if (elapsed >= Plugin.Instance.Config.ScpStandAddHPTime)
                {
                    if (elapsed - lastHealTime >= interval)
                    {
                        player.Heal(player.Role.Type.IsScp() ? Plugin.Instance.Config.ScpStandAddHPCount * 2 : Plugin.Instance.Config.ScpStandAddHPCount * 2);
                        ScpStandHP[player] = (stopwatch, elapsed, player.Position);

                    }
                }
            }
            else
            {
                // 移动了，重置计时
                stopwatch.Restart();
                ScpStandHP[player] = (stopwatch, 0.0, player.Position);
            }
        }
        public IEnumerator<float> RefreshAllPlayers()
        {
            while (true)
            {
                foreach (var player in Player.Enumerable.Where(x=>this.CheckEnabled(x)))
                {
                    if (player == null)
                        continue;

                    try
                    {
                        if (player.Role?.Base is IFpcRole fpcRole)
                            HandleScpStandHeal(player, fpcRole);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[scpheal] {player?.Nickname ?? "Unknown"}: {e.GetType().Name} - {e.Message}");
                    }
                }

                yield return Timing.WaitForSeconds(0.25f);
            }
        }
        public CoroutineHandle rec;

        public override void Init()
        {
            rec = Timing.RunCoroutine(RefreshAllPlayers(), segment: Segment.FixedUpdate);
            ins = this;
            base.Init();

        }
        public override void Delete()
        {

            base.Delete();
        }
    }

}
