using FishNet.Broadcast;

namespace FishNet.Alven.SessionManagement
{
    internal readonly struct PlayerConnectionChangeBroadcast : IBroadcast
    {
        public readonly int ClientPlayerId;
        public readonly int ConnectionId;
        public readonly PlayerConnectionState State;

        public PlayerConnectionChangeBroadcast(int clientPlayerId, int connectionId, PlayerConnectionState state)
        {
            ClientPlayerId = clientPlayerId;
            ConnectionId = connectionId;
            State = state;
        }
    }
}