namespace FishNet.Alven.SessionManagement
{
    public readonly struct PlayerConnectionStateArgs
    {
        public readonly LocalPlayerConnectionState State;

        public PlayerConnectionStateArgs(LocalPlayerConnectionState state)
        {
            State = state;
        }
    }
}