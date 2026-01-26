using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using Exiled.API.Enums;
using Exiled.API.Features;
using PlayerRoles;

namespace GwangjuRunningManLoader
{
    public class RunningManConfig : EventConfig
    {
        [Description("How many lives each prisoner gets.")]
        public int PrisonerLives { get; set; } = 3;

        [Description("How many players will spawn as the jailors.")]
        public RoleCount JailorRoleCount { get; set; } = new RoleCount(1, 5, 25f);

        [Description("A list of loadouts for the jailors.")]
        public List<Loadout> JailorLoadouts { get; set; } = new List<Loadout>()
    {
        new Loadout()
        {
            Roles = new Dictionary<RoleTypeId, int>() { { RoleTypeId.NtfCaptain, 100 } },
            Items = new List<ItemType>()
            {
                ItemType.Jailbird,
                ItemType.ArmorLight,ItemType.Lantern
            },
            Effects = new List<EffectData>() { new EffectData() { Type="FogControl", Intensity = 0 }, new EffectData(){Type="Scp207",Intensity = 0} },

                InfiniteAmmo = AmmoMode.InfiniteAmmo

        },
    };

        [Description("A list of loadouts for the prisoners.")]
        public List<Loadout> PrisonerLoadouts { get; set; } = new List<Loadout>()
        {
            new Loadout()
            {
                Roles = new Dictionary<RoleTypeId, int>()
                {
                    { RoleTypeId.ClassD, 100 }
                },
                Items = new List<ItemType>(){
                    ItemType.ArmorCombat,ItemType.Lantern
                },
                            Effects = new List<EffectData>() { new EffectData() { Type="FogControl", Intensity = 0 } },

                InfiniteAmmo = AmmoMode.InfiniteAmmo
            }
        };

        public List<Loadout> MedicalLoadouts { get; set; } = new List<Loadout>()
    {
        new Loadout()
        {
            Health = 100
        }
    };

        public List<Loadout> AdrenalineLoadouts { get; set; } = new List<Loadout>()
    {
        new Loadout()
        {
            ArtificialHealth = new ArtificialHealth()
            {
                InitialAmount = 100f,
                MaxAmount = 100f,
                RegenerationAmount = 0,
                AbsorptionPercent = 70,
                Permanent = false,
                Duration = 0
            }
        }
    };
    }
}