using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Va en el jugador (slime). Mantener K lanza manzanas como vomito.
// Empieza BLOQUEADO: se desbloquea con UnlockLaunch() (lo llama TriggerChaser).
[RequireComponent(typeof(Rigidbody))]
public class AppleLauncher : MonoBehaviour
{
    [Header("Proyectil")]
    [Tooltip("El PREFAB (desde la carpeta Project) de la manzana lanzada")]
    public GameObject applePrefab;
    [Tooltip("Hijo del jugador desde donde salen las manzanas (la 'boca')")]
    public Transform launchPoint;
    public float launchSpeed = 9f;
    public float upwardBoost = 3f;
    public float upwardSpread = 1.5f;
    [Tooltip("Segundos entre una manzana y otra al mantener K")]
    public float fireRate = 0.12f;

    [Header("Punteria (vista 3D)")]
    [Tooltip("En 3D apunta solo al perseguidor mas cercano (con ventaja por su movimiento)")]
    public bool aimAtChaser = true;
    public float autoAimRange = 14f;
    [Tooltip("Margen aleatorio de altura al apuntar (da sensacion de vomito sin fallar)")]
    public float aimSpread = 0.4f;

    [Header("Estado")]
    [Tooltip("DEJAR DESMARCADO: se desbloquea al pisar el trigger del Chaser")]
    public bool unlockedAtStart = false;
    [Tooltip("Texto UI tipo 'Presiona K'. Se muestra al desbloquear y se oculta al primer lanzamiento")]
    public GameObject launchPrompt;

    [Header("Debug")]
    public bool debugLogs = true;

    private Rigidbody rb;
    private bool isUnlocked;
    private bool hasLaunched = false;
    private bool isIn2D = true;
    private Vector3 lastDir = Vector3.forward; // +Z es "derecha" en la vista 2D
    private float nextFireTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        isUnlocked = unlockedAtStart;
        if (launchPrompt != null) launchPrompt.SetActive(false);
    }

    private void OnEnable()
    {
        PerspectiveController.OnPerspectiveChanged += HandlePerspectiveChange;

        PerspectiveController pc = Object.FindFirstObjectByType<PerspectiveController>();
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

        // El aviso solo se muestra si todavia no ha aprendido a lanzar
        if (launchPrompt != null && !hasLaunched) launchPrompt.SetActive(true);
    }

    // Para reiniciar la partida desde el menu
    public void ResetState()
    {
        isUnlocked = unlockedAtStart;
        hasLaunched = false;
        if (launchPrompt != null) launchPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!isUnlocked) return;
        if (Time.timeScale == 0f) return; // pausa o dialogo

        // Recordamos hacia donde mira el slime segun su movimiento
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        if (v.sqrMagnitude > 0.1f) lastDir = v.normalized;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.kKey.isPressed && Time.time >= nextFireTime)
        {
            TryLaunch();
        }
    }

    private Vector3 GetLaunchDirection()
    {
        // En 2D el movimiento es solo sobre el eje Z: lanzamos a izquierda o derecha
        if (isIn2D) return lastDir.z >= 0f ? Vector3.forward : Vector3.back;
        return lastDir;
    }

    // En 3D: velocidad exacta para que la manzana (con gravedad) llegue al perseguidor
    private bool TryGetAimVelocity(Vector3 origin, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (isIn2D || !aimAtChaser) return false;

        ChaserEnemy best = null;
        float bestDist = autoAimRange;
        foreach (ChaserEnemy c in Object.FindObjectsByType<ChaserEnemy>(FindObjectsSortMode.None))
        {
            if (!c.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(origin, c.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        if (best == null) return false;

        // Centro aproximado del perseguidor
        Collider col = best.GetComponentInChildren<Collider>();
        Vector3 targetPos = col != null ? col.bounds.center : best.transform.position;

        // Ventaja: apuntamos a donde ESTARA cuando llegue la manzana
        Vector3 targetVel = Vector3.zero;
        NavMeshAgent agent = best.GetComponent<NavMeshAgent>();
        if (agent != null) targetVel = agent.velocity;
        targetVel.y = 0f;

        Vector3 predicted = targetPos;
        for (int i = 0; i < 2; i++)
        {
            Vector3 f = predicted - origin;
            f.y = 0f;
            float t = f.magnitude / launchSpeed;
            predicted = targetPos + targetVel * t;
        }

        Vector3 flat = predicted - origin;
        float dy = flat.y;
        flat.y = 0f;
        float dist = flat.magnitude;
        if (dist < 0.5f) return false;

        float time = dist / launchSpeed;
        float g = Mathf.Abs(Physics.gravity.y);
        float vy = dy / time + 0.5f * g * time + Random.Range(-aimSpread, aimSpread);

        velocity = flat.normalized * launchSpeed + Vector3.up * vy;
        return true;
    }

    private void TryLaunch()
    {
        if (applePrefab == null)
        {
            Debug.LogWarning("AppleLauncher: falta asignar Apple Prefab.");
            return;
        }
        if (AppleInventory.Instance == null || !AppleInventory.Instance.TryConsume()) return;

        nextFireTime = Time.time + fireRate;

        Vector3 dir = GetLaunchDirection();
        Vector3 origin = launchPoint != null
            ? launchPoint.position
            : transform.position + Vector3.up * 0.5f + dir * 0.5f;

        // Se conserva la rotacion del prefab (si no, un Quad puede quedar de canto frente a la camara)
        GameObject apple = Instantiate(applePrefab, origin, applePrefab.transform.rotation);

        AppleProjectile projectile = apple.GetComponent<AppleProjectile>();
        if (projectile == null) projectile = apple.AddComponent<AppleProjectile>();

        Vector3 velocity;
        bool aimed = TryGetAimVelocity(origin, out velocity);
        if (!aimed)
        {
            float up = upwardBoost + Random.Range(-upwardSpread, upwardSpread);
            velocity = dir * launchSpeed + Vector3.up * up;
        }

        if (debugLogs)
            Debug.Log($"[AppleLauncher] disparo en {(isIn2D ? "2D" : "3D")} | apuntando al chaser={aimed} | origen={origin} | velocidad={velocity}");

        // Se pasan los colliders del slime para que la manzana no choque con el
        projectile.Launch(velocity, GetComponentsInChildren<Collider>());

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySystemSFX(SoundType.AppleLaunch);

        // Ya aprendio la mecanica: ocultamos el aviso
        hasLaunched = true;
        if (launchPrompt != null && launchPrompt.activeSelf) launchPrompt.SetActive(false);
    }
}
