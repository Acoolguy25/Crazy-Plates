using Mirror;
using Mirror.SimpleWeb;
using Mirror.Authenticators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class CustomBasicAuthenticator : BasicAuthenticator
{
    public static Dictionary<string, double> kickIPs = new();
    public readonly static List<string> banIPs = new();
    public static bool allowExternalConnections = false;
    public static string serverIPAddress = "";
    public static ushort serverPort = 0;
    //public static ushort maximumPlayers = 100;
    public static CustomBasicAuthenticator singleton { get; private set; }
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null;
    }
#endif
    public void Begin(Dictionary<string, object> options, bool clientOnly){
        singleton = this;
        if (clientOnly) {
            allowExternalConnections = false;
            return;
        }
        allowExternalConnections = options != null && !Convert.ToBoolean(options["LANOnly"]);
        serverIPAddress = Convert.ToString(options["ServerIP"]);
        if (!IsValidIPv4(serverIPAddress))
            throw new ArgumentException("ServerIP is invalid");
        Debug.Log($"Local IP Address: {serverIPAddress}");
        CustomNetworkManager.singleton2.networkAddress = allowExternalConnections ? "0.0.0.0" : serverIPAddress;
        try {
            serverPort = options != null ? Convert.ToUInt16(options["ServerPort"]) : (ushort)27777;
        }
        catch (Exception) {
            throw new ArgumentException("ServerPort is invalid");
        }
        PortTransport webTransport = CustomNetworkManager.singleton2.transport as PortTransport;
        webTransport.Port = serverPort;
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
    public static string GetLocalIPAddress() {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 only
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
        //using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
        //    // Doesn’t actually send data — just used to figure out which local adapter would be used
        //    socket.Connect("8.8.8.8", 65530);
        //    var endPoint = socket.LocalEndPoint as IPEndPoint;
        //    return endPoint?.Address.ToString() ?? throw new Exception("No IPv4 address found!");
        //}
    }
    bool IsValidIPv4(string ipString) {
        if (IPAddress.TryParse(ipString, out var address)) {
            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }
        return false;
    }
    public override void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg) {
        
        //Debug.Log($"Authentication Request: {msg.authUsername} {msg.authPassword}");

        if (connectionsPendingDisconnect.Contains(conn)) return;

        string crashReason = null;
        double kickTime;
        if (!(conn is LocalConnectionToClient)) {
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
        }

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
            Debug.Log($"Removed Authentication Response: {authResponseMessage.message}");

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