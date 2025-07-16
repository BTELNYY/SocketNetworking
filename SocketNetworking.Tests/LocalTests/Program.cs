using System;
using HarmonyLib.Tools;

namespace SocketNetworking.Tests.LocalTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HarmonyFileLog.Writer = Console.Out;
            HarmonyFileLog.Enabled = true;
            Modding.Patching.Fields.FieldWatcher.InjectIL(typeof(Test));
            Modding.Patching.Fields.FieldWatcher.FieldChanged += FieldWatcher_FieldChanged;
            Test test = new Test();
            test.TestField();
            OrderedLinkedList<int> list = new OrderedLinkedList<int>(new int[] { 3, -1, 93, 9999, -38384, 34, 88 });
            Console.WriteLine(list.ToString());
        }

        private static void FieldWatcher_FieldChanged(object sender, Modding.Patching.Fields.FieldChangeEventArgs e)
        {
            Console.WriteLine($"Target: {e.Target.GetType().FullName}, Field: {e.Field.Name}, Value: {e.NewValue}");
        }
    }

    public class Test
    {
        static int wow = 0;

        int field1 = 0;

        float field2 = 0f;

        int[] ints = new int[0];

        public void TestField()
        {
            wow += 1;
            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                field1 = random.Next();
                field2 = random.Next();
            }
            ints = new int[3];
            ints[0] = wow;
            ints[1] = field1;
            ints[2] = 0;
        }
    }
}