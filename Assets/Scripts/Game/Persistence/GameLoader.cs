using UnityEngine;

public class GameLoader : MonoBehaviour
{
    void Start()
    {
        SaveManager.LoadGame();
        SaveManager.SavedGameData savedData = SaveManager.SaveInstance;
        SingleplayerMenu.Instance.UpdateSinglePlayerTime(savedData.singleplayerTime);
    }
}
