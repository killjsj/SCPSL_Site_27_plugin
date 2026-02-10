using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PlayerArms;

namespace Next_generationSite_27.UnionP
{
    class RaPlayerQueryReplacer : BaseClass
    {
        public override void Delete()
        {
            LabApi.Events.Handlers.PlayerEvents.RequestedRaPlayerInfo -= PlayerEvents_RequestedRaPlayerInfo;
        }
        public static MySQLConnect sql => Plugin.plugin.connect;

        private void PlayerEvents_RequestedRaPlayerInfo(LabApi.Events.Arguments.PlayerEvents.PlayerRequestedRaPlayerInfoEventArgs ev)
        {
            var Pbans = sql.QueryAllBan(ev.Target.UserId);

            if (Pbans != null)
            {
                if (Pbans.Count > 0)
                {
                    ev.InfoBuilder.AppendLine($"");
                    ev.InfoBuilder.AppendLine($"以下是封禁记录:");
                    foreach (var arg in Pbans)
                    {
                        ev.InfoBuilder.AppendLine($"{arg.start_time} 到 {arg.end_time} by:{arg.issuer_name} reason:{arg.reason}");
                    }
                }else
                {
                    ev.InfoBuilder.AppendLine($"无封禁记录");

                }
            }
        }

        public override void Init()
        {
            LabApi.Events.Handlers.PlayerEvents.RequestedRaPlayerInfo += PlayerEvents_RequestedRaPlayerInfo;
        }
    }
}
