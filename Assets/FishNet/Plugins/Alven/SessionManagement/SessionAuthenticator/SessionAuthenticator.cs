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

        /// <summary>
        /// Called when connection authentication is passed.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        protected bool PassAuthentication(NetworkConnection connection, string playerId)
        {
            ServerSessionManager sessionManager = NetworkManager.GetServerSessionManager();
            return sessionManager.SetupPlayerConnection(this, playerId, connection);
        }

        /// <summary>
        /// Called when connection authentication is failed.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        protected void FailAuthentication(NetworkConnection connection)
        {
            OnAuthenticationResult?.Invoke(connection, false);
        }
    }
}