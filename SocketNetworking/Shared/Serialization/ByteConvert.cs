using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared.Exceptions;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;

namespace SocketNetworking.Shared.Serialization
{
    /// <summary>
    /// <see cref="ByteConvert"/> is a utility class to allow converting of data to and from bytes without manually using <see cref="ByteReader"/> or <see cref="ByteWriter"/>. It is still recommended to use the formerly mentioned classes.
    /// </summary>
    public class ByteConvert
    {
        static ByteConvert()
        {
            Log = new Log("[Network Serialization]");
        }

        public static Log Log;

        /// <summary>
        /// List of allowed types for serialization. (Technically you can serialize anything but that's not a good idea, for complex structures just make use of <see cref="IPacketSerializable"/>)
        /// </summary>
        public static readonly Type[] SupportedTypes =
        {
            typeof(IEnumerable),
            typeof(Enum),

            typeof(string),
            typeof(bool),

            typeof(byte),
            typeof(sbyte),

            typeof(short),
            typeof(ushort),

            typeof(int),
            typeof(uint),

            typeof(long),
            typeof(ulong),

            typeof(float),
            typeof(double),

            typeof(NetworkClient),
            typeof(INetworkObject),
            typeof(IPacketSerializable),
        };

        /// <summary>
        /// Serializes given data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static SerializedData Serialize<T>(T data)
        {
            SerializedData sData = Serialize((object)data);
            return sData;
        }

        /// <summary>
        /// Serializes given data, if the data cannot be serialized, <see cref="NetworkConversionException"/> is thrown.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NetworkConversionException"></exception>
        public static SerializedData Serialize(object data)
        {
            ByteWriter writer = new ByteWriter();
            if (data == null)
            {
                return new SerializedData()
                {
                    Type = typeof(void),
                    DataNull = true,
                    Data = new byte[] { },
                };
            }
            Type dataType = data.GetType();
            SerializedData sData = new SerializedData
            {
                Type = dataType
            };

            if (dataType.IsSubclassOf(typeof(NetworkClient)))
            {
                NetworkClient client = data as NetworkClient;
                writer.WriteInt(client.ClientID);
                sData.Type = typeof(NetworkClient);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType.IsSubclassDeep(typeof(INetworkObject)))
            {
                INetworkObject networkObject = data as INetworkObject;
                writer.WriteInt(networkObject.NetworkID);
                sData.Type = data.GetType();
                sData.Data = writer.Data;
                return sData;
            }

            if (data is IPacketSerializable serializable)
            {
                writer.WritePacketSerialized<IPacketSerializable>(serializable);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType.GetInterfaces().Contains(typeof(IEnumerable)) && data is IEnumerable<object> enumerable)
            {
                IEnumerable<object> values = enumerable;
                SerializableList<object> list = new SerializableList<object>(values);
                writer.WritePacketSerialized<SerializableList<object>>(list);
                sData.Data = writer.Data;
                return sData;
            }

            if (NetworkManager.TypeToTypeWrapper.ContainsKey(dataType))
            {
                object obj = Activator.CreateInstance(NetworkManager.TypeToTypeWrapper[dataType]);
                PropertyInfo valueInfo = obj.GetType().GetProperty(nameof(TypeWrapper<object>.Value));
                valueInfo.SetValue(obj, data, null);
                MethodInfo serialize = obj.GetType().GetMethod(nameof(TypeWrapper<object>.Serialize));
                byte[] array = (byte[])serialize.Invoke(obj, null);
                writer.Write(array);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType.IsEnum)
            {
                Enum lastEnum = (Enum)data.LastEnum();
                int value = (int)(object)lastEnum;
                dataType = typeof(int);
                data = Convert.ChangeType(data, typeof(int));
                sData.Type = data.GetType();
            }

            if (dataType == typeof(string))
            {
                string value = (string)Convert.ChangeType(data, typeof(string));
                writer.WriteString(value);
                sData.Data = writer.Data;
                return sData;
            }
            if (dataType == typeof(bool))
            {
                bool value = (bool)Convert.ChangeType(data, typeof(bool));
                writer.WriteBool(value);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType == typeof(byte))
            {
                byte value = (byte)Convert.ChangeType(data, typeof(byte));
                writer.WriteByte(value);
                sData.Data = writer.Data;
                return sData;
            }
            if (dataType == typeof(sbyte))
            {
                sbyte value = (sbyte)Convert.ChangeType(data, typeof(sbyte));
                writer.WriteSByte(value);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType == typeof(int))
            {
                int value = (int)Convert.ChangeType(data, typeof(int));
                writer.WriteInt(value);
                sData.Data = writer.Data;
                return sData;
            }
            if (dataType == typeof(uint))
            {
                uint value = (uint)Convert.ChangeType(data, typeof(uint));
                writer.WriteUInt(value);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType == typeof(long))
            {
                long value = (long)Convert.ChangeType(data, typeof(long));
                writer.WriteLong(value);
                sData.Data = writer.Data;
                return sData;
            }
            if (dataType == typeof(ulong))
            {
                ulong value = (ulong)Convert.ChangeType(data, typeof(ulong));
                writer.WriteULong(value);
                sData.Data = writer.Data;
                return sData;
            }

            if (dataType == typeof(float))
            {
                float value = (float)Convert.ChangeType(data, typeof(float));
                writer.WriteFloat(value);
                sData.Data = writer.Data;
                return sData;
            }
            if (dataType == typeof(double))
            {
                double value = (float)Convert.ChangeType(data, typeof(double));
                writer.WriteDouble(value);
                sData.Data = writer.Data;
                return sData;
            }

            throw new NetworkConversionException($"Type '{data.GetType().FullName}' cannot be serialized. Please try making a TypeWrapper, or making this type IPacketSerializable");
        }

        /// <summary>
        /// Deserializes Data from a byte array with casting to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T DeserializeRaw<T>(byte[] data)
        {
            SerializedData sData = new SerializedData()
            {
                Data = data,
                Type = typeof(T),
            };
            T output = Deserialize<T>(sData, out _);
            return output;
        }

        /// <summary>
        /// Deserializes Data from a <see cref="SerializedData"/> parameter. <paramref name="read"/> is the amount of bytes read.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="read"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NetworkConversionException"></exception>
        public static object Deserialize(SerializedData data, out int read)
        {
            ByteReader reader = new ByteReader(data.Data);
            if (data.DataNull)
            {
                read = 0;
                return null;
            }

            if (data.Type == null)
            {
                read = 0;
                return null;
            }

            if (data.Type == typeof(void))
            {
                read = reader.ReadBytes;
                return null;
            }

            if (data.Type == typeof(NetworkClient))
            {
                int clientId = reader.ReadInt();
                NetworkClient client = null;
                if (NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    client = NetworkServer.GetClient(clientId);
                }
                else if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    client = NetworkClient.Clients.Where(x => x.ClientID == clientId).FirstOrDefault();
                }
                if (client == default(NetworkClient))
                {
                    throw new Exception("Can't find the NetworkClient which is referenced in this serialization.");
                }
                read = reader.ReadBytes;
                return client;
            }

            if (data.Type.GetInterfaces().Contains(typeof(INetworkObject)))
            {
                int id = reader.ReadInt();
                read = reader.ReadBytes;
                (INetworkObject, NetworkObjectData) obj = NetworkManager.GetNetworkObjectByID(id);
                if (obj.Item1 == null)
                {
                    return null;
                }
                return obj.Item1;
            }

            if (data.Type.GetInterfaces().Contains(typeof(IPacketSerializable)))
            {
                object obj = Activator.CreateInstance(data.Type);
                IPacketSerializable serializable = (IPacketSerializable)obj;
                read = serializable.Deserialize(data.Data).ReadBytes;
                return serializable;
            }

            if (NetworkManager.TypeToTypeWrapper.ContainsKey(data.Type))
            {
                Type wrapper = NetworkManager.TypeToTypeWrapper[data.Type];
                object type = Activator.CreateInstance(wrapper);
                object output = type.GetType().GetMethod("Deserialize").Invoke(type, new object[] { data.Data });
                FieldInfo item1 = output.GetType().GetField(nameof(ValueTuple<object, int>.Item1));
                FieldInfo item2 = output.GetType().GetField(nameof(ValueTuple<object, int>.Item2));
                read = (int)item2.GetValue(output);
                return Convert.ChangeType(item1.GetValue(output), data.Type);
            }

            if (data.Type.IsEnum)
            {
                int value = reader.ReadInt();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            if (data.Type == typeof(string))
            {
                string str = reader.ReadString();
                read = reader.ReadBytes;
                return Convert.ChangeType(str, data.Type);
            }
            if (data.Type == typeof(bool))
            {
                bool value = reader.ReadBool();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            if (data.Type == typeof(byte))
            {
                byte value = reader.ReadByte();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }
            if (data.Type == typeof(sbyte))
            {
                sbyte value = reader.ReadSByte();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            if (data.Type == typeof(short))
            {
                short value = reader.ReadShort();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }
            if (data.Type == typeof(ushort))
            {
                ushort value = reader.ReadUShort();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            if (data.Type == typeof(int))
            {
                int value = reader.ReadInt();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }
            if (data.Type == typeof(uint))
            {
                uint value = reader.ReadUInt();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            if (data.Type == typeof(long))
            {
                long value = reader.ReadLong();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }
            if (data.Type == typeof(ulong))
            {
                ulong value = reader.ReadULong();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            if (data.Type == typeof(float))
            {
                float value = reader.ReadFloat();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }
            if (data.Type == typeof(double))
            {
                double value = reader.ReadDouble();
                read = reader.ReadBytes;
                return Convert.ChangeType(value, data.Type);
            }

            throw new NetworkConversionException($"Type '{data.Type.FullName}' cannot be deserialized. Please try making a TypeWrapper, or making this type IPacketSerializable");
        }

        /// <summary>
        /// Deserializes Data and casts it to <typeparamref name="T"/>. If <see cref="SerializedData.Type"/> and <typeparamref name="T"/> do not match, <see cref="NetworkConversionException"/> is thrown.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="read"></param>
        /// <returns></returns>
        /// <exception cref="NetworkConversionException"></exception>
        public static T Deserialize<T>(SerializedData data, out int read)
        {
            if (data.Type != typeof(T))
            {
                throw new NetworkConversionException("Types provided do not match.");
            }
            T output = (T)Deserialize(data, out read);
            return output;
        }

        /// <summary>
        /// Tries to Deserialize a <see cref="SerializedData"/> from the byte array and then tries to cast the contained data to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NetworkConversionException"></exception>
        public static T Deserialize<T>(byte[] data)
        {
            ByteReader br = new ByteReader(data);
            SerializedData sData = br.ReadPacketSerialized<SerializedData>();
            if (!br.IsEmpty)
            {
                Log.GlobalWarning("Provided Data Array was not emptied by the deserializer, probably extra bytes?");
            }
            Type givenType = sData.Type;
            if (givenType == null)
            {
                throw new NetworkConversionException($"Type {givenType.Name} cannot be found.");
            }
            if (!typeof(T).IsAssignableFrom(givenType))
            {
                throw new NetworkConversionException("Given Type is not Assignable from the deserialized type.");
            }
            return Deserialize<T>(sData, out _);
        }
    }

    /// <summary>
    /// The <see cref="SerializedData"/> struct contains the <see cref="System.Type"/> and the Data for the serialized data.
    /// </summary>
    public struct SerializedData : IPacketSerializable
    {
        Type _type;

        /// <summary>
        /// The <see cref="System.Type"/> of the contained data.
        /// </summary>
        public Type Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        /// <summary>
        /// Determines if the data is treated as being <see langword="null"/>.
        /// </summary>
        public bool DataNull;

        /// <summary>
        /// The data buffer.
        /// </summary>
        public byte[] Data;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            DataNull = reader.ReadBool();
            if (DataNull)
            {
                Data = new byte[0];
                Type = typeof(void);
                return reader;
            }
            Type type = reader.ReadWrapper<SerializableType, Type>();
            Type = type;
            Data = reader.ReadByteArray();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            if (Type == null)
            {
                Type = typeof(void);
            }
            if (Data == null || Type == typeof(void))
            {
                writer.WriteBool(true);
                return writer;
            }
            else
            {
                writer.WriteBool(false);
            }
            writer.WriteWrapper<SerializableType, Type>(new SerializableType(Type));
            writer.WriteByteArray(Data);
            return writer;
        }

        /// <summary>
        /// Template <see cref="SerializedData"/> to represent a value that is <see langword="null"/>.
        /// </summary>
        public static SerializedData NullData
        {
            get
            {
                SerializedData data = new SerializedData
                {
                    Type = typeof(void),
                    DataNull = true,
                    Data = null
                };
                return data;
            }
        }

        public override string ToString()
        {
            return $"Type: {Type}, DataNull: {DataNull}, Data: {Data.Length}";
        }
    }
}
