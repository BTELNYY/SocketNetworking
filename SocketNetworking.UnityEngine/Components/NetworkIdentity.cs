using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.TypeWrappers;
using UnityEditor.SearchService;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkIdentity : NetworkObject
    {
        public UnityNetworkObject ObjectData
        {
            get
            {
                return new UnityNetworkObject()
                {
                    Name = gameObject.name,
                    Tree = new List<string>(),
                    PrefabID = Prefab.PrefabID,
                };
            }
            private set
            {
                
            }
        }

        public NetworkPrefab Prefab;

        public override ByteReader RecieveExtraData(byte[] extraData)
        {
            ByteReader reader = base.RecieveExtraData(extraData);
            ObjectData = reader.ReadPacketSerialized<UnityNetworkObject>();
            return reader;
        }

        public override ByteWriter SendExtraData()
        {
            ByteWriter writer = base.SendExtraData();
            writer.WritePacketSerialized<UnityNetworkObject>(ObjectData);
            return writer;
        }
    }

    public struct UnityNetworkObject : IPacketSerializable
    {
        public string Name;

        public int PrefabID;

        public List<string> Tree;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Name = reader.ReadString();
            PrefabID = reader.ReadInt();
            Tree = reader.ReadPacketSerialized<SerializableList<string>>().ContainedList;
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Name);
            writer.WriteInt(PrefabID);
            SerializableList<string> tree = new SerializableList<string>(Tree);
            writer.WritePacketSerialized<SerializableList<string>>(tree);
            return writer.Data;
        }
    }
}
