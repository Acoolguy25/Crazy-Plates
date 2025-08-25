using UnityEngine;
using Mirror;
using System.Collections.Generic;
public static class Reflection {
    [Server]
    public static uint Serialize(Component comp) {
        Debug.Assert(comp.TryGetComponent<NetworkIdentity>(out var identity), "NetworkIdentity component not found on the Transform.");
        return identity.netId;
    }
    public static T Deserialize<T>(uint netId) {
        if (GetSpawned().TryGetValue(netId, out var identity)) {
            return identity.transform.GetComponent<T>();
        }
        Debug.LogWarning($"NetworkIdentity with netId {netId} not found.");
        return default;
    }
    private static Dictionary<uint, NetworkIdentity> GetSpawned() {
        if (NetworkServer.active)
            return NetworkServer.spawned;
        else if (NetworkClient.active) {
            return NetworkClient.spawned;
        }
        else {
            Debug.LogError("Reflection: Unable to GetSpawned(); Neither server/client active!");
            return default;
        }
    }
}