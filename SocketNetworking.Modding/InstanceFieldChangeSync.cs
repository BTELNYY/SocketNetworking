using System;
using System.Reflection;
using SocketNetworking.Client;
using SocketNetworking.Modding.Patching.Fields;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Modding
{
    public class InstanceFieldChangeSync<T> : NetworkSyncVar<FieldChanged>
    {
        public InstanceFieldChangeSync(FieldChanged value, T target) : base(value)
        {
            Type = typeof(T);
            Target = target;
            FieldWatcher.FieldChanged += OnFieldChanged;
        }

        public InstanceFieldChangeSync(INetworkObject ownerObject, FieldChanged value, T target) : base(ownerObject, value)
        {
            Type = typeof(T);
            Target = target;
            FieldWatcher.FieldChanged += OnFieldChanged;
        }

        public InstanceFieldChangeSync(INetworkObject ownerObject, OwnershipMode syncOwner, T target) : base(ownerObject, syncOwner)
        {
            Type = typeof(T);
            Target = target;
            FieldWatcher.FieldChanged += OnFieldChanged;
        }

        public InstanceFieldChangeSync(INetworkObject ownerObject, OwnershipMode syncOwner, T target, FieldChanged value) : base(ownerObject, syncOwner, value)
        {
            Type = typeof(T);
            Target = target;
            FieldWatcher.FieldChanged += OnFieldChanged;
        }

        ~InstanceFieldChangeSync()
        {
            FieldWatcher.FieldChanged -= OnFieldChanged;
        }

        private void OnFieldChanged(object sender, FieldChangeEventArgs args)
        {
            if (sender == null)
            {
                return;
            }
            if (sender.GetType() != typeof(T) || sender != Target)
            {
                return;
            }
            FieldChanged field = new FieldChanged(args.Field.Name, args.NewValue);
            Value = field;
        }

        public Type Type { get; }

        public object Target { get; set; }

        public BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override void RawSet(object value, NetworkClient who)
        {
            if (value is FieldChanged changed)
            {
                Target.GetType().GetField(changed.FieldName, BindingFlags)?.SetValue(Target, changed.Data);
            }
            base.RawSet(value, who);
        }
    }

    public class FieldChanged : IByteSerializable
    {
        public FieldChanged()
        {
            FieldName = "";
            Data = null;
        }

        public FieldChanged(string name, object data)
        {
            FieldName = name;
            Data = data;
        }


        public string FieldName;

        public object Data;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            FieldName = reader.ReadString();
            Data = reader.ReadObject<object>();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(FieldName);
            writer.WriteObject(Data);
            return writer;
        }
    }
}
