#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  PA_ErrorTracker — 런타임/에디터 에러 자동 캡처·분석
//  메뉴: P.A. System > 🐛 Error Tracker (priority = 6)
//
//  설계 의도:
//   • Application.logMessageReceivedThreaded 훅으로 실시간 캡처.
//   • 스택 trace 의 (at File.cs:line) 패턴 파싱 → 클릭 시 IDE 점프.
//   • NullReference / KeyNotFound / MissingReference 같은 흔한 패턴은
//     "어떤 필드가 누락 됐는지" 자동 추론 + 해결 힌트 제공.
//   • [InitializeOnLoad] 로 에디터 시작 시 자동 등록 → 누구도 수동 활성 안 해도 됨.
//   • 무한 스팸 방지: 같은 에러는 카운트만 누적, 최대 200개 버퍼.
//
//  자주 발생하는 패턴 사전:
//   - NullReferenceException at NpcDialogue → dialogueData 미연결
//   - KeyNotFoundException in HiringService.GetCandidate → availableCandidates 비어있음
//   - MissingReferenceException → SerializedField 가 삭제된 객체 가리킴
//   - UnassignedReferenceException → Inspector 슬롯이 None
// ═══════════════════════════════════════════════════════════════════════════
[InitializeOnLoad]
public class PA_ErrorTracker : EditorWindow
{
    // ── 메뉴 진입 ─────────────────────────────────────────────────────────────
    [MenuItem("P.A. System/🐛 Error Tracker", priority = 6)]
    public static void Open()
    {
        var w = GetWindow<PA_ErrorTracker>("🐛 에러 트래커");
        w.minSize = new Vector2(560, 600);
        w.Show();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  정적: 에디터 로드 시 훅 자동 등록
    // ══════════════════════════════════════════════════════════════════════
    static PA_ErrorTracker()
    {
        // 두 번 등록 방지
        Application.logMessageReceivedThreaded -= OnLog;
        Application.logMessageReceivedThreaded += OnLog;
    }

    // ── 캡처된 항목 ───────────────────────────────────────────────────────────
    [Serializable]
    public class CapturedError
    {
        public DateTime Time;
        public LogType  Type;
        public string   Message;
        public string   Stack;
        public int      Count = 1;          // 같은 에러 누적
        public string   FirstFile;          // 점프용
        public int      FirstLine;
        public string   Diagnosis;          // 자동 분석 한 줄
        public string   Hint;               // 해결 힌트
        public string   Hash;               // 중복 제거 키
    }

    static readonly List<CapturedError> _buffer = new();
    const int MAX_BUFFER = 200;

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log) return;        // 일반 Debug.Log 는 스킵
        if (type == LogType.Warning && _filterErrorsOnly) return;
        // 자기 자신이 찍는 로그 무시
        if (condition.StartsWith("[PA ")) return;

        var hash = HashError(condition, stackTrace);

        lock (_buffer)
        {
            // 동일 에러 누적
            var existing = _buffer.FirstOrDefault(e => e.Hash == hash);
            if (existing != null)
            {
                existing.Count++;
                existing.Time = DateTime.Now;
                return;
            }

            var (file, line) = ParseFirstFrame(stackTrace);
            var (diag, hint) = AnalyzePattern(condition, stackTrace);

            _buffer.Insert(0, new CapturedError
            {
                Time      = DateTime.Now,
                Type      = type,
                Message   = condition,
                Stack     = stackTrace ?? "",
                FirstFile = file,
                FirstLine = line,
                Diagnosis = diag,
                Hint      = hint,
                Hash      = hash,
            });
            if (_buffer.Count > MAX_BUFFER) _buffer.RemoveAt(_buffer.Count - 1);
        }

        // 윈도우가 열려있으면 다시 그리기
        if (HasOpenInstances<PA_ErrorTracker>())
            EditorApplication.delayCall += () => GetWindow<PA_ErrorTracker>().Repaint();
    }

    static bool _filterErrorsOnly = false; // OnLog 가 즉시 참조하기 위해 static

    // ── 패턴 분석 (자주 발생 에러 → 해결 힌트) ──────────────────────────────────
    static (string diagnosis, string hint) AnalyzePattern(string msg, string stack)
    {
        // NullReferenceException — 어느 필드가 null 인지 추정
        if (msg.Contains("NullReferenceException"))
        {
            // NpcDialogue.Interact()
            if (stack != null && stack.Contains("NpcDialogue"))
                return ("NpcDialogue.dialogueData 미연결",
                        "씬의 NPC GameObject Inspector → NpcDialogue → Dialogue Data 슬롯에 " +
                        "Resources/Dialogues/Dialogue_*.asset 드래그. " +
                        "또는 Dev Console > 🩹 자동 수리 클릭.");

            if (stack != null && stack.Contains("HiringService"))
                return ("HiringService.availableCandidates 비어있음 또는 null",
                        "Resources/Candidates/ 에셋이 없으면 Dev Console > 누락 데이터 보완 실행. " +
                        "에셋이 있으면 자동 수리가 자동 채움.");

            if (stack != null && stack.Contains("ItemRegistry"))
                return ("ItemRegistry.allItems 비어있음",
                        "Inspector 에서 Items/ 폴더 드래그 또는 자동 수리 실행.");

            if (stack != null && stack.Contains("ProducerNpcController"))
                return ("ProducerNpcController.productionData 또는 workSpot 미연결",
                        "씬의 NPC_Farmer/Miner/Lumberjack/Fisher Inspector 에서 " +
                        "Production Data 와 Work Spot Transform 연결.");

            if (stack != null && stack.Contains("SpecialistNpcController"))
                return ("SpecialistNpcController.targetWorkbench 미연결",
                        "Chef/Blacksmith/Tailor/Carpenter NPC 의 Target Workbench 슬롯에 " +
                        "Workbench_Kitchen/Forge/SewingTable/Basic 연결.");

            if (stack != null && stack.Contains("PlayerInteraction"))
                return ("PlayerInteraction.farmlandPrefab 또는 Camera 참조 누락",
                        "Player Inspector > PlayerInteraction 컴포넌트의 빈 슬롯 확인.");

            if (stack != null && stack.Contains("InventoryUI") || (stack != null && stack.Contains("HotbarUI")))
                return ("UI 슬롯 프리팹 또는 Inventory 참조 누락",
                        "Dev Console > UI 자동 구축 재실행 또는 자동 수리.");

            return ("NullReferenceException — Inspector 참조 누락",
                    "Console 의 스택 trace 첫 줄을 클릭 → 해당 클래스의 [SerializeField] 슬롯 확인.");
        }

        // MissingReferenceException — 객체가 삭제됨
        if (msg.Contains("MissingReferenceException"))
            return ("이미 Destroy 된 객체 참조",
                    "씬을 다시 로드하거나 Dev Console > 모두 자동 구축 (Ctrl+R) 실행.");

        // UnassignedReferenceException
        if (msg.Contains("UnassignedReferenceException"))
            return ("Inspector 슬롯이 None",
                    "스택 trace 의 클래스 Inspector 에서 빈 슬롯 채우기.");

        // KeyNotFoundException
        if (msg.Contains("KeyNotFoundException"))
            return ("Dictionary key 없음 — 데이터 누락",
                    "Resources/Items, Candidates, Dialogues 폴더 에셋이 충분한지 확인. " +
                    "Dev Console > 누락 데이터 보완 실행.");

        // ArgumentNullException
        if (msg.Contains("ArgumentNullException"))
            return ("메서드 인자가 null",
                    "스택 trace 메서드 시그니처에서 null 들어가는 매개변수 확인.");

        // IndexOutOfRangeException
        if (msg.Contains("IndexOutOfRangeException") || msg.Contains("ArgumentOutOfRangeException"))
            return ("배열/리스트 인덱스 범위 초과",
                    "Inventory.size, Hotbar 슬롯 수, ShopSlot 배열 크기 확인.");

        // EventSystem 없음
        if (msg.Contains("EventSystem"))
            return ("EventSystem 누락 — UI 입력 작동 안함",
                    "Dev Console > 🎮 EventSystem 재생성 또는 🩹 자동 수리.");

        // NavMesh
        if (msg.Contains("NavMesh") || msg.Contains("SetDestination"))
            return ("NavMesh 미생성",
                    "Hierarchy > Ground 선택 → Inspector > NavMeshSurface > Bake.");

        // 없으면 일반
        return ("자세한 분석 없음", "스택 trace 첫 줄 클릭 → 코드 확인");
    }

    static (string file, int line) ParseFirstFrame(string stack)
    {
        if (string.IsNullOrEmpty(stack)) return (null, 0);
        // Unity 스택 형식: " (at Assets/Scripts/Foo.cs:123)"
        var m = Regex.Match(stack, @"\(at (Assets/[^:]+\.cs):(\d+)\)");
        if (m.Success && int.TryParse(m.Groups[2].Value, out int ln))
            return (m.Groups[1].Value, ln);
        return (null, 0);
    }

    static string HashError(string msg, string stack)
    {
        // 메시지 + 스택 첫 줄로 해시
        string firstLine = "";
        if (!string.IsNullOrEmpty(stack))
        {
            int nl = stack.IndexOf('\n');
            firstLine = nl > 0 ? stack.Substring(0, nl) : stack;
        }
        return (msg + "|" + firstLine).GetHashCode().ToString();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GUI
    // ══════════════════════════════════════════════════════════════════════
    Vector2 _scroll;
    bool _autoScrollNew = true;
    string _searchFilter = "";
    bool _showInfo = false;
    bool _showWarn = true;
    bool _showError = true;
    HashSet<int> _expanded = new();

    void OnEnable() => EditorApplication.update += Tick;
    void OnDisable() => EditorApplication.update -= Tick;
    void Tick() { Repaint(); }

    void OnGUI()
    {
        DrawHeader();
        DrawStats();
        DrawFilters();
        DrawSeparator();
        DrawList();
    }

    void DrawHeader()
    {
        var hdr = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🐛 Project P.A. — 에러 트래커", hdr, GUILayout.Height(28));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🗑 비우기", GUILayout.Width(80)))
        { lock (_buffer) _buffer.Clear(); _expanded.Clear(); }
        if (GUILayout.Button("📋 클립보드 복사", GUILayout.Width(110)))
            CopyAllToClipboard();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"실시간 캡처 — 동일 에러 자동 누적 — 최대 {MAX_BUFFER}건 버퍼",
            EditorStyles.miniLabel);
    }

    void DrawStats()
    {
        int err  = _buffer.Count(e => e.Type == LogType.Exception || e.Type == LogType.Error || e.Type == LogType.Assert);
        int warn = _buffer.Count(e => e.Type == LogType.Warning);
        int totalCount = _buffer.Sum(e => e.Count);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (err == 0 && warn == 0)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.5f, 1f, 0.5f);
            EditorGUILayout.LabelField("✅ 캡처된 에러 없음 — 깨끗한 상태!", EditorStyles.boldLabel);
            GUI.color = prev;
        }
        else
        {
            EditorGUILayout.LabelField(
                $"❌ 에러 {err}개 (발생 {_buffer.Where(e=>e.Type!=LogType.Warning).Sum(e=>e.Count)}회)    " +
                $"⚠️ 경고 {warn}개    🧮 총 누적 {totalCount}회",
                EditorStyles.boldLabel);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawFilters()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(18));
        _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.MinWidth(120));
        _showError = GUILayout.Toggle(_showError, "❌ 에러", "Button", GUILayout.Width(70));
        _showWarn  = GUILayout.Toggle(_showWarn,  "⚠ 경고", "Button", GUILayout.Width(70));
        _showInfo  = GUILayout.Toggle(_showInfo,  "ℹ 정보", "Button", GUILayout.Width(70));
        _filterErrorsOnly = !_showWarn;
        EditorGUILayout.EndHorizontal();
    }

    void DrawList()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        List<CapturedError> snapshot;
        lock (_buffer) snapshot = _buffer.ToList();

        foreach (var e in snapshot)
        {
            if (!PassFilter(e)) continue;
            DrawErrorRow(e);
        }

        if (snapshot.Count == 0)
            EditorGUILayout.HelpBox(
                "아직 캡처된 에러가 없습니다.\n\n" +
                "▶ Play 모드 진입 후 에러가 발생하면 여기에 자동으로 누적됩니다.\n" +
                "동일 에러는 카운트만 증가합니다.",
                MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    bool PassFilter(CapturedError e)
    {
        bool isErr = e.Type == LogType.Error || e.Type == LogType.Exception || e.Type == LogType.Assert;
        if (isErr && !_showError) return false;
        if (e.Type == LogType.Warning && !_showWarn) return false;
        if (e.Type == LogType.Log && !_showInfo) return false;

        if (!string.IsNullOrEmpty(_searchFilter))
        {
            var s = _searchFilter.ToLowerInvariant();
            if (!e.Message.ToLowerInvariant().Contains(s) &&
                !(e.Stack ?? "").ToLowerInvariant().Contains(s)) return false;
        }
        return true;
    }

    void DrawErrorRow(CapturedError e)
    {
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = e.Type switch
        {
            LogType.Exception => new Color(1f, 0.55f, 0.55f),
            LogType.Error     => new Color(1f, 0.55f, 0.55f),
            LogType.Assert    => new Color(1f, 0.7f, 0.4f),
            LogType.Warning   => new Color(1f, 0.86f, 0.45f),
            _                 => new Color(0.7f, 0.85f, 1f),
        };

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 1번째 줄: 헤더
        EditorGUILayout.BeginHorizontal();
        string emoji = e.Type switch
        {
            LogType.Exception => "💥",
            LogType.Error     => "❌",
            LogType.Assert    => "🚨",
            LogType.Warning   => "⚠️",
            _                 => "ℹ️",
        };
        GUILayout.Label(emoji, GUILayout.Width(22));

        var tStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
        GUILayout.Label(e.Time.ToString("HH:mm:ss"), tStyle, GUILayout.Width(58));

        if (e.Count > 1)
        {
            var c = GUI.color;
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label($"×{e.Count}", EditorStyles.boldLabel, GUILayout.Width(40));
            GUI.color = c;
        }
        else
        {
            GUILayout.Space(40);
        }

        var msgStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
        EditorGUILayout.LabelField(Truncate(e.Message, 200), msgStyle);
        EditorGUILayout.EndHorizontal();

        // 2번째 줄: 자동 진단
        if (!string.IsNullOrEmpty(e.Diagnosis))
        {
            EditorGUILayout.LabelField($"  🔬 진단: {e.Diagnosis}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(e.Hint))
            {
                var hintStyle = new GUIStyle(EditorStyles.miniLabel)
                { wordWrap = true, fontStyle = FontStyle.Italic };
                EditorGUILayout.LabelField($"  💡 해결: {e.Hint}", hintStyle);
            }
        }

        // 3번째 줄: 액션 버튼
        EditorGUILayout.BeginHorizontal();
        if (!string.IsNullOrEmpty(e.FirstFile))
        {
            var loc = $"{e.FirstFile}:{e.FirstLine}";
            if (GUILayout.Button($"📂 {loc}", GUILayout.MaxWidth(420)))
                OpenInIDE(e.FirstFile, e.FirstLine);
        }
        GUILayout.FlexibleSpace();
        bool exp = _expanded.Contains(e.GetHashCode());
        if (GUILayout.Button(exp ? "▲ 스택 숨기기" : "▼ 스택 보기", GUILayout.Width(110)))
        {
            if (exp) _expanded.Remove(e.GetHashCode());
            else _expanded.Add(e.GetHashCode());
        }
        if (GUILayout.Button("🗑", GUILayout.Width(28)))
        {
            lock (_buffer) _buffer.Remove(e);
        }
        EditorGUILayout.EndHorizontal();

        // 펼친 스택
        if (_expanded.Contains(e.GetHashCode()) && !string.IsNullOrEmpty(e.Stack))
        {
            EditorGUILayout.SelectableLabel(e.Stack,
                EditorStyles.textArea, GUILayout.MinHeight(80));
        }

        EditorGUILayout.EndVertical();
        GUI.backgroundColor = prevBg;
        EditorGUILayout.Space(2);
    }

    static void OpenInIDE(string assetRelative, int line)
    {
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetRelative);
        if (obj != null)
            AssetDatabase.OpenAsset(obj, line);
        else
            Debug.LogWarning($"[PA ErrorTracker] 파일을 찾지 못함: {assetRelative}");
    }

    static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "…";
    }

    void CopyAllToClipboard()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# P.A. 에러 트래커 덤프  ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
        sb.AppendLine($"총 {_buffer.Count}개 고유 / {_buffer.Sum(e=>e.Count)}회 누적\n");

        foreach (var e in _buffer)
        {
            sb.AppendLine($"## [{e.Type}] {e.Message}  (×{e.Count})");
            if (!string.IsNullOrEmpty(e.Diagnosis)) sb.AppendLine($"진단: {e.Diagnosis}");
            if (!string.IsNullOrEmpty(e.Hint))      sb.AppendLine($"해결: {e.Hint}");
            if (!string.IsNullOrEmpty(e.FirstFile)) sb.AppendLine($"위치: {e.FirstFile}:{e.FirstLine}");
            if (!string.IsNullOrEmpty(e.Stack))
            {
                sb.AppendLine("```");
                sb.AppendLine(e.Stack);
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[PA ErrorTracker] 클립보드 복사 완료");
    }

    static void DrawSeparator()
    {
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
    }
}
#endif
