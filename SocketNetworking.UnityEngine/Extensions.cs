using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine
{
    public static class Extensions
    {
        public static Vector3 ReadVector3(this ByteReader reader)
        {
            Vector3 result = new Vector3();
            result.x = reader.ReadFloat();
            result.y = reader.ReadFloat();
            result.z = reader.ReadFloat();
            return result;
        }

        public static void WriteVector3(this ByteWriter writer, Vector3 value)
        {
            writer.WriteFloat(value.x);
            writer.WriteFloat(value.y);
            writer.WriteFloat(value.z);
        }

        public static Vector3Int ReadVector3Int(this ByteReader reader)
        {
            Vector3Int result = new Vector3Int();
            result.x = reader.ReadInt();
            result.y = reader.ReadInt();
            result.z = reader.ReadInt();
            return result;
        }

        public static void WriteVector3Int(this ByteWriter writer, Vector3Int value)
        {
            writer.WriteInt(value.x);
            writer.WriteInt(value.y);
            writer.WriteInt(value.z);
        }

        public static Vector2 ReadVector2(this ByteReader reader)
        {
            Vector2 vector = new Vector2();
            vector.x = reader.ReadFloat();
            vector.y = reader.ReadFloat();
            return vector;
        }

        public static void WriteVector2(this ByteWriter writer, Vector2 value)
        {
            writer.WriteFloat(value.x);
            writer.WriteFloat(value.y);
        }

        public static Vector2Int ReadVector2Int(this ByteReader reader)
        {
            Vector2Int result = new Vector2Int();
            result.x = reader.ReadInt();
            result.y = reader.ReadInt();
            return result;
        }

        public static void WriteVector3Int(this ByteWriter writer, Vector2Int value)
        {
            writer.WriteInt(value.x);
            writer.WriteInt(value.y);
        }

        public static Vector4 ReadVector4(this ByteReader reader)
        {
            Vector4 result = new Vector4();
            result.x = reader.ReadFloat();
            result.y = reader.ReadFloat();
            result.z = reader.ReadFloat();
            result.w = reader.ReadFloat();
            return result;
        }

        public static void WriteVector4(this ByteWriter writer, Vector4 value)
        {
            writer.WriteFloat(value.x);
            writer.WriteFloat(value.y);
            writer.WriteFloat(value.z);
            writer.WriteFloat(value.w);
        }

        public static Quaternion ReadQuaternion(this ByteReader reader)
        {
            Quaternion result = new Quaternion();
            Vector4 vector = reader.ReadVector4();
            result.x = vector.x;
            result.y = vector.y;
            result.z = vector.z;
            result.w = vector.w;
            return result;
        }

        public static void WriteQuaternion(this ByteWriter writer, Quaternion value)
        {
            writer.WriteFloat(value.x);
            writer.WriteFloat(value.y);
            writer.WriteFloat(value.z);
            writer.WriteFloat(value.w);
        }

        public static Color ReadColor(this ByteReader reader)
        {
            float r = reader.ReadFloat();
            float g = reader.ReadFloat();
            float b = reader.ReadFloat();
            float a = reader.ReadFloat();
            Color result = new Color(r, g, b, a);
            return result;
        }

        public static void WriteColor(this ByteWriter writer, Color value)
        {
            writer.WriteFloat(value.r);
            writer.WriteFloat(value.g);
            writer.WriteFloat(value.b);
            writer.WriteFloat(value.a);
        }
    }
}
