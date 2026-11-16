using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // Temblor sutil y rápido para el cambio de 2D a 3D
    public void ShakePerspectiveChange()
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulseWithForce(0.3f);
    }

    // Temblor violento para cuando el jugador muere o recibe daño
    public void ShakeDeath()
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulseWithForce(1.2f);
    }

    // Temblor personalizado por si quieres usarlo en otras cosas (ej: caída pesada)
    public void ShakeCustom(float force)
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulseWithForce(force);
    }
}