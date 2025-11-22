using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class QuickPlayerController : NetworkBehaviour
{
    [Header("Motion Ref.")]
    [SerializeField] Rigidbody rb;
    [SerializeField] CapsuleCollider col;
    [SerializeField] LayerMask collisionLayer;

    [SerializeField] Transform head;
    [SerializeField] Transform cam;
    [SerializeField] GameObject camThingy;

    [Header("Motion Settings")]
    [SerializeField, Range(0, 1)] float airControl;
    [SerializeField] float moveSpeed = 12;
    [SerializeField] float maxSpeed = 5;
    [SerializeField] float maxAirWishSpeed = 8;
    [SerializeField] float airAcceleration = 100;

    [SerializeField] float gravity = 12;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] bool enableJumping = true;

    [SerializeField] float coyoteTime = 0.2f;
    float coyoteTimer = 0;
    bool canJump = true;

    Vector3 inputMotion, controlMotion, finalMotion;
    bool isGrounded = true;

    [Header("Camera Settings.")]
    [SerializeField] CharacterAnimation charAnim;
    [SerializeField] GameObject charModel;
    [SerializeField] GameObject headModel;
    [SerializeField] float tiltAmount = 10;
    [SerializeField] float tiltLerpSpeed = 10;
    Vector3 tiltVector;
    float mx, my;


    [Header("Shooting")]
    [SerializeField] PlayerVoiceLines playerVoice;
    [SerializeField] PlayerGun playerGun;
    [SerializeField] Transform gunFirePoint;
    [SerializeField] Transform logicalFirePoint;
    [SerializeField] float gunCooldown;
    float gunCdTimer;


    [SerializeField] GameObject laserPrefab;
    private BounceRay laser;

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    bool GroundCheck()
    {
        bool _result = Physics.CheckSphere(transform.position + new Vector3(0, col.radius - 0.1f, 0), col.radius - 0.05f, collisionLayer);
        return _result;
    }

    public override void Spawned()
    {
        camThingy.SetActive(HasStateAuthority);
        //charModel.SetActive(HasStateAuthority == false);
        headModel.SetActive(HasStateAuthority == false);
        charModel.SetActive(HasStateAuthority == false);

        var players = FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None);
        var spawnPoints = FindObjectsByType<SpawnPointPlayer>(FindObjectsSortMode.None); // Imagine caching any of this

        // Get some spawn points that are far enough from other players
        const float MIN_DISTANCE = 5.0f;
        var validPoints = new List<SpawnPointPlayer>();
        foreach (var potentialSpawnPoint in spawnPoints) {
            bool valid = false;
            foreach (var player in players) {
                if (Vector3.Distance(potentialSpawnPoint.transform.position, player.transform.position) < MIN_DISTANCE) {
                    valid = false;
                }
            }

            // TODO Add a raycast to make sure we don't spawn visible to other players

            if (valid) {
                validPoints.Add(potentialSpawnPoint);
            }
        }

        // Select the spawn point to actually use
        SpawnPointPlayer spawnPoint = spawnPoints.GetRandom();
        if (validPoints.Count > 0) {
            spawnPoint = validPoints.GetRandom();
        } else {
            Debug.LogWarning("Unable to find a suitable spawn point, choosing a random one");
        }

        var rb = GetComponent<Rigidbody>();
        rb.position = spawnPoint.transform.position + Vector3.up;
        rb.rotation = spawnPoint.transform.rotation;
        transform.position = spawnPoint.transform.position + Vector3.up;
        transform.rotation = spawnPoint.transform.rotation;
    }

    Vector3 posLastFrame = Vector3.zero;

    void Update() {
        if (GameManager.Instance != null) {
            if (!HasStateAuthority)
            {
                // Online player stuff
                isGrounded = GroundCheck();
                my = Mathf.Clamp(cam.localEulerAngles.x, -89, 89);
                Vector3 moveDir = transform.position - posLastFrame;
                moveDir.y = 0;
                float move = Vector3.Dot(head.forward, moveDir) >= 0 ? 1 : -1;
                float strafe = Vector3.Dot(head.right, moveDir) >= 0 ? 1 : -1;

                if (moveDir.magnitude <= 0.05) {
                    move = 0;
                    strafe = 0;
                }
                charAnim.SetValues(move, strafe, my, isGrounded);
                posLastFrame = transform.position;
                return;
            }
        }

        if (GroundCheck())
        {
            if (rb.velocity.y <= 0)
            {
                if (isGrounded == false)
                {
                    isGrounded = true;
                }
                canJump = true;
                coyoteTimer = coyoteTime;
            }
        }
        else
        {
            isGrounded = false;
            if (coyoteTimer > 0)
            {
                coyoteTimer -= Time.deltaTime;
            }
            else
            {
                canJump = false;
            }
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        CameraMotion(mouseX, mouseY);

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        Movement(moveX, moveY, jumpInput);

        gunCdTimer -= Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && gunCdTimer <= 0) {
            gunCdTimer = gunCooldown;
            playerGun.RPC_ReportCosmeticBullet(logicalFirePoint.position + transform.forward * 0.3f, logicalFirePoint.rotation, gunFirePoint.position);
            playerVoice.TryPlayEvent(PlayerVoiceLines.VoiceEvent.OnShotGun);
        }

        if(Input.GetMouseButtonDown(1)) {
            laser = Instantiate(laserPrefab,logicalFirePoint.position + transform.forward * 0.3f, logicalFirePoint.rotation, logicalFirePoint.transform).GetComponent<BounceRay>();
            laser.Preview(gunFirePoint.position);
        }
        if(Input.GetMouseButton(1)) {
            if(laser != null) {
                laser.Preview(gunFirePoint.position);
            }
        }
        if(Input.GetMouseButtonUp(1)) {
            if(laser != null) {
                Destroy(laser.gameObject);
                laser = null;
            }
        }
    }


    void FixedUpdate(){
        if (GameManager.Instance != null) {
            if (!HasStateAuthority)
            {
                return;
            }
        }

        rb.AddForce(Vector3.down * gravity);

        Vector3 motionCalc = -rb.velocity;
        motionCalc.y = 0;

        rb.AddForce(motionCalc * ((moveSpeed * (isGrounded ? 1 : airControl)) / maxSpeed));
        rb.AddForce(inputMotion * (moveSpeed * (isGrounded ? 1 : airControl)));

        if (isGrounded == false)
        {
            AirAccelerate();
        }
    }
     void AirAccelerate()
    {
        Vector3 wishdir = inputMotion;
        float wishspeed = maxSpeed;
        float airaccelerate = airAcceleration;

        float addspeed;
        float accelspeed;
        float currentspeed;

        if (wishspeed > maxAirWishSpeed)
        {
            wishspeed = maxAirWishSpeed;
        }

        currentspeed = Vector3.Dot(rb.velocity, wishdir);

        addspeed = wishspeed - currentspeed;

        if (addspeed <= 0f)
            return;

        accelspeed = airaccelerate * Time.fixedDeltaTime * wishspeed;

        if (accelspeed > addspeed)
        {
            accelspeed = addspeed;
        }

        rb.velocity += accelspeed * wishdir;
    }

    void Movement(float moveX, float moveY, bool jumpInput)
    {
        //What direction are we moving in?
        inputMotion = new Vector3(moveX, 0, moveY);
        inputMotion = head.rotation * Vector3.ClampMagnitude(inputMotion, 1);

        //How much control do we have?
        controlMotion = Vector3.Lerp(controlMotion, inputMotion * maxSpeed, moveSpeed * (isGrounded ? 1 : airControl) * Time.deltaTime);

        //The final motion result!
        finalMotion = controlMotion;

        if (enableJumping && jumpInput && canJump)
        {
            canJump = false;

            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = Mathf.Sqrt(2 * jumpHeight * gravity);
            rb.velocity = currentVelocity;
            playerVoice.TryPlayEvent(PlayerVoiceLines.VoiceEvent.OnJump);
        }
    }

    void CameraMotion(float mouseX, float mouseY)
    {
        mx += mouseX;
        my -= mouseY;

        my = Mathf.Clamp(my, -89, 89);
        my = 0;

        tiltVector = Vector3.Lerp(tiltVector, head.InverseTransformDirection(rb.velocity) * tiltAmount, tiltLerpSpeed * Time.deltaTime);

        head.localEulerAngles = new Vector3(0, mx, 0);
        cam.localEulerAngles = new Vector3(my - tiltVector.y, 0, -tiltVector.x);
    }

    public void KillPlayer() {
        MapInstance.ActiveInstance.RPC_ReportKill(Object.StateAuthority);
    }
}
