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
                Harmony.Unpatch(method, HarmonyPatchType.Transpiler, "com.btelnyy.socketnetworking.patching");
            }
        }

        public static void InjectIL(Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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


        static FieldInfo lastArray;

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
                if (instruction.opcode == OpCodes.Ldfld)
                {
                    FieldInfo info = instruction.operand as FieldInfo;
                    if (info != null && info.FieldType.IsArray)
                    {
                        lastArray = info;
                    }
                }
                if (instruction.opcode == OpCodes.Stelem || instruction.opcode == OpCodes.Stelem_Ref || instruction.opcode == OpCodes.Stelem_I || instruction.opcode == OpCodes.Stelem_I1 || instruction.opcode == OpCodes.Stelem_I2 || instruction.opcode == OpCodes.Stelem_I4 || instruction.opcode == OpCodes.Stelem_I8 || instruction.opcode == OpCodes.Stelem_R4 || instruction.opcode == OpCodes.Stelem_R8)
                {
                    if (lastArray != null)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldtoken, lastArray);
                        yield return new CodeInstruction(OpCodes.Call, typeof(FieldWatcher).GetMethod(nameof(ReportFieldChange), BindingFlags.Static | BindingFlags.NonPublic));
                    }
                    lastArray = null;
                }
            }
        }

        private static void ReportFieldChange(object target, RuntimeFieldHandle fieldHandle)
        {
            FieldInfo fieldInfo = FieldInfo.GetFieldFromHandle(fieldHandle);
            if (fieldInfo.DeclaringType != target.GetType())
            {
                return;
            }
            //Console.WriteLine($"Target: {target.GetType().FullName}, Field: {fieldInfo.Name}, Value: {fieldInfo.GetValue(target)}");
            FieldChangeEventArgs args = new FieldChangeEventArgs(target, fieldInfo, fieldInfo.GetValue(target));
            FieldChanged?.Invoke(target, args);
        }
    }
}
