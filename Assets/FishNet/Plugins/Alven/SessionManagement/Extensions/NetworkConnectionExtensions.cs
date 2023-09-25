using FishNet.Connection;

namespace FishNet.Alven.SessionManagement
{
    public static class NetworkConnectionExtensions
    {
        public static SessionPlayer GetSessionPlayer(this NetworkConnection connection)
        {
            if (connection.NetworkManager.ServerManager.Clients.TryGetValue(connection.ClientId, out NetworkConnection c))
            {
                if (ReferenceEquals(connection, c))
                {
                    return connection.NetworkManager.GetServerSessionManager().GetPlayer(connection);
                }
            }

            if (connection.IsLocalClient)
            {
                return connection.NetworkManager.GetClientSessionManager().Player;
            }

            if (connection.NetworkManager.ClientManager.Clients.TryGetValue(connection.ClientId, out c))
            {
                if (ReferenceEquals(connection, c))
                {
                    return connection.NetworkManager.GetClientSessionManager().GetPlayer(connection);
                }
            }
            
            connection.NetworkManager.LogError("Could not find session player for connection");
            return SessionPlayer.Empty;
        }
    }
}