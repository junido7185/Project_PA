#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Vertical Slice 정비 — 접지/충돌/NPC 실측 감사 (에디트 모드, 씬 무수정).
//
// 출력:
// 1) 플레이어: CharacterController 치수, 모델 자식 오프셋, Animator/Avatar 상태
// 2) NPC: NavMeshAgent 치수/baseOffset/stoppingDistance, Animator 상태
// 3) 지면/길: 이름에 Ground/Path/Road/Plaza 가 들어간 렌더러의 top-Y 와 콜라이더 유무
// 4) 콜라이더 과대 목록: 콜라이더 부피가 렌더러 경계보다 훨씬 크거나, 렌더러 없이 충돌만 있는 것
public static class PA_PhysicsAudit
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";

    [MenuItem("Project PA/Audit/Run Physics Audit")]
    public static void RunPhysicsAudit()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("PA_PhysicsAudit: scene open failed");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        var sb = new StringBuilder();
        AuditPlayer(sb);
        AuditNpcs(sb);
        AuditGround(sb);
        AuditColliders(sb);

        Debug.Log(sb.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void AuditPlayer(StringBuilder sb)
    {
        var player = GameObject.Find("Player");
        sb.AppendLine("=== [AUDIT] PLAYER ===");
        if (player == null) { sb.AppendLine("Player not found"); return; }

        sb.AppendLine($"root pos={player.transform.position} scale={player.transform.localScale}");

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            sb.AppendLine($"CharacterController center={cc.center} height={cc.height:0.###} radius={cc.radius:0.###} skin={cc.skinWidth:0.###} step={cc.stepOffset:0.###} | capsule bottom(worldY)={player.transform.position.y + cc.center.y - cc.height / 2f:0.###}");

        foreach (Transform child in player.transform)
        {
            var renderers = child.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                sb.AppendLine($"child '{child.name}' localPos={child.localPosition} (no renderer)");
                continue;
            }
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            sb.AppendLine($"child '{child.name}' localPos={child.localPosition} localScale={child.localScale} rendererBounds min={b.min} max={b.max} (feetY={b.min.y:0.###})");
        }

        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null)
            sb.AppendLine($"Animator on '{animator.gameObject.name}' controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")} avatar={(animator.avatar != null ? animator.avatar.name : "null")} human={(animator.avatar != null && animator.avatar.isHuman)} applyRootMotion={animator.applyRootMotion}");
        else
            sb.AppendLine("Animator: none");
    }

    static void AuditNpcs(StringBuilder sb)
    {
        sb.AppendLine("=== [AUDIT] NPC ===");
        var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            var agent = npc.GetComponent<NavMeshAgent>();
            var animator = npc.GetComponentInChildren<Animator>(true);
            string agentInfo = agent != null
                ? $"agent radius={agent.radius:0.##} height={agent.height:0.##} baseOffset={agent.baseOffset:0.##} stop={agent.stoppingDistance:0.##} speed={agent.speed:0.##} angular={agent.angularSpeed:0} avoid={agent.obstacleAvoidanceType}"
                : "no agent";
            string animInfo = animator != null
                ? $"anim={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")} human={(animator.avatar != null && animator.avatar.isHuman)}"
                : "no animator";
            sb.AppendLine($"NPC '{npc.name}' pos={npc.transform.position} | {agentInfo} | {animInfo}");
        }
        sb.AppendLine($"NPC total={npcs.Length}");
    }

    static void AuditGround(StringBuilder sb)
    {
        sb.AppendLine("=== [AUDIT] GROUND/PATH ===");
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (!(n.Contains("ground") || n.Contains("path") || n.Contains("road") || n.Contains("plaza") || n.Contains("walk")))
                continue;

            var col = r.GetComponent<Collider>();
            sb.AppendLine($"'{r.gameObject.name}' topY={r.bounds.max.y:0.###} bottomY={r.bounds.min.y:0.###} size={r.bounds.size} collider={(col != null ? col.GetType().Name : "NONE")}");
        }
    }

    static void AuditColliders(StringBuilder sb)
    {
        sb.AppendLine("=== [AUDIT] OVERSIZED / INVISIBLE COLLIDERS ===");
        int flagged = 0;
        foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col == null || !col.enabled) continue;
            if (col.GetComponent<CharacterController>() != null) continue;
            if (col.GetComponentInParent<NpcController>() != null) continue;

            var renderers = col.GetComponentsInChildren<Renderer>(true);
            Bounds cb = col.bounds;
            string path = GetPath(col.transform);
            string trig = col.isTrigger ? " TRIGGER" : "";

            if (renderers.Length == 0)
            {
                // 렌더러가 하나도 없는 순수 충돌체 — 보이지 않는 벽 후보.
                sb.AppendLine($"[NO-RENDERER]{trig} '{path}' {col.GetType().Name} bounds size={cb.size} center={cb.center}");
                flagged++;
                continue;
            }

            Bounds rb = renderers[0].bounds;
            foreach (var r in renderers) rb.Encapsulate(r.bounds);

            float cv = Mathf.Max(0.001f, cb.size.x * cb.size.y * cb.size.z);
            float rv = Mathf.Max(0.001f, rb.size.x * rb.size.y * rb.size.z);
            bool muchBigger = cv > rv * 2.0f
                || cb.size.x > rb.size.x + 1.0f
                || cb.size.z > rb.size.z + 1.0f;

            if (muchBigger)
            {
                sb.AppendLine($"[OVERSIZED]{trig} '{path}' {col.GetType().Name} colSize={cb.size} vs visSize={rb.size}");
                flagged++;
            }
        }
        sb.AppendLine($"flagged={flagged}");
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
