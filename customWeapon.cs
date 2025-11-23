using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using Next_generationSite_27.UnionP.heavy.role;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP
{
    [CustomItem(ItemType.Jailbird)]
    class WhipS : CustomItemPlus
    {
        public static uint WhipId = 411;
        public override uint Id { get => WhipId; set { } }
        public override string Name { get => "鞭子"; set { } }
        public override string Description { get => "Niggers"; set { } }
        public override float Weight { get => 3f; set { } }

        public override SpawnProperties SpawnProperties { get => new SpawnProperties(); set { } }
        public override void Init()
        {
            Type = ItemType.Jailbird;
            DefaultAudioManager.RegisterAudio("swing", () =>
    File.OpenRead($"{Paths.Configs}\\Plugins\\union_plugin\\swing.wav"));
            base.Init();
        }
        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Item.ChargingJailbird += OnChargingJailbird;
            Exiled.Events.Handlers.Item.Swinging += OnSwinging;
            Exiled.Events.Handlers.Player.Hurting += OnHurt;
            base.SubscribeEvents();
        }
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            Exiled.Events.Handlers.Item.ChargingJailbird -= OnChargingJailbird;
            Exiled.Events.Handlers.Item.Swinging -= OnSwinging;
            Exiled.Events.Handlers.Player.Hurting += OnHurt;

        }
        protected void OnChargingJailbird(ChargingJailbirdEventArgs ev)
        {
            if (ev.Player.CurrentItem != null && Check(ev.Jailbird))
            {
                ev.IsAllowed = false;
            }
        }
        public void OnSwinging(SwingingEventArgs ev)
        {
            if (ev.Player.CurrentItem != null && Check(ev.Item))
            {
                DefaultAudioManager.Instance.PlayAudio("swing", ev.Player.Position, false, volume: 2f,
                    minDistance: 0.1f,
                    maxDistance: 15f,
                    priority: AudioPriority.High,
                    configureSpeaker: null,
                    queue: false,
                    isSpatial: false,
                    persistent: true,
                    lifespan: 0f,
                    autoCleanup: false);
            }
        }
        public void OnHurt(HurtingEventArgs ev)
        {
            if(ev.DamageHandler.Base is JailbirdDamageHandler)
            {
                if (ev.Player.CurrentItem != null && Check(ev.Player.CurrentItem))
                {
                    ev.Amount = 2;
                }
            }
            
        }
        //public override
    }
}
