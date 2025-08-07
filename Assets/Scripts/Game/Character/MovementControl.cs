using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MovementControl : MonoBehaviour {
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

    public float radiusMultiple = 2.25f;
    public float heightMultiple = 2f;
    public ushort noTouchDelay = 5;

    void Start() {
        charCntl = GetComponent<CharacterControl>();
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        layerMask = ~LayerMask.GetMask("Character");
    }
    void UpdateContact(TouchingCollider contact) {
        var followInstance = contact.followInstance;
        contact.lastFollowPos = followInstance.position;
        contact.lastFollowSize = followInstance.localScale;
        contact.lastFollowRot = followInstance.rotation;
    }
    void FixedUpdate() {
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

        RaycastHit[] hits = Physics.BoxCastAll(
            boxCenter,
            boxSize * 0.5f,
            Vector3.down,
            rb.rotation,
            castDistance,
            layerMask,
            QueryTriggerInteraction.Ignore
        );
        IsGrounded = hits.Length > 0;

#if UNITY_EDITOR
        // Draw the cast box
        DrawBoxCast(boxCenter, boxSize, rb.rotation, Color.red, castDistance);
#endif
        // Update follow instance
        List<Transform> foundItems = new();
        //Transform newFollow = null;

        foreach (RaycastHit hit in hits) {
            if (IsGrounded && hit.collider.CompareTag("Plate")) {
                //newFollow = hit.collider.transform;
                Transform theirTransform = hit.collider.transform;
                foundItems.Add(theirTransform);
            }
        }
        // Check conflicting
        for (int i = contacts.Count - 1; i >= 0; i--) {
            var contact = contacts[i];
            if (foundItems.Contains(contact.followInstance)){ // Still touching
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
            TouchingCollider newContact = new();
            newContact.followInstance = hit;
            UpdateContact(newContact);
            contacts.Add(newContact);
        }
    }

    void MovePosition(Vector3 position) {
        if ((position - rb.position).magnitude > 0.0001f)
            //rb.MovePosition(position);
            rb.position = position;
    }

    Vector3 ApplyPlatformMotion() {
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
            summation += newPosition - rb.position;
        }
        return rb.position + summation;
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
