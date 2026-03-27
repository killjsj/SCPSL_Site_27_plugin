using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Handlers;
using ProjectMER.Commands.Modifying.Rotation;
using ProjectMER.Commands.Utility;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Schematics;
using RoundRestarting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
using Player = Exiled.API.Features.Player;

namespace AT4
{
    public class Config : IConfig
    {

        public bool Debug { get; set; } = false;

        public bool IsEnabled { get; set; } = true;

        public float Damage { get; set; } = 3000f;



    }

    public class AT4launcher : Plugin<Config>{

        public static AT4launcher Instance { get; private set; }

        public override String Name => "AT4plugin";

        public override string Author => "Site27-Whitedoor";

        public override Version Version => new Version(1,0,0);

        public override void OnEnabled()
        {

            Instance = this;

            CustomItem.RegisterItems();

            Exiled.Events.Handlers.Player.ChangingItem += AT4Item.Instance.OnGlobalChangeingItem;
            Exiled.Events.Handlers.Player.DroppedItem += AT4Item.Instance.OnItemDropped;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {

            Instance = null;

            Exiled.Events.Handlers.Player.ChangingItem -= AT4Item.Instance.OnGlobalChangeingItem;
            Exiled.Events.Handlers.Player.DroppedItem -= AT4Item.Instance.OnItemDropped;

            CustomItem.UnregisterItems();

            base.OnDisabled();
        }


    }

}
