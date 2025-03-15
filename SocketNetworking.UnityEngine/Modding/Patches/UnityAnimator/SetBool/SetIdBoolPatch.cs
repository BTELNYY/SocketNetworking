using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using System;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.SetBool
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetBool), new Type[] { typeof(int), typeof(bool) })]
    public class SetIdBoolPatch
    {
        public static void Prefix(Animator __instance, int id, bool value)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator != null)
            {
                if (rAnimator.IsOwner)
                {
                    rAnimator.NetworkSetBool(id, value);
                }
            }
        }
    }
}
