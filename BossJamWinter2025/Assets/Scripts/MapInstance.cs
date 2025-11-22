using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public enum GameState {
    PreGame,
    MidGame,
    PostGame,
}

public class MapInstance : NetworkBehaviour {
    public static MapInstance ActiveInstance { get; private set; }

    [Networked] public GameState currentState { get; private set; } = GameState.PreGame;
    [Networked] public TickTimer currentStateTimer { get; private set; }

    [Networked] [Capacity(16)] private NetworkDictionary<PlayerRef, int> kills => default;
    [Networked] private int killGoal { get; set; } = 5;

    public NetworkObject playerPrefab;

    public override void Spawned() {
        base.Spawned();

        GameManager.Instance.OnMapBootstrapLoaded(this);
        ActiveInstance = this;
        SpawnOwnPlayer();
    }

    private void SpawnOwnPlayer() {
        var spawnPoint = GameObject.FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None).GetRandom(); // TODO Find a spawn point that doesn't have other people around it
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

        // Check if we need to respawn // TODO this should be in MidGame update, but since that is sectioned off for StateAuthority, we have it here for now
        var allPlayers = GameObject.FindObjectsOfType<QuickPlayerController>();
        if (!allPlayers.Any((x) => x.HasStateAuthority)) {
            SpawnOwnPlayer();
        }

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
