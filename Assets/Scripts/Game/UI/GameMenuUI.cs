using DG.Tweening;
using StarterAssets;
using UnityEngine;

public class GameMenuUI : MonoBehaviour
{
    public bool MenuActive { get; private set; }

    public Canvas gameCanvas;
    private Canvas menuCanvas;
    private CanvasGroup menuGroup;
    private CanvasGroup gameGroup;
    private void Start()
    {
        menuCanvas = GetComponent<Canvas>();
        menuGroup = menuCanvas.GetComponent<CanvasGroup>();
        gameGroup = gameCanvas.GetComponent<CanvasGroup>();
        MenuActive = menuGroup.alpha == 1f;
        StarterAssetsInputs.Instance.menuToggledEvent += ToggleMenu;
    }
    private void SetMenuEnabled(bool enabled, float duration = 0.5f)
    {
        if (MenuActive == enabled)
            return;
        MenuActive = enabled;
        menuGroup.DOFade(enabled? 1: 0, duration).SetUpdate(true);
        menuGroup.interactable = enabled;
        gameGroup.interactable = !enabled;
        if (ServerProperties.Instance.SinglePlayer)
        {
            Tween tween = DOTween.To(() => Time.timeScale, x => Time.timeScale = x, enabled ? 0f : 1f, duration);
            tween.SetUpdate(true);
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
}
