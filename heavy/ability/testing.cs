using CustomPlayerEffects;
using CustomRendering;
using DrawableLine;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Roles;
using MEC;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles.Subroutines;
using ProjectMER.Features.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;

namespace Next_generationSite_27.UnionP.heavy.ability
{
   
    public class TestAbility1 : ItemKeyAbility
    {
        //public override KeyCode KeyCode => KeyCode.Mouse2;

        public override string Name => "范围扫描";

        public override string Des => "扫描周围35m内的敌人";

        public override int id => 188;
        public override double Time => 30;
        public override int TotalCount { get; set; } = 6;
        public override float WaitForDoneTime => 0;
        public static AbilityCooldown cd = new();

        public override KeyCode KeyCode => KeyCode.Mouse0;

        public override bool OnTrigger()
        {
            foreach (var p in Player.Enumerable)
            {
                if (player != p && Vector3.Distance(player.Position, p.Position) <= 35f)
                {
                    if (HitboxIdentity.IsEnemy(player.ReferenceHub, p.ReferenceHub))
                    {
                        new DrawableLineMessage(0.5f, Color.red * new Color(1, 1, 1, 1 - (Vector3.Distance(player.Position, p.Position) / 150) + 0.01f), new Vector3[2] { p.CameraTransform.position + 0.2f * Vector3.down, player.Position }).SendToHubsConditionally(x => x == player.ReferenceHub);
                    }
                }
            }
            return true;
        }
        public TestAbility1() : base()
        {
            //TotalCount = 1;
        }
    }
}
