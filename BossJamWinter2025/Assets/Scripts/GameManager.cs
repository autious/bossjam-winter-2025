using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks {
    public NetworkObject playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> playerInstances = new();

    private NetworkRunner runner;
    private string roomIdentifier = "test_room";

    private async void StartGame(GameMode gameMode) {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        await runner.StartGame(new StartGameArgs() {
            GameMode = gameMode,
            SessionName = roomIdentifier,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        });
    }

    protected void OnGUI() {
        if (runner == null) {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host")) {
                StartGame(GameMode.Host);
            }
            roomIdentifier = GUILayout.TextField(roomIdentifier);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Join")) {
                StartGame(GameMode.Client);
            }
            GUILayout.EndVertical();
        }
    }


    // INetworkRunnerCallbacks

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        var data = new NetworkInputData();
        if (Input.GetKey(KeyCode.W)) data.direction += Vector2.up;
        if (Input.GetKey(KeyCode.S)) data.direction += Vector2.down;
        if (Input.GetKey(KeyCode.A)) data.direction += Vector2.left;
        if (Input.GetKey(KeyCode.D)) data.direction += Vector2.right;
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        if (runner.IsServer) {
            var playerInstance = runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);

            Debug.Assert(!playerInstances.ContainsKey(player), "Already create a player instance for this player ref");
            playerInstances[player] = playerInstance;
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (playerInstances.ContainsKey(player)) {
            runner.Despawn(playerInstances[player]);
            playerInstances.Remove(player);
        }
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
