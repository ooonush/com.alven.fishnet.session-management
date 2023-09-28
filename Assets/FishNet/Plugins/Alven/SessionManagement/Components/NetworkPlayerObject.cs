using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Utility.Extension;
using UnityEngine;

namespace FishNet.Alven.SessionManagement
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerObject : NetworkBehaviour
    {
        [SyncVar(OnChange = nameof(OnOwnerPlayerChanged))]
        private SessionPlayer _ownerPlayer;

        internal bool GivingOwnership;
        public SessionPlayer OwnerPlayer => IsOffline ? null : _ownerPlayer;
        public SessionPlayer LocalPlayer => ClientSessionManager.Player;
        public ServerSessionManager ServerSessionManager => NetworkManager.GetServerSessionManager();
        public ClientSessionManager ClientSessionManager => NetworkManager.GetClientSessionManager();

        public event Action<SessionPlayer, SessionPlayer, bool> OnOwnershipPlayer;

        internal void Initialize(SessionPlayer ownerPlayer)
        {
            SetOwner(ownerPlayer);
        }

        internal void SetOwner(SessionPlayer newOwner)
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

        [Server]
        public void GiveOwnership(SessionPlayer newOwner)
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