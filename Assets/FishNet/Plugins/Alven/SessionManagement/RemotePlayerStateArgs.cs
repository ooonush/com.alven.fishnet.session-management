namespace FishNet.Alven.SessionManagement
{
    public readonly struct RemotePlayerStateArgs
    {
        public readonly PlayerConnectionState State;
        public readonly int ClientPlayerId;

        internal RemotePlayerStateArgs(PlayerConnectionState state, int clientPlayerId)
        {
            State = state;
            ClientPlayerId = clientPlayerId;
        }
    }
}