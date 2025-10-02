using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP.UI
{
    interface IHintShowHUD
    {
        bool HasMessage(string messageID);
        void AddMessage(string id, Func<Player, string[]> getter, float duration = 5f, ScreenLocation location = ScreenLocation.CenterBottom);
        void AddMessage(string id, string message, float duration = 5f, ScreenLocation location = ScreenLocation.CenterBottom);
        void RemoveMessage(string id);
        IHintShowHUD GetHUD(Player player);
    }
    public enum ScreenLocation
    {
        Top,
        CenterTop,
        Center,
        CenterBottom,
        Bottom,
        Custom,
        NtfSpawn,
        ChaosSpawn,
        MiddleLeft,
        MiddleRight,
        Middle,
        ReversedForPlayerLVShow,
        Scp914
    }
}
