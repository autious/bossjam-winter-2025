using Fusion;
using System;
using UnityEngine;

public class QuickPlayerController : NetworkBehaviour
{
    [Header("Motion Ref.")]
    [SerializeField] Rigidbody rb;
    [SerializeField] CapsuleCollider col;
    [SerializeField] LayerMask collisionLayer;

    [SerializeField] Transform head;
    [SerializeField] Transform cam;

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
    [SerializeField] float tiltAmount = 10;
    [SerializeField] float tiltLerpSpeed = 10;
    Vector3 tiltVector;
    float mx, my;
    
    
    [Header("Shooting")] 
    [SerializeField] PlayerGun playerGun;
    [SerializeField] Transform gunFirePoint;
    [SerializeField] float gunCooldown;
    float gunCdTimer;


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
        cam.gameObject.SetActive(HasStateAuthority);
    }

    void Update()
    {
        if (!HasStateAuthority)
        {
            return;
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
            playerGun.RPC_ReportCosmeticBullet(gunFirePoint.position, gunFirePoint.rotation);
        }
    }

    void FixedUpdate(){
        if (!HasStateAuthority)
        {
            return;
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
        }
    }

    void CameraMotion(float mouseX, float mouseY)
    {
        mx += mouseX;
        my -= mouseY;

        my = Mathf.Clamp(my, -89, 89);

        tiltVector = Vector3.Lerp(tiltVector, head.InverseTransformDirection(rb.velocity) * tiltAmount, tiltLerpSpeed * Time.deltaTime);

        head.localEulerAngles = new Vector3(0, mx, 0);
        cam.localEulerAngles = new Vector3(my - tiltVector.y, 0, -tiltVector.x);
    }
}
