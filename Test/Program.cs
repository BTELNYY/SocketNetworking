using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SocketNetworking;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FieldWatcher.InjectIL<Test>();
            Test test = new Test();
            test.TestField();
        }
    }

    public class Test
    {
        int field1 = 0;

        float field2 = 0f;

        public void TestField()
        {
            field1++;
            field2 += 1f;
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

        public static void RemoveInjectedIL<T>()
        {
            var type = typeof(T);
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                Harmony.Unpatch(method, HarmonyPatchType.Transpiler, "com.btelnyy.socketnetowking.patching");
            }
        }

        public static void InjectIL<T>()
        {
            var type = typeof(T);

            //Use deep method to patch parents as well.
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
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
                        // --- IL Stack at this point ---
                        // 1. Object instance (target) [BEFORE Stfld]
                        // 2. New value                 [BEFORE Stfld]

                        // Duplicate instance (since stfld will consume it)
                        yield return new CodeInstruction(OpCodes.Dup); // Stack: [target, target, value]

                        // Load field metadata
                        yield return new CodeInstruction(OpCodes.Ldtoken, fieldInfo); // Stack: [field, target, target, value]

                        //// Swap the top two items so that the field handle comes before the value
                        //yield return new CodeInstruction(OpCodes.Call, typeof(FieldInfo).GetMethod(nameof(FieldInfo.GetFieldFromHandle), new[] { typeof(RuntimeFieldHandle) }));
                        //// Stack: [target, value, fieldInfo]

                        // Call the event handler
                        yield return new CodeInstruction(OpCodes.Call, typeof(FieldWatcher).GetMethod(nameof(ReportFieldChange), BindingFlags.Static | BindingFlags.NonPublic));

                        // Stack: [target, value]
                    }
                }
                yield return instruction;
            }
        }

        private static void ReportFieldChange(object target, RuntimeFieldHandle fieldHandle)
        {
            var fieldInfo = FieldInfo.GetFieldFromHandle(fieldHandle);
            Console.WriteLine($"Target: {target.GetType().FullName}, Field: {fieldInfo.Name}");
        }
    }
}
