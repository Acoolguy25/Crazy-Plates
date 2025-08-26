using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Assertions;
using TMPro;
using Mirror.BouncyCastle.Bcpg;
using Mirror;

public class SingleplayerMenu : MonoBehaviour
{
    public static SingleplayerMenu Instance = null;
    public TextMeshProUGUI SingleplayerTimeText;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            Destroy(this);
            return;
        }
        //Assert.IsNull(Instance, "SinglePlayerMenu is not null in Awake()");
        Instance = this;
    }
    public void UpdateSinglePlayerTime(double newTime) {
        SingleplayerTimeText.text = "Best Time: " + SingleplayerTimeGUI.DisplayTimePassed(newTime);
    }
    public void SingleplayerExitActivated()
    {
        LobbyUI.Instance.ChangeToPanel(null);
    }
    public void SingleplayerStartActivated()
    {
        if (NetworkClient.active)
            return;
        LockCore.LockAll(GameLobby.startGameLock);
        LobbyUI.lockedAll = true;
        CustomNetworkManager.singleton2.Init(singleplayer: true, clientOnly: true);
        CustomNetworkManager.singleton2.StartHost();
        //StartCoroutine(SingleplayerStartGame());
    }
    //private IEnumerator SingleplayerStartGame(string sceneName = "Default")
    //{
    //    LobbyUI.Instance.FadeBlackScreen(1);
    //    yield return new WaitForSecondsRealtime(2.5f); // wait for screen to fade to black
    //    LobbyUI.Instance.DisconnectConnection();
    //    LobbyUI.Instance.ResetTimeScale();
    //    Scene scene = SceneManager.GetSceneByName(sceneName);
    //    if (scene.isLoaded) {
    //        AsyncOperation unload_op = SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.None);
    //        yield return unload_op;
    //    }
    //     // enable everything!
    //    AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
    //    yield return op;
    //    Scene newlyLoadedScene = SceneManager.GetSceneByName(sceneName);
    //    Assert.IsTrue(newlyLoadedScene != null && newlyLoadedScene.isLoaded, "[SinglePlayerMenu] Scene is not loaded!");
    //    LobbyUI.Instance.FadeBlackScreen(0);
    //}
}