namespace FishNet.Alven.SessionManagement
{
    public enum PlayerConnectionState : byte
    {
        Connected,
        Disconnected,
        Reconnected,
        Leaved
    }
}