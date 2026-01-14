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

    [Header("Input Buffer (fixes spamming)")]
    public float inputBufferTime = 0.2f; // 0.15–0.25 feels good

    private Rigidbody rb;
    private Animator animator;
    private CatControls controls;

    private Vector2 moveInput;
    private bool isRunning;
    private bool isGrounded;
    private float jumpCooldownTimer;

    // buffered input timers
    private float interactBufferTimer;
    private float senseBufferTimer;

    void Awake()
    {
        controls = new CatControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Run.performed += _ => isRunning = true;
        controls.Player.Run.canceled += _ => isRunning = false;

        controls.Player.Jump.performed += _ => TryJump();

        // IMPORTANT: buffer these so you don't have to hit the exact frame
        controls.Player.Interact.performed += _ => interactBufferTimer = inputBufferTime;
        controls.Player.Sense.performed += _ => senseBufferTimer = inputBufferTime;

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
    }

    void Update()
    {
        // tick down buffers
        if (interactBufferTimer > 0f) interactBufferTimer -= Time.deltaTime;
        if (senseBufferTimer > 0f) senseBufferTimer -= Time.deltaTime;

        UpdateGrounded();
        HandleMovement();
        UpdateTimers();
    }

    // Call these from other scripts (ScentClue / Sense reveal)
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

    private void HandleMovement()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        bool hasInput = input.sqrMagnitude > 0.01f;

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 moveDir =
            (transform.forward * input.z +
             transform.right * input.x).normalized * targetSpeed;

        Vector3 vel = rb.linearVelocity;
        vel.x = moveDir.x;
        vel.z = moveDir.z;
        rb.linearVelocity = vel;

        if (hasInput && moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        animator.SetFloat("Speed", horizontalSpeed);
    }

    private void TryJump()
    {
        if (!isGrounded) return;
        if (jumpCooldownTimer > 0f) return;

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpCooldownTimer = jumpCooldown;

        animator.SetTrigger("Stretch");
    }

    private void UpdateGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, ~0);
        animator.SetBool("Grounded", isGrounded);
    }

    private void UpdateTimers()
    {
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;
    }

    void OnDestroy()
    {
        controls.Disable();
    }
}
