using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

enum GameState {
    PreGame,
    MidGame,
    PostGame,
}

public class MapInstance : NetworkBehaviour {
    [Networked] private GameState currentState { get; set; } = GameState.PreGame;
    [Networked] private TickTimer currentStateTimer { get; set; }

    [Networked] [Capacity(16)] private NetworkDictionary<PlayerRef, int> kills => default;
    [Networked] private int killGoal { get; set; } = 5;

    public NetworkObject playerPrefab;

    public override void Spawned() {
        base.Spawned();

        GameManager.Instance.OnMapBootstrapLoaded(this);

        // Just SEND it
        var spawnPoint = GameObject.FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None).GetRandom();
        Runner.Spawn(playerPrefab, spawnPoint.transform.position + Vector3.up, spawnPoint.transform.rotation, Runner.LocalPlayer);
    }

    public void StartRound() {
        currentState = GameState.PreGame;
        currentStateTimer = TickTimer.CreateFromSeconds(Runner, 4);
        kills.Clear();
        killGoal = 2;

        Debug.Log("Started PreGame");
    }

    public override void FixedUpdateNetwork() {
        base.FixedUpdateNetwork();

        if (HasStateAuthority) {
            switch (currentState) {
                case GameState.PreGame:
                    UpdatePreGame();
                    break;
                case GameState.MidGame:
                    UpdateMidGame();
                    break;
                case GameState.PostGame:
                    UpdatePostGame();
                    break;
            }
        }
    }

    protected virtual void UpdatePreGame() {
        if (currentStateTimer.Expired(Runner)) {
            Debug.Log("Starting Game...");

            currentState = GameState.MidGame;
            currentStateTimer = TickTimer.CreateFromSeconds(Runner, 60);
            kills.Clear();
            killGoal = 2;
        }
    }

    protected virtual void UpdateMidGame() {
        if (currentStateTimer.Expired(Runner)) {
            Debug.Log("Ending Game...");

            currentState = GameState.PostGame;
            currentStateTimer = TickTimer.CreateFromSeconds(Runner, 20);
        }
    }

    protected virtual void UpdatePostGame() {
        if (currentStateTimer.Expired(Runner)) {
            Debug.Log("Game Ended, triggering next map");

            enabled = false; // Prevent further updates
            GameManager.Instance.NextMap();
        }
    }
}
