using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using System;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.SetFloat
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetFloat), new Type[] { typeof(string), typeof(float), typeof(float), typeof(float) })]
    public class SetStringFloatPatchExtended
    {
        public static void Prefix(Animator __instance, string name, float value, float dampTime, float deltaTime)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator != null)
            {
                if (rAnimator.IsOwner)
                {
                    rAnimator.NetworkSetFloat(name, value, dampTime, deltaTime);
                }
            }
        }
    }
}
