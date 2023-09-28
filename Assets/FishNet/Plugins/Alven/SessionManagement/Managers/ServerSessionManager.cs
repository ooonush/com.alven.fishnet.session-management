using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Transporting;
using UnityEngine;

namespace FishNet.Alven.SessionManagement
{
    [DisallowMultipleComponent]
    [AddComponentMenu("FishNet/Manager/ServerSessionManager")]
    [DefaultExecutionOrder(-20000)]
    [RequireComponent(typeof(ServerManager))]
    public sealed class ServerSessionManager : MonoBehaviour
    {
        public NetworkManager NetworkManager => _serverManager.NetworkManager;
        public IReadOnlyDictionary<int, SessionPlayer> Players => _playersByClientIds;
        public bool IsSessionStarted;

        public event Action OnSessionStarted;
        public event Action<SessionPlayer, RemotePlayerConnectionStateArgs> OnRemotePlayerConnectionState;

        private readonly Dictionary<string, SessionPlayer> _players = new Dictionary<string, SessionPlayer>();
        private readonly Dictionary<int, SessionPlayer> _playersByClientIds = new Dictionary<int, SessionPlayer>();
        private readonly Dictionary<int, SessionPlayer> _playersByConnectionIds = new Dictionary<int, SessionPlayer>();
        private ServerManager _serverManager;

        private int _nextClientPlayerId;
        internal bool ShareIds;

        private void Awake()
        {
            _serverManager = GetComponent<ServerManager>();
            
            if (!NetworkManager) return;
            Type type = _serverManager.GetType();
            FieldInfo fieldInfo = type.GetField("_shareIds", BindingFlags.NonPublic | BindingFlags.Instance);
            ShareIds = (bool)fieldInfo!.GetValue(_serverManager);
            NetworkManager.RegisterInstance(this);
            
            _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _serverManager.OnServerConnectionState += OnServerConnectionState;
        }

        private void OnDestroy()
        {
            if (!NetworkManager) return;
            NetworkManager.UnregisterInstance<ServerSessionManager>();
            
            _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            _serverManager.OnServerConnectionState -= OnServerConnectionState;
            
            Reset();
        }

        public void StartSession()
        {
            IsSessionStarted = true;
            OnSessionStarted?.Invoke();
        }

        public void EndSession()
        {
            IsSessionStarted = false;

            foreach (SessionPlayer player in Players.Values.ToArray())
            {
                if (!player.IsConnected)
                {
                    RemovePlayer(player);
                }
            }
        }

        internal SessionPlayer GetPlayer(NetworkConnection connection)
        {
            return _playersByConnectionIds[connection.ClientId];
        }

        internal bool SetupPlayerConnection(SessionAuthenticator authenticator, string playerId, NetworkConnection connection)
        {
            if (!_players.TryGetValue(playerId, out SessionPlayer player))
            {
                player = new SessionPlayer(NetworkManager, _nextClientPlayerId, connection, playerId);
                _nextClientPlayerId++;
                
                AddPlayer(player);
                BroadcastPlayerConnectionChange(player, PlayerConnectionState.Connected, false);
                BroadcastPlayerConnected(player, false);
                connection.OnLoadedStartScenes += OnLoadedStartScenes;
                authenticator.InvokeAuthenticationResult(connection, true);
                InvokeOnRemotePlayerConnectionState(player, PlayerConnectionState.Connected);
                return true;
            }

            if (!player.IsConnected)
            {
                ReconnectPlayer(player, connection);
                BroadcastPlayerConnectionChange(player, PlayerConnectionState.Reconnected, false);
                BroadcastPlayerConnected(player, true);
                connection.OnLoadedStartScenes += OnLoadedStartScenes;
                authenticator.InvokeAuthenticationResult(connection, true);
                InvokeOnRemotePlayerConnectionState(player, PlayerConnectionState.Reconnected);
                return true;
            }

            NetworkManager.LogWarning("Player with id " + playerId + " is already connected. Authentication failed.");
            authenticator.InvokeAuthenticationResult(connection, false);
            return false;
        }

        private void OnLoadedStartScenes(NetworkConnection connection, bool asServer)
        {
            if (!asServer) return;

            connection.OnLoadedStartScenes -= OnLoadedStartScenes;
        }

        private void BroadcastPlayerConnected(SessionPlayer player, bool isReconnected)
        {
            var message = new PlayerConnectedBroadcast(player, isReconnected);
            _serverManager.Broadcast(player.NetworkConnection, message, false);
        }

        private void ReconnectPlayer(SessionPlayer player, NetworkConnection connection)
        {
            _playersByConnectionIds.Remove(player.ConnectionId);
            _playersByConnectionIds.Add(connection.ClientId, player);
            player.SetupReconnection(connection);
        }

        private void DisconnectPlayer(SessionPlayer player, bool permanently)
        {
            if (permanently)
            {
                InvokeOnRemotePlayerConnectionState(player, PlayerConnectionState.PermanentlyDisconnected);
                BroadcastPlayerConnectionChange(player, PlayerConnectionState.PermanentlyDisconnected);
                RemovePlayer(player);
                player.Dispose();
            }
            else
            {
                InvokeOnRemotePlayerConnectionState(player, PlayerConnectionState.TemporarilyDisconnected);
                BroadcastPlayerConnectionChange(player, PlayerConnectionState.TemporarilyDisconnected);
                // player.DisconnectTemporarily();
            }
        }

        private void InvokeOnRemotePlayerConnectionState(SessionPlayer player, PlayerConnectionState state)
        {
            var args = new RemotePlayerConnectionStateArgs(state, player.ClientPlayerId);
            OnRemotePlayerConnectionState?.Invoke(player, args);
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Reset();
            }
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (connection.Authenticated && args.ConnectionState == RemoteConnectionState.Stopped)
            {
                connection.OnLoadedStartScenes -= OnLoadedStartScenes;
                DisconnectPlayer(GetPlayer(connection), !IsSessionStarted);
            }
        }

        private void AddPlayer(SessionPlayer player)
        {
            _players.Add(player.PlayerId, player);
            _playersByClientIds.Add(player.ClientPlayerId, player);
            _playersByConnectionIds[player.ConnectionId] = player;
        }

        private void RemovePlayer(SessionPlayer player)
        {
            _players.Remove(player.PlayerId);
            _playersByClientIds.Remove(player.ClientPlayerId);
            _playersByConnectionIds.Remove(player.ConnectionId);
        }

        private void Reset()
        {
            foreach (SessionPlayer player in _players.Values.ToArray())
            {
                RemovePlayer(player);
                player.Dispose();
            }

            _nextClientPlayerId = 1;
            IsSessionStarted = false;
        }

        /// <summary>
        /// Sends a player connection state change to owner and other clients if applicable.
        /// </summary>
        private void BroadcastPlayerConnectionChange(SessionPlayer player, PlayerConnectionState state, bool requireAuthenticated = true)
        {
            NetworkConnection conn = player.NetworkConnection;
            bool connected = state == PlayerConnectionState.Connected || state == PlayerConnectionState.Reconnected;
            if (ShareIds)
            {
                var changeMsg = new PlayerConnectionChangeBroadcast(player.ClientPlayerId, conn.ClientId, state);
                foreach (NetworkConnection c in _serverManager.Clients.Values)
                {
                    if (c.Authenticated)
                    {
                        _serverManager.Broadcast(c, changeMsg, requireAuthenticated);
                    }
                }

                if (connected)
                {
                    int[] clientPlayerIds = _playersByClientIds.Keys.ToArray();
                    int[] connectionIds = _players.Values.Select(p => p.NetworkConnection.ClientId).ToArray();
                    var allMsg = new ConnectedPlayersBroadcast(clientPlayerIds, connectionIds);
                    conn.Broadcast(allMsg, requireAuthenticated);
                }
            }
            else if (connected)
            {
                /* Send broadcast only to the client which just disconnected.
                 * Only send if connecting. If the client is disconnected there's no reason
                 * to send them a disconnect msg. */
                var changeMsg = new PlayerConnectionChangeBroadcast(player.ClientPlayerId, conn.ClientId, state);
                _serverManager.Broadcast(conn, changeMsg, requireAuthenticated);
            }
        }
    }
}