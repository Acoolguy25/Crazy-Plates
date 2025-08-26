using DG.Tweening; // Required for DOTween
using Mirror;
using Mirror.BouncyCastle.Bcpg;
using Newtonsoft.Json.Serialization;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class LobbyUI : MonoBehaviour {
    public static LockUI LobbyLock;

    public static LobbyUI Instance { get; private set; } = null;

    [SerializeField]
    private static GameObject[] lobbyOnlyObjects;
    [SerializeField]
    private static Transform FadePanel;
    [SerializeField]
    private static Transform Panels;
    [SerializeField]
    public static Transform CurrentPanel { get; private set; }
    [SerializeField]
    private static CanvasGroup lobbyPannelsGroup;
    private static NotificationData QuitGameNotData;
    private static Coroutine backToLobbyCoroutine = null;
    private static LockReason LobbyUILockReason = new LockReason("LobbyLock", LockTag.Lobby);
    public static bool lockedAll;

    private Canvas canvas;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    private void Awake() {
        Debug.Assert(transform.parent == null, "LobbyUI must be a root object in the scene!");
        if (Instance != null && Instance != this) {
            //Debug.LogError("Deleting duplicate LobbyUI");
            Destroy(gameObject); // Ensure only one instance
            Destroy(this);
            return;
        }
        Assert.IsNull(Instance, "LobbyUI is not null in Awake()");
        Instance = this;
        

        canvas = GetComponent<Canvas>();
        Panels = transform.GetChild(0);
        lobbyPannelsGroup = Panels.GetComponent<CanvasGroup>();
        FadePanel = transform.GetChild(transform.childCount - 1);

        CurrentPanel = Panels.GetChild(0);
        //CurrentPanel = defaultPanel;
        foreach (Transform panel in Panels) {
            panel.gameObject.SetActive(CurrentPanel == panel);
        }
        LobbyLock = lobbyPannelsGroup.GetComponent<LockUI>();

        QuitGameNotData = new NotificationData
        {
            Title = "Quit Game?",
            Message = "Are you sure that you want to quit the game?",
            Buttons = NotificationScript.YesNoButtons,
            Callback = btn =>
            {
                if (btn == NotificationButton.Yes)
                    OnQuit();
            }
        };
    }
    private void CheckForSinglePlayer() {
        bool isActiveScene = !SceneManager.GetSceneByName("Default").isLoaded;
        if (isActiveScene) {
            FadePanel.GetComponent<Image>().color = new Color(0, 0, 0, 1); // Set initial color to black with full opacity
            FadeBlackScreen(0f, start: true);
        }
        else {
            FadeBlackScreen(0f, duration: 0f, start: true);
            //LobbyLock.Lock();
        }
        SetCanvasVisibility(isActiveScene, true);
    }
    private void Start() {
        DontDestroyOnLoad(gameObject);
        //SceneManager.sceneLoaded += OnSceneLoaded;
        //SceneManager.sceneUnloaded += OnSceneUnloaded;
        CheckForSinglePlayer();
    }
    public void ChangeToPanel(Transform panel = null) {
        if (panel == null)
            panel = Panels.GetChild(0);
        CurrentPanel.gameObject.SetActive(false);
        panel.gameObject.SetActive(true);
        CurrentPanel = panel;
    }
    private static LockReason FadeScreenReason = new LockReason("FadeScreen", LockTag.Lobby);
    public void FadeBlackScreen(float alpha, float duration = 2f, bool start = false) {
        Image fadePanelGroup = FadePanel.GetComponent<Image>();
        GenericTweens.TweenImage(fadePanelGroup, alpha, duration);
        if (!start)
            LockCore.ToggleLockAll(FadeScreenReason, alpha == 1f); // Lock the lobby when fading to black

        //FadePanel.GetComponent<Image>().CrossFadeAlpha(alpha, duration, true);
    }
    public void SetCanvasVisibility(bool enabled, bool started = false) {
        lobbyPannelsGroup.alpha = enabled ? 1 : 0;
        //foreach (var obj in lobbyOnlyObjects) {
        //obj.SetActive(enabled);
        //}
        //if (!started)
        //    LobbyLock.ToggleLock(!enabled);
        LobbyLock.ToggleLock(LobbyUILockReason, !enabled);
    }
    public void BackToLobby(float duration = 2f, bool disconnect = true, string loadScene = "MainMenu") {
        if (backToLobbyCoroutine != null)
            return;
        backToLobbyCoroutine = StartCoroutine(_BackToLobby(duration, disconnect, loadScene));
    }
    public void DisconnectConnection(bool LeaveWillingly = true) {
        if (LeaveWillingly)
            LobbyJoin.DidLeave = true;
        Debug.Log("Disconnecting from server...");
        if (NetworkClient.active && NetworkManager.networkSceneName != "MainMenu" && NetworkManager.networkSceneName != "") {
            SetCanvasVisibility(true);
        }
        if (NetworkServer.active && NetworkClient.isConnected) {
            NetworkManager.singleton.StopHost(); // Host mode
        }
        else if (NetworkServer.active) {
            NetworkManager.singleton.StopServer(); // Dedicated server
        }
        else if (NetworkClient.isConnected || NetworkClient.isConnecting) {
            NetworkManager.singleton.StopClient(); // Client only
        }
    }
    public static void TweenTimeScale(float newTimeScale, float duration) {
        DOTween.Kill(Time.timeScale);
        if (duration > 0) {
            Tween tween = DOTween.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, duration);
            tween.SetEase(Ease.OutQuad);
            tween.SetUpdate(true);
            tween.SetTarget(Time.timeScale);
        }
        else {
            Time.timeScale = newTimeScale;
        }
    }
    public void ResetTimeScale() {
        TweenTimeScale(1f, 0f);
    }
    private IEnumerator _BackToLobby(float duration, bool disconnect, string loadScene) {
        if (GameMenuUI.Instance)
            GameMenuUI.Instance.DisableAllUI();

        if (duration > 0f) {
            if (duration > 3f) {
                yield return new WaitForSecondsRealtime(duration - 3f);
                duration = 3f;
            }
            LobbyUI.Instance.FadeBlackScreen(1, duration);
            yield return new WaitForSecondsRealtime(duration); // wait for screen to fade to black
        }
        if (disconnect)
            DisconnectConnection();
        //for (int i = 0; i < SceneManager.sceneCount; i++) {
        //    Scene scene = SceneManager.GetSceneAt(i);
        //    if (loadScene != null && scene.name != loadScene && scene.isLoaded) {
        //        var op = SceneManager.UnloadSceneAsync(scene);
        //        yield return op;
        //    }
        //}
        if (loadScene != null) {
            Scene menuScene = SceneManager.GetSceneByName(loadScene);
            if (!menuScene.isLoaded) {
                AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(loadScene);
                yield return asyncOperation;
            }
        }

        CameraController.Instance.SetActiveCamera("Orbit");
        //LobbyLock.Unlock();
        ResetTimeScale();
        LockCore.UnlockTag(LockTag.Game);
        LockCore.UnlockTag(LockTag.Menu);
        SetCanvasVisibility(true); // re-enable everything!
        if (duration > 0f && loadScene != null) {
            LobbyUI.Instance.FadeBlackScreen(0);
            yield return new WaitForSeconds(2f);
        }
        backToLobbyCoroutine = null;
    }
    [Client]
    public void GameStartingFunc() {
        FadeBlackScreen(1f);
        if (lockedAll)
            LockCore.UnlockAll(GameLobby.startGameLock);
    }
    public void QuitGameButton() {
        NotificationScript.AddNotification(QuitGameNotData);
    }
    public void OnQuit() {
        Debug.Log("User has quit the game");
        FadeBlackScreen(1f, 0f);
        Application.Quit(0);
    }
    //public void OnSceneLoaded(Scene scene, LoadSceneMode mode){
    //    if (scene.name != "MainMenu")
    //        SetCanvasVisibility(false);
    //    //FadeBlackScreen(0f);
    //}
    //public void OnSceneUnloaded(Scene scene) {
    //    if (scene.name != "MainMenu")
    //        SetCanvasVisibility(true);
    //}
}
