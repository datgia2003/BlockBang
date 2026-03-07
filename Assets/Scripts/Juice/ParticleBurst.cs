using System.Collections;
using UnityEngine;

/// <summary>
/// Code-only particle burst. Spawns small square SpriteRenderers that fly outward and fade.
/// No external prefabs required — everything is generated at runtime.
/// </summary>
public class ParticleBurst : MonoBehaviour
{
    public static ParticleBurst Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────

    /// <summary>Burst particles at a world position with a given color.</summary>
    public void Burst(Vector3 worldPos, Color color, int count = 8, float speed = 3f, float lifetime = 0.45f, float size = 0.18f)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-15f, 15f);
            float radians = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            float spd = speed * Random.Range(0.6f, 1.4f);
            float sz  = size  * Random.Range(0.6f, 1.2f);
            StartCoroutine(ParticleRoutine(worldPos, dir * spd, color, lifetime, sz));
        }
    }

    /// <summary>Short upward pop for block-placed feedback.</summary>
    public void PopBurst(Vector3 worldPos, Color color)
        => Burst(worldPos, color, count: 6, speed: 2.2f, lifetime: 0.35f, size: 0.14f);

    /// <summary>Big explosion for when a full line is cleared.</summary>
    public void LineClearBurst(Vector3 worldPos, Color color)
        => Burst(worldPos, color, count: 12, speed: 4.5f, lifetime: 0.55f, size: 0.20f);

    // ─────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────

    private IEnumerator ParticleRoutine(Vector3 startPos, Vector2 velocity, Color color, float lifetime, float size)
    {
        // Create a tiny quad as a particle
        var go = new GameObject("Particle");
        go.transform.position = startPos;
        go.transform.localScale = Vector3.one * size;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCachedSquareSprite();
        sr.color = color;
        sr.sortingOrder = 100; // render on top of everything

        float elapsed = 0f;
        Vector3 pos = startPos;
        // Gravity-like pull downward
        Vector2 gravity = new Vector2(0f, -4.5f);

        while (elapsed < lifetime)
        {
            float t = elapsed / lifetime;
            // Alpha fade out
            sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t * t));
            // Scale shrink
            float scale = size * Mathf.Lerp(1f, 0f, t);
            go.transform.localScale = Vector3.one * scale;

            velocity += gravity * Time.deltaTime;
            pos += (Vector3)(velocity * Time.deltaTime);
            go.transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(go);
    }

    // Cache a single 1×1 white pixel sprite so we don't re-create it every burst
    private static Sprite _squareSprite;
    private static Sprite GetCachedSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _squareSprite;
    }
}
