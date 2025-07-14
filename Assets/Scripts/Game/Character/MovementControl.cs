using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MovementControl : MonoBehaviour {
    [Tooltip("Indicates if the character is grounded")]
    public bool IsGrounded = false;

    private CharacterControl charCntl;
    private BoxCollider boxCollider;
    private Rigidbody rb;
    private Transform followInstance;
    private Vector3 lastFollowPos;
    private Vector3 lastFollowSize;
    private Quaternion lastFollowRot;
    private int layerMask;
    private int noTouchingCount = 0;

    public float radiusMultiple = 2.25f;
    public float heightMultiple = 2f;

    void Start() {
        charCntl = GetComponent<CharacterControl>();
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        layerMask = ~LayerMask.GetMask("Character");
    }

    void FixedUpdate() {
        if (charCntl.isRagdoll) {
            followInstance = null;
            IsGrounded = false;
            return;
        }
        //else if (true) {
        //    IsGrounded = true;
        //    followInstance = null;
        //    return;
        //}
        // Calculate vertical speed manually since there's no Rigidbody
        float platformVerticalSpeed = 0f;
        if (followInstance != null) {
            platformVerticalSpeed = 1.5f * Mathf.Abs((followInstance.position.y - lastFollowPos.y) * Time.fixedDeltaTime * 50f);
        }

        Vector3 targetPosition = ApplyPlatformMotion();
        Bounds bounds = boxCollider.bounds;

        // Shrink bounds slightly inward to avoid detecting side walls
        Vector3 boxSize = new Vector3(
            boxCollider.size.x * transform.lossyScale.x * 0.9f,
            0.05f,
            boxCollider.size.z * transform.lossyScale.z * 0.9f
        );
        // Dynamic parameters based on speed
        float offsetUp = Mathf.Clamp(0.3f + platformVerticalSpeed/2, 0.1f, 6.0f);       // e.g. from 0.1 to 1.0
        float castDistance = Mathf.Clamp(0.45f + platformVerticalSpeed, 0.45f, 12.0f); // e.g. from 0.65 to 2.0

        Vector3 boxCenter = targetPosition + boxCollider.center - Vector3.up * (boxCollider.size.y * 0.5f - 0.025f)
            + Vector3.up * offsetUp;

        //Debug.Log($"Platform vertical speed: {platformVerticalSpeed}, up {offsetUp} x{castDistance}");

        RaycastHit hit;
        IsGrounded = Physics.BoxCast(
            boxCenter,
            boxSize * 0.5f,
            Vector3.down,
            out hit,
            rb.rotation,
            castDistance,
            layerMask,
            QueryTriggerInteraction.Ignore
        );

#if UNITY_EDITOR
        // Draw the cast box
        DrawBoxCast(boxCenter, boxSize, rb.rotation, Color.red, castDistance);
#endif
        // Update follow instance
        Transform newFollow = null;
        if (IsGrounded && hit.collider.CompareTag("Plate")) {
            newFollow = hit.collider.transform;
        }

        if (newFollow != followInstance) {
            if (newFollow == null && noTouchingCount < 5) {
                noTouchingCount++;
                return;
            }
            noTouchingCount = 0;
            followInstance = newFollow;

            if (followInstance != null) {
                lastFollowPos = followInstance.position;
                lastFollowSize = followInstance.localScale;
                lastFollowRot = followInstance.rotation;
            }
        }
    }

    void MovePosition(Vector3 position) {
        if ((position - rb.position).magnitude > 0.0001f)
            //rb.MovePosition(position);
            rb.position = position;
    }

    Vector3 ApplyPlatformMotion() {
        if (followInstance == null) return rb.position;
        Quaternion quaternion = rb.rotation;

        Vector3 posDelta = followInstance.position - lastFollowPos;
        Quaternion rotDelta = followInstance.rotation * Quaternion.Inverse(lastFollowRot);
        Vector3 offset = transform.position - followInstance.position;
        Vector3 rotatedOffset = rotDelta * offset;

        Vector3 newPosition = followInstance.position + rotatedOffset + posDelta;

        Vector3 scaleDelta = followInstance.localScale - lastFollowSize;
        if (scaleDelta != Vector3.zero) {
            Vector3 scaledOffset = new Vector3(
                offset.x * (scaleDelta.x / lastFollowSize.x),
                offset.y * (scaleDelta.y / lastFollowSize.y),
                offset.z * (scaleDelta.z / lastFollowSize.z)
            );
            if (scaledOffset.magnitude > 0.001f)
                newPosition += scaledOffset;
        }

        MovePosition(newPosition);

        // Only apply Y-axis rotation delta
        float yRotation = rotDelta.eulerAngles.y;
        Quaternion yRotDelta = Quaternion.Euler(0f, yRotation, 0f);
        rb.MoveRotation(yRotDelta * rb.rotation);

        // Store state for next frame
        lastFollowPos = followInstance.position;
        lastFollowSize = followInstance.localScale;
        lastFollowRot = followInstance.rotation;

        return newPosition;
    }
#if UNITY_EDITOR
    void DrawBoxCast(Vector3 center, Vector3 size, Quaternion rotation, Color color, float castDistance) {
        Vector3 halfExtents = size * 0.5f;
        Vector3[] corners = new Vector3[8];

        // Calculate the 8 corners of the box
        for (int i = 0; i < 8; i++) {
            float x = (i & 1) == 0 ? -1 : 1;
            float y = (i & 2) == 0 ? -1 : 1;
            float z = (i & 4) == 0 ? -1 : 1;
            Vector3 cornerOffset = new Vector3(x, y, z);
            corners[i] = center + rotation * Vector3.Scale(cornerOffset, halfExtents);
        }

        // Draw edges between corners (top square and bottom square)
        for (int i = 0; i < 4; i++) {
            Debug.DrawLine(corners[i], corners[(i + 1) % 4], color);         // bottom face
            Debug.DrawLine(corners[i + 4], corners[((i + 1) % 4) + 4], color); // top face
            Debug.DrawLine(corners[i], corners[i + 4], color);               // verticals
        }

        // Draw lines to the bottom of the cast
        for (int i = 0; i < 8; i++) {
            Debug.DrawLine(corners[i], corners[i] + Vector3.down * castDistance, color);
        }
    }
#endif
}
