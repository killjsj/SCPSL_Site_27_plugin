using DrawableLine;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using MEC;
using PlayerRoles.Subroutines;
using ProjectMER.Features.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.heavy.ability
{
    /// <summary>
    /// 幽默无名
    /// </summary>
    public class NoSoundMove : KeyAbility
    {
        public override KeyCode KeyCode => KeyCode.Mouse1;

        public override string Name => "静步";

        public override string Des => "1分钟内 -100%声音 +35%移速";

        public override int id => this.GetType().FullName.GetHashCode();
        public override double Time => 40;
        public override float WaitForDoneTime => 50;
        public override bool OnTrigger()
        {
            player.EnableEffect(EffectType.MovementBoost, 70, WaitForDoneTime);
            player.EnableEffect(EffectType.SilentWalk, 255, WaitForDoneTime);
            return true;
        }
        internal NoSoundMove(Player player) : base(player)
        {
            TotalCount = 2;
        }
        public NoSoundMove() : base()
        {
            TotalCount = 2;
        }
    }
}
