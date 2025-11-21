using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TempPlayer : NetworkBehaviour {
    private new Camera camera;

    protected void Awake() {
        camera = GetComponentInChildren<Camera>();
    }

    public override void Spawned() {
        camera.enabled = HasInputAuthority;
    }

    protected void Update() {
        if (HasStateAuthority) {
            if (Input.GetKey(KeyCode.W)) transform.position += 5 * Time.deltaTime * transform.forward;
            if (Input.GetKey(KeyCode.S)) transform.position -= 5 * Time.deltaTime * transform.forward;
            if (Input.GetKey(KeyCode.A)) transform.position -= 5 * Time.deltaTime * transform.right;
            if (Input.GetKey(KeyCode.D)) transform.position += 5 * Time.deltaTime * transform.right;
        }
    }
}
