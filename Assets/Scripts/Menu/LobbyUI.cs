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

public class LobbyUI : MonoBehaviour
{
    public static LockUI LobbyLock;

    public static LobbyUI Instance { get; private set; }

    public GameObject[] lobbyOnlyObjects;
    public Transform FadePanel;
    public Transform Panels;
    public Transform CurrentPanel { get; private set; }

    private CanvasGroup lobbyPannelsGroup;
    static NotificationData QuitGameNotData;
    
    private Canvas canvas;
    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;

        canvas = GetComponent<Canvas>();
        lobbyPannelsGroup = Panels.GetComponent<CanvasGroup>();

        CurrentPanel = Panels.GetChild(0);
        //CurrentPanel = defaultPanel;
        foreach (Transform panel in Panels) {
            panel.gameObject.SetActive(CurrentPanel == panel);
        }
        LobbyLock = lobbyPannelsGroup.GetComponent<LockUI>();

        QuitGameNotData = new NotificationData {
            Title = "Quit Game?", 
            Message = "Are you sure that you want to quit the game?",
            Buttons = NotificationScript.YesNoButtons,
            Callback = btn => OnQuit()
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
            LobbyLock.Lock();
        }
        SetCanvasVisibility(isActiveScene);
    }
    private void Start()
    {
        CheckForSinglePlayer();
    }
    public void ChangeToPanel(Transform panel = null) {
        if (panel == null)
            panel = Panels.GetChild(0);
        CurrentPanel.gameObject.SetActive(false);
        panel.gameObject.SetActive(true);
        CurrentPanel = panel;
    }
    public void FadeBlackScreen(float alpha, float duration = 2f, bool start = false)
    {
        Image fadePanelGroup = FadePanel.GetComponent<Image>();
        GenericTweens.TweenImage(fadePanelGroup, alpha, duration);
        if (!start)
            LockCore.ToggleLockAll(alpha == 1f); // Lock the lobby when fading to black

        //FadePanel.GetComponent<Image>().CrossFadeAlpha(alpha, duration, true);
    }
    public void SetCanvasVisibility(bool enabled)
    {
        lobbyPannelsGroup.alpha = enabled? 1: 0;
        foreach (var obj in lobbyOnlyObjects)
        {
            obj.SetActive(enabled);
        }
    }
    public void BackToLobby(bool Instant = false)
    {
        StartCoroutine(_BackToLobby(Instant));
    }
    public void DisconnectConnection() {
        if (NetworkServer.active && NetworkClient.isConnected) {
            NetworkManager.singleton.StopHost(); // Host mode
        }
        else if (NetworkServer.active) {
            NetworkManager.singleton.StopServer(); // Dedicated server
        }
        else if (NetworkClient.isConnected) {
            NetworkManager.singleton.StopClient(); // Client only
        }
    }
    public void TweenTimeScale(float newTimeScale, float duration) {
        DOTween.Kill(Time.timeScale);
        if (duration > 0) {
            Tween tween = DOTween.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, duration);
            tween.SetEase(Ease.OutQuad);
            tween.SetUpdate(true);
            tween.SetTarget(Time.timeScale);
        }
        else {
            Time.timeScale= newTimeScale;
        }
    }
    public void ResetTimeScale() {
        TweenTimeScale(1f, 0f);
    }
    private IEnumerator _BackToLobby(bool Instant)
    {
        if (GameMenuUI.Instance)
            GameMenuUI.Instance.DisableAllUI();

        if (!Instant)
        {
            LobbyUI.Instance.FadeBlackScreen(1);
            yield return new WaitForSecondsRealtime(2f); // wait for screen to fade to black
        }
        DisconnectConnection();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene != gameObject.scene && scene.isLoaded)
            {
                var op = SceneManager.UnloadSceneAsync(scene);
                yield return op;
            }
        }

        LobbyLock.Unlock();
        ResetTimeScale();
        LobbyUI.Instance.SetCanvasVisibility(true); // re-enable everything!
        if (!Instant)
        {
            LobbyUI.Instance.FadeBlackScreen(0);
            yield return new WaitForSeconds(2f);
        }
    }
    public void QuitGameButton() {
        NotificationScript.AddNotification(QuitGameNotData);
    }
    public void OnQuit() {
        Debug.Log("User has quit the game");
        Application.Quit(0);
    }
}
