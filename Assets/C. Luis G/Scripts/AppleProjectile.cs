using UnityEngine;

// Cualquier script que implemente esto, podrá ser golpeado por manzanas.
public interface IAppleTarget
{
    void OnAppleHit();
}

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class AppleProjectile : MonoBehaviour
{
    [Header("Configuración")]
    public float lifetime = 3f;
    public bool destroyOnHit = true;
    public ParticleSystem splatPrefab;

    [Header("Filtro de Objetivos")]
    [Tooltip("El tag exacto que tiene tu enemigo Chaser en el Inspector")]
    public string targetTag = "Enemy";

    private bool hasHit = false;
    private Rigidbody rb;

    public void Launch(Vector3 velocity, Collider[] collidersToIgnore)
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = velocity;

        Collider myCollider = GetComponent<Collider>();
        if (collidersToIgnore != null)
        {
            foreach (Collider other in collidersToIgnore)
            {
                if (other != null) Physics.IgnoreCollision(myCollider, other);
            }
        }
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        CheckHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        CheckHit(other);
    }

    private void CheckHit(Collider otherCollider)
    {
        // Escudo: Ignoramos si toca al jugador de alguna forma extraña
        if (otherCollider.CompareTag("Player")) return;

        // Si toca al objetivo (Chaser)
        if (otherCollider.CompareTag(targetTag))
        {
            IAppleTarget target = otherCollider.GetComponentInParent<IAppleTarget>();
            if (target != null)
            {
                target.OnAppleHit();
            }
        }

        // Destrucción (Paredes, Suelo, Chaser)
        if (destroyOnHit)
        {
            hasHit = true;

            if (splatPrefab != null)
            {
                ParticleSystem splat = Instantiate(splatPrefab, transform.position, Quaternion.identity);
                splat.Play();
                Destroy(splat.gameObject, splat.main.duration);
            }

            Destroy(gameObject);
        }
    }
}