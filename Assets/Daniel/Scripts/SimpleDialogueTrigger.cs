using UnityEngine;

public class SimpleDialogueTrigger : MonoBehaviour
{
    [Header("Configuración del Diálogo")]
    [Tooltip("Si está marcado, el diálogo solo saltará la primera vez que el jugador toque esta zona.")]
    public bool triggerOnlyOnce = true;

    [Tooltip("Escribe aquí las líneas de diálogo que quieres mostrar.")]
    public DialogueLine[] dialogueLines;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Si ya se activó y queremos que sea de un solo uso, lo ignoramos
        if (triggerOnlyOnce && hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (DialogueManager.Instance != null)
            {
                // Llamamos al diálogo de forma simple, sin callback final
                DialogueManager.Instance.StartDialogue(dialogueLines);
            }
            else
            {
                Debug.LogWarning("No se encontró el DialogueManager en la escena.");
            }
        }
    }

    // Opcional: Útil por si quieres que el diálogo vuelva a estar disponible al reiniciar nivel
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}