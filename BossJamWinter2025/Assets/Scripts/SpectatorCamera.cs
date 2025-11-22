using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorCamera : MonoBehaviour {
    private AudioListener audioListener;

    protected void Awake() {
        audioListener = GetComponent<AudioListener>();
    }

    protected void Update() {
        audioListener.enabled = Camera.main == null || Camera.main.gameObject == gameObject;
    }
}
