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

// Voz de cada personaje: su sonido, volumen, tono y cada cuantas letras suena
[System.Serializable]
public class SpeakerVoice
{
    public SpeakerType speaker;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;          // Agudo (>1) o grave (<1)
    [Range(0f, 0.5f)] public float pitchVariation = 0.05f; // Variacion aleatoria para que no suene robotico
    [Min(1)] public int letterInterval = 2;             // Suena cada N letras
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject dialogueContainer; // El objeto contenedor principal
    public TextMeshProUGUI dialogueText;

    [Header("Portraits Configuration")]
    [Tooltip("A�ade aqu� a los personajes y arrastra su GameObject de imagen correspondiente")]
    public List<SpeakerPortrait> speakerPortraitsList = new List<SpeakerPortrait>();
    private Dictionary<SpeakerType, GameObject> portraitDictionary;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public InputAction continueAction;

    [Header("Voces por personaje")]
    [Tooltip("AudioSource propio del dialogo. Su Output debe ser el grupo SFX del Mixer")]
    public AudioSource voiceSource;
    public List<SpeakerVoice> speakerVoices = new List<SpeakerVoice>();
    private Dictionary<SpeakerType, SpeakerVoice> voiceDictionary;
    private SpeakerType currentSpeaker = SpeakerType.None;

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

        // Diccionario de voces
        voiceDictionary = new Dictionary<SpeakerType, SpeakerVoice>();
        foreach (var v in speakerVoices)
        {
            if (v != null && !voiceDictionary.ContainsKey(v.speaker))
                voiceDictionary.Add(v.speaker, v);
        }

        // 2. Inicializamos el diccionario de retratos
        portraitDictionary = new Dictionary<SpeakerType, GameObject>();
        foreach (var sp in speakerPortraitsList)
        {
            if (!portraitDictionary.ContainsKey(sp.speaker))
            {
                portraitDictionary.Add(sp.speaker, sp.portraitObject);
                // Aseguramos que todas las im�genes empiecen apagadas
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
        currentSpeaker = nextLine.speaker;

        // 3. Apagamos todas las im�genes primero
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

        // Buscamos la voz del personaje que habla (null si no tiene)
        voiceDictionary.TryGetValue(currentSpeaker, out SpeakerVoice voice);
        int letterCount = 0;

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            letterCount++;

            if (voice != null && voice.clip != null && voiceSource != null
                && !char.IsWhiteSpace(letter)
                && letterCount % voice.letterInterval == 0)
            {
                voiceSource.pitch = voice.pitch + Random.Range(-voice.pitchVariation, voice.pitchVariation);
                voiceSource.PlayOneShot(voice.clip, voice.volume);
            }

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

        // Apagamos los retratos al cerrar el di�logo
        foreach (var kvp in portraitDictionary)
        {
            if (kvp.Value != null) kvp.Value.SetActive(false);
        }

        Time.timeScale = 1f;

        onDialogueCompleteCallback?.Invoke();
    }
}