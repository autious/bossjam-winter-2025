using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Hittable : MonoBehaviour
{
    public abstract void OnHit(Vector3 hitPoint, Vector3 hitNormal, bool cosmetic);
}
