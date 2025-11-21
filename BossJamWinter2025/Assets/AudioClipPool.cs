using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioClipPool : MonoBehaviour
{

    public AudioClip[] pool;
    private AudioSource source;
    private void Awake() {
        source = GetComponent<AudioSource>();
        if(source != null) {
            if(source != null && pool != null && pool.Length > 0) {
                source.clip = pool[Random.Range(0, pool.Length)];
            }
            source.Play();

            StartCoroutine(DestroyWhenFinished());
        }
    }

    private IEnumerator DestroyWhenFinished() {
        while(source.isPlaying) {
            yield return null;
        }
        Destroy(gameObject);
    }
}
