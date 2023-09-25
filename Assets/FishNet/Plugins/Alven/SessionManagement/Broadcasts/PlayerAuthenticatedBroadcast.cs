using FishNet.Broadcast;

namespace FishNet.Alven.SessionManagement
{
    internal readonly struct PlayerAuthenticatedBroadcast : IBroadcast
    {
        public readonly SessionPlayer Player;

        public PlayerAuthenticatedBroadcast(SessionPlayer player)
        {
            Player = player;
        }
    }
}