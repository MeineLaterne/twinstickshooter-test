using DarkRift;
using DarkRift.Client;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour {

    [SerializeField] private InputField nameInput;
    private string loginName;

    private void Start() {
        ConnectionManager.Instance.OnConnected += StartLogin;
        ConnectionManager.Instance.Client.MessageReceived += OnMessage;
    }

    private void OnDestroy() {
        ConnectionManager.Instance.OnConnected -= StartLogin;
        ConnectionManager.Instance.Client.MessageReceived -= OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e) { 
        using (var message = e.GetMessage()) {
            switch ((MessageTag)message.Tag) {
                
                case MessageTag.LoginAccepted:
                    OnLoginAccepted(message.Deserialize<LoginResponseData>());
                    break;
                
                case MessageTag.LoginDenied:
                    OnLoginDenied();
                    break;
                
            }
        }
    }

    private void OnLoginAccepted(LoginResponseData data) {
        Debug.Log($"login successful! Got id: {data.ID}");

        ConnectionManager.Instance.PlayerId = data.ID;
        ConnectionManager.Instance.LobbyData = data.LobbyData;

        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    private void OnLoginDenied() {
        Debug.LogError("login denied!");
    }

    // wird vom ConnectionManager aufgerufen wenn die Verbindung
    // zum Server steht.
    // hier senden wir dann einen login request an den Server, der uns dann eine id (und später evtl. nen session token oder so) zurückgibt
    public void StartLogin() {
        loginName = string.IsNullOrEmpty(nameInput.text) ? Guid.NewGuid().ToString() : nameInput.text;
        using(var message = Message.Create((ushort)MessageTag.LoginRequest, new LoginRequestData(loginName))) {
            ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
        }
    }

}
