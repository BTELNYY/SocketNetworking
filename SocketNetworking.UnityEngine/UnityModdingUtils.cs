using System.Reflection;
using HarmonyLib;
using SocketNetworking.Shared;

namespace SocketNetworking.UnityEngine
{
    public class UnityModdingUtils
    {
        public static Harmony Harmony = new Harmony("com.btelnyy.socketnetworking.patching.unityengine");

        public static void Init()
        {
            //NetworkManager.ImportAssembly(Assembly.GetExecutingAssembly());
            Harmony.PatchAll();
        }
    }
}
