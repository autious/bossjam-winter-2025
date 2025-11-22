using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using System.Threading.Tasks;

public enum GameState {
    PreGame,
    MidGame,
    PostGame,
    None,
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

    public NetworkObject playerPrefab;

    public Coroutine respawnCoroutine;

    protected void Update() {
        // We likely don't have authority over this object, so we cannot do this in `FixedUpdateNetwork`
        if (Object == null || !Object.IsValid) {
            return;
        }

        if (respawnCoroutine == null) {
            // Check if we need to respawn // TODO this should be in MidGame update, but since that is sectioned off for StateAuthority, we have it here for now
            var allPlayers = GameObject.FindObjectsOfType<QuickPlayerController>();
            if (!allPlayers.Any((x) => x.HasStateAuthority)) {
                respawnCoroutine = StartCoroutine(RespawnCoroutine());
            }
        }

    }

    public override void Spawned() {
        base.Spawned();

        Debug.Assert(ActiveInstance == null, "There already was an active map instance, something went wrong");
        ActiveInstance = this;

        GameManager.Instance.OnMapBootstrapLoaded(this);
    }

    public IEnumerator RespawnCoroutine() {
        yield return new WaitForSeconds(1);

        yield return Runner.SpawnAsync(playerPrefab, Vector3.zero, Quaternion.identity, Runner.LocalPlayer);
        yield return new WaitForSeconds(2);
        respawnCoroutine = null;
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

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_ReportKill(PlayerRef killedPlayer, RpcInfo info = default) {
        bool isLocalPlayerInvolved = killedPlayer == Runner.LocalPlayer || info.Source == Runner.LocalPlayer;

        // Gather some data
        NetworkPlayerData.TryGet(out NetworkPlayerData killee, killedPlayer);
        NetworkPlayerData.TryGet(out NetworkPlayerData killer, info.Source);
        if (killee == null || killer == null) {
            Debug.LogWarning("We got a kill report, but one of the participants has no Network Player Data.. Ignoring!");
            return;
        }

        // Despawn the player object when killed
        if (isLocalPlayerInvolved && Runner.LocalPlayer == killedPlayer) {
            var player = GameObject.FindObjectsOfType<QuickPlayerController>().FirstOrDefault((x) => x.HasStateAuthority);

            var spectatorCamera = GameObject.FindFirstObjectByType<SpectatorCamera>();
            spectatorCamera.transform.position = player.camThingy.transform.position;
            spectatorCamera.transform.rotation = player.camThingy.transform.rotation;
            Runner.Despawn(player.Object);
        }

        // Add feed fluff
        var feedColor = isLocalPlayerInvolved ? "#ae0c01ff" : "#f3fcf3ff";
        GameManager.Instance.feed.Enqueue(new FeedEntry() {
            message = $"<color={feedColor}><b>{killer.playerName}</b> killed <b>{killee.playerName}</b></color>",
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
            currentStateTimer = TickTimer.CreateFromSeconds(Runner, 120);
            kills.Clear();
            killGoal = 5;
        }
    }

    protected virtual void UpdateMidGame() {
        return; // Just ignore timers and win conditions

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

            GameManager.Instance.NextMap();
            currentState = GameState.None;
        }
    }
}
