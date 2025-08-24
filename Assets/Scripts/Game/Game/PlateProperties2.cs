using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using Mirror;
//using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Plate {
    public class CustomTweenParams {
        [SyncVar] public Ease ease;
        [SyncVar] public int loops;
        [SyncVar] public LoopType loopType;
        [SyncVar] public Vector3 strength = Vector3.zero;
    }
    public class TweenInstance {
        [SyncVar] public string name = "<unk>";
        public Tween tween;
        public Vector3 value = Vector3.zero;
        [SyncVar] public CustomTweenParams tweenParams;
        [SyncVar] public bool isRelative = true;
        [SyncVar] public Vector3 goal;
        [SyncVar] public double startTime;
        [SyncVar] public float duration;

        public Action onFinished;
    };
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
    public class TweenEnumerator {
        public string name;
        //public Coroutine enumerator = null;
        public SyncList<TweenInstance> activeInstances = new();
        public Action<TweenEnumerator, Vector3, Vector3> callback;
        public Vector3 absoluteValue;
        public Vector3 tempOffset = Vector3.zero;
        [SyncVar] public Vector3 permOffset = Vector3.zero;
        public Vector3 prevValue = Vector3.zero;
    };
    public class PlateProperties2 : NetworkBehaviour {
        
        private Dictionary<Transform, PlateAddable> addables_list = new();
        public Vector3 shakeStrength { get; private set; } = Vector3.zero;
        public Transform[] addable_containers;
        public void InsertPlateAddable(PlateAddable addable) {
            addables_list.Add(addable.transform, addable);
            addable.transform.SetParent((Transform)addable_containers.GetValue((int)addable.type));
            addable.Update(tweenEnumerators["localScale"].absoluteValue);
            if (NetworkServer.active)
                NetworkServer.Spawn(addable.transform.gameObject);
        }
        public void InsertPlateAddable(Transform newTrans) {
            Transform instance = Instantiate(newTrans);
            PlateAddable addable = new(instance);
            InsertPlateAddable(addable);
        }
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

        private readonly SyncDictionary<string, TweenEnumerator> tweenEnumerators = new();
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
        private void TweenCompleted(TweenEnumerator tweenEnum, TweenInstance tweenInstance) {
            if (tweenInstance.tweenParams.loopType != LoopType.Yoyo) {
                if (tweenInstance.isRelative)
                    tweenEnum.permOffset += tweenInstance.goal;
                else
                    tweenEnum.absoluteValue = tweenInstance.goal;
            }

            if (isServer) {
                tweenEnum.activeInstances.Remove(tweenInstance);
                tweenInstance.onFinished?.Invoke();
            }
            tweenInstance.tween = null;
        }
        void OnAddToSyncList(TweenEnumerator tweenEnum, TweenInstance tweenInstance) {
            CustomTweenParams my_params = tweenInstance.tweenParams;
            float timeProgressed = (float)(SharedFunctions.GetNetworkTime() - tweenInstance.startTime);
            tweenInstance.tween = DOTween.To(() => tweenInstance.value, delegate (Vector3 x) {
                tweenInstance.value = x;
            }, tweenInstance.goal, tweenInstance.duration)
            .SetEase(my_params.ease).SetLoops(my_params.loops, my_params.loopType)
            .SetTarget(transform).SetAutoKill(true)
            .SetUpdate(UpdateType.Manual, false)
            .OnComplete(() => TweenCompleted(tweenEnum, tweenInstance));
            //Debug.Log($"Adding tween {tweenInstance.name} to {tweenEnum.name} list with duration" +
            //$"{tweenInstance.duration}, timeProgressed {timeProgressed}");
            if (timeProgressed < 0)
                tweenInstance.tween.SetDelay(-timeProgressed);
            else
                tweenInstance.tween.ManualUpdate(timeProgressed, timeProgressed);
        }
        [Client]
        void OnAddToSyncListClient(TweenEnumerator tweenEnum, TweenInstance tweenInstance) {
            OnAddToSyncList(tweenEnum, tweenInstance);
        }
        void Start() {
            rb = GetComponent<Rigidbody>();
            if (isServer) {
                tweenEnumerators.Add("localScale", new TweenEnumerator{
                    name = "localScale",
                    absoluteValue = render.localScale,
                });
                tweenEnumerators.Add("position", new TweenEnumerator{
                    name = "position",
                    absoluteValue = transform.position + render.localScale.y / 2 * Vector3.up,
                });
                tweenEnumerators.Add("rotation", new TweenEnumerator{
                    name = "rotation",
                    absoluteValue = transform.localRotation.eulerAngles,
                });
            }
            tweenEnumerators.OnAdd += OnTweenEnumeratorAdded;
            foreach (string tweenEnumKey in tweenEnumerators.Keys) {
                OnTweenEnumeratorAdded(tweenEnumKey);
            }
        }
        private void OnTweenEnumeratorAdded(string key) {
            TweenEnumerator tweenEnum = tweenEnumerators[key];
            if (isClient) {
                if (key == "localScale") {
                    tweenEnum.callback = (TweenEnumerator self, Vector3 value, Vector3 previousScale) =>
                    {
                        if (value.x <= 0f || value.y <= 0f || value.z <= 0f) {
                            value = Vector3.zero;
                            Destroy(gameObject);
                            Destroy(this);
                        }
                        // Apply ratio to children
                        foreach (KeyValuePair<Transform, PlateAddable> pair in addables_list) {
                            pair.Value.Update(value);
                        }
                        previousScale = value;
                        DoMassUpdate();
                        render.localScale = value;
                    };
                }
                else if (key == "position") {
                    tweenEnum.callback = (TweenEnumerator self, Vector3 value, Vector3 previousPosition) =>
                    {
                        transform.localPosition = value;
                    };
                }
                else if (key == "rotation") {
                    tweenEnum.callback = (TweenEnumerator self, Vector3 value, Vector3 previousRotation) =>
                    {
                        rotation.localRotation = Quaternion.Euler(value);
                    };
                }
                else {
                    Debug.LogError("Unknown TweenEnumerator Key: " + key);
                    return;
                }
                tweenEnum.activeInstances.OnAdd += ((int idx) =>
                {
                    //Debug.Log($"Adding tween {tweenInstance.name} to {tweenEnum.name} list");
                    OnAddToSyncList(tweenEnum, tweenEnum.activeInstances[idx]);
                });
                foreach (TweenInstance tweenInstance in tweenEnum.activeInstances) {
                    //Debug.Log($"Adding existing tween {tweenInstance.name} to {tweenEnum.name} list");
                    OnAddToSyncList(tweenEnum, tweenInstance);
                }
            }
            tweenEnum.prevValue = tweenEnum.absoluteValue;
            tweenEnum.callback(tweenEnum, tweenEnum.absoluteValue, tweenEnum.prevValue);
            tweenEnum.prevValue = tweenEnum.absoluteValue;

        }
        private void FixedUpdate() {
            foreach (var enumerator in tweenEnumerators.Values) {
                enumerator.tempOffset = Vector3.zero;
                for (int i = enumerator.activeInstances.Count - 1; i >= 0; i--) {
                    TweenInstance tweenInst = enumerator.activeInstances[i];
                    if (isClient) {
                        tweenInst.tween.ManualUpdate(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
                        if (tweenInst.tween != null) {
                            if (tweenInst.isRelative)
                                enumerator.tempOffset += tweenInst.value;
                            else
                                enumerator.absoluteValue = tweenInst.value;
                        }
                    }
                    else {
                        if (tweenInst.duration * tweenInst.tweenParams.loops + tweenInst.startTime < SharedFunctions.GetNetworkTime()) {
                            enumerator.activeInstances.RemoveAt(i);
                            continue;
                        }
                    }
                }
                if (isClient) {
                    Vector3 summation = enumerator.absoluteValue + enumerator.tempOffset + enumerator.permOffset;
                    if (enumerator.prevValue != summation) {
                        enumerator.callback(enumerator, summation, enumerator.prevValue);
                        enumerator.prevValue = summation;
                    }
                }
            }
        }
        private void AddTweenToList(string name, TweenInstance tweenInstance) {
            //Debug.Log($"Adding tween to {name} list: {tweenInstance.name}");
            TweenEnumerator enumer = tweenEnumerators[name];
            enumer.activeInstances.Add(tweenInstance);
            if (!NetworkServer.active)
                OnAddToSyncList(enumer, tweenInstance);
        }
        public void CreateShakeTween(string fromName,
            Vector3 strength, float duration = 0.3f, Tuple<float, float> delayRange = null, bool forceOverride = false) {
            if (shakeStrength != Vector3.zero && !forceOverride) {
                shakeStrength = strength;
                return;
            }
            shakeStrength = strength;

            float delay = UnityEngine.Random.Range(delayRange.Item1, delayRange.Item2);
            Vector3 to = GetPointOnValidAxes(shakeStrength);
            //Debug.Log($"Creating shake tween {fromName} with strength {strength} to point {to}");

            TweenInstance tweenInstance =
                CreateBasicTween("position", fromName, to, duration, Ease.OutQuad, 2, LoopType.Yoyo, true, delay);
            tweenInstance.onFinished += () => CreateShakeTween(fromName, shakeStrength, duration, delayRange, forceOverride = true);
        }
        public void CreateRelMoveTween(string fromName,
            Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear) {
            CreateBasicTween("position", fromName, to, time, easeMethod, isRel: true);
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
        TweenInstance CreateBasicTween(string name, string fromName,
            Vector3 to, float time = 1f, Ease easeMethod = Ease.Linear,
            int loops = 0, LoopType loopType = LoopType.Restart, bool isRel = true,
            float delay = 0f) {
            TweenInstance tweenInstance = new TweenInstance
            {
                name = fromName,
                isRelative = isRel,
                value = Vector3.zero,
                duration = time,
            };
            tweenInstance.tweenParams = new CustomTweenParams
            {
                ease = easeMethod,
                loops = loops,
                loopType = loopType
            };
            tweenInstance.goal = to;
            tweenInstance.startTime = SharedFunctions.GetNetworkTime() - delay;
            AddTweenToList(name, tweenInstance);
            return tweenInstance;
        }
        public void SetPlateUnstable(bool unstable, float stability) {
            stability = Mathf.Min(0.25f, stability);
            physics_rb.freezeRotation = !unstable;
            DoMassUpdate();
        }
        public void InsertAddable(Transform transform, PlateAddable plateAddable) {

        }
    }
}