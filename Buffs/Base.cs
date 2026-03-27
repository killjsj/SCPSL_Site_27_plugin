using CommandSystem;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using GameCore;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.Hints;
using MEC;
using Next_generationSite_27.UnionP.Buffs.personal;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static InventorySystem.Items.Firearms.ShotEvents.ShotEventManager;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

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
        public abstract string Description { get; }
        public static List<BuffBase> RegBuffBases = new();
        public static List<BuffBase> GlobalBuffs = new();
        public static bool adminOverride = false;
        public override void Delete()
        {
            //throw new NotImplementedException();
            RegBuffBases.Remove(this);
        }
        public  bool CheckEnabledInternal() => Round.InProgress && !adminOverride;
        public override void Init()
        {
            //throw new NotImplementedException();
            RegBuffBases.Add(this);
        }
    }
    public abstract class PersonalBuffBase : BuffBase
    {
        public List<Player> ServeringPLayers = new List<Player>();
        public virtual bool CanEnable(Player player) => true; // called on ChangeRole
        public bool CheckEnabled(Player player)
        {
            if (player == null) return false;
            return base.CheckEnabledInternal() && ServeringPLayers.Contains(player);
        }
    }
    public abstract class GlobalBuffBase : BuffBase
    {
        public virtual bool CanEnable() => true; // called on ROundStart
        public bool CheckEnabled()
        {
            return GlobalBuffs.Contains(this) && base.CheckEnabledInternal();
        }
    }
    class BufStartInit : BaseClass
    {
        public Dictionary<Player, List<PersonalBuffBase>> playersBuffs = new();
        void WaitingForPlayers()
        {
        }
        public Hint BuffHint;
        void RoundStarted()
        {
            //return;
            Timing.CallDelayed(0.95f, () =>
            {
                BuffBase.GlobalBuffs.Clear();
                var buffcount = 5;
                var availableBuffs = new List<BuffBase>(BuffBase.RegBuffBases);
                availableBuffs.RemoveAll(x => x is not GlobalBuffBase || (x is GlobalBuffBase g && !g.CanEnable()));
                for (int i = 0; i < buffcount; i++)
                {
                    if (availableBuffs.Count <= 0)
                        break;
                    var index = UnityEngine.Random.Range(0, availableBuffs.Count);
                    BuffBase.GlobalBuffs.Add(availableBuffs[index]);
                    availableBuffs.RemoveAt(index);
                }
                foreach (var buff in BuffBase.GlobalBuffs)
                {
                    Log.Info($"[Buffs] Enabled Buff: {buff.BuffName} ({buff.Type})");
                }
            });
            //);

        }
        public string HintUpdater(AutoContentUpdateArg ev)
        {
            var s = new StringBuilder();
            if (ev.PlayerDisplay != null && ev.PlayerDisplay.ReferenceHub != null)
            {
                var player = Player.Get(ev.PlayerDisplay.ReferenceHub);
                try
                {
                    List<PersonalBuffBase> personalBuffBases = null;
                    if (Round.IsStarted)
                    {
                        if (player != null && !player.IsDead)
                        {
                            playersBuffs.TryGetValue(player, out personalBuffBases);
                            s.AppendLine($"当前个人增益:");
                        }
                        else
                        {
                            if (player != null)
                            {
                                if (player.Role is SpectatorRole sr && sr.SpectatedPlayer != null)
                                {
                                    playersBuffs.TryGetValue(sr.SpectatedPlayer, out personalBuffBases);

                                    s.AppendLine($"目标个人增益:");
                                }
                            }
                        }
                        if(personalBuffBases == null)
                        {
                            personalBuffBases = new List<PersonalBuffBase>();
                        }
                        foreach (var i in personalBuffBases)
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
                            s.Append($"-{color} {i.BuffName}:{i.Description}({i.Type})</color>\n");
                        }
                        s.AppendLine($"当前全局增益:");
                        foreach (var i in BuffBase.GlobalBuffs)
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
                            s.Append($"-{color} {i.BuffName}:{i.Description}({i.Type})</color>\n");
                        }
                    }
                    else
                    {
                        s.AppendLine($"等待分配...");

                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            return s.ToString();
        }
        public void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player == null) return;
            if (!playersBuffs.TryGetValue(ev.Player, out var players))
            {
                players = new List<PersonalBuffBase>();
            }
            else
            {
                foreach (var item in players)
                {
                    item.ServeringPLayers.Remove(ev.Player);
                }
            }
            players.Clear();
            if (ev.NewRole.IsDead()) return;
            Timing.CallDelayed(0.5f, () =>
            {
                var EnablableBuffs = new List<BuffBase>(BuffBase.RegBuffBases);
                // 先移除不应被分配的项（保留 PersonalBuffBase 的可能项）
                    var availableBuffs = EnablableBuffs.OfType<PersonalBuffBase>().ToList();
                availableBuffs.RemoveAll(x =>  !x.CanEnable(ev.Player));
                var count = EnablableBuffs.Count;
                if (count > 0)
                {
                    // 只挑选 PersonalBuffBase 类型，避免向 players 中添加 null
                    // 限制单个玩家最多分配的个人 Buff 数量
                    int assignCount = Math.Min(availableBuffs.Count, 3);

                    for (int i = 0; i < assignCount && availableBuffs.Count > 0; i++)
                    {
                        var index = UnityEngine.Random.Range(0, availableBuffs.Count);
                        var sel = availableBuffs[index];
                        if (sel != null)
                        {
                            players.Add(sel);
                            availableBuffs.RemoveAt(index);
                        }
                        else
                        {
                            // 保守移除并尝试补足数量
                            availableBuffs.RemoveAt(index);
                            i--;
                        }
                    }
                    if (ev.NewRole.IsScp())
                    {
                        if (!players.Any(x => x is ScpHeal))
                        {
                            players.Add(ScpHeal.ins);
                        }
                    }
                }
                foreach (var item in players)
                {
                    if (item != null)
                    {
                        item.ServeringPLayers.Add(ev.Player);
                    }
                }
                playersBuffs[ev.Player] = players;
            });

        }

        public void Verified(VerifiedEventArgs ev)
        {
            if (ev.Player != null)
            {
                ev.Player.AddHint(BuffHint);
            }
        }
        public override void Init()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += WaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole += ChangingRole;
            Exiled.Events.Handlers.Player.Verified += Verified;
            BuffHint = new Hint()
            {
                XCoordinate = 1,
                YCoordinate = 900,
                Alignment = HintServiceMeow.Core.Enum.HintAlignment.Left,
                AutoText = HintUpdater,
                //SyncSpeed = HintServiceMeow.Core.Enum.HintSyncSpeed.Slow,
                FontSize = 20
            };
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= WaitingForPlayers;
            Exiled.Events.Handlers.Player.ChangingRole -= ChangingRole;
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Exiled.Events.Handlers.Player.Verified -= Verified;
        }
    }
    [CommandSystem.CommandHandler(typeof(ClientCommandHandler))]
    class ListBuffRA : ICommand
    {
        string ICommand.Command => "ListBufs";

        string[] ICommand.Aliases => new[] { "ListBuf", "lb" };

        string ICommand.Description => "列出所有可用Buff";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"Buff列表：\n";
            foreach (var i in BuffBase.RegBuffBases)
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
                response += $"-{color} class:{i.GetType().Name} name:{i.BuffName} desc:{i.Description} ({i.Type})</color>\n";
            }
            return true;
        }
    }
    [CommandSystem.CommandHandler(typeof(RemoteAdminCommandHandler))]
    class DisableBuffRA : ICommand
    {
        string ICommand.Command => "DisableBuf";

        string[] ICommand.Aliases => new[] { "disBuf", "db" };

        string ICommand.Description => "关闭/开启buf";

        bool ICommand.Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
            {
                return false;
            }
            BuffBase.adminOverride = !BuffBase.adminOverride;
            response = BuffBase.adminOverride ? "已关闭" : "已开启";
            return true;
        }
    }
}
