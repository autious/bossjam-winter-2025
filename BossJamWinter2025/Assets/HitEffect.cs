using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour {
    public GameObject sphere;

    public float start = 0.01f;
    public float end = 0.2f;
    public float duration = 0.5f;
    private float startTime;
    void Start() {
        startTime = Time.time;
    }

    void Update() {
        float scale = Mathf.Lerp(start, end, Mathf.Clamp01((Time.time - startTime) / duration));
        sphere.transform.localScale = new Vector3(scale, scale, scale);
        if(Time.time - startTime > duration) {
            Destroy(gameObject);
        }
    }
}
