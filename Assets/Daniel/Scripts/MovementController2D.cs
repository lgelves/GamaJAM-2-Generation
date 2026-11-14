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
    public InputAction moveAction;
    public InputAction jumpAction;

    private Vector2 moveInput;

    [Header("Ground Detection (Raycast)")]
    [Tooltip("La capa (Layer) que representa el suelo")]
    public LayerMask groundLayer;
    [Tooltip("Longitud del rayo. Debe ser un poco más larga que la mitad de la altura de tu personaje")]
    public float rayLength = 1.1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJumpPerformed;
        }

        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.identity;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);

        // FIX BUG 3: Limpiamos peticiones de salto viejas al encender
        wantsToJump = false;
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // FIX BUG 3: Evitamos que se quede un salto guardado al apagar
        wantsToJump = false;
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        HandleSpriteFlip();

        // FIX BUGS 1, 2 y 4: Raycast constante hacia abajo. 
        // No importa si el script se apaga, si hay múltiples baldosas o si tocamos una pared.
        CheckGroundedStatus();
    }

    private void FixedUpdate()
    {
        Vector3 newVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveInput.x * moveSpeed);
        rb.linearVelocity = newVelocity;

        if (wantsToJump)
        {
            // Reseteamos la velocidad en Y antes de saltar para evitar fuerzas acumuladas raras
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            wantsToJump = false;
            // No necesitamos forzar isGrounded a false, el Raycast lo detectará en el instante que subas
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            wantsToJump = true;
        }
    }

    private void CheckGroundedStatus()
    {
        // Dispara un rayo desde el centro del personaje hacia abajo.
        // Si el punto de anclaje de tu personaje está en los PIES en lugar del centro, cambia rayLength a un valor pequeño como 0.2f
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength, groundLayer);

        // (Opcional) Dibuja el rayo en la escena para que puedas calibrar la longitud 'rayLength'
        Debug.DrawRay(transform.position, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);
    }

    private void HandleSpriteFlip()
    {
        if (spriteTransform == null) return;

        if (moveInput.x > 0.01f)
        {
            spriteTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else if (moveInput.x < -0.01f)
        {
            spriteTransform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
    }
}
