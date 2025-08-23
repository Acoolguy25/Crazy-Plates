using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class OptionInputBoxData : OptionBaseData {
    public string defaultValue = "";
    public string placeholderText = "Enter text here";
}
public class OptionInputBox : OptionBase {
    public TMP_InputField inputField;
    public override T Get<T>() {
        if (typeof(T) == typeof(string) || typeof(T) == typeof(object)) {
            return (T)(object)inputField.text;
        }
        Debug.LogError($"Get<{typeof(T)}> called on OptionInputBox, but only string is supported.");
        return default;
        //return base.Get<T>();
    }
    public override void Set<T>(T val) {
        if (typeof(T) == typeof(string) || typeof(T) == typeof(object)) {
            inputField.text = (string)(object)val;
            return;
        }
        Debug.LogError($"Set<{typeof(T)}> called on OptionInputBox, but only string is supported.");
    }
    public override void SetEnabled(bool enabled) {
        inputField.interactable = enabled;
    }
    public override void Init(OptionBaseData data) {
        Debug.Assert(inputField != null, "InputField is not assigned in OptionInputBox.");
        OptionInputBoxData inputBoxData = data as OptionInputBoxData;
        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = inputBoxData.placeholderText;
        Set(inputBoxData.defaultValue);
        inputField.onValueChanged.Invoke(inputBoxData.defaultValue);
        inputField.onValueChanged.AddListener((val) => onChange?.Invoke());
        base.Init(data);
    }
}
