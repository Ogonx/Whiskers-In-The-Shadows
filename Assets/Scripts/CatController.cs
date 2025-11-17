using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class CatController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private CatControls controls;
    private Vector2 moveInput = Vector2.zero;
    private bool isRunning = false;

    void Awake()
    {
        controls = new CatControls();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Run.performed += ctx => isRunning = ctx.ReadValueAsButton();
        controls.Player.Run.canceled += ctx => isRunning = false;
        controls.Player.Jump.performed += ctx => OnJump();
        controls.Player.Interact.performed += ctx => OnInteract();
        controls.Player.Crouch.performed += ctx => animator?.SetBool("Crouched", true);
        controls.Player.Crouch.canceled += ctx => animator?.SetBool("Crouched", false);
        controls.Enable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;

        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -0.35f, 0);
        }
    }

    void FixedUpdate()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        animator.SetBool("Grounded", isGrounded);

        // Movement input -> world space relative to Cat forward
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);

        // Flip if the model faces the wrong way:
        direction = transform.TransformDirection(direction);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 move = direction.normalized * currentSpeed * Time.fixedDeltaTime;

        // Move and rotate
        if (direction.sqrMagnitude > 0.0001f)
        {
            // MovePosition for smooth physics-movement
            rb.MovePosition(rb.position + move);

            // Rotate to movement direction
            Quaternion targetRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 10f));
        }
        else
        {
            // still call MovePosition even if zero to keep consistent physics updates
            rb.MovePosition(rb.position);
        }

        // Drive animations: Speed parameter should match Blend Tree thresholds
        // Use moveInput magnitude * speed so Walk=~1, Run=~3 (example)
        float speedForAnimator = moveInput.magnitude * (isRunning ? runSpeed : walkSpeed);
        animator.SetFloat("Speed", speedForAnimator, 0.1f, Time.fixedDeltaTime * 4f); // damped smoothing
    }

    void OnJump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger("Jump");
        }
    }

    void OnInteract()
    {
        animator.SetTrigger("Interact");
    }

    void OnDestroy()
    {
        controls.Disable();
    }
}
