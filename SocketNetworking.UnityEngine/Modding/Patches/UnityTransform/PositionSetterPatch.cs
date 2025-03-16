﻿using HarmonyLib;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.position), MethodType.Setter)]
    public class PositionSetterPatch
    {
        public static bool Prefix(Transform __instance, Vector3 value)
        {
            Components.NetworkTransform netTransform = UnityNetworkManager.GetNetworkTransform(__instance.gameObject);
            if (netTransform != null)
            {
                netTransform.NetworkPosition = value;
                return false;
            }
            return true;
        }
    }
}
