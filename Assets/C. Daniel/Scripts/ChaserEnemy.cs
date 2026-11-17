using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Necesario para las corrutinas

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DeathTrigger))]
public class ChaserEnemy : MonoBehaviour, IAppleTarget // <-- AÑADIDO: IAppleTarget
{
    [Header("Chaser Settings")]
    public Transform playerTarget;
    public float moveSpeed = 4f;

    [Header("Stun Settings")]
    [Tooltip("Segundos que se queda quieto al recibir una manzana")]
    public float stunDuration = 2f;
    private bool isStunned = false; // <-- AÑADIDO: Control de aturdimiento

    private NavMeshAgent agent;
    private Vector3 initialPosition;
    private bool initialActiveState;
    private bool isIn2D = true;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeSetup();
    }

    private void InitializeSetup()
    {
        if (isInitialized) return;

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

    public void ResetChaser()
    {
        InitializeSetup();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.Warp(initialPosition);
        }
        else
        {
            transform.position = initialPosition;
        }

        if (gameObject.activeSelf != initialActiveState)
        {
            gameObject.SetActive(initialActiveState);
        }

        isStunned = false; // <-- AÑADIDO: Quitar aturdimiento al resetear
    }

    public void UpdateResetPosition()
    {
        initialPosition = transform.position;
        initialActiveState = true;
    }

    private void Update()
    {
        // <-- AÑADIDO: Si está aturdido, abortamos el Update y no se mueve
        if (isStunned) return;

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

    // --- AÑADIDO: Lógica de la manzana (IAppleTarget) ---
    public void OnAppleHit()
    {
        if (!isStunned && gameObject.activeInHierarchy)
        {
            StartCoroutine(StunCoroutine());
        }
    }

    private IEnumerator StunCoroutine()
    {
        isStunned = true;
        StopAgent(); // Frena en seco

        // (Opcional) Si quieres que se ponga rojo, podrías cambiar el color de su material aquí

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
    }
}