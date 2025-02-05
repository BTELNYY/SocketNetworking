﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Shared;
using UnityEditor.SearchService;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkIdentity : NetworkBehavior
    {
        public override bool Spawnable => true;

        /// <summary>
        /// If true, <see cref="INetworkObject.OnSync(Client.NetworkClient)"/> will be used to call <see cref="NetworkObjectExtensions.NetworkSpawn(INetworkObject, Client.NetworkClient)"/> to spawn this object on this new client.
        /// </summary>
        public bool AutoSpawn = false;

        public override void OnSync(NetworkClient client)
        {
            base.OnSync(client);
            if (AutoSpawn)
            {
                this.NetworkSpawn(client);
            }
        }

        public UnityNetworkBehavior ObjectData
        {
            get
            {
                return new UnityNetworkBehavior()
                {
                    Name = gameObject.name,
                    Tree = gameObject.GetTree().ToList(),
                    PrefabID = this.PrefabID,
                };
            }
            private set
            {
                
            }
        }

        public int PrefabID; 

        public override ByteReader RecieveExtraData(byte[] extraData)
        {
            ByteReader reader = base.RecieveExtraData(extraData);
            ObjectData = reader.ReadPacketSerialized<UnityNetworkBehavior>();
            return reader;
        }

        public override ByteWriter SendExtraData()
        {
            ByteWriter writer = base.SendExtraData();
            writer.WritePacketSerialized<UnityNetworkBehavior>(ObjectData);
            return writer;
        }
    }

    public struct UnityNetworkBehavior : IPacketSerializable
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
