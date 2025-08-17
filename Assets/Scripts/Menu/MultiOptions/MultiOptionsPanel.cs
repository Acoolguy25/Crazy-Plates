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
    public void Init(MultiOptionsPanelData multiOptionsPanelData) {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        foreach (var optionData in multiOptionsPanelData.options)
        {
            string prefabName = optionData.GetType().Name.Replace("Data", "").Replace("Option","");
            GameObject prefabClone = Resources.Load<GameObject>($"UI/MultiOptions/{prefabName}Option");
            prefabClone = Instantiate(prefabClone, content);
            OptionBase optionBase = prefabClone.GetComponent<OptionBase>();
            optionBase.Init(optionData);
            //OptionBase option = Instantiate(optionPrefabs[optionData]).GetComponent<OptionBase>();
            //option.Init(optionData);
            //option.onChange += () => Debug.Log($"{optionData.name} changed to {option.Get<float>()}");
        }
        scrollbar.value = 1f;
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
