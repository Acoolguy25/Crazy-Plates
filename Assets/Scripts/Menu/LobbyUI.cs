using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Required for DOTween

public class LobbyUI : MonoBehaviour
{
    private Canvas canvas;
    public GameObject FadePanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas = GetComponent<Canvas>();
        FadePanel.GetComponent<Image>().DOFade(0, 2f);
    }

}
