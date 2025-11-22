using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;

#pragma warning disable UNT0006 // Incorrect message signature (Believe it is confusing Unity's own networking methods with Fusions')

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks {
    public static GameManager Instance { get; private set; }

    public NetworkRunner runner;
    private string roomIdentifier = "test_room";

    public string[] gameplayScenePaths;

    protected void Awake() {
        Debug.Assert(Instance == null, "Trying to assign a second GameManager singleton instance!");
        Instance = this;
    }

    private async void StartGame() {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        await runner.StartGame(new StartGameArgs() {
            GameMode = GameMode.Shared,
            SessionName = roomIdentifier,
            Scene = null,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        });
    }

    protected void OnGUI() {
        if (runner == null) {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enter")) {
                StartGame();
            }
            roomIdentifier = GUILayout.TextField(roomIdentifier);
            GUILayout.EndHorizontal();
        }
    }

    public void NextMap() {
        var sceneIndex = SceneUtility.GetBuildIndexByScenePath(gameplayScenePaths.GetRandom());
        Debug.Assert(sceneIndex >= 0, "Failed getting scene from path, possibly forgot to add it to the build scene list");

        runner.LoadScene(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);
    }

    public void OnMapBootstrapLoaded(MapInstance mapBootstrap) {
        if (runner == null) {
            Debug.LogWarning("MapBootstrap loaded without an active NetworkRunner, the game will not run as expected");
            return;
        }

        if (runner.IsSharedModeMasterClient) {
            // Just start instantly for now
            mapBootstrap.StartRound();
        }
    }


    // INetworkRunnerCallbacks

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) {}
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        // If we are the master client, we need to initiate the game for everyone
        if (runner.IsSharedModeMasterClient) {
            NextMap();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {}
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
