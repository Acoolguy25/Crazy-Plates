using UnityEngine;

public class MovementControl : MonoBehaviour
{
    [Tooltip("Indicates if the character is grounded")]
    public bool IsGrounded = false; // Indicates if the character is grounded
    
    
    private CharacterControl charCntl; // Reference to the CharacterController component
    private CharacterController unityCharCntl; // Reference to the Unity CharacterController component
    private Transform followInstance;
    private Vector3 lastFollowPos;
    private Vector3 lastFollowSize;
    void Start()
    {
        charCntl = GetComponent<CharacterControl>();
        unityCharCntl = GetComponent<CharacterController>();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (charCntl.isRagdoll)
        {
            IsGrounded = false;
            return;
        }
        LateUpdate();
        Collider[] hitColliders = Physics.OverlapSphere(
                transform.position + unityCharCntl.center + Vector3.down * unityCharCntl.height/2,
                0.02f);
        IsGrounded = false;
        Transform newFollow = null;
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.layer != gameObject.layer)
            {
                IsGrounded = true;
                if (hitCollider.gameObject.tag == "Plate")
                {
                    newFollow = hitCollider.transform;
                    break;
                }
            }
        }
        if (newFollow != followInstance)
        {
            followInstance = newFollow;
            if (newFollow != null)
            {
                lastFollowPos = newFollow.position;  // Update for next frame
                lastFollowSize = newFollow.localScale; // Store the size of the new follow object
            }
        }
        else
        {   
            followInstance = newFollow;
        }
    }

    void LateUpdate()
    {
        if (followInstance != null)
        {
            Vector3 pos_delta = followInstance.position - lastFollowPos;
            Vector3 size_delta = followInstance.localScale - lastFollowSize;

            transform.position += pos_delta;

            Vector3 offsetFromPlatform = transform.position - followInstance.position;


            if (size_delta != Vector3.zero)
            {
                Vector3 scaledOffset = new Vector3(
                    offsetFromPlatform.x * (size_delta.x / lastFollowSize.x),
                    offsetFromPlatform.y * (size_delta.y / lastFollowSize.y),
                    offsetFromPlatform.z * (size_delta.z / lastFollowSize.z)
                );
                transform.position += scaledOffset;
            }

            lastFollowPos = followInstance.position;
            lastFollowSize = followInstance.localScale; // Update the size for next frame
        }
    }
}
