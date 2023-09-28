namespace FishNet.Alven.SessionManagement
{
    public readonly struct RemotePlayerConnectionStateArgs
    {
        public readonly PlayerConnectionState State;
        public readonly int ClientPlayerId;

        internal RemotePlayerConnectionStateArgs(PlayerConnectionState state, int clientPlayerId)
        {
            State = state;
            ClientPlayerId = clientPlayerId;
        }
    }
}