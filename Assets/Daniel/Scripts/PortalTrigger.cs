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
        // Usamos las funciones que acabamos de crear en tu UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowCredits();
        }

        // Ocultamos al jugador para que no quede estorbando en la pantalla
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.SetActive(false);
    }
}