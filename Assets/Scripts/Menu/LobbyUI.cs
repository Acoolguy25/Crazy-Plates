using DG.Tweening; // Required for DOTween
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
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
    void Start()
    {
        canvas = GetComponent<Canvas>();
        lobbyPannelsGroup = Panels.GetComponent<CanvasGroup>();

        CurrentPanel = defaultPanel = Panels.GetChild(0);
        //CurrentPanel = defaultPanel;
        foreach (Transform panel in Panels)
        {
            panel.gameObject.SetActive(CurrentPanel == panel);
        }

        FadePanel.GetComponent<Image>().color = new Color(0, 0, 0, 1); // Set initial color to black with full opacity
        FadeBlackScreen(0);
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
        FadePanel.GetComponent<Image>().CrossFadeAlpha(alpha, duration, true);
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
        if (UILocked == enabled)
            return;
        lobbyPannelsGroup.interactable = !enabled;
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
}
