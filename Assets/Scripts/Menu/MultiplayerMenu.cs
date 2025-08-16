using UnityEngine;

public class MultiplayerMenu : MonoBehaviour
{
    public void MultiplayerExitActivated() {
        LobbyUI.Instance.ChangeToPanel(null);
    }
}
