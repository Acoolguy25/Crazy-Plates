using Mirror;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System;

public static class SharedFunctions
{
    public static double GetNetworkTime()
    {
        return ((NetworkServer.active || NetworkClient.active) && !ServerProperties.Instance.SinglePlayer) ? NetworkTime.time : Time.timeAsDouble;
    }
    public static IList<T> ShuffleList<T>(this IList<T> list, System.Random rng) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }
    public static GameObject FindInactiveWithTag(string tag) {
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (go.CompareTag(tag))
                return go;
        }
        return null;
    }
}