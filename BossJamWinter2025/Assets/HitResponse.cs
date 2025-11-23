using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitResponse : Hittable {
    public GameObject[] hitEffects;
    public override void OnHit(Vector3 hitPoint, Vector3 hitNormal, bool cosmetic) {
        if(hitEffects != null) {
            foreach(var effect in hitEffects) {
                Instantiate(effect, hitPoint, Quaternion.FromToRotation(Vector3.forward,hitNormal));
            }
        }
    }
}
