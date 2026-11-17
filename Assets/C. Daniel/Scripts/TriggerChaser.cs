using UnityEngine;

public class TriggerChaser : MonoBehaviour
{
    public GameObject chaser;

    [Header("Manzanas")]
    [Tooltip("Al entrar, desbloquea el lanzamiento de manzanas con la tecla K")]
    public bool unlockAppleLauncher = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (chaser != null)
            {
                Debug.Log("el jugador activo el trigger");
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySystemSFX(SoundType.ChaserAlert);
                chaser.SetActive(true);
            }

            if (unlockAppleLauncher)
            {
                AppleLauncher launcher = other.GetComponentInParent<AppleLauncher>();
                if (launcher == null) launcher = Object.FindFirstObjectByType<AppleLauncher>();
                if (launcher != null) launcher.UnlockLaunch();
            }
        }
    }
}
