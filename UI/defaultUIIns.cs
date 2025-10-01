using AudioManagerAPI.Config;
using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Management;
using AudioManagerAPI.Features.Speakers;
using AudioManagerAPI.Features.Static;
using Exiled.API.Features;
using Google.Protobuf.WellKnownTypes;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using MEC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static HintServiceMeow.Core.Models.HintContent.AutoContent;

namespace Next_generationSite_27.UnionP.UI
{
    static class UiManager
    {
        public static  IHintShowHUD GetHUD(this Player player)
        {
            return HSM_hintServ.GetPlayerHUD(player);
        }
        public static void AddMessage(this Player player,string id, Func<Player, string[]> getter, float duration = 5, ScreenLocation location = ScreenLocation.CenterBottom)
        {
            GetHUD(player).AddMessage(id, getter, duration, location);
        }

        public static bool HasMessage(this Player player, string messageID)
        {
            return GetHUD(player).HasMessage(messageID);

        }

        public static void RemoveMessage(this Player player, string id)
        {
            GetHUD(player).RemoveMessage(id);
        }

        public static void AddMessage(this Player player, string id, string c, float duration = 5, ScreenLocation location = ScreenLocation.CenterBottom)
        {
            GetHUD(player).AddMessage(id, c, duration, location);

        }
    }

}
