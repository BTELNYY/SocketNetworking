using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SocketNetworking.Modding.Patching.Fields
{
    public static class FieldWatcher
    {
        public static event EventHandler<FieldChangeEventArgs> FieldChanged;

        public static Harmony Harmony => HarmonyHolder.Harmony;

        private static List<Type> _watched = new List<Type>();

        public static void RemoveInjectedIL<T>(BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            RemoveInjectedIL(typeof(T), flags);
        }

        public static void RemoveInjectedIL(Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (_watched.Contains(type))
            {
                return;
            }
            else
            {
                _watched.Add(type);
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                Harmony.Unpatch(method, HarmonyPatchType.Transpiler, "com.btelnyy.socketnetowking.patching");
            }
        }

        public static void InjectIL(Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (!_watched.Contains(type))
            {
                return;
            }
            else
            {
                _watched.Add(type);
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                try
                {
                    Harmony.Patch(method, transpiler: new HarmonyMethod(typeof(FieldWatcher).GetMethod(nameof(InterceptFieldWrites), BindingFlags.Static | BindingFlags.NonPublic)));
                }
                catch { }
            }
        }

        public static void InjectIL<T>(BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            Type type = typeof(T);
            InjectIL(type, flags);
        }

        private static IEnumerable<CodeInstruction> InterceptFieldWrites(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld)
                {
                    FieldInfo fieldInfo = instruction.operand as FieldInfo;
                    if (fieldInfo != null)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldtoken, fieldInfo);
                        yield return new CodeInstruction(OpCodes.Call, typeof(FieldWatcher).GetMethod(nameof(ReportFieldChange), BindingFlags.Static | BindingFlags.NonPublic));
                    }
                }
            }
        }

        private static void ReportFieldChange(object target, RuntimeFieldHandle fieldHandle)
        {
            FieldInfo fieldInfo = FieldInfo.GetFieldFromHandle(fieldHandle);
            Console.WriteLine($"Target: {target.GetType().FullName}, Field: {fieldInfo.Name}, Value: {fieldInfo.GetValue(target)}");
            FieldChangeEventArgs args = new FieldChangeEventArgs(target, fieldInfo, fieldInfo.GetValue(target));
            FieldChanged?.Invoke(target, args);
        }
    }
}
