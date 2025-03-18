using System;
using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityAnimator.SetFloat
{
    [HarmonyPatch(typeof(Animator), nameof(Animator.SetFloat), new Type[] { typeof(int), typeof(float), typeof(float), typeof(float) })]
    public class SetIdFloatPatchExtended
    {
        public static void Prefix(Animator __instance, int id, float value, float dampTime, float deltaTime)
        {
            NetworkAnimator rAnimator = UnityNetworkManager.GetNetworkAnimator(__instance.gameObject);
            if (rAnimator != null)
            {
                if (rAnimator.IsOwner)
                {
                    rAnimator.NetworkSetFloat(id, value, dampTime, deltaTime);
                }
            }
        }
    }
}
