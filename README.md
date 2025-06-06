# SocketNetworking
 A Networking Library using Vairous Protocols and custom packets.

 The main parts you'll be using are Tcp/Udp/MixedNetworkClient, Tcp/Udp/MixedNetworkServer, INetworkObject, CustomPacket, and NetworkManager.

## Install Requirements
 * .NET Framework 4.8
 * Visual Studio 2022

## Import directions
 * Create a git submodule for this project within your project files.

**OR**

 * Build the .dll from the project and use it.

## Understanding the design
 * This is a complicated library, and you should understand how its meant to be used! The library is designed for Server/Client communication, so you should understand how to treat both sides of the connection. (Tip: If you are ever lost, `NetworkManager.WhereAmI` can help you!)
 * Servers are authoritive by default, meaning they have all the permission in the library. You can delegate permission to clients via various methods for the various functions in the library.
 * The `NetworkClient` class is only known to the local client, and the server (which knows all of them). So on the server, you can find any client you want.
 * Packets are sent and recieved on seperate threads. On the local client, one thread is used to send packets, and another to recieve and handle them. This creates an issue if your application does not support multithreading! The fix is simple, use the `NetworkClient.ManualPacketHandle` property. this will prevent the Client recieve thread from handling the packet for you, it will simply queue it. On the server, a thread pool is used, a single thread handles reading and writing (recieving and sending) packets for multiple clients. You can always increase the amount of threads you use, however this will result in the program eating resources.

## Features

### Security

#### SSL
 * Provide a valid certificate path in `NetworkServerConfig.CertificatePath` for the server to advertise.
 * SSL is done before encryption.
 * If SSL fails, the client will disconnect.
 * SSL Certificates should have the subject as the **connection address** of the server. (use a domain name if possible)
 * For example, if the client connected to `server.xyz`, the client will try to check `server.xyz` on the certificate. This also applies to raw IP addresses.
 * Only supported on TCP and Mixed Network Client types. UDP Clients are not supported. (The UDP Communication for the `MixedNetworkClient` is protected through encryption provided with the library, as keys are only sent over SSL connections)

#### Server-Client Encryption
 * All communication between the server and client is encrypted before the `NetworkClient.Ready` state is set to true.
 * You can make encryption optional or disable it with the `NetworkServerConfig.EncryptionMode` enum. The default is `Required`. `Request` will allow the client to request encryption, and `Disabled` will prevent encryption (Not recommended)
 * The encryption system generates unique public/private keys per client as well as unique symmetrical keys per client. Both servers and clients generate keys (for those who want end-to-end encryption).
 * Packet bodies are encrypted, but not the packet header. This contians very limited information (such as the packet type and destination object ID, as well as the sender and reciever, althought these can be changed before sending the packet.)
 * Both UDP and TCP traffic is encrypted.

#### Permissions/Client Access
 * You can control what clients can see what objects, and who can change values or call Networked methods.
 * See `INetworkObject` for details.
 * All actions are validated locally and then on the remote reciever.

### Communication/Ease of use

#### SyncVars, NetworkObjects and NetworkInvoke
 * All of these will be explained later, but in short hand, these allow you to write less netcode and more application code.
 * NetworkInvoke is a RPC like system to call methods across the network.
 * SyncVars allow you to synchronize value changes
 * NetworkObjects allow you to organize and spawn in objects as needed.

### Performance

#### Caching
 * All reflection targets are cached before use.
 * Strings which are known on both clients (type names, assembly names) are hashed to avoid sending long strings
 * All `INetworkObject` events are called in code (without reflection) to avoid delays
 * Packet reading is done in a way to avoid waiting for the stream unless there actually is data to read, so the method is only blocking when a packet can be read.

#### Threading
 * Clients use 2 threads for Netcode, one to read, one to write
 * Writiing is done via a queue on its own thread, so its non-blocking to you.
 * Reading is done via a seperate queue, and optionally can be handled manually by your application via `NetworkClient.ManualPacketHandle`.
 * Servers handle clients in a round robin thread configuration, where each thread handles a few clients, this can be changed in `NetworkServerConfig`.
 * Server threads are allocated to ensure all threads have a similar amount of clients, as to avoid useless threads.

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

#### Example Project Setup
![image](https://github.com/user-attachments/assets/6eb43c32-3c1a-4e57-800d-312b4623151f)

#### NetworkClient Setup
![image](https://github.com/user-attachments/assets/bada4602-1545-4e34-974b-46824cb4dc06)

#### NetworkServer Setup
![image](https://github.com/user-attachments/assets/8cfae43b-1f72-42a4-8e03-b1f7050a06c7)

#### ExampleLogger
![image](https://github.com/user-attachments/assets/d90c5bff-12b3-4fed-8cc4-b26fed11915b)

## Importing Custom Content
 * Assumming you marked your TypeWrappers with the TypeWrapperAttribute, Custom Packets with the PacketDefinitionAttrbute and NetworkObjects, you can call `NetworkManager.ImportAssmebly(Assembly)` to have the library recognize your new content.

### Making Custom Type Wrappers
 * Usually, you would use `IPacketSerializable` for types you created, but TypeWrappers can be used to wrap classes you didn't create.
 * Inherit from `TypeWrapper<T>` where T is the type you wish to wrap.
 * Implement the `Serialize()` and `Deserialize()` methods as needed.
 * Make sure to write some unit tests!

#### A TypeWrapper
![image](https://github.com/user-attachments/assets/306c98d2-9eb6-4e1e-8ae6-9000178ae979)

#### A IPacketSerializable type
![image](https://github.com/user-attachments/assets/c347f805-d5f3-40ab-be94-32a96e70636f)

### Making Custom Packets
 * Usually, the `NetworkInvoke()` method within the network client can be used for Network objects, but if you want to, you can create custom packets. 
 * Inherit from the `CustomPacket` class in `SocketNetworking.PacketSystem.Packets`
 * Ensure you have the attribute `PacketDefinition()`
 * Ensure you override the `Serialize()` and `Deserialize()` methods, you **must** call the `base.Serialize()` and `base.Deserialize()` methods. 
 * If you make any modifications to the data contained in the packet that you yourself did not define, (eg parent class flags, values, etc) you must do so before calling the `base.Serialize()` function.
 * Packet custom IDs are handled automatically by the library, Do not rely on them being the same each time.

#### A Custom Packet
![image](https://github.com/user-attachments/assets/ff11c149-cc19-4ddb-8961-0a081e31a2fa)

## Network Objects

### Making NetworkObjects
 * Create a class, and Inherit from `INetworkObject` or `NetworkObject` (interface vs class).
 * Register and add your object with the library with `NetworkManager.AddNetworkObject(INetworkObject)`. This should only be called on the server.
 * To spawn your object, use the `NetworkSpawn()` extension. (Class: `SocketNetworking.NetworkObjectExtensions`, it also has some goodies for setting Owners, Visibility, OwnershipModes, and NetworkIDs!)
 * And you are all done, your object is now registered and spawned.

#### Using Network Objects
 Network objects can have various properties and methods, you will need to know about `NetworkInvoke`, `PacketListener`s and `NetworkSyncVar`s

#### Network Avatars
 * Network Avatars are NetworkObjects which are owned by a certain client and visible to everyone.
 * All Network Avatars can be created from the `INetworkAvatar` interface. They also behave just like `INetworkObject`s. I strongly recommend using `NetworkAvatarBase` as the base class as it provides additional functions like public key broadcasting and auto destruction on client disconnect.
 * To specify and Avatar, You can specify its type in `NetworkServer.ClientAvatar` which accepts a `Type`.

#### Basic Network Avatar code (From the examples chat program)
![image](https://github.com/user-attachments/assets/cf9ea1b2-a5bf-4042-9316-c9300010cb32)

#### NetworkInvoke
 * A Network Invocation is a call to run a method on another object somewhere.
 * To define one, you will need to create a new instance method on either the `NetworkClient` or a Registered `INetworkObject`. These methods have 2 parts, the method itself, and the attribute. The method itself can have any return type (assuming you can serialize it) and must have either a `NetworkHandle` or `NetworkClient` as its first parameter, all additional parameters can be anything serializable.
 * You will now need the attribute `NetworkInvokableAttribute`. The first parameter is the direction, this is important. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only.
 * **WARNING** `NetworkInvoke<T>` is **thread blocking** do not call this on the packet reading thread as this will cause the application to lock up.

#### A NetworkInvoke method (above) and its caller (lower)
![image](https://github.com/user-attachments/assets/e0526e01-ebf1-4d9c-a216-911d34304cfa)

#### NetworkSyncVars
 * To create a `NetworkSyncVar<T>`, define a **field** in your `NetworkObject` with the type `NetworkSyncVar<>`, you can use any serializable type as the generic parameter.
 * Within the classes constructor, you can optionally assign a value to the `NetworkSyncVar`. If you do not, the value will be null until the object is registered.
 * When you want to update the value, set the `NetworkSyncVar<T>.Value` property, security modifiers apply.
 * You must either own the object, or be the server to make the change.
 * `NetworkSyncVar`s have their own permission setting, the `NetworkSyncVar<>.OwnershipMode`. This can differ from the `NetworkObject.OwnershipMode` to allow for publicly changing values.

#### A NetworkSyncVar being created and set.
![image](https://github.com/user-attachments/assets/65218d0f-d166-4846-9c06-2252df134fac)

#### PacketListener
 * A Packet Listener is a method which allows the capture of packets addressed to the `INetworkObject` or `NetworkClient` its on.
 * To create one, create a method of any security level and have the arguments as the Packet you want to capture and a `NetworkHandle`.
 * Now place an attribute, The `PacketListenerAttribute`. You will need to provide a few arguments. First, the type of the Packet you are listening for. (Hint, use `typeof()`.) Secondly, you will need to specify the direction, it works the same as the NetworkInvokableAttribute. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only.

#### A PacketListener
![image](https://github.com/user-attachments/assets/9ae43a66-cb65-4c04-9b76-50ca7e195d39)

