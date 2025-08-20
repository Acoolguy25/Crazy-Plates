using UnityEngine;
using Mirror;
using TMPro;

public class GameLobby : MonoBehaviour
{
    public static GameLobby singleton { get; private set; }
    public Transform GameLobbyTransform;
    public TMP_InputField gameCodeText;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        singleton = null; // Reset singleton in editor to avoid issues with reloading scenes
    }
#endif
    private void Awake() {
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
    }
    public void End(){
        playerListCore.Clear();
        MultiplayerMenu.singleton.MultiplayerChangePanel(MultiplayerMenu.singleton.StartPanel);
    }
    [Client]
    private void OnPlayersChanged() {
        OptionBaseData[] playerList = new OptionBaseData[ServerProperties.Instance.players.Count];
        foreach (PlayerData player in ServerProperties.Instance.players)
        {
            OptionPlayerListData playerListData = new OptionPlayerListData()
            {
                name = player.ipAddress,
                description = $"{player.displayName}{(player.isLocalPlayer ? " (You)" : "")}",
                isEnabled = NetworkClient.activeHost && !player.isLocalPlayer,
                playerData = player
            };
            playerList[ServerProperties.Instance.players.IndexOf(player)] = playerListData;
        }
        playerListCore.Init(new MultiOptionsPanel.MultiOptionsPanelData()
        {
            options = playerList
        });
        //foreach (PlayerData player in ServerProperties.Instance.players) {
        //    OptionPlayerList code = playerListCore.GetScript<OptionPlayerList>(player.ipAddress);
        //}
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
}
