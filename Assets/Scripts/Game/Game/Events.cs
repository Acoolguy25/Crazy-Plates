using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;

[Serializable]
public enum EventType
{
    Plate,
    Player,
    Global
}

[Serializable]
public enum EventColor
{
    red,
    green,
    blue,
    yellow,
    purple,
    cyan,
    white
}

[Serializable]
public class GameEvent
{
    public string name;
    public EventType eventType;
    public EventColor eventColor;
    public string displayText;
    public string description;
    public Tuple<string, string> displayUnits;
    public Tuple<ushort, ushort> affectRange;
    public Tuple<ushort, ushort, ushort> variantRange;
    public ushort gameMultiplier;
    public ushort debugMultiplier;
    public ushort multiplier => GameRunner.debugMode ? debugMultiplier : gameMultiplier;

    [NonSerialized] public Action<GameEvent, GameObject, float> Activate;
}

public class Events : MonoBehaviour
{
    [Header("List of Game Events")]
    public List<GameEvent> events;
    private int totalWeight;
    public List<Color> eventColors;
    public short[] debugEventNo; // For debugging purposes, to select a specific event
    ushort debugEventIndex = 0; // For debugging purposes, to select a specific event by index

    void Start()
    {
        // Initialize event actions manually because lambdas are not serialized
        events = new List<GameEvent>
        {
            new GameEvent // idx 0
            {
                name = "Grow",
                eventType = EventType.Plate,
                eventColor = EventColor.green,
                displayText = "linearly expand by {0} {1}",
                description = "The plate increases in size.",
                gameMultiplier = 4, debugMultiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("meter", "meters"),
                variantRange = new Tuple<ushort, ushort, ushort>(2, 8, 4),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    PlateProperties2 prop = target.GetComponent<PlateProperties2>();
                    //prop.setPlateSize(prop.plateSize + new Vector3(variant, 0, variant));
                    prop.CreateRelSizeTween(self.name, new Vector3(variant, 0, variant), Mathf.Sqrt(variant) / 2f, Ease.Linear);
                }
            },
            new GameEvent // idx 1
            {
                name = "Shrink",
                eventType = EventType.Plate,
                eventColor = EventColor.red,
                displayText = "linearly shrink by {0} {1}",
                description = "The plate decreases in size along the horizontal axes.",
                gameMultiplier = 4, debugMultiplier = 0,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("meter", "meters"),
                variantRange = new Tuple<ushort, ushort, ushort>(4, 12, 4),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    PlateProperties2 prop = target.GetComponent<PlateProperties2>();
                    //prop.setPlateSize(prop.plateSize + new Vector3(variant, 0, variant));
                    prop.CreateRelSizeTween(self.name, -new Vector3(variant, 0, variant), Mathf.Sqrt(variant) / 2f, Ease.Linear);
                }
            },
            new GameEvent // idx 2
            {
                name = "Grow Tall",
                eventType = EventType.Plate,
                eventColor = EventColor.blue,
                displayText = "grow tall by {0} {1}",
                description = "The plate increases in height along the vertical axis.",
                gameMultiplier = 4, debugMultiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("meter", "meters"),
                variantRange = new Tuple<ushort, ushort, ushort>(4, 12, 4),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    //float time = Mathf.Sqrt(variant) / 2f;
                    //PlateProperties prop = target.GetComponent<PlateProperties>();
                    //prop.setPlateSize(prop.plateSize + new Vector3(0, variant, 0), time, Ease.Linear);
                    //prop.setPlatePos(prop.platePos + new Vector3(0, variant/2, 0), time, Ease.Linear);
                    PlateProperties2 prop = target.GetComponent<PlateProperties2>();
                    //prop.setPlateSize(prop.plateSize + new Vector3(variant, 0, variant));
                    prop.CreateRelSizeTween(self.name, variant * Vector3.up, Mathf.Sqrt(variant) / 2f, Ease.Linear);
                }
            },
            new GameEvent // idx 3
            {
                name = "Dance Party",
                eventType = EventType.Plate,
                eventColor = EventColor.red,
                displayText = "start dancing with a shake of x{0} {1}",
                description = "The plate shakes continuously in the horizontal axes.",
                gameMultiplier = 4, debugMultiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("magnitude", "magnitude"),
                variantRange = new Tuple<ushort, ushort, ushort>(3, 6, 4),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    //PlateProperties prop = target.GetComponent<PlateProperties>();
                    //prop.setPlateShake(prop.plateShake + new Vector3(variant/4, 0, variant/4));
                    PlateProperties2 prop = target.GetComponent<PlateProperties2>();
                    prop.CreateShakeTween(self.name, new Vector3(variant*4, 0, variant*4));
                }
            },
            new GameEvent // idx 4
            {
                name = "Spin",
                eventType = EventType.Plate,
                eventColor = EventColor.blue,
                displayText = "spin at a rate of {0} {1}",
                description = "The plate spins continuously in the horizontal direction.",
                gameMultiplier = 4, debugMultiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("rpm", "rpm"),
                variantRange = new Tuple<ushort, ushort, ushort>(5, 15, 1),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    //PlateProperties prop = target.GetComponent<PlateProperties>();
                    float revolutionsPerSec = 1 / (variant / 60f); // Convert RPM to degrees per second
                    //prop.setPlateAngularRotation(prop.rotateSpeed + Vector3.up * degreesPerSecond);
                    PlateProperties2 prop = target.GetComponent<PlateProperties2>();
                    prop.CreateRelRotation(self.name, Vector3.up * 360, revolutionsPerSec, easeMethod: Ease.Linear,
                        loops: -1, loopType: LoopType.Incremental);
                }
            },
            new GameEvent // idx 5
            {
                name = "Flip",
                eventType = EventType.Plate,
                eventColor = EventColor.red,
                displayText = "flip at a rate of {0} {1}",
                description = "The plate spins continuously in the horizontal direction.",
                gameMultiplier = 4, debugMultiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("rpm", "rpm"),
                variantRange = new Tuple<ushort, ushort, ushort>(12, 23, 1),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    //PlateProperties prop = target.GetComponent<PlateProperties>();
                    //float degreesPerSecond = variant / 60f * 360f; // Convert RPM to degrees per second
                    Vector2 randomUnitVec = UnityEngine.Random.insideUnitCircle.normalized.normalized;
                    //prop.setPlateAngularRotation(prop.rotateSpeed + new Vector3(randomUnitVec.x, 0, randomUnitVec.y) * degreesPerSecond);
                    float revolutionsPerSec = 60f / variant; // Convert RPM to seconds per rev
                    PlateProperties2 prop = target.GetComponent<PlateProperties2>();
                    prop.CreateRelRotation(self.name, new Vector3(randomUnitVec.x, 0, randomUnitVec.y) * 360, revolutionsPerSec, easeMethod: Ease.Linear,
                        loops: -1, loopType: LoopType.Incremental);

                }
            },
            new GameEvent // idx 6
            {
                name = "Elevator",
                eventType = EventType.Plate,
                eventColor = EventColor.blue,
                displayText = "become an elevator",
                description = "Elevator cycles through 2–6 floors.",
                gameMultiplier = 4, debugMultiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("rpm", "rpm"),
                variantRange = new Tuple<ushort, ushort, ushort>(1, 10, 2),
                Activate = (GameEvent self, GameObject target, float variant) =>
                {
                    Elevator elev = target.GetOrAddComponent<Elevator>();
                    elev.floors = (ushort) 
                        Math.Min(6, elev.floors + UnityEngine.Random.Range(2, 6)); // Random number of floors between 2 and 5
                    elev.SetValues(elev.deltaElevation + variant,
                        elev.speedPerSecond + variant);
                }
            }
        };
        // Define colors corresponding to EventColor enum
        eventColors = new List<Color>()
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            new Color(0.5f, 0f, 0.5f), // purple
            Color.cyan,
            Color.white
        };
        // Calculate total weight of all the events, and cache it
        totalWeight = 0;
        foreach (var e in events)
        {
            totalWeight += e.multiplier;
        }
    }
    public GameEvent GetRandomWeightedEvent()
    {
        if (debugEventNo.Length > 0)
        {
            short eventNO = debugEventNo[(debugEventIndex) % debugEventNo.Length];
            if (eventNO < 0) {
                return null;
            }
            debugEventIndex++;
            return events[eventNO];
        }
        int rand = UnityEngine.Random.Range(0, totalWeight);

        int runningTotal = 0;
        foreach (var e in events)
        {
            runningTotal += e.multiplier;
            if (rand < runningTotal)
                return e;
        }

        return null; // Should never happen
    }
}
