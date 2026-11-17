using System.Collections;
using UnityEngine;
using TMPro;

// Contador de manzanas en la esquina superior izquierda.
// Este script va en un objeto SIEMPRE ACTIVO (ej: el Canvas del HUD).
// 'container' es un hijo con el icono de manzana + el texto.
public class AppleUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject container;
    public TextMeshProUGUI countText;

    [Header("Ajustes")]
    public bool hideWhenEmpty = true;
    public string format = "x {0}";

    [Header("Efecto al cambiar")]
    public float popScale = 1.3f;
    public float popDuration = 0.15f;

    private Coroutine popRoutine;

    private void OnEnable()
    {
        AppleInventory.OnCountChanged += Refresh;
    }

    private void OnDisable()
    {
        AppleInventory.OnCountChanged -= Refresh;
    }

    private void Start()
    {
        int current = AppleInventory.Instance != null ? AppleInventory.Instance.Count : 0;
        Refresh(current, false);
    }

    private void Refresh(int count) => Refresh(count, true);

    private void Refresh(int count, bool animate)
    {
        if (countText != null) countText.text = string.Format(format, count);
        if (container != null) container.SetActive(count > 0 || !hideWhenEmpty);

        if (animate && container != null && container.activeInHierarchy)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(Pop());
        }
    }

    private IEnumerator Pop()
    {
        Transform t = container.transform;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(elapsed / popDuration) * Mathf.PI); // sube y baja
            t.localScale = Vector3.one * Mathf.Lerp(1f, popScale, k);
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
