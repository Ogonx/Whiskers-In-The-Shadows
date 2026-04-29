using UnityEngine;
using UnityEngine.InputSystem;

public class CatController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float jumpCooldown = 0.6f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundMask;

    [Header("Input Buffer")]
    public float inputBufferTime = 0.2f; // how long an input press is remembered after the frame it happened

    [Header("Sniff Audio")]
    [SerializeField] AudioSource sniffSource;
    [SerializeField] AudioClip sniffClip;
    [Range(0f, 1f)][SerializeField] float sniffVolume = 0.7f;
    [SerializeField] Vector2 sniffPitchRange = new Vector2(0.92f, 1.05f);

    [Header("Footsteps")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClipsHard;  // footstep clips for hard floors inside the house
    [SerializeField] AudioClip[] footstepClipsSoft;  // footstep clips for soft ground in the forest
    [SerializeField] float footstepInterval = 0.5f;  // time between footstep sounds
    [SerializeField] Vector2 footstepPitchRange = new Vector2(0.9f, 1.1f);
    [Range(0f, 1f)][SerializeField] float footstepVolume = 0.8f;

    Rigidbody rb;
    Animator animator;
    CatControls controls; // generated input actions asset

    Vector2 moveInput;
    bool isRunning;
    bool isGrounded;
    float jumpCooldownTimer;
    float interactBufferTimer; // counts down after E is pressed
    float senseBufferTimer;    // counts down after Q is pressed
    float footstepTimer;

    bool inHouse;   // set by HouseZone trigger, selects hard footstep clips
    bool inForest;  // set by ForestZone trigger, selects soft footstep clips

    void Awake()
    {
        controls = new CatControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Run.performed += _ => isRunning = true;
        controls.Player.Run.canceled += _ => isRunning = false;

        controls.Player.Jump.performed += _ => TryJump();

        controls.Player.Interact.performed += _ => interactBufferTimer = inputBufferTime; // buffer E press

        controls.Player.Sense.performed += _ =>
        {
            senseBufferTimer = inputBufferTime; // buffer Q press
            if (ScentClue.CurrentActiveTrail != null) PlaySniff(); // play sniff sound if trail active
        };

        controls.Enable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0f, 0.05f, 0f); // just above the feet
        }

        if (!sniffSource) sniffSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // count down input buffer timers each frame
        if (interactBufferTimer > 0f) interactBufferTimer -= Time.deltaTime;
        if (senseBufferTimer > 0f) senseBufferTimer -= Time.deltaTime;

        UpdateGrounded();
        HandleMovement();
        UpdateTimers();
    }

    public bool ConsumeInteractPressed()
    {
        if (interactBufferTimer <= 0f) return false;
        interactBufferTimer = 0f; // consume the input so it cant fire twice
        return true;
    }

    public bool ConsumeSensePressed()
    {
        if (senseBufferTimer <= 0f) return false;
        senseBufferTimer = 0f;
        return true;
    }

    public void FreezeMovement()
    {
        moveInput = Vector2.zero;
        isRunning = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;    // stop all movement
            rb.angularVelocity = Vector3.zero;
        }
        controls.Player.Disable(); // block all player input
    }

    public void UnfreezeMovement()
    {
        controls.Player.Enable(); // restore player input
    }

    public void SetInHouse(bool value) { inHouse = value; }    // called by HouseZone
    public void SetInForest(bool value) { inForest = value; }  // called by ForestZone

    void HandleMovement()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        bool hasInput = input.sqrMagnitude > 0.01f;

        Vector3 moveDir = (transform.forward * input.z + transform.right * input.x).normalized
                        * (isRunning ? runSpeed : walkSpeed);

        // apply horizontal velocity while preserving vertical (gravity)
        Vector3 vel = rb.linearVelocity;
        vel.x = moveDir.x;
        vel.z = moveDir.z;
        rb.linearVelocity = vel;

        if (hasInput && moveDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), rotationSpeed * Time.deltaTime);

        animator.SetFloat("Speed", new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude);

        if (moveInput.y > 0.1f && isGrounded)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayFootstep();
                footstepTimer = isRunning ? footstepInterval * 0.6f : footstepInterval; // faster interval when running
            }
        }
        else if (moveInput.y <= 0.1f)
        {
            footstepTimer = footstepInterval; // reset timer when not moving forward
        }
    }

    void TryJump()
    {
        if (!isGrounded || jumpCooldownTimer > 0f) return;

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // apply upward jump force
        jumpCooldownTimer = jumpCooldown;
        animator.SetTrigger("Stretch"); // play jump animation
    }

    void UpdateGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, ~0); // check if on ground
        animator.SetBool("Grounded", isGrounded);
    }

    void UpdateTimers()
    {
        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
    }

    void PlayFootstep()
    {
        AudioClip[] clips = null;

        if (inHouse) clips = footstepClipsHard;       // hard floor inside
        else if (inForest) clips = footstepClipsSoft; // soft ground outside
        else return;                                   // no footstep zone, skip

        if (footstepSource == null || clips == null || clips.Length == 0) return;
        footstepSource.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y); // randomise pitch slightly
        footstepSource.PlayOneShot(clips[Random.Range(0, clips.Length)], footstepVolume); // random clip
    }

    void PlaySniff()
    {
        if (!sniffSource || !sniffClip) return;
        sniffSource.pitch = Random.Range(sniffPitchRange.x, sniffPitchRange.y);
        sniffSource.PlayOneShot(sniffClip, sniffVolume);
    }

    void OnDestroy() => controls.Disable(); // clean up input when destroyed
}