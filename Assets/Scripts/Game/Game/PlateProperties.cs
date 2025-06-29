using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using Mirror.BouncyCastle.Asn1.X509;
using Mirror.Examples.AdditiveLevels;
using System;
using System.Collections;
using UnityEngine;
using Time = UnityEngine.Time;
public class PlateProperties : MonoBehaviour
{
    [Header("Modifiable Stats")]
    public Vector3 plateSize { get; private set; }
    public Vector3 platePos { get; private set; }
    public Vector3 plateShake { get; private set; }
    public Vector3 plateOffset { get; private set; }
    public Vector3 rotateSpeed { get; private set; }
    public bool isRotating { get; private set; }

    public Rigidbody root;
    public GameObject render;
    public GameObject offsetContainer;

    private Tween plateSizeTween_;
    private Tween platePosTween_;
    private Tween plateShakeTween_;
    private Tween platePosTweenY_;

    private float computeTime(Vector3 newVec, Vector3 oldVec) {
        //return Mathf.Sqrt(
        //    Mathf.Pow(newVec.x - oldVec.x, 2) +
        //    Mathf.Pow(newVec.y - oldVec.y, 2) +
        //    Mathf.Pow(newVec.z - oldVec.z, 2)
        //);
        return Vector3.Distance(newVec, oldVec) / 2f; // Adjusted for smoother transitions
    }

    public void setPlateSizePosOffset(float deltaY, float duration, Ease easeMethod)
    {
        if (platePosTweenY_ != null)
        {
            platePosTweenY_.Kill();
        }
        plateOffset += Vector3.up * deltaY / 2;
        platePosTweenY_ = offsetContainer.transform.DOLocalMoveY(
            plateOffset.y, duration
        ).SetEase(easeMethod);
        platePosTweenY_.OnComplete(() =>
        {
            platePosTweenY_ = null;
        });
    }

    public void setPlateSize(Vector3 newSize, float time = -1f, Ease easeMethod = Ease.OutQuad)
    {
        newSize = Vector3.Max(newSize, Vector3.zero);
        if (time < 0f)
        {
            time = computeTime(newSize, plateSize) / 2f;
        }
        //Debug.Log($"Setting plate size to {newSize}");
        if (plateSizeTween_ != null)
        {
            time += Mathf.Max(0f, plateSizeTween_.Duration() - plateSizeTween_.Elapsed());
            plateSizeTween_.Kill();
        }
        if (newSize.y > 0)
        {
            setPlateSizePosOffset(newSize.y - plateSize.y, time, easeMethod);
        }
        plateSizeTween_ = render.transform.DOScale(newSize, time);
        plateSizeTween_.SetEase(easeMethod);
        plateSizeTween_.OnComplete(() =>
        {
            plateSizeTween_ = null;
            if (plateSize.x <= 0 || plateSize.y <= 0 || plateSize.z <= 0)
            {
                Destroy(gameObject);
                Destroy(this);
            }
        });
        plateSize = newSize;
    }
    public void setPlatePos(Vector3 newPos, float time = -1f, Ease easeMethod = Ease.OutQuad)
    {
        if (time < 0f)
        {
            time = computeTime(newPos, platePos);
        }
        //Debug.Log($"Setting plate position to {newPos}");
        if (platePosTween_ != null)
        {
            time += Mathf.Max(0f, platePosTween_.Duration() - platePosTween_.Elapsed());
            platePosTween_.Kill();
        }
        platePosTween_ = transform.DOLocalMove(newPos, time);
        //platePosTween_ = DOTween.To(() => root.position, pos => root.MovePosition(pos), newPos, time);
        platePosTween_.SetEase(easeMethod);
        platePosTween_.SetUpdate(true); // Ensure it updates even if the game is paused
        platePosTween_.OnComplete(() =>
        {
            platePosTween_ = null;
        });
        platePos = newPos;
    }
    private void shakeInator(Vector3 strength)
    {
        if (plateShakeTween_ != null)
        {
            plateShakeTween_.Kill();
        }
        plateShakeTween_ = DOTween.Shake(() => root.position, delegate (Vector3 x) {
            root.MovePosition(x);
        }, duration: 2f, strength: strength, vibrato: 0, randomness: 90,
        fadeOut: true).SetTarget(root).SetSpecialStartupMode(SpecialStartupMode.SetShake);
        plateShakeTween_.SetLoops(-1, LoopType.Restart);
        //plateShakeTween_.SetRelative(true);

        //plateShakeTween_.OnComplete(() => shakeInator(magnitude));
    }
    public void setPlateShake(Vector3 magnitude)
    {
        if (magnitude == plateShake) return;
        plateShake = magnitude;
        shakeInator(magnitude);
    }
    private IEnumerator doAngularRotationLoop()
    {
        while (rotateSpeed != Vector3.zero)
        {
            root.MoveRotation(root.rotation * Quaternion.Euler(rotateSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
    }
    public void setPlateAngularRotation(Vector3 magnitude)
    {
        rotateSpeed = magnitude;
        root.constraints = (magnitude.x == 0? RigidbodyConstraints.FreezeRotationX: 0)
            | (magnitude.y == 0 ? RigidbodyConstraints.FreezeRotationY: 0)
            | (magnitude.z == 0 ? RigidbodyConstraints.FreezeRotationZ: 0);
        if (isRotating)
            return;
        isRotating = true;
        StartCoroutine(doAngularRotationLoop());
    }
    void OnEnable()
    {
        platePos = transform.position;
        plateSize = render.transform.localScale;
        plateShake = Vector3.zero;
        plateOffset = Vector3.zero;
        rotateSpeed = Vector3.zero;
        isRotating = false;
        //Debug.Log($"{platePos} {plateSize}");

        //setPlateSize(plateSize + Vector3.up * 10, 2f);
        //setPlatePos(platePos + Vector3.up * 10 / 2, 2f);

        //root.transform.DOMove(platePos + Vector3.up * 10/2, 2f).OnComplete(() =>
        //{
        //    Debug.Log($"Local position: {render.transform.localPosition}");
        //});
    }
}
