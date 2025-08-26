using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameLobby : MonoBehaviour
{
    public static GameLobby singleton { get; private set; }
    public Transform GameLobbyTransform;
    public UnityEngine.UI.Button startGameBtn, RefreshBtn;
    public TextMeshProUGUI startGameText, deleteText;
    public TextMeshProUGUI titleText;
    public TMP_InputField gameCodeText;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null; // Reset singleton in editor to avoid issues with reloading scenes
    }
#endif
    private void Awake() {
        if (singleton != null && singleton != this) {
            Destroy(gameObject);
            Destroy(this);
            return;
        }
        Debug.Assert(singleton == null, "There can only be one GameLobby instance.");
        singleton = this;
    }
    public MultiOptionsPanel playerListCore;
    [Client]
    public void Begin(){
        MultiplayerMenu.singleton.MultiplayerChangePanel(GameLobbyTransform);
        ServerProperties.Instance.players.OnChange += (a, b, c) => OnPlayersChanged();
        OnPlayersChanged();
        OnGameCodeChanged(ServerProperties.Instance.GameCode);
        deleteText.text = NetworkClient.activeHost ? "Delete Lobby" : "Leave Lobby";
        RefreshBtn.gameObject.SetActive(NetworkClient.activeHost);
        startGameBtn.gameObject.SetActive(NetworkClient.activeHost);
    }
    [Client]
    private void SetStartButton(string text = null) {
        if (text == null) {
            startGameText.text = "Start Game";
            startGameBtn.interactable = true;
        }
        else {
            startGameText.text = text;
            #if !UNITY_EDITOR
                startGameBtn.interactable = false;
            #endif
        }
    }
    public void RefreshBtns() {
        if (!NetworkClient.ready) {
            SetStartButton("Loading..");
            titleText.text = $"Multiplayer";
        }
        else if (ServerProperties.Instance.PlayerCount < GameRunner.MinPlayersMultiPlayer) {
            SetStartButton("Need Players");
            titleText.text = $"Waiting For Players ({ServerProperties.Instance.PlayerCount}/{GameRunner.MinPlayersMultiPlayer})";
        }
        else {
            SetStartButton();
            titleText.text = $"Multiplayer ({ServerProperties.Instance.PlayerCount}/{ServerProperties.Instance.MaxPlayers})";
        }
    }
    public void End(){
        playerListCore.Clear();
        if (MultiplayerMenu.singleton.CurrentPanel == MultiplayerMenu.singleton.LobbyPanel)
            MultiplayerMenu.singleton.MultiplayerChangePanel(MultiplayerMenu.singleton.StartPanel);
    }
    [Client]
    private void OnPlayersChanged() {
        OptionBaseData[] playerList = new OptionBaseData[ServerProperties.Instance.players.Count];
        foreach (uint playerIdx in ServerProperties.Instance.players)
        {
            PlayerController player = Reflection.Deserialize<PlayerController>(playerIdx);
            OptionPlayerListData playerListData = new OptionPlayerListData()
            {
                name = player.netId.ToString(),
                //description = $"{player.displayName}{(player.isLocalPlayer ? " (You)" : "")}",
                isEnabled = NetworkClient.activeHost && !player.isLocalPlayer,
                playerData = player
            };
            playerList[ServerProperties.Instance.players.IndexOf(playerIdx)] = playerListData;
        }
        playerListCore.Init(new MultiOptionsPanel.MultiOptionsPanelData()
        {
            options = playerList
        });
        //foreach (PlayerData player in ServerProperties.Instance.players) {
        //    OptionPlayerList code = playerListCore.GetScript<OptionPlayerList>(player.ipAddress);
        //}
        RefreshBtns();
    }
    [Client]
    public void OnGameCodeChanged(string gameCode) {
        gameCodeText.text = gameCode;
    }
    [Client]
    public void CopyGameCode() {
        GUIUtility.systemCopyBuffer = ServerProperties.Instance.GameCode;
    }
    [Client]
    public void ResetGameCode() {
        NotificationScript.AddNotification(new NotificationData(
            "Reset Game Code",
            "Are you sure you want to reset the game code? This will change the game code for all new players.",
            NotificationScript.YesNoButtons,
            (btn) => {
                if (btn == NotificationButton.Yes) {
                    ServerLobby.singleton.CreateNewGameCode();
                }
            }
        ));
    }
    public static LockReason startGameLock = new LockReason("StartGame", LockTag.Game);
    [Client]
    public void StartGameBtn() {
        if (!NetworkClient.ready)
            return;
        LockCore.LockAll(startGameLock);
        LobbyUI.lockedAll = true;
        ServerLobby.singleton.CmdStartGame();
    }
    [Client]
    public void LeaveGameBtn() {
        MultiplayerMenu.singleton.MultiplayerExitLobby();
    }
}
