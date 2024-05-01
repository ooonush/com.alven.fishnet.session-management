using FishNet.Managing;
using FishNet.Serializing;

namespace FishNet.Alven.SessionManagement
{
    public static class SessionPlayerSerializer
    {
        public static void WriteSessionPlayer(this Writer writer, SessionPlayer player)
        {
            writer.WriteInt32(player.NetworkConnection.ClientId);
            writer.WriteInt32(player.ClientPlayerId);
        }

        public static SessionPlayer ReadSessionPlayer(this Reader reader)
        {
            int connectionId = reader.ReadInt32();
            int clientPlayerId = reader.ReadInt32();
            
            if (clientPlayerId == SessionPlayer.UNSET_CLIENTID)
            {
                return SessionPlayer.Empty;
            }
            else
            {
                //Prefer server.
                NetworkManager networkManager = reader.NetworkManager;
                if (networkManager.IsServerStarted)
                {
                    SessionPlayer result;
                    if (networkManager.GetServerSessionManager().Players.TryGetValue(clientPlayerId, out result))
                    {
                        return result;
                    }
                    //If also client then try client side data.
                    else if (networkManager.IsClientStarted)
                    {
                        //If found in client collection then return.
                        if (networkManager.GetClientSessionManager().Players.TryGetValue(clientPlayerId, out result))
                            return result;
                        /* Otherwise make a new instance.
                         * We do not know if this is for the server or client so
                         * initialize it either way. Connections rarely come through
                         * without being in server/client side collection. */
                        else
                        {
                            return new SessionPlayer(networkManager, clientPlayerId, connectionId);
                        }
                    }
                    //Only server and not found.
                    else
                    {
                        networkManager.LogWarning($"Unable to find Session Player for read ClientPlayerId " + clientPlayerId + " An empty connection will be returned.");
                        return SessionPlayer.Empty;
                    }
                }
                //Try client side, will only be able to fetch against local connection.
                else
                {
                    //If value is self then return self.
                    if (clientPlayerId == networkManager.GetClientSessionManager().Player.ClientPlayerId)
                        return networkManager.GetClientSessionManager().Player;
                    //Try client side dictionary.
                    else if (networkManager.GetClientSessionManager().Players
                             .TryGetValue(clientPlayerId, out SessionPlayer result))
                        return result;
                    /* Otherwise make a new instance.
                     * We do not know if this is for the server or client so
                     * initialize it either way. Connections rarely come through
                     * without being in server/client side collection. */
                    else
                        return new SessionPlayer(networkManager, clientPlayerId, connectionId);
                }
            }
        }
    }
}