using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[System.Serializable]
public class OptionBaseData {
    public string name;
    public string description;
    public bool isEnabled = true;
}
public class OptionBase : MonoBehaviour {
    public Action onChange;
    public Text descriptionText;
    public virtual void Set<T>(T val) {
        Debug.LogError($"Set<{val.GetType()}>() called on OptionBase");
    }
    public virtual T Get<T>() {
        Debug.LogError($"Get() called on OptionBase");
        return default;
    }
    public virtual void SetEnabled(bool enabled) {
        Debug.LogError($"SetEnabled(bool) called on OptionBase, but should be overridden in derived classes.");
    }
    public virtual void Init(OptionBaseData data) {
        descriptionText.text = data.description;
    }
}
