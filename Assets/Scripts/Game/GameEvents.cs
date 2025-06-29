using DG.Tweening.Core.Easing;
using Mirror;
using System;
using UnityEngine;
public struct GameMessage
{
    public string Message;
    public double Duration;

    public GameMessage(string message, double duration)
    {
        Message = message;
        Duration = duration;
    }
}

public class GameEvents : NetworkBehaviour
{
    public static GameEvents Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persist between scenes
    }
    [Client]
    public void OnClientBegin()
    {
        if (!isLocalPlayer) return;
        OnNewGameMessage(GameMessage, GameMessage);
        OnDescMessageChanged(DescMessage, DescMessage);
    }
    public GameCanvasMain gameCanvasMain;
    [SyncVar(hook = nameof(OnNewGameMessage))]
    public GameMessage GameMessage = new GameMessage( "", 0d );
    [SyncVar(hook = nameof(OnDescMessageChanged))]
    public string DescMessage = "";
    void OnNewGameMessage(GameMessage _, GameMessage newString)
    {
        StartCoroutine(gameCanvasMain.UpdateTopBar(newString));
    }
    void OnDescMessageChanged(string _, string newString)
    {
        gameCanvasMain.UpdateDescBar(newString);
    }
}
