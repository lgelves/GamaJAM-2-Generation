using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Respawn Offset")]
    [Tooltip("Adds height so the player doesn't spawn stuck in the floor")]
    public float yOffset = 1f;

    private void Awake()
    {
        // Nos aseguramos de que sea un Trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Tomamos la posición del checkpoint y le sumamos la altura
            Vector3 safePosition = transform.position + new Vector3(0, yOffset, 0);

            // Le enviamos la posición al GameManager
            GameManager.Instance.SetCheckpoint(safePosition);
        }
    }
}

