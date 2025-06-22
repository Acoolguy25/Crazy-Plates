using UnityEngine;
using DG.Tweening; // Required for DOTween
using Mirror;

public class LobbyRunner : MonoBehaviour
{
    public Transform moon;
    public Transform planet;

    // Update is called once per frame
    void Update()
    {
        moon.RotateAround(planet.position, Vector3.up, 5 * Time.deltaTime);
        moon.LookAt(planet);
    }
    void Start()
    {
        NetworkManager.singleton.StartHost();
    }
}
