using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using SocketNetworking.Attributes;
using SocketNetworking.Client;
using SocketNetworking.Exceptions;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Server;
using SocketNetworking.Shared.NetworkObjects;

namespace SocketNetworking.Shared.Serialization
{
    public class ByteConvert
    {
        static ByteConvert()
        {
            Log = new Log("[Network Serialization]");
        }

        public static Log Log;

        /// <summary>
        /// List of allowed types for serialization. (Technically you can serialize anything but thats not a good idea, for complex structures just make use of <see cref="IPacketSerializable"/>)
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

        public static SerializedData Serialize<T>(T data)
        {
            SerializedData sData = Serialize((object)data);
            return sData;
        }

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
            SerializedData sData = new SerializedData();
            sData.Type = dataType;

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

            throw new NetworkSerializationException($"Type '{data.GetType().FullName}' cannot be serialized. Please try making a TypeWrapper, or making this type IPacketSerializable");
            return SerializedData.NullData;
            SerializableList<SerializedData> fieldData = new SerializableList<SerializedData>();
            SerializableList<SerializedData> propertyData = new SerializableList<SerializedData>();
            if (dataType.GetCustomAttribute<NetworkSerialized>() != null)
            {
                List<FieldInfo> fields = dataType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkNonSerialized>() == null).ToList();
                List<PropertyInfo> properties = dataType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite && x.CanRead && x.GetCustomAttributes<NetworkNonSerialized>() == null).ToList();

                foreach (FieldInfo field in fields)
                {
                    SerializedData returneData = Serialize(field.GetValue(data));
                    fieldData.Add(returneData);
                }
                foreach (PropertyInfo property in properties)
                {
                    SerializedData returneData = Serialize(property.GetValue(data));
                    propertyData.Add(returneData);
                }
            }
            else
            {
                List<FieldInfo> fields = dataType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkSerialized>() != null).ToList();
                List<PropertyInfo> properties = dataType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite && x.CanRead && x.GetCustomAttributes<NetworkSerialized>() != null).ToList();
                foreach (FieldInfo field in fields)
                {
                    SerializedData returneData = Serialize(field.GetValue(data));
                    fieldData.Add(returneData);
                }
                foreach (PropertyInfo property in properties)
                {
                    SerializedData returneData = Serialize(property.GetValue(data));
                    propertyData.Add(returneData);
                }
            }

            writer.WritePacketSerialized<SerializableList<SerializedData>>(fieldData);
            writer.WritePacketSerialized<SerializableList<SerializedData>>(propertyData);
            sData.Data = writer.Data;
            sData.DataNull = sData.Data == null;
            return sData;
        }

        public static T DeserializeRaw<T>(byte[] data)
        {
            SerializedData sData = new SerializedData()
            {
                Data = data,
                Type = typeof(T),
            };
            T output = Deserialize<T>(sData, out int read);
            return output;
        }

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
                    throw new Exception("Can't find the networkclient which is referenced in this serialization.");
                }
                read = reader.ReadBytes;
                return client;
            }

            if(data.Type.GetInterfaces().Contains(typeof(INetworkObject)))
            {
                int id = reader.ReadInt();
                read = reader.ReadBytes;
                (INetworkObject, NetworkObjectData) obj = NetworkManager.GetNetworkObjectByID(id);
                if(obj.Item1 == null)
                {
                    return null;
                }
                return obj.Item1 as INetworkObject;
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
                object typper = Activator.CreateInstance(wrapper);
                object output = typper.GetType().GetMethod("Deserialize").Invoke(typper, new object[] { data.Data });
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

            throw new NetworkSerializationException($"Type '{data.GetType().FullName}' cannot be deserialized. Please try making a TypeWrapper, or making this type IPacketSerializable");
            object newObject = Activator.CreateInstance(data.Type);
            List<SerializedData> fieldData = reader.ReadPacketSerialized<SerializableList<SerializedData>>().ContainedList;
            List<SerializedData> propertyData = reader.ReadPacketSerialized<SerializableList<SerializedData>>().ContainedList;
            int customReadBytes = 0;
            if (data.Type.GetCustomAttribute<NetworkSerialized>() != null)
            {
                List<FieldInfo> fields = data.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkNonSerialized>() == null).ToList();
                List<PropertyInfo> properties = data.Type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite && x.CanRead && x.GetCustomAttributes<NetworkNonSerialized>() == null).ToList();
                int counter = 0;
                foreach (FieldInfo field in fields)
                {
                    field.SetValue(newObject, Deserialize(fieldData[counter], out int bytes));
                    customReadBytes += bytes;
                }
                counter = 0;
                foreach (PropertyInfo property in properties)
                {
                    property.SetValue(newObject, Deserialize(propertyData[counter], out int bytes));
                    customReadBytes += bytes;
                }
            }
            else
            {
                List<FieldInfo> fields = data.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkSerialized>() != null).ToList();
                List<PropertyInfo> properties = data.Type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite && x.CanRead && x.GetCustomAttributes<NetworkSerialized>() != null).ToList();
                int counter = 0;
                foreach (FieldInfo field in fields)
                {
                    field.SetValue(newObject, Deserialize(fieldData[counter], out int bytes));
                    customReadBytes += bytes;
                }
                counter = 0;
                foreach (PropertyInfo property in properties)
                {
                    property.SetValue(newObject, Deserialize(propertyData[counter], out int bytes));
                    customReadBytes += bytes;
                }
            }
            read = reader.ReadBytes;
            return newObject;
        }

        public static T Deserialize<T>(SerializedData data, out int read)
        {
            if (data.Type != typeof(T))
            {
                throw new NetworkDeserializationException("Types provided do not match.");
            }
            read = 0;
            T output = (T)Deserialize(data, out read);
            return output;
        }

        public static T Deserialize<T>(byte[] data)
        {
            ByteReader br = new ByteReader(data);
            SerializedData sData = br.ReadPacketSerialized<SerializedData>();
            if (!br.IsEmpty)
            {
                Log.GlobalWarning("Provided Data Array was not emptied by the deseiralizer, probably extra bytes?");
            }
            Type givenType = sData.Type;
            if (givenType == null)
            {
                throw new NetworkDeserializationException($"Type {givenType.Name} cannnot be found.");
            }
            if (!typeof(T).IsAssignableFrom(givenType))
            {
                throw new NetworkDeserializationException("Given Type is not Assignable from the deserialized type.");
            }
            return Deserialize<T>(sData, out int read);
        }
    }

    public struct SerializedData : IPacketSerializable
    {
        Type _type;

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

        public bool DataNull;

        public byte[] Data;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Type type = reader.ReadWrapper<SerializableType, Type>();
            Type = type;
            DataNull = reader.ReadBool();
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
            writer.WriteWrapper<SerializableType, Type>(new SerializableType(Type));
            if (Data == null)
            {
                writer.WriteBool(true);
                Data = new byte[] { };
            }
            else
            {
                writer.WriteBool(false);
            }
            writer.WriteByteArray(Data);
            return writer;
        }

        //Used internally to represent void returns in RPC and what not.
        public static SerializedData NullData
        {
            get
            {
                SerializedData data = new SerializedData();
                data.Type = typeof(void);
                data.DataNull = true;
                data.Data = null;
                return data;
            }
        }
    }
}
