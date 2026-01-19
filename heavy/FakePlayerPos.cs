using Exiled.API.Features;
using HarmonyLib;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Next_generationSite_27.UnionP.heavy
{
    public static class FakePlayerPos
    {
        public static Dictionary< ReferenceHub,Vector3> Pos =new Dictionary<ReferenceHub, Vector3>();
        public static void SendFakePlayerPos(Player player, Vector3 pos)
        {
            Pos[player.ReferenceHub] = pos;
            // ((LabApi.Features.Wrappers.Player)player).AddAmmo()
        }
        public static void RemoveSendFakePlayerPos(Player player)
        {
            Pos.Remove(player.ReferenceHub);

        }
    }
    [HarmonyPatch(typeof(FpcServerPositionDistributor), "GetNewSyncData")]
    class Patch_GetNewSyncData
    {
        [HarmonyPatch]
        [HarmonyPrefix]
        static bool Prefix(
            ReferenceHub receiver,
            ReferenceHub target,
            FirstPersonMovementModule fpmm,
            bool isInvisible,
            ref FpcSyncData __result)
        {
            if(FakePlayerPos.Pos.TryGetValue(target,out var pos) && receiver != target)
            {
                FpcSyncData prev = default;
                RelativePosition fakePos = new RelativePosition(pos);
                __result = new FpcSyncData(
                    prev,
                    fpmm.SyncMovementState,
                    fpmm.IsGrounded,
                    fakePos,
                    fpmm.MouseLook
                );
                return false;
            }
            return true;
        }
    }

}
