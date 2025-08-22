using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[System.Serializable]
public class OptionToggleData : OptionBaseData {
    public bool defaultValue = true;
}
public class OptionToggle : OptionBase {
    public UnityEngine.UI.Toggle toggle;
    public override T Get<T>() {
        if (typeof(T) == typeof(bool) || typeof(T) == typeof(object)) {
            return (T)(object)toggle.isOn;
        }
        Debug.LogError($"Get<{typeof(T)}> called on OptionToggle, but only bool is supported.");
        return default;
    }
    public override void Set<T>(T val) {
        if (typeof(T) == typeof(bool)) {
            toggle.isOn = (bool)(object)val;
            return;
        }
        Debug.LogError($"Set<{typeof(T)}> called on OptionToggle, but only bool is supported.");
    }
    public override void SetEnabled(bool enabled) {
        toggle.isOn = enabled;
    }
    public override void Init(OptionBaseData data) {
        Debug.Assert(toggle != null, "Toggle component is not assigned in OptionToggle.");
        OptionToggleData toggleData = data as OptionToggleData;
        SetEnabled(toggleData.defaultValue);
        toggle.onValueChanged.Invoke(toggleData.defaultValue);
        toggle.onValueChanged.AddListener((val) => onChange?.Invoke());
        base.Init(data);
    }
}