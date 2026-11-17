using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class AppleLauncher : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public GameObject applePrefab;
    [Tooltip("Punto desde donde salen las manzanas (la 'boca')")]
    public Transform launchPoint;
    public float launchSpeed = 10f;
    public float upwardBoost = 4f;
    public float fireRate = 0.15f;

    [Header("Puntería Automática (Vista 3D)")]
    public float autoAimRange = 15f;

    [Header("Estado de la Habilidad")]
    public bool unlockedAtStart = false;

    private Rigidbody rb;
    private bool isUnlocked;
    private bool isIn2D = true;
    private Vector3 lastDir = Vector3.forward;
    private float nextFireTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        isUnlocked = unlockedAtStart;
    }

    private void OnEnable()
    {
        PerspectiveController.OnPerspectiveChanged += HandlePerspectiveChange;

        // CORRECCIÓN: Se quitó el "Object." para evitar ambigüedad
        PerspectiveController pc = FindFirstObjectByType<PerspectiveController>();
        if (pc != null) isIn2D = pc.IsIn2DView;
    }

    private void OnDisable()
    {
        PerspectiveController.OnPerspectiveChanged -= HandlePerspectiveChange;
    }

    private void HandlePerspectiveChange(bool is2DView)
    {
        isIn2D = is2DView;
    }

    public void UnlockLaunch()
    {
        isUnlocked = true;
    }

    public void ResetState()
    {
        isUnlocked = unlockedAtStart;
    }

    private void Update()
    {
        if (!isUnlocked || Time.timeScale == 0f) return;

        // Guardamos hacia dónde miramos
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            lastDir = rb.linearVelocity;
            lastDir.y = 0;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.kKey.isPressed && Time.time >= nextFireTime)
        {
            TryLaunch();
        }
    }

    private void TryLaunch()
    {
        if (applePrefab == null) return;

        // --- NUEVO: CONDICIÓN DE INVENTARIO ---
        // Descuenta una manzana. Si TryConsume devuelve false (porque tienes 0), aborta el disparo.
        if (AppleInventory.Instance != null && !AppleInventory.Instance.TryConsume())
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

        Vector3 origin = launchPoint != null ? launchPoint.position : transform.position + Vector3.up * 0.5f;
        GameObject apple = Instantiate(applePrefab, origin, applePrefab.transform.rotation);

        Vector3 targetDirection = GetSimpleAimDirection(origin);
        Rigidbody appleRb = apple.GetComponent<Rigidbody>();
        if (appleRb != null)
        {
            // Velocidad recta + impulso hacia arriba = parábola perfecta
            appleRb.linearVelocity = (targetDirection.normalized * launchSpeed) + (Vector3.up * upwardBoost);
        }

        IgnorePlayerCollisions(apple);

        // if (SoundManager.Instance != null) SoundManager.Instance.PlaySystemSFX(SoundType.AppleLaunch);
    }

    private Vector3 GetSimpleAimDirection(Vector3 origin)
    {
        if (isIn2D) return lastDir.z >= 0f ? Vector3.forward : Vector3.back;

        ChaserEnemy closestChaser = null;
        float closestDist = autoAimRange;

        // CORRECCIÓN: Se quitó el "Object." para evitar ambigüedad
        ChaserEnemy[] allChasers = FindObjectsByType<ChaserEnemy>(FindObjectsSortMode.None);

        foreach (ChaserEnemy chaser in allChasers)
        {
            if (!chaser.gameObject.activeInHierarchy) continue;
            float dist = Vector3.Distance(origin, chaser.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestChaser = chaser;
            }
        }

        if (closestChaser != null)
        {
            Vector3 direction = closestChaser.transform.position - origin;
            direction.y = 0;
            return direction;
        }

        return lastDir;
    }

    private void IgnorePlayerCollisions(GameObject apple)
    {
        Collider appleCollider = apple.GetComponent<Collider>();
        Collider[] playerColliders = GetComponentsInChildren<Collider>();

        if (appleCollider != null)
        {
            foreach (Collider playerCol in playerColliders)
            {
                Physics.IgnoreCollision(appleCollider, playerCol);
            }
        }
    }
}