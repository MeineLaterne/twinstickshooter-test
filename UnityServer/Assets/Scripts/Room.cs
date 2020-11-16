using DarkRift;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Room : MonoBehaviour {

    public string RoomName => roomName;
    public byte Slots => slots;

    public List<ClientConnection> ClientConnections { get; } = new List<ClientConnection>();

    [SerializeField] private string roomName;
    [SerializeField] private byte slots;
    
    private Scene scene;
    private PhysicsScene physicsScene;

    public void Initialize(string roomName, byte slots) {
        this.roomName = roomName;
        this.slots = slots;

        var csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene($"room_{roomName}", csp);
        physicsScene = scene.GetPhysicsScene();

        SceneManager.MoveGameObjectToScene(gameObject, scene);
    }

    public void AddPlayer(ClientConnection clientConnection) {
        if (ClientConnections.Contains(clientConnection)) {
            return;
        }

        clientConnection.Room = this;
        ClientConnections.Add(clientConnection);

        using (var msg = Message.CreateEmpty((ushort)MessageTag.JoinRoomAccepted)) {
            clientConnection.client.SendMessage(msg, SendMode.Reliable);
        }
    }

    public void RemovePlayer(ClientConnection clientConnection) {
        clientConnection.Room = null;
        ClientConnections.Remove(clientConnection);
    }

    public void Close() {
        foreach (var clientConnection in ClientConnections) {
            RemovePlayer(clientConnection);
        }
        Destroy(gameObject);
    }
}
