using System;
using Unity;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

public class ServerEvents: MonoBehaviour {
    public static ServerEvents Instance;

    public Action<PlayerController> PlayerDied;

    private void Awake() {
        Assert.IsNull(Instance);

        Instance = this;
    }
}