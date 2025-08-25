using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class LockCore : MonoBehaviour {
    private static List<LockCore> instances = new();

    protected int lockCount = 0;
    public static int globalCount = 0;
    protected int exemptions = 0;
    protected bool locked = false;
    [Header("Storage Container for other classes")]
    public bool genericLock = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        instances.Clear();
        globalCount = 0;
    }
    private void ChangeLockCount(int amount, bool started = false) {
        Assert.IsTrue(exemptions >= 0, "Exemptions are negative for " + gameObject.name);
        Assert.IsTrue(lockCount >= 0, "LockCount is negative: " + lockCount.ToString() + " for " + gameObject.name);
        Assert.IsTrue(globalCount >= 0, "GlobalCount is negative: " + globalCount.ToString() + " | " + gameObject.name);
        Assert.IsTrue(amount + lockCount >= 0, "LockCount is less than amount: " + lockCount.ToString() + " < " + 
            amount.ToString() + " | " + gameObject.name);
        lockCount += amount;
        int total = lockCount + globalCount - exemptions;
        bool setLocked = total > 0;
        if (locked != setLocked || started)
            SetLocked(setLocked, started);

#if UNITY_EDITOR
        if (gameObject.name == "Panels") {
            //Debug.Log("Total Lock: " + lockCount + " | " + globalCount + " | " + exemptions);
        }
#endif
    }
    protected virtual void Awake() {
        instances.Add(this);
        ChangeLockCount(0, true);
    }
    void OnDestroy() {
        Assert.IsTrue(instances.Remove(this), "LockUI was initalized but not constructed before being destroyed!");
    }
    protected virtual void SetLocked(bool enabled, bool started) {
        locked = enabled;
    }
    public void Lock() {
        ChangeLockCount(1);
    }
    public void Unlock() {
        ChangeLockCount(-1);
    }
    public void ToggleLock(bool enabled) {
        ChangeLockCount(enabled ? 1 : -1);
    }
    public void SetLockCount(uint val) {
        ChangeLockCount((int)val - lockCount);
    }
    public void AddExemption(int val = 1) {
        exemptions += val;
        ChangeLockCount(0);
    }
    public void RemoveExemption(int val = 1) {
        exemptions -= val;
        ChangeLockCount(0);
    }
    public void ToggleExemption(bool enabled) {
        exemptions += (enabled ? 1 : -1);
        ChangeLockCount(0);
    }
    public static void LockAll() {
        globalCount++;
        foreach (LockCore instance in instances) {
            instance.ChangeLockCount(0);
        }
    }
    public static void UnlockAll() {
        globalCount--;
        foreach (LockCore instance in instances) {
            //if (instance.exemptions > 0)
                //instance.exemptions--;
            instance.ChangeLockCount(0);
        }
    }
    public static void ToggleLockAll(bool enabled) {
        globalCount += (enabled ? 1 : -1);
        foreach (LockCore instance in instances) {
            instance.ChangeLockCount(0);
        }
    }
}
