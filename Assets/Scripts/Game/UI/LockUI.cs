using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class LockUI : LockCore
{
    private CanvasGroup canvasGroup;

    protected override void Awake() {
        canvasGroup = GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup, "LockUI requires a CanvasGroup component!");
        base.Awake();
    }
    protected override void SetLocked(bool enabled, bool started) {
        //Debug.Log($"{(enabled ? "Locked" : "Unlocked")} {gameObject.name} | {lockCount} {globalCount} {exemptions}");
        base.SetLocked(enabled, started);
        canvasGroup.interactable = !enabled;
        canvasGroup.blocksRaycasts = !enabled;
        if (enabled && EventSystem.current && EventSystem.current.currentSelectedGameObject
            && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(canvasGroup.transform))
            EventSystem.current.SetSelectedGameObject(null); // Deselect if the locked UI is currently selected
    }
}