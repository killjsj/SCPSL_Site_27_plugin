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
   
    public class TestAbility1 : KeyAbility
    {
        //public override KeyCode KeyCode => KeyCode.Mouse2;

        public override string Name => "范围扫描";

        public override string Des => "扫描周围35m内的敌人";

        public override int id => 188;
        public override double Time => 1;
        public override int TotalCount { get; set; } = 6;
        public override float WaitForDoneTime => 0;
        public static AbilityCooldown cd = new();

        public override KeyCode KeyCode => KeyCode.Mouse0;

        public override bool OnTrigger()
        {
            var c = player.CameraTransform.GetComponent<UnityEngine.Camera>();

            if (c == null)
            {
                c = player.CameraTransform.gameObject
                    .AddComponent<UnityEngine.Camera>();
                c.fieldOfView = 90;
                
            }
            foreach (var a in Player.Enumerable)
            {
                var v = c.WorldToScreenPoint(a.Position);
                //v.x = v.x * 1300;
                //v.y = v.y * 700;
                Log.Info(v);
                if (player.GetHUD() is HSM_hintServ hSM_HintServ)
                {
                    var h = hSM_HintServ.hud;
                    var hint = new HintServiceMeow.Core.Models.Hints.Hint()
                    {
                        Text = $"<pos={v.x}px>{a.Role.Name} ({Vector3.Distance(a.Position, player.Position)}) ScreenPos:{v}</pos>",
                        YCoordinate = v.y,
                        XCoordinate = v.x,
                        YCoordinateAlign = HintServiceMeow.Core.Enum.HintVerticalAlign.Top,
                        
                    };
                    h.AddHint(hint);
                    Timing.CallDelayed(1f, () =>
                    {
                        h.RemoveHint(hint);
                    });
                }
            }
            Log.Info("DOne!");
            return true;
        }

        internal TestAbility1(Player player) : base(player)
        {
            //TotalCount = 1;
        }
        public TestAbility1() : base()
        {
            //TotalCount = 1;
        }
    }
}
