using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Code-only visual effects for each Element type.
/// Generates particles, line-renderers and flashes purely in C# — no prefabs needed.
/// Attach to a persistent GameObject alongside ParticleBurst.
/// </summary>
public class ElementVFX : MonoBehaviour
{
    public static ElementVFX Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC API — called from effect scripts
    // ═══════════════════════════════════════════════════════════

    /// <summary>Fire explosion: embers + rising smoke + screen flash.</summary>
    public void PlayFireVFX(Vector3 worldPos)
    {
        StartCoroutine(FireBurstRoutine(worldPos));
        StartCoroutine(ScreenFlashRoutine(new Color(1f, 0.35f, 0f, 0.18f), 0.12f));
        SoundManager.Instance?.Play(SoundManager.SFX.FireExplode);
    }

    /// <summary>Ice shatter: crystal shards + freeze ring pulse.</summary>
    public void PlayIceVFX(Vector3 worldPos)
    {
        StartCoroutine(IceShatterRoutine(worldPos));
        StartCoroutine(FreezeRingRoutine(worldPos));
        SoundManager.Instance?.Play(SoundManager.SFX.IceShatter);
    }

    /// <summary>Lightning strike: arc lines between origin and targets.</summary>
    public void PlayLightningVFX(Vector3 origin, List<Vector3> targetPositions)
    {
        foreach (var target in targetPositions)
            StartCoroutine(LightningArcRoutine(origin, target));
        StartCoroutine(ScreenFlashRoutine(new Color(0.7f, 0.7f, 1f, 0.15f), 0.08f));
        SoundManager.Instance?.Play(SoundManager.SFX.LightningStrike);
    }

    /// <summary>Ice melt: drip particles (used when ice becomes normal).</summary>
    public void PlayIceMeltVFX(Vector3 worldPos)
    {
        StartCoroutine(IceMeltRoutine(worldPos));
    }

    // ═══════════════════════════════════════════════════════════
    //  FIRE
    // ═══════════════════════════════════════════════════════════

    private IEnumerator FireBurstRoutine(Vector3 pos)
    {
        // 1. Large fast embers (orange/yellow) fan outward
        SpawnEmbers(pos, count: 14, speed: 5.5f, lifetime: 0.50f,
                    colorA: new Color(1f, 0.6f, 0f), colorB: new Color(1f, 0.9f, 0.1f),
                    size: 0.20f, gravity: -2f);

        // 2. Slower smoke puffs (dark grey), float upward
        yield return new WaitForSeconds(0.04f);
        SpawnSmoke(pos, count: 6, speed: 1.2f, lifetime: 0.70f,
                   color: new Color(0.25f, 0.25f, 0.25f, 0.6f), size: 0.32f);

        // 3. Tiny hot sparks stochastic
        for (int i = 0; i < 8; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float sp = Random.Range(3f, 7f);
            StartCoroutine(SparkRoutine(pos, dir * sp, Color.white, 0.25f, 0.06f));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ICE
    // ═══════════════════════════════════════════════════════════

    private IEnumerator IceShatterRoutine(Vector3 pos)
    {
        // Shards: elongated thin particles in ice colours
        int shardCount = 10;
        for (int i = 0; i < shardCount; i++)
        {
            float angle = (360f / shardCount) * i + Random.Range(-12f, 12f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float sp = Random.Range(3f, 6f);
            Color iceTint = Color.Lerp(new Color(0.5f, 0.9f, 1f), Color.white, Random.value);
            StartCoroutine(ShardRoutine(pos, dir * sp, iceTint, 0.45f, Random.Range(0.12f, 0.22f)));
        }
        yield break;
    }

    private IEnumerator FreezeRingRoutine(Vector3 pos)
    {
        // Expanding ring drawn by LineRenderer
        var go = new GameObject("FreezeRing");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lr, new Color(0.6f, 0.95f, 1f, 0.9f), 0.06f);
        lr.useWorldSpace = true;

        float duration = 0.35f;
        float elapsed  = 0f;
        int segments   = 32;
        lr.positionCount = segments + 1;

        while (elapsed < duration)
        {
            float t      = elapsed / duration;
            float radius = Mathf.Lerp(0.1f, 1.4f, t);
            float alpha  = Mathf.Lerp(0.9f, 0f, t);
            lr.startColor = lr.endColor = new Color(0.6f, 0.95f, 1f, alpha);

            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                lr.SetPosition(i, pos + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }

    private IEnumerator IceMeltRoutine(Vector3 pos)
    {
        // Small cyan drips falling downward
        for (int i = 0; i < 5; i++)
        {
            Vector2 vel = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1.5f, -3f));
            StartCoroutine(SparkRoutine(pos, vel, new Color(0.4f, 0.85f, 1f), 0.4f, 0.09f));
        }
        yield break;
    }

    // ═══════════════════════════════════════════════════════════
    //  LIGHTNING
    // ═══════════════════════════════════════════════════════════

    private IEnumerator LightningArcRoutine(Vector3 origin, Vector3 target)
    {
        var go = new GameObject("LightningArc");
        var lr = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lr, new Color(0.8f, 0.8f, 1f, 1f), 0.05f);
        lr.useWorldSpace = true;

        float duration = 0.18f;
        float elapsed  = 0f;
        int segments   = 8;
        lr.positionCount = segments + 1;

        while (elapsed < duration)
        {
            float t     = elapsed / duration;
            float alpha = t < 0.3f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);
            lr.startColor = lr.endColor = new Color(0.85f, 0.85f, 1f, alpha);

            // Draw jagged line with perpendicular random offsets
            for (int i = 0; i <= segments; i++)
            {
                float frac = (float)i / segments;
                Vector3 straight = Vector3.Lerp(origin, target, frac);
                // Jitter perpendicular to arc direction
                Vector3 perp = Vector3.Cross((target - origin).normalized, Vector3.forward);
                float jitter = (i == 0 || i == segments) ? 0f : Random.Range(-0.25f, 0.25f);
                lr.SetPosition(i, straight + perp * jitter);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);

        // Tiny flash at target
        StartCoroutine(SpawnFlashAt(target, new Color(0.8f, 0.8f, 1f), 0.10f));
    }

    // ═══════════════════════════════════════════════════════════
    //  SCREEN FLASH
    // ═══════════════════════════════════════════════════════════

    private IEnumerator ScreenFlashRoutine(Color flashColor, float duration)
    {
        // Create a fullscreen quad in screen space using a world-space sprite
        // placed right in front of the camera
        var cam = Camera.main;
        if (cam == null) yield break;

        var go = new GameObject("ScreenFlash");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.sortingOrder = 200;

        // Scale to cover screen
        float height = cam.orthographicSize * 2f;
        float width  = height * cam.aspect;
        go.transform.position   = cam.transform.position + Vector3.forward * 5f;
        go.transform.localScale = new Vector3(width, height, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            sr.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * (1f - t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }

    // ═══════════════════════════════════════════════════════════
    //  PARTICLE PRIMITIVES
    // ═══════════════════════════════════════════════════════════

    private void SpawnEmbers(Vector3 pos, int count, float speed, float lifetime,
                              Color colorA, Color colorB, float size, float gravity)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float sp  = speed * Random.Range(0.5f, 1.5f);
            Color col = Color.Lerp(colorA, colorB, Random.value);
            float sz  = size  * Random.Range(0.6f, 1.3f);
            StartCoroutine(EmberRoutine(pos, dir * sp, col, lifetime, sz, gravity));
        }
    }

    private void SpawnSmoke(Vector3 pos, int count, float speed, float lifetime,
                             Color color, float size)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 vel = new Vector2(Random.Range(-0.5f, 0.5f), speed * Random.Range(0.6f, 1.4f));
            StartCoroutine(SmokeRoutine(pos, vel, color, lifetime, size * Random.Range(0.7f, 1.3f)));
        }
    }

    private IEnumerator EmberRoutine(Vector3 startPos, Vector2 velocity, Color color,
                                      float lifetime, float size, float gravityY)
    {
        var (go, sr) = CreateParticle(color, size, 101);
        Vector3 pos = startPos;
        float elapsed = 0f;

        while (elapsed < lifetime && go != null)
        {
            float t = elapsed / lifetime;
            sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t * t));
            go.transform.localScale = Vector3.one * (size * Mathf.Lerp(1f, 0f, t));
            velocity += new Vector2(0f, gravityY) * Time.deltaTime;
            pos += (Vector3)(velocity * Time.deltaTime);
            go.transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator SmokeRoutine(Vector3 startPos, Vector2 velocity, Color color,
                                      float lifetime, float size)
    {
        var (go, sr) = CreateParticle(color, size, 99);
        Vector3 pos = startPos;
        float elapsed = 0f;

        while (elapsed < lifetime && go != null)
        {
            float t = elapsed / lifetime;
            // Smoke: starts transparent, peaks, then fades
            float alpha = t < 0.2f
                ? Mathf.Lerp(0f, color.a, t / 0.2f)
                : Mathf.Lerp(color.a, 0f, (t - 0.2f) / 0.8f);
            sr.color = new Color(color.r, color.g, color.b, alpha);
            // Smoke expands as it rises
            go.transform.localScale = Vector3.one * (size * (1f + t * 0.8f));
            pos += (Vector3)(velocity * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator ShardRoutine(Vector3 startPos, Vector2 velocity, Color color,
                                      float lifetime, float size)
    {
        var (go, sr) = CreateParticle(color, size, 101);
        // Shards are elongated (tall thin rectangle)
        go.transform.localScale = new Vector3(size * 0.35f, size, 1f);
        go.transform.rotation   = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Vector3 pos = startPos;
        float elapsed = 0f;
        float spin = Random.Range(-400f, 400f); // degrees/sec

        while (elapsed < lifetime && go != null)
        {
            float t = elapsed / lifetime;
            sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
            go.transform.localScale = new Vector3(size * 0.35f, size * Mathf.Lerp(1f, 0f, t), 1f);
            go.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            velocity += new Vector2(0f, -4f) * Time.deltaTime;
            pos += (Vector3)(velocity * Time.deltaTime);
            go.transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator SparkRoutine(Vector3 startPos, Vector2 velocity, Color color,
                                      float lifetime, float size)
    {
        var (go, sr) = CreateParticle(color, size, 102);
        Vector3 pos = startPos;
        float elapsed = 0f;

        while (elapsed < lifetime && go != null)
        {
            float t = elapsed / lifetime;
            sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
            go.transform.localScale = Vector3.one * (size * Mathf.Lerp(1f, 0f, t));
            pos += (Vector3)(velocity * Time.deltaTime);
            go.transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator SpawnFlashAt(Vector3 pos, Color color, float size)
    {
        var (go, sr) = CreateParticle(color, size, 103);
        float elapsed = 0f, dur = 0.12f;
        while (elapsed < dur && go != null)
        {
            float t = elapsed / dur;
            sr.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
            go.transform.localScale = Vector3.one * (size * (1f + t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private (GameObject go, SpriteRenderer sr) CreateParticle(Color color, float size, int sortOrder)
    {
        var go = new GameObject("VFXParticle");
        go.transform.position   = Vector3.zero;
        go.transform.localScale = Vector3.one * size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetSquareSprite();
        sr.color        = color;
        sr.sortingOrder = sortOrder;
        return (go, sr);
    }

    private static void ConfigureLineRenderer(LineRenderer lr, Color color, float width)
    {
        lr.material          = new Material(Shader.Find("Sprites/Default"));
        lr.startColor        = lr.endColor = color;
        lr.startWidth        = lr.endWidth = width;
        lr.numCapVertices    = 2;
        lr.sortingOrder      = 105;
    }

    // Shared 1×1 white sprite
    private static Sprite _sq;
    private static Sprite GetSquareSprite()
    {
        if (_sq != null) return _sq;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _sq = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _sq;
    }
}
