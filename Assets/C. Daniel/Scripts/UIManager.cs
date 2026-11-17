using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Player Management")]
    [Tooltip("Arrastra aqu� al GameObject de tu Slime/Jugador")]
    public GameObject playerObject;

    [Header("Main Menu UI")]
    public GameObject mainMenuPanel; // El Canvas/Panel global del men�
    [Tooltip("C�mara que apunta a la zona bonita del nivel para el men�")]
    public CinemachineCamera mainMenuCamera;

    [Header("Sub-Men�s (Contenedores)")]
    public GameObject mainMenuButtonsContainer; // Contiene Play, Opciones, Cr�ditos, etc.
    public GameObject optionsContainer;         // Contiene los Sliders de audio
    public GameObject creditsContainer;         // Contiene el texto de cr�ditos

    [Header("Death Screen UI")]
    public GameObject deathScreenUI;
    public float fadeDuration = 1f;
    private Coroutine deathFadeCoroutine;

    [Header("Pause Menu UI")]
    public GameObject pauseMenuPanel;
    public InputAction pauseAction;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += ctx => TogglePause();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null) pauseAction.Disable();
    }

    private void Start()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMainMenuBGM();
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            ShowMainMenuButtons(); // Asegura que al iniciar se vean los botones principales
            if (mainMenuCamera != null) mainMenuCamera.Priority = 20;
        }

        // A�ADIDO: Apagamos al jugador mientras vemos el men�
        if (playerObject != null) playerObject.SetActive(false);
    }

    // --- L�GICA DE NAVEGACI�N DEL MEN� PRINCIPAL ---

    public void ShowMainMenuButtons()
    {
        if (mainMenuButtonsContainer != null) mainMenuButtonsContainer.SetActive(true);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (creditsContainer != null) creditsContainer.SetActive(false);
    }

    public void ShowOptions()
    {
        if (mainMenuButtonsContainer != null) mainMenuButtonsContainer.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(true);
        if (creditsContainer != null) creditsContainer.SetActive(false);
    }

    public void ShowCredits()
    {
        if (mainMenuButtonsContainer != null) mainMenuButtonsContainer.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (creditsContainer != null) creditsContainer.SetActive(true);
    }

    // --- L�GICA DE ESTADOS DEL JUEGO ---

    public void StartGame()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayGameplayBGM();
        Time.timeScale = 1f;

        // A�ADIDO: Prendemos al jugador para poder controlarlo
        if (playerObject != null) playerObject.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.FullGameReset();
        }

        if (mainMenuPanel != null)
        {
            StartCoroutine(FadeUI(mainMenuPanel, 1f, 0f, fadeDuration));
        }

        if (mainMenuCamera != null)
        {
            mainMenuCamera.Priority = 0;
        }
    }

    public void LoadMainMenu()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMainMenuBGM();
        Time.timeScale = 1f;
        isPaused = false;

        // A�ADIDO: Lo apagamos de nuevo al volver al men�
        if (playerObject != null) playerObject.SetActive(false);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (deathScreenUI != null) deathScreenUI.SetActive(false);

        if (mainMenuPanel != null)
        {
            ShowMainMenuButtons(); // Resetea la vista para que no se quede abierto Opciones
            StartCoroutine(FadeUI(mainMenuPanel, 0f, 1f, fadeDuration));
        }

        if (mainMenuCamera != null)
        {
            mainMenuCamera.Priority = 20;
        }
    }

    // --- L�GICA DE VICTORIA ---
    public void ShowWinCredits()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMainMenuBGM();
        Time.timeScale = 1f;
        isPaused = false;

        // Ocultamos al jugador y otros men�s
        if (playerObject != null) playerObject.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (deathScreenUI != null) deathScreenUI.SetActive(false);

        // Encendemos el panel principal, pero forzamos a que muestre los Cr�ditos
        if (mainMenuPanel != null)
        {
            if (mainMenuButtonsContainer != null) mainMenuButtonsContainer.SetActive(false);
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (creditsContainer != null) creditsContainer.SetActive(true);

            // Aparecemos la pantalla suavemente
            StartCoroutine(FadeUI(mainMenuPanel, 0f, 1f, fadeDuration));
        }

        // Hacemos que Cinemachine vuelva a la c�mara bonita de fondo
        if (mainMenuCamera != null)
        {
            mainMenuCamera.Priority = 20;
        }
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit(); // Cierra el juego al exportar la build (PC/Mac/Linux)

#if UNITY_EDITOR
        // Esta l�nea detiene el modo Play cuando est�s probando dentro del Editor de Unity
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // --- L�GICA DE MUERTE ---

    public void ShowDeathScreen()
    {
        if (deathScreenUI != null)
        {
            if (deathFadeCoroutine != null) StopCoroutine(deathFadeCoroutine);

            CanvasGroup cg = deathScreenUI.GetComponent<CanvasGroup>();
            float currentAlpha = cg != null ? cg.alpha : 0f;

            deathFadeCoroutine = StartCoroutine(FadeUI(deathScreenUI, currentAlpha, 1f, fadeDuration));
        }
    }

    public void HideDeathScreen()
    {
        if (deathScreenUI != null)
        {
            if (deathFadeCoroutine != null) StopCoroutine(deathFadeCoroutine);

            CanvasGroup cg = deathScreenUI.GetComponent<CanvasGroup>();
            float currentAlpha = cg != null ? cg.alpha : 1f;

            deathFadeCoroutine = StartCoroutine(FadeUI(deathScreenUI, currentAlpha, 0f, fadeDuration));
        }
    }

    public void RetryGame()
    {
        HideDeathScreen();
        if (GameManager.Instance != null) GameManager.Instance.RespawnPlayer();
    }

    // --- L�GICA DE PAUSA ---

    public void TogglePause()
    {
        if ((mainMenuPanel != null && mainMenuPanel.activeSelf) ||
            (deathScreenUI != null && deathScreenUI.activeSelf) ||
            (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)) return;

        isPaused = !isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // --- FUNCIONES COMUNES ---

    private IEnumerator FadeUI(GameObject uiObject, float startAlpha, float targetAlpha, float duration)
    {
        if (targetAlpha > 0f) uiObject.SetActive(true);

        CanvasGroup cg = uiObject.GetComponent<CanvasGroup>();
        if (cg == null) cg = uiObject.AddComponent<CanvasGroup>();

        if (targetAlpha > 0f) cg.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            cg.blocksRaycasts = false;
            uiObject.SetActive(false);
        }
    }
}