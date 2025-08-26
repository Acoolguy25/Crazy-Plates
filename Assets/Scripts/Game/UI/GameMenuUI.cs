using DG.Tweening;
using Mirror;
using StarterAssets;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class GameMenuUI : MonoBehaviour
{
    public static GameMenuUI Instance;
    public bool MenuActive { get; private set; }

    public Canvas gameCanvas;
    private Canvas menuCanvas;
    private CanvasGroup menuGroup;
    //private CanvasGroup gameGroup;
    private LockUI gameLock;
    private LockUI menuLock;
    //private LockControlMap menuControlMapLock;
    private LockReason MenuLockReason = new LockReason("Menu", LockTag.Menu);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }

    private void Awake()
    {
        Assert.IsNull(Instance, "GameMenuUI is not null in Awake()");
        Instance = this;

        menuCanvas = GetComponent<Canvas>();

        menuGroup = menuCanvas.GetComponent<CanvasGroup>();
        gameLock = gameCanvas.GetComponent<LockUI>();

        menuLock = menuGroup.GetComponent<LockUI>();
        //menuControlMapLock = StarterAssetsInputs.Instance.AddComponent<LockControlMap>();
        //menuControlMapLock.actionName = "Menu";
        MenuActive = false; //menuGroup.alpha == 1f;
    }
    private void Start() {
        StarterAssetsInputs.Instance.menuToggledEvent += ToggleMenu;
        MenuActive = !MenuActive;
        SetMenuEnabled(!MenuActive, 0f); // Start with menu disabled
        //SetMenuEnabled(false, 0f);
    }
    private void OnDestroy() {
        Instance = null;
    }
    private void SetMenuEnabled(bool enabled, float duration = 0.5f)
    {
        if (MenuActive == enabled)
            return;
        MenuActive = enabled;
        menuGroup.DOKill();
        GenericTweens.TweenCanvasGroup(menuGroup, enabled ? 1 : 0, duration, menuLock);
        if (duration != 0f) {
            gameLock.ToggleLock(MenuLockReason, enabled);
            //menuControlMapLock.ToggleLock(MenuLockReason, !enabled);
        }
        //menuGroup.DOFade(enabled? 1: 0, duration).SetUpdate(true);
        //menuGroup.interactable = menuGroup.blocksRaycasts = enabled;
        //gameGroup.interactable = gameGroup.blocksRaycasts = !enabled;
        if (ServerProperties.Instance.SinglePlayer)
        {
            LobbyUI.TweenTimeScale(enabled ? 0f : 1f, duration);
        }
    }
    public void CloseMenu()
    {
        SetMenuEnabled(false);
    }
    public void OpenMenu()
    {
        SetMenuEnabled(true);
    }
    public void ToggleMenu()
    {
        SetMenuEnabled(!MenuActive);
    }
    public void DisableAllUI(bool includeMenuOnSinglePlayer = false)
    {
        //menuGroup.interactable = gameGroup.interactable = false;
        if (ServerProperties.Instance.SinglePlayer || includeMenuOnSinglePlayer)
            menuLock.Lock();
        gameLock.Lock();
    }
    public void QuitGame(bool Instant = false)
    {
        Assert.IsNotNull(LobbyUI.Instance, "QuitGame called but LobbyUI undefined");
        menuLock.Lock();
        SetMenuEnabled(false);
        if (Instant)
            LobbyUI.Instance.BackToLobby(0f);
        else
            LobbyUI.Instance.BackToLobby();
    }
    private void OnApplicationPause(bool pause) {
        if (pause && StarterAssetsInputs.Instance.GetControlsEnabled("Menu"))
            SetMenuEnabled(true, 0f); // instantly enable menu!
    }
}
