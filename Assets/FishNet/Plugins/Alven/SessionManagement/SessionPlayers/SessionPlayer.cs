using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;

namespace FishNet.Alven.SessionManagement
{
    public sealed class SessionPlayer
    {
        public const int UNSET_CLIENTID = -1;
        public static SessionPlayer Empty { get; private set; } = new SessionPlayer();

        public readonly HashSet<NetworkPlayerObject> Objects = new HashSet<NetworkPlayerObject>();
        public NetworkManager NetworkManager { get; private set; }
        public int ClientPlayerId { get; private set; } = UNSET_CLIENTID;
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

        public event Action<NetworkPlayerObject> OnObjectAdded;
        public event Action<NetworkPlayerObject> OnObjectRemoved;

        internal bool IsConnectedFirstTime { get; set; } = true;
        internal int ConnectionId { get; private set; } = NetworkConnection.UNSET_CLIENTID_VALUE;

        private NetworkConnection _connection;
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
            Objects.Clear();
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

        internal void AddObject(NetworkPlayerObject networkObject)
        {
            if (!IsValid) return;

            if (Objects.Add(networkObject))
            {
                OnObjectAdded?.Invoke(networkObject);
            }
        }

        internal void RemoveObject(NetworkPlayerObject networkObject)
        {
            if (!IsValid)
            {
                Objects.Clear();
                return;
            }

            if (Objects.Remove(networkObject))
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