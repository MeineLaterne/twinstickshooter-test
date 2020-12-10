using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using DarkRift.Client.Unity;
using UnityEngine;

[RequireComponent(typeof(UnityClient))]
public class ConnectionManager : MonoBehaviour {
    public static ConnectionManager Instance { get; private set; }
    public UnityClient Client { get; private set; }
    public ushort PlayerId { get; set; }
    public LobbyData LobbyData { get; set; }

    internal delegate void OnConnectedDelegate();
    internal event OnConnectedDelegate OnConnected;

    [SerializeField] private string ipAddress;
    [SerializeField] private int port;

    private Process serverProcess;

    public void TryConnect() {
        Client.ConnectInBackground(IPAddress.Parse(ipAddress), port, true, OnConnectionResponse);
    }

    public void TryStartServer() {
        var serverPath = Directory.GetCurrentDirectory().Replace("UnityClient", @"UnityServer\Build\UnityServer.exe");

        if (File.Exists(serverPath)) {
            serverProcess = Process.Start(serverPath, "-batchmode -nographics");

            if (serverProcess == null || serverProcess.HasExited == true) {
                UnityEngine.Debug.Log("failed to start server");
                return;
            }

            UnityEngine.Debug.Log("server running");
        }
    }

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
                UnityEngine.Debug.Log("connecting...");
                break;

            case DarkRift.ConnectionState.Connected:
                // erfolgreich connected
                // nachdem man verbunden ist kann der login prozess starten
                // siehe LoginManager.cs
                OnConnected?.Invoke();
                break;

            default:
                UnityEngine.Debug.LogError($"failed to connect to {ipAddress}:{port}");
                break;
            
        }
    }

    private void OnDestroy() {
        if (serverProcess != null) {
            serverProcess.Kill();
            serverProcess.WaitForExit();
            serverProcess.Dispose();
        }
    }

}
