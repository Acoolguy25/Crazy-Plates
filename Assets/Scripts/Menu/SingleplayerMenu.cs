using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Assertions;
using TMPro;
using Mirror.BouncyCastle.Bcpg;

public class SingleplayerMenu : MonoBehaviour
{
    public static SingleplayerMenu Instance;
    public TextMeshProUGUI SingleplayerTimeText;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    private void Awake() {
        Assert.IsNull(Instance, "SinglePlayerMenu is not null in Awake()");
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
        LobbyUI.LobbyLock.Lock();
        StartCoroutine(SingleplayerStartGame());
    }
    private IEnumerator SingleplayerStartGame(string sceneName = "Default")
    {
        LobbyUI.Instance.FadeBlackScreen(1);
        yield return new WaitForSecondsRealtime(2.5f); // wait for screen to fade to black
        LobbyUI.Instance.DisconnectConnection();
        LobbyUI.Instance.ResetTimeScale();
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded) {
            AsyncOperation unload_op = SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.None);
            yield return unload_op;
        }
        LobbyUI.Instance.SetCanvasVisibility(false); // disable everything!
        AsyncOperation op2 = null;
        try {
            op2 = SceneManager.UnloadSceneAsync("Default", UnloadSceneOptions.None);
        }
        catch { }
        if (op2 != null)
            yield return op2;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return op;
        Scene newlyLoadedScene = SceneManager.GetSceneByName("Default");
        Assert.IsTrue(newlyLoadedScene != null && newlyLoadedScene.isLoaded, "[SinglePlayerMenu] Scene is not loaded!");
        LobbyUI.Instance.FadeBlackScreen(0);
    }
}
