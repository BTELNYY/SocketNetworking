using System;
using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.SetBool
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetBool), new Type[] { typeof(string), typeof(bool) })]
    public class SetStringBoolPatch
    {
        public static void Prefix(Animator __instance, string name, bool value)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator != null)
            {
                if (rAnimator.IsOwner)
                {
                    rAnimator.NetworkSetBool(name, value);
                }
            }
        }
    }
}
