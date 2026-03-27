using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Handlers;
using Next_generationSite_27.UnionP;
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

namespace Next_generationSite_27.UnionP.heavy
{

    public class AT4Starter : BaseClass{

        public float Damage { get; set; } = 3000f;
        public static AT4Starter Instance { get; private set; }

        public override void Init()
        {

            Instance = this;

            //CustomItem.RegisterItems();

            Exiled.Events.Handlers.Player.ChangingItem += AT4Item.Instance.OnGlobalChangeingItem;
            Exiled.Events.Handlers.Player.DroppedItem += AT4Item.Instance.OnItemDropped;

            //base.Init();
        }

        public override void Delete()
        {

            Instance = null;

            Exiled.Events.Handlers.Player.ChangingItem -= AT4Item.Instance.OnGlobalChangeingItem;
            Exiled.Events.Handlers.Player.DroppedItem -= AT4Item.Instance.OnItemDropped;

            //CustomItem.UnregisterItems();

            //base.Delete();
        }


    }

}
