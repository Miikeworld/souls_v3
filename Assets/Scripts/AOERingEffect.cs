using UnityEngine;
using System.Collections;

/// <summary>
/// Creates a ring-shaped visual effect for AOE attacks.
/// The ring scales up over time and fades out.
/// Assign your material to the ringMaterial field.
/// </summary>
public class AOERingEffect : MonoBehaviour
{
    [Header("Appearance")]
    public Material ringMaterial;
    public Color ringColor = Color.red;
    public float startRadius = 0.5f;
    public float endRadius = 5f;
    public float thickness = 0.2f;
    public int segments = 64;

    [Header("Animation")]
    public float duration = 1f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh ringMesh;
    private Coroutine animationCoroutine;

    void Awake()
    {
        // If this script was manually added to an existing GameObject (not spawned dynamically), warn and disable
        if (GetComponent<Animator>() != null || GetComponent<UnityEngine.AI.NavMeshAgent>() != null || GetComponent<Entity>() != null)
        {
            Debug.LogWarning("[AOERingEffect] This script should not be attached directly to character GameObjects. Disabling.");
            enabled = false;
            return;
        }

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = ringMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        CreateRingMesh();
    }

    void CreateRingMesh()
    {
        ringMesh = new Mesh();
        ringMesh.name = "AOE Ring";

        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uv = new Vector2[segments * 2];

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Inner vertex
            vertices[i * 2] = new Vector3(cos * (1 - thickness), 0, sin * (1 - thickness));
            uv[i * 2] = new Vector2(0, (float)i / segments);

            // Outer vertex
            vertices[i * 2 + 1] = new Vector3(cos, 0, sin);
            uv[i * 2 + 1] = new Vector2(1, (float)i / segments);

            // Triangles
            int next = (i + 1) % segments;
            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = i * 2 + 1;
            triangles[i * 6 + 2] = next * 2;
            triangles[i * 6 + 3] = next * 2;
            triangles[i * 6 + 4] = i * 2 + 1;
            triangles[i * 6 + 5] = next * 2 + 1;
        }

        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.uv = uv;
        ringMesh.RecalculateNormals();
        ringMesh.RecalculateBounds();

        meshFilter.mesh = ringMesh;
    }

    void Start()
    {
        // Start animation on spawn
        PlayAnimation();
    }

    public void PlayAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateRing());
    }

    IEnumerator AnimateRing()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale
            float scale = Mathf.Lerp(startRadius, endRadius, scaleCurve.Evaluate(t));
            transform.localScale = Vector3.one * scale;

            // Fade
            if (meshRenderer.material != null)
            {
                Color color = ringColor;
                color.a = fadeCurve.Evaluate(t);
                meshRenderer.material.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (ringMesh != null)
            Destroy(ringMesh);
    }

    /// <summary>
    /// Spawns an AOE ring effect at the specified position.
    /// </summary>
    public static AOERingEffect Spawn(Vector3 position, Material material, float radius, float duration = 1f)
    {
        GameObject go = new GameObject("AOE Ring");
        go.transform.position = position;

        AOERingEffect effect = go.AddComponent<AOERingEffect>();
        effect.ringMaterial = material;
        effect.endRadius = radius;
        effect.duration = duration;

        return effect;
    }
}
