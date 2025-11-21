using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TempPlayer : NetworkBehaviour {
    private NetworkCharacterController characterController;
    private new Camera camera;

    protected void Awake() {
        characterController = GetComponent<NetworkCharacterController>();
        camera = GetComponentInChildren<Camera>();
    }

    public override void Spawned() {
        camera.enabled = HasInputAuthority;
    }

    public override void FixedUpdateNetwork() {
        base.FixedUpdateNetwork();

        if (GetInput(out NetworkInputData data)) {
            Vector3 movement = new Vector3(data.direction.x, 0, data.direction.y);
            movement = movement * 5 * Runner.DeltaTime;
            characterController.Move(movement);
        }
    }
}
