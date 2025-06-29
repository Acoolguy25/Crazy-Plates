using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.MaterialProperty;


public class PlateProperties2 : MonoBehaviour {
    private class TweenInstance {
        public string name = "<unk>";
        public Tween tween;
        public Vector3 value = Vector3.zero;
        public bool isRelative = true;
        public bool isComplete => tween == null || !tween.IsActive() || !tween.IsPlaying();
        public Vector3 strength = Vector3.zero;
        public bool isPermanent = false;
    };
    private class TweenEnumerator {
        public string name;
        public Coroutine enumerator = null;
        public List<TweenInstance> activeInstances = new List<TweenInstance>();
        public Action<TweenEnumerator, Vector3> callback;
        public Vector3 absoluteValue;
        public Vector3 tempOffset = Vector3.zero;
        public Vector3 permOffset = Vector3.zero;
    };
    private Rigidbody rb;

    public Transform render;
    private Dictionary<string, TweenEnumerator> tweenEnumerators;

    public Vector3 GetPointOnValidAxes(Vector3 strength) {
        Vector3 dir = UnityEngine.Random.onUnitSphere;
        if (strength.x == 0) dir.x = 0;
        if (strength.y == 0) dir.y = 0;
        if (strength.z == 0) dir.z = 0;
        dir.Normalize();
        dir *= strength.magnitude;
        Vector3 normStrength = strength;
        normStrength.Normalize();
        Vector3 newVec = new Vector3(
            dir.x * normStrength.x,
            dir.y * normStrength.y,
            dir.z * normStrength.z
        ) * Mathf.Sqrt(2);
        Debug.Log($"strength - {strength.sqrMagnitude} | newvec - {newVec.sqrMagnitude}");
        return newVec;
    }

    void Awake() {
        rb = GetComponent<Rigidbody>();
        tweenEnumerators = new Dictionary<string, TweenEnumerator>
        {
            ["localScale"] = new TweenEnumerator {
                name = "localScale",
                absoluteValue = render.localScale,
                callback = (TweenEnumerator self, Vector3 value) => {
                    if (value.x <= 0f || value.y <= 0f || value.z <= 0f) {
                        value = Vector3.zero;
                        Destroy(gameObject);
                        Destroy(this);
                    }
                    //float deltaY = value.y - render.localScale.y;
                    //if (deltaY != 0f) {
                    //    tweenEnumerators["position"].permOffset += deltaY / 2 * Vector3.up;
                    //}
                    render.localScale = value;
                }
            },
            ["position"] = new TweenEnumerator {
                name = "position",
                absoluteValue = rb.position,
                callback = (TweenEnumerator self, Vector3 value) => {
                    //rb.position = value;
                    transform.localPosition = value;
                    //rb.MovePosition(value);
                }
            },
            ["rotation"] = new TweenEnumerator
            {
                name = "rotation",
                absoluteValue = rb.rotation.eulerAngles,
                callback = (TweenEnumerator self, Vector3 value) =>
                {
                    rb.MoveRotation(Quaternion.Euler(value));
                }
            }
        };
    }
    IEnumerator Run(TweenEnumerator enumerator) {
        while (enumerator.activeInstances.Count > 0) {
            enumerator.tempOffset = Vector3.zero;
            for (int i = enumerator.activeInstances.Count - 1; i >= 0; i--){
                TweenInstance tweenInst = enumerator.activeInstances[i];
                tweenInst.tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                if (!tweenInst.isComplete) {
                    if (tweenInst.isRelative)
                        enumerator.tempOffset += tweenInst.value;
                    else
                        enumerator.absoluteValue = tweenInst.value;
                }
            }
            Vector3 summation = enumerator.absoluteValue + enumerator.tempOffset + enumerator.permOffset;
            enumerator.callback(enumerator, summation);
            yield return null;
        }
        enumerator.enumerator = null;
    }
    private void FindTweenInList(string name, Func<TweenInstance, bool> check, out TweenInstance found) {
        found = null;
        List<TweenInstance> checkList = tweenEnumerators[name].activeInstances;
        for (int i = checkList.Count - 1; i >= 0; i--) {
            TweenInstance tweenInst = checkList[i];
            if (check(tweenInst)) {
                found = tweenInst;
                return;
            }
        }
    }
    private void AddTweenToList(string name, TweenInstance tweenInstance) {
        //Debug.Log($"Adding tween to {name} list: {tweenInstance.name}");
        TweenEnumerator enumer = tweenEnumerators[name];
        Assert.IsNotNull(enumer, $"No enumerator found for {name}");
        tweenInstance.tween.SetUpdate(UpdateType.Manual);
        enumer.activeInstances.Add(tweenInstance);
        if (!tweenInstance.isPermanent)
            tweenInstance.tween.OnComplete(() => {
                //tweenInstance.tween.Kill();
                RemoveTweenFromList(name, (theirInstance) => theirInstance == tweenInstance);
                enumer.permOffset += tweenInstance.value;
            });
        if (enumer.enumerator == null) {
            enumer.enumerator = StartCoroutine(Run(enumer));
        }
    }
    private void RemoveTweenFromList(string name, Func<TweenInstance, bool> check) {
        List<TweenInstance> checkList = tweenEnumerators[name].activeInstances;
        for (int i = checkList.Count - 1; i >= 0; i--) {
            TweenInstance tweenInst = checkList[i];
            if (check(tweenInst)) {
                tweenInst.tween.Kill();
                tweenEnumerators[name].activeInstances.Remove(tweenInst);
            }
        }
    }
    public void CreateShakeTween(string fromName,
        Vector3 strength, float duration = 2f, Ease easeMethod = Ease.OutBounce, bool forceStart = false) {
        bool doBreak = false;
        RemoveTweenFromList("position", (TweenInstance theirInstance) =>
        {
            if (theirInstance.name == fromName) {
                if (!forceStart) {
                    theirInstance.strength += strength;
                    doBreak = true;
                }
                return forceStart;
            }
            return false;
        });
        if (doBreak)
            return;
        TweenInstance tweenInstance = new TweenInstance {
            name = fromName,
            isRelative = true,
            strength = strength,
            isPermanent = true,
        };
        Vector3 to = GetPointOnValidAxes(strength);
        tweenInstance.tween = DOTween.To(() => tweenInstance.value, delegate (Vector3 x) {
            tweenInstance.value = x;
        }, to, duration)
        .SetEase(easeMethod).SetLoops(2, LoopType.Yoyo)
        .SetTarget(transform).SetAutoKill(true);
        AddTweenToList("position", tweenInstance);
        tweenInstance.tween.onComplete = () => CreateShakeTween(fromName, tweenInstance.strength, duration, easeMethod, true);
        Debug.Log($"Creating shake tween {fromName} with strength {strength} to point {to}");
    }
    public Tween CreateRelMoveTween(string fromName,
        Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear) {
        return CreateBasicTween("position", fromName, to, time, easeMethod, isRel: true);
    }
    public void CreateAbsMoveTween(string fromName,
        Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear) {
        CreateBasicTween("position", fromName, to, time, easeMethod, isRel: false);
    }
    public void CreateRelSizeTween(string fromName,
        Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear) {
        CreateBasicTween("localScale", fromName, to, time, easeMethod, isRel: true);
        if (to.y != 0f) {
            CreateBasicTween("position", fromName, to.y / 2 * Vector3.up, time, easeMethod, isRel: true);
        }
    }
    public void CreateRelRotation(string fromName,
        Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear,
        int loops = 0, LoopType loopType = LoopType.Restart) {
        CreateBasicTween("rotation", fromName, to, time, easeMethod, loops, loopType, isRel: true);
    }
    public Tween CreateBasicTween(string name, string fromName,
        Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear,
        int loops = 0, LoopType loopType = LoopType.Restart, bool isRel = true) {
        TweenInstance tweenInstance = new TweenInstance
        {
            name = fromName,
            isRelative = isRel,
            value = Vector3.zero
        };
        tweenInstance.tween = DOTween.To(() => tweenInstance.value, delegate (Vector3 x) {
            tweenInstance.value = x;
        }, to, time)
        .SetEase(easeMethod).SetLoops(loops, loopType)
        .SetTarget(transform).SetAutoKill(true);
        AddTweenToList(name, tweenInstance);
        return tweenInstance.tween;
    }
}
