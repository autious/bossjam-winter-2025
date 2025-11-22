using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlayerHittable : Hittable {
    public QuickPlayerController playerController;

    protected void Awake() {
        Debug.Assert(playerController != null, $"{nameof(QuickPlayerController)} was not assigned on KillPlayerHittable ({gameObject.name})");
    }

    public override void OnHit(Vector3 hitPoint, Vector3 hitNormal, bool cosmetic) {
        if (!cosmetic) {
            Debug.Log("Player was killed");
            playerController.KillPlayer();
        }
    }
}
