using System;
using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.Trigger
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetTrigger), new Type[] { typeof(string) })]
    public class SetStringTriggerPatch
    {
        public static void Prefix(Animator __instance, string name)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator == null)
            {
                return;
            }
            if (rAnimator.IsOwner)
            {
                rAnimator.NetworkSetTrigger(name);
            }
        }
    }
}
