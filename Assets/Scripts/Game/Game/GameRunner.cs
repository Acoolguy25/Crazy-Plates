using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Plate;
public class GameRunner : MonoBehaviour
{
    [Header("Placement Settings")]
    public int n = 3;              // Grid size (n x n)
    public float sep_x = 0.5f;     // Space between plates in X
    public float sep_z = 0.5f;     // Space between plates in Z

    [Header("Asset Settings")]
    public GameObject platePrefab; // Plate prefab (e.g., Cube or Plane)
    //public GameObject playerPrefab;

    [Header("Private Variables")]
    private Events eventsScript;
    private Coroutine gameCoroutine;

    [Header("Game Settings")]
    public float gameStartDelay = 5f;
    public bool allEventsAtOnce = false; // If true, all events will be run at once, otherwise one by one
    private float TimeBetweenEvents;
    private bool activeServer => NetworkServer.active;
    

    [Header("Debug")]
    public static bool debugMode = true;
    public static bool debugObjectsEnabled = false;
    //public short debugEventNo = -1;
    public bool runGame = true; // Whether to run the game automatically
    public bool gameRunning;
    public GameObject[] debugObjects;
    public int debugFrameRate = -1;

    void Awake() {
        if (!debugMode)
            return;
        if (!activeServer && !SceneManager.GetSceneByName("MainMenu").isLoaded) {
            //SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
        }
        //SceneManager.SetActiveScene(gameObject.scene);
    }

    string GetDescMessage(string[] messages, ushort length) {
        string retStr = string.Empty;
        ushort messagesLength = (ushort)messages.Length;
        for (ushort i = 0; i < messages.Length; i++) {
            if (messages[i] == string.Empty) {
                messagesLength = i;
                break;
            }
            if (i == 0) {
                retStr += messages[i];
            }
            else if (i == length - 1) {
                if (i == 1)
                    retStr += " and " + messages[i];
                else
                    retStr += ", and " + messages[i];
            }
            else {
                retStr += ", " + messages[i];
            }
        }
        //if (messages.Length < length) {
        //    if (messages.Length == 0)
        //        retStr = "and";
        //    else if (messages.Length == 1)
        //        retStr += " and";
        //    else
        //        retStr += ", and";
        //}
        //if (messagesLength == length) {
        //    retStr += ".";
        //}
        return retStr;
    }
    IEnumerator RunGame()
    {
        if (activeServer)
            yield return new WaitUntil(() => NetworkServer.active && NetworkTime.time > 0);
#if !UNITY_EDITOR
            GameEvents.Instance.GameMessage = new GameMessage(
                "Game will begin in {0}",
                SharedFunctions.GetNetworkTime() + gameStartDelay);
            yield return new WaitForSeconds(gameStartDelay);
#endif
        ServerProperties.Instance.GameInProgress = true;
        ServerProperties.Instance.GameStartTime = SharedFunctions.GetNetworkTime();
        while (true){
        startOfLoop: while (debugMode && !runGame)
                yield return null;
            while (transform.childCount == 0) {
                GameEvents.Instance.GameMessage = new GameMessage(
                    "No Plates Found!", 0d);
                GameEvents.Instance.DescMessage = "Good luck surviving";
                yield return new WaitForSeconds(1f);
            }
            while (transform.childCount > 0){
                Application.targetFrameRate = debugFrameRate;
                GameEvent selEvent;
                while (true) {
                    selEvent = eventsScript.GetRandomWeightedEvent();
                    if (selEvent != null)
                        break;
                    yield return null;  
                }
                ushort percentage = (ushort) UnityEngine.Random.Range(selEvent.affectRange.Item1, selEvent.affectRange.Item2 + 1);
                ushort platesAffected = (ushort) Mathf.Ceil((float) ServerProperties.Instance.PlayerCount * percentage / 100);
                float variant = -69f;
                if (selEvent.variantRange.Item3 != 0)
                    variant =
                        (float)UnityEngine.Random.Range(selEvent.variantRange.Item1 * selEvent.variantRange.Item3, selEvent.variantRange.Item2 * selEvent.variantRange.Item3) / ((float)selEvent.variantRange.Item3);


                string typeText = selEvent.eventType == EventType.Plate ? "plates" : selEvent.eventType == EventType.Player ? "players" : "the world";
                //Debug.Log($"{platesAffected} {typeText} will {string.Format(selEvent.displayText, variant, variant == 1 ? selEvent.displayUnits.Item1 : selEvent.displayUnits.Item2)}");
                if (activeServer) {
                    GameEvents.Instance.GameMessage = new GameMessage(
                    $"{platesAffected} {typeText} will {string.Format(selEvent.displayText, variant, variant == 1 ? selEvent.displayUnits.Item1 : selEvent.displayUnits.Item2)}" + " in {0}",
                    SharedFunctions.GetNetworkTime() + TimeBetweenEvents);
                    GameEvents.Instance.DescMessage = selEvent.description;
                }
                yield return new WaitForSeconds(TimeBetweenEvents);
                string[] targets = new string[platesAffected];
                for (ushort i = 0; i < platesAffected; i++)
                {
                    targets[i] = string.Empty;
                }
                for (ushort i = 0; i < platesAffected; i++)
                {
                    if (transform.childCount == 0)
                        goto startOfLoop;
                    GameObject plate = transform.GetChild(UnityEngine.Random.Range(0, transform.childCount)).gameObject;
                    Outline outlineCode = plate.GetOrAddComponent<Outline>();
                    outlineCode.OutlineColor = eventsScript.eventColors[(int)selEvent.eventColor];
                    outlineCode.OutlineMode = Outline.Mode.OutlineAll;
                    outlineCode.OutlineWidth = 10f;
                    targets[i] = plate.name;
                    Destroy(outlineCode, 0.6f);
                    if (plate && plate.transform.parent != null)
                        selEvent.Activate(selEvent, plate, variant);
                    if (!allEventsAtOnce){
                        if (activeServer)
                            GameEvents.Instance.DescMessage = GetDescMessage(targets, platesAffected);
                        yield return new WaitForSeconds(0.65f);
                    }
                }
                if (allEventsAtOnce) {
                    if (activeServer)
                        GameEvents.Instance.DescMessage = GetDescMessage(targets, platesAffected);
                    yield return new WaitForSeconds(0.5f);
                }
                else
                    yield return new WaitForSeconds(0.3f);
                if (activeServer)
                    GameEvents.Instance.DescMessage += ".";
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
    public void StartGame()
    {
        if (gameRunning)
            return;
        if (debugMode && debugObjectsEnabled) {
            foreach (GameObject obj in debugObjects) {
                obj.SetActive(true);
            }
        }
        if (platePrefab == null)
        {
            Debug.LogError("Plate prefab is not assigned.");
            return;
        }
        gameRunning = true;
        //DOTween.Init(false, false, LogBehaviour.Default).SetCapacity(200, 50).SetManualUpdate(true);
        DOTween.useSmoothDeltaTime = true; // Use smooth delta time for animations
        DOTween.defaultUpdateType = UpdateType.Normal; // Use FixedUpdate for animations
        DOTween.timeScale = 1f;
        eventsScript = GetComponent<Events>();
        TimeBetweenEvents = 3f;
        // Get actual size of the prefab (renderer bounds)
        Vector3 plateSize = GetPrefabWorldSize(platePrefab);
        float w = plateSize.x;
        float h = plateSize.z;

        // Calculate center offset so the middle plate is at (0, 0, 0)
        float gridOffsetX = ((n - 1) * (w + sep_x)) / 2f;
        float gridOffsetZ = ((n - 1) * (h + sep_z)) / 2f;

        int plateCount = 0;

        List<Transform> spiralPlates = new List<Transform>();
        int x = n / 2, z = n / 2, step = 1, dir = 0;
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        while (spiralPlates.Count < n * n) {
            for (int r = 0; r < 2; r++) {
                for (int i = 0; i < step; i++) {
                    if (x >= 0 && x < n && z >= 0 && z < n) {
                        Vector3 pos = new Vector3(x * (w + sep_x) - gridOffsetX, 0, z * (h + sep_z) - gridOffsetZ);
                        GameObject plate = Instantiate(platePrefab, pos, Quaternion.identity, transform);
                        plate.name = $"Plate_{++plateCount}";
                        NetworkServer.Spawn(plate);
                        spiralPlates.Add(plate.transform);
                    }
                    x += dirs[dir].x; z += dirs[dir].y;
                }
                dir = (dir + 1) % 4;
            }
            step++;
        }

        ServerProperties.Instance.AlivePlayers = 0;
        int[] playersIdx = new int[ServerProperties.Instance.PlayerCount];
        for (int i = 0; i < ServerProperties.Instance.PlayerCount; i++)
            playersIdx[i] = i;
        var shuffledPlayers = SharedFunctions.ShuffleList(playersIdx, ServerProperties.Instance.Random);
        foreach (int playerIdx in shuffledPlayers) {
            Assert.IsTrue(spiralPlates.Count > 0, "No more spawnpoints (Players > Plates)");
            Transform plate = spiralPlates[0];
            Transform render = plate.GetComponent<PlateProperties2>().render;
            Vector3 vecOffset = (render.lossyScale.y) * Vector3.up;
            PlayerData playerData = ServerProperties.Instance.players[playerIdx];

            playerData.gamemode = PlayerGamemode.Alive;
            ServerProperties.Instance.AlivePlayers++;
            playerData.playerController.SpawnCharacter(
                "PlayerCharacter",
                render.position + vecOffset);
            spiralPlates.RemoveAt(0);
        }

        ServerEvents.Instance.PlayerDied += OnDied;

        gameCoroutine = StartCoroutine(RunGame());
    }

    public void OnDied(PlayerController player) {
        if (ServerProperties.Instance.AlivePlayers <= (ServerProperties.Instance.SinglePlayer? 0: 1))
            EndGame();
    }

    public void EndGame() {
        Assert.IsNotNull(gameCoroutine, "EndGame() called but game not started!");
        StopCoroutine(gameCoroutine);
        GameEvents.Instance.SurvivalTime = ServerProperties.Instance.GameDuration;
        ServerProperties.Instance.GameInProgress = false;
    }

    private Vector3 GetPrefabWorldSize(GameObject prefab)
    {
        Renderer rend = prefab.GetComponent<Renderer>();
        if (rend != null)
            return rend.bounds.size;
        else
            return prefab.transform.localScale;
    }
}
