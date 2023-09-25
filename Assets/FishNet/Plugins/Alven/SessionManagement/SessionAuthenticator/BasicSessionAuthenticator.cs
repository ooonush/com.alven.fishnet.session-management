using System;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;

namespace FishNet.Alven.SessionManagement
{
    public sealed class BasicSessionAuthenticator : SessionAuthenticator
    {
        private string _playerId;

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);

            _playerId = Guid.NewGuid().ToString();

            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            networkManager.ServerManager.RegisterBroadcast<PlayerIdBroadcast>(OnPlayerIdBroadcast, false);
        }

        private void OnPlayerIdBroadcast(NetworkConnection connection, PlayerIdBroadcast broadcast)
        {
            PassAuthentication(connection, broadcast.PlayerId);
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                NetworkManager.ClientManager.Broadcast(new PlayerIdBroadcast(_playerId));
            }
        }
    }
}