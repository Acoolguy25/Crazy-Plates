using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Unity.Cinemachine;
using System.Collections;
using StarterAssets;
using UnityEngine.Assertions;


public class CharacterControl : NetworkBehaviour
{
    //private Camera mainCam;
    //public GameObject cineObject;
    [Header("Public Variables")]
    [SyncVar] public bool isRagdoll = true;
    [SyncVar] public bool isDead = false;
    [SyncVar] public uint health = 0;
    [SyncVar] public uint maxHealth = 0;

    [Header("Ragdoll variables")]
    private Rigidbody[] rigidBodies;
    private Collider[] colliders;
    private Joint[] joints;
    private Rigidbody rootRigidbody;


    [Header("Character Components")]
    private Collider main_collider;
    private Animator charAnimator;
    private CharMovement thirdPersonController;
    [ClientCallback]
    void Start()
    {
        //Debug.Log($"START CALLED: AUTHORITY: {authority} | {isServer} | {isClient}");
        //if (!authority)
            //return;
        //transform.position = new Vector3(0, 10, 0);
        main_collider = GetComponent<Collider>();
        charAnimator = GetComponent<Animator>();
        rootRigidbody = GetComponent<Rigidbody>();
        //networkIdentity = GetComponent<NetworkIdentity>();
        thirdPersonController = GetComponent<CharMovement>();
        charAnimator.keepAnimatorStateOnDisable = false;

        rigidBodies = transform.GetChild(0).GetComponentsInChildren<Rigidbody>();
        colliders = transform.GetChild(0).GetComponentsInChildren<Collider>();
        joints = GetComponentsInChildren<Joint>();
        
        SetRagdoll(false);

        //mainCam = Camera.main;
        //var vcam = mainCam.GetComponent<CinemachineCamera>();
        //vcam.Follow = cineObject.transform;
    }
    [ServerCallback]
    private void FixedUpdate()
    {
        if ((transform.position.y < -10 && !isRagdoll))
        {
            //Debug.Log($"DIED ON: {isServer} | {isClient} | {authority}");
            //Debug.Log("CharacterControl: Resetting character position due to falling below ground level.");
            KillCharacter();
        }
    }
    [Server]
    public void KillCharacter() {
        if (isDead)
            return;
        health = 0;
        isDead = true;
        SetRagdoll(true);
        gameObject.SendMessageUpwards("OnDied", SendMessageOptions.DontRequireReceiver);
        //transform.parent.SendMessage("OnDied", SendMessageOptions.DontRequireReceiver);
        if (isServer && connectionToClient != null) { // player died
            ServerProperties.Instance.AlivePlayers--;
            ServerEvents.Instance.PlayerDied?.Invoke(GetComponent<PlayerController>());
            KillCharacterRpc(connectionToClient);
        }
    }
    [TargetRpc]
    public void KillCharacterRpc(NetworkConnectionToClient _) {
        SetRagdoll(true);
        gameObject.SendMessageUpwards("OnDiedRpc", SendMessageOptions.DontRequireReceiver);
        //transform.parent.SendMessage("OnDiedRpc", SendMessageOptions.DontRequireReceiver);
    }
    [Server]
    public void TakeDamage(uint damage) {
        if (health <= damage)
            health = 0;
        else
            health -= damage;
        if (health == 0 && maxHealth != 0) {
            KillCharacter();
        }
    }
    [ClientRpc]
    public void SetRagdoll_FromServer(bool set)
    {
        SetRagdoll(set);
    }
    void SetRagdoll(bool ragdollActive)
    {
        ragdollActive = ragdollActive || isDead;
        if (isRagdoll == ragdollActive) return; // No change needed
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
            rigidbody.isKinematic = !ragdollActive;
            if (!rigidbody.isKinematic) {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
            rigidbody.detectCollisions = ragdollActive;
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
