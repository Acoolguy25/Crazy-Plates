using UnityEngine;
using System.Collections;
using System;

public class UnifiedDelay: MonoBehaviour {
    public static UnifiedDelay Instance;
    private void Awake() {
        Instance = this;
    }
    private void Start() {
        DontDestroyOnLoad(this);
    }
    public void Delay(float time, Action callback) {
        StartCoroutine(DelayCoroutine(time, callback));
    }
    private static IEnumerator DelayCoroutine(float time, Action callback) {
        if (time == 0f)
            yield return null; // Wait for one frame
        else
            yield return new WaitForSecondsRealtime(time);
        callback?.Invoke();
    }
}