# Session Management for FishNet.

There is often a need to save the player's state when disconnecting from the server and accurately
restore them to the player when reconnecting. For example, you may want to save a player's health, inventory.
and other data so that the player can reconnect to that session and continue from the same state.

Currently, FishNet does not have a built-in feature that allows you to do this, so this asset may look
unfamiliar. But I've tried to keep it as simple as possible and make it look like it's a native feature.

But you can participate in testing and report bugs. You can also suggest ideas to improve the documentation and add new features to the asset.

If you have further questions, come find me as `ooonush` in the [FirstGearGames Discord](https://discord.gg/Ta9HgDh4Hj)!

## Support the Developer

Asset is completely free and created entirely by myself.
**If you want to support me, you can do so with [Donation Alerts](https://www.donationalerts.com/r/ooonush).**

## Player Identification.

In FishNet, when a player reconnects, a new NetworkConnection with a different ClientId is created for that player.
This means that the server has no way of knowing whether a new player is connecting or reconnecting a previously connected player.

Thus, the Session Manager uses the PlayerId string to identify players.
When connecting, a player must authenticate by passing his PlayerId to the server.
The server checks this PlayerId for correctness and matches it with the PlayerId of previously connected players.
If such a PlayerId is found, the player reconnects and the server transfers the previously owned NetworkObjects to the player.
Otherwise, the player is connected as a new player.

This asset already has an authenticator that generates a PlayerId for the player.
In order to use it, you must add a `BasicSessionAuthenticator` component to the scene and assign it to the
[Authenticator](https://fish-networking.gitbook.io/docs/manual/components/authenticator) field in the
[ServerManager](https://fish-networking.gitbook.io/docs/manual/components/managers/server-manager) component.

The `BasicSessionAuthenticator` may not suit you and you can implement your own custom authentication.
To do this you need to inherit the `SessionAuthenticator` abstract class.
The authentication happens in the same way as in the 
[Authenticator](https://fish-networking.gitbook.io/docs/manual/components/authenticator) that is in FishNet.
The main difference is that instead of the `OnAuthenticationResult` event, you have to call the `PassAuthentication` or `FailAuthentication` methods depending on whether the authentication has passed.
You can see an example use case in the `BasicSessionAuthenticator.cs` script.

### Unity Authentication Support

There is also support for `Unity Authentication` in this package.
To use it, you need to install [Unity Authentication](https://unity.com/products/authentication) package and use `UnitySessionAuthenticator` component.

## Components Setup

For the session manager to work, you must add several components:
1. `SessionAuthenticator` you want to use. Remember to assign it to the `Authenticator` field in the `ServerManager` component.
2. `ServerSessionManager` in the `ServerManager` GameObject.
3. `ClientSessionManager` in the `ClientManager` GameObject.

## SessionPlayer
Instead of `NetworkConnection`, the `SessionPlayer` class is used in the Session Manager.
Unlike `NetworkConnection`, `SessionPlayer` does not change when you reconnect.

In `SessionPlayer` you can access properties such as:
- string PlayerId // Available to server.
- int ClientPlayerId // Available to server and clients.
- bool IsLocalPlayer
- bool IsConnected
- NetworkConnection NetworkConnection
- ...

As you can see, the `PlayerId` string is only available on the server, which ensures security.
Clients cannot recognize the PlayerId of other players, and so that they can still distinguish between players, there is a ClientPlayerId.
This is a unique player identifier that is available to both server and clients.

## ServerSessionManager.

`ServerSessionManager`, oddly enough, is responsible for the server side of Session Management. This is similar to `ServerManager`.
You can get the `ServerSessionManager` by using `NetworkManager.GetServerSessionManager()`.

An important detail is that by default, `ServerSessionManager` does not store information about previously connected players.
That is, when reconnecting, players will connect as new players.

In order to change this, you must call the `StartSession()` method.
And when you no longer need to store previously connected players, you can call the `EndSession()` method.
You can also change the `IsSessionStarted` value in the inspector.

The OnRemotePlayerConnectionState event is available in this class. It is called when the player states are changed:
- **Connected** - The player has been connected first time. Is called after the player has been authenticated.
- **Reconnected** - The player has been reconnected to this session. Is called after the player has been authenticated.
- **PermanentlyDisconnected** - The player has been permanently disconnected and cannot reconnect to this session. Next time it will connect as a new player with the same PlayerId.
- **TemporarilyDisconnected** - The player has been temporarily disconnected and can reconnect to this session using their own PlayerId.

## ClientSessionManager.

The `ClientSessionManager` is responsible for the client side of Session Management. This is similar to `ClientManager`.
You can get the `ClientSessionManager` by using `NetworkManager.GetClientSessionManager()`.

In this class, besides `OnRemotePlayerConnectionState`, the `OnPlayerConnectionState` event is also available, which is called for the local player:
- **Connected** // The player has been connected first time. Is called after the player has been authenticated.
- **Reconnected** // The player has been reconnected to this session. Is called after the player has been authenticated.
- **Disconnected** // The player was disconnected. If a session was started on the server, he was disconnected temporarily and can reconnect. Otherwise he is disconnected permanently and next time he will connect as a new player with the same PlayerId.

## Getting SessionPlayer from NetworkConnection.

In some cases, you can access the `NetworkConnection` using the `NetworkConnection.GetSessionPlayer()` method.

This can be done inside `NetworkBehaviour` inside methods like `OnStartNetwork()`, `OnStartServer()` and so on.
Basically, whenever the `NetworkBehaviour` exists on the network.

You can also access `SessionPlayer` during calls to `ServerManager.OnAuthenticationResult`,
`ClientManager.OnAuthenticated`, and `ClientManager.OnClientConnectionState`, `ClientManager.OnRemoteConnectionState`
when ConnectionState is **Started**.

Example:
```csharp
private void Awake()
{
    InstanceFinder.ClientManager.OnRemoteConnectionState += OnRemoteConnectionState;
}

private void OnRemoteConnectionState(RemoteConnectionStateArgs args)
{
    NetworkConnection connection = InstanceFinder.ClientManager.Clients[args.ConnectionId];
    if (args.ConnectionState == RemoteConnectionState.Started)
    {
        // Getting SessionPlayer from NetworkConnection.
        SessionPlayer player = connection.GetSessionPlayer();
        Debug.Log("SessionPlayer Started : " + player.ClientPlayerId);
    }
    else
    {
        // SessionPlayer is not available when ConnectionState is not Started.
        // SessionPlayer player = connection.GetSessionPlayer();
    }
}
```

This may seem complicated and confusing, so I recommend not using callbacks from the
`ServerManager` and `ClientManager` to get `SessionPlayer`. It is better to use `ClientSessionManager` and `ServerSessionManager`.

## SessionPlayer's ownership of NetworkObjects.

Just like `NetworkConnection`, `SessionPlayer` can own NetworkObjects.
Objects owned by the player become the property of the server when temporarily disconnected.
When reconnected, they are transferred back to the player.

To give ownership of an object to a player, you must add a `NetworkSessionObject` component
and call the `GiveOwnershipPlayer(SessionPlayer newOwner)` method. And `RemoveOwnership()` to remove ownership:

```csharp
public class Foo : NetworkBehaviour
{
    public void CustomGiveOwnership(SessionPlayer sessionPlayer)
    {
        GetComponent<NetworkSessionObject>().GiveOwnershipPlayer(sessionPlayer);
    }

    public void CustomRemoveOwnership(SessionPlayer sessionPlayer)
    {
        RemoveOwnership();
    }
}
```

You can also call the `Spawn()` method to create an object in the player's ownership:
```csharp
[SerializeField] private NetworkSessionObject _playerPrefab;

private void Awake()
{
    // Getting the ServerSessionManager
    var serverSessionManager = InstanceFinder.GetInstance<ServerSessionManager>();
    serverSessionManager.OnRemotePlayerConnectionState += OnRemotePlayerConnectionState;
}

private void OnRemotePlayerConnectionState(SessionPlayer sessionPlayer, RemotePlayerConnectionStateArgs args)
{
    if (args.State == PlayerConnectionState.Connected)
    {
        NetworkSessionObject player = Instantiate(_playerPrefab);
        
        // Spawn player with session player ownership.
        InstanceFinder.ServerManager.Spawn(player, sessionPlayer);
    }
}
```

There is a `SessionPlayerSpawner` component in the asset that follows the `PlayerSpawner` logic from FishNet,
but creates an object in the possession of SessionPlayer instead of `NetworkConnection`.
