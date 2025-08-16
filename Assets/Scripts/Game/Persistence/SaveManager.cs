using System.IO;
using UnityEngine;

public static class SaveManager {
    private static string savePath = Application.persistentDataPath + "/save.json";
    private static string password = "";
    [SerializeField]
    public class SavedGameData {
        public double singleplayerTime;
        public SavedGameData(float time = 0f) {
            singleplayerTime = time;
        }
    }
    public static SavedGameData SaveInstance;
    public static void SaveGame() {
        string json = JsonUtility.ToJson(SaveInstance, true);
        //string encrypted = Encryption.EncryptDecrypt(json, password);

#if UNITY_WEBGL
        // On WebGL, use PlayerPrefs
        PlayerPrefs.SetString("SavedGameData", json);
        PlayerPrefs.Save();
        Debug.Log("Game Saved to PlayerPrefs (WebGL)");
#else
        // On PC, write to file
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved to " + savePath);
#endif
    }

    public static void LoadGame() {
        string json = string.Empty;
#if UNITY_WEBGL
        if (PlayerPrefs.HasKey("SavedGameData")) {
            json = PlayerPrefs.GetString("SavedGameData");
        }
        else {
            Debug.LogWarning("No save found in PlayerPrefs (WebGL)");
        }
#else
        if (File.Exists(savePath))
        {
            json = File.ReadAllText(savePath);
        }
        else
        {
            Debug.LogWarning("No save file found at " + savePath);
        }
#endif
        if (json.Length > 0) {
            //string decrypted = Encryption.EncryptDecrypt(json, password);
            SaveInstance = JsonUtility.FromJson<SavedGameData>(json);
            Debug.Log("GameData Loaded");
            //return JsonUtility.FromJson<SavedGameData>(json);
        }
        else {
            SaveInstance = new SavedGameData();
        }
    }

    public static void DeleteSave() {
#if UNITY_WEBGL
        PlayerPrefs.DeleteKey("SaveData");
#else
        if (File.Exists(savePath))
            File.Delete(savePath);
#endif
        Debug.Log("Save data deleted");
    }
}
