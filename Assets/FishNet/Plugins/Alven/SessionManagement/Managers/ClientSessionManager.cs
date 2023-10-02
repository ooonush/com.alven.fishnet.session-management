using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Client;
using FishNet.Transporting;
using UnityEngine;

namespace FishNet.Alven.SessionManagement
{
    [DisallowMultipleComponent]
    [AddComponentMenu("FishNet/Manager/ClientSessionManager")]
    [DefaultExecutionOrder(-20000)]
    [RequireComponent(typeof(ClientManager))]
    public sealed class ClientSessionManager : MonoBehaviour
    {
        /// <summary>
        /// The local Session Player.
        /// </summary>
        public SessionPlayer Player { get; private set; } = SessionPlayer.Empty;
        /// <summary>
        /// Connected Session Players by ClientPlayerIds.
        /// </summary>
        public IReadOnlyDictionary<int, SessionPlayer> Players => _players;
        /// <summary>
        /// NetworkManager for client.
        /// </summary>
        public NetworkManager NetworkManager => _clientManager.NetworkManager;
        /// <summary>
        /// Called after the remote Session Player connection state changes.
        /// </summary>
        public event Action<RemotePlayerConnectionStateArgs> OnRemotePlayerConnectionState;
        /// <summary>
        /// Called after the local Session Player connection state changes.
        /// </summary>
        public event Action<PlayerConnectionStateArgs> OnPlayerConnectionState;

        private readonly Dictionary<int, SessionPlayer> _players = new Dictionary<int, SessionPlayer>();
        private readonly Dictionary<int, SessionPlayer> _playersByConnectionIds = new Dictionary<int, SessionPlayer>();
        private ClientManager _clientManager;

        private void Awake()
        {
            _clientManager = GetComponent<ClientManager>();
            if (!NetworkManager) return;
            NetworkManager.RegisterInstance(this);
            _clientManager.OnClientConnectionState += OnClientConnectionState;
            _clientManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _clientManager.OnAuthenticated += OnAuthenticated;
            
            _clientManager.RegisterBroadcast<PlayerConnectedBroadcast>(OnPlayerConnectedBroadcast);
            _clientManager.RegisterBroadcast<ConnectedPlayersBroadcast>(OnConnectedPlayersBroadcast);
            _clientManager.RegisterBroadcast<PlayerConnectionChangeBroadcast>(OnPlayerConnectionBroadcast);
        }

        private void OnRemoteConnectionState(RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                SessionPlayer player = _playersByConnectionIds[args.ConnectionId];

                if (player.IsConnectedFirstTime)
                {
                    InvokeOnPlayerConnectionState(PlayerConnectionState.Connected, player.ClientPlayerId);
                }
                else
                {
                    InvokeOnPlayerConnectionState(PlayerConnectionState.Reconnected, player.ClientPlayerId);
                }
            }
        }

        private void OnDestroy()
        {
            if (!NetworkManager) return;
            NetworkManager.UnregisterInstance<ClientSessionManager>();

            _clientManager.OnClientConnectionState -= OnClientConnectionState;
            _clientManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            _clientManager.OnAuthenticated -= OnAuthenticated;

            _clientManager.UnregisterBroadcast<PlayerConnectedBroadcast>(OnPlayerConnectedBroadcast);
            _clientManager.UnregisterBroadcast<ConnectedPlayersBroadcast>(OnConnectedPlayersBroadcast);
            _clientManager.UnregisterBroadcast<PlayerConnectionChangeBroadcast>(OnPlayerConnectionBroadcast);
    
            Reset();
        }

        internal SessionPlayer GetPlayer(NetworkConnection connection) => _playersByConnectionIds[connection.ClientId];

        /// <summary>
        /// Called before ClientManager.OnAuthenticated.
        /// </summary>
        private void OnPlayerConnectedBroadcast(PlayerConnectedBroadcast broadcast)
        {
            Player = broadcast.Player;
            Player.IsConnectedFirstTime = !broadcast.IsReconnected;
        }

        private void OnAuthenticated()
        {
            if (Player.IsConnectedFirstTime)
            {
                InvokeOnPlayerConnectionState(LocalPlayerConnectionState.Connected);
            }
            else
            {
                InvokeOnPlayerConnectionState(LocalPlayerConnectionState.Reconnected);
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                InvokeOnPlayerConnectionState(LocalPlayerConnectionState.Disconnected);
                Reset();
            }
        }

        private void InvokeOnPlayerConnectionState(LocalPlayerConnectionState state)
        {
            OnPlayerConnectionState?.Invoke(new PlayerConnectionStateArgs(state));
        }

        private void Reset()
        {
            Player = SessionPlayer.Empty;
            ClearPlayers();
        }

        private void OnConnectedPlayersBroadcast(ConnectedPlayersBroadcast broadcast)
        {
            ClearPlayers();

            for (var i = 0; i < broadcast.ClientPlayerIds.Length; i++)
            {
                int clientPlayerId = broadcast.ClientPlayerIds[i];
                int connectionId = broadcast.ConnectionIds[i];

                AddPlayer(new SessionPlayer(NetworkManager, clientPlayerId, connectionId));
            }
        }

        private void ClearPlayers()
        {
            foreach (SessionPlayer player in _players.Values.ToArray())
            {
                RemovePlayer(player);
                player.Dispose();
            }
        }

        private void OnPlayerConnectionBroadcast(PlayerConnectionChangeBroadcast args)
        {
            int clientPlayerId = args.ClientPlayerId;
            PlayerConnectionState state = args.State;
            switch (state)
            {
                case PlayerConnectionState.Connected:
                    AddPlayer(new SessionPlayer(NetworkManager, clientPlayerId, args.ConnectionId));
                    // InvokeOnPlayerConnectionState will be called from OnRemoteConnectionState.
                    break;
                case PlayerConnectionState.Reconnected:
                    ReconnectPlayer(_players[clientPlayerId], args.ConnectionId);
                    // InvokeOnPlayerConnectionState will be called from OnRemoteConnectionState.
                    break;
                case PlayerConnectionState.TemporarilyDisconnected:
                    InvokeOnPlayerConnectionState(state, clientPlayerId);
                    break;
                case PlayerConnectionState.PermanentlyDisconnected:
                    if (Players.TryGetValue(clientPlayerId, out SessionPlayer p))
                    {
                        RemovePlayer(p);
                        p.Dispose();
                    }
                    InvokeOnPlayerConnectionState(state, clientPlayerId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InvokeOnPlayerConnectionState(PlayerConnectionState state, int clientPlayerId)
        {
            var rcs = new RemotePlayerConnectionStateArgs(state, clientPlayerId);
            OnRemotePlayerConnectionState?.Invoke(rcs);
        }

        private void AddPlayer(SessionPlayer player)
        {
            _players[player.ClientPlayerId] = player;
            _playersByConnectionIds[player.ConnectionId] = player;
        }

        private void RemovePlayer(SessionPlayer player)
        {
            _players.Remove(player.ClientPlayerId);
            _playersByConnectionIds.Remove(player.ConnectionId);
        }

        private void ReconnectPlayer(SessionPlayer player, int connectionId)
        {
            player.SetupReconnection(connectionId);
            _playersByConnectionIds.Remove(player.ConnectionId);
            _playersByConnectionIds[connectionId] = player;
        }
    }
}