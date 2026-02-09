using CommandSystem;
using Exiled.API.Features;
using GameCore;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static InventorySystem.Items.Firearms.ShotEvents.ShotEventManager;

namespace Next_generationSite_27.UnionP.Buffs
{
    public abstract class BuffBase : BaseClass
    {
        public enum BuffType
        {
            Positive,
            Negative,
            Mixed
        }
        public abstract BuffType Type { get; }
        public abstract string BuffName { get; }
        public static List<BuffBase> RegBuffBases = new();
        public static List<BuffBase> RoundBuffs = new();
        public override void Delete()
        {
            //throw new NotImplementedException();
            RegBuffBases.Remove(this);
        }
        public virtual bool CanEnable() => true; // called on ROundStart
        public bool CheckEnabled() => RoundBuffs.Contains(this);
        public override void Init()
        {
            //throw new NotImplementedException();
            RegBuffBases.Add(this);
        }
    }
    class BufStartInit : BaseClass
    {
        void WaitingForPlayers()
        {
            BuffBase.RoundBuffs.Clear();
            var buffcount = 3;
            var availableBuffs = new List<BuffBase>(BuffBase.RegBuffBases);
            for (int i = 0; i < buffcount; i++)
            {
                if (availableBuffs.Count <= 0)
                    break;
                var index = UnityEngine.Random.Range(0, availableBuffs.Count);
                BuffBase.RoundBuffs.Add(availableBuffs[index]);
                availableBuffs.RemoveAt(index);
            }
        }
        void RoundStarted()
        {
            Timing.CallDelayed(0.5f, () =>
            {
                var EnablableBuffs = new List<BuffBase>(BuffBase.RegBuffBases);
                var count = BuffBase.RoundBuffs.RemoveAll(x => !x.CanEnable());
                EnablableBuffs.RemoveAll(x => !x.CanEnable() || BuffBase.RoundBuffs.Contains(x));
                if (count > 0)
                {
                    var availableBuffs = new List<BuffBase>(EnablableBuffs);
                    for (int i = 0; i < count; i++)
                    {
                        if (availableBuffs.Count <= 0)
                            break;
                        var index = UnityEngine.Random.Range(0, availableBuffs.Count);
                        BuffBase.RoundBuffs.Add(availableBuffs[index]);
                        availableBuffs.RemoveAt(index);
                    }
                }
                foreach (var buff in BuffBase.RoundBuffs)
                {
                    Log.Info($"[Buffs] Enabled Buff: {buff.BuffName} ({buff.Type})");
                }
            });

        }
        public override void Init()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += WaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= WaitingForPlayers;
        }
    }
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class ListBuffs : ICommand
    {
        string ICommand.Command => "ListBufs";

        string[] ICommand.Aliases => new[] { "" };

        string ICommand.Description => "列出所有本回合Buff";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"本回合Buff列表{(Round.IsStarted ? "" : "(可能在回合开始后有变化)")}：\n";
            foreach (var i in BuffBase.RoundBuffs)
            {
                string color = "";
                switch(i.Type)
                {
                    case BuffBase.BuffType.Positive:
                        color = "<color=green>";
                        break;
                    case BuffBase.BuffType.Negative:
                        color = "<color=red>";
                        break;
                    case BuffBase.BuffType.Mixed:
                        color = "<color=yellow>";
                        break;
                }
                response += $"- {color} {i.BuffName} ({i.Type})</color>\n";
            }
            return true;
        }
    }
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class RefBuffs : ICommand
    {
        string ICommand.Command => "RefreshBufs";

        string[] ICommand.Aliases => new[] { "RefBuf" };

        string ICommand.Description => "刷新本回合Buff";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Round.IsStarted)
            {
                response = "Failed 回合已开始";
                return false;
            }
            var p = Plugin.plugin.connect.QueryUser(Player.Get(sender).UserId);
            if(p.point >= 40)
            {
                BuffBase.RoundBuffs.Clear();
                var buffcount = 3;
                var availableBuffs = new List<BuffBase>(BuffBase.RegBuffBases);
                for (int i = 0; i < buffcount; i++)
                {
                    if (availableBuffs.Count <= 0)
                        break;
                    var index = UnityEngine.Random.Range(0, availableBuffs.Count);
                    BuffBase.RoundBuffs.Add(availableBuffs[index]);
                    availableBuffs.RemoveAt(index);
                }
                PlayerManager.AddPoint(Player.Get(sender), -40);
                response = "Done!";
                response += $"本回合Buff列表{(Round.IsStarted ? "" : "(可能在回合开始后有变化)")}：\n";
                foreach (var i in BuffBase.RoundBuffs)
                {
                    string color = "";
                    switch (i.Type)
                    {
                        case BuffBase.BuffType.Positive:
                            color = "<color=green>";
                            break;
                        case BuffBase.BuffType.Negative:
                            color = "<color=red>";
                            break;
                        case BuffBase.BuffType.Mixed:
                            color = "<color=yellow>";
                            break;
                    }
                    response += $"- {color} {i.BuffName} ({i.Type})</color>\n";
                }
                return true;
            }
            else
            {
                response = "Failed 点数不足40点";
                return false;
            }
        }
    }

}
