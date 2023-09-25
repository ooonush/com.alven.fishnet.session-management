using System;
using FishNet.Authenticating;
using FishNet.Connection;

namespace FishNet.Alven.SessionManagement
{
    public abstract class SessionAuthenticator : Authenticator
    {
        public sealed override event Action<NetworkConnection, bool> OnAuthenticationResult;

        internal void InvokeAuthenticationResult(NetworkConnection connection, bool result)
        {
            OnAuthenticationResult?.Invoke(connection, result);
        }

        protected bool PassAuthentication(NetworkConnection connection, string playerId)
        {
            ServerSessionManager sessionManager = NetworkManager.GetServerSessionManager();
            return sessionManager.SetupPlayerConnection(this, playerId, connection);
        }

        protected void FailAuthentication(NetworkConnection connection)
        {
            OnAuthenticationResult?.Invoke(connection, false);
        }
    }
}