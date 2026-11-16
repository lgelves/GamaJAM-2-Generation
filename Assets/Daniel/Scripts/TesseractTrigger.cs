using UnityEngine;

public class TesseractTrigger : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("Velocidad de rotación en los ejes X, Y, Z. Ej: (0, 50, 0) para rotar sobre sí mismo.")]
    public Vector3 rotationSpeed = new Vector3(15f, 50f, 15f);

    [Header("Configuración del Diálogo")]
    public DialogueLine[] tesseractDialogue;

    private bool hasTriggered = false;

    private void Update()
    {
        // Rota el objeto continuamente en cada frame
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Usamos hasTriggered para que el evento solo ocurra una vez
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            // Le pasamos el diálogo y la función que se ejecutará al terminar (el Callback)
            DialogueManager.Instance.StartDialogue(tesseractDialogue, BreakRealityTo3D);
        }
    }

    private void BreakRealityTo3D()
    {
        PerspectiveController pc = Object.FindFirstObjectByType<PerspectiveController>();
        if (pc != null)
        {
            pc.Force3DMode();
        }

        // 2. Aquí puedes añadir código para despertar al Chaser si estaba desactivado
        // ej: GameManager.Instance.WakeUpChaser();

        // 3. Desaparecemos el cubo
        gameObject.SetActive(false);
    }
}