using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

// Presentation adapter for existing NavMeshAgent links. Destinations, production,
// shopping and home routines remain with their existing authorities.
[DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
public sealed class FirstDayStepTraversal : MonoBehaviour
{
    NavMeshAgent _agent;
    bool _previousAutomatic, _jumping;
    Vector3 _from, _to;
    float _elapsed, _duration, _clearance;
    public bool IsJumping => _jumping;
    public float Progress => _jumping ? Mathf.Clamp01(_elapsed/_duration) : 0f;
    public int CompletedSteps { get; private set; }

    void OnEnable()
    {
        _agent=GetComponent<NavMeshAgent>();
        _previousAutomatic=_agent.autoTraverseOffMeshLink;
        _agent.autoTraverseOffMeshLink=false;
    }
    void OnDisable()
    {
        if (_agent!=null) _agent.autoTraverseOffMeshLink=_previousAutomatic;
        _jumping=false;
    }
    void Update()
    {
        if (_agent==null || !_agent.enabled || !_agent.isOnNavMesh || Time.timeScale<=0f) return;
        if (!_agent.isOnOffMeshLink) { _jumping=false; return; }
        if (!_jumping)
        {
            var data=_agent.currentOffMeshLinkData;
            var owner=data.owner as NavMeshLink;
            if (owner==null || !owner.name.StartsWith("DemoTerraceStepLink_",System.StringComparison.Ordinal))
            {
                // Existing level sector seams retain their normal walk crossing.
                _from=transform.position; _to=data.endPos+Vector3.up*_agent.baseOffset;
                transform.position=Vector3.MoveTowards(_from,_to,Mathf.Max(.1f,_agent.speed)*Time.deltaTime);
                if ((transform.position-_to).sqrMagnitude<.0025f) _agent.CompleteOffMeshLink();
                return;
            }
            if (_agent.isStopped) return;
            _from=transform.position; _to=data.endPos+Vector3.up*_agent.baseOffset;
            _elapsed=0f;
            _duration=Mathf.Clamp(Vector3.ProjectOnPlane(_to-_from,Vector3.up).magnitude/Mathf.Max(.1f,_agent.speed),.45f,.85f);
            // A squared arc clears the upper block before crossing its face in
            // either direction. No line through the cliff and no Warp call.
            _clearance=.35f+Mathf.Abs(_to.y-_from.y)*.7f;
            _jumping=true;
        }
        _elapsed+=Time.deltaTime;
        float t=Mathf.Clamp01(_elapsed/_duration);
        transform.position=Vector3.Lerp(_from,_to,t)+Vector3.up*(4f*t*(1f-t)*_clearance);
        Vector3 forward=Vector3.ProjectOnPlane(_to-_from,Vector3.up);
        if(forward.sqrMagnitude>.001f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(forward),12f*Time.deltaTime);
        if(t>=1f) { _agent.CompleteOffMeshLink(); CompletedSteps++; _jumping=false; }
    }
}
