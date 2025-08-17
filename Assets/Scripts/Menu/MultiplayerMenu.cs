using UnityEngine;

public class MultiplayerMenu : MonoBehaviour
{
    public Transform StartPanel, CreatePanel, JoinPanel;
    public void MultiplayerExitActivated() {
        LobbyUI.Instance.ChangeToPanel(null);
        MultiplayerChangePanel(StartPanel);
    }
    public void MultiplayerChangePanel(Transform panel) {
        foreach (Transform child in StartPanel.parent) {
            child.gameObject.SetActive(panel == child);
        }
    }
    private void Start() {
        MultiplayerChangePanel(StartPanel);
    }
}
