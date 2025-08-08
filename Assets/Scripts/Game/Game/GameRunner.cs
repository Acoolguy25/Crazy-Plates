using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

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

    [Header("Debug")]
    public static bool debugMode = true;
    //public short debugEventNo = -1;
    public bool runGame = true; // Whether to run the game automatically
    public int debugFrameRate = -1;
    private float TimeBetweenEvents;
    public bool activeServer;

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
                NetworkTime.time + gameStartDelay);
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
                    Outline outlineCode = plate.AddComponent<Outline>();
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
        activeServer = NetworkServer.active;
        if (platePrefab == null)
        {
            Debug.LogError("Plate prefab is not assigned.");
            return;
        }
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

        List<Transform> CenterPlates = new();
        for (int x = 0; x < n; x++){
            for (int z = 0; z < n; z++){
                Vector3 pos = new Vector3(
                    x * (w + sep_x) - gridOffsetX,
                    0,
                    z * (h + sep_z) - gridOffsetZ
                );

                GameObject plate = Instantiate(platePrefab, pos, Quaternion.identity, transform);
                plate.name = $"Plate_{++plateCount}";
                if (activeServer)
                    NetworkServer.Spawn(plate);
                if (Mathf.Abs(x - n / 2) * Mathf.Abs(z - n / 2) <= ServerProperties.Instance.players.Count) {
                    //ServerProperties.Instance.SpawnPoints.Add(pos + vecOffset);
                    CenterPlates.Insert(UnityEngine.Random.Range(0, CenterPlates.Count+1), plate.transform);
                }
            }
        }
        foreach (PlayerData playerData in ServerProperties.Instance.players) {
            Assert.IsTrue(CenterPlates.Count > 0, "No more spawnpoints (Players > Plates)");
            Transform plate = CenterPlates[0];
            Vector3 vecOffset = (plate.GetComponent<PlateProperties2>().render.lossyScale.y / 2) * Vector3.up;

            playerData.playerController.SpawnCharacter(playerData.playerController.connectionToClient,
                "Character",
                plate.position + vecOffset);
            CenterPlates.RemoveAt(0);
        }

        ServerEvents.Instance.PlayerDied += OnDied;

        gameCoroutine = StartCoroutine(RunGame());
    }

    public void OnDied(PlayerController player) {
        if (ServerProperties.Instance.AlivePlayers <= (ServerProperties.Instance.SinglePlayer? 0: 1))
            EndGame();
    }

    public void EndGame() {
        Assert.IsNotNull(gameCoroutine);
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
