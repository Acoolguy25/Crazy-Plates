using Mirror;
using Mirror.SimpleWeb;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using UnityEngine;
using System.Collections;

public class ServerLobby : NetworkBehaviour
{
    public static ServerLobby singleton;
    [SyncVar(hook = nameof(OnGameStartingChanged))]
    public bool GameStarting = false;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null;
    }
#endif
    private void Awake() {
        if (singleton != null && singleton != this) {
            Destroy(gameObject);
            Destroy(this);
            return;
        }
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
        GameStarting = false;
        base.OnStartServer();
    }
    [Command(requiresAuthority = true)]
    public void CreateNewGameCode() {
        CreateJoinCode();
    }
    [Command(requiresAuthority = true)]
    public void CmdStartGame() {
        StartGame();
    }
    [Command(requiresAuthority = true)]
    private void StartGame() {
        if (GameStarting)
            return;
        GameStarting = true;
        StartCoroutine(ServerGameStart());
    }
    [Server]
    public IEnumerator ServerGameStart() {
        while (true) {
            bool isReady = true;
            foreach (var keyValPair in NetworkServer.connections) {
                NetworkConnectionToClient conn = keyValPair.Value;
                if (conn.connectionId == 1)
                    continue;
                if (!conn.isReady) {
                    isReady = false;
                    break;
                }
            }
            if (isReady)
                break;
            else
                yield return null;
        }
        RpcGameStarting();
        ServerEvents.Instance.ServerEventsBegin();
        yield return new WaitForSecondsRealtime(2.5f);
        CustomNetworkManager.singleton2.ServerChangeScene("Default");
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
    [ClientRpc]
    public void BackToLobby(double endTime) {
        LobbyUI.Instance.BackToLobby((float) System.Math.Max(0d, endTime - SharedFunctions.GetNetworkTime()), false, null);
    }
    [ClientRpc]
    public void RpcGameStarting() {
        GameStarting = true;
        LobbyUI.Instance.GameStartingFunc();
    }
    [Client]
    public override void OnStartAuthority() {
        base.OnStartAuthority();
        if (ServerProperties.Instance.SinglePlayer)
            CmdStartGame();
    }
    [Server]
    public void GameEnd() {
        GameEndRpc();
    }
    [Client]
    public void OnGameStartingChanged(bool oldVal, bool newVal) {
        if (!newVal)
            LobbyUI.Instance.FadeBlackScreen(0f);
    }
    [ClientRpc]
    public void GameEndRpc() {
        if (ServerProperties.Instance.SinglePlayer) {
            if (GameEvents.Instance.SurvivalTime > SaveManager.SaveInstance.singleplayerTime) {
                SaveManager.SaveInstance.singleplayerTime = GameEvents.Instance.SurvivalTime;
                SaveManager.SaveGame();
                SingleplayerMenu.Instance.UpdateSinglePlayerTime(SaveManager.SaveInstance.singleplayerTime);
            }
        }
    }
    
    public void Start() {
        DontDestroyOnLoad(this);
    }
}
