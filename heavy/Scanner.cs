using DrawableLine;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;

namespace Next_generationSite_27.UnionP.heavy
{
    public class Scannner : BaseClass
    {
        public override void Delete()
        {
            //throw new NotImplementedException();
        }

        public override void Init()
        {
            //throw new NotImplementedException();
        }
        public static uint ScannerItemID = 5160;
        [CustomItem(ItemType.ArmorHeavy)]
        public class scanner : CustomArmor
        {
            public static scanner ins;
            public override uint Id { get; set; } = ScannerItemID;
            public override string Name { get; set; } = "扫描护具";
            public override string Description { get; set; } = "每两秒扫描150米内的所有人";
            public override float Weight { get; set; } = 10;
            public override SpawnProperties SpawnProperties { get; set; } = null;
            //public override Vector3 Scale { get; set; } = new Vector3(2f, 2f, 2f);
            //override
            protected override void ShowPickedUpMessage(Player player)
            {
                //player.Broadcast(4, "", global::Broadcast.BroadcastFlags.Normal, true);
                var p = player;

                //p.AddMessage("AEH_GET_HINT" + DateTime.Now.ToString(), "<size=28><color=red>你获得了绝对排斥护具,请查看Server-Specific修改按键</color></size>", 4f, ScreenLocation.Center);

                base.ShowPickedUpMessage(player);
            }
            public static IEnumerator<float> Scanner(Player p)
            {
                while (p.IsAlive)
                {
                    foreach (var player in Player.Enumerable)
                    {
                        if (player != p && Vector3.Distance(player.Position, p.Position) <= 150f)
                        {
                            if (HitboxIdentity.IsEnemy(player.ReferenceHub, p.ReferenceHub))
                            {
                                new DrawableLineMessage(0.6f, Color.red, new Vector3[2] { p.CameraTransform.position + 0.2f * Vector3.down, player.Position }).SendToHubsConditionally(x => x == p.ReferenceHub);
                            }
                            else
                            {
                                new DrawableLineMessage(0.6f, Color.green, new Vector3[2] { p.CameraTransform.position + 0.2f * Vector3.down, player.Position }).SendToHubsConditionally(x => x == p.ReferenceHub);
                            }
                        }
                    }
                    yield return Timing.WaitForSeconds(2f);
                }
            }
            protected override void OnPickingUp(PickingUpItemEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    if (!CHs.ContainsKey(ev.Player))
                    {
                        var handle = Plugin.RunCoroutine(Scanner(ev.Player));
                        CHs[ev.Player] = handle;
                    }
                }
                base.OnPickingUp(ev);
            }
            protected override void OnDroppingItem(DroppingItemEventArgs ev)
            {
                if (Check(ev.Item))
                {
                    //Plugin.Unregister(ev.Player, Plugin.MenuCache.Where(a => a.Id == Plugin.Instance.Config.SettingIds[Features.AEHKey] || a.Id == Plugin.Instance.Config.SettingIds[Features.Scp5kHeader]))
                    if (CHs.TryGetValue(ev.Player, out var handle))
                    {
                        Timing.KillCoroutines(handle);
                        CHs.Remove(ev.Player);
                    }
                }
                base.OnDroppingItem(ev);
            }
            protected override void OnUpgrading(UpgradingEventArgs ev)
            {
                if (Check(ev.Pickup))
                {
                    ev.IsAllowed = false;
                }
                base.OnUpgrading(ev);
            }
            public static Dictionary<Player, CoroutineHandle> CHs = new Dictionary<Player, CoroutineHandle>();
            public override void Init()
            {
                Type = ItemType.ArmorHeavy;
                ins = this;

                base.Init();
            }
        }
    }
}
