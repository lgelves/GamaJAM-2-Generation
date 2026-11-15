using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MovementController3D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    [Tooltip("Multiplier to compensate for the isometric visual illusion on the Z axis (Depth)")]
    public float zSpeedMultiplier = 1.4f; // 1.4 a 1.5 suele ser el punto dulce para cámaras a 45 grados

    private Rigidbody rb;

    [Header("Sprite Visuals")]
    [Tooltip("Assign the child GameObject that contains the SpriteRenderer")]
    public Transform spriteTransform;
    [Tooltip("Rotation applied when moving right")]
    public Vector3 rightRotation = new Vector3(0f, 75f, 0f);
    [Tooltip("Rotation applied when moving left")]
    public Vector3 leftRotation = new Vector3(0f, -75f, 0f);

    [Header("Input Configuration")]
    [Tooltip("Create a Vector2 action (Up/Down/Left/Right) for WASD")]
    public InputAction moveAction;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }

    private void Update()
    {
        // 1. Read the input (Vector2)
        moveInput = moveAction.ReadValue<Vector2>();

        // 2. Update sprite rotation based on horizontal movement
        HandleSpriteRotation();
    }

    private void FixedUpdate()
    {
        // 3. Apply physics velocity with the Z multiplier
        Vector3 newVelocity = new Vector3(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y,
            moveInput.y * (moveSpeed * zSpeedMultiplier) // Aplicamos el parche visual aquí
        );

        rb.linearVelocity = newVelocity;
    }

    private void HandleSpriteRotation()
    {
        if (spriteTransform == null) return;

        if (moveInput.x > 0.01f)
        {
            spriteTransform.localRotation = Quaternion.Euler(rightRotation);
        }
        else if (moveInput.x < -0.01f)
        {
            spriteTransform.localRotation = Quaternion.Euler(leftRotation);
        }
    }
}