using Mirror;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class MultiplayerMenu : MonoBehaviour
{
    public Transform StartPanel, CreatePanel, JoinPanel, LobbyPanel;
    public Transform OptionsPanel;
    public Transform CurrentPanel;
    public ServerProperties serverProperties;
    private MultiOptionsPanel scriptOptions;
    public static MultiplayerMenu singleton;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null; // Reset singleton in editor to avoid issues with reloading scenes
    }
#endif
    private void Awake() {
        Debug.Assert(singleton == null, "There can only be one MultiplayerMenu instance.");
        singleton = this;
    }
    public void MultiplayerExitActivated() {
        LobbyUI.Instance.ChangeToPanel(null);
        MultiplayerChangePanel(StartPanel);
    }
    public void MultiplayerChangePanel(Transform panel) {
        CurrentPanel = panel;
        foreach (Transform child in StartPanel.parent) {
            child.gameObject.SetActive(panel == child);
        }
        OptionsPanel.gameObject.SetActive(panel == CreatePanel);
    }
    public void MultiplayerCreateLobby() {
        LockCore.LockAll();

        try {
            CustomNetworkManager.singleton2.Init(scriptOptions.GetKeyValuePairs(), singleplayer: false);
            CustomNetworkManager.singleton2.StartHost();
            MultiplayerChangePanel(LobbyPanel);
        }
        catch (Exception e) {
            Debug.Log($"Failed to start game: {e.Message}");
            LobbyUI.Instance.DisconnectConnection(true);
            NotificationScript.AddNotification(new NotificationData("Create Server Failed",
                $"Failed To Create Server: {e.Message}", NotificationScript.OkOnlyButtons));
        }

        LockCore.UnlockAll();
    }
    public void MultiplayerLeaveLobby() {
        NotificationData leaveLobbyNotiData = new NotificationData("Leave Lobby?",
        "Are you sure you want to leave the lobby?", NotificationScript.YesNoButtons, OnLeaveLobby);
        NotificationScript.AddNotification(leaveLobbyNotiData);
    }
    public void MultiplayerDeleteLobby() {
        if (!NetworkClient.activeHost) {
            MultiplayerLeaveLobby();
            return;
        }
        NotificationData deleteLobbyNotiData = new NotificationData("Delete Lobby?",
        "Are you sure you want to delete the lobby?", NotificationScript.YesNoButtons, OnLeaveLobby);
        NotificationScript.AddNotification(deleteLobbyNotiData);
    }
    public void OnLeaveLobby(NotificationButton btn) {
        if (btn == NotificationButton.Yes) {
            LobbyUI.Instance.DisconnectConnection(true);
            MultiplayerChangePanel(StartPanel);
        }
    }
    private void Start() {
        scriptOptions = OptionsPanel.GetComponentInChildren<MultiOptionsPanel>();
        scriptOptions.Init(new MultiOptionsPanel.MultiOptionsPanelData()
        {
            options = new OptionBaseData[]
            {
                new OptionInputBoxData()
                {
                    name = "ServerIP",
                    description = "Server IP Address",
                    defaultValue = CustomBasicAuthenticator.GetLocalIPAddress(),
                    placeholderText = "Enter IP Address",
                },
                new OptionInputBoxData()
                {
                    name = "ServerPort",
                    description = "Server IP Port",
                    defaultValue = "27777",
                    placeholderText = "Enter port number",
                },
                new OptionSliderData()
                {
                    name = "MaxPlayers",
                    description = "Maximum Players",
                    minValue = 2f,
                    maxValue = 9f,
                    defaultValue = 5f,
                },
                new OptionToggleData()
                {
                    name = "LANOnly",
                    description = "LAN Only",
                    defaultValue = false,
                },
            }
        });
        MultiplayerChangePanel(StartPanel);
    }
}
