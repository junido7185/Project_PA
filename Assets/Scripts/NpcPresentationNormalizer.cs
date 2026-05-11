using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class NpcPresentationNormalizer : MonoBehaviour
{
    public enum NpcAnimationMode
    {
        RootOnlyProcedural,
        HumanoidProcedural,
        HumanoidController
    }

    public const float VisualScale = 2f;
    public const float ColliderRadius = 0.6f;
    public const float ColliderHeight = 3.6f;
    public const float AgentRadius = 0.55f;
    public const float AgentHeight = 3.6f;

    public RuntimeAnimatorController animatorController;
    public NpcAnimationMode animationMode = NpcAnimationMode.HumanoidProcedural;
    public bool applyOnAwake = true;
    public bool proceduralMoveBob = true;
    [Range(0f, 0.2f)] public float proceduralBobAmplitude = 0.035f;
    public float proceduralBobFrequency = 5.5f;

    NavMeshAgent _agent;
    Transform _visualRoot;
    Vector3 _visualBaseLocalPosition;
    float _bobTime;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (applyOnAwake)
            Normalize(gameObject, animatorController);
        CacheVisualBase();
    }

    void OnEnable()
    {
        _agent = GetComponent<NavMeshAgent>();
        CacheVisualBase();
    }

    void LateUpdate()
    {
        if (animationMode != NpcAnimationMode.RootOnlyProcedural || !proceduralMoveBob)
            return;

        if (_visualRoot == null)
            CacheVisualBase();
        if (_visualRoot == null)
            return;

        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        float planarSpeed = 0f;
        if (_agent != null)
        {
            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;
            planarSpeed = velocity.magnitude;
        }

        Vector3 target = _visualBaseLocalPosition;
        if (planarSpeed > 0.05f)
        {
            float speedFactor = Mathf.Clamp(planarSpeed, 0.75f, 2.25f);
            _bobTime += Time.deltaTime * proceduralBobFrequency * speedFactor;
            target.y += Mathf.Abs(Mathf.Sin(_bobTime)) * proceduralBobAmplitude;
        }
        else
        {
            _bobTime = 0f;
        }

        _visualRoot.localPosition = Vector3.Lerp(_visualRoot.localPosition, target, Time.deltaTime * 12f);
    }

    public static bool Normalize(GameObject npcRoot, RuntimeAnimatorController fallbackController = null)
    {
        if (npcRoot == null) return false;

        bool changed = false;
        Transform root = npcRoot.transform;
        var normalizer = npcRoot.GetComponent<NpcPresentationNormalizer>();
        NpcAnimationMode mode = normalizer != null
            ? normalizer.animationMode
            : NpcAnimationMode.HumanoidProcedural;
        RuntimeAnimatorController targetController = fallbackController != null
            ? fallbackController
            : normalizer != null ? normalizer.animatorController : null;
        bool useHumanoidProcedural = mode == NpcAnimationMode.HumanoidProcedural && HasValidHumanoidAnimator(npcRoot);
        if (mode == NpcAnimationMode.HumanoidProcedural && !useHumanoidProcedural)
        {
            mode = NpcAnimationMode.RootOnlyProcedural;
            if (normalizer != null && normalizer.animationMode != mode)
            {
                normalizer.animationMode = mode;
                changed = true;
            }
        }

        bool useHumanoidController = mode == NpcAnimationMode.HumanoidController && targetController != null;

        if (!Approximately(root.localScale, Vector3.one))
        {
            root.localScale = Vector3.one;
            changed = true;
        }

        Transform visual = FindVisualRoot(root);
        if (visual != null)
        {
            changed |= SetLocalPosition(visual, Vector3.zero);
            changed |= SetLocalRotation(visual, Quaternion.identity);
            changed |= SetLocalScale(visual, Vector3.one * VisualScale);

            Transform primitiveVisual = root.Find("Visual");
            if (primitiveVisual != null && primitiveVisual != visual && primitiveVisual.gameObject.activeSelf)
            {
                primitiveVisual.gameObject.SetActive(false);
                changed = true;
            }
        }
        else
        {
            Transform primitiveVisual = root.Find("Visual");
            if (primitiveVisual != null)
            {
                changed |= SetLocalPosition(primitiveVisual, new Vector3(0f, ColliderHeight * 0.5f, 0f));
                changed |= SetLocalScale(primitiveVisual, Vector3.one * (ColliderHeight * 0.5f));
            }
        }

        var collider = npcRoot.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = npcRoot.AddComponent<CapsuleCollider>();
            changed = true;
        }

        changed |= SetCapsule(collider);

        var agent = npcRoot.GetComponent<NavMeshAgent>();
        if (agent != null)
            changed |= SetAgent(agent);

        if (normalizer != null)
        {
            RuntimeAnimatorController expectedController = useHumanoidController ? targetController : null;
            if (normalizer.animatorController != expectedController)
            {
                normalizer.animatorController = expectedController;
                changed = true;
            }
        }

        foreach (var animator in npcRoot.GetComponentsInChildren<Animator>(true))
        {
            changed |= ConfigureAnimator(
                animator,
                useHumanoidController ? targetController : null,
                useHumanoidController,
                useHumanoidProcedural);
        }

        changed |= EnsureProceduralAnimator(npcRoot, useHumanoidProcedural);

        if (normalizer != null)
        {
            normalizer._agent = agent;
            normalizer.CacheVisualBase();
        }

        return changed;
    }

    void CacheVisualBase()
    {
        _visualRoot = FindVisualRoot(transform);
        if (_visualRoot != null)
            _visualBaseLocalPosition = _visualRoot.localPosition;
    }

    static Transform FindVisualRoot(Transform root)
    {
        Transform named = FindDescendant(root, "CharacterVisual");
        if (named != null) return named;

        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != root) return animator.transform;

        var skinned = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skinned != null)
        {
            var parentAnimator = skinned.GetComponentInParent<Animator>();
            if (parentAnimator != null && parentAnimator.transform != root)
                return parentAnimator.transform;
            return skinned.transform;
        }

        return null;
    }

    static Transform FindDescendant(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindDescendant(root.GetChild(i), name);
            if (result != null) return result;
        }

        return null;
    }

    static bool SetCapsule(CapsuleCollider collider)
    {
        bool changed = false;
        if (Mathf.Abs(collider.radius - ColliderRadius) > 0.001f)
        {
            collider.radius = ColliderRadius;
            changed = true;
        }

        if (Mathf.Abs(collider.height - ColliderHeight) > 0.001f)
        {
            collider.height = ColliderHeight;
            changed = true;
        }

        Vector3 center = new Vector3(0f, ColliderHeight * 0.5f, 0f);
        if (!Approximately(collider.center, center))
        {
            collider.center = center;
            changed = true;
        }

        if (collider.direction != 1)
        {
            collider.direction = 1;
            changed = true;
        }

        if (collider.isTrigger)
        {
            collider.isTrigger = false;
            changed = true;
        }

        return changed;
    }

    static bool SetAgent(NavMeshAgent agent)
    {
        bool changed = false;
        if (Mathf.Abs(agent.radius - AgentRadius) > 0.001f)
        {
            agent.radius = AgentRadius;
            changed = true;
        }

        if (Mathf.Abs(agent.height - AgentHeight) > 0.001f)
        {
            agent.height = AgentHeight;
            changed = true;
        }

        if (Mathf.Abs(agent.baseOffset) > 0.001f)
        {
            agent.baseOffset = 0f;
            changed = true;
        }

        if (agent.stoppingDistance < 0.2f)
        {
            agent.stoppingDistance = 0.2f;
            changed = true;
        }

        if (agent.angularSpeed < 240f)
        {
            agent.angularSpeed = 240f;
            changed = true;
        }

        return changed;
    }

    static bool HasValidHumanoidAnimator(GameObject root)
    {
        if (root == null) return false;

        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator != null && animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid)
                return true;
        }

        return false;
    }

    static bool EnsureProceduralAnimator(GameObject root, bool active)
    {
        if (root == null) return false;

        bool changed = false;
        var procedural = root.GetComponent<NpcHumanoidProceduralAnimator>();
        if (active)
        {
            if (procedural == null)
            {
                procedural = root.AddComponent<NpcHumanoidProceduralAnimator>();
                changed = true;
            }

            if (!procedural.enabled)
            {
                procedural.enabled = true;
                changed = true;
            }

            procedural.RefreshAvatar();
        }
        else if (procedural != null && procedural.enabled)
        {
            procedural.enabled = false;
            changed = true;
        }

        return changed;
    }

    static bool ConfigureAnimator(
        Animator animator,
        RuntimeAnimatorController controller,
        bool useController,
        bool keepEnabledWithoutController)
    {
        if (animator == null) return false;

        bool changed = false;
        if (animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            changed = true;
        }

        if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            changed = true;
        }

        if (animator.updateMode != AnimatorUpdateMode.Normal)
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            changed = true;
        }

        if (useController)
        {
            if (!animator.enabled)
            {
                animator.enabled = true;
                changed = true;
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                changed = true;
            }
        }
        else if (keepEnabledWithoutController)
        {
            if (animator.runtimeAnimatorController != null)
            {
                animator.runtimeAnimatorController = null;
                changed = true;
            }

            ResetAnimatorToRest(animator);

            if (!animator.enabled)
            {
                animator.enabled = true;
                changed = true;
            }
        }
        else
        {
            if (animator.runtimeAnimatorController != null)
            {
                animator.runtimeAnimatorController = null;
                changed = true;
            }

            ResetAnimatorToRest(animator);

            if (animator.enabled)
            {
                animator.enabled = false;
                changed = true;
            }
        }

        return changed;
    }

    static void ResetAnimatorToRest(Animator animator)
    {
        if (animator == null) return;

        bool wasEnabled = animator.enabled;
        if (!animator.enabled)
            animator.enabled = true;

        animator.Rebind();
        animator.Update(0f);

        if (!wasEnabled)
            animator.enabled = false;
    }

    static bool SetLocalPosition(Transform transform, Vector3 value)
    {
        if (Approximately(transform.localPosition, value)) return false;
        transform.localPosition = value;
        return true;
    }

    static bool SetLocalRotation(Transform transform, Quaternion value)
    {
        if (Quaternion.Angle(transform.localRotation, value) < 0.01f) return false;
        transform.localRotation = value;
        return true;
    }

    static bool SetLocalScale(Transform transform, Vector3 value)
    {
        if (Approximately(transform.localScale, value)) return false;
        transform.localScale = value;
        return true;
    }

    static bool Approximately(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude <= 0.000001f;
    }
}
