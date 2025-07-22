using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using SocketNetworking.Client;
using SocketNetworking.Modding.Patching.Fields;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Modding
{
    public class InstanceFieldChangeSync<T> : NetworkSyncVar<FieldChanged>
    {
        protected InstanceFieldChangeSync()
        {

        }

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
            if (!_whitelistFields.Contains(args.Field))
            {
                return;
            }
            if (_blacklistFields.Contains(args.Field))
            {
                return;
            }
            if (!Flags.HasFlag(BindingFlags.NonPublic) && args.Field.IsPrivate)
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

        /// <summary>
        /// What fields should be synced?
        /// </summary>
        public BindingFlags Flags { get; set; } = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Defines how the <see cref="InstanceFieldChangeSync{T}"/> will sync fields. This value is not synced if changed, so you should not change this.
        /// </summary>
        public FieldWatchMode Mode { get; set; } = FieldWatchMode.All;

        private List<FieldInfo> _whitelistFields = new List<FieldInfo>();

        private List<FieldInfo> _blacklistFields = new List<FieldInfo>();

        public void WhitelistByType(Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            foreach (FieldInfo field in typeof(T).GetFields(flags).Where(x => x.FieldType == type))
            {
                WhitelistField(field);
            }
        }

        public void BlacklistByType(Type type, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            foreach (FieldInfo field in typeof(T).GetFields(flags).Where(x => x.FieldType == type))
            {
                BlacklistField(field);
            }
        }

        public void WhitelistField(FieldInfo info)
        {
            if (info.DeclaringType != typeof(T))
            {
                throw new InvalidOperationException($"Field {info.Name} is not a part of {typeof(T).FullName}");
            }
            if (_whitelistFields.Contains(info))
            {
                return;
            }
            _whitelistFields.Add(info);
        }

        public void UnwhitelistField(FieldInfo info)
        {
            if (info.DeclaringType != typeof(T))
            {
                throw new InvalidOperationException($"Field {info.Name} is not a part of {typeof(T).FullName}");
            }
            if (!_whitelistFields.Contains(info))
            {
                return;
            }
            _whitelistFields.Remove(info);
        }

        public void BlacklistField(FieldInfo info)
        {
            if (info.DeclaringType != typeof(T))
            {
                throw new InvalidOperationException($"Field {info.Name} is not a part of {typeof(T).FullName}");
            }
            if (_blacklistFields.Contains(info))
            {
                return;
            }
            _blacklistFields.Add(info);
        }

        public void UnblacklistField(FieldInfo info)
        {
            if (info.DeclaringType != typeof(T))
            {
                throw new InvalidOperationException($"Field {info.Name} is not a part of {typeof(T).FullName}");
            }
            if (!_blacklistFields.Contains(info))
            {
                return;
            }
            _blacklistFields.Remove(info);
        }

        /// <summary>
        /// Called when the <see cref="SyncAll"/> function has finished.
        /// </summary>
        public event Action SyncAllCompleted;

        /// <summary>
        /// Called when the <see cref="SyncAllTo(NetworkClient)"/> function has completed.
        /// </summary>
        public event Action<NetworkClient> SyncAllToCompleted;

        public void SyncAll()
        {
            SyncVarData data = GetData();
            SyncVarUpdatePacket packet = GetPacket();
            List<SyncVarData> list = new List<SyncVarData>();
            switch (Mode)
            {
                case FieldWatchMode.All:
                    foreach (FieldInfo info in typeof(T).GetFields(Flags))
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
                case FieldWatchMode.Whitelist:
                    foreach (FieldInfo info in _whitelistFields)
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
                case FieldWatchMode.Blacklist:
                    foreach (FieldInfo info in Target.GetType().GetFields().Except(_blacklistFields))
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
                case FieldWatchMode.Both:
                    foreach (FieldInfo info in _whitelistFields.Except(_blacklistFields))
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
            }
            List<List<SyncVarData>> lists = Utils.SplitIntoChunks<SyncVarData>(list, Packet.MaxPacketSize / 2);
            foreach (List<SyncVarData> list1 in lists)
            {
                packet.Data = list1;
                if (NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    NetworkServer.SendToAll(packet, x => OwnerObject.CheckVisibility(x));
                }
                else if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    if ((OwnershipMode == OwnershipMode.Client) && OwnerObject.OwnerClientID == NetworkClient.LocalClient.ClientID || OwnerObject.HasPrivilege(NetworkClient.LocalClient.ClientID))
                    {
                        NetworkClient.LocalClient.Send(packet);
                    }
                    else if (OwnershipMode == OwnershipMode.Public)
                    {
                        NetworkClient.LocalClient.Send(packet);
                    }
                    else
                    {
                        throw new SecurityException("Tried to set a SyncVar which the local client does not own.");
                    }
                }
            }
            SyncAllCompleted?.Invoke();
        }

        public void SyncAllTo(NetworkClient who)
        {
            if (NetworkManager.WhereAmI != ClientLocation.Remote)
            {
                return;
            }
            SyncVarData data = GetData();
            SyncVarUpdatePacket packet = GetPacket();
            List<SyncVarData> list = new List<SyncVarData>();
            switch (Mode)
            {
                case FieldWatchMode.All:
                    foreach (FieldInfo info in typeof(T).GetFields(Flags))
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
                case FieldWatchMode.Whitelist:
                    foreach (FieldInfo info in _whitelistFields)
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
                case FieldWatchMode.Blacklist:
                    List<SyncVarData> list3 = new List<SyncVarData>();
                    foreach (FieldInfo info in Target.GetType().GetFields().Except(_blacklistFields))
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
                case FieldWatchMode.Both:
                    List<SyncVarData> list4 = new List<SyncVarData>();
                    foreach (FieldInfo info in _whitelistFields.Except(_blacklistFields))
                    {
                        FieldChangeEventArgs args = new FieldChangeEventArgs(Target, info);
                        data.Data = ByteConvert.Serialize(args);
                        list.Add(data);
                    }
                    break;
            }
            List<List<SyncVarData>> lists = Utils.SplitIntoChunks<SyncVarData>(list, Packet.MaxPacketSize / 2);
            foreach (List<SyncVarData> list1 in lists)
            {
                packet.Data = list1;
                who.Send(packet);
            }
            SyncAllToCompleted?.Invoke(who);
        }

        public enum FieldWatchMode
        {
            /// <summary>
            /// All fields will be synced (if possible)
            /// </summary>
            All = 0,
            /// <summary>
            /// Only whitelisted fields will be synced.
            /// </summary>
            Whitelist = 1,
            /// <summary>
            /// Only non-blacklisted fields will be synced.
            /// </summary>
            Blacklist = 2,
            /// <summary>
            /// Both Whitelisted and Blacklisted fields will be sent. First, whitelists are applied, then the blacklist
            /// </summary>
            Both = 3,
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
            return (int)Serialize().Length;
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
