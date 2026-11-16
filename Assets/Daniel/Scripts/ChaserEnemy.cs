using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DeathTrigger))]
public class ChaserEnemy : MonoBehaviour
{
    [Header("Chaser Settings")]
    public Transform playerTarget;
    public float moveSpeed = 4f;

    private NavMeshAgent agent;
    private Vector3 initialPosition;
    private bool initialActiveState; // Recuerda si empezó apagado o prendido
    private bool isIn2D = true;

    private bool isInitialized = false;

    private void Awake()
    {
        InitializeSetup();
    }

    private void InitializeSetup()
    {
        if (isInitialized) return;

        // Guardamos su posición y estado ANTES de que el GameManager lo mueva accidentalmente
        initialPosition = transform.position;
        initialActiveState = gameObject.activeSelf;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = moveSpeed;

        isInitialized = true;
    }

    private void OnEnable()
    {
        PerspectiveController.OnPerspectiveChanged += HandlePerspectiveChange;

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
        // Ya no usamos Start para esto. Todo se asegura en InitializeSetup.
    }

    private void HandlePerspectiveChange(bool is2DView)
    {
        isIn2D = is2DView;
    }

    public void ResetChaser()
    {
        // 1. Nos aseguramos de que haya guardado su posición original por si estaba apagado
        InitializeSetup();

        // 2. Lo movemos a su posición de origen
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.Warp(initialPosition);
        }
        else
        {
            transform.position = initialPosition;
        }

        // 3. NUEVO: Si el Chaser empezó oculto para el Trigger, lo volvemos a ocultar 
        // para que el jugador pueda volver a activar el Trigger al morir.
        if (gameObject.activeSelf != initialActiveState)
        {
            gameObject.SetActive(initialActiveState);
        }
    }

    public void UpdateResetPosition()
    {
        initialPosition = transform.position;
        // Si el jugador tocó un checkpoint, asumimos que el estado base del chaser ahora es estar activo
        initialActiveState = true;
    }

    private void Update()
    {
        bool isMainMenuActive = UIManager.Instance != null &&
                                UIManager.Instance.mainMenuPanel != null &&
                                UIManager.Instance.mainMenuPanel.activeSelf;

        bool isDialogueActive = DialogueManager.Instance != null &&
                                DialogueManager.Instance.IsDialogueActive;

        if (playerTarget == null || !playerTarget.gameObject.activeInHierarchy || isMainMenuActive || isDialogueActive)
        {
            StopAgent();
            return;
        }

        if (!isIn2D)
        {
            if (agent.isOnNavMesh && agent.isStopped)
            {
                agent.isStopped = false;
            }
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            StopAgent();
        }
    }

    private void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }
}