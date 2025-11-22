using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.VisualScripting;

// Player metadata, each player spawns one while they are in the room, whoever has StateAuthority is the PlayerRef who's data this belongs to
public class NetworkPlayerData : NetworkBehaviour {
    [Networked] public NetworkString<_32> playerName { get; set; }
    [Networked] public Color color { get; set; }


    public static List<NetworkPlayerData> instances = new();

    public override void Spawned() {
        base.Spawned();

        instances.Add(this);
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
