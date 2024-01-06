using System;
using UnityEngine;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;

namespace FishNet.Alven.SessionManagement
{
    [AddComponentMenu("FishNet/Session Management/BasicSessionAuthenticator")]
    public sealed class BasicSessionAuthenticator : SessionAuthenticator
    {
        private struct PlayerIdBroadcast : IBroadcast
        {
            public string PlayerId;
        }

        private string PlayerIdKey => $"{_profile}.fishnet.alven.session-management.player-id";
        private string _profile = "default";
        private string _playerId;

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);

#if PARRELSYNC && UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                _profile = $"Clone_{ParrelSync.ClonesManager.GetArgument()}_Profile";
            }
#endif

            SignIn();

            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            networkManager.ServerManager.RegisterBroadcast<PlayerIdBroadcast>(OnPlayerIdBroadcast, false);
        }

        private void SignIn()
        {
            _playerId = PlayerPrefs.GetString(PlayerIdKey);
            if (string.IsNullOrEmpty(_playerId))
            {
                _playerId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PlayerIdKey, _playerId);
            }
            NetworkManager.Log($"Player {_profile} with PlayerId {_playerId} signed in" );
        }

        private void OnPlayerIdBroadcast(NetworkConnection connection, PlayerIdBroadcast broadcast)
        {
            PassAuthentication(connection, broadcast.PlayerId);
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                NetworkManager.ClientManager.Broadcast(new PlayerIdBroadcast { PlayerId = _playerId });
            }
        }
    }
}