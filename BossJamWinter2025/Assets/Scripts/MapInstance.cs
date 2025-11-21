using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.IO;
using UnityEngine.Events;

public class MapInstance : NetworkBehaviour {
    [Networked] private bool started { get; set; }
    [Networked] private TickTimer timeRemaining { get; set; }
    [Networked] private TickTimer endTimer { get; set; }
    [Networked] [Capacity(16)] private NetworkDictionary<PlayerRef, int> kills => default;
    [Networked] private int killGoal { get; set; }

    public NetworkObject playerPrefab;

    // public UnityEvent RoundEnded;

    public override void Spawned() {
        base.Spawned();

        GameManager.Instance.OnMapBootstrapLoaded(this);

        // Just SEND it
        var spawnPoint = GameObject.FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None).GetRandom();
        Runner.Spawn(playerPrefab, spawnPoint.transform.position + Vector3.up, spawnPoint.transform.rotation, Runner.LocalPlayer);
    }

    public void StartRound() {
        started = true;
        timeRemaining = TickTimer.CreateFromSeconds(Runner, 60);
        kills.Clear();
        killGoal = 2;

        Debug.Log("Round started");
    }

    public override void FixedUpdateNetwork() {
        base.FixedUpdateNetwork();

        if (HasStateAuthority) {
            if (endTimer.Expired(Runner)) {
                enabled = false;
                Debug.Log("Map ended, triggering next map");
                GameManager.Instance.NextMap();
            }

            if (started && timeRemaining.Expired(Runner)) {
                Debug.Log("Map ended, waiting...");
                endTimer = TickTimer.CreateFromSeconds(Runner, 10);
            }
        }
    }
}
