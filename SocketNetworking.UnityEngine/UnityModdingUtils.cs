using HarmonyLib;

namespace SocketNetworking.UnityEngine
{
    public class UnityModdingUtils
    {
        public static Harmony Harmony = new Harmony("com.btelnyy.socketnetworking.patching.unityengine");

        public static void PatchAll()
        {
            Harmony.PatchAll();
        }
    }
}
