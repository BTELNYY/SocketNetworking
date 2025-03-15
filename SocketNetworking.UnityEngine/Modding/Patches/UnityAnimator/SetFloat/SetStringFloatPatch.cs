using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using System;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.SetFloat
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetFloat), new Type[] { typeof(string), typeof(float) })]
    public class SetStringFloatPatch
    {
        public static void Prefix(Animator __instance, string name, float value)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator != null)
            {
                if (rAnimator.IsOwner)
                {
                    rAnimator.NetworkSetFloat(name, value);
                }
            }
        }
    }
}
