using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool wantsToJump = false;

    [Header("Sprite Visuals")]
    [Tooltip("Assign the child GameObject that contains the SpriteRenderer")]
    public Transform spriteTransform;

    [Header("Input Configuration")]
    [Tooltip("Use the SAME Vector2 action as 3D for horizontal movement")]
    public InputAction moveAction;
    [Tooltip("Create a new Button action for jumping (e.g., Spacebar)")]
    public InputAction jumpAction;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // 1. Activar Inputs
        if (moveAction != null) moveAction.Enable();

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJumpPerformed;
        }

        // 2. Lógica Visual: Resetear rotación del sprite
        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.identity;
        }

        // 3. Lógica de Físicas: Congelar X (Profundidad) y mantener rotaciones congeladas
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX;

        // 4. Detener cualquier velocidad residual en X para que no resbale al hacer el cambio
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
    }

    private void OnDisable()
    {
        // 1. Desactivar Inputs
        if (moveAction != null) moveAction.Disable();

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }

        // 2. Lógica de Físicas: Descongelar X al salir del modo 2D
        // (Volvemos a dejar solo las rotaciones congeladas para el 3D)
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        // 1. Read movement input (We only care about moveInput.x for left/right on WASD)
        moveInput = moveAction.ReadValue<Vector2>();

        // 2. Flip sprite based on direction
        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
        // 3. Apply horizontal movement
        // IMPORTANT: I set movement to the Z axis as requested. 
        // If your 2D perspective was actually aligned to X, just swap the 0f and the moveInput.x * moveSpeed
        Vector3 newVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveInput.x * moveSpeed);
        rb.linearVelocity = newVelocity;

        // 4. Handle Physics Jump
        if (wantsToJump)
        {
            // Apply upward force instantly
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            wantsToJump = false;
            isGrounded = false; // Instantly set to false to prevent double-jumping exploits
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        // When the player presses jump, check if they are allowed to jump
        if (isGrounded)
        {
            wantsToJump = true;
        }
    }

    private void HandleSpriteFlip()
    {
        if (spriteTransform == null) return;

        if (moveInput.x > 0.01f) // Moving forward/right
        {
            spriteTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else if (moveInput.x < -0.01f) // Moving backward/left
        {
            spriteTransform.localRotation = Quaternion.Euler(0f,90f, 0f);
        }
    }

    // --- GROUND DETECTION LOGIC ---

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the object we collided with has the "Ground" tag
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // If we walk off a ledge without jumping, we are no longer grounded
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
