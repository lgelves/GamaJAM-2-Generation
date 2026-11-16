using UnityEngine;

public class TriggerChaser : MonoBehaviour
{
    public GameObject chaser;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (chaser != null)
            {
                Debug.Log("el jugador activo el trigger");
                chaser.SetActive(true);
            }


        }
    }
}
