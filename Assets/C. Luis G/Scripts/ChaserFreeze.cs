using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Va en el mismo objeto que ChaserEnemy.
// Cuando una manzana lo golpea, el perseguidor se congela unos segundos.
[RequireComponent(typeof(ChaserEnemy))]
public class ChaserFreeze : MonoBehaviour, IAppleTarget
{
    [Header("Congelamiento")]
    public float freezeDuration = 4f;

    [Header("Efecto visual")]
    public bool tintWhileFrozen = true;
    public Color frozenTint = new Color(0.55f, 0.85f, 1f);

    public bool IsFrozen { get; private set; }

    private ChaserEnemy chaser;
    private NavMeshAgent agent;
    private MeshRenderer[] meshRenderers;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;
    private MaterialPropertyBlock block;
    private Coroutine freezeRoutine;

    private void Awake()
    {
        chaser = GetComponent<ChaserEnemy>();
        agent = GetComponent<NavMeshAgent>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        block = new MaterialPropertyBlock();
    }

    // La llama AppleProjectile al chocar
    public void OnAppleHit()
    {
        Freeze(freezeDuration);
    }

    public void Freeze(float duration)
    {
        if (!gameObject.activeInHierarchy) return;

        // Si ya estaba congelado, reinicia el tiempo
        if (freezeRoutine != null) StopCoroutine(freezeRoutine);
        freezeRoutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        BeginFreeze();
        yield return new WaitForSeconds(duration); // tiempo escalado: no corre en pausa ni en dialogos
        EndFreeze();
        freezeRoutine = null;
    }

    private void BeginFreeze()
    {
        if (!IsFrozen)
        {
            IsFrozen = true;
            ApplyTint(true);
        }

        // Apagar ChaserEnemy detiene su Update, que es lo que lo mueve hacia el jugador
        chaser.enabled = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private void EndFreeze()
    {
        if (!IsFrozen) return;
        IsFrozen = false;

        // Al reactivarse, ChaserEnemy relee la perspectiva y reanuda la persecucion si esta en 3D
        chaser.enabled = true;
        ApplyTint(false);
    }

    // Si el perseguidor se desactiva (ej: reaparecer tras morir) no debe quedar congelado para siempre
    private void OnDisable()
    {
        freezeRoutine = null;
        EndFreeze();
    }

    private void ApplyTint(bool frozen)
    {
        if (!tintWhileFrozen) return;

        foreach (MeshRenderer r in meshRenderers)
        {
            if (r == null) continue;
            if (frozen)
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", frozenTint); // URP
                block.SetColor("_Color", frozenTint);     // shaders estandar
                r.SetPropertyBlock(block);
            }
            else
            {
                r.SetPropertyBlock(null);
            }
        }

        if (frozen)
        {
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null) continue;
                originalSpriteColors[i] = spriteRenderers[i].color;
                spriteRenderers[i].color = frozenTint;
            }
        }
        else if (originalSpriteColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length && i < originalSpriteColors.Length; i++)
            {
                if (spriteRenderers[i] != null) spriteRenderers[i].color = originalSpriteColors[i];
            }
        }
    }
}
