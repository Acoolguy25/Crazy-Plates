using DG.Tweening;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public static class GenericTweens {
    public static void SetCanvasGroupEnabled(LockUI lockUI, bool enabled) {
        Assert.IsNotNull(lockUI, "SetCanvasGroupEnabled called but lockUI is null");
        //canvasGroup.interactable = enabled;
        //canvasGroup.blocksRaycasts = enabled;
        if (lockUI.genericLock != enabled) {
            //Debug.Log($"SetCanvasGroup {(enabled? "Enabled": "Disabled")} {lockUI.name}");
            lockUI.ToggleLock(enabled);
            lockUI.genericLock = enabled;
        }
    }
    public static Tween TweenCanvasGroup(CanvasGroup canvasGroup, float alpha, float duration, LockUI lockUI = null) {
        Assert.IsNotNull(canvasGroup, "TweenCanvasGroup called but canvasGroup is null!");
        canvasGroup.DOKill();
        if (alpha == 0f && lockUI)
            SetCanvasGroupEnabled(lockUI, true);
        Tween tween = DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, alpha, duration)
        .SetUpdate(true) // UI always uses unscaled time
        .SetTarget(canvasGroup)
        .SetAutoKill(true);
        if (alpha == 1f && lockUI) {
            //tween.onComplete += () => SetCanvasGroupEnabled(lockUI, false);
            tween.onKill += () => SetCanvasGroupEnabled(lockUI, false);
        }
        return tween;
    }
    public static Tween TweenImage(Image image, float alpha, float duration) {
        image.DOKill();
        Tween tween = DOTween.To(() => image.color.a, x => image.color = new Color(image.color.r, image.color.g, image.color.b, x), alpha, duration)
        .SetUpdate(true) // UI always uses unscaled time
        .SetTarget(image);
        return tween;
    }
}
