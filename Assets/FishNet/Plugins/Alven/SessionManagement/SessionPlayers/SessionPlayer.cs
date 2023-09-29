using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;

namespace FishNet.Alven.SessionManagement
{
    public sealed class SessionPlayer
    {
        internal const int UNSET_CLIENTID = -1;
        public static SessionPlayer Empty { get; private set; } = new SessionPlayer();

        /// <summary>
        /// Objects owned by this Session Player. Available to this connection and server.
        /// </summary>
        public IReadOnlyCollection<NetworkSessionObject> Objects => _objects;
        public NetworkManager NetworkManager { get; private set; }
        /// <summary>
        /// Available to server and clients.
        /// </summary>
        public int ClientPlayerId { get; private set; } = UNSET_CLIENTID;
        /// <summary>
        /// Available to server.
        /// </summary>
        public string PlayerId { get; private set; }
        public bool IsLocalPlayer => ClientSessionManager.Player == this;
        public bool IsValid => ClientPlayerId != UNSET_CLIENTID;
        public bool IsConnected => NetworkConnection != null && NetworkConnection.IsActive;
        public NetworkConnection NetworkConnection
        {
            get
            {
                if (_connection != null && _connection.ClientId == ConnectionId)
                {
                    return _connection;
                }

                if (_asServer)
                {
                    if (NetworkManager.ServerManager.Clients.TryGetValue(ConnectionId, out _connection))
                    {
                        return _connection;
                    }
                }
                else
                {
                    // This is done to get rid of some of the problems of the initialization order of NetworkConnection and SessionPlayer.
                    if (NetworkManager.ClientManager.Connection.ClientId == ConnectionId)
                    {
                        _connection = NetworkManager.ClientManager.Connection;
                        return _connection;
                    }

                    if (NetworkManager.ClientManager.Clients.TryGetValue(ConnectionId, out _connection))
                    {
                        return _connection;
                    }
                }

                return NetworkManager.EmptyConnection;
            }
            private set
            {
                _connection = value;
                ConnectionId = value == null ? NetworkConnection.UNSET_CLIENTID_VALUE : _connection.ClientId;
            }
        }

        /// <summary>
        /// Invokes when an NetworkPlayerObject is added for this player. Available to this connection and server.
        /// </summary>
        public event Action<NetworkSessionObject> OnObjectAdded;
        /// <summary>
        /// Invokes when an NetworkPlayerObject is removed for this player. Available to this connection and server.
        /// </summary>
        public event Action<NetworkSessionObject> OnObjectRemoved;

        internal bool IsConnectedFirstTime { get; set; } = true;
        internal int ConnectionId { get; private set; } = NetworkConnection.UNSET_CLIENTID_VALUE;

        private NetworkConnection _connection;
        private readonly HashSet<NetworkSessionObject> _objects = new HashSet<NetworkSessionObject>();
        private readonly bool _asServer;
        private ClientSessionManager ClientSessionManager =>
            NetworkManager == null ? null : NetworkManager.GetClientSessionManager();

        internal SessionPlayer(NetworkManager networkManager, int clientPlayerId, int connectionId, bool asServer = false)
        {
            NetworkManager = networkManager;
            ClientPlayerId = clientPlayerId;
            ConnectionId = connectionId;
            _asServer = asServer;
        }

        internal SessionPlayer(NetworkManager networkManager, int clientPlayerId, NetworkConnection connection, bool asServer = false)
        {
            NetworkManager = networkManager;
            ClientPlayerId = clientPlayerId;
            NetworkConnection = connection;
            _asServer = asServer;
        }

        internal SessionPlayer(NetworkManager networkManager, int clientPlayerId, NetworkConnection connection, string playerId, bool asServer = true)
        {
            NetworkManager = networkManager;
            ClientPlayerId = clientPlayerId;
            NetworkConnection = connection;
            PlayerId = playerId;
            _asServer = asServer;
        }

        public SessionPlayer()
        {
            
        }

        internal void Dispose()
        {
            NetworkManager = null;
            ConnectionId = NetworkConnection.UNSET_CLIENTID_VALUE;
            PlayerId = null;
            ClientPlayerId = UNSET_CLIENTID;
            _objects.Clear();
            _connection = null;
        }

        internal void SetupReconnection(NetworkConnection connection)
        {
            IsConnectedFirstTime = false;
            NetworkConnection = connection;
        }

        internal void SetupReconnection(int connectionId)
        {
            IsConnectedFirstTime = false;
            ConnectionId = connectionId;
        }

        internal void AddObject(NetworkSessionObject networkObject)
        {
            if (!IsValid) return;

            if (_objects.Add(networkObject))
            {
                OnObjectAdded?.Invoke(networkObject);
            }
        }

        internal void RemoveObject(NetworkSessionObject networkObject)
        {
            if (!IsValid)
            {
                _objects.Clear();
                return;
            }

            if (_objects.Remove(networkObject))
            {
                OnObjectRemoved?.Invoke(networkObject);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SessionPlayer other)
                return Equals(other);
            return false;
        }

        public bool Equals(SessionPlayer other)
        {
            if (other is null) return false;
            //If either is -1 Id.
            if (ClientPlayerId == UNSET_CLIENTID || other.ClientPlayerId == UNSET_CLIENTID)
                return false;
            //Same object.
            if (ReferenceEquals(this, other)) return true;

            return ClientPlayerId == other.ClientPlayerId;
        }

        public override int GetHashCode() => ClientPlayerId;

        public static bool operator ==(SessionPlayer a, SessionPlayer b)
        {
            if (a is null && b is null)
                return true;
            if (a is null)
                return false;

            return !(b == null) && b.Equals(a);
        }

        public static bool operator !=(SessionPlayer a, SessionPlayer b) => !(a == b);
    }
}