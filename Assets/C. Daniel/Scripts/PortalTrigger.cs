using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    [Header("Diálogo Final")]
    public DialogueLine[] portalDialogue;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            DialogueManager.Instance.StartDialogue(portalDialogue, TriggerWinSequence);
        }
    }

    private void TriggerWinSequence()
    {
        // Llamamos a la secuencia completa de victoria
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWinCredits();
        }

        // (Opcional) Desaparecemos el portal después de usarlo
        gameObject.SetActive(false);
    }
}