using UnityEngine;

// Lleva la cuenta de manzanas que el slime se ha comido.
// Ponlo en un objeto vacio de la escena (ej: el mismo donde estan los managers).
public class AppleInventory : MonoBehaviour
{
    public static AppleInventory Instance { get; private set; }

    // La UI se suscribe a esto para actualizarse sola
    public static event System.Action<int> OnCountChanged;

    public int Count { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void Add(int amount = 1)
    {
        Count += amount;
        OnCountChanged?.Invoke(Count);
    }

    // Gasta una manzana. Devuelve false si no habia ninguna.
    public bool TryConsume()
    {
        if (Count <= 0) return false;
        Count--;
        OnCountChanged?.Invoke(Count);
        return true;
    }

    public void ResetInventory()
    {
        Count = 0;
        OnCountChanged?.Invoke(Count);
    }
}
