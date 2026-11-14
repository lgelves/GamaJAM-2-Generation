using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DeathTrigger))]
public class ChaserEnemy : MonoBehaviour
{
    [Header("Chaser Settings")]
    public Transform playerTarget;
    public float moveSpeed = 4f;

    [Header("Perspective Control")]
    public PerspectiveController gamePerspective;

    private NavMeshAgent agent;
    private Vector3 initialPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    private void Start()
    {
        // Guardamos el punto de origen al iniciar el juego
        initialPosition = transform.position;
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
        // Actualizamos la posición del enemigo al tocar un Checkpoint
        initialPosition = transform.position;
    }

    private void Update()
    {
        // NUEVO: Verificamos si el panel del menú principal está encendido
        bool isMainMenuActive = UIManager.Instance != null &&
                                UIManager.Instance.mainMenuPanel != null &&
                                UIManager.Instance.mainMenuPanel.activeSelf;

        // ESTADO DE ESPERA: Si no hay jugador, el jugador está muerto, O el menú principal está abierto
        if (playerTarget == null || gamePerspective == null || !playerTarget.gameObject.activeInHierarchy || isMainMenuActive)
        {
            if (agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return; // Cortamos el código para ahorrar rendimiento
        }

        // ESTADO DE JUEGO
        if (gamePerspective.movement3DScript.enabled)
        {
            // En 3D: Perseguir
            if (agent.isOnNavMesh && agent.isStopped)
            {
                agent.isStopped = false;
            }
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            // En 2D: Congelar
            if (agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }
    }
}