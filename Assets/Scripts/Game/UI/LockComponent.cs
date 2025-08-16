using UnityEngine;

public class LockComponent : LockCore {
    public MonoBehaviour componentType;
    protected override void Awake() {
        base.Awake();
    }
    public void SetComponentType(MonoBehaviour component) {
        componentType = component;
        if (componentType == null) {
            Debug.LogError("LockComponent requires a MonoBehaviour component to lock!");
        }
    }
    protected override void SetLocked(bool enabled, bool started) {
        //Debug.Log("Set LockComponent " + (enabled ? "Locked" : "Unlocked") 
            //+ " " + gameObject.name + " | " + lockCount + " " + globalCount + " " + exemptions);
        base.SetLocked(enabled, started);
        componentType.enabled = !enabled;
    }
}