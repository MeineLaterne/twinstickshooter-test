using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(XmlUnityServer))]
public class ServerManager : MonoBehaviour {
    
    public readonly Dictionary<ushort, ClientConnection> Players = new Dictionary<ushort, ClientConnection>();
    public readonly Dictionary<string, ClientConnection> PlayersByName = new Dictionary<string, ClientConnection>();
    
    public static ServerManager Instance { get; private set; }

    private DarkRiftServer server;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        server = GetComponent<XmlUnityServer>().Server;
        server.ClientManager.ClientConnected += OnClientConnected;
        server.ClientManager.ClientDisconnected += OnClientDisconnected;
    }

    private void OnDestroy() {
        server.ClientManager.ClientConnected -= OnClientConnected;
        server.ClientManager.ClientDisconnected -= OnClientDisconnected;
    }

    private void OnClientConnected(object sender, ClientConnectedEventArgs e) {
        e.Client.MessageReceived += OnMessage;
    }
    
    private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
        if (Players.TryGetValue(e.Client.ID, out ClientConnection clientConnection)) {
            clientConnection.OnClientDisconnected(sender, e);
        }
        e.Client.MessageReceived -= OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e) {
        IClient client = (IClient)sender;
        using(var message = e.GetMessage()) {
            switch ((MessageTag)message.Tag) {
                case MessageTag.LoginRequest:
                    OnClientLogin(client, message.Deserialize<LoginRequestData>());
                    break;
            }
        }
    }

    private void OnClientLogin(IClient client, LoginRequestData data) {
        if (PlayersByName.ContainsKey(data.UserName)) {
            // client bereits eingeloggt oder name bereits vergeben
            using (var message = Message.CreateEmpty((ushort)MessageTag.LoginDenied)) {
                client.SendMessage(message, SendMode.Reliable);
            }
            return;
        }

        // unsubscriben. Ab jetzt werden messages vom client über
        // die ClientConnection gehandhabt.
        client.MessageReceived -= OnMessage;

        // ClientConnection erstellen und cachen
        var cc = new ClientConnection(data.UserName, client);
        Players.Add(client.ID, cc);
        PlayersByName.Add(data.UserName, cc);

        // Antwort an den client senden
        using (var m = Message.Create((ushort)MessageTag.LoginAccepted, new LoginResponseData(client.ID, new LobbyData(RoomManager.Instance.GetRoomData())))) {
            client.SendMessage(m, SendMode.Reliable);
        }
    }
}
