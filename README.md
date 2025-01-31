# SocketNetworking
 A Networking Library using Vairous Protocols and custom packets.

 The main parts you'll be using are Tcp/Udp/MixedNetworkClient, Tcp/Udp/MixedNetworkServer, INetworkObject, CustomPacket, and NetworkManager.

## Install Requirements
 * .NET Framework 4.8

## Understanding the design
 * This is a complicated library, and you should understand how its meant to be used! The library is designed for Server/Client communication, so you should understand how to treat both sides of the connection. (Tip: If you are ever lost, `NetworkManager.WhereAmI` can help you!)
 * Servers are authoritive by default, meaning they have all the permission in the library. You can delegate permission to clients via various methods for the various functions in the library.
 * The `NetworkClient` class is only known to the local client, and the server (which knows all of them). So on the server, you can find any client you want do whatever.
 * Packets are sent and recieved on seperate threads. On the local client, one thread is used to send packets, and another to recieve and handle them. This creates an issue if your application does not support multithreading! The fix is simple, use the `NetworkClient.ManualPacketHandle` property. this will prevent the Client recieve thread from handling the packet for you, it will simply queue it. On the server, a thread pool is used, a single thread handles reading and writing (recieving and sending) packets for multiple clients. You can always increase the amount of threads you use, however this will result in the program eating resources.

## Getting Started
 * The best project setup is 3 Projects: The Client project, the shared/library project, and the server project.
 * The client should handle your client code, UI, etc.
 * The shared project should handle all the stuff you want to be the same for the server and client, for exmaple, custom `NetowrkClient` types, Custom Packets, Custom Network Objects, etc.
 * The server project should handle all your server stuff, for example databases, buisness logic, etc.
 * For unity mods/projects, you won't have several projects like this. You will need to find a way make all your code work together in one project. See the Unity section for more details.
 * OK, you have created your three projects, what now?
  * In all your projects with executing code (Client and Server) subscribe to `Log.OnLog`, this will provide useful debugging information. Then, Use `NetworkManager.ImportAssembly(yourSharedAssmeblyHere)` to tell the library to add new packets, etc. (Can't find the assmebly? Make a method within the Shared project and call `Assembly.GetExecutingAssmebly()` to get the assmebly)
  * In your Client, you will now need to create a new client object. If you have made a subclass, use that. However otherwise, use the `TcpNetworkClient` or `MixedNetworkClient`. (I do not suggest creating a `UdpNetworkClient` due to no protections existing for UDP traffic, it may not work at all, or very poorly.) Now, Call `NetworkClient.InitLocalClient()`. This object is now ready to be used, You can call `NetworkClient.Connect(string, int, string)` to connect to a remote resource.
  * In your Server, you will need to create a new server object. Servers are singletons, you cannot have more than one at a time. Match the protocol you chose on the client to the server, so TCP = TCP, Mixed = Mixed, etc. Now that you have your object, feel free to load in any configuration you'd like into `NetworkServer.Config`. After you are all done, Call `NetworkServer.StartServer()` to allow the server to listen for clients.

## Importing Custom Content
 * Assumming you marked your TypeWrappers with the TypeWrapperAttribute, Custom Packets with the PacketDefinitionAttrbute and NetworkObjects, you can call `NetworkManager.ImportAssmebly(Assembly)` to have the library recognize your new content.

### Making Custom Type Wrappers
 * Usually, you would use `IPacketSerializable` for types you created, but TypeWrappers can be used to wrap classes you didn't create.
 * Inherit from `TypeWrapper<T>` where T is the type you wish to wrap.
 * Implement the `Serialize()` and `Deserialize()` methods as needed.
 * Make sure to write some unit tests!

### Making Custom Packets
 * Usually, the `NetworkInoke()` method within the network client can be used for Network objects, but if you want to, you can create custom packets. 
 * Inherit from the `CustomPacket` class in `SocketNetworking.PacketSystem.Packets`
 * Ensure you have the attribute `PacketDefinition()`
 * Ensure you override the `Serialize()` and `Deserialize()` methods, you **must** call the `base.Serialize()` and `base.Deserialize()` methods. 
 * If you make any modifications to the data contained in the packet that you yourself did not define, (eg parent class flags, values, etc) you must do so before calling the `base.Serialize()` function.
 * Packet custom IDs are handled automatically by the library, Do not rely on them being the same each time.

 ### Making NetworkObjects
  * Create a class, and Inherit from `INetworkObject` or `NetworkObject` (interface vs class).
  * Register and add your object with the library with `NetworkManager.AddNetworkObject(INetworkObject)`. This should only be called on the server.
  * To spawn your object, use the `NetworkSpawn()` extension. (Class: `SocketNetworking.NetworkObjectExtensions`, it also has some goodies for setting Owners, Visibility, OwnershipModes, and NetworkIDs!)
  * And you are all done, your object is now registered and spawned.

## NetworkInvoke and PacketListener

### NetworkInvoke
 * A Network Invocation is a call to run a method on another object somewhere.
 * To define one, you will need to create a new instance method on either the `NetworkClient` or a Registered `INetworkObject`. These methods have 2 parts, the method itself, and the attribute. The method itself can have any return type (assuming you can serialize it) and must have either a `NetworkHandle` or `NetworkClient` as its first parameter, all additional parameters can be anything serializable.
 * You will now need the attribute `NetworkInvokableAttribute`. The first parameter is the direction, this is important. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only.

### PacketListener
 * A Packet Listener is a method which allows the capture of packets addressed to the `INetworkObject` or `NetworkClient` its on.
 * To create one, create a method of any security level and have the arguments as the Packet you want to capture and a `NetworkHandle`.
 * Now place an attribute, The `PacketListenerAttribute`. You will need to provide a few arguments. First, the type of the Packet you are listening for. (Hint, use `typeof()`.) Secondly, you will need to specify the direction, it works the same as the NetworkInvokableAttribute. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only.