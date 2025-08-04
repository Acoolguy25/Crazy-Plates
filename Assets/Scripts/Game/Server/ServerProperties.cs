using UnityEngine;
using Mirror;
using DG.Tweening;

public class ServerProperties : NetworkBehaviour
{
    [SyncVar]
    public ushort AlivePlayers = 1;
    [SyncVar]
    public ushort PlayerCount = 1;
    [SyncVar]
    public bool SinglePlayer = true;
    public static ServerProperties Instance { get; private set; }
    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(Instance);
    }
    private void Start()
    {
        DOTween.defaultTimeScaleIndependent = true;
    }
}
