using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;

namespace FishNet.Alven.SessionManagement
{
    public sealed class SessionPlayer
    {
        public const int UNSET_CLIENTID = -1;
        public static SessionPlayer Empty => new SessionPlayer();

        public NetworkManager NetworkManager { get; private set; }
        public int ClientPlayerId { get; private set; } = UNSET_CLIENTID;
        public string PlayerId { get; private set; }
        public bool IsLocalPlayer => ClientSessionManager.Player.ClientPlayerId == ClientPlayerId;
        public bool IsValid => ConnectionId != NetworkConnection.UNSET_CLIENTID_VALUE;
        public NetworkConnection NetworkConnection
        {
            get
            {
                // This is done to get rid of some of the problems of the initialization order of NetworkConnection and SessionPlayer.
                if (_asServer)
                {
                    return GetNetworkConnection(NetworkManager.ServerManager.Clients);
                }

                if (NetworkManager.ClientManager.Connection.ClientId == ConnectionId)
                {
                    return NetworkManager.ClientManager.Connection;
                }

                return GetNetworkConnection(NetworkManager.ClientManager.Clients);
                
                NetworkConnection GetNetworkConnection(IReadOnlyDictionary<int, NetworkConnection> clients)
                {
                    return clients.TryGetValue(ConnectionId, out NetworkConnection connection) ? connection : NetworkManager.EmptyConnection;
                }
            }
        }
        public bool IsConnected => NetworkConnection != null && NetworkConnection.IsActive;

        internal bool IsConnectedFirstTime { get; private set; } = true;

        internal int ConnectionId { get; private set; } = NetworkConnection.UNSET_CLIENTID_VALUE;
        private readonly bool _asServer;

        private ClientSessionManager ClientSessionManager =>
            NetworkManager == null ? null : NetworkManager.GetClientSessionManager();

        internal SessionPlayer(NetworkManager networkManager, int clientPlayerId, int connectionId, bool asServer,
            string playerId = default)
        {
            NetworkManager = networkManager;
            ClientPlayerId = clientPlayerId;
            ConnectionId = connectionId;
            _asServer = asServer;
            PlayerId = playerId;
        }

        public SessionPlayer()
        {
            
        }

        internal void SetupReconnection(int connectionId)
        {
            IsConnectedFirstTime = false;
            ConnectionId = connectionId;
        }

        internal void Dispose()
        {
            NetworkManager = null;
            ConnectionId = NetworkConnection.UNSET_CLIENTID_VALUE;
            PlayerId = null;
            ClientPlayerId = UNSET_CLIENTID;
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