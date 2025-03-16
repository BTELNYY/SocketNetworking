# SocketNetworking.UnityEngine

This library is supposed to be used with *modding*, if you want to make multiplayer games, I suggest [Mirror](https://mirror-networking.com/).

## Getting started

1. Get yourself a unity game which **is not using IL2CPP**. You can tell if a game is using this is there is a `GameAssembly.dll` within the same folder as the `GameName.exe` and `GameName_Data` folder.
2. Get [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html)
3. Install and create a project from the BepInEx templates.
4. Load all needed assemblies using `Assembly.LoadFile(string)` or `Assembly.Load(string)` methods. (You will need to load `SocketNetworking.dll`, `SocketNetworking.UnityEngine.dll` and `SocketNetworking.Modding.dll`.
5. Call `UnityNetworkManager.Init()`. <- this should be done before you do any networking code, this method will also use [HarmonyX](https://github.com/BepInEx/HarmonyX) to patch `Transform`s, `Animators`, and anything else.
6. Ensure you have registered your `NetworkObjectSpawnerDelegate` with the `NetworkManager` which targets your specific network prefabs.
7. Ensure you have patched any methods/classes/properties/fields as needed.
8. Create a client or server, do networking stuff

## Components

All components should be a part of the `GameObject` which represents a prefab. You could assign them before the object is returned in the `NetworkObjectSpawnerDelegate` or have them modified on startup.

### NetworkIdentity
* The core identity of a networked object. All objects must have one.
* Stores the ID, owner, etc.
* Can be spawned. (results in replication of object, if configured.)

### NetworkTransform
* The position of a `NetworkIdentity` in space.
* Cannot be spawned, use the `NetworkIdentity` instead.

### NetworkAnimator
* Animates the `NetworkIdentity`
* Cannot be spawned, use the `NetworkIdentity` instead.

## Drawbacks

* No duplicates, a single `NetworkIdentity` cannot have multiple `NetworkComponents`s of the same time as the library will find the first one when seeking methods as a part of `NetworkInvoke` or `PacketListener`.
* No local desync. if an object has a `NetworkComponent` which overrides a unity class, such as `NetworkTransform` or `NetworkAnimator`, you cannot modify the patched properties without triggering the network security checks, which may fail, resulting in your code not doing anything.
* Risky parenting of transforms. The library will attempt to find the parent of a transform when spawning a `NetworkIdentity` but may not find it. It is recommended you instead localize where a specific object should exist and move it there upon spawn. ("RoomContainer" -> Parent of all rooms)
