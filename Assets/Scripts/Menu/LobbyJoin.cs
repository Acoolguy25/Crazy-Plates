using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Mirror;
using Mirror.SimpleWeb;
using UnityEngine.SceneManagement;

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
        string joinCode, ipAddress, password;
        int port;
        string[] addressParts, parts;
        try {
            joinCode = Encryption.DecryptAscii(encryptedJoinCode, Encryption.liveEncryptionPassword);
            if (string.IsNullOrEmpty(joinCode)) {
                return "Join code cannot be null or empty.";
            }
            parts = joinCode.Split('|');
            if (parts.Length != 2) {
                return "Invalid join code format. Expected format: 'IP:Port|RandomId'.";
            }
            addressParts = parts[0].Split(':');
            password = parts[1];
            if (addressParts.Length != 2) {
                return "Invalid IP address format in join code.";
            }
            ipAddress = addressParts[0];
            if (!int.TryParse(addressParts[1], out port)) {
                return "Invalid port number in join code.";
            }
        }
        catch (Exception e) {
            Debug.LogError($"Failed to decode: {e.Message}");
            return "Invalid Join Code";
        }
        try {
            CustomNetworkManager.singleton2.networkAddress = ipAddress;
            NotificationScript.AddNotification(new NotificationData("Joining Game", $"Connecting to {ipAddress}:{port}...", NotificationScript.CancelOnlyButtons, CancelJoin));
            CustomNetworkManager.singleton2.GetComponent<SimpleWebTransport>().port = (ushort)port;
            CustomNetworkManager.singleton2.Init(password: password, clientOnly: true);
            CustomNetworkManager.singleton2.StartClient();
        }
        catch (Exception e) {
            Debug.LogError($"Failed to start client: {e.Message}");
            return $"Failed to connect to the server.\n{e.Message}";
        }
        return null;
    }
    public void JoinGame() {
        NetworkClient.OnErrorEvent += (error, str) => JoinGameFail(str);
        //CustomNetworkManager.singleton2.GetComponent<SimpleWebTransport>().OnClientError += (error, str) => JoinGameFail(str);
        isJoining = true;
        DidLeave = false;
        string output = JoinGameInternal(joinGameField.text);
        if (output != null) {
            JoinGameFail(output);
            return;
        }
    }
    public void CancelJoin(NotificationButton btn){
        if (btn == NotificationButton.None)
            return;
        StopJoin();
        //if (btn == NotificationButton.Cancel)
        LobbyUI.Instance.DisconnectConnection(btn == NotificationButton.Cancel);
    }
    public void JoinGameFail(string message, string title = "Join Game Failed") {
        if (!isJoining)
            return;
        if (DidLeave)
            return;
        NotificationScript.AddNotification(new NotificationData(title, message, NotificationScript.OkOnlyButtons));
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "MainMenu" && scene.isLoaded)
            //SceneManager.LoadSceneAsync("MainMenu");
            LobbyUI.Instance.BackToLobby(0f, false);
        StopJoin();
        isJoining = false;
    }
    public void StopJoin() {
        NotificationScript.DeleteNotification("Joining Game");
    }
}