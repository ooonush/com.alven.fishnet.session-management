using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine.SceneManagement;

namespace FishNet.Alven.SessionManagement
{
    public static class ServerManagerExtensions
    {
        public static void Spawn(this ServerManager serverManager, NetworkPlayerObject playerObject, SessionPlayer player, Scene scene = default)
        {
            playerObject.GivingOwnership = true;
            playerObject.Initialize(player);
            serverManager.Spawn(playerObject.NetworkObject, player.NetworkConnection, scene);
            playerObject.GivingOwnership = false;
        }

        public static void Spawn(this ServerManager serverManager, NetworkObject networkObject, SessionPlayer player, Scene scene = default)
        {
            var networkPlayerObject = networkObject.GetComponent<NetworkPlayerObject>();
            if (!networkPlayerObject)
            {
                serverManager.NetworkManager.LogWarning("NetworkObject does not have a NetworkPlayerObject component.");
            }
            else
            {
                Spawn(serverManager, networkPlayerObject, player, scene);
            }
        }
    }
}