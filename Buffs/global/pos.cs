
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using GameCore;
using MEC;
using Next_generationSite_27.UnionP.heavy;
using Next_generationSite_27.UnionP.heavy.role;
using Next_generationSite_27.UnionP.Turret;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.Buffs
{
    public class FireSupport : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "火力压制";
        public override string Description => "刷新时九尾获得狗官枪 混沌随机获得榴弹炮";

        public override void Init()
        {
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;

            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RespawnedTeam += RespawnedTeam;

            base.Delete();
        }
        public void RespawnedTeam(RespawnedTeamEventArgs ev)
        {
            try
            {
                if (CheckEnabled())
                {
                    if (ev.Wave.TargetFaction == Faction.FoundationStaff)
                    {
                        foreach (var item in ev.Players)
                        {
                            if (item.Role.Type != RoleTypeId.NtfCaptain)
                            {
                                item.AddItem(ItemType.GunFRMG0);
                            }
                        }
                    }
                    else
                    {
                        
                                BombGun.bomb_gun.ins.Give(ev.Players.GetRandomValue());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
    public class DDTime : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "D国崛起";

        public override string Description => "开局给一个DD刷手枪";
        public override void Init()
        {
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;

            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;

            base.Delete();
        }
        public void RoundStarted()
        {
            Timing.CallDelayed(1.4f, () => { 
            if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
            {
                var p = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.ClassD).GetRandomValue();
                if (p != null)
                {
                    p.AddItem(ItemType.GunCOM18);
                }

            } });
        }
    }
    public class MianDuiWoBa : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "胖宝宝盾牌";
        public override string Description => "开局给一个保安上2粉可乐";

        public override void Init()
        {
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;

            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;

            base.Delete();
        }
        public void RoundStarted()
        {
            Timing.CallDelayed(1.4f, () => {
                if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {
                    var p = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.FacilityGuard).GetRandomValue();
                    if (p != null)
                    {
                        p.EnableEffect(EffectType.AntiScp207, 2, 0f);
                    }

                }
            });
        }
    }
    [CustomItem(ItemType.Coin)]
    public class MagicCoin : CustomItemPlus
    {
        public override uint Id { get; set; } = 2839;
        public override string Name { get; set; } = "神奇硬币";
        public override string Description { get; set; } = "一次性硬币,可以将半径3米范围内的自己和其他人传送到随机位置";
        public override float Weight { get; set; } = 1;
        public override SpawnProperties SpawnProperties { get; set; } = null;
        public static MagicCoin ins;
        public override void Init()
        {
            ins = this;
            base.Init();
        }
        protected override void OnUsed(Player player, Item item)
        {
            if (Check(item))
            {
                var tp = Player.Enumerable.Where(x => Vector3.Distance(x.Position, player.Position) <= 3f).ToList();
                var t = Room.Random();
                foreach (var p in tp)
                {
                    p.Position = t.Position + Vector3.up;
                }
                item.Destroy();
            }
            base.OnUsed(player, item);
        }
    }
    public class PaoLuLeXiongDiPaoLuLe : GlobalBuffBase
    {
        public override BuffType Type => BuffType.Positive;

        public override string Description => "一个d有一个一次性的硬币可以将半径3米范围内的自己和其他人传送到随机位置";
        public override string BuffName => "D国消失";
        public override void Init()
        {
            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;

            base.Init();
        }
        public override void Delete()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;

            base.Delete();
        }
        public void RoundStarted()
        {
            Timing.CallDelayed(2f, () => {
                if (this.CheckEnabled() && AutoEvent.AutoEvent.EventManager.CurrentEvent == null)
                {
                    var p = Player.Enumerable.Where(x => x.Role.Type == RoleTypeId.ClassD).ToList().RandomItem();
                    if (p != null)
                    {
                        //p.EnableEffect(EffectType.AntiScp207, 2, 0f);
                        MagicCoin.ins.Give(p);
                    }

                }
            });
        }
    }
    public class AT4 : GlobalBuffBase
    {

        public override BuffType Type => BuffType.Positive;
        public override string BuffName => "重火力预算";

        public override string Description => "生成火箭筒和JS-L1";

        public override void Init()
        {

            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;

            base.Init();
        }
        public override void Delete()
        {

            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;

            base.Delete();
        }
        public void OnRoundStarted()
        {
            Timing.CallDelayed(1.5f, () =>
            {
                try
                {

                    if (!CheckEnabled()) return;

                    Room HID = Room.List.FirstOrDefault(r => r.Type == RoomType.HczHid);

                    if (HID == null)
                    {

                        Log.Info("Can't find HID");

                        return;

                    }

                    Vector3 SPpos = HID.transform.position + new Vector3(0, 5, 0);
                    AT4Item.Instance.Spawn(SPpos);

                    Room W = Room.List.FirstOrDefault(r => r.Type == RoomType.HczArmory);

                    if (W == null)
                    {

                        Log.Info("Can't find HczArmory");

                        return;

                    }

                    SPpos = W.transform.position + new Vector3(0, 5, 0);
                    JS_L1.MagicGun1_JS_L1.ins.Spawn(SPpos);

                }
                catch (Exception ex)
                {
                    Log.Error($"Error in OnRoundStarted: {ex}");
                }

            });
        }
    }
}
