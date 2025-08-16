using UnityEngine;
using Mirror;
using kcp2k;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public bool isDedicatedServer = true;
    public GameRunner gameRunner;
    public GameObject[] EnableOnStart;
    public ServerProperties serverProperties;
    public override void Start()
    {
        base.Start();
        //if (!ServerProperties.Instance.SinglePlayer)
        //{
        //    DontDestroyOnLoad(this);
        //}
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        //Debug.Log("Server started and ready to accept connections.");
        serverProperties.Begin();
        foreach (var gameObj in EnableOnStart)
            gameObj.SetActive(true);
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        //Debug.Log("Client connected to server.");
        serverProperties.Begin();
        GameEvents.Instance.OnClientBegin();
        foreach (var gameObj in EnableOnStart)
            gameObj.SetActive(true);
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn) {
        InstantiateParameters parameters = new InstantiateParameters() {
            scene = gameObject.scene,
            worldSpace = false,
            parent = transform
        };
        GameObject player = Instantiate(playerPrefab, parameters);
        player.transform.position = Vector3.zero;
        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.AddPlayerForConnection(conn, player);

        player.GetComponent<PlayerController>().ServerStartUp();

        if (ServerProperties.Instance.SinglePlayer)
            gameRunner.StartGame();
    }
}
