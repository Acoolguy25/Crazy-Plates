using DG.Tweening;
using UnityEngine;

public class GameMenuUI : MonoBehaviour
{
    public bool MenuActive { get; private set; }

    public Canvas gameCanvas;
    private Canvas menuCanvas;
    private CanvasGroup menuGroup;
    private void Start()
    {
        menuCanvas = GetComponent<Canvas>();
        menuGroup = GetComponent<CanvasGroup>();
        MenuActive = menuGroup.alpha == 1f;
    }
    private void SetMenuEnabled(bool enabled, float duration = 0.5f)
    {
        if (MenuActive == enabled)
            return;
        MenuActive = enabled;
        gameCanvas.GetComponent<CanvasGroup>().interactable = !enabled;
        menuGroup.DOFade(enabled? 0: 1, duration);
        if (ServerProperties.Instance.SinglePlayer)
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, enabled? 1f: 0f, duration);
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
    public void OnToggleMenu()
    {
        ToggleMenu();
    }
}
