using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

public enum GameState {
    PreGame,
    MidGame,
    PostGame,
}

public struct FeedEntry {
    public string message;
    public float time;
}

public class MapInstance : NetworkBehaviour {
    [Preserve]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Init() {
        ActiveInstance = null;
    }

    public static MapInstance ActiveInstance { get; private set; }

    [Networked] public GameState currentState { get; private set; } = GameState.PreGame;
    [Networked] public TickTimer currentStateTimer { get; private set; }

    [Networked] [Capacity(16)] private NetworkDictionary<PlayerRef, int> kills => default;
    [Networked] private int killGoal { get; set; } = 5;

    public Queue<FeedEntry> feed = new();

    public NetworkObject playerPrefab;

    protected void Update() {
        // Remove items from the feed
        while (feed.TryPeek(out var entry) && entry.time + 5 < Time.unscaledTime) {
            feed.Dequeue();
        }
    }

    public override void Spawned() {
        base.Spawned();

        Debug.Assert(ActiveInstance == null, "There already was an active map instance, something went wrong");
        ActiveInstance = this;

        GameManager.Instance.OnMapBootstrapLoaded(this);
        SpawnOwnPlayer();
    }

    private void SpawnOwnPlayer() {
        var players = FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None);
        var spawnPoints = FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None); // Imagine caching any of this

        // Get some spawn points that are far enough from other players
        const float MIN_DISTANCE = 10.0f;
        var validPoints = new List<SpawnPointPlayer>();
        foreach (var potentialSpawnPoint in spawnPoints) {
            bool valid = false;
            foreach (var player in players) {
                if (Vector3.Distance(potentialSpawnPoint.transform.position, player.transform.position) < MIN_DISTANCE) {
                    valid = false;
                }
            }

            // TODO Add a raycast to make sure we don't spawn visible to other players

            if (valid) {
                validPoints.Add(potentialSpawnPoint);
            }
        }

        // Select the spawn point to actually use
        SpawnPointPlayer spawnPoint = spawnPoints.GetRandom();
        if (validPoints.Count > 0) {
            spawnPoint = validPoints.GetRandom();
        } else {
            Debug.LogWarning("Unable to find a suitable spawn point, choosing a random one");
        }

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

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_ReportKill(PlayerRef killedPlayer, RpcInfo info = default) {
        Debug.Log($"{info.Source} reported a kill");

        // Add fluff for everyone
        feed.Enqueue(new FeedEntry() {
            message = $"Hello World! {info.Source} -> {killedPlayer}",
            time = Time.unscaledTime,
        });

        // Set the logical kill on master client only
        if (Runner.IsSharedModeMasterClient) {
            if (!kills.TryGet(info.Source, out int killCount)) {
                killCount = 0;
            }

            killCount++;
            kills.Set(info.Source, killCount);
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
        var killTargetReached = false;
        foreach ((PlayerRef player, int count) in kills) {
            if (count >= killGoal) {
                killTargetReached = true;
                break;
            }
        }

        if (currentStateTimer.Expired(Runner) || killTargetReached) {
            Debug.Log("Ending Game...");

            currentState = GameState.PostGame;
            currentStateTimer = TickTimer.CreateFromSeconds(Runner, 10);
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
