using UnityEngine;
using Mirror;
using DG.Tweening;
using System.Collections.Generic;

public class ServerProperties : NetworkBehaviour
{
    [SyncVar]
    public ushort AlivePlayers = 1;
    //[SyncVar]
    public int PlayerCount => players.Count;
    [SyncVar]
    public ushort MaxPlayers = 5;
    [SyncVar]
    public bool SinglePlayer = true;
    [SyncVar]
    public bool GameInProgress = false;
    [SyncVar]
    public double GameStartTime = 0d;
    [SyncVar]
    public System.Random Random;
    [SyncVar(hook = nameof(OnGameCodeChanged))]
    public string GameCode = string.Empty;

    // Non sync vars
    public static ushort playersNeeded = 1;

    public double GameDuration {
        get {
            if (!GameInProgress)
                return 0d;
            else
                return SharedFunctions.GetNetworkTime() - GameStartTime;
        }
    }
    
    readonly public SyncList<PlayerData> players = new();

    //[Header("Server Properties")]
    //public List<Vector3> SpawnPoints;
    public static ServerProperties Instance { get; private set; }
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
#endif
    public void Begin()
    {
        if (Instance != this && Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        return;
    }
    [Client]
    private void OnGameCodeChanged(string oldCode, string newCode) {
        GameLobby.singleton.OnGameCodeChanged(newCode);
    }
    public override void OnStartServer() {
        AlivePlayers = 0;
        //PlayerCount = 0;
        GameInProgress = false;
        GameStartTime = 0d;
        Random = new System.Random();
        //GameCode = "Loading...";
        players.Clear();

        DOTween.defaultTimeScaleIndependent = false;
    }
}
