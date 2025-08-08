using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Assertions;

public class SingleplayerMenu : MonoBehaviour
{
    public static SingleplayerMenu Instance;
    private void Awake() {
        Assert.IsNull(Instance);
        Instance = this;
    }
    public void SingleplayerExitActivated()
    {
        LobbyUI.Instance.ChangeToPanel(null);
    }
    public void SingleplayerStartActivated()
    {
        LobbyUI.Instance.AddLock();
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
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return op;
        Scene newlyLoadedScene = SceneManager.GetSceneByName("Default");
        Assert.IsTrue(newlyLoadedScene != null && newlyLoadedScene.isLoaded);
        LobbyUI.Instance.FadeBlackScreen(0);
    }
}
