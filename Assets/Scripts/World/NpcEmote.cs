using UnityEngine;

// 주민 감정 표시(레퍼런스 04 — 기뻐하는 주민 머리 둘레로 별이 피어오른다). 표시 전용: 충돌·상호작용·저장 없음.
// 선물·친밀도 단계 순간에 별 몇 개가 머리 둘레에서 엇갈려 생겨 떠오르며 사라진다. 별 모양은 발견물 반짝임과 같은 그림.
public sealed class NpcEmote : MonoBehaviour
{
    public static int Bursts { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCount() => Bursts = 0;

    static readonly Color Pink = new Color(1f, .62f, .78f), Gold = new Color(1f, .86f, .42f), Cream = new Color(1f, .97f, .86f);
    const float Life = 1.5f, Spread = 1.1f, Rise = .6f, Size = .28f;

    Transform _head;
    float _start;
    int _count;
    SpriteRenderer[] _stars;
    Vector3[] _offsets;
    float[] _delays;

    // strength: 0 평범한 선물(금빛 3), 1 좋아하는 선물(분홍·금빛 6), 2 친밀도 단계(섞어 8).
    public static void Burst(Transform npc, int strength)
    {
        if (npc == null) return;
        var go = new GameObject("NpcEmote");
        var emote = go.AddComponent<NpcEmote>();
        emote._head = npc;
        emote._count = strength <= 0 ? 3 : strength == 1 ? 6 : 8;
        emote.Build(strength);
        Bursts++;
    }

    void Build(int strength)
    {
        _start = Time.time;
        _stars = new SpriteRenderer[_count];
        _offsets = new Vector3[_count];
        _delays = new float[_count];
        for (int i = 0; i < _count; i++)
        {
            float a = (i / (float)_count) * Mathf.PI * 2f + Random.Range(-.3f, .3f);
            _offsets[i] = new Vector3(Mathf.Cos(a) * Random.Range(.25f, .45f), Random.Range(-.1f, .15f), Mathf.Sin(a) * Random.Range(.25f, .45f));
            _delays[i] = i * (Spread / _count);
            var star = new GameObject("Star" + i).AddComponent<SpriteRenderer>();
            star.transform.SetParent(transform, false);
            star.sprite = FindSparkle.StarSprite;
            if (FindSparkle.StarMaterial != null) star.sharedMaterial = FindSparkle.StarMaterial;
            Color c = strength <= 0 ? Gold : strength == 1 ? (i % 2 == 0 ? Pink : Gold) : (i % 3 == 0 ? Pink : i % 3 == 1 ? Gold : Cream);
            star.color = new Color(c.r, c.g, c.b, 0f);
            star.enabled = false;
            _stars[i] = star;
        }
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        float age = Time.time - _start;
        if (_head == null || age > Spread + Life) { Destroy(gameObject); return; }
        Vector3 top = _head.position + Vector3.up * 1.9f;
        for (int i = 0; i < _stars.Length; i++)
        {
            float t = (age - _delays[i]) / Life;
            var star = _stars[i];
            if (t < 0f || t > 1f) { star.enabled = false; continue; }
            star.enabled = true;
            float alpha = t < .2f ? t / .2f : 1f - (t - .2f) / .8f;
            Color c = star.color; c.a = Mathf.Clamp01(alpha); star.color = c;
            star.transform.position = top + _offsets[i] * (1f + t * .4f) + Vector3.up * (Rise * t);
            if (cam != null) star.transform.rotation = Quaternion.LookRotation(star.transform.position - cam.transform.position, cam.transform.up) * Quaternion.Euler(0, 0, t * 120f + i * 30f);
            star.transform.localScale = Vector3.one * Size * (.7f + .3f * Mathf.Sin(t * Mathf.PI));
        }
    }
}
