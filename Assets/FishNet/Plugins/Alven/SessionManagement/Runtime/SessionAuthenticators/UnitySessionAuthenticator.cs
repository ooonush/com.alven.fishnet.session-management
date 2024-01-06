#if UNITY_SERVICES_AUTHENTICATION

using System.Threading.Tasks;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace FishNet.Alven.SessionManagement
{
    [AddComponentMenu("FishNet/Session Management/UnitySessionAuthenticator")]
    public sealed class UnitySessionAuthenticator : SessionAuthenticator
    {
        private struct PlayerIdBroadcast : IBroadcast
        {
            public string PlayerId;
        }

        [Tooltip("Automatically call UnityServices.InitializeAsync() and sign in anonymously.")]
        public bool AutoSignIn = true;

        private Task _authenticationTask;

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            
            if (AutoSignIn)
            {
                _authenticationTask = SignInAnonymouslyAsync();
            }
            
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            networkManager.ServerManager.RegisterBroadcast<PlayerIdBroadcast>(OnPlayerIdBroadcast, false);
        }

        private async Task SignInAnonymouslyAsync()
        {
            await UnityServices.InitializeAsync();
            
#if PARRELSYNC && UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                var profile = $"parrelsync_clone_{ParrelSync.ClonesManager.GetArgument()}";
                AuthenticationService.Instance.SwitchProfile(profile);
            }
#endif
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            string playerId = AuthenticationService.Instance.PlayerId;
            NetworkManager.Log($"Player {AuthenticationService.Instance.Profile} with PlayerId {playerId} signed in" );
        }

        private void OnPlayerIdBroadcast(NetworkConnection connection, PlayerIdBroadcast broadcast)
        {
            PassAuthentication(connection, broadcast.PlayerId);
        }

        private async void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started) return;
            
            if (_authenticationTask != null)
            {
                await _authenticationTask;
            }
            
            string playerId = AuthenticationService.Instance.PlayerId;
            NetworkManager.ClientManager.Broadcast(new PlayerIdBroadcast { PlayerId = playerId });
        }
    }
}

#endif