using FishNet.Managing;

namespace FishNet.Alven.SessionManagement
{
    internal static class NetworkManagerSessionExtensions
    {
        public static ServerSessionManager GetServerSessionManager(this NetworkManager networkManager)
        {
            return networkManager.GetInstance<ServerSessionManager>();
        }

        public static ClientSessionManager GetClientSessionManager(this NetworkManager networkManager)
        {
            return networkManager.GetInstance<ClientSessionManager>();
        }
    }
}