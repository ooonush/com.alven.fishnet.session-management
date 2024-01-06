using System;
using FishNet.Managing;

namespace FishNet.Alven.SessionManagement
{
    internal static class NetworkManagerSessionExtensions
    {
        public static ServerSessionManager GetServerSessionManager(this NetworkManager networkManager)
        {
            if (!networkManager.HasInstance<ServerSessionManager>())
            {
                throw new InvalidOperationException("ServerSessionManager component is not found.");
            }
            return networkManager.GetInstance<ServerSessionManager>();
        }

        public static ClientSessionManager GetClientSessionManager(this NetworkManager networkManager)
        {
            if (!networkManager.HasInstance<ClientSessionManager>())
            {
                throw new InvalidOperationException("ClientSessionManager component is not found.");
            }
            return networkManager.GetInstance<ClientSessionManager>();
        }
    }
}