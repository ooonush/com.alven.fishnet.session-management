using FishNet.Managing;
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
        public static void Spawn(this ServerManager serverManager, NetworkSessionObject playerObject, SessionPlayer player, Scene scene = default, bool rebuildObservers = true)
        {
            if (rebuildObservers || playerObject.Observers.Contains(player.NetworkConnection))
            {
                playerObject.GivingOwnership = true;
                playerObject.Initialize(player);

                foreach (NetworkSessionObject childSessionObject in playerObject.ChildNetworkSessionObjects)
                {
                    childSessionObject.GivingOwnership = true;
                    childSessionObject.Initialize(player);
                }
            
                serverManager.Spawn(playerObject.NetworkObject, player.NetworkConnection, scene);
            
                foreach (NetworkSessionObject child in playerObject.ChildNetworkSessionObjects)
                {
                    child.GivingOwnership = false;
                }
                playerObject.GivingOwnership = false;
            }
            else
            {
                playerObject.Initialize(player);
                serverManager.Spawn(playerObject.NetworkObject, scene: scene);
            }
        }

        /// <summary>
        /// Spawns an object over the network. Can only be called on the server.
        /// </summary>
        /// <param name="networkObject">NetworkObject instance to spawn.</param>
        /// <param name="player">SessionPlayer to give ownership to.</param>
        public static void Spawn(this ServerManager serverManager, NetworkObject networkObject, SessionPlayer player, Scene scene = default, bool rebuildObservers = true)
        {
            var networkPlayerObject = networkObject.GetComponent<NetworkSessionObject>();
            if (!networkPlayerObject)
            {
                serverManager.NetworkManager.LogWarning("NetworkObject does not have a NetworkPlayerObject component.");
            }
            else
            {
                Spawn(serverManager, networkPlayerObject, player, scene, rebuildObservers);
            }
        }
    }
}