using DG.Tweening;
using DG.Tweening.Core.Easing;
using Mirror;
using StarterAssets;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class GameCanvasMain : MonoBehaviour
{
    public static GameCanvasMain Instance;
    public static CanvasGroup SelectedGroup = null;
    public Dictionary<string, LockUI> GameLocks = new();
    private GameCanvasElems _gameCanvasElements;
    private Coroutine _topbar_coroutine;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
        SelectedGroup = null;
    }
    private void Awake()
    {
        Assert.IsTrue(Instance == null);
        _gameCanvasElements = GetComponent<GameCanvasElems>();
        Instance = this;
        foreach (CanvasGroup group in transform.GetComponentsInChildren<CanvasGroup>(true)){
            if (group.gameObject == gameObject)
                continue;
            GameLocks.Add(group.name, group.GetComponent<LockUI>());
        }
        SetCanvasGroup(_gameCanvasElements.defaultGroup, 0f);
    }
    //private void Start() {
        
    //}
    public void UpdateTopBar(GameMessage newText) {
        if (_topbar_coroutine != null)
            StopCoroutine(_topbar_coroutine);
        _topbar_coroutine = StartCoroutine(UpdateTopBar_(newText));
    }
    private IEnumerator UpdateTopBar_(GameMessage newText)
    {
        if (newText.Duration != Double.MinValue)
        {
            float timeToWait, roundedTime;
            do
            {
                timeToWait = (float) (newText.Duration - SharedFunctions.GetNetworkTime());
                roundedTime = Mathf.Max(0f, Mathf.Floor(timeToWait * 10f) / 10f); // Round to 1 decimal place
                _gameCanvasElements.title.text = string.Format(newText.Message, roundedTime.ToString("F1"));
                if (roundedTime <= 0f)
                {
                    break;
                }
                yield return new WaitForSeconds(roundedTime - timeToWait);
            } while (true);
        }
        else
        {
            _gameCanvasElements.title.text = newText.Message;
        }
        _topbar_coroutine = null;
    }
    public void UpdateDescBar(string newText)
    {
        _gameCanvasElements.desc.text = newText;
    }
    public void UpdateSurvivalTime(double newTime){
        _gameCanvasElements.highscore.text = SingleplayerTimeGUI.DisplayTimePassed(newTime);
    }
    public void SetCanvasGroup(CanvasGroup group, float duration = 2f) {
        if (SelectedGroup == group)
            return;
        foreach (CanvasGroup child in GetComponentsInChildren<CanvasGroup>(true)) {
            if (child.gameObject == gameObject)
                continue;
            DOTween.Kill(child);
            bool enabled = child == group;
            //Tween tween = DOTween.To(() => child.alpha, x => child.alpha = x, enabled? 1: 0, duration);
            //tween.SetUpdate(true);
            //tween.SetTarget(child);

            //child.interactable = child.blocksRaycasts = enabled;
            //GameLocks[child.name].ToggleLock(!enabled);
            GenericTweens.TweenCanvasGroup(child, enabled ? 1 : 0, duration, GameLocks[child.name]);
        }
        SelectedGroup = group;
    }
    private void OnDisable() {
        if (_topbar_coroutine != null)
            StopCoroutine(_topbar_coroutine);
    }
}
