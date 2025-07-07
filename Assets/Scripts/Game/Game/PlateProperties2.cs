using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
//using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlateProperties2.PlateAddable;
using static UnityEngine.Rendering.DebugUI;
using Mirror;


public class PlateProperties2 : NetworkBehaviour {

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
        //public Coroutine enumerator = null;
        public List<TweenInstance> activeInstances = new List<TweenInstance>();
        public Action<TweenEnumerator, Vector3, Vector3> callback;
        public Vector3 absoluteValue;
        public Vector3 tempOffset = Vector3.zero;
        public Vector3 permOffset = Vector3.zero;
        public Vector3 prevValue = Vector3.zero;
    };
    private Dictionary<Transform, PlateAddable> addables_list = new();
    public Transform[] addable_containers;
    public class PlateAddable {
        public PlateAddable(Transform trans, Vector3 scalePos, Vector3 offsetPos, PlateAddableType new_type) {
            transform = trans;
            scalePosition = scalePos;
            offsetPosition = offsetPos;
            type = new_type;
        }
        public PlateAddable(Transform trans) {
            PlateAddableProperties addable_prop = trans.GetComponent<PlateAddableProperties>();
            transform = trans;
            scalePosition = addable_prop.scalePosition;
            offsetPosition = addable_prop.offsetPosition;
            type = addable_prop.type;
        }
        public Transform transform;
        public Vector3 scalePosition;
        public Vector3 offsetPosition;
        public PlateAddableType type;
        public enum PlateAddableType : ushort {
            All,
            PositionAndRotation,
            Position,
        }
        public void Update(Vector3 newScale) {
            transform.localPosition = Vector3.Scale(scalePosition, newScale) + offsetPosition
                + scalePosition.y * Vector3.up * transform.localScale.y;
        }
    }
    [Server]
    public void InsertPlateAddable(PlateAddable addable) {
        addables_list.Add(addable.transform, addable);
        addable.transform.SetParent((Transform) addable_containers.GetValue((int) addable.type));
        addable.Update(tweenEnumerators["localScale"].absoluteValue);
        NetworkServer.Spawn(addable.transform.gameObject);
    }
    [Server]
    public void InsertPlateAddable(Transform newTrans) {
        Transform instance = Instantiate(newTrans);
        PlateAddable addable = new(instance);
        InsertPlateAddable(addable);
    }
    [Server]
    public void RemovePlateAddable(PlateAddable addable, bool delete = true) {
        addables_list.Remove(addable.transform);
        if (delete) {
            Destroy(addable.transform);
        }
    }
    private Rigidbody rb;
    public Rigidbody physics_rb;
    public Transform rotation;
    public Transform render;
    //public Transform all_addables, pos_addables, pos_rot_addables;
    private Dictionary<string, TweenEnumerator> tweenEnumerators;
    public float mass_density = 1f;
    public float stability { get; private set; } = 1f;

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
        //Debug.Log($"strength - {strength.sqrMagnitude} | newvec - {newVec.sqrMagnitude}");
        return newVec;
    }
    public void DoMassUpdate() {
        Vector3 value = tweenEnumerators["localScale"].absoluteValue;
        rb.mass = value.x * value.y * value.z * mass_density;
        physics_rb.mass = rb.mass * stability;
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        tweenEnumerators = new Dictionary<string, TweenEnumerator>
        {
            ["localScale"] = new TweenEnumerator {
                name = "localScale",
                absoluteValue = render.localScale,
                callback = (TweenEnumerator self, Vector3 value, Vector3 previousScale) => {
                    if (value.x <= 0f || value.y <= 0f || value.z <= 0f) {
                        value = Vector3.zero;
                        Destroy(gameObject);
                        Destroy(this);
                    }
                    //float deltaY = value.y - render.localScale.y;
                    //if (deltaY != 0f) {
                    //    tweenEnumerators["position"].permOffset += deltaY / 2 * Vector3.up;
                    //}

                    // Apply ratio to children
                    foreach (KeyValuePair<Transform, PlateAddable> pair in addables_list) {
                        pair.Value.Update(value);
                    }
                    previousScale = value;
                    DoMassUpdate();
                    render.localScale = value;
                }
            },
            ["position"] = new TweenEnumerator {
                name = "position",
                absoluteValue = transform.position + render.localScale.y/2 * Vector3.up,
                callback = (TweenEnumerator self, Vector3 value, Vector3 previousPosition) => {
                    //rb.position = value;
                    transform.localPosition = value;
                    //rotation_rb.MovePosition(value);
                    //rotation_rb.position = value;
                    //rotation_rb.transform.localPosition = Vector3.zero;
                    //rb.MovePosition(value);

                }
            },
            ["rotation"] = new TweenEnumerator
            {
                name = "rotation",
                absoluteValue = transform.localRotation.eulerAngles,
                callback = (TweenEnumerator self, Vector3 value, Vector3 previousRotation) =>
                {
                    rotation.localRotation = Quaternion.Euler(value);
                }
            }
        };
        foreach (TweenEnumerator tweenEnum in tweenEnumerators.Values) {
            //if (tweenEnum.name != "localScale") {
            tweenEnum.prevValue = tweenEnum.absoluteValue;
            tweenEnum.callback(tweenEnum, tweenEnum.absoluteValue, tweenEnum.prevValue);
            tweenEnum.prevValue = tweenEnum.absoluteValue;
            //}
        }
    }
    IEnumerator Run(TweenEnumerator enumerator) {
        //while (enumerator.activeInstances.Count > 0) {
        //    enumerator.tempOffset = Vector3.zero;
        //    for (int i = enumerator.activeInstances.Count - 1; i >= 0; i--){
        //        TweenInstance tweenInst = enumerator.activeInstances[i];
        //        tweenInst.tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        //        if (!tweenInst.isComplete) {
        //            if (tweenInst.isRelative)
        //                enumerator.tempOffset += tweenInst.value;
        //            else
        //                enumerator.absoluteValue = tweenInst.value;
        //        }
        //    }
        //    Vector3 summation = enumerator.absoluteValue + enumerator.tempOffset + enumerator.permOffset;
        //    enumerator.callback(enumerator, summation);
        //    yield return null;
        //}
        //enumerator.enumerator = null;
        yield break;
    }
    private void FixedUpdate() {
        foreach (var enumerator in tweenEnumerators.Values) {
            enumerator.tempOffset = Vector3.zero;
            for (int i = enumerator.activeInstances.Count - 1; i >= 0; i--) {
                TweenInstance tweenInst = enumerator.activeInstances[i];
                tweenInst.tween.ManualUpdate(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
                if (!tweenInst.isComplete) {
                    if (tweenInst.isRelative)
                        enumerator.tempOffset += tweenInst.value;
                    else
                        enumerator.absoluteValue = tweenInst.value;
                }
            }
            Vector3 summation = enumerator.absoluteValue + enumerator.tempOffset + enumerator.permOffset;
            if (enumerator.prevValue != summation) {
                enumerator.callback(enumerator, summation, enumerator.prevValue);
                enumerator.prevValue = summation;
            }
        }
    }
    //private void FixedUpdate() {
        //rotation_rb.transform.localPosition = Vector3.zero;
    //}
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
        //Assert.IsNotNull(enumer, $"No enumerator found for {name}");
        tweenInstance.tween.SetUpdate(UpdateType.Manual);
        enumer.activeInstances.Add(tweenInstance);
        if (!tweenInstance.isPermanent)
            tweenInstance.tween.OnComplete(() => {
                //tweenInstance.tween.Kill();
                RemoveTweenFromList(name, (theirInstance) => theirInstance == tweenInstance);
                enumer.permOffset += tweenInstance.value;
            });
        //if (enumer.enumerator == null) {
            //enumer.enumerator = StartCoroutine(Run(enumer));
        //}
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
        Vector3 strength, float duration = 0.3f, Tuple<float, float> delayRange = null, bool forceStart = false) {
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
        .SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo)
        .SetDelay(UnityEngine.Random.Range(delayRange.Item1, delayRange.Item2))
        .SetTarget(transform).SetAutoKill(true);
        tweenInstance.tween.onComplete = () => CreateShakeTween(fromName, tweenInstance.strength, duration, delayRange, true);
        AddTweenToList("position", tweenInstance);
        //Debug.Log($"Creating shake tween {fromName} with strength {strength} to point {to}");
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
    public void SetPlateUnstable(bool unstable, float stability) {
        stability = Mathf.Min(0.25f, stability);
        physics_rb.freezeRotation = !unstable;
        DoMassUpdate();
    }
    public void InsertAddable(Transform transform, PlateAddable plateAddable) {

    }
}
