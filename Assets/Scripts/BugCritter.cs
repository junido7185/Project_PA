using System.Collections;
using UnityEngine;

// Small demo critter. Inventory remains the only reward authority.
// P5 — 발견(날갯짓·떠다님) → 접근 → 잠자리채 E 휘두르기 → 접촉 순간 명중(포획) 또는 빗나감(놀라 날아감) → 실제 나비 보상.
public sealed class BugCritter : MonoBehaviour, IInteractable
{
    Item _reward;
    Vector3 _home, _origin;
    float _phase, _elapsed;
    bool _captured, _claiming, _swinging;
    string _feedback;
    Transform _leftWing, _rightWing, _model;
    Vector3 _modelScale;
    // 놀란 나비는 잠깐 날아올라 근처의 새 자리로 옮긴다. 그동안은 잡을 수 없다.
    Vector3 _fleeFrom, _fleeTo;
    float _fleeStartedAt = -10f;
    const float FleeSeconds = 1.1f;

    public bool Captured => _captured;
    public bool Fleeing => Time.time - _fleeStartedAt < FleeSeconds;
    public Vector3 Home => _home;
    public string LastFeedback => _feedback ?? string.Empty;
    public const float RoamRadius = .45f;
    // 프롬프트는 2.05m에서 뜨지만 잠자리채가 실제로 덮는 거리는 이보다 짧다. 멀리서 휘두르면 빗나간다.
    public const float NetReach = 1.6f;

    public void Configure(Item reward, float phase)
    {
        _reward = reward;
        _home = _origin = transform.position;
        _phase = phase;
        _leftWing = transform.Find("LeftWing");
        _rightWing = transform.Find("RightWing");
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer.transform != _leftWing && renderer.transform != _rightWing) { _model = renderer.transform; break; }
        // Demo256의 임시 구 날개 대신 로컬 나비 모델을 쓴다(날개 폭 약 0.6m). 구 날개는 숨기기만 한다.
        var assets = FirstDayStudioAssets.Load();
        if (_model == null && assets != null && assets.butterfly != null)
        {
            var model = Instantiate(assets.butterfly, transform);
            model.name = "ButterflyModel";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = assets.butterfly.transform.localRotation;
            model.transform.localScale = assets.butterfly.transform.localScale * .5f;
            foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            _model = model.transform;
            foreach (var wing in new[] { _leftWing, _rightWing })
                if (wing != null && wing.GetComponent<Renderer>() != null) wing.GetComponent<Renderer>().enabled = false;
        }
        if (_model != null) _modelScale = _model.localScale;
    }

    static Vector3 Drift(float t) =>
        new Vector3(Mathf.Sin(t * .65f) * RoamRadius, Mathf.Sin(t * 2f) * .08f, Mathf.Cos(t * .43f) * RoamRadius);

    void Update()
    {
        if (_captured) return;
        _elapsed += Time.deltaTime;
        // Bounded idle drift, with no global random state or navigation ownership.
        float t = _elapsed + _phase;
        Vector3 before = transform.position;
        if (Fleeing)
        {
            float k = Mathf.Clamp01((Time.time - _fleeStartedAt) / FleeSeconds);
            transform.position = Vector3.Lerp(_fleeFrom, _fleeTo, Mathf.SmoothStep(0f, 1f, k)) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.1f);
            Flap(t * 2.2f);
        }
        else
        {
            transform.position = _home + Drift(t);
            Flap(t);
        }
        Vector3 move = Vector3.ProjectOnPlane(transform.position - before, Vector3.up);
        if (move.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move.normalized), Time.deltaTime * 6f);
    }

    void Flap(float t)
    {
        float flap = Mathf.Sin(t * 15f) * 35f;
        if (_leftWing != null) _leftWing.localRotation = Quaternion.Euler(0, 0, flap);
        if (_rightWing != null) _rightWing.localRotation = Quaternion.Euler(0, 0, -flap);
        // 단일 메시 모델은 날개 폭을 접었다 펴서 날갯짓을 보인다.
        if (_model != null)
            _model.localScale = new Vector3(_modelScale.x * (.3f + .7f * Mathf.Abs(Mathf.Cos(t * 7.5f))), _modelScale.y, _modelScale.z);
    }

    bool InReach(GameObject interactor, float reach, float facing)
    {
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - interactor.transform.position, Vector3.up);
        return delta.sqrMagnitude <= reach * reach && delta.sqrMagnitude >= .0025f &&
            Mathf.Abs(transform.position.y - interactor.transform.position.y) <= 2f &&
            Vector3.Dot(interactor.transform.forward, delta.normalized) >= facing;
    }

    void Say(string message, bool toast)
    {
        _feedback = message;
        if (toast) FirstDayWorldPresentation.Toast(message, false);
    }

    public void Interact(GameObject interactor)
    {
        if (_captured || _claiming || _swinging || Fleeing || !isActiveAndEnabled || interactor == null || _reward == null) return;
        var inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || inventory != Inventory.instance) return;
        Item held = EquipmentSystem.CurrentHeld(interactor);
        if (held == null || held.toolType != ToolType.Net)
        { Say("잠자리채를 들면 나비를 잡을 수 있어요.", false); return; }
        if (!InReach(interactor, 2.05f, .75f))
        { Say("나비에게 다가가 바라보세요.", false); return; }
        StartCoroutine(SwingNet(interactor, inventory));
    }

    // 잠자리채가 내려오는 순간(스윙 접촉) 나비가 그물 안에 있으면 잡힌다. 그 사이의 연타는 새 스윙을 만들지 않는다.
    IEnumerator SwingNet(GameObject interactor, Inventory inventory)
    {
        _swinging = true;
        GatherFeedback.Swish();
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _swinging = false;
        if (_captured || interactor == null) yield break;
        Item held = EquipmentSystem.CurrentHeld(interactor);
        if (held == null || held.toolType != ToolType.Net) yield break;
        if (!InReach(interactor, NetReach, .6f))
        {
            Say("빗나갔어요! 나비가 날아갔어요.", true);
            Startle(interactor.transform.position);
            yield break;
        }
        _claiming = true;
        bool added;
        try { added = inventory.AddInstance(new ItemInstance(_reward)); }
        finally { _claiming = false; }
        if (!added)
        {
            // 가방이 가득 차면 잡은 나비를 놓아준다(유실 없음, 내구도 차감 없음). 자리를 비우면 다시 잡을 수 있다.
            Say("가방이 가득 차서 나비를 놓아줬어요.", true);
            Startle(interactor.transform.position);
            yield break;
        }
        _captured = true;
        Say("나비를 잡았어요!", false);
        GatherFeedback.Catch();
        ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        DemoHotbarPreference.Prefer(_reward); // P8: 잡은 나비도 바로 들고 진열할 수 있게 핫바 우선
        FirstDayWorldPresentation.Acquired(_reward, 1, transform.position);
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        StartCoroutine(Netted(interactor.transform));
    }

    // 잡힌 나비가 잠자리채 쪽으로 빨려 들어가며 작아진다(0.25초).
    IEnumerator Netted(Transform holder)
    {
        Vector3 start = transform.position, scale = transform.localScale;
        for (float age = 0f; age < .25f; age += Time.deltaTime)
        {
            float k = age / .25f;
            Vector3 target = holder != null ? holder.position + holder.forward * .7f + Vector3.up * 1.1f : start;
            transform.position = Vector3.Lerp(start, target, k);
            transform.localScale = scale * (1f - k);
            yield return null;
        }
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        transform.localScale = scale;
    }

    // 플레이어 반대쪽으로 2.6m 날아가 같은 높이의 걸을 수 있는 땅 위에 새 자리를 잡는다. 처음 자리에서 4m 안에 머문다.
    void Startle(Vector3 from)
    {
        Vector3 away = Vector3.ProjectOnPlane(transform.position - from, Vector3.up);
        if (away.sqrMagnitude < .01f) away = Vector3.forward;
        var grid = FindFirstObjectByType<WorldGridService>();
        float originGround = _origin.y - .85f;
        if (grid != null && grid.WorldToCell(_origin, out var originCell) && grid.CellToWorld(originCell, out var originFloor))
            originGround = originFloor.y;
        float hover = _origin.y - originGround;
        Vector3 next = _origin;
        foreach (float turn in new[] { 0f, 35f, -35f, 70f, -70f, 110f, -110f })
        {
            Vector3 candidate = _home + Quaternion.Euler(0f, turn, 0f) * away.normalized * 2.6f;
            if (Vector3.ProjectOnPlane(candidate - _origin, Vector3.up).magnitude > 4f) continue;
            if (grid != null)
            {
                if (!grid.WorldToCell(candidate, out var cell) || !grid.TryGetCell(cell, out var data) || !data.IsWalkable ||
                    !grid.CellToWorld(cell, out var ground) || Mathf.Abs(ground.y - originGround) > 1f) continue;
                candidate.y = ground.y + hover;
            }
            next = candidate;
            break;
        }
        _fleeStartedAt = Time.time;
        _fleeFrom = transform.position;
        _home = next;
        _fleeTo = _home + Drift(_elapsed + _phase + FleeSeconds);
    }

    // 결과(빗나감·놓아줌·포획)는 토스트/획득 표시로 알리고, [E] 프롬프트에는 다음 행동만 둔다.
    public string GetInteractPrompt() => "나비 · 잠자리채 휘두르기";
}
