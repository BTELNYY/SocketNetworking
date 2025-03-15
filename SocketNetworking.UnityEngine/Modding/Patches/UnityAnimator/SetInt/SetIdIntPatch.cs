using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using System;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.SetInt
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetInteger), new Type[] { typeof(int), typeof(int) })]
    public class SetIdIntPatch
    {
        public static void Prefix(Animator __instance, int id, int value)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator != null)
            {
                if (rAnimator.IsOwner)
                {
                    rAnimator.NetworkSetInteger(id, value);
                }
            }
        }
    }
}
