using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

// Player metadata, each player spawns one while they are in the room, whoever has StateAuthority is the PlayerRef who's data this belongs to
public class NetworkPlayerData : NetworkBehaviour {
    [SerializeField] [Networked] [Capacity(32)] public string playerName { get; set; }
    [SerializeField] [Networked] public Color color { get; set; }

    public static List<NetworkPlayerData> instances = new();

    protected void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    public override void Spawned() {
        base.Spawned();

        instances.Add(this);
        if (MapInstance.ActiveInstance != null) {
            MapInstance.ActiveInstance.feed.Enqueue(new FeedEntry() {
                message = $"{playerName} joined!",
                time = Time.unscaledTime,
            });
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        base.Despawned(runner, hasState);

        Debug.Assert(instances.Contains(this), "Trying to remove a NetworkPlayerData instance that wasn't registered!");
        instances.Remove(this);
    }

    public static bool TryGet(out NetworkPlayerData data, PlayerRef playerRef) {
        foreach (var instance in instances) {
            if (instance.Object.StateAuthority == playerRef) {
                data = instance;
                return true;
            }
        }

        data = null;
        return false;
    }
}
