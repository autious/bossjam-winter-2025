using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlinkingText : MonoBehaviour {
    private TMP_Text text;

    private float waitTime = 0.5f;

    protected void Awake() {
        text = GetComponent<TMP_Text>();
    }

    protected void Update() {
        waitTime -= Time.deltaTime;
        if (waitTime <= 0) {
            waitTime = 0.5f;
            text.enabled = !text.enabled;
        }
    }
}
