using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour {
    [SerializeField] private Text roomName;
    [SerializeField] private Text roomSlots;
    [SerializeField] private Button joinButton;

    public void Set(LobbyManager lobbyManager, RoomData roomData) {
        roomName.text = roomData.Name;
        roomSlots.text = $"{roomData.Slots} / {roomData.MaxSlots}";
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(delegate { lobbyManager.SendJoinRequest(roomData.Name); });
    }
}
