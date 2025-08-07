using DG.Tweening;
using DG.Tweening.Core.Easing;
using Mirror;
using StarterAssets;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class GameCanvasMain : MonoBehaviour
{
    public static GameCanvasMain Instance;
    private GameCanvasElems _gameCanvasElements;
    private Coroutine _topbar_coroutine;
    private void Awake()
    {
        Assert.IsTrue(Instance == null);
        _gameCanvasElements = GetComponent<GameCanvasElems>();
        Instance = this;
    }
    public void UpdateTopBar(GameMessage newText) {
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
    public void SetCanvasGroup(CanvasGroup group, float duration = 2f) {
        foreach (CanvasGroup child in GetComponentsInChildren<CanvasGroup>(true)) {
            if (child.gameObject == gameObject)
                continue;
            DOTween.Kill(child);
            bool enabled = child == group;
            Tween tween = DOTween.To(() => child.alpha, x => child.alpha = x, enabled? 1: 0, duration);
            tween.SetUpdate(true);
            tween.SetTarget(child);
            child.interactable = child.blocksRaycasts = enabled;
        }
    }
    public IEnumerator PlayerDied() {
        if (ServerProperties.Instance.SinglePlayer) {
            if (_topbar_coroutine != null)
                StopCoroutine(_topbar_coroutine);
            StarterAssetsInputs.Instance.SetControlsEnabled("Menu", false);
            _gameCanvasElements.defaultGroup.alpha = 1f;
            _gameCanvasElements.deathGroup.alpha = 0f;

            SetCanvasGroup(null, 2f);

            CameraController.Instance.SetActiveCamera("Death");
            LobbyUI.Instance.TweenTimeScale(0f, 6f);

            yield return new WaitForSecondsRealtime(4f);
            if (_gameCanvasElements != null && _gameCanvasElements.deathGroup != null)
                SetCanvasGroup(_gameCanvasElements.deathGroup, 1.8f);
        }
        yield return null;
    }
    public void SinglePlayer_RetryGame() {
        _gameCanvasElements.deathGroup.interactable = false;
        GameMenuUI.Instance.DisableAllUI();

        LobbyUI.Instance.RemoveLock();
        SingleplayerMenu.Instance.SingleplayerStartActivated();
    }
}
