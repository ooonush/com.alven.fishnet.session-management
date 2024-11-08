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
        /// <param name="rebuildObservers">True to give ownership and add player to the Observers. False to give ownership without adding player to the Observers. This can be useful if you want to give ownership of the object that is in a scene that the player does not currently have loaded. </param>.
        public static void Spawn(this ServerManager serverManager, NetworkSessionObject playerObject, SessionPlayer player, Scene scene = default, bool rebuildObservers = true)
        {
            playerObject.GivingOwnership = true;
            playerObject.IsSpawning = true;
            playerObject.Initialize(player);
            
            foreach (NetworkSessionObject childSessionObject in playerObject.ChildNetworkSessionObjects)
            {
                childSessionObject.GivingOwnership = true;
                childSessionObject.IsSpawning = true;
                childSessionObject.Initialize(player);
            }
            
            if (rebuildObservers)
            {
                serverManager.Spawn(playerObject.NetworkObject, player.NetworkConnection, scene);
            }
            else
            {
                serverManager.Spawn(playerObject.NetworkObject, scene: scene);
            }
            
            foreach (NetworkSessionObject child in playerObject.ChildNetworkSessionObjects)
            {
                child.GivingOwnership = false;
                child.IsSpawning = false;
            }
            playerObject.IsSpawning = false;
            playerObject.GivingOwnership = false;
        }

        /// <summary>
        /// Spawns an object over the network. Can only be called on the server.
        /// </summary>
        /// <param name="networkObject">NetworkObject instance to spawn.</param>
        /// <param name="player">SessionPlayer to give ownership to.</param>
        /// <param name="rebuildObservers">True to give ownership and add player to the Observers. False to give ownership without adding player to the Observers. This can be useful if you want to give ownership of the object that is in a scene that the player does not currently have loaded. </param>.
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