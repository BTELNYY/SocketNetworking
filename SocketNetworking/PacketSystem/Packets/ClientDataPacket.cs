﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.PacketSystem.Packets
{
    [PacketDefinition]
    public sealed class ClientDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ClientData;

        public ProtocolConfiguration Configuration { get; set; } = new ProtocolConfiguration();
        
        public string PasswordHash { get; private set; } = "lol";

        public ClientDataPacket(string password) 
        {
            PasswordHash = password.GetStringHash();
        }

        public ClientDataPacket()
        {

        }

        public override PacketReader Deserialize(byte[] data)
        {
            PacketReader reader = base.Deserialize(data);
            PasswordHash = reader.ReadString();
            Configuration = reader.Read<ProtocolConfiguration>();
            return reader;
        }

        public override PacketWriter Serialize()
        {
            PacketWriter writer = base.Serialize();
            writer.WriteString(PasswordHash);
            writer.Write<ProtocolConfiguration>(Configuration);
            return writer;
        }
    }
}