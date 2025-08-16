using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class SingleplayerTimeGUI : MonoBehaviour
{
    private static TextMeshProUGUI timer;
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
    private static double timerValue = 0d;
    private void Start()
    {
        if (!ServerProperties.Instance.SinglePlayer) {
            Destroy(gameObject);
            Destroy(this);
        }
        timerValue = 0d;
        timer = GetComponent<TextMeshProUGUI>();
    }
    public static void UpdateDisplay(double time) {
        timerValue = Math.Max(timerValue, time);
        timer.text = DisplayTimePassed(timerValue);
    }
    private void OnGUI() {
        if (ServerProperties.Instance.GameInProgress)
            if (ServerProperties.Instance.AlivePlayers > 0)
                UpdateDisplay(ServerProperties.Instance.GameDuration);
            else
                timer.text = string.Empty;
    }
    
}
