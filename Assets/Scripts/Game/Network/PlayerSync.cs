using UnityEngine;
using Mirror;

public class PlayerSync : NetworkBehaviour {
    
    [SyncVar(hook = nameof(Check))]
    public uint correspondingNetId = 0;
    public override void OnStartClient() {
        base.OnStartClient();
    }
    [Client]
    protected void Check(uint oldNetId, uint newNetId) {
        if (newNetId != 0) {
            Transform newParent = Reflection.Deserialize<Transform>(newNetId);
            transform.SetParent(newParent);
            newParent.SendMessage("AddedTransform", transform);
        }
    }
}