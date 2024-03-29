﻿using FishNet.Connection;
using FishNet.Managing;

namespace FishNet.Alven.SessionManagement
{
    public static class NetworkConnectionExtensions
    {
        /// <summary>
        /// SessionPlayer for this connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static SessionPlayer GetSessionPlayer(this NetworkConnection connection)
        {
            if (connection == null || !connection.IsValid)
            {
                return SessionPlayer.Empty;
            }
            
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