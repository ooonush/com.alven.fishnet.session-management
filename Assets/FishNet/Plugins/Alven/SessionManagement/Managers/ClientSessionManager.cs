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
    public sealed class ClientSessionManager : MonoBehaviour
    {
        [SerializeField] private ClientManager _clientManager;

        public SessionPlayer Player { get; private set; } = SessionPlayer.Empty;
        public IReadOnlyDictionary<int, SessionPlayer> Players => _players;
        public NetworkManager NetworkManager => _clientManager.NetworkManager;

        // public event Action<RemotePlayerStateArgs> OnPlayerConnectionState;

        private readonly Dictionary<int, SessionPlayer> _players = new Dictionary<int, SessionPlayer>();
        private readonly Dictionary<int, SessionPlayer> _playersByConnectionIds = new Dictionary<int, SessionPlayer>();

        private void Awake()
        {
            NetworkManager.RegisterInstance(this);
            
            _clientManager.OnClientConnectionState += OnClientConnectionState;
            _clientManager.OnRemoteConnectionState += OnRemoteConnectionState;
            
            _clientManager.RegisterBroadcast<ConnectedPlayersBroadcast>(OnConnectedPlayersBroadcast);
            _clientManager.RegisterBroadcast<PlayerAuthenticatedBroadcast>(OnPlayerAuthenticatedBroadcast);
            _clientManager.RegisterBroadcast<PlayerConnectionChangeBroadcast>(OnPlayerConnectionBroadcast);
        }

        private void OnDestroy()
        {
            NetworkManager.UnregisterInstance<ClientSessionManager>();
            
            _clientManager.OnClientConnectionState -= OnClientConnectionState;
            _clientManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            
            _clientManager.UnregisterBroadcast<ConnectedPlayersBroadcast>(OnConnectedPlayersBroadcast);
            _clientManager.UnregisterBroadcast<PlayerAuthenticatedBroadcast>(OnPlayerAuthenticatedBroadcast);
            _clientManager.UnregisterBroadcast<PlayerConnectionChangeBroadcast>(OnPlayerConnectionBroadcast);
            
            Reset();
        }

        /// <summary>
        /// Called before ClientManager.OnAuthenticated.
        /// </summary>
        private void OnPlayerAuthenticatedBroadcast(PlayerAuthenticatedBroadcast broadcast)
        {
            Player = broadcast.Player;
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Reset();
            }
        }

        private void Reset() => ClearPlayers();

        private void OnConnectedPlayersBroadcast(ConnectedPlayersBroadcast broadcast)
        {
            ClearPlayers();

            for (var i = 0; i < broadcast.ClientPlayerIds.Length; i++)
            {
                int clientPlayerId = broadcast.ClientPlayerIds[i];
                int connectionId = broadcast.ConnectionIds[i];

                AddPlayer(new SessionPlayer(NetworkManager, clientPlayerId, connectionId, false));
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

        /// <summary>
        /// Called before OnRemoteConnectionState.
        /// </summary>
        private void OnPlayerConnectionBroadcast(PlayerConnectionChangeBroadcast args)
        {
            int clientPlayerId = args.ClientPlayerId;
            Debug.Log(args.State);
            PlayerConnectionState state = args.State;
            switch (state)
            {
                case PlayerConnectionState.Connected:
                    AddPlayer(new SessionPlayer(NetworkManager, clientPlayerId, args.ConnectionId, false));
                    // InvokeOnPlayerConnectionState will be called from OnRemoteConnectionState.
                    break;
                case PlayerConnectionState.Reconnected:
                    ReconnectPlayer(_playersByConnectionIds[clientPlayerId], args.ConnectionId);
                    // InvokeOnPlayerConnectionState will be called from OnRemoteConnectionState.
                    break;
                case PlayerConnectionState.Disconnected:
                    InvokeOnPlayerConnectionState(state, clientPlayerId);
                    break;
                case PlayerConnectionState.Leaved:
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

        /// <summary>
        /// Called after OnPlayerConnectionBroadcast.
        /// </summary>
        private void OnRemoteConnectionState(RemoteConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case RemoteConnectionState.Started:
                    SessionPlayer player = _players.Values.First(p => p.NetworkConnection.ClientId == args.ConnectionId);
                    PlayerConnectionState state = player.IsConnectedFirstTime
                        ? PlayerConnectionState.Connected
                        : PlayerConnectionState.Reconnected;
                    InvokeOnPlayerConnectionState(state, player.ClientPlayerId);
                    break;
                case RemoteConnectionState.Stopped:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // TODO
        private void InvokeOnPlayerConnectionState(PlayerConnectionState state, int clientPlayerId)
        {
            var rcs = new RemotePlayerStateArgs(state, clientPlayerId);
            // OnPlayerConnectionState?.Invoke(rcs);
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
        
        public SessionPlayer GetPlayer(NetworkConnection connection) => _playersByConnectionIds[connection.ClientId];
    }
}