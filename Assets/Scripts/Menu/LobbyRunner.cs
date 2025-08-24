using UnityEngine;

public class LobbyRunner : MonoBehaviour
{
    public GameRunner gameRunner;
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
        //NetworkManager.singleton.StartHost();
        //StartCoroutine(gameRunner.runGame());
        //gameRunner.StartGame();
    }
}
