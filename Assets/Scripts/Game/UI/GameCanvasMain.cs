using DG.Tweening.Core.Easing;
using System;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using Mirror;

public class GameCanvasMain : MonoBehaviour
{
    private GameCanvasElems _gameCanvasElements;
    private void Awake()
    {
        _gameCanvasElements = GetComponent<GameCanvasElems>();
    }
    public IEnumerator UpdateTopBar(GameMessage newText)
    {
        if (newText.Duration != Double.MinValue)
        {
            float timeToWait, roundedTime;
            do
            {
                timeToWait = (float) (newText.Duration - NetworkTime.time);
                roundedTime = Mathf.Max(0f, Mathf.Floor(timeToWait * 10f) / 10f); // Round to 1 decimal place
                _gameCanvasElements.title.text = string.Format(newText.Message, roundedTime.ToString("F1"));
                if (roundedTime <= 0f)
                {
                    yield break;
                }
                yield return new WaitForSeconds(roundedTime - timeToWait);
            } while (true);
        }
        else
        {
            _gameCanvasElements.title.text = newText.Message;
            yield break;
        }
    }
    public void UpdateDescBar(string newText)
    {
        _gameCanvasElements.desc.text = newText;
    }
}
