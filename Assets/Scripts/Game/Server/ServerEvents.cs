using System;
using Unity;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

public class ServerEvents: MonoBehaviour {
    public static ServerEvents Instance;

    public Action<PlayerController> PlayerDied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    private void Awake() {
        Assert.IsNull(Instance);

        Instance = this;
    }
}