using UnityEngine;
using Mirror;

public class TempGameRunner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CustomNetworkManager.singleton.StartHost();
    }
}
