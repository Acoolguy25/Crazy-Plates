using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class SingleplayerTimeGUI : MonoBehaviour
{
    private TextMeshProUGUI timer;
    public static string DisplayTimePassed(double timepassed) {
        var timeStruct = System.TimeSpan.FromSeconds(timepassed);
        if (timeStruct.TotalHours >= 1) {
            return timeStruct.ToString(@"hh\:mm\:ss\.ff");
        }
        else if (timeStruct.TotalMinutes >= 1) {
            return timeStruct.ToString(@"mm\:ss\.ff");
        }
        else {
            return timeStruct.ToString(@"ss\.ff");
        }
    }
    private void Start()
    {
        if (!ServerProperties.Instance.SinglePlayer) {
            Destroy(gameObject);
            Destroy(this);
        }
        timer = GetComponent<TextMeshProUGUI>();
    }
    private void OnGUI() {
        if (ServerProperties.Instance.GameInProgress)
            if (ServerProperties.Instance.AlivePlayers > 0)
                timer.text = DisplayTimePassed(ServerProperties.Instance.GameDuration);
            else if (GameEvents.Instance.SurvivalTime != 0d)
                timer.text = DisplayTimePassed(GameEvents.Instance.SurvivalTime);
            else
                timer.text = string.Empty;
    }
}
