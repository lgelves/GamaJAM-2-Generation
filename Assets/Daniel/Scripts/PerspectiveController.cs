using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PerspectiveController : MonoBehaviour
{
    // --- CAMBIO AÑADIDO: Evento global al que otros se suscriben ---
    public static event System.Action<bool> OnPerspectiveChanged;

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

    private bool in2DView = true;

    // --- CAMBIO AÑADIDO: Getter público para que otros scripts pregunten en qué estado estamos, sin poder modificarlo ---
    public bool IsIn2DView => in2DView;

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
        cam2DLateral.Priority = 10;
        camIsometric.Priority = 5;

        Activate2DMode();
    }

    private void ChangeView(InputAction.CallbackContext context)
    {
        in2DView = !in2DView;

        // AÑADIDO: Hacemos temblar la cámara un poquito para vender el efecto de que la realidad cambia
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakePerspectiveChange();
        }

        if (in2DView)
        {
            cam2DLateral.Priority = 10;
            camIsometric.Priority = 5;
            Activate2DMode();
        }
        else
        {
            camIsometric.Priority = 10;
            cam2DLateral.Priority = 5;
            Activate3DMode();
        }
    }

    private void Activate3DMode()
    {
        if (movement3DScript != null) movement3DScript.enabled = true;
        if (movement2DScript != null) movement2DScript.enabled = false;

        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.Euler(rotationFor3D);
        }

        // --- CAMBIO AÑADIDO: Avisamos a todo el juego que pasamos a 3D (is2D = false) ---
        OnPerspectiveChanged?.Invoke(false);
    }

    private void Activate2DMode()
    {
        if (movement3DScript != null) movement3DScript.enabled = false;
        if (movement2DScript != null) movement2DScript.enabled = true;

        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.Euler(rotationFor2D);
        }

        // --- CAMBIO AÑADIDO: Avisamos a todo el juego que pasamos a 2D (is2D = true) ---
        OnPerspectiveChanged?.Invoke(true);
    }

    public void Force2DMode()
    {
        in2DView = true;
        cam2DLateral.Priority = 10;
        camIsometric.Priority = 5;
        Activate2DMode();
    }

    public void Force3DMode()
    {
        in2DView = false;
        camIsometric.Priority = 10;
        cam2DLateral.Priority = 5;
        Activate3DMode();
    }
}