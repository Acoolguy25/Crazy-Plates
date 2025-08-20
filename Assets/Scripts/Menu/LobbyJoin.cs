using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Mirror;
using Mirror.SimpleWeb;

public class LobbyJoin : MonoBehaviour {
    public static bool DidLeave;
    public TMP_InputField joinGameField;
    public static LobbyJoin singleton;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null;
    }
#endif
    private void Awake() {
        singleton = this;
    }
    bool isJoining = false;
    private string JoinGameInternal(string encryptedJoinCode) {
        string joinCode = Encryption.DecryptAscii(encryptedJoinCode, Encryption.liveEncryptionPassword);
        if (string.IsNullOrEmpty(joinCode)) {
            return "Join code cannot be null or empty.";
        }
        string[] parts = joinCode.Split('|');
        if (parts.Length != 2) {
            return "Invalid join code format. Expected format: 'IP:Port|RandomId'.";
        }
        string[] addressParts = parts[0].Split(':');
        string password = parts[1];
        if (addressParts.Length != 2) {
            return "Invalid IP address format in join code.";
        }
        string ipAddress = addressParts[0];
        if (!int.TryParse(addressParts[1], out int port)) {
            return "Invalid port number in join code.";
        }
        DidLeave = false;
        CustomNetworkManager.singleton2.networkAddress = ipAddress;
        CustomNetworkManager.singleton2.GetComponent<SimpleWebTransport>().port = (ushort)port;
        CustomNetworkManager.singleton2.Init(password: password);
        try {
            CustomNetworkManager.singleton2.StartClient();
        }
        catch (Exception e) {
            Debug.LogError($"Failed to start client: {e.Message}");
            return $"Failed to connect to the server.\n{e.Message}";
        }
        return null;
    }
    public void JoinGame() {
        NotificationScript.AddNotification(new NotificationData("Joining Game", "Please wait while we connect you to the game...", NotificationScript.CancelOnlyButtons));
        NetworkClient.OnErrorEvent += (error, str) => JoinGameFail(str);
        CustomNetworkManager.singleton2.GetComponent<SimpleWebTransport>().OnClientError += (error, str) => JoinGameFail(str);
        isJoining = true;
        string output = JoinGameInternal(joinGameField.text);
        if (output != null) {
            JoinGameFail(output);
            return;
        }
    }
    public void CancelJoin(NotificationButton btn) {
        StopJoin();
        LobbyUI.Instance.DisconnectConnection();
    }
    public void JoinGameFail(string message, string title = "Join Game Failed") {
        if (!isJoining)
            return;
        if (DidLeave)
            return;
        NotificationScript.AddNotification(new NotificationData(title, message, NotificationScript.OkOnlyButtons));
        CancelJoin(NotificationButton.None);
        isJoining = false;
    }
    public void StopJoin() {
        NotificationScript.DeleteNotification("Joining Game");
    }
}