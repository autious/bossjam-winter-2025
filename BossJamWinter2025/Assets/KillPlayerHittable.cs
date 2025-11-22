using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlayerHittable : Hittable {
    QuickPlayerController playerController;
    public override void OnHit(Vector3 hitPoint, Vector3 hitNormal, bool cosmetic) {
        if(!cosmetic) {
            playerController.KillPlayer();
        }
        Debug.Log("Player was killed");
    }
}
