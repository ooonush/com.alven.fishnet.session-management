using FishNet.Connection;
using FishNet.Managing;
using FishNet.Serializing;

namespace FishNet.Alven.SessionManagement
{
    public static class SessionPlayerSerializer
    {
        public static void WriteSessionPlayer(this Writer writer, SessionPlayer player)
        {
            writer.WriteNetworkConnection(player.NetworkConnection);
            writer.WriteInt32(player.ClientPlayerId);
        }

        public static SessionPlayer ReadSessionPlayer(this Reader reader)
        {
            NetworkConnection connection = reader.ReadNetworkConnection();
            int value = reader.ReadInt32();
            
            if (value == SessionPlayer.UNSET_CLIENTID)
            {
                return SessionPlayer.Empty;
            }
            else
            {
                //Prefer server.
                NetworkManager networkManager = reader.NetworkManager;
                if (networkManager.IsServer)
                {
                    SessionPlayer result;
                    if (networkManager.GetServerSessionManager().Players.TryGetValue(value, out result))
                    {
                        return result;
                    }
                    //If also client then try client side data.
                    else if (networkManager.IsClient)
                    {
                        //If found in client collection then return.
                        if (networkManager.GetClientSessionManager().Players.TryGetValue(value, out result))
                            return result;
                        /* Otherwise make a new instance.
                         * We do not know if this is for the server or client so
                         * initialize it either way. Connections rarely come through
                         * without being in server/client side collection. */
                        else
                        {
                            return new SessionPlayer(networkManager, value, connection.ClientId, true);
                        }
                    }
                    //Only server and not found.
                    else
                    {
                        networkManager.LogWarning($"Unable to find Session Player for read ClientPlayerId " + value + " An empty connection will be returned.");
                        return SessionPlayer.Empty;
                    }
                }
                //Try client side, will only be able to fetch against local connection.
                else
                {
                    //If value is self then return self.
                    if (value == networkManager.GetClientSessionManager().Player.ClientPlayerId)
                        return networkManager.GetClientSessionManager().Player;
                    //Try client side dictionary.
                    else if (networkManager.GetClientSessionManager().Players.TryGetValue(value, out SessionPlayer result))
                        return result;
                    /* Otherwise make a new instance.
                     * We do not know if this is for the server or client so
                     * initialize it either way. Connections rarely come through
                     * without being in server/client side collection. */
                    else
                        return new SessionPlayer(networkManager, value, connection.ClientId, false);
                }
            }
        }
    }
}