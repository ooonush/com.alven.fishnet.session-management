using FishNet.Broadcast;

namespace FishNet.Alven.SessionManagement
{
    internal readonly struct PlayerIdBroadcast : IBroadcast
    {
        public readonly string PlayerId;

        public PlayerIdBroadcast(string playerId)
        {
            PlayerId = playerId;
        }
    }
}