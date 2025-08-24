using UnityEngine;
using Mirror;
public static class Reflection {
    [Server]
    public static uint Serialize(Transform transform) {
        Debug.Assert(transform.TryGetComponent<NetworkIdentity>(out var identity), "NetworkIdentity component not found on the Transform.");
        return identity.netId;
    }
    [Client]
    public static Transform Deserialize(uint netId) {
        if (NetworkClient.spawned.TryGetValue(netId, out var identity)) {
            return identity.transform;
        }
        Debug.LogWarning($"NetworkIdentity with netId {netId} not found.");
        return null;
    }
}