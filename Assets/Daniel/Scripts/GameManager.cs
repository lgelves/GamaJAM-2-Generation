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

    private Vector3 initialSpawnPosition;
    private bool isInvincible = false;

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
        // EL ESCUDO: Si es invencible O ya está apagado/muerto, ignoramos por completo la petición
        if (isInvincible || !player.activeInHierarchy) return;

        player.SetActive(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDeathScreen();
        }
    }

    public void RespawnPlayer()
    {
        if (player != null)
        {
            ResetAllEnemies();

            player.transform.position = currentRespawnPosition;
            player.SetActive(true);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            StartCoroutine(InvincibilityTimer());
        }
    }

    public void FullGameReset()
    {
        if (player != null)
        {
            ResetAllEnemies();

            currentRespawnPosition = initialSpawnPosition;
            player.transform.position = initialSpawnPosition;
            player.SetActive(true);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

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