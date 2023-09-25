using FishNet.Broadcast;

namespace FishNet.Alven.SessionManagement
{
    internal readonly struct ConnectedPlayersBroadcast : IBroadcast
    {
        public readonly int[] ClientPlayerIds;
        public readonly int[] ConnectionIds;

        public ConnectedPlayersBroadcast(int[] clientPlayerIds, int[] connectionIds)
        {
            ClientPlayerIds = clientPlayerIds;
            ConnectionIds = connectionIds;
        }
    }
}