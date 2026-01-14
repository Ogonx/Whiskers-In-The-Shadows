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

    private Rigidbody rb;
    private Animator animator;
    private CatControls controls;
    private Vector2 moveInput;
    private bool isRunning;
    private bool isGrounded;
    private float jumpCooldownTimer;

    void Awake()
    {
        controls = new CatControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Run.performed += ctx => isRunning = true;
        controls.Player.Run.canceled += ctx => isRunning = false;

        controls.Player.Jump.performed += ctx => TryJump();

        controls.Enable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // auto-create groundCheck if you forgot to assign one
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0f, 0.05f, 0f);
        }
    }

    void Update()
    {
        UpdateGrounded();
        HandleMovement();
        UpdateTimers();
    }

    // ----------------- MOVEMENT -----------------

    private void HandleMovement()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        bool hasInput = input.sqrMagnitude > 0.01f;

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        // direction relative to cat facing
        Vector3 moveDir =
            (transform.forward * input.z +
             transform.right * input.x).normalized * targetSpeed;

        // apply horizontal velocity (no sliding)
        Vector3 vel = rb.linearVelocity;
        vel.x = moveDir.x;
        vel.z = moveDir.z;
        rb.linearVelocity = vel;

        // rotate towards movement direction
        if (hasInput && moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime);
        }

        // animation speed based on horizontal velocity
        float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        animator.SetFloat("Speed", horizontalSpeed);   // use this in your blend tree
    }

    // ----------------- JUMP -----------------

    private void TryJump()
    {
        if (!isGrounded) return;                 // can only jump on ground
        if (jumpCooldownTimer > 0f) return;      // cooldown not finished

        // reset vertical velocity so jumps are consistent
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpCooldownTimer = jumpCooldown;

        animator.SetTrigger("Stretch");
    }

    // ----------------- GROUND CHECK -----------------

    private void UpdateGrounded()
    {
        // Check against ALL layers for now (so floor is definitely detected)
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            ~0);   // <--- this is the important change

        animator.SetBool("Grounded", isGrounded);
    }


    // ----------------- TIMERS -----------------

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
