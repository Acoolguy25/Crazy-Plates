using UnityEngine;
using Mirror;
using kcp2k;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public bool isDedicatedServer = true;
    public GameRunner gameRunner;
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started and ready to accept connections.");
        gameRunner.StartGame();
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Client connected to server.");
    }
    public override void Awake()
    {
        if (transport == null)
        {
            if (SceneManager.GetActiveScene().name == "MainMenu" || isDedicatedServer)
            {
                transport = gameObject.AddComponent<DummyTransport>();
            }
            else
            {
                transport = gameObject.AddComponent<KcpTransport>();
            }
        }
        base.Awake();
    }
    public override void Start() {
        //gameRunner = GetComponent<GameRunner>();
        base.Start();
    }
}
