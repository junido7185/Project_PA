using System.Collections.Generic;
using System.Text;
using UnityEngine;

// QA 전용(캡처 진단): 대상 루트가 프레임의 어느 단계·실행 순서 구간에서 움직였는지 누적한다. 게임 로직에는 붙지 않는다.
// Update 구간은 실행 순서 경계(-30000, -100, -1, 1, 100, 250, 30000)마다 잘라 기록한다.
[DefaultExecutionOrder(-30000)]
public sealed class PositionPhaseTracer : MonoBehaviour
{
    public float Engine, LateScripts, Between;
    readonly SortedDictionary<int, float> _update = new SortedDictionary<int, float>();
    Vector3 _mark;
    bool _started;

    void Awake()
    {
        gameObject.AddComponent<PositionProbeM100>().Owner = this;
        gameObject.AddComponent<PositionProbeM1>().Owner = this;
        gameObject.AddComponent<PositionProbeP1>().Owner = this;
        gameObject.AddComponent<PositionProbeP100>().Owner = this;
        gameObject.AddComponent<PositionProbeP250>().Owner = this;
        gameObject.AddComponent<PositionPhaseTracerEnd>().Owner = this;
    }

    public void ResetCounters() { Engine = LateScripts = Between = 0f; _update.Clear(); }

    float Step()
    {
        float d = Vector3.ProjectOnPlane(transform.position - _mark, Vector3.up).magnitude;
        _mark = transform.position;
        return d;
    }

    void Update() { float d = Step(); if (_started) Between += d; _started = true; }
    void LateUpdate() => Engine += Step();
    internal void UpdateMark(int order) { float d = Step(); _update.TryGetValue(order, out float v); _update[order] = v + d; }
    internal void LateEnd() => LateScripts += Step();

    public string Summary
    {
        get
        {
            var sb = new StringBuilder("update[");
            foreach (var pair in _update) if (pair.Value > .005f) sb.Append($"..{pair.Key}={pair.Value:0.00} ");
            return sb.Append($"] engine={Engine:0.00} late={LateScripts:0.00} between={Between:0.00}").ToString();
        }
    }
}

public abstract class PositionProbeBase : MonoBehaviour
{
    internal PositionPhaseTracer Owner;
    protected abstract int Order { get; }
    void Update() { if (Owner != null) Owner.UpdateMark(Order); }
}

[DefaultExecutionOrder(-100)] public sealed class PositionProbeM100 : PositionProbeBase { protected override int Order => -100; }
[DefaultExecutionOrder(-1)] public sealed class PositionProbeM1 : PositionProbeBase { protected override int Order => -1; }
[DefaultExecutionOrder(1)] public sealed class PositionProbeP1 : PositionProbeBase { protected override int Order => 1; }
[DefaultExecutionOrder(100)] public sealed class PositionProbeP100 : PositionProbeBase { protected override int Order => 100; }
[DefaultExecutionOrder(250)] public sealed class PositionProbeP250 : PositionProbeBase { protected override int Order => 250; }

[DefaultExecutionOrder(30000)]
public sealed class PositionPhaseTracerEnd : MonoBehaviour
{
    internal PositionPhaseTracer Owner;
    void Update() { if (Owner != null) Owner.UpdateMark(30000); }
    void LateUpdate() { if (Owner != null) Owner.LateEnd(); }
}
