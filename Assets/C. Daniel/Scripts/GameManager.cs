using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Reference")]
    public GameObject player;

    [Header("Respawn Settings")]
    public Vector3 currentRespawnPosition;
    [Tooltip("Tiempo de invulnerabilidad al reaparecer (segundos)")]
    public float invincibilityTime = 1.5f;

    [Header("Death VFX")]
    [Tooltip("El prefab del sistema de partículas de explosión")]
    public ParticleSystem explosionPrefab;
    public float shrinkDuration = 0.3f;

    private Vector3 initialSpawnPosition;
    private Vector3 originalPlayerScale = Vector3.one; // Guardamos la escala original para el respawn
    private bool isInvincible = false;
    private bool isDying = false; // Evita que la secuencia de muerte se active dos veces seguidas

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (player != null)
        {
            initialSpawnPosition = player.transform.position;
            currentRespawnPosition = initialSpawnPosition;
            originalPlayerScale = player.transform.localScale; // Guardamos el tamaño inicial
        }
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        currentRespawnPosition = newPosition;
        Debug.Log("Player Checkpoint saved at: " + newPosition);

        // Actualizamos la posición del enemigo al tocar el checkpoint
        ChaserEnemy[] allChasers = Object.FindObjectsByType<ChaserEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ChaserEnemy chaser in allChasers)
        {
            chaser.UpdateResetPosition();
        }
    }

    public void TriggerPlayerDeath(GameObject deadPlayer)
    {
        // EL ESCUDO: Si es invencible, ya está muerto o la secuencia ya inició, ignoramos por completo
        if (isInvincible || !player.activeInHierarchy || isDying) return;

        isDying = true;
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // 1. Congelamos al jugador para que no caiga ni se mueva mientras se encoge
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        Vector3 startScale = player.transform.localScale;
        Vector3 targetScale = Vector3.zero;
        float elapsed = 0f;

        // FASE 1: Encogimiento (Implosión)
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            player.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / shrinkDuration);
            yield return null;
        }
        player.transform.localScale = targetScale;

        // FASE 2: Explosión (VFX)
        if (explosionPrefab != null)
        {
            ParticleSystem vfx = Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        // FASE 3: Sonido de Muerte
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerSFX(SoundType.PlayerHurt);
        }

        // FASE 4: UI de Muerte y apagado
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDeathScreen();
        }

        player.SetActive(false);
    }

    public void RespawnPlayer()
    {
        if (player != null)
        {
            ResetAllEnemies();

            player.transform.position = currentRespawnPosition;
            player.transform.localScale = originalPlayerScale; // REINICIAMOS EL TAMAÑO
            player.SetActive(true);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            isDying = false; // Liberamos el candado de muerte
            StartCoroutine(InvincibilityTimer());
        }
    }

    public void FullGameReset()
    {
        if (player != null)
        {
            ResetAllEnemies();

            // Forzar perspectiva 2D al reiniciar desde el menú
            PerspectiveController pc = Object.FindFirstObjectByType<PerspectiveController>();
            if (pc != null) pc.Force2DMode();

            currentRespawnPosition = initialSpawnPosition;
            player.transform.position = initialSpawnPosition;
            player.transform.localScale = originalPlayerScale; // REINICIAMOS EL TAMAÑO
            player.SetActive(true);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            isDying = false; // Liberamos el candado de muerte
            StartCoroutine(InvincibilityTimer());
        }
    }

    private void ResetAllEnemies()
    {
        ChaserEnemy[] allChasers = Object.FindObjectsByType<ChaserEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ChaserEnemy chaser in allChasers)
        {
            chaser.ResetChaser();
        }
    }

    private IEnumerator InvincibilityTimer()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }
}