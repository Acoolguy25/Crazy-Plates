using DG.Tweening.Core.Easing;
using Mirror;
using System;
using UnityEngine;

public class LobbyEvents : GameEvents {
    public override void OnNewGameMessage(GameMessage _, GameMessage newString) {
        return; // No action in lobby
    }
    public override void OnDescMessageChanged(string _, string newString) {
        return; // No action in lobby
    }
}
