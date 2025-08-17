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
        textMeshPro.text = message;
    }
    public void UpdateText(float value) {
        textMeshPro.text = value.ToString();
    }
}
