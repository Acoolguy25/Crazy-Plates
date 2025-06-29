using System.Linq;
using UnityEngine;

public class MovementControl : MonoBehaviour {
    [Tooltip("Indicates if the character is grounded")]
    public bool IsGrounded = false; // Indicates if the character is grounded


    private CharacterControl charCntl; // Reference to the CharacterController component
    private CharacterController unityCharCntl; // Reference to the Unity CharacterController component
    private Transform followInstance;
    private Vector3 lastFollowPos;
    private Vector3 lastFollowSize;
    private Quaternion lastFollowRot;
    private int layerMask; // Layer mask to ignore character layer

    public static Vector3 GetWorldScale(Transform transform) {
        Vector3 scale = transform.localScale;
        Transform parent = transform.parent;

        while (parent != null) {
            scale = Vector3.Scale(scale, parent.localScale);
            parent = parent.parent;
        }

        return scale;
    }
    void Start() {
        charCntl = GetComponent<CharacterControl>();
        unityCharCntl = GetComponent<CharacterController>();
        layerMask = ~LayerMask.GetMask("Character");
    }
    private void Update() {
        LateUpdate();
    }
    // Update is called once per frame
    void FixedUpdate() {
        LateUpdate();
        if (charCntl.isRagdoll) // || !unityCharCntl.isGrounded
        {
            //Debug.Log($"No longer touching ground!!");
            followInstance = null;
            IsGrounded = false;
            return;
        }
        Collider[] hitColliders = Physics.OverlapSphere(
                transform.position + unityCharCntl.center - Vector3.up * unityCharCntl.height / 2,
                unityCharCntl.radius, layerMask);
        IsGrounded = hitColliders.LongLength > 0;
        Transform newFollow = null;
        if (!followInstance || !hitColliders.Contains(followInstance.GetComponent<Collider>())) {
            foreach (Collider hitCollider in hitColliders) {
                if (hitCollider.gameObject.tag == "Plate") {
                    newFollow = hitCollider.transform;
                    break;
                }
            }
        }
        else {
            newFollow = followInstance;
        }
        if (newFollow != followInstance) {
            followInstance = newFollow;
            if (newFollow != null) {
                lastFollowPos = newFollow.position;  // Update for next frame
                lastFollowSize = (newFollow.localScale); // Store the size of the new follow object
                lastFollowRot = newFollow.rotation;
            }
        }
        else {
            followInstance = newFollow;
        }
    }
    void LateUpdate() {
        if (IsGrounded && followInstance != null) {
            Vector3 pos_delta = followInstance.position - lastFollowPos;
            Vector3 size_delta = (followInstance.localScale) - lastFollowSize;

            transform.position += pos_delta;
            //Debug.Log($"MovementControl: Adjusting position by {pos_delta} due to size change of the platform.");

            Vector3 offsetFromPlatform = transform.position - followInstance.position;

            Quaternion rotationDelta = followInstance.rotation * Quaternion.Inverse(lastFollowRot);

            // Rotate the offset around the platform’s new rotation
            Vector3 rotatedOffset = rotationDelta * offsetFromPlatform;

            // Recalculate new world position
            transform.position = followInstance.position + rotatedOffset;
            Vector3 rotationEuler = rotationDelta.eulerAngles;
            rotationDelta = Quaternion.Euler(Vector3.up * rotationEuler.y);

            // Apply world rotation change to the player
            transform.rotation = rotationDelta * transform.rotation;

            if (size_delta != Vector3.zero) {
                Vector3 scaledOffset = new Vector3(
                    offsetFromPlatform.x * (size_delta.x / lastFollowSize.x),
                    offsetFromPlatform.y * (size_delta.y / lastFollowSize.y),
                    offsetFromPlatform.z * (size_delta.z / lastFollowSize.z)
                );
                transform.position += scaledOffset;
                //Debug.Log($"MovementControl: Adjusting position by {scaledOffset} due to size change of the platform.");
            }



            lastFollowPos = followInstance.position;
            lastFollowSize = (followInstance.localScale); // Update the size for next frame
            lastFollowRot = followInstance.rotation;
        }
    }
}
