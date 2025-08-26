using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Windows;

public class LockControlMap : LockCore {
    public string actionName;
    private StarterAssetsInputs inputs;

    protected override void Awake() {
        if (!inputs)
            inputs = GetComponent<StarterAssetsInputs>();
        Assert.IsNotNull(inputs, "LockControlMap requires a StarterAssetsInputs component!");
        base.Awake();
    }
    protected override void SetLocked(bool enabled, bool started) {
        //Debug.Log($"{(enabled ? "Locked" : "Unlocked")} {gameObject.name} | {lockCount} {globalCount} {exemptions}");
        base.SetLocked(enabled, started);
        inputs.SetControlsEnabled(actionName, !enabled);
    }
}