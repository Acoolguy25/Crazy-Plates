using Mirror;
using UnityEngine;
using UnityEngine.Assertions;

public class SharedFunctions : MonoBehaviour
{
    public static double GetNetworkTime()
    {
        return ((NetworkServer.active || NetworkClient.active) && !ServerProperties.Instance.SinglePlayer) ? NetworkTime.time : Time.timeAsDouble;
    }
}
