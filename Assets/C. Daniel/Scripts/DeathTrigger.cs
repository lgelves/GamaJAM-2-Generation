using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    // Enter: Se dispara en el frame exacto en que se tocan
    private void OnTriggerEnter(Collider other) => CheckDeath(other.gameObject);
    private void OnCollisionEnter(Collision collision) => CheckDeath(collision.gameObject);

    // Stay: Se dispara continuamente si siguen en contacto
    private void OnTriggerStay(Collider other) => CheckDeath(other.gameObject);
    private void OnCollisionStay(Collision collision) => CheckDeath(collision.gameObject);

    private void CheckDeath(GameObject targetObj)
    {
        if (targetObj.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerPlayerDeath(targetObj);
            }
        }
    }
}