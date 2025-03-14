using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SocketNetworking.Tests.LocalTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FieldWatcher.InjectILDeep<Test>();
            Test test = new Test();
            test.TestField();
            OrderedLinkedList<int> list = new OrderedLinkedList<int>(new int[] { 3, -1, 93, 9999, -38384, 34, 88 });
            Console.WriteLine(list.ToString());
        }
    }

    public class Test
    {
        static int wow = 0;

        int field1 = 0;

        float field2 = 0f;

        public void TestField()
        {
            wow += 1;
            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                field1 = random.Next();
                field2 = random.Next();
            }
        }
    }

    public static class FieldWatcher
    {
        static FieldWatcher()
        {
            Harmony.DEBUG = true;
            Harmony = new Harmony("com.btelnyy.socketnetowking.patching");
        }

        public static Harmony Harmony { get; }

        public static void RemoveDeepInjectedIL<T>(BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        {
            Type type = typeof(T);
            foreach (MethodInfo method in type.GetMethodsDeep(flags))
            {
                Harmony.Unpatch(method, HarmonyPatchType.Transpiler, "com.btelnyy.socketnetowking.patching");
            }
        }

        public static void InjectILDeep<T>(BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        {
            Type type = typeof(T);

            //Use deep method to patch parents as well.
            foreach (MethodInfo method in type.GetMethodsDeep(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    Harmony.Patch(method, transpiler: new HarmonyMethod(typeof(FieldWatcher).GetMethod(nameof(InterceptFieldWrites), BindingFlags.Static | BindingFlags.NonPublic)));
                }
                catch { }
            }
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

        }
    }
}
