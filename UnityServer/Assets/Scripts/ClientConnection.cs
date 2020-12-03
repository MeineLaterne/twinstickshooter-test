
using DarkRift.Server;

public class ClientConnection {
    public readonly string userName;
    public readonly IClient client;

    public Room Room { get; set; }
    public ServerPlayer ServerPlayer { get; set; }

    public ClientConnection(string userName, IClient client) {
        this.userName = userName;
        this.client = client;
        this.client.MessageReceived += OnMessage;
    }

    public void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
        if (Room != null) {
            Room.RemovePlayer(this);
        }
        ServerManager.Instance.Players.Remove(client.ID);
        ServerManager.Instance.PlayersByName.Remove(userName);
        client.MessageReceived -= OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e) {
        var client = (IClient)sender;
        using (var m = e.GetMessage()) {
            switch ((MessageTag)m.Tag) {

                case MessageTag.GameInput:
                    ServerPlayer.ReceiveInput(m.Deserialize<PlayerInputData>());
                    break;
                
                case MessageTag.BulletRequest:
                    Room.OnBulletRequest(m.Deserialize<BulletRequestData>());
                    break;
                
                case MessageTag.JoinRoomRequest:
                    RoomManager.Instance.TryJoinRoom(client, m.Deserialize<JoinRoomRequestData>());
                    break;

                case MessageTag.StartGameRequest:
                    Room.SpawnPlayer(this);
                    break;


            }
        }
    }
}
