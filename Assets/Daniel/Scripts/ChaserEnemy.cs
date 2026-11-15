using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DeathTrigger))]
public class ChaserEnemy : MonoBehaviour
{
    [Header("Chaser Settings")]
    public Transform playerTarget;
    public float moveSpeed = 4f;

    // Eliminamos la variable pública gamePerspective para desacoplar el código
    private NavMeshAgent agent;
    private Vector3 initialPosition;
    private bool isIn2D = true; // Control local de la perspectiva

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    private void OnEnable()
    {
        // 1. Nos suscribimos al grito global de cambio de perspectiva
        PerspectiveController.OnPerspectiveChanged += HandlePerspectiveChange;

        // 2. Usamos el nuevo Getter seguro que acabas de crear para saber el estado inicial
        PerspectiveController pc = Object.FindFirstObjectByType<PerspectiveController>();
        if (pc != null)
        {
            isIn2D = pc.IsIn2DView;
        }
    }

    private void OnDisable()
    {
        PerspectiveController.OnPerspectiveChanged -= HandlePerspectiveChange;
    }

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void HandlePerspectiveChange(bool is2DView)
    {
        isIn2D = is2DView;
    }

    public void ResetChaser()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.Warp(initialPosition);
        }
        else
        {
            transform.position = initialPosition;
        }
    }

    public void UpdateResetPosition()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        // 1. Verificamos si el Menú Principal está abierto
        bool isMainMenuActive = UIManager.Instance != null &&
                                UIManager.Instance.mainMenuPanel != null &&
                                UIManager.Instance.mainMenuPanel.activeSelf;

        // 2. NUEVO: Verificamos si hay un diálogo activo (El Cubo o el Portal Final)
        bool isDialogueActive = DialogueManager.Instance != null &&
                                DialogueManager.Instance.IsDialogueActive;

        // ESTADO DE ESPERA ABSOLUTA (Si el jugador muere, está en el menú, o está leyendo un diálogo)
        if (playerTarget == null || !playerTarget.gameObject.activeInHierarchy || isMainMenuActive || isDialogueActive)
        {
            StopAgent();
            return;
        }

        // ESTADO DE JUEGO
        if (!isIn2D)
        {
            // En 3D: Perseguir ferozmente
            if (agent.isOnNavMesh && agent.isStopped)
            {
                agent.isStopped = false;
            }
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            // En 2D: Congelarse como estatua
            StopAgent();
        }
    }

    // Función auxiliar para detener al agente de forma limpia y evitar bugs de físicas
    private void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }
}
