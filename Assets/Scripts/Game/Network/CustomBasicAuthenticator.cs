using Mirror;
using Mirror.Authenticators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomBasicAuthenticator : BasicAuthenticator
{
    public static Dictionary<string, double> kickIPs = new();
    public readonly static List<string> banIPs = new();
    //public static ushort maximumPlayers = 100;
    public static CustomBasicAuthenticator singleton { get; private set; }
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null;
    }
#endif
    public void Begin() {
        singleton = this;
    }
    private void Accept() {
        ClientAccept();
    }
    private void Reject(string reason, string title = "Authentication Failed") {
        LobbyJoin.singleton.JoinGameFail(reason, title);
        LobbyUI.Instance.DisconnectConnection();
        Debug.LogError($"Authentication Response: {reason}");

        // Authentication has been rejected
        ClientReject();
    }
    public override void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg) {
        
        //Debug.Log($"Authentication Request: {msg.authUsername} {msg.authPassword}");

        if (connectionsPendingDisconnect.Contains(conn)) return;

        string crashReason = null;
        double kickTime;
        if (banIPs.Any((ip) => ip == conn.address)) {
            crashReason = "You are banned from this server.";
        }
        else if (kickIPs.TryGetValue(conn.address, out kickTime)) {
            double timeLeft = kickTime - Time.realtimeSinceStartupAsDouble;
            if (timeLeft > 0d)
                crashReason = timeLeft > (86400 * 365) ? "You are banned from this server" : $"You are kicked from this server. Try again in {Math.Ceiling(timeLeft)} seconds.";
            else
                kickIPs.Remove(conn.address);
        }
        if (crashReason == null)
            if (ServerProperties.Instance.MaxPlayers <= NetworkServer.connections.Count)
                crashReason = $"The maximum player count of {ServerProperties.Instance.MaxPlayers} has been reached";

        bool shouldPass = crashReason == null && msg.authUsername == serverUsername && msg.authPassword == serverPassword;
        // check the credentials by calling your web server, database table, playfab api, or any method appropriate.
        if (shouldPass || conn is LocalConnectionToClient) {
            // create and send msg to client so it knows to proceed
            AuthResponseMessage authResponseMessage = new AuthResponseMessage
            {
                code = 100,
                message = "Success"
            };

            conn.Send(authResponseMessage);

            // Accept the successful authentication
            ServerAccept(conn);
        }
        else {
            connectionsPendingDisconnect.Add(conn);

            // create and send msg to client so it knows to disconnect
            AuthResponseMessage authResponseMessage = new AuthResponseMessage
            {
                code = 200,
                message = crashReason != null? crashReason: "Invalid Credentials"
            };

            conn.Send(authResponseMessage);

            // must set NetworkConnection isAuthenticated = false
            conn.isAuthenticated = false;

            // disconnect the client after 1 second so that response message gets delivered
            StartCoroutine(DelayedDisconnect(conn, 1f));
        }
    }
    public override void OnAuthResponseMessage(AuthResponseMessage msg) {
        if (msg.code == 100) {
            Accept();
        }
        else {
            Reject(msg.message);
        }
    }
    public void SetPassword(string password_)
    {
        password = password_;
        serverPassword = password_;
    }
    [Server]
    protected IEnumerator DelayedDisconnect(NetworkConnectionToClient conn, string reason, float time = 1f) {
        CustomNetworkManager.serverLobby.SendDisconnectMessage(conn, reason);
        yield return new WaitForSecondsRealtime(time);
        // Check if connection is still active
        if (NetworkServer.connections.ContainsValue(conn))
            conn.Disconnect();
    }
    [Server]
    public void KickPlayer(NetworkConnectionToClient connection, string reason, double duration = 30d) {
        string ipAddress = connection.address;
        if (kickIPs.ContainsKey(ipAddress))
            kickIPs[ipAddress] = Math.Max(kickIPs[ipAddress], Time.realtimeSinceStartupAsDouble) + duration;
        else
            kickIPs.Add(ipAddress, Time.realtimeSinceStartupAsDouble + duration);
        StartCoroutine(DelayedDisconnect(connection, reason));
    }
    [Server]
    public void BanPlayer(NetworkConnectionToClient connection, string reason) {
        KickPlayer(connection, reason, 50 * 365 * 86400); // 50 years
    }
}