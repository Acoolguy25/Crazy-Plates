using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MultiOptionsPanel : MonoBehaviour
{
    [System.Serializable]
    public class MultiOptionsPanelData {
        public OptionBaseData[] options;
    }
    public Transform content;
    public Scrollbar scrollbar;
    private readonly Dictionary<string, OptionBase> current_options = new();
    private RectTransform contentRectTransform;
    private RectTransform windowRectTransform;

    float totalHeight = 0;
    private void Awake() {
        contentRectTransform = content.GetComponent<RectTransform>();
        windowRectTransform = transform.parent.GetComponent<RectTransform>();
        scrollbar.value = 1f;
        OnRectTransformDimensionsChange();
    }
    private void _Clear() {
        totalHeight = 0;
        foreach (Transform child in content) {
            Destroy(child.gameObject);
        }
        current_options.Clear();
    }
    public void Clear() { // Public facing clear
        _Clear();
        OnRectTransformDimensionsChange();
    }
    public void Init(MultiOptionsPanelData multiOptionsPanelData) {
        _Clear();
        foreach (var optionData in multiOptionsPanelData.options)
        {
            string prefabName = optionData.GetType().Name.Replace("Data", "").Replace("Option","");
            GameObject prefabClone = Resources.Load<GameObject>($"UI/MultiOptions/{prefabName}Option");
            prefabClone = Instantiate(prefabClone, content);
            prefabClone.name = optionData.name;
            OptionBase optionBase = prefabClone.GetComponent<OptionBase>();
            optionBase.Init(optionData);
            Debug.Assert(!current_options.ContainsKey(optionData.name), "MultiOptionsPanelData Duplicate Name: " + optionData.name);
            current_options[optionData.name] = optionBase;
            //OptionBase option = Instantiate(optionPrefabs[optionData]).GetComponent<OptionBase>();
            //option.Init(optionData);
            //option.onChange += () => Debug.Log($"{optionData.name} changed to {option.Get<float>()}");
            totalHeight += prefabClone.GetComponent<RectTransform>().sizeDelta.y;
        }
        OnRectTransformDimensionsChange();
        scrollbar.value = 1f;
    }
    public T GetScript<T>(string optionName) {
        T optionBase = current_options[optionName].GetComponent<T>();
        if (optionBase == null) {
            Debug.LogError($"Option with name {optionName} does not have an OptionBase component.");
            return default;
        }
        return optionBase;
    }
    private void OnRectTransformDimensionsChange() {
        if (!didAwake)
            return;
        contentRectTransform.sizeDelta = new Vector2(
            contentRectTransform.sizeDelta.x, Math.Max(totalHeight, windowRectTransform.rect.height));
    }
    //private void Start() {
    //    // Debug start
    //    Init(new MultiOptionsPanelData()
    //    {
    //        options = new OptionBaseData[]
    //        {
    //            new OptionSliderData()
    //            {
    //                name = "Volume",
    //                description = "Adjust the volume level",
    //                minValue = 0f,
    //                maxValue = 1f,
    //                defaultValue = 0f,
    //            },
    //            new OptionToggleData()
    //            {
    //                name = "Fullscreen",
    //                description = "Enable or disable fullscreen mode",
    //            },
    //        }
    //    });
    //}
}
