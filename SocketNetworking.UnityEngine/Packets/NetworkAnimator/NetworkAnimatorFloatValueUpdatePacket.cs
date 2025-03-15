using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorFloatValueUpdatePacket : CustomPacket
    {
        public NetworkAnimatorFloatValueUpdatePacket() { }

        public NetworkAnimatorFloatValueUpdatePacket(string name, float value)
        {
            Value = value;
            ValueHash = Animator.StringToHash(name);
        }

        public NetworkAnimatorFloatValueUpdatePacket(int id, float value)
        {
            Value = value;
            ValueHash = id;
        }

        public NetworkAnimatorFloatValueUpdatePacket(string name, float value, float dampTime, float deltaTime)
        {
            Value = value;
            DampTime = dampTime;
            DeltaTime = deltaTime;
            ReadFloatSpecificValues = true;
            ValueHash = Animator.StringToHash(name);
        }

        public NetworkAnimatorFloatValueUpdatePacket(int id, float value, float dampTime, float deltaTime)
        {
            Value = value;
            DampTime = dampTime;
            DeltaTime = deltaTime;
            ReadFloatSpecificValues = true;
            ValueHash = id;
        }

        public float Value { get; set; } = 0f;

        public int ValueHash { get; set; } = 0;

        public bool ReadFloatSpecificValues { get; set; } = false;

        public float DampTime { get; set; } = float.PositiveInfinity;

        public float DeltaTime { get; set; } = float.PositiveInfinity;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(ValueHash);
            writer.WriteFloat(Value);
            writer.WriteBool(ReadFloatSpecificValues);
            if (ReadFloatSpecificValues)
            {
                writer.WriteFloat(DampTime);
                writer.WriteFloat(DeltaTime);
            }
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ValueHash = reader.ReadInt();
            Value = reader.ReadFloat();
            ReadFloatSpecificValues = reader.ReadBool();
            if (ReadFloatSpecificValues)
            {
                DampTime = reader.ReadFloat();
                DeltaTime = reader.ReadFloat();
            }
            return reader;
        }
    }
}
