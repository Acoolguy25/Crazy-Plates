using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

[System.Flags]
public enum LockTag {
    None = 0,
    Menu = 1 << 0,
    Game = 1 << 1,
    Lobby = 1 << 2,
}
public struct LockReason {
    public string name;
    public LockTag category;
    public static bool operator ==(LockReason first, LockReason second) {
        return first.name == second.name && first.category == second.category;
    }
    public static bool operator !=(LockReason first, LockReason second) {
        return !(first == second);
    }
    public override bool Equals(object obj) {
        if (!(obj is LockReason))
            return false;

        var other = (LockReason)obj;
        return this == other;
    }

    public override int GetHashCode() {
        return HashCode.Combine(name, category);
    }
    public LockReason(string name_, LockTag category_) {
        this.name = name_;
        this.category = category_;
    }
}

public class LockCore : MonoBehaviour {
    private static List<LockCore> instances = new();

    protected int lockCount = 0;
    protected List<LockReason> additionalReasons = new();
    protected static List<LockReason> globalReasons = new();
    public static int globalCount => globalReasons.Count;
    protected int exemptions = 0;
    protected bool locked = false;
    [Header("Storage Container for other classes")]
    public bool genericLock = false;

    

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        instances.Clear();
        globalReasons.Clear();
        //globalCount = 0;
    }
#if UNITY_EDITOR
    private string DebugShowListReasons(List<LockReason> reasons) {
        List<string> ret = new();
        foreach (LockReason reason in reasons) {
            ret.Add(reason.name);
        }
        return string.Join(", ", ret);
    }
#endif
    private void ChangeLockCount(int amount, bool started = false) {
        Assert.IsTrue(exemptions >= 0, "Exemptions are negative for " + gameObject.name);
        Assert.IsTrue(lockCount >= 0, "LockCount is negative: " + lockCount.ToString() + " for " + gameObject.name);
        Assert.IsTrue(globalCount >= 0, "GlobalCount is negative: " + globalCount.ToString() + " | " + gameObject.name);
        Assert.IsTrue(amount + lockCount >= 0, "LockCount is less than amount: " + lockCount.ToString() + " < " + 
            amount.ToString() + " | " + gameObject.name);
        lockCount += amount;
        int total = lockCount + additionalReasons.Count + globalCount - exemptions;
        bool setLocked = total > 0;
        if (locked != setLocked || started)
            SetLocked(setLocked, started);

#if UNITY_EDITOR
        if (gameObject.name == "Panels") {
            //Debug.Log("Total Lock: " + lockCount + " | " + globalCount + " | " + exemptions + " | " + DebugShowListReasons(globalReasons) + " | " + DebugShowListReasons(additionalReasons));
        }
#endif
    }
    protected virtual void Awake() {
        instances.Add(this);
        ChangeLockCount(0, true);
    }
    void OnDestroy() {
        Assert.IsTrue(instances.Remove(this), "LockUI was initalized but not constructed before being destroyed!");
        SetLocked(false, false);
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
    public void Lock(LockReason reason) {
        if (additionalReasons.IndexOf(reason) != -1) {
            //Debug.LogWarning($"Reason {reason.name} arleady Locked!");
            return;
        }
        additionalReasons.Add(reason);
        ChangeLockCount(0);
    }
    public void Unlock(LockReason reason) {
        int idx = additionalReasons.IndexOf(reason);
        if (idx == -1) {
            //Debug.LogWarning($"Reason {reason.name} tried to Unlock without first locking!");
            return;
        }
        additionalReasons.RemoveAt(idx);
        ChangeLockCount(0);
    }
    public void ToggleLock(LockReason reason, bool enabled) {
        if (enabled)
            Lock(reason);
        else
            Unlock(reason);
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
    public static void LockAll(LockReason reason) {
        if (globalReasons.IndexOf(reason) != -1) {
            //Debug.LogWarning($"Reason {reason.name} already LockedAll!");
            return;
        }
        //globalCount++;
        globalReasons.Add(reason);
        foreach (LockCore instance in instances) {
            instance.ChangeLockCount(0);
        }
    }
    public static void UnlockAll(LockReason reason) {
        int idx = globalReasons.IndexOf(reason);
        if (idx == -1) {
            //Debug.LogWarning($"Reason {reason.name} tried to UnlockAll without first locking!");
            return;
        }
        //globalCount--;
        globalReasons.RemoveAt(idx);
        foreach (LockCore instance in instances) {
            //if (instance.exemptions > 0)
                //instance.exemptions--;
            instance.ChangeLockCount(0);
        }
    }
    public static void UnlockTag(LockTag tag) {
        //foreach (LockReason reason in globalReasons) {
        for (int i = globalReasons.Count - 1; i >= 0; i --) {
            LockReason reason = globalReasons[i];
            if ((reason.category & tag) != 0) {
                UnlockAll(reason);
            }
        }
    }
    public static void ToggleLockAll(LockReason reason, bool enabled) {
        if (enabled)
            LockAll(reason);
        else
            UnlockAll(reason);
    }
}
