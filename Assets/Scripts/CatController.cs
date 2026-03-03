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
    public float inputBufferTime = 0.2f;

    [Header("Sniff Audio")]
    [SerializeField] AudioSource sniffSource;
    [SerializeField] AudioClip sniffClip;
    [Range(0f, 1f)][SerializeField] float sniffVolume = 0.7f;
    [SerializeField] Vector2 sniffPitchRange = new Vector2(0.92f, 1.05f);

    [Header("Footsteps")]
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip[] footstepClipsHard;
    [SerializeField] AudioClip[] footstepClipsSoft;
    [SerializeField] float footstepInterval = 0.5f;
    [SerializeField] Vector2 footstepPitchRange = new Vector2(0.9f, 1.1f);
    [Range(0f, 1f)][SerializeField] float footstepVolume = 0.8f;

    Rigidbody rb;
    Animator animator;
    CatControls controls;

    Vector2 moveInput;
    bool isRunning;
    bool isGrounded;
    float jumpCooldownTimer;
    float interactBufferTimer;
    float senseBufferTimer;
    float footstepTimer;

    bool inHouse;
    bool inForest;

    void Awake()
    {
        controls = new CatControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Run.performed += _ => isRunning = true;
        controls.Player.Run.canceled += _ => isRunning = false;

        controls.Player.Jump.performed += _ => TryJump();

        controls.Player.Interact.performed += _ => interactBufferTimer = inputBufferTime;

        controls.Player.Sense.performed += _ =>
        {
            senseBufferTimer = inputBufferTime;
            if (ScentClue.CurrentActiveTrail != null) PlaySniff();
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
            groundCheck.localPosition = new Vector3(0f, 0.05f, 0f);
        }

        if (!sniffSource) sniffSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (interactBufferTimer > 0f) interactBufferTimer -= Time.deltaTime;
        if (senseBufferTimer > 0f) senseBufferTimer -= Time.deltaTime;

        UpdateGrounded();
        HandleMovement();
        UpdateTimers();
    }

    public bool ConsumeInteractPressed()
    {
        if (interactBufferTimer <= 0f) return false;
        interactBufferTimer = 0f;
        return true;
    }

    public bool ConsumeSensePressed()
    {
        if (senseBufferTimer <= 0f) return false;
        senseBufferTimer = 0f;
        return true;
    }

    public void SetInHouse(bool value) { inHouse = value; }
    public void SetInForest(bool value) { inForest = value; }

    void HandleMovement()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        bool hasInput = input.sqrMagnitude > 0.01f;

        Vector3 moveDir = (transform.forward * input.z + transform.right * input.x).normalized
                        * (isRunning ? runSpeed : walkSpeed);

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
                footstepTimer = isRunning ? footstepInterval * 0.6f : footstepInterval;
            }
        }
        else if (moveInput.y <= 0.1f)
        {
            footstepTimer = footstepInterval;
        }
    }

    void TryJump()
    {
        if (!isGrounded || jumpCooldownTimer > 0f) return;

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpCooldownTimer = jumpCooldown;
        animator.SetTrigger("Stretch");
    }

    void UpdateGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, ~0);
        animator.SetBool("Grounded", isGrounded);
    }

    void UpdateTimers()
    {
        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
    }

    void PlayFootstep()
    {
        AudioClip[] clips = null;

        if (inHouse) clips = footstepClipsHard;
        else if (inForest) clips = footstepClipsSoft;
        else return;

        if (footstepSource == null || clips == null || clips.Length == 0) return;
        footstepSource.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y);
        footstepSource.PlayOneShot(clips[Random.Range(0, clips.Length)], footstepVolume);
    }

    void PlaySniff()
    {
        if (!sniffSource || !sniffClip) return;
        sniffSource.pitch = Random.Range(sniffPitchRange.x, sniffPitchRange.y);
        sniffSource.PlayOneShot(sniffClip, sniffVolume);
    }

    void OnDestroy() => controls.Disable();
}