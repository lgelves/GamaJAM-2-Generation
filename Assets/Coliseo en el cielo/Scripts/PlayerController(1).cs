using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador en primera persona: WASD para moverse, Shift para correr,
/// Espacio para saltar, mouse para mirar. Esc libera el cursor.
/// Usa el nuevo Input System (Keyboard.current / Mouse.current).
/// Requiere un CharacterController en el mismo objeto.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float gravity = -20f;

    [Header("Cámara")]
    [SerializeField] Transform cameraPivot;          // la Main Camera (hija del jugador)
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float maxLookAngle = 85f;

    CharacterController controller;
    float verticalVelocity;
    float pitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        LockCursor(true);
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        // Cursor
        if (kb.escapeKey.wasPressedThisFrame) LockCursor(false);
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

        if (Cursor.lockState == CursorLockMode.Locked)
            Look(mouse);

        Move(kb);
    }

    void Look(Mouse mouse)
    {
        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;

        transform.Rotate(Vector3.up * delta.x);

        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move(Keyboard kb)
    {
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

        Vector3 dir = (transform.right * x + transform.forward * z);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        // Gravedad y salto
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (controller.isGrounded && kb.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = dir * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
