using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Utility.Extension;
using UnityEngine;

namespace FishNet.Alven.SessionManagement
{
    /// <summary>
    /// Component for managing session players network objects.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerObject : NetworkBehaviour
    {
        [SyncVar(OnChange = nameof(OnOwnerPlayerChanged))]
        private SessionPlayer _ownerPlayer;
        /// <summary>
        /// Owner SessionPlayer of this object.
        /// </summary>
        public SessionPlayer OwnerPlayer => IsOffline ? null : _ownerPlayer;
        /// <summary>
        /// The local SessionPlayer of the client calling this method.
        /// </summary>
        public SessionPlayer LocalPlayer => ClientSessionManager.Player;
        public ServerSessionManager ServerSessionManager => NetworkManager.GetServerSessionManager();
        public ClientSessionManager ClientSessionManager => NetworkManager.GetClientSessionManager();

        /// <summary>
        /// Invoked on after ownership has changed.
        /// </summary>
        public event Action<SessionPlayer, SessionPlayer, bool> OnOwnershipPlayer;

        internal bool GivingOwnership;

        internal void Initialize(SessionPlayer ownerPlayer)
        {
            SetOwner(ownerPlayer);
        }

        private void SetOwner(SessionPlayer newOwner)
        {
            if (newOwner != null && newOwner.IsValid)
            {
                _ownerPlayer = newOwner;
            }
            else
            {
                _ownerPlayer = null;
            }
        }

        /// <summary>
        /// Gives ownership to newOwner.
        /// </summary>
        /// <param name="newOwner">Session Player</param>
        [Server]
        public void GiveOwnershipPlayer(SessionPlayer newOwner)
        {
            GivingOwnership = true;
            if (newOwner != null && newOwner.IsValid)
            {
                _ownerPlayer = newOwner;
                if (newOwner.IsConnected)
                {
                    GiveOwnership(newOwner.NetworkConnection);
                }
                else
                {
                    RemoveOwnership();
                }
            }
            else
            {
                _ownerPlayer = null;
            }

            GivingOwnership = false;
        }

        /// <summary>
        /// Removes ownership from all players.
        /// </summary>
        [Server]
        public void RemoveOwnershipPlayer()
        {
            GiveOwnershipPlayer(null);
        }
        
        private void OnOwnerPlayerChanged(SessionPlayer prev, SessionPlayer next, bool asServer)
        {
            if (!NetworkManager.DoubleLogic(asServer) && (IsServer || next == null || next.IsLocalPlayer))
            {
                if (prev != null && prev.IsValid)
                {
                    prev.RemoveObject(this);
                }

                if (next != null && next.IsValid)
                {
                    next.AddObject(this);
                }
            }
            OnOwnershipPlayer?.Invoke(prev, next, asServer);
        }

        public override void OnStopNetwork()
        {
            OwnerPlayer?.RemoveObject(this);
        }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            if (!GivingOwnership && OwnerPlayer != null && connection == OwnerPlayer.NetworkConnection && OwnerPlayer.IsConnected && Owner != connection)
            {
                GivingOwnership = true;
                GiveOwnership(connection);
                GivingOwnership = false;
            }
        }

        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            if (GivingOwnership) return;
            
            _ownerPlayer = null;
        }

        public override void OnDespawnServer(NetworkConnection connection)
        {
            if (OwnerPlayer != null && OwnerPlayer.IsValid && OwnerPlayer.NetworkConnection == connection)
            {
                GivingOwnership = true;
                RemoveOwnership();
                GivingOwnership = false;
            }
        }
    }
}