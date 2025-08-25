using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Cinemachine;
using StarterAssets;
using Unity.VisualScripting;

public enum PlayerGamemode: byte {
    Alive,
    Spectator,
    Menu
}

public class PlayerController : NetworkBehaviour
{
    [Header("Static Variables")]
    public static PlayerController Player { get; private set; }
    public GameObject[] CharacterPrefabs;
    [Header("Shared Public Variables")]
    public CharacterControl characterControl { get; private set; }
    //public Transform cineObject; // reference
    //[Header("Private Server Variables")]
    //public NetworkConnectionToClient clientConnection;
    //private Vector3 MoveToSpawnpoint() {
    //    int spawnPoints = ServerProperties.Instance.SpawnPoints.Count;
    //    Assert.IsTrue(spawnPoints > 0, "No Spawnpoints Left!");
    //    int spawnIdx = Random.Range(0, spawnPoints);
    //    Vector3 spawnLoc = ServerProperties.Instance.SpawnPoints[spawnIdx];
    //    ServerProperties.Instance.SpawnPoints.RemoveAt(spawnIdx);
    //    return spawnLoc;
    //}
    [Header("Sync Public Variables")]
    [SyncVar] public string ipAddress;
    [SyncVar] public string displayName;
    [SyncVar] public PlayerGamemode gamemode;

    //[SyncVar(hook = nameof(OnActiveCharacterChanged))]
    public Transform activeCharacter = null;
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Player = null;
    }
#endif
    public static Vector3 GetCharacterOffset(Transform character) {
        Collider collider = character.GetComponent<Collider>();
        Vector3 charOffset = Vector3.zero;
        if (collider != null) {
            charOffset += Vector3.up * (character.position.y - collider.bounds.min.y);
        }
        return charOffset;
    }
    [ClientCallback]
    private void Start()
    {
        //Assert.IsTrue(cineObject, "Cinemachine Object is missing!");

        if (isLocalPlayer) {
            Player = this;
        }
        if (!NetworkClient.activeHost) {
            DontDestroyOnLoad(this);
            transform.name = $"Unnamed Player";
        }
        //foreach (Transform child in transform) {
        //    child.gameObject.SetActive(false);
        //}
    }
    //public void OnActiveCharacterChanged(string _, string newCharName) {
    //    if (isLocalPlayer)
    //        return;
    //    foreach (Transform child in transform) {
    //        child.gameObject.SetActive(child.name == newCharName);
    //    }
    //}
    [Server]
    public void SpawnCharacter(string characterName, Vector3 position) {
        if (activeCharacter) {
            NetworkServer.Destroy(activeCharacter.gameObject);
            activeCharacter = null;
        }
        for (int i = 0; i < CharacterPrefabs.Length; i++) {
            if (CharacterPrefabs[i].name == characterName) {
                position += GetCharacterOffset(CharacterPrefabs[i].transform);
                activeCharacter = Instantiate(CharacterPrefabs[i], position, Quaternion.identity, transform).transform;
                break;
            }
        }
        Debug.Assert(activeCharacter, $"CharacterName \"{characterName}\" not found!");
        activeCharacter.name = characterName;
        NetworkServer.Spawn(activeCharacter.gameObject, connectionToClient);
        //NetworkServer.ReplacePlayerForConnection(client, activeCharacter.gameObject);
        //SpawnCharacterRpc(connectionToClient, Reflection.Serialize(activeCharacter), position);
        activeCharacter.gameObject.GetComponent<PlayerSync>().correspondingNetId = Reflection.Serialize(transform);
    }
    private void CharacterDestroyed() {
        if (activeCharacter == null) {
            return;
        }
        Debug.Log("Character Destroyed!");
        activeCharacter = null;
        if (isLocalPlayer)
            CameraController.Instance.SetActiveCamera("Orbit");
    }
    [ClientCallback]
    private void OnTransformChildrenChanged() {
        CharacterDestroyed();
    }
    //[TargetRpc]
    //public void SpawnCharacterRpc(NetworkConnectionToClient client, uint activeCharacter_, Vector3 position) {
    public void AddedTransform(Transform activeCharacter_) {
        activeCharacter = activeCharacter_;
        //activeCharacter = transform.Find(activeCharacter_);
        Assert.IsNotNull(activeCharacter, $"CharacterName {activeCharacter_} not found!");
        //Vector3 charOffset = GetCharacterOffset(activeCharacter);
        //activeCharacter.position = position + charOffset;

        if (isLocalPlayer) {
            CameraController.Instance.SetCameraTarget(activeCharacter.Find("PlayerCameraRoot"));
            CameraController.Instance.SetActiveCamera("ThirdPerson");
        }
        characterControl = activeCharacter.GetComponent<CharacterControl>();
        //Debug.Break();
    }
    [Client]
    private void OnDiedClient() {
        if (!isLocalPlayer)
            return;
        Assert.IsTrue(isOwned, "OnDeath: I don't have authority!");
        Assert.IsTrue(characterControl.isDead, "Character is not dead and is supposed to be!");

        StartCoroutine(DeathUI.Instance.PlayerDied());
        if (ServerProperties.Instance.SinglePlayer) {
            if (GameEvents.Instance.SurvivalTime > SaveManager.SaveInstance.singleplayerTime) {
                SaveManager.SaveInstance.singleplayerTime = GameEvents.Instance.SurvivalTime;
                SaveManager.SaveGame();
                SingleplayerMenu.Instance.UpdateSinglePlayerTime(SaveManager.SaveInstance.singleplayerTime);
            }
        }
    }
}
