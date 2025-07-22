using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;

public class Elevator : MonoBehaviour
{
    [Header("Elevator Settings")]
    public float deltaElevation { get; private set; } = 5f;
    public float speedPerSecond { get; private set; } = 4f;
    public ushort floors = 0;
    public bool upDirection = true;

    private float currentElevation = 0f;
    private ushort cur_floor = 0;
    private PlateProperties2 plateProperties;
    private Coroutine moveRoutine = null;
    private float MoveYBy(float deltaElevatorChange)
    {
        float timeChange = Mathf.Abs(deltaElevatorChange / speedPerSecond);
        //plateProperties.setPlateSizePosOffset(deltaElevatorChange,
        //timeChange, Ease.Linear);
        if (timeChange == 0) {
            return 0f;
        }
        plateProperties.CreateRelMoveTween("elevator_move",
            new Vector3(0f, deltaElevatorChange, 0f),
            timeChange, Ease.Linear);
        currentElevation += deltaElevatorChange;
        return timeChange;
    }
    private IEnumerator moveCoroutine()
    {
        while (floors > 0){
            cur_floor = (ushort)math.clamp(cur_floor + (upDirection ? 1 : -1), 0, floors - 1);
            do
            {
                float timeWait = MoveYBy(cur_floor * deltaElevation - currentElevation);
                if (timeWait != 0f)
                    yield return new WaitForSeconds(timeWait);
                else
                    break;
            } while (true);
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 4f));
            if (cur_floor == 0 && !upDirection)
            {
                upDirection = true; // Change direction if at the bottom
            }
            else if (cur_floor == floors - 1 && upDirection)
            {
                upDirection = false; // Change direction if at the top
            }
        }
        moveRoutine = null;
    }
    private void OnEnable()
    {
        plateProperties = GetComponent<PlateProperties2>();
        moveRoutine = StartCoroutine(moveCoroutine());
    }
    public void SetValues(float newDeltaElevation, float newSpeedPerSecond)
    {
        newDeltaElevation = Mathf.Min(newDeltaElevation, 25f);
        newSpeedPerSecond = Mathf.Min(newSpeedPerSecond, 20f);
        deltaElevation = newDeltaElevation;
        speedPerSecond = newSpeedPerSecond;
        if (moveRoutine == null)
        {
            moveRoutine = StartCoroutine(moveCoroutine());
        }
    }
}
