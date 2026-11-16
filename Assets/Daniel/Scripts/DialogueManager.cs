using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

// 1. Definimos los personajes disponibles
public enum SpeakerType
{
    None,
    Player,
    Chaser,
    Tesseract // El cubo
}

[System.Serializable]
public struct DialogueLine
{
    public SpeakerType speaker;
    [TextArea(3, 5)] public string message;
}

// Estructura para conectar el Enum con el GameObject en el Inspector
[System.Serializable]
public struct SpeakerPortrait
{
    public SpeakerType speaker;
    public GameObject portraitObject;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject dialogueContainer; // El objeto contenedor principal
    public TextMeshProUGUI dialogueText;

    [Header("Portraits Configuration")]
    [Tooltip("Añade aquí a los personajes y arrastra su GameObject de imagen correspondiente")]
    public List<SpeakerPortrait> speakerPortraitsList = new List<SpeakerPortrait>();
    private Dictionary<SpeakerType, GameObject> portraitDictionary;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public InputAction continueAction;

    private Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
    private bool isTyping = false;
    private string currentFullLine = "";
    private System.Action onDialogueCompleteCallback;

    public bool IsDialogueActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 2. Inicializamos el diccionario de retratos
        portraitDictionary = new Dictionary<SpeakerType, GameObject>();
        foreach (var sp in speakerPortraitsList)
        {
            if (!portraitDictionary.ContainsKey(sp.speaker))
            {
                portraitDictionary.Add(sp.speaker, sp.portraitObject);
                // Aseguramos que todas las imágenes empiecen apagadas
                if (sp.portraitObject != null) sp.portraitObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        if (continueAction != null)
        {
            continueAction.Enable();
            continueAction.performed += OnContinuePressed;
        }
    }

    private void OnDisable()
    {
        if (continueAction != null) continueAction.Disable();
    }

    private void Start()
    {
        if (dialogueContainer != null) dialogueContainer.SetActive(false);
    }

    public void StartDialogue(DialogueLine[] lines, System.Action onComplete = null)
    {
        IsDialogueActive = true;
        onDialogueCompleteCallback = onComplete;
        linesQueue.Clear();

        foreach (DialogueLine line in lines)
        {
            linesQueue.Enqueue(line);
        }

        dialogueContainer.SetActive(true);
        Time.timeScale = 0f;

        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (linesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine nextLine = linesQueue.Dequeue();

        // 3. Apagamos todas las imágenes primero
        foreach (var kvp in portraitDictionary)
        {
            if (kvp.Value != null) kvp.Value.SetActive(false);
        }

        // 4. Encendemos solo la imagen del personaje que habla (si no es None)
        if (nextLine.speaker != SpeakerType.None && portraitDictionary.TryGetValue(nextLine.speaker, out GameObject portraitToActivate))
        {
            if (portraitToActivate != null) portraitToActivate.SetActive(true);
        }

        currentFullLine = nextLine.message;
        StartCoroutine(TypeSentence(currentFullLine));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;

            if (SoundManager.Instance != null && dialogueText.text.Length % 2 == 0)
                SoundManager.Instance.PlaySystemSFX(SoundType.UIHover);

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    private void OnContinuePressed(InputAction.CallbackContext context)
    {
        if (!IsDialogueActive) return;

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullLine;
            isTyping = false;
        }
        else
        {
            DisplayNextLine();
        }
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;
        dialogueContainer.SetActive(false);

        // Apagamos los retratos al cerrar el diálogo
        foreach (var kvp in portraitDictionary)
        {
            if (kvp.Value != null) kvp.Value.SetActive(false);
        }

        Time.timeScale = 1f;

        onDialogueCompleteCallback?.Invoke();
    }
}