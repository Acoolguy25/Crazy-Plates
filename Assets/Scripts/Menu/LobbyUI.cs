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

public class LobbyUI : MonoBehaviour
{
    private int UILockCount = 0;
    public bool UILocked => (UILockCount > 0);

    public static LobbyUI Instance { get; private set; }

    public GameObject[] lobbyOnlyObjects;
    public Transform FadePanel;
    public Transform Panels;
    public Transform CurrentPanel { get; private set; }

    private Transform defaultPanel;
    private CanvasGroup lobbyPannelsGroup;
    private Canvas canvas;
    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
    private void CheckForSinglePlayer() {
        bool isActiveScene = !SceneManager.GetSceneByName("Default").isLoaded;
        if (isActiveScene) {
            FadePanel.GetComponent<Image>().color = new Color(0, 0, 0, 1); // Set initial color to black with full opacity
            FadeBlackScreen(0f);
        }
        else {
            FadeBlackScreen(0f, 0f);
            AddLock();
        }
        SetCanvasVisibility(isActiveScene);
    }
    private void Start()
    {
        canvas = GetComponent<Canvas>();
        lobbyPannelsGroup = Panels.GetComponent<CanvasGroup>();

        CurrentPanel = defaultPanel = Panels.GetChild(0);
        //CurrentPanel = defaultPanel;
        foreach (Transform panel in Panels)
        {
            panel.gameObject.SetActive(CurrentPanel == panel);
        }
        CheckForSinglePlayer();
    }
    public void ChangeToPanel(Transform panel = null) {
        if (panel == null)
            panel = Panels.GetChild(0);
        CurrentPanel.gameObject.SetActive(false);
        panel.gameObject.SetActive(true);
        CurrentPanel = panel;
    }
    public void FadeBlackScreen(float alpha, float duration = 2f)
    {
        DOTween.Kill(FadePanel);
        Tween tween = FadePanel.GetComponent<Image>().DOFade(alpha, duration);
        tween.SetTarget(FadePanel);
        tween.SetUpdate(true);
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
    private void _SetUILocked(bool enabled)
    {
        if (UILocked == enabled) {
            if (enabled)
                Assert.IsTrue(!lobbyPannelsGroup.interactable, "Lobby Pannel was set to interactable through foreign agent!");
            return;
        }
        lobbyPannelsGroup.interactable = lobbyPannelsGroup.blocksRaycasts = !enabled;
    }
    public void AddLock()
    {
        Assert.IsTrue(UILockCount >= 0);
        _SetUILocked(true);
        UILockCount++;
    }
    public void RemoveLock()
    {
        Assert.IsTrue(UILockCount >= 1);
        _SetUILocked(false);
        UILockCount--;
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

        LobbyUI.Instance.RemoveLock();
        ResetTimeScale();
        LobbyUI.Instance.SetCanvasVisibility(true); // re-enable everything!
        if (!Instant)
        {
            LobbyUI.Instance.FadeBlackScreen(0);
            yield return new WaitForSeconds(2f);
        }
    }
}
