using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Cinemachine;
using StarterAssets;

public enum PlayerGamemode: byte {
    Alive,
    Spectator,
    Menu
}
public struct PlayerData {
    public PlayerController playerController;
    public string displayName;
    public PlayerGamemode gamemode;
    public PlayerData(PlayerController playerController, string displayName, PlayerGamemode gamemode) {
        this.playerController = playerController;
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
    public Transform cineObject; // reference
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Player = null;
    }
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
        if (!authority)
            return;
        Assert.IsTrue(cineObject, "Cinemachine Object is missing!");

        if (isLocalPlayer) {
            Player = this;
        }
    }
    [Server]
    public void ServerStartUp() {
        uint plrIdx = ServerProperties.Instance.PlayerCount;
        //this.clientConnection = connectionToClient;
        PlayerData playerData = new PlayerData(this, 
            ServerProperties.Instance.SinglePlayer? "You": "Player" + plrIdx.ToString(),
            PlayerGamemode.Alive
        );
        ServerProperties.Instance.players.Add(playerData);
        ServerProperties.Instance.PlayerCount++;
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

        CameraController.Instance.SetCameraTarget(cineObject);
        characterControl = selCharacter.GetComponent<CharacterControl>();
    }
    [Client]
    private void OnDiedRpc() {
        Assert.IsTrue(authority, "OnDeath: I don't have authority!");
        Assert.IsTrue(characterControl.isDead, "Character is not dead and is supposed to be!");
        StartCoroutine(DeathUI.Instance.PlayerDied());
    }
}
