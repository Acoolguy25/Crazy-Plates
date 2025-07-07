using UnityEngine;
using Mirror;

public class ServerProperties : NetworkBehaviour
{
    [SyncVar]
    public ushort AlivePlayers = 1;
    [SyncVar]
    public ushort PlayerCount = 1;
    public static ServerProperties Instance { get; private set; }
    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(Instance);
    }
}
