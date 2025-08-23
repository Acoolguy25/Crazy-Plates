using Mirror;
using Mirror.SimpleWeb;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using UnityEngine;

public class ServerLobby : NetworkBehaviour
{
    public static ServerLobby singleton;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null;
    }
#endif
    private void Awake() {
        Debug.Assert(singleton == null, "There can only be one ServerLobby instance.");
        singleton = this;
    }
    [Command(requiresAuthority = true)]
    public void PunishPlayer(string ipAddress, bool isBan) {
        foreach (var conn in NetworkServer.connections.Values) {
            if (conn.address == ipAddress) {
                if (isBan)
                    CustomBasicAuthenticator.singleton.BanPlayer(conn, "The host has banned you");
                else
                    CustomBasicAuthenticator.singleton.KickPlayer(conn, "The host kicked you");
                return;
            }
        }
        Debug.LogError($"No player with IP {ipAddress} found to punish.");
    }
    [Server]
    public override void OnStartServer() {
        CreateJoinCode();
    }
    [Command(requiresAuthority = true)]
    public void CreateNewGameCode() {
        CreateJoinCode();
    }
    private static string ShortRandomId(int length = 12) {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var data = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(data);

        char[] result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[data[i] % chars.Length];

        return new string(result);
    }
    [Server]
    public void CreateJoinCode() {
        if (Transport.active is PortTransport portTransport) {
            //serverProperties.GameCode = $"{GetLocalIPAddress()}|{";
            string password = ShortRandomId();
            string rawCode = $"{CustomBasicAuthenticator.serverIPAddress}:{CustomBasicAuthenticator.serverPort}|{password}";
            string encodedCode = Encryption.EncryptAscii(rawCode, Encryption.liveEncryptionPassword);
            //Debug.Log($"New game code created: {encodedCode} (Password: {rawCode})");
            ServerProperties.Instance.GameCode = encodedCode;
            CustomBasicAuthenticator.singleton.SetPassword(password);
        }
    }
    [TargetRpc]
    public void NotificationToPlayer(NetworkConnection target, string title, string message) {
        NotificationScript.AddNotification(new NotificationData(title, message, NotificationScript.OkOnlyButtons));
    }
    [TargetRpc]
    public void SendDisconnectMessage(NetworkConnection target, string message) {
        LobbyJoin.singleton.JoinGameFail(message, "Disconnected");
        LobbyUI.Instance.DisconnectConnection(LeaveWillingly: false);
    }
}
