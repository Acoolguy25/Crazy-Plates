using Mirror;
using System.Collections;
using UnityEngine;

public class TestScript : NetworkBehaviour
{
    [ServerCallback]
    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        //GetComponent<NetworkIdentity>().AssignClientAuthority(NetworkServer.connections[0]);
        RpcTest();
        TargetRpcTest(connectionToClient);
        Debug.Log("Signals Sent!");
    }
    [ClientRpc]
    void RpcTest() {
        Debug.Log("<-- RPC PASSED -- >");
    }
    [TargetRpc]
    void TargetRpcTest(NetworkConnectionToClient networkConnectionToClient) {
        Debug.Log($"<-- TargetRPC PASSED -- >");
    }
}
