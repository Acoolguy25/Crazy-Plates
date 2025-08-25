using StarterAssets;
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

public class DeathUI : MonoBehaviour
{
    public static DeathUI Instance;

    private GameCanvasElems _gameCanvasElements;
    private GameCanvasMain gameCanvasMain;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    private void Awake() {
        Assert.IsNull(Instance, "DeathUI is not null in Awake");
        Instance = this;
    }
    private void Start() {
        gameCanvasMain = GameCanvasMain.Instance;
        _gameCanvasElements = gameCanvasMain.GetComponent<GameCanvasElems>();
    }
    public IEnumerator PlayerDied() {
        Assert.IsNotNull(LobbyUI.Instance, "Lobby Scene is not loaded!");

        CameraController.Instance.SetActiveCamera("Death");
        if (ServerProperties.Instance.SinglePlayer) {
            gameCanvasMain.enabled = false;
            StarterAssetsInputs.Instance.SetControlsEnabled("Menu", false);
            _gameCanvasElements.defaultGroup.alpha = 1f;
            _gameCanvasElements.deathGroup.alpha = 0f;
            gameCanvasMain.SetCanvasGroup(null, 2f);

            LobbyUI.TweenTimeScale(0f, 6f);

            yield return new WaitForSecondsRealtime(4f);
            if (_gameCanvasElements != null && _gameCanvasElements.deathGroup != null)
                gameCanvasMain.SetCanvasGroup(_gameCanvasElements.deathGroup, 1.8f);
        }
        else {
            _gameCanvasElements.GetComponent<LockUI>().Lock();
        }
        yield return null;
    }
    public void SinglePlayer_RetryGame() {
        _gameCanvasElements.deathGroup.interactable = false;
        GameMenuUI.Instance.DisableAllUI();

        LobbyUI.LobbyLock.Unlock();
        SingleplayerMenu.Instance.SingleplayerStartActivated();
    }
    public void BackToLobby() {
        LobbyUI.Instance.BackToLobby();
    }
}
