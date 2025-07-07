using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Unity.Cinemachine;
using System.Collections;
using StarterAssets;


public class CharacterControl : NetworkBehaviour
{
    private Camera mainCam;
    public GameObject cineObject;
    [SyncVar]
    public bool isRagdoll = false;

    private Rigidbody[] rigidBodies;
    private Collider[] colliders;
    private Joint[] joints;
    private Rigidbody rootRigidbody;

    [Header("Character Components")]
    private Collider main_collider;
    private Animator charAnimator;
    private CharMovement thirdPersonController;
    [Client]
    void Awake()
    {
        if (!isLocalPlayer) return;
        transform.position = new Vector3(0, 10, 0);
    }
    [Client]
    void Start()
    {
        if (!isLocalPlayer) return;
        main_collider = GetComponent<Collider>();
        charAnimator = GetComponent<Animator>();
        rootRigidbody = GetComponent<Rigidbody>();
        thirdPersonController = GetComponent<CharMovement>();
        charAnimator.keepAnimatorStateOnDisable = false;

        rigidBodies = transform.GetChild(0).GetComponentsInChildren<Rigidbody>();
        colliders = transform.GetChild(0).GetComponentsInChildren<Collider>();
        joints = GetComponentsInChildren<Joint>();
        
        SetRagdoll(false, true);

        mainCam = Camera.main;
        var vcam = mainCam.GetComponent<CinemachineCamera>();
        vcam.Follow = cineObject.transform;
    }
    [Server]
    void FixedUpdate()
    {
        if ((transform.position.y < -10 && !isRagdoll))
        {
            Debug.Log("CharacterControl: Resetting character position due to falling below ground level.");
            SetRagdoll_FromServer(!isRagdoll);
            SetRagdoll(!isRagdoll);
        }
    }
    [ClientRpc]
    void SetRagdoll_FromServer(bool set)
    {
        SetRagdoll(set);
    }
    void SetRagdoll(bool ragdollActive, bool started = false)
    {
        if (!started && isRagdoll == ragdollActive) return; // No change needed
        thirdPersonController.enabled = !ragdollActive;
        main_collider.enabled = !ragdollActive;
        charAnimator.enabled = !ragdollActive;
        foreach (CharacterJoint joint in joints)
        {
            joint.enableCollision = ragdollActive;
        }
        foreach (Collider collider in colliders)
        {
            collider.enabled = ragdollActive;
        }
        foreach (Rigidbody rigidbody in rigidBodies)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.detectCollisions = ragdollActive;
            rigidbody.isKinematic = ragdollActive;
            rigidbody.useGravity = ragdollActive;
        }
        if (!ragdollActive)
        {
            // Reset Animator state
            charAnimator.SetFloat("Speed", 0f);
            charAnimator.SetFloat("MotionSpeed", 0f);
            charAnimator.SetBool("Jump", false);
            charAnimator.SetBool("Grounded", false);
            charAnimator.SetBool("FreeFall", false);
            charAnimator.Update(0f);
            charAnimator.Rebind();
        }
        isRagdoll = ragdollActive;
        //Debug.Log("CharacterControl: Ragdoll state set to " + ragdollActive);
    }
}
