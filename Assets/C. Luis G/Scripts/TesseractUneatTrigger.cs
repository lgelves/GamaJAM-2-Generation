using UnityEngine;

// El Tesseracto le dice al jugador que devuelva las manzanas.
// Al terminar el dialogo se desbloquea el lanzamiento (tecla K) y aparece el aviso.
// Usalo en OTRO tesseracto/zona distinta a la de TesseractTrigger, o los dos
// abriran dialogo al mismo tiempo.
public class TesseractUneatTrigger : MonoBehaviour
{
    [Header("Dialogo")]
    public DialogueLine[] uneatDialogue;

    [Header("Condicion")]
    [Tooltip("Manzanas minimas para que el Tesseracto hable (0 = siempre)")]
    public int minApples = 1;

    private bool hasTriggered = false;
    private AppleLauncher launcher;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;

        if (minApples > 0 && (AppleInventory.Instance == null || AppleInventory.Instance.Count < minApples))
            return; // todavia no trae manzanas: puede volver mas tarde

        hasTriggered = true;

        launcher = other.GetComponentInParent<AppleLauncher>();
        if (launcher == null) launcher = Object.FindFirstObjectByType<AppleLauncher>();

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(uneatDialogue, UnlockLauncher);
        else
            UnlockLauncher();
    }

    private void UnlockLauncher()
    {
        if (launcher != null) launcher.UnlockLaunch();
        else Debug.LogWarning("No se encontro AppleLauncher en el jugador.");
    }

    // Opcional: por si quieres que el Tesseracto vuelva a hablar tras reiniciar
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
