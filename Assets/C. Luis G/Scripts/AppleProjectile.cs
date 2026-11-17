using System.Collections.Generic;
using UnityEngine;

// Cualquier cosa que deba reaccionar a una manzana lanzada implementa esto
// (ej: ChaserFreeze en el perseguidor).
public interface IAppleTarget
{
    void OnAppleHit();
}

// Va en el prefab de la manzana LANZADA.
// Launch() prepara solo el Rigidbody y el Collider, asi que funciona aunque el prefab
// no los tenga bien configurados (kinematico, sin collider, MeshCollider de Quad, etc.).
public class AppleProjectile : MonoBehaviour
{
    public float lifetime = 3f;
    public bool destroyOnHit = true;
    [Tooltip("Tiempo tras salir en el que un choque con el suelo/paredes no la destruye (evita que muera al nacer)")]
    public float spawnGraceTime = 0.1f;
    [Tooltip("Opcional: particulas de salpicadura al chocar")]
    public ParticleSystem splatPrefab;
    [Tooltip("Muestra en la Console que pasa con cada manzana lanzada")]
    public bool debugLogs = true;

    private bool hasHit = false;
    private float launchTime;

    public void Launch(Vector3 velocity, Collider[] collidersToIgnore)
    {
        launchTime = Time.time;

        // Esta manzana NO debe poder recogerse otra vez
        foreach (Apple pickup in GetComponentsInChildren<Apple>(true))
            Destroy(pickup);

        // Rigidbody: lo buscamos (incluso en hijos) o lo creamos
        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // el sprite siempre mira a la camara
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Un MeshCollider NO convexo (el de un Quad) no funciona en un Rigidbody dinamico
        foreach (MeshCollider mc in GetComponentsInChildren<MeshCollider>(true))
        {
            if (!mc.convex)
            {
                mc.enabled = false;
                Destroy(mc);
            }
        }

        List<Collider> colliders = new List<Collider>();
        foreach (Collider c in GetComponentsInChildren<Collider>())
            if (c.enabled) colliders.Add(c);

        // Sin collider util: le damos una esfera del tamano de lo que se ve
        if (colliders.Count == 0)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();

            Renderer[] rends = GetComponentsInChildren<Renderer>();
            float worldRadius = 0.25f;
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                sc.center = transform.InverseTransformPoint(b.center);
                worldRadius = Mathf.Clamp(Mathf.Max(b.extents.x, b.extents.y, b.extents.z), 0.15f, 0.5f);
            }

            Vector3 s = transform.lossyScale;
            float scale = Mathf.Max(0.0001f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
            sc.radius = worldRadius / scale;
            colliders.Add(sc);
        }

        foreach (Collider c in colliders)
        {
            c.isTrigger = false;
            if (collidersToIgnore == null) continue;

            foreach (Collider other in collidersToIgnore)
            {
                if (other != null && other.enabled && other.gameObject.activeInHierarchy)
                    Physics.IgnoreCollision(c, other);
            }
        }

        rb.linearVelocity = velocity;
        Destroy(gameObject, lifetime);

        if (debugLogs)
            Debug.Log($"[AppleProjectile] '{name}' lanzada. velocidad={velocity} kinematic={rb.isKinematic} colliders={colliders.Count} pos={transform.position}");
    }

    // Choque fisico (suelo, paredes, o un enemigo con collider solido)
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        IAppleTarget target = collision.collider.GetComponentInParent<IAppleTarget>();

        // Recien nacida: un roce con el suelo no cuenta
        if (target == null && Time.time - launchTime < spawnGraceTime) return;

        if (debugLogs)
            Debug.Log($"[AppleProjectile] choco con '{collision.collider.name}' (objetivo={(target != null)})");

        Impact(target);
    }

    // Enemigo con collider "Is Trigger" (ej: el del DeathTrigger): solo cuenta si es un objetivo
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        IAppleTarget target = other.GetComponentInParent<IAppleTarget>();
        if (target == null) return; // ignora checkpoints, zonas de dialogo, etc.

        if (debugLogs)
            Debug.Log($"[AppleProjectile] golpeo (trigger) a '{other.name}'");

        Impact(target);
    }

    private void Impact(IAppleTarget target)
    {
        if (target != null) target.OnAppleHit();

        if (!destroyOnHit) return;
        hasHit = true;

        if (splatPrefab != null)
        {
            ParticleSystem splat = Instantiate(splatPrefab, transform.position, Quaternion.identity);
            splat.Play();
            Destroy(splat.gameObject, 2f);
        }

        Destroy(gameObject);
    }
}
