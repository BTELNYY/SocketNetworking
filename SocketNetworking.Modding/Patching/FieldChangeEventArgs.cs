using System;
using System.Reflection;

namespace SocketNetworking.Modding.Patching
{
    public class FieldChangeEventArgs : EventArgs
    {
        public object Target { get; }
        public FieldInfo Field { get; }
        public object NewValue { get; }

        public FieldChangeEventArgs(object target, FieldInfo field, object newValue)
        {
            Target = target;
            Field = field;
            NewValue = newValue;
        }
    }
}
