using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PerspectiveController : MonoBehaviour
{
    [Header("Cinemachine 3 Cameras")]
    public CinemachineCamera camIsometric;
    public CinemachineCamera cam2DLateral;

    [Header("Player Components")]
    public MonoBehaviour movement3DScript;
    public MonoBehaviour movement2DScript;

    [Header("Sprite Configuration")]
    [Tooltip("Assign the child GameObject that contains the SpriteRenderer")]
    public Transform spriteTransform;
    [Tooltip("Base local rotation of the sprite when in 3D mode (e.g., 0,0,0)")]
    public Vector3 rotationFor3D = Vector3.zero;
    [Tooltip("Base local rotation of the sprite when in 2D mode (e.g., 0,90,0)")]
    public Vector3 rotationFor2D = new Vector3(0f, 90f, 0f);

    [Header("Input Configuration")]
    [Tooltip("Assign the action here (e.g., a button) to change the camera")]
    public InputAction inputChangePerspective;

    private bool in2DView = false;

    private void OnEnable()
    {
        if (inputChangePerspective != null)
        {
            inputChangePerspective.Enable();
            inputChangePerspective.performed += ChangeView;
        }
    }

    private void OnDisable()
    {
        if (inputChangePerspective != null)
        {
            inputChangePerspective.performed -= ChangeView;
            inputChangePerspective.Disable();
        }
    }

    private void Start()
    {
        // Initial state: 3D active
        camIsometric.Priority = 10;
        cam2DLateral.Priority = 5;

        Activate3DMode();
    }

    private void ChangeView(InputAction.CallbackContext context)
    {
        in2DView = !in2DView;

        if (in2DView)
        {
            // Transition to 2D
            cam2DLateral.Priority = 10;
            camIsometric.Priority = 5;
            Activate2DMode();
        }
        else
        {
            // Transition to 3D
            camIsometric.Priority = 10;
            cam2DLateral.Priority = 5;
            Activate3DMode();
        }
    }

    private void Activate3DMode()
    {
        // 1. Swap Scripts
        if (movement3DScript != null) movement3DScript.enabled = true;
        if (movement2DScript != null) movement2DScript.enabled = false;

        // 2. Adjust Base Sprite Rotation for 3D
        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.Euler(rotationFor3D);
        }
    }

    private void Activate2DMode()
    {
        // 1. Swap Scripts
        if (movement3DScript != null) movement3DScript.enabled = false;
        if (movement2DScript != null) movement2DScript.enabled = true;

        // 2. Adjust Base Sprite Rotation for 2D
        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.Euler(rotationFor2D);
        }
    }
}