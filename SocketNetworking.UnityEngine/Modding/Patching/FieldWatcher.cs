using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace SocketNetworking.UnityEngine.Modding.Patching
{
    public static class FieldWatcher
    {
        public static Harmony Harmony { get; } = new Harmony("com.btelnyy.socketnetowking.patching");

        public static event EventHandler<FieldChangeEventArgs> FieldChanged;

        public static void RemoveInjectedIL<T>()
        {
            var type = typeof(T);
            foreach (var method in type.GetMethodsDeep(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Harmony.Unpatch(method, HarmonyPatchType.Transpiler, "com.btelnyy.socketnetowking.patching");
            }
        }

        public static void InjectIL<T>()
        {
            var type = typeof(T);

            //Use deep method to patch parents as well.
            foreach (var method in type.GetMethodsDeep(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Harmony.Patch(method, transpiler: new HarmonyMethod(typeof(FieldWatcher).GetMethod(nameof(InterceptFieldWrites), BindingFlags.Static | BindingFlags.NonPublic)));
            }
        }

        private static IEnumerable<CodeInstruction> InterceptFieldWrites(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stfld)
                {
                    var fieldInfo = instruction.operand as FieldInfo;
                    if (fieldInfo != null)
                    {
                        yield return new CodeInstruction(OpCodes.Dup); //Duplicate value before storing
                        yield return new CodeInstruction(OpCodes.Ldtoken, fieldInfo); //Load field handle
                        yield return new CodeInstruction(OpCodes.Call, typeof(FieldWatcher).GetMethod(nameof(ReportFieldChange), BindingFlags.Static | BindingFlags.NonPublic));
                    }
                }

                //return old instruction to prevent it from being deleted.
                yield return instruction;
            }
        }
        
        private static void ReportFieldChange(object target, RuntimeFieldHandle fieldHandle, object newValue)
        {
            var fieldInfo = FieldInfo.GetFieldFromHandle(fieldHandle);
            FieldChanged?.Invoke(null, new FieldChangeEventArgs(target, fieldInfo, newValue));
        }
    }
}
