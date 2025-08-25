using UnityEngine;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class OptionPlayerListData : OptionBaseData {
    public PlayerController playerData;
}
public class OptionPlayerList : OptionBase {
    public Transform HostButtons;

    public PlayerController player_;
    private bool isLocalPlayer = false;
    public override void SetEnabled(bool enabled) {
        if (enabled)
            enabled = NetworkClient.activeHost && !isLocalPlayer;
        HostButtons.gameObject.SetActive(enabled);
    }
    public override void Init(OptionBaseData data) {
        OptionPlayerListData playerListData = data as OptionPlayerListData;
        player_ = playerListData.playerData;
        isLocalPlayer = player_.isLocalPlayer;
        player_.PropertyChanged += UpdateProperties;
        SetEnabled(true);
        UpdateProperties();
    }
    private void UpdateProperties() {
        data_.description = $"{player_.displayName}{(player_.isLocalPlayer ? " (You)" : "")}";
        base.Init(data_);
    }
    public void KickButtonActivate() {
        NotificationScript.AddNotification(new NotificationData(
            $"Kick Player ({player_.ipAddress})",
            $"Are you sure you want to kick {player_.displayName}?",
            NotificationScript.YesNoButtons,
            (btn) => {
                if (btn == NotificationButton.Yes) {
                    ServerLobby.singleton.PunishPlayer(player_.ipAddress, false);
                }
            }
        ));
    }
    public void BanButtonActivate() {
        NotificationScript.AddNotification(new NotificationData(
            $"Ban Player ({player_.ipAddress})",
            $"Are you sure you want to ban {player_.displayName}?",
            NotificationScript.YesNoButtons,
            (btn) => {
                if (btn == NotificationButton.Yes) {
                    ServerLobby.singleton.PunishPlayer(player_.ipAddress, true);
                }
            }
        ));
    }
    private void OnDisable() {
        player_.PropertyChanged -= UpdateProperties;
    }
}
