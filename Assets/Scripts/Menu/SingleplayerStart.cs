using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using NUnit.Framework;

public class SingleplayerMenu : MonoBehaviour
{
    public void SingleplayerExitActivated()
    {
        LobbyUI.Instance.ChangeToPanel(null);
    }
    public void SingleplayerStartActivated()
    {
        StartCoroutine(SingleplayerStartGame());
    }
    public IEnumerator SingleplayerStartGame()
    {
        LobbyUI.Instance.AddLock();
        LobbyUI.Instance.FadeBlackScreen(1);
        yield return new WaitForSecondsRealtime(2f); // wait for screen to fade to black
        LobbyUI.Instance.SetCanvasVisibility(false); // disable everything!
        AsyncOperation op = SceneManager.LoadSceneAsync("Default", LoadSceneMode.Additive);
        yield return op;
        Scene newlyLoadedScene = SceneManager.GetSceneByName("Default");
        Assert.IsTrue(newlyLoadedScene != null && newlyLoadedScene.isLoaded);
        LobbyUI.Instance.FadeBlackScreen(0);
    }
}
