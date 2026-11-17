using UnityEngine;

[DisallowMultipleComponent]
public class Apple : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GetTopApple().Collect();
    }

    // Si hay scripts Apple anidados (padre e hijo) o varios colliders disparando a la vez,
    // solo el Apple de mas arriba cuenta, y una unica vez.
    private Apple GetTopApple()
    {
        Apple[] chain = GetComponentsInParent<Apple>(true); // de este objeto hacia arriba
        return chain[chain.Length - 1];
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySystemSFX(SoundType.Collectible);

        if (AppleInventory.Instance != null)
            AppleInventory.Instance.Add(1);

        Destroy(gameObject);
    }
}
