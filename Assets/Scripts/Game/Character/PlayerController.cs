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
public struct PlayerData {
    public PlayerController playerController;
    public string ipAddress;
    public string displayName;
    public PlayerGamemode gamemode;
    public NetworkConnection serverConnection => playerController.connectionToServer;
    public NetworkConnectionToClient clientConnection => playerController.connectionToClient;
    public bool isLocalPlayer => playerController.isLocalPlayer;
    public PlayerData(PlayerController playerController, string ip, string displayName, PlayerGamemode gamemode) {
        this.playerController = playerController;
        this.ipAddress = ip;
        this.displayName = displayName;
        this.gamemode = gamemode;
    }
}

public class PlayerController : NetworkBehaviour
{
    [Header("Static Variables")]
    public static PlayerController Player { get; private set; }
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
    [SyncVar(hook = nameof(OnActiveCharacterChanged))]
    public string activeCharacter = "";
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
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }
    }
    public void OnActiveCharacterChanged(string _, string newCharName) {
        if (isLocalPlayer)
            return;
        foreach (Transform child in transform) {
            child.gameObject.SetActive(child.name == newCharName);
        }
    }
    [Server]
    public void ServerStartUp() {
        int plrIdx = ServerProperties.Instance.PlayerCount;
        //this.clientConnection = connectionToClient;
        PlayerData playerData = new PlayerData(this, connectionToClient.address,
            ServerProperties.Instance.SinglePlayer? "You": "Player" + plrIdx.ToString(),
            PlayerGamemode.Menu
        );
        ServerProperties.Instance.players.Add(playerData);
        //ServerProperties.Instance.PlayerCount++;
    }
    [TargetRpc]
    public void SpawnCharacter(NetworkConnectionToClient client, string characterName, Vector3 position) {
        Transform selCharacter = null;
        foreach (Transform child in transform) {
            bool found = child.name == characterName;
            if (found)
                selCharacter = child;
            child.gameObject.SetActive(found);
        }
        Assert.IsNotNull(selCharacter, "CharacterName not found!");
        Vector3 charOffset = GetCharacterOffset(selCharacter);
        selCharacter.position = position + charOffset;

        CameraController.Instance.SetCameraTarget(selCharacter.Find("PlayerCameraRoot"));
        characterControl = selCharacter.GetComponent<CharacterControl>();
        //Debug.Break();
    }
    [Client]
    private void OnDiedRpc() {
        Assert.IsTrue(authority, "OnDeath: I don't have authority!");
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
