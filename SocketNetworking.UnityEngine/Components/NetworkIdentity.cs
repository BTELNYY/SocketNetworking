using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking.Client;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkIdentity : NetworkBehavior
    {
        private void Awake()
        {
            if (AutoRegister && !UnityNetworkManager.IsPrefab(gameObject))
            {
                RegisterObject();
            }
        }

        public override bool Spawnable => true;

        /// <summary>
        /// If true, <see cref="INetworkObject.OnSync(NetworkClient)"/> will be used to call <see cref="NetworkObjectExtensions.NetworkSpawn(INetworkObject, Client.NetworkClient)"/> to spawn this object on the new client.
        /// </summary>
        public bool AutoSpawn = false;

        /// <summary>
        /// If true, and if this Identity is not a prefab, will automatically register using <see cref="NetworkBehavior.RegisterObject()"/>
        /// </summary>
        public virtual bool AutoRegister => true;

        private List<NetworkComponent> _components = new List<NetworkComponent>();

        public void RegisterComponent(NetworkComponent component)
        {
            if (_components.Contains(component)) return;
            _components.Add(component);
        }

        public void UnregisterComponent(NetworkComponent component)
        {
            _components.Remove(component);
        }

        public void DisableComponents()
        {
            foreach (NetworkComponent component in _components)
            {
                component.Enabled = false;
            }
        }

        public void EnableComponents()
        {
            foreach (NetworkComponent component in _components)
            {
                component.Enabled = true;
            }
        }

        public override void Destroy()
        {
            foreach (NetworkComponent component in _components)
            {
                component.Destroy();
            }
            base.Destroy();
        }

        public List<NetworkComponent> Components => _components;

        public void DestroyComponents()
        {
            foreach (NetworkComponent component in _components)
            {
                component.Destroy();
            }
        }

        public virtual void SetupComponents()
        {

        }

        public override void OnSync(NetworkClient client)
        {
            base.OnSync(client);
            if (AutoSpawn)
            {
                this.NetworkSpawn(client);
            }
        }

        private UnityObjectData _data;

        public UnityObjectData ObjectData
        {
            get
            {
                if (_data == null)
                {
                    _data = new UnityObjectData()
                    {
                        Name = gameObject.name,
                        Tree = gameObject.GetTree().ToList(),
                        PrefabID = this.PrefabID,
                    };
                }
                return _data;
            }
            private set
            {
                _data = value;
            }
        }

        public int PrefabID { get; set; }

        public override ByteReader ReceiveExtraData(byte[] extraData)
        {
            ByteReader reader = base.ReceiveExtraData(extraData);
            ObjectData = reader.ReadPacketSerialized<UnityObjectData>();
            int count = reader.ReadInt();
            if (count != _components.Count)
            {
                throw new InvalidOperationException($"Mismatch of component count from peer. Expected: {_components.Count}, Got: {count}");
            }
            for (int i = 0; i < count; i++)
            {
                ComponentData data = reader.ReadPacketSerialized<ComponentData>();
                _components[data.Index].ReceiveComponentData(data.Data);
            }
            return reader;
        }

        public override ByteWriter SendExtraData()
        {
            ByteWriter writer = base.SendExtraData();
            writer.WritePacketSerialized<UnityObjectData>(ObjectData);
            writer.WriteInt(_components.Count);
            for (int i = 0; i < _components.Count; i++)
            {
                ComponentData data = new ComponentData(i, _components[i].SendComponentData().Data);
                writer.WritePacketSerialized<ComponentData>(data);
            }
            return writer;
        }

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            SetupComponents();
        }
    }

    public class ComponentData : IByteSerializable
    {
        public ComponentData()
        {

        }

        public ComponentData(int index, byte[] data)
        {
            Index = index;
            Data = data;
        }

        public int Index { get; set; }

        public byte[] Data { get; set; }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = data.GetReader();
            Index = reader.ReadInt();
            Data = reader.ReadByteArray();
            return reader;
        }

        public int GetLength()
        {
            return (int)Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(Index);
            writer.WriteByteArray(Data);
            return writer;
        }
    }

    public class UnityObjectData : IByteSerializable
    {
        public string Name { get; set; }

        public int PrefabID { get; set; }

        public List<string> Tree { get; set; }

        public byte[] Extra { get; set; } = new byte[0];

        public virtual ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Name = reader.ReadString();
            PrefabID = reader.ReadInt();
            Tree = reader.ReadPacketSerialized<SerializableList<string>>().ContainedList;
            Extra = reader.ReadByteArray();
            return reader;
        }

        public virtual int GetLength()
        {
            return (int)Serialize().Length;
        }

        public virtual ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Name);
            writer.WriteInt(PrefabID);
            SerializableList<string> tree = new SerializableList<string>(Tree);
            writer.WritePacketSerialized<SerializableList<string>>(tree);
            writer.WriteByteArray(Extra);
            return writer;
        }
    }
}
