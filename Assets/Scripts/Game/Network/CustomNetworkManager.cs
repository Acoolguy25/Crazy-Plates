using kcp2k;
using Mirror;
using Mirror.SimpleWeb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public bool isDedicatedServer = true;
    public GameRunner gameRunner;
    //public GameObject[] EnableOnStart;
    public ServerProperties serverProperties;
    public static ServerLobby serverLobby;
    public static CustomBasicAuthenticator CustomBasicAuthenticator;
    public static CustomNetworkManager singleton2;

    private NetworkIdentity serverIdentity;
    public override void Awake() {
        if (singleton2 != null && singleton2 != this) {
            Destroy(gameObject);
            Destroy(this);
            return;
        }
        singleton2 = this;
        serverLobby = serverProperties.GetComponent<ServerLobby>();
        serverIdentity = serverProperties.GetComponent<NetworkIdentity>();
        CustomBasicAuthenticator = GetComponent<CustomBasicAuthenticator>();
        base.Awake();
    }
    public override void Start()
    {
        base.Start();
        //if (!ServerProperties.Instance.SinglePlayer)
        //{
        //    DontDestroyOnLoad(this);
        //}
    }
    public void Init(Dictionary<string, object> options = null, bool singleplayer = false, string password = null, bool clientOnly = false) {
        if (singleplayer)
            transport = GetComponent<DummyTransport>();
        else
            transport = GetComponent<SimpleWebTransport>();
        GameObject serverPropGameObj = SharedFunctions.FindInactiveWithTag("ServerProperties");
        serverProperties = serverPropGameObj.GetComponent<ServerProperties>();
        serverPropGameObj.SetActive(true);
        //transport = GetComponent<KcpTransport>();
        Transport.active = transport;
        if (options != null) {
            serverProperties.MaxPlayers = Convert.ToUInt16(options["MaxPlayers"]);
            CustomBasicAuthenticator.allowExternalConnections = !Convert.ToBoolean(options["LANOnly"]);
        }
        else {
            serverProperties.MaxPlayers = 1;
            CustomBasicAuthenticator.allowExternalConnections = false;
        }
        serverProperties.SinglePlayer = singleplayer;
        serverProperties.BeforeStartServer();
        
        CustomBasicAuthenticator.Begin(options: options, clientOnly: clientOnly);
        if (password != null)
            CustomBasicAuthenticator.SetPassword(password);
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        //Debug.Log("Server started and ready to accept connections.");
    }
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        //Debug.Log("Client connected to server.");
        
    }
    public override void OnClientDisconnect() {
        base.OnClientDisconnect();
        //Debug.Log("Client disconnected from server.");
        if (!serverProperties.SinglePlayer) {
            GameLobby.singleton.End();
            if (LobbyJoin.singleton)
                LobbyJoin.singleton.JoinGameFail("Disconnected from server.");
        }
    }
    public override void OnServerConnect(NetworkConnectionToClient client) {
        base.OnServerConnect(client);
        if (client is LocalConnectionToClient)
            serverIdentity.AssignClientAuthority(client);
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn) {
        if (serverProperties == null) {
            base.OnServerDisconnect(conn);
            return;
        }
        bool found = false;
        foreach (uint playerIdx in serverProperties.players) {
            PlayerController player = Reflection.Deserialize<PlayerController>(playerIdx);
            if (player.connectionToClient == conn) {
                found = true;
                ServerEvents.Instance.PlayerDied?.Invoke(player);
                //serverProperties.PlayerCount--;
                serverProperties.players.Remove(playerIdx);
                break;
            }
        }
        if (!found && conn.connectionId != 1) {
            Debug.LogWarning($"Player with connection ID {conn.connectionId} not found in player list on disconnect.");
        }
        base.OnServerDisconnect(conn);
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient client) {
        InstantiateParameters parameters = new InstantiateParameters() {
            scene = gameObject.scene,
            worldSpace = false,
            parent = transform
        };
        GameObject player = Instantiate(playerPrefab, parameters);
        player.transform.position = Vector3.zero;
        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={client.connectionId}]";
        //serverProperties.PlayerCount++;
        NetworkServer.AddPlayerForConnection(client, player);

        int plrIdx = ServerProperties.Instance.PlayerCount;

        PlayerController playerData = player.GetComponent<PlayerController>();
        //(player.GetComponent<PlayerController>(), client.address,
        //    ServerProperties.Instance.SinglePlayer ? "You" : "Player" + plrIdx.ToString(),
        //    PlayerGamemode.Menu
        //);
        playerData.ipAddress = client.address;
        playerData.displayName = ServerProperties.Instance.SinglePlayer ? "You" : "Player" + plrIdx.ToString();
        playerData.gamemode = PlayerGamemode.Menu;

        ServerProperties.Instance.players.Add(Reflection.Serialize(playerData));


        //if (ServerProperties.Instance.SinglePlayer)
            //StartGame();
    }
    private bool waitingForSceneChange = true;
    private bool waitingForPlayers = true;
    [Server]
    protected void StartGame() {
        if (waitingForSceneChange || waitingForPlayers)
            return;
        GameObject gameRunner = SharedFunctions.FindInactiveWithTag("GameRunner");
        //UnifiedDelay.Instance.Delay(3f, gameRunner.GetComponent<GameRunner>().StartGame);
        gameRunner.GetComponent<GameRunner>().StartGame();
    }
    public override void ServerChangeScene(string newSceneName) {
        waitingForSceneChange = true;
        base.ServerChangeScene(newSceneName);
        waitingForPlayers = ArePlayersReady();
    }
    public override void OnServerSceneChanged(string sceneName) {
        base.OnServerSceneChanged(sceneName);

        if (sceneName == "MainMenu") {
            return;
        }
        waitingForSceneChange = false;
        StartGame();
    }
    public bool ArePlayersReady() {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values) {
            if (!conn.isAuthenticated)
                continue;
            if (!conn.isReady) {
                return false;
            }
        }
        return true;
    }
    public override void OnServerReady(NetworkConnectionToClient client) {
        base.OnServerReady(client);
        
        waitingForPlayers = ArePlayersReady();
        StartGame();
        //Debug.Log("Client is ready on server.");
    }
    
    //public void StartGame() {
    //    GameEvents.Instance.OnClientBegin();
    //    foreach (var gameObj in EnableOnStart)
    //        gameObj.SetActive(true);
    //    gameRunner.StartGame();
    //}
    //public void ServerLoadAdditive(string sceneName) {
    //    if (!NetworkServer.active) {
    //        Debug.LogError("ServerLoadAdditive can only be called on the server.");
    //        return;
    //    }

    //    if (SceneManager.GetSceneByName(sceneName).isLoaded) {
    //        Debug.Log($"Scene {sceneName} already loaded.");
    //        return;
    //    }

    //    // Load it on the server
    //    SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

    //    // Tell clients to load it
    //    NetworkServer.SendToAll(new SceneMessage
    //    {
    //        sceneName = sceneName,
    //        sceneOperation = SceneOperation.LoadAdditive
    //    });
    //}

    //public void ServerUnloadAdditive(string sceneName) {
    //    if (!NetworkServer.active) {
    //        Debug.LogError("ServerUnloadAdditive can only be called on the server.");
    //        return;
    //    }

    //    if (!SceneManager.GetSceneByName(sceneName).isLoaded) {
    //        Debug.Log($"Scene {sceneName} is not loaded.");
    //        return;
    //    }

    //    // Unload on server
    //    SceneManager.UnloadSceneAsync(sceneName);

    //    // Tell clients
    //    NetworkServer.SendToAll(new SceneMessage
    //    {
    //        sceneName = sceneName,
    //        sceneOperation = SceneOperation.UnloadAdditive
    //    });
    //}


    public override void OnClientError(TransportError error, string reason) {
        LobbyJoin.singleton.JoinGameFail(reason);
        base.OnClientError(error, reason);
    }
    public override void OnClientTransportException(Exception exception) {
        LobbyJoin.singleton.JoinGameFail(exception.Message);
        
        base.OnClientTransportException(exception);
    }
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) {
        bool before = SceneManager.GetActiveScene().name == "MainMenu";
        bool isNow = newSceneName == "MainMenu";
        if (before != isNow) {
            LobbyUI.Instance.SetCanvasVisibility(isNow);
        }
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
    }
    public override void OnClientSceneChanged() {
        if (networkSceneName == "MainMenu")
            LobbyUI.Instance.FadeBlackScreen(0f);

        base.OnClientSceneChanged();
    }
}