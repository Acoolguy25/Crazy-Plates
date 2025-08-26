using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public class MovementControl : NetworkBehaviour {
    [Tooltip("Indicates if the character is grounded")]
    public bool IsGrounded = false;

    private class TouchingCollider {
        public Transform followInstance;
        public Vector3 lastFollowPos;
        public Vector3 lastFollowSize;
        public Quaternion lastFollowRot;
        public int noTouchingCount = 0;
    };
    private List<TouchingCollider> contacts = new();
    private CharacterControl charCntl;
    private BoxCollider boxCollider;
    private Rigidbody rb;
    private int layerMask;

    //public float radiusMultiple = 2.25f;
    //public float heightMultiple = 2f;
    public static ushort noTouchDelay = 0;
    public override void OnStartAuthority() {
        charCntl = GetComponent<CharacterControl>();
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        layerMask = ~LayerMask.GetMask("Character", "UI");
    }
    void UpdateContact(TouchingCollider contact) {
        var followInstance = contact.followInstance;
        contact.lastFollowPos = followInstance.position;
        contact.lastFollowSize = followInstance.localScale;
        contact.lastFollowRot = followInstance.rotation;
    }
    [ClientCallback]
    void FixedUpdate() {
        if (!isOwned || !charCntl) return;
        if (charCntl.isRagdoll) {
            contacts = new();
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
        foreach (var contact in contacts) {
            var followInstance = contact.followInstance;
            var lastFollowPos = contact.lastFollowPos;
            if (followInstance != null) {
                platformVerticalSpeed = Mathf.Max(platformVerticalSpeed,
                    1.5f * Mathf.Abs((followInstance.position.y - lastFollowPos.y) * Time.fixedDeltaTime * 50f));
            }
        }

        // Calculate box position & size
        Vector3 targetPosition = ApplyPlatformMotion();
        Bounds bounds = boxCollider.bounds;

        Vector3 boxSize = new Vector3(
            boxCollider.size.x * transform.lossyScale.x * 0.9f,
            10f,
            boxCollider.size.z * transform.lossyScale.z * 0.9f
        );

        float offsetUp = 1f;

        Vector3 boxCenter = (bounds.center - transform.position) + transform.position
            - Vector3.up * (bounds.size.y * 0.5f - boxSize.y * 0.5f)
            + Vector3.down * offsetUp;

        Quaternion boxRotation = transform.rotation;

        // Check overlaps
        Collider[] overlaps = Physics.OverlapBox(
            boxCenter,
            boxSize * 0.5f,
            boxRotation,
            layerMask,
            QueryTriggerInteraction.Ignore
        );

        IsGrounded = overlaps.Length > 0;

#if UNITY_EDITOR
        if (!IsGrounded) {
            DrawBox(boxCenter, boxSize * 0.5f, boxRotation, Color.red, 0f);
            //Debug.Break();
        }
#endif

        // Update follow instance
        List<Transform> foundItems = new();
        //Transform newFollow = null;

        foreach (Collider collider in overlaps) {
            if (IsGrounded && collider.CompareTag("Plate")) {
                //newFollow = hit.collider.transform;
                Transform theirTransform = collider.transform;
                foundItems.Add(theirTransform);
            }
        }
        // Check conflicting
        for (int i = contacts.Count - 1; i >= 0; i--) {
            var contact = contacts[i];
            if (foundItems.Contains(contact.followInstance)) { // Still touching
                UpdateContact(contact);
                contact.noTouchingCount = 0;
                foundItems.Remove(contact.followInstance);
            }
            else { // No longer touching
                contact.noTouchingCount++;
                if (contact.noTouchingCount > noTouchDelay) {
                    contacts.Remove(contact);
                }
            }
        }
        // New touching items
        foreach (var hit in foundItems) {
            TouchingCollider newContact = new(){
                followInstance = hit
            };
            UpdateContact(newContact);
            contacts.Add(newContact);
        }

        MovePosition(targetPosition);
    }

    void MovePosition(Vector3 position) {
        //if ((position - transform.position).magnitude > 0f)
        //rb.MovePosition(position);
        transform.position = position;
        //transform.position = position;
    }

    Vector3 ApplyPlatformMotion() {
        if (contacts.Count == 0) {
            // No platforms, just return current position
            return transform.position;
        }
        Vector3 summation = Vector3.zero;
        foreach (var contact in contacts) {
            var followInstance = contact.followInstance;
            var lastFollowPos = contact.lastFollowPos;
            var lastFollowRot = contact.lastFollowRot;
            var lastFollowSize = contact.lastFollowSize;
            if (followInstance == null) continue;
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
                newPosition += scaledOffset;
            }


            // Only apply Y-axis rotation delta
            float yRotation = rotDelta.eulerAngles.y;
            Quaternion yRotDelta = Quaternion.Euler(0f, yRotation, 0f);
            rb.MoveRotation(yRotDelta * rb.rotation);

            // Store state for next frame
            lastFollowPos = followInstance.position;
            lastFollowSize = followInstance.localScale;
            lastFollowRot = followInstance.rotation;
            summation += newPosition - transform.position;
        }
        Vector3 total = transform.position + summation;
        return total;
    }
#if UNITY_EDITOR
    void DrawBoxCast(Vector3 center, Vector3 size, Quaternion rotation, Color color, float castDistance, float duration, Vector3 direction) {
        Vector3 halfExtents = size * 0.5f;

        Vector3 start = center;
        Vector3 end = center + direction.normalized * castDistance;

        DrawBox(start, halfExtents, rotation, color, duration);
        DrawBox(end, halfExtents, rotation, color, duration);

        for (int i = 0; i < 8; i++) {
            Vector3 s = GetBoxCorner(start, halfExtents, rotation, i);
            Vector3 e = GetBoxCorner(end, halfExtents, rotation, i);
            Debug.DrawLine(s, e, color, duration);
        }
    }
    void DrawBoxCast(Vector3 center, Vector3 size, Quaternion rotation, Color color, float castDistance, float duration) {
        Vector3 halfExtents = size * 0.5f;

        // Use rotation to find cast direction in world space
        Vector3 dir = rotation * Vector3.down;

        Vector3 start = center;
        Vector3 end = center + dir * castDistance;

        DrawBox(start, halfExtents, rotation, color, duration);
        DrawBox(end, halfExtents, rotation, color, duration);

        // Draw lines between corners
        for (int i = 0; i < 8; i++) {
            Vector3 s = GetBoxCorner(start, halfExtents, rotation, i);
            Vector3 e = GetBoxCorner(end, halfExtents, rotation, i);
            Debug.DrawLine(s, e, color, duration);
        }
    }

    void DrawBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color, float duration) {
        // Bottom square
        DrawBoxEdge(center, halfExtents, rotation, 0, 1, color, duration);
        DrawBoxEdge(center, halfExtents, rotation, 1, 3, color, duration);
        DrawBoxEdge(center, halfExtents, rotation, 3, 2, color, duration);
        DrawBoxEdge(center, halfExtents, rotation, 2, 0, color, duration);

        // Top square
        DrawBoxEdge(center, halfExtents, rotation, 4, 5, color, duration);
        DrawBoxEdge(center, halfExtents, rotation, 5, 7, color, duration);
        DrawBoxEdge(center, halfExtents, rotation, 7, 6, color, duration);
        DrawBoxEdge(center, halfExtents, rotation, 6, 4, color, duration);

        // Vertical edges
        for (int i = 0; i < 4; i++)
            DrawBoxEdge(center, halfExtents, rotation, i, i + 4, color, duration);
    }

    void DrawBoxEdge(Vector3 center, Vector3 halfExtents, Quaternion rotation, int i1, int i2, Color color, float duration) {
        Debug.DrawLine(GetBoxCorner(center, halfExtents, rotation, i1),
                       GetBoxCorner(center, halfExtents, rotation, i2),
                       color, duration);
    }

    Vector3 GetBoxCorner(Vector3 center, Vector3 halfExtents, Quaternion rotation, int index) {
        float x = (index & 1) == 0 ? -1 : 1;
        float y = (index & 2) == 0 ? -1 : 1;
        float z = (index & 4) == 0 ? -1 : 1;
        return center + rotation * Vector3.Scale(new Vector3(x, y, z), halfExtents);
    }
#endif
}
