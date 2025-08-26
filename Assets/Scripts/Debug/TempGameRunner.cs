using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TempGameRunner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject[] EnableOnStart;
    private void Start() //public IEnumerator Start()
    {
        //yield return new WaitForSeconds(5f);
        //GetComponent<CustomNetworkManager>().StartHost();

        
        if (NetworkClient.active) {
            foreach (var gameObj in EnableOnStart)
                gameObj.SetActive(true);
            //GameEvents.Instance.OnClientBegin();

        }
        else if (!SceneManager.GetSceneByName("MainMenu").isLoaded) {
            SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
        }
        //if (NetworkServer.active)
            //GetComponent<GameRunner>().StartGame();

    }
}
