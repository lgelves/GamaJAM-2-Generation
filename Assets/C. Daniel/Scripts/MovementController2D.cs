using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Efectos Visuales")]
    public ParticleSystem dustParticles;
    public TrailRenderer slimeTrail;

    // Necesitamos recordar si en el frame anterior est�bamos en el aire para detectar el aterrizaje
    private bool wasGrounded = true;

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
    [Tooltip("Longitud del rayo. Debe ser un poco m�s larga que la mitad de la altura de tu personaje")]
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

        wantsToJump = false;
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        HandleSpriteFlip();

        // 1. Calculamos si estamos en el suelo en este frame
        CheckGroundedStatus();

        // 2. Comprobamos si acabamos de aterrizar
        if (!wasGrounded && isGrounded)
        {
            if (dustParticles != null) dustParticles.Play();

            // A�ADIDO: Sonido de aterrizaje
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlayerSFX(SoundType.PlayerLand);
            }
        }

        // 3. Guardamos el estado actual para compararlo en el siguiente frame
        // El rastro solo emite si estamos en el suelo y movi�ndonos
        if (slimeTrail != null)
        {
            slimeTrail.emitting = isGrounded;
        }
        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        Vector3 newVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, moveInput.x * moveSpeed);
        rb.linearVelocity = newVelocity;

        if (wantsToJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // A�ADIDO: Emitimos polvo justo en el instante que aplicamos la fuerza de salto
            if (dustParticles != null) dustParticles.Play();

            // A�ADIDO: Sonido de salto
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlayerSFX(SoundType.PlayerJump);
            }

            wantsToJump = false;
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
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength, groundLayer);
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
    private void OnTriggerEnter(Collider other) // Detectamos colisiones con coleccionables - Luis G
    {
        if (other.CompareTag("Player"))
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySystemSFX(SoundType.Collectible);
            gameObject.SetActive(false);
        }
    }
}