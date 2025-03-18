using System;
using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.Trigger
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.ResetTrigger), new Type[] { typeof(int) })]
    public class ResetIdTriggerPatch
    {
        public static void Prefix(Animator __instance, int id)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator == null)
            {
                return;
            }
            if (rAnimator.IsOwner)
            {
                rAnimator.NetworkResetTrigger(id);
            }
        }
    }
}
