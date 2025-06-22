using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    public ushort multiplier;

    [NonSerialized] public Action<GameObject, float> Activate;
}

public class Events : MonoBehaviour
{
    [Header("List of Game Events")]
    public List<GameEvent> events;
    private int totalWeight;
    public List<Color> eventColors;

    void Awake()
    {
        // Initialize event actions manually because lambdas are not serialized
        events = new List<GameEvent>
        {
            new GameEvent
            {
                name = "Grow",
                eventType = EventType.Plate,
                eventColor = EventColor.green,
                displayText = "linearly expand by {0} {1}",
                description = "The plate increases in size.",
                multiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("meter", "meters"),
                variantRange = new Tuple<ushort, ushort, ushort>(2, 8, 4),
                Activate = (GameObject target, float variant) =>
                {
                    PlateProperties prop = target.GetComponent<PlateProperties>();
                    prop.setPlateSize(prop.plateSize + new Vector3(variant, 0, variant));
                }
            },
            new GameEvent
            {
                name = "Shrink",
                eventType = EventType.Plate,
                eventColor = EventColor.red,
                displayText = "linearly shrink by {0} {1}",
                description = "The plate decreases in size along the horizontal axes.",
                multiplier = 0,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("meter", "meters"),
                variantRange = new Tuple<ushort, ushort, ushort>(4, 12, 4),
                Activate = (GameObject target, float variant) =>
                {
                    PlateProperties prop = target.GetComponent<PlateProperties>();
                    prop.setPlateSize(prop.plateSize - new Vector3(variant, 0, variant));
                }
            },
            new GameEvent
            {
                name = "Grow Tall",
                eventType = EventType.Plate,
                eventColor = EventColor.red,
                displayText = "grow tall by {0} {1}",
                description = "The plate increases in height along the vertical axis.",
                multiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("meter", "meters"),
                variantRange = new Tuple<ushort, ushort, ushort>(4, 12, 4),
                Activate = (GameObject target, float variant) =>
                {
                    float time = Mathf.Sqrt(variant) / 2f;
                    PlateProperties prop = target.GetComponent<PlateProperties>();
                    prop.setPlateSize(prop.plateSize + new Vector3(0, variant, 0), time, Ease.Linear);
                    prop.setPlatePos(prop.platePos + new Vector3(0, variant/2, 0), time*2, Ease.Linear);
                }
            },
            new GameEvent
            {
                name = "Shake",
                eventType = EventType.Plate,
                eventColor = EventColor.red,
                displayText = "shake it off with a magnitude of x{0} {1}",
                description = "The plate shakes continuously in the horizontal axes.",
                multiplier = 40,
                affectRange = new Tuple<ushort, ushort>(1, 3),
                displayUnits = new Tuple<string, string>("magnitude", "magnitude"),
                variantRange = new Tuple<ushort, ushort, ushort>(4, 12, 4),
                Activate = (GameObject target, float variant) =>
                {
                    PlateProperties prop = target.GetComponent<PlateProperties>();
                    prop.setPlateShake(prop.plateShake + new Vector3(variant/4, 0, variant/4));
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
