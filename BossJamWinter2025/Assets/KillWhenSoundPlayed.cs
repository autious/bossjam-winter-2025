using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillWhenSoundPlayed : MonoBehaviour {
    private AudioSource audioSource;
    void Start() {
        audioSource = GetComponent<AudioSource>();
    }

    void Update() {
        if(audioSource != null && audioSource.isPlaying == false) {
            Destroy(gameObject);
        }
    }
}
