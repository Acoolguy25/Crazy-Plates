using DG.Tweening; // Required for DOTween
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }

    private Canvas canvas;
    public Transform FadePanel;
    public Transform Panels;
    public Transform CurrentPanel { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas = GetComponent<Canvas>();
        FadePanel.GetComponent<Image>().color = new Color(0, 0, 0, 1); // Set initial color to black with full opacity
        FadePanel.GetComponent<Image>().CrossFadeAlpha(0f, 2f, true);
        CurrentPanel = Panels.GetChild(0);
    }
    public void ChangeToPanel(Transform panel) {
        CurrentPanel.gameObject.SetActive(false);
        panel.gameObject.SetActive(true);
        CurrentPanel = panel;
    }
}
