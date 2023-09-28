using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine.SceneManagement;

namespace FishNet.Alven.SessionManagement
{
    public static class ServerManagerExtensions
    {
        /// <summary>
        /// Spawns an object over the network. Can only be called on the server.
        /// </summary>
        /// <param name="playerObject">NetworkPlayerObject instance to spawn.</param>
        /// <param name="player">SessionPlayer to give ownership to.</param>
        public static void Spawn(this ServerManager serverManager, NetworkPlayerObject playerObject, SessionPlayer player, Scene scene = default)
        {
            playerObject.GivingOwnership = true;
            playerObject.Initialize(player);
            serverManager.Spawn(playerObject.NetworkObject, player.NetworkConnection, scene);
            playerObject.GivingOwnership = false;
        }

        /// <summary>
        /// Spawns an object over the network. Can only be called on the server.
        /// </summary>
        /// <param name="networkObject">NetworkObject instance to spawn.</param>
        /// <param name="player">SessionPlayer to give ownership to.</param>
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