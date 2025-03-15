using HarmonyLib;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.position), MethodType.Setter)]
    public class PositionSetterPatch
    {
        public static void Prefix(Transform __instance, Vector3 __value)
        {
            return;
        }
    }
}
