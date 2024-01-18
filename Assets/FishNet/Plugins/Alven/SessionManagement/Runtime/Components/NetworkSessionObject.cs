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
    public sealed class NetworkSessionObject : NetworkBehaviour
    {
        private readonly SyncVar<SessionPlayer> _ownerPlayer = new SyncVar<SessionPlayer>();
        /// <summary>
        /// Owner SessionPlayer of this object.
        /// </summary>
        public SessionPlayer OwnerPlayer
        {
            get => IsOffline ? null : _ownerPlayer.Value;
            set => _ownerPlayer.Value = value;
        }

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

        [SerializeField, HideInInspector]
        internal NetworkSessionObject[] ChildNetworkSessionObjects;

        private void Awake()
        {
            _ownerPlayer.OnChange += OnOwnerPlayerChanged;
        }

        private void OnDestroy()
        {
            _ownerPlayer.OnChange -= OnOwnerPlayerChanged;
        }

        internal void Initialize(SessionPlayer ownerPlayer)
        {
            SetOwner(ownerPlayer);
        }

        private void SetOwner(SessionPlayer newOwner)
        {
            if (newOwner != null && newOwner.IsValid)
            {
                OwnerPlayer = newOwner;
            }
            else
            {
                OwnerPlayer = null;
            }
        }

        /// <summary>
        /// Gives ownership to newOwner.
        /// </summary>
        /// <param name="newOwner">Session Player</param>
        /// <param name="rebuildObservers">
        /// If True, the object will be transferred to the owner immediately.
        /// If player is not its observer, the observers are rebuilt.
        /// False if there is no need to rebuild the observers.
        /// The object will be transferred to the player's possession as soon as the player becomes an observer.</param>
        [Server]
        public void GiveOwnershipPlayer(SessionPlayer newOwner, bool rebuildObservers = true)
        {
            if (rebuildObservers || NetworkObject.Observers.Contains(newOwner?.NetworkConnection))
            {
                GivingOwnership = true;
                if (newOwner != null && newOwner.IsValid)
                {
                    OwnerPlayer = newOwner;
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
                    OwnerPlayer = null;
                }

                GivingOwnership = false;
            }
            else
            {
                SetOwner(newOwner);
            }
        }

        private void OnOwnerPlayerChanged(SessionPlayer prev, SessionPlayer next, bool asServer)
        {
            if (!NetworkManager.DoubleLogic(asServer) && (IsServerInitialized || next == null || next.IsLocalPlayer))
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
            if (GivingOwnership) return;
            
            if (connection == OwnerPlayer?.NetworkConnection && Owner != connection)
            {
                GivingOwnership = true;
                GiveOwnership(connection);
                GivingOwnership = false;
            }
        }

        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            if (GivingOwnership) return;
            
            OwnerPlayer = null;
        }

        public override void OnDespawnServer(NetworkConnection connection)
        {
            bool hasOwner = Owner != null && Owner.IsValid && Owner.Objects.Contains(NetworkObject);
            
            if (hasOwner && OwnerPlayer != null && OwnerPlayer.IsValid && OwnerPlayer.NetworkConnection == connection)
            {
                GivingOwnership = true;
                RemoveOwnership();
                GivingOwnership = false;
            }
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            ChildNetworkSessionObjects = GetComponentsInChildren<NetworkSessionObject>();
        }
    }
}