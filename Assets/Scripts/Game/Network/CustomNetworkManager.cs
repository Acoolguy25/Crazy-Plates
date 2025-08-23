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
    public GameObject[] EnableOnStart;
    public ServerProperties serverProperties;
    public static ServerLobby serverLobby;
    public static CustomBasicAuthenticator CustomBasicAuthenticator;
    public static CustomNetworkManager singleton2;

    private NetworkIdentity serverIdentity;
    public override void Awake() {
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
        //transport = GetComponent<KcpTransport>();
        Transport.active = transport;
        if (options != null) {
            serverProperties.MaxPlayers = Convert.ToUInt16(options["MaxPlayers"]);
            CustomBasicAuthenticator.allowExternalConnections = !Convert.ToBoolean(options["LANOnly"]);
        }
        else
            CustomBasicAuthenticator.allowExternalConnections = false;
        serverProperties.SinglePlayer = singleplayer;
        serverProperties.Begin();
        
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
        serverProperties.Begin();
        GameLobby.singleton.Begin();
        if (LobbyJoin.singleton)
            LobbyJoin.singleton.StopJoin();
    }
    public override void OnClientDisconnect() {
        base.OnClientDisconnect();
        //Debug.Log("Client disconnected from server.");
        GameLobby.singleton.End();
        if (LobbyJoin.singleton)
            LobbyJoin.singleton.JoinGameFail("Disconnected from server.");
        
    }
    public override void OnServerConnect(NetworkConnectionToClient conn) {
        base.OnServerConnect(conn);
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn) {
        bool found = false;
        foreach (PlayerData player in serverProperties.players) {
            if (player.clientConnection == conn) {
                found = true;
                if (player.gamemode == PlayerGamemode.Alive) {
                    serverProperties.AlivePlayers--;
                }
                //serverProperties.PlayerCount--;
                serverProperties.players.Remove(player);
                break;
            }
        }
        if (!found) {
            Debug.LogWarning($"Player with connection ID {conn.connectionId} not found in player list on disconnect.");
        }
        base.OnServerDisconnect(conn);
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
        //serverProperties.PlayerCount++;
        NetworkServer.AddPlayerForConnection(conn, player);

        player.GetComponent<PlayerController>().ServerStartUp();

        if (conn is LocalConnectionToClient)
            serverIdentity.AssignClientAuthority(conn);

        if (ServerProperties.Instance.SinglePlayer)
            StartGame();
    }
    public void StartGame() {
        GameEvents.Instance.OnClientBegin();
        foreach (var gameObj in EnableOnStart)
            gameObj.SetActive(true);
        gameRunner.StartGame();
    }
    
    public override void OnClientError(TransportError error, string reason) {
        LobbyJoin.singleton.JoinGameFail(reason);
        base.OnClientError(error, reason);
    }
    public override void OnClientTransportException(Exception exception) {
        LobbyJoin.singleton.JoinGameFail(exception.Message);
        
        base.OnClientTransportException(exception);
    }
}
