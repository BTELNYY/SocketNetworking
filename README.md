# SocketNetworking
 A Networking Library using Vairous Protocols and custom packets.

 The main parts you'll be using are Tcp/UdpNetworkClient, NetworkServer, INetworkObject, CustomPacket, and NetworkManager.

## Install Requirements
 * .NET Framework 4.8

## Importing Custom Content
 * Assumming you marked your TypeWrappers, Custom Packets and NetworkObjects, you can call `NetworkManager.ImportAssmebly(Assembly yourAssemblyHere)` to have the library recognize your new content.

### Making Custom Type Wrappers
 * Usually, you would use `IPacketSerializable` for types you created, but TypeWrappers can be used to wrap classes you didn't create.
 * Inherit from `TypeWrapper<T>` where T is the type you wish to wrap.
 * Implement the `Serialize()` and `Deserialize()` methods as needed.
 * Make sure to write some unit tests!

### Making Custom Packets
 * Usually, the `NetworkInoke()` method within the network client can be used for Network objects, but if you want to you, you can create custom packets. 
 * Inherit from the `CustomPacket` class in `SocketNetworking.PacketSystem.Packets`
 * Ensure you have the attribute `PacketDefinition()`
 * Ensure you override the `Serialize()` and `Deserialize()` methods, you **must** call the `base.Serialize()` and `base.Deserialize()` methods. 
 * If you make any modifications to the data contained in the packet that you yourself did not define, (eg parent class flags, values, etc) you must do so before calling the `base.Serialize()` function.

## Network Objects
* Can be used to move logic out of your Custom network client implemntation, or to represent other users, objects, or whatever else you'll need
* Each object has an `NetworkID`, and `OwnerClientID` and various other properties. 
 * The `NetworkID` is a unique integer which is used to tell the object whats going on.
 * The `OwnerClientID` is the owner of the object. Note that the server may also own the object.

### Register Network Objects
