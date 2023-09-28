using FishNet.Broadcast;

namespace FishNet.Alven.SessionManagement
{
    internal readonly struct PlayerConnectedBroadcast : IBroadcast
    {
        public readonly SessionPlayer Player;
        public readonly bool IsReconnected;

        public PlayerConnectedBroadcast(SessionPlayer player, bool isReconnected)
        {
            Player = player;
            IsReconnected = isReconnected;
        }
    }
}