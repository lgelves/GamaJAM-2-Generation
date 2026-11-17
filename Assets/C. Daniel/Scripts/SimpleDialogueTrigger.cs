using UnityEngine;

public class SimpleDialogueTrigger : MonoBehaviour
{
    [Header("Configuraci�n del Di�logo")]
    [Tooltip("Si est� marcado, el di�logo solo saltar� la primera vez que el jugador toque esta zona.")]
    public bool triggerOnlyOnce = true;

    [Tooltip("Escribe aqu� las l�neas de di�logo que quieres mostrar.")]
    public DialogueLine[] dialogueLines;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Si ya se activ� y queremos que sea de un solo uso, lo ignoramos
        if (triggerOnlyOnce && hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (DialogueManager.Instance != null)
            {
                // Llamamos al di�logo de forma simple, sin callback final
                DialogueManager.Instance.StartDialogue(dialogueLines);
            }
            else
            {
                Debug.LogWarning("No se encontr� el DialogueManager en la escena.");
            }
        }
    }

    // Opcional: �til por si quieres que el di�logo vuelva a estar disponible al reiniciar nivel
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}