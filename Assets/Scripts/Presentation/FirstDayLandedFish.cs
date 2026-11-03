using System.Collections;
using UnityEngine;

// Temporary world presentation. FishingSpot and the existing daily stock loop own the reward.
// 피드백 5(2026-10-31): 뭍에 올라온 물고기는 난이도(종·크기)에 따라 보이지 않는 체력과 도망 시간을 가진다.
// 무엇을 들고 있든(맨손·낚싯대 포함) 때릴 수 있고 도끼·곡괭이가 더 세다. 포획 타격은 어떤 도구의 내구도도 쓰지 않는다.
// 제때 잡지 못하면 물로 튀어 돌아간다(보상·기록·내구도 변화 없음).
public sealed class FirstDayLandedFish : MonoBehaviour, IInteractable
{
    public Item Item { get; private set; }
    public int Hits { get; private set; }
    public bool ReadyToClaim => _health <= 0f;
    public bool OnLand { get; private set; }
    public bool Escaping { get; private set; }
    public Vector3 DryRest { get; private set; }
    public float Difficulty { get; private set; }
    // 남은 도망 시간(초). 기절(체력 0)한 뒤에는 도망가지 않는다.
    public float EscapeIn => OnLand && !ReadyToClaim && !Escaping ? Mathf.Max(0f, _escapeAt - Time.time) : float.PositiveInfinity;
    const float WarnSeconds = 3f;
    FishingSpot _owner;
    Transform _visual;
    Quaternion _restRotation;
    Vector3 _visualRest, _visualScale, _waterReturn;
    bool _pending, _claimed;
    float _nextHitAt, _landedAt, _escapeAt, _health = 1f, _hitAt = -10f;

    public static FirstDayLandedFish Create(FishingSpot owner, Item item, GameObject model, Vector3 from, Vector3 dryRest, float difficulty = .3f)
    {
        if (owner == null || item == null || model == null) return null;
        var root = new GameObject("LandedFish_" + item.id);
        root.transform.SetParent(owner.transform, true); root.transform.position = from;
        var fish = root.AddComponent<FirstDayLandedFish>();
        fish._owner = owner; fish.Item = item; fish.DryRest = dryRest; fish._waterReturn = from;
        fish.Difficulty = Mathf.Clamp01(difficulty);
        // 숨은 체력: 쉬운 물고기는 도구 1~2번·맨손 2번, 어려운 물고기는 도구 3번·맨손 4번 안팎(개체마다 조금 다르다).
        fish._health = Mathf.Lerp(.85f, 2.7f, fish.Difficulty) + Random.Range(-.15f, .15f);
        var visual = Instantiate(model, root.transform);
        foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Destroy(root); return null; }
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        visual.transform.localScale *= .85f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, .01f);
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        visual.transform.position += root.transform.position - bounds.center + Vector3.up * (bounds.size.y / 2 + .05f);
        fish._visual = visual.transform;
        fish._visualRest = visual.transform.localPosition; fish._restRotation = visual.transform.localRotation;
        fish._visualScale = visual.transform.localScale;
        var contact = root.AddComponent<SphereCollider>(); contact.isTrigger = true;
        contact.radius = .42f; contact.center = Vector3.up * .25f;
        fish.StartCoroutine(fish.Land(from));
        return fish;
    }

    // 쉬운 물고기는 12초, 어려운 물고기는 6.5초 안에 잡아야 한다.
    public static float EscapeSecondsFor(float difficulty) => Mathf.Lerp(12f, 6.5f, Mathf.Clamp01(difficulty));

    IEnumerator Land(Vector3 from)
    {
        for (float elapsed = 0; elapsed < .85f; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / .85f);
            transform.position = Vector3.Lerp(from, DryRest, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.5f);
            yield return null;
        }
        transform.position = DryRest; OnLand = true; _landedAt = Time.time;
        _escapeAt = _landedAt + EscapeSecondsFor(Difficulty);
        GatherFeedback.Splash();
    }

    void Update()
    {
        if (!OnLand || _visual == null || _claimed || Escaping) return;
        float now = Time.time, t = now - _landedAt;
        if (!ReadyToClaim && !_pending && now >= _escapeAt) { StartCoroutine(Escape()); return; }
        // 도망 직전 3초는 더 빠르고 크게 퍼덕인다(곧 물로 돌아간다는 신호).
        float urgency = ReadyToClaim ? 0f : 1f - Mathf.Clamp01((_escapeAt - now) / WarnSeconds);
        float rate = 8f + urgency * 7f, amp = 1f + urgency * .8f;
        float flop = ReadyToClaim ? 0 : Mathf.Max(0, Mathf.Sin(t * rate)) * .18f * amp;
        // All motion stays on the checked dry cell. The fish cannot jump into the sea or a building.
        transform.position = DryRest + new Vector3(Mathf.Sin(t * 1.9f), 0, Mathf.Cos(t * 2.3f)) * (ReadyToClaim ? 0 : .12f * amp);
        _visual.localPosition = _visualRest + Vector3.up * flop;
        _visual.localRotation = _restRotation * Quaternion.Euler(0, Mathf.Sin(t * 2) * 12, ReadyToClaim ? 25 : Mathf.Sin(t * rate) * 32 * amp);
        // 맞은 순간 잠깐 납작해진다.
        float flinch = Mathf.Clamp01(1f - (now - _hitAt) / .22f);
        _visual.localScale = Vector3.Scale(_visualScale, new Vector3(1f + .18f * flinch, 1f - .3f * flinch, 1f + .18f * flinch));
    }

    // 확인된 마른 칸에서 출발해 낚은 물 위치로 튀어 돌아간다(경로 위 다른 물체와 충돌하지 않는 표시 전용 포물선).
    IEnumerator Escape()
    {
        Escaping = true;
        Vector3 from = transform.position, to = _waterReturn;
        _visual.localScale = _visualScale;
        for (float elapsed = 0; elapsed < .6f; elapsed += Time.deltaTime)
        {
            float k = Mathf.Clamp01(elapsed / .6f);
            transform.position = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.1f);
            _visual.localRotation = _restRotation * Quaternion.Euler(0, 0, k * 540f);
            yield return null;
        }
        GatherFeedback.Splash();
        OnLand = false;
        if (_owner != null) _owner.OnLandedFishEscaped(this);
        if (this != null) Destroy(gameObject);
    }

    // 무엇을 들고 있어도(맨손 포함) 때릴 수 있다. PlayerInteraction이 도구 실패 처리 전에 이 값을 묻는다.
    public static bool CanStrike(Item held) => true;

    // 한 번 때릴 때 깎는 숨은 체력: 도끼·곡괭이·무기 1, 잠자리채 .8, 맨손·낚싯대·그 밖의 물건 .7.
    public static float StrikePower(Item held) => held == null ? .7f :
        held.toolType == ToolType.Axe || held.toolType == ToolType.Pickaxe || held.toolType == ToolType.Weapon ? 1f :
        held.toolType == ToolType.Net ? .8f : .7f;

    bool InReach(GameObject player)
    {
        if (player == null || player.GetComponent<Inventory>() != Inventory.instance) return false;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - player.transform.position, Vector3.up);
        return delta.sqrMagnitude <= 2.05f * 2.05f && delta.sqrMagnitude > .0025f &&
            Mathf.Abs(transform.position.y - player.transform.position.y) < 2 &&
            Vector3.Dot(player.transform.forward, delta.normalized) >= .75f;
    }

    public void Interact(GameObject player)
    {
        if (!OnLand || Escaping || _claimed || _pending || _owner == null || !InReach(player)) return;
        if (ReadyToClaim) { Claim(player); return; }
        if (Time.time < _nextHitAt) return;
        _pending = true; _nextHitAt = Time.time + .55f;
        StartCoroutine(Contact(player));
    }

    IEnumerator Contact(GameObject player)
    {
        Item held = EquipmentSystem.CurrentHeld(player);
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _pending = false;
        if (_claimed || Escaping || !OnLand || !InReach(player) || EquipmentSystem.CurrentHeld(player) != held) yield break;
        Hits++; _hitAt = Time.time;
        _health -= StrikePower(held);
        // 때리는 도중에 튀어 나가지 않게 도망 시각을 조금 미룬다.
        _escapeAt = Mathf.Max(_escapeAt, Time.time + 1.2f);
        GatherFeedback.Splash();
        if (ReadyToClaim) Claim(player);
    }

    void Claim(GameObject player)
    {
        if (!_owner.TryClaimLandedFish(this, player)) return;
        _claimed = true; gameObject.SetActive(false);
    }

    public string GetInteractPrompt()
    {
        string name = ItemDisplayName.For(Item);
        if (Escaping) return name + " · 물로 도망쳤어요";
        if (!OnLand) return name + " · 끌어올리는 중";
        if (ReadyToClaim) return name + " · 가방을 비우고 담기";
        return EscapeIn <= WarnSeconds ? name + " · 곧 도망가요! 얼른 때려서 잡기" : name + " · 때려서 잡기 (맨손 가능)";
    }
}
