using UnityEngine;
using System.Collections;
using Mirror;

public class TempGameRunner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() //public IEnumerator Start()
    {
        //yield return new WaitForSeconds(5f);
        GetComponent<CustomNetworkManager>().StartHost();
    }
}
