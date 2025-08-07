using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Cinemachine;
using StarterAssets;

public class PlayerController : NetworkBehaviour
{
    [Header("Static Variables")]
    public static PlayerController Player { get; private set; }
    [Header("Public Variables")]
    public CharacterControl characterControl { get; private set; }
    public Transform cineObject; // reference
    [ClientCallback]
    private void Start()
    {
        if (!authority) return;
        Assert.IsTrue(cineObject, "Cinemachine Object is missing!");
        characterControl = GetComponent<CharacterControl>();

        if (isLocalPlayer) {
            CameraController.Instance.SetCameraTarget(cineObject);
            Player = this;
        }
    }
    [Client]
    private void OnDiedRpc() {
        Assert.IsTrue(authority, "OnDeath: I don't have authority!");
        Assert.IsTrue(characterControl.isDead, "Character is not dead and is supposed to be!");
        StartCoroutine(GameCanvasMain.Instance.PlayerDied());
    }
}
