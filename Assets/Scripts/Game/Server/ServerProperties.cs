using UnityEngine;
using Mirror;
using DG.Tweening;
using System.Collections.Generic;

public class ServerProperties : NetworkBehaviour
{
    [SyncVar]
    public ushort AlivePlayers = 1;
    [SyncVar]
    public ushort PlayerCount = 0;
    [SyncVar]
    public bool SinglePlayer = true;
    [SyncVar]
    public bool GameInProgress = false;
    [SyncVar]
    public double GameStartTime = 0d;

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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        DOTween.defaultTimeScaleIndependent = true;
    }
}
