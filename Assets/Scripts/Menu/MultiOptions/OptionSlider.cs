using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class OptionSliderData: OptionBaseData {
    public float minValue = 0f;
    public float maxValue = 1f;
    public float defaultValue = 0.5f;
}
public class OptionSlider : OptionBase {
    public Slider slider;
    public override T Get<T>(){
        if (typeof(T) == typeof(float)) {
            return (T) (object) slider.value;
        }
        Debug.LogError($"Get<{typeof(T)}> called on OptionSlider, but only float is supported.");
        return default;
    }
    public override void Set<T>(T val) {
        if (typeof(T) == typeof(float)) {
            slider.value = (float)(object)val;
            return;
        }
        Debug.LogError($"Set<{typeof(T)}> called on OptionSlider, but only float is supported.");
    }
    public override void SetEnabled(bool enabled) {
        slider.enabled = enabled;
    }
    public override void Init(OptionBaseData data) {
        Debug.Assert(slider != null, "Slider component is not assigned in OptionSlider.");
        OptionSliderData sliderData = data as OptionSliderData;
        Debug.Assert(sliderData.minValue <= sliderData.defaultValue, "Slider min > defaultValue");
        Debug.Assert(sliderData.maxValue >= sliderData.defaultValue, "Slider max < defaultValue");
        Debug.Assert(sliderData.defaultValue == Mathf.Floor(sliderData.defaultValue), "Slider defaultValue is not a whole number");
        slider.minValue = sliderData.minValue;
        slider.maxValue = sliderData.maxValue;
        slider.value = sliderData.defaultValue;
        slider.onValueChanged.Invoke(sliderData.defaultValue);
        slider.onValueChanged.AddListener((val) => onChange?.Invoke());
        base.Init(data);
    }
}
