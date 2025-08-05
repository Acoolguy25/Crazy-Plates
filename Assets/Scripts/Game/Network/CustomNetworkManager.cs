using UnityEngine;
using Mirror;
using kcp2k;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public bool isDedicatedServer = true;
    public GameRunner gameRunner;
    public override void Start()
    {
        base.Start();
        if (!ServerProperties.Instance.SinglePlayer)
        {
            DontDestroyOnLoad(this);
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        //Debug.Log("Server started and ready to accept connections.");
        gameRunner.StartGame();
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        //Debug.Log("Client connected to server.");
        GameEvents.Instance.OnClientBegin();
    }
}
