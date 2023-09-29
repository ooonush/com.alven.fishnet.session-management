namespace FishNet.Alven.SessionManagement
{
    public enum LocalPlayerConnectionState
    {
        /// <summary>
        /// The player has been connected first time.
        /// Is called after the player has been authenticated.
        /// </summary>
        Connected,
        /// <summary>
        /// The player has been reconnected to this session.
        /// Is called after the player has been authenticated.
        /// </summary>
        Reconnected,
        /// <summary>
        /// The player was disconnected. If a session was started on the server, he was disconnected temporarily and can reconnect.
        /// Otherwise he is disconnected permanently and next time he will connect as a new player with the same PlayerId.
        /// </summary>
        Disconnected
    }
}