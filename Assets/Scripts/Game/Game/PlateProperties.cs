using UnityEngine;
using DG.Tweening;
using System;

public class PlateProperties : MonoBehaviour
{
    [Header("Modifiable Stats")]
    public Vector3 plateSize { get; private set; }
    public Vector3 platePos { get; private set; }
    public Vector3 plateShake { get; private set; }
    private Tween plateSizeTween_;
    private Tween platePosTween_;
    private Tween plateShakeTween_;

    private float computeTime(Vector3 newVec, Vector3 oldVec) {
        return Mathf.Sqrt(
            Mathf.Pow(newVec.x - oldVec.x, 2) +
            Mathf.Pow(newVec.y - oldVec.y, 2) +
            Mathf.Pow(newVec.z - oldVec.z, 2)
        );
    }

    public void setPlateSize(Vector3 newSize, float time = -1f, Ease easeMethod = Ease.OutQuad)
    {
        newSize = Vector3.Max(newSize, Vector3.zero);
        if (time < 0f)
        {
            time = computeTime(newSize, plateSize) / 2f;
        }
        //Debug.Log($"Setting plate size to {newSize}");
        plateSize = newSize;
        if (plateSizeTween_ != null)
        {
            plateSizeTween_.Kill();
        }
        plateSizeTween_ = transform.DOScale(newSize, 2f);
        plateSizeTween_.SetEase(easeMethod);
        plateSizeTween_.OnComplete(() => {
            plateSizeTween_ = null;
            if (plateSize.x <= 0 || plateSize.y <= 0 || plateSize.z <= 0) {
                Destroy(gameObject);
                Destroy(this);
            }
        });
    }
    public void setPlatePos(Vector3 newPos, float time = -1f, Ease easeMethod = Ease.OutQuad)
    {
        if (time < 0f)
        {
            time = computeTime(newPos, platePos);
        }
        //Debug.Log($"Setting plate position to {newPos}");
        platePos = newPos;
        if (platePosTween_ != null)
        {
            platePosTween_.Kill();
        }
        if (plateShakeTween_ != null)
        {
            plateShakeTween_.Kill();
        }
        platePosTween_ = transform.DOMove(newPos, time);
        platePosTween_.SetEase(easeMethod);
        //platePosTween_.OnUpdate(() =>
        //{
        //    if (plateShake != Vector3.zero)
        //    {
        //        if (plateShakeTween_ != null)
        //        {
        //            plateShakeTween_.Kill();
        //        }
        //        plateShakeTween_ = transform.DOShakePosition(0.1f, plateShake, 10, 90, false, true);
        //    }
        //});
        platePosTween_.OnComplete(() =>
        {
            platePosTween_ = null;
            shakeInator(plateShake);
        });
    }
    private void shakeInator(Vector3 magnitude)
    {
        if (plateShakeTween_ != null)
        {
            plateShakeTween_.Kill(true);
        }
        plateShakeTween_ = transform.DOShakePosition(2f, magnitude, 4, 90, false, false);
        plateShakeTween_.SetLoops(-1, LoopType.Restart);
        //plateShakeTween_.OnComplete(() => shakeInator(magnitude));
    }
    public void setPlateShake(Vector3 magnitude)
    {
        if (magnitude == plateShake) return;
        plateShake = magnitude;
        shakeInator(magnitude);
    }
    void Awake()
    {
        platePos = transform.position;
        plateSize = transform.localScale;
        plateShake = Vector3.zero;
    }
}
