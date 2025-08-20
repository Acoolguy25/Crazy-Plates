using System;
using TMPro;
using UnityEngine;

public class TextUpdater : MonoBehaviour
{
    private TextMeshProUGUI textMeshPro;
    void Awake() {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }
    public void UpdateText(string message) {
        if (!textMeshPro)
            Awake();
        textMeshPro.text = message;
    }
    public void UpdateText(float value) {
        if (!textMeshPro)
            Awake();
        textMeshPro.text = value.ToString();
    }
}
