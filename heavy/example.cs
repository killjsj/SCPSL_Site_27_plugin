using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using MapGeneration;
using MEC;
using Mirror;
using Next_generationSite_27.UnionP.Scp5k;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using Respawning;
using Respawning.Waves;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.heavy.Scannner;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;
//using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Example : BaseClass
    {
      
        public static List<Player> diedPlayer
        {
            get
            {
                var t = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.Spectator).ToList();
                t.ShuffleList();
                return t;
            }
        }
        public static PConfig config => Plugin.Instance.Config;

        public override void Init()
        {
            //throw new NotImplementedException();
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStart;
            //throw new NotImplementedException();
        }
        public static void OnRoundStart()
        {
        }
       
    }
}
