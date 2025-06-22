using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class GameRunner : MonoBehaviour
{
    [Header("Grid Settings")]
    public int n = 5;              // Grid size (n x n)
    public float sep_x = 0.5f;     // Space between plates in X
    public float sep_z = 0.5f;     // Space between plates in Z

    [Header("Plate Settings")]
    public GameObject platePrefab; // Plate prefab (e.g., Cube or Plane)

    [Header("Game Events")]
    private Events eventsScript;

    [Header("Debug")]
    public bool debugMode = false;
    public short debugEventNo = -1;
    IEnumerator RunGame()
    {
        if (debugMode && debugEventNo < 0)
            yield break;
        while (transform.childCount > 0)
            {
                GameEvent selEvent = eventsScript.GetRandomWeightedEvent();
                ushort platesAffected = (ushort)UnityEngine.Random.Range(selEvent.affectRange.Item1, selEvent.affectRange.Item2);
                float variant = 0f;
                if (selEvent.variantRange.Item3 != 0)
                    variant =
                        (float)UnityEngine.Random.Range(selEvent.variantRange.Item1 * selEvent.variantRange.Item3, selEvent.variantRange.Item2 * selEvent.variantRange.Item3) / ((float)selEvent.variantRange.Item3);


                string typeText = selEvent.eventType == EventType.Plate ? "plates" : selEvent.eventType == EventType.Player ? "players" : "the world";
                Debug.Log($"{platesAffected} {typeText} will {string.Format(selEvent.displayText, variant, variant == 1 ? selEvent.displayUnits.Item1 : selEvent.displayUnits.Item2)}");
                yield return new WaitForSeconds(3f);
                for (ushort i = 0; i < platesAffected; i++)
                {
                    if (transform.childCount == 0)
                        break;
                    GameObject plate = transform.GetChild(UnityEngine.Random.Range(0, transform.childCount)).gameObject;
                    Outline outlineCode = plate.AddComponent<Outline>();
                    outlineCode.OutlineColor = eventsScript.eventColors[(int)selEvent.eventColor];
                    outlineCode.OutlineMode = Outline.Mode.OutlineAll;
                    outlineCode.OutlineWidth = 10f;
                    yield return new WaitForSeconds(0.6f); // example
                    Destroy(outlineCode);
                    if (plate && plate.transform.parent != null)
                        selEvent.Activate(plate, variant);
                    yield return new WaitForSeconds(0.5f);
                }
            }
    }
    [Server]
    public void StartGame()
    {
        if (platePrefab == null)
        {
            Debug.LogError("Plate prefab is not assigned.");
            return;
        }
        eventsScript = GetComponent<Events>();
        // Get actual size of the prefab (renderer bounds)
        Vector3 plateSize = GetPrefabWorldSize(platePrefab);
        float w = plateSize.x;
        float h = plateSize.z;

        // Calculate center offset so the middle plate is at (0, 0, 0)
        float gridOffsetX = ((n - 1) * (w + sep_x)) / 2f;
        float gridOffsetZ = ((n - 1) * (h + sep_z)) / 2f;

        int plateCount = 0;

        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                Vector3 pos = new Vector3(
                    x * (w + sep_x) - gridOffsetX,
                    plateSize.y/2,
                    z * (h + sep_z) - gridOffsetZ
                );

                GameObject plate = Instantiate(platePrefab, pos, Quaternion.identity, transform);
                plate.name = $"Plate_{++plateCount}";
                NetworkServer.Spawn(plate);
            }
        }

        StartCoroutine(RunGame());
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
