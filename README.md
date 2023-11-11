# SocketNetworking
 A Networking Library using TCP/IP and custom packets.

 The main parts you'll be using are NetworkClient, NetworkServer, INetworkObject, CustomPacket, and NetworkManager.

 ## NetworkClient
  The main Class for handling all communication. 

  * If you are on the Local side (Client), You will create an instance of this class and Call the `InitLocalClient()` method. After this, you'll need to call the `Connect()` method.

  * If you are on the server, you do not create the NetworkClient instance, the library will for you.

 ## NetworkServer
 Class for handling all Server related stuff.

  * You can specify the Bind IP, Listen Port, or the Type the NetworkClient should have.

  * When you are ready, call `StartServer()` NOTE: You should set the variables you wish to modify BEFORE starting the server.
