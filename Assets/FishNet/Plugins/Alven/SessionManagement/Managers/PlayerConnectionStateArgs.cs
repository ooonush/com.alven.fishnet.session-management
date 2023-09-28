namespace FishNet.Alven.SessionManagement
{
    public readonly struct PlayerConnectionStateArgs
    {
        public readonly LocalPlayerConnectionState State;

        internal PlayerConnectionStateArgs(LocalPlayerConnectionState state)
        {
            State = state;
        }
    }
}