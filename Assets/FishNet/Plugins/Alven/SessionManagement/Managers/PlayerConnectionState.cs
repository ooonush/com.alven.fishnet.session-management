namespace FishNet.Alven.SessionManagement
{
    public enum PlayerConnectionState : byte
    {
        Connected,
        Reconnected,
        PermanentlyDisconnected,
        TemporarilyDisconnected
    }
}