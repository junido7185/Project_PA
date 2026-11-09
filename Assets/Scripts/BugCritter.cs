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
    // 2026-11-09 꽃 사이를 옮겨 다니는 곤충(호박벌, FlyTo): 비행 시간·높이·좌우 흔들림을 따로 쓴다. 놀라 날 때는 기본값(1.1초, 1.1m, 흔들림 없음).
    float _flightSeconds = FleeSeconds, _flightArc = 1.1f, _flightWobble;
    bool _ownWings;

    public bool Captured => _captured;

    // 그날 잡은 개체(이름 → 잡은 횟수) 기록. 저장에 남겨 이어하기 뒤에도 하루 상한을 지킨다(날이 바뀌면 기록은 새 날 것만).
    // 2026-11-04 하루 안 반복(Content-insects-plan §C.1): 잡힌 개체는 게임 시간 1.5시간 뒤 처음 자리로 돌아오고, 개체마다 하루 2번까지.
    public const int DailyCap = 2;
    public const float RearmHours = 1.5f;
    static readonly System.Collections.Generic.Dictionary<string, int> Caught = new System.Collections.Generic.Dictionary<string, int>();
    public static int CaughtDay { get; private set; }
    public static int RestoredHidden { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCaught() { Caught.Clear(); CaughtDay = 0; RestoredHidden = 0; }
    static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
    public static int CaughtToday(string critterName) => CaughtDay == Today && Caught.TryGetValue(critterName, out int n) ? n : 0;
    // 저장 형식 "이름:횟수"(이전 형식 "이름"은 1번으로 읽는다).
    public static System.Collections.Generic.List<string> CaughtSnapshot()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var pair in Caught) list.Add(pair.Key + ":" + pair.Value);
        return list;
    }

    public static void RestoreCaught(int day, System.Collections.Generic.List<string> names)
    {
        Caught.Clear(); CaughtDay = 0;
        if (day <= 0 || names == null || day != Today) return;
        CaughtDay = day;
        foreach (var entry in names)
        {
            if (string.IsNullOrEmpty(entry)) continue;
            int colon = entry.LastIndexOf(':');
            int count = 1;
            string key = entry;
            if (colon > 0 && int.TryParse(entry.Substring(colon + 1), out int parsed)) { key = entry.Substring(0, colon); count = parsed; }
            Caught[key] = Mathf.Max(count, Caught.TryGetValue(key, out int had) ? had : 0);
        }
        foreach (var critter in FindObjectsByType<BugCritter>(FindObjectsSortMode.None)) critter.HideIfCaught();
    }

    // 이어하기: 그날 상한까지 잡은 개체만 숨긴다(한 번 잡은 개체는 돌아와 있는 상태로 둔다).
    void HideIfCaught()
    {
        if (_captured || CaughtToday(name) < DailyCap) return;
        _captured = true; RestoredHidden++; _captureDay = Today; _rearmAt = -1f;
        HideBody();
    }

    static int RecordCaught(string critterName)
    {
        if (CaughtDay != Today) { Caught.Clear(); CaughtDay = Today; }
        Caught.TryGetValue(critterName, out int n);
        Caught[critterName] = ++n;
        return n;
    }

    // 숨길 때 실제로 켜져 있던 렌더러·충돌체만 기억해 두고, 돌아올 때 그것만 다시 켠다(나비의 숨긴 임시 날개는 그대로).
    readonly System.Collections.Generic.List<Renderer> _shownRenderers = new System.Collections.Generic.List<Renderer>();
    readonly System.Collections.Generic.List<Collider> _activeColliders = new System.Collections.Generic.List<Collider>();
    int _captureDay = -1;
    float _rearmAt = -1f;
    public int Rearms { get; private set; }

    void RememberBody()
    {
        _shownRenderers.Clear(); _activeColliders.Clear();
        foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) _shownRenderers.Add(r);
        foreach (var c in GetComponentsInChildren<Collider>()) if (c.enabled) _activeColliders.Add(c);
    }

    void HideBody()
    {
        RememberBody();
        foreach (var c in _activeColliders) c.enabled = false;
        foreach (var r in _shownRenderers) r.enabled = false;
    }

    void TryRearm()
    {
        if (!_captured || _claiming || _swinging) return;
        bool newDay = _captureDay >= 0 && Today != _captureDay;
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 0f;
        if (!newDay && (_rearmAt < 0f || hour < _rearmAt)) return;
        _captured = false; _rearmAt = -1f; _captureDay = -1; Rearms++;
        _home = _origin; transform.position = _origin;
        if (_model != null) _model.localScale = _modelScale;
        foreach (var c in _activeColliders) if (c != null) c.enabled = true;
        foreach (var r in _shownRenderers) if (r != null) r.enabled = true;
    }
    public Item Reward => _reward;
    // 2026-11-04 곤충 종류(Content-insects-plan B): 이름은 잡히는 아이템 표시 이름(나비·장수풍뎅이…), 행동은 종별 값(기본 = 나비 그대로).
    public string Noun => _reward != null ? ItemDisplayName.For(_reward) : "곤충";
    bool _perched;
    float _wary, _reach = NetReach, _waryReadyAt;
    public bool Perched => _perched;
    public float Reach => _reach;
    public int WaryStartles { get; private set; }

    // perched: 떠다니지 않고 자리에 붙어 있다(날갯짓은 놀라 날 때만). wary > 0: 그 거리 안에서 플레이어가 달리면 먼저 날아간다.
    public void ConfigureSpecies(bool perched, float wary, float reach)
    {
        _perched = perched; _wary = Mathf.Max(0f, wary); _reach = reach > 0f ? reach : NetReach;
    }

    static bool HasBatchim(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        char c = word[word.Length - 1];
        return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0;
    }
    // 받침에 맞춘 조사: Josa("나비", "을", "를") → "나비를", Josa("장수풍뎅이", "이", "가") → "장수풍뎅이가".
    public static string Josa(string word, string withBatchim, string withoutBatchim) => word + (HasBatchim(word) ? withBatchim : withoutBatchim);
    public bool Fleeing => Time.time - _fleeStartedAt < _flightSeconds;

    // 놀람 없이 새 자리로 날아간다(나는 동안은 잡을 수 없다). 처음 자리(_origin)는 그대로라 잡힌 뒤에는 처음 자리로 돌아온다.
    public bool FlyTo(Vector3 to, float seconds, float arc, float wobble)
    {
        if (_captured || _claiming || _swinging || Fleeing || !isActiveAndEnabled) return false;
        _flightSeconds = Mathf.Max(.3f, seconds); _flightArc = arc; _flightWobble = wobble;
        _fleeStartedAt = Time.time;
        _fleeFrom = transform.position;
        _home = to;
        _fleeTo = _perched ? to : to + Drift(_elapsed + _phase + _flightSeconds);
        return true;
    }

    // 모델에 날개가 따로 있다(LeftWing/RightWing 축 아래에 날개 메시). 날갯짓은 축 회전만 하고 몸 폭을 접지 않는다.
    public void UseOwnWings() => _ownWings = true;
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
        HideIfCaught(); // 이어하기에서 기록이 먼저 되돌려졌으면 같은 날 잡은 개체는 놓지 않는다
    }

    static Vector3 Drift(float t) =>
        new Vector3(Mathf.Sin(t * .65f) * RoamRadius, Mathf.Sin(t * 2f) * .08f, Mathf.Cos(t * .43f) * RoamRadius);

    void Update()
    {
        if (_captured) { TryRearm(); return; }
        _elapsed += Time.deltaTime;
        // Bounded idle drift, with no global random state or navigation ownership.
        float t = _elapsed + _phase;
        Vector3 before = transform.position;
        WatchForRunner();
        if (Fleeing)
        {
            float k = Mathf.Clamp01((Time.time - _fleeStartedAt) / _flightSeconds);
            Vector3 side = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(_fleeTo - _fleeFrom, Vector3.up)).normalized;
            transform.position = Vector3.Lerp(_fleeFrom, _fleeTo, Mathf.SmoothStep(0f, 1f, k)) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * _flightArc)
                + side * (Mathf.Sin(k * Mathf.PI * 3f) * Mathf.Sin(k * Mathf.PI) * _flightWobble);
            Flap(t * 2.2f);
        }
        else if (_perched)
        {
            transform.position = _home;
            if (_model != null) _model.localScale = _modelScale;
            if (_ownWings)
            {
                if (_leftWing != null) _leftWing.localRotation = Quaternion.identity;
                if (_rightWing != null) _rightWing.localRotation = Quaternion.identity;
            }
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

    // 조심스러운 곤충: 플레이어가 가까이에서 달리면(걷기 4.2 · 달리기 ×1.6) 잡기 전에 날아간다. 내려앉은 뒤 2초는 다시 놀라지 않는다.
    PlayerController _runner;
    int _fastFrames;
    void WatchForRunner()
    {
        if (_wary <= 0f || Fleeing || _claiming || _swinging || Time.time < _waryReadyAt) { _fastFrames = 0; return; }
        var inventory = Inventory.instance;
        if (inventory == null) return;
        if (_runner == null) _runner = inventory.GetComponent<PlayerController>();
        // reg224: 한 프레임짜리 속도 튐(순간이동 뒤 지면 밀어내기·계단 보정)도 '달려옴'으로 읽혀 방아깨비·반딧불이가 날아갔다 →
        // 실제 달리기처럼 3프레임 넘게 이어질 때만 놀란다.
        _fastFrames = _runner != null && _runner.ActualPlanarSpeed > 4.6f ? _fastFrames + 1 : 0;
        if (_fastFrames < 3) return;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - inventory.transform.position, Vector3.up);
        if (delta.sqrMagnitude > _wary * _wary) return;
        WaryStartles++;
        Say($"빨리 다가가서 {Josa(Noun, "이", "가")} 날아갔어요. 천천히 다가가 보세요.", true);
        Startle(inventory.transform.position);
        _waryReadyAt = Time.time + FleeSeconds + 2f;
    }

    void Flap(float t)
    {
        float flap = Mathf.Sin(t * 15f) * 35f;
        if (_leftWing != null) _leftWing.localRotation = Quaternion.Euler(0, 0, flap);
        if (_rightWing != null) _rightWing.localRotation = Quaternion.Euler(0, 0, -flap);
        // 단일 메시 모델은 날개 폭을 접었다 펴서 날갯짓을 보인다.
        if (_model != null && !_ownWings)
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
        { Say($"잠자리채를 들면 {Josa(Noun, "을", "를")} 잡을 수 있어요.", false); return; }
        if (!InReach(interactor, 2.05f, .75f))
        { Say($"{Noun}에게 다가가 바라보세요.", false); return; }
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
        if (!InReach(interactor, _reach, .6f))
        {
            Say($"빗나갔어요! {Josa(Noun, "이", "가")} 날아갔어요.", true);
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
            Say($"가방이 가득 차서 {Josa(Noun, "을", "를")} 놓아줬어요.", true);
            Startle(interactor.transform.position);
            yield break;
        }
        _captured = true;
        int caughtToday = RecordCaught(name);
        _captureDay = Today;
        float nowHour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 0f;
        _rearmAt = caughtToday < DailyCap && nowHour + RearmHours < 24f ? nowHour + RearmHours : -1f;
        RememberBody();
        Say($"{Josa(Noun, "을", "를")} 잡았어요!", false);
        GatherFeedback.Catch();
        ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        DemoHotbarPreference.Prefer(_reward); // P8: 잡은 나비도 바로 들고 진열할 수 있게 핫바 우선
        FirstDayWorldPresentation.Acquired(_reward, 1, transform.position);
        foreach (var collider in _activeColliders) collider.enabled = false;
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
        foreach (var renderer in _shownRenderers) if (renderer != null) renderer.enabled = false;
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
        _flightSeconds = FleeSeconds; _flightArc = 1.1f; _flightWobble = 0f;
        _fleeStartedAt = Time.time;
        _fleeFrom = transform.position;
        _home = next;
        _fleeTo = _home + Drift(_elapsed + _phase + FleeSeconds);
    }

    // 결과(빗나감·놓아줌·포획)는 토스트/획득 표시로 알리고, [E] 프롬프트에는 다음 행동만 둔다.
    public string GetInteractPrompt() => Fleeing && !_captured ? $"{Noun} · 내려앉을 때까지 기다려요" : $"{Noun} · 잠자리채 휘두르기";
}
