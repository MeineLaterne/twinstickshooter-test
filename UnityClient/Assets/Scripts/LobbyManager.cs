using DarkRift;
using DarkRift.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour {
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomListItemPrefab;

    public void SendJoinRequest(string roomName) {
        using (var msg = Message.Create((ushort)MessageTag.JoinRoomRequest, new JoinRoomRequestData(roomName))) {
            ConnectionManager.Instance.Client.SendMessage(msg, SendMode.Reliable);
        }
    }

    public void RefreshRoomList(LobbyData lobbyData) {
        RoomListItem[] roomListItems = roomListContainer.GetComponentsInChildren<RoomListItem>();

        if (roomListItems.Length > lobbyData.Rooms.Length) {
            for (int i = lobbyData.Rooms.Length; i < roomListItems.Length; i++) {
                Destroy(roomListItems[i].gameObject);
            }
        }

        for (int i = 0; i < lobbyData.Rooms.Length; i++) {
            var roomData = lobbyData.Rooms[i];
            if (i < roomListItems.Length) {
                roomListItems[i].Set(this, roomData);
            } else {
                var rli = Instantiate(roomListItemPrefab, roomListContainer);
                rli.GetComponent<RoomListItem>().Set(this, roomData);
            }
        }

    }

    private void Start() {
        ConnectionManager.Instance.Client.MessageReceived += OnMessage;
        RefreshRoomList(ConnectionManager.Instance.LobbyData);
    }

    private void OnDestroy() {
        ConnectionManager.Instance.Client.MessageReceived -= OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e) {
        using (var msg = e.GetMessage()) {
            switch ((MessageTag)msg.Tag) {
                case MessageTag.JoinRoomAccepted:
                    OnJoinRoomAccepted();
                    break;
                
                case MessageTag.JoinRoomDenied:
                    OnJoinRoomDenied(msg.Deserialize<LobbyData>());
                    break;
            }
        }
    }

    private void OnJoinRoomAccepted() => SceneManager.LoadScene("Game");

    private void OnJoinRoomDenied(LobbyData lobbyData) => RefreshRoomList(lobbyData);
}
