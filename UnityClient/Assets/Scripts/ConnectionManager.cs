using System;
using System.Net;
using DarkRift.Client.Unity;
using UnityEngine;

[RequireComponent(typeof(UnityClient))]
public class ConnectionManager : MonoBehaviour {
    public static ConnectionManager Instance { get; private set; }
    public UnityClient Client { get; private set; }
    public ushort PlayerID { get; set; }
    public LobbyData LobbyData { get; set; }

    internal delegate void OnConnectedDelegate();
    internal event OnConnectedDelegate OnConnected;

    [SerializeField] private string ipAddress;
    [SerializeField] private int port;

    
    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Client = GetComponent<UnityClient>();
    }

    private void OnConnectionResponse(Exception e) {
        switch (Client.ConnectionState) {
            case DarkRift.ConnectionState.Connecting:
                Debug.Log("connecting...");
                break;

            case DarkRift.ConnectionState.Connected:
                // erfolgreich connected
                // nachdem man verbunden ist kann der login prozess starten
                // siehe LoginManager.cs
                OnConnected?.Invoke();
                Debug.Log("connected to server");
                break;

            default:
                Debug.LogError($"failed to connect to {ipAddress}:{port}");
                break;
            
        }
    }

    public void TryConnect() {
        Client.ConnectInBackground(IPAddress.Parse(ipAddress), port, true, OnConnectionResponse);
    }
}
