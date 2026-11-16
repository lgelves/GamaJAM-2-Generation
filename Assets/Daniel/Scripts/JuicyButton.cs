using UnityEngine;
using UnityEngine.EventSystems;

// Implementamos las interfaces de EventSystem para detectar el mouse exactamente
public class JuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;

    [Header("Efecto Visual")]
    public float hoverScaleMultiplier = 1.1f;  // Crece 10% al pasar el mouse
    public float clickScaleMultiplier = 0.95f; // Se encoge un 5% al presionar

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScaleMultiplier;

        // Disparamos el sonido de Hover
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySystemSFX(SoundType.UIHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * clickScaleMultiplier;

        // Disparamos el sonido de Clic
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySystemSFX(SoundType.UIClick);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Al soltar el clic, vuelve al tamaño de "Hover" (porque el mouse sigue encima)
        transform.localScale = originalScale * hoverScaleMultiplier;
    }

    private void OnDisable()
    {
        // Previene un bug donde el botón se queda grande si apagas el menú mientras el mouse está encima
        transform.localScale = originalScale;
    }
}