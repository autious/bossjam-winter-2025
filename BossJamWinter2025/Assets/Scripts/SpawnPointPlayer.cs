using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawn Point marker for players
public class SpawnPointPlayer : MonoBehaviour {
    protected void OnDrawGizmos() {
        // TODO replace with a nicer representation
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + Vector3.up, new Vector3(1, 2, 1));
    }
}
