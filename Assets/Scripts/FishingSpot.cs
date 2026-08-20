using System.Collections;
using UnityEngine;

// Player-facing fishing action layered on the existing daily stock-prep state.
public class FishingSpot : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0.2f)] float castDuration = 1.25f;

    DaytimeStockPrepPoint _stockPoint;
    Coroutine _castRoutine;
    bool _isFishing;
    string _lastFeedback = "낚싯대를 드리울 수 있어요.";

    public bool IsFishing => _isFishing;
    public string LastFeedback => _lastFeedback;

    public void Configure(DaytimeStockPrepPoint stockPoint)
    {
        _stockPoint = stockPoint;
    }

    void Awake()
    {
        if (_stockPoint == null)
            _stockPoint = GetComponentInParent<DaytimeStockPrepPoint>();
    }

    void OnDisable()
    {
        CancelCast();
    }

    public void Interact(GameObject interactor)
    {
        if (_isFishing)
            return;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
        {
            _lastFeedback = "지금은 낚시할 수 없어요.";
            Debug.LogWarning("[FishingSpot] Fishing requires a day-prep stock point and loop controller.");
            return;
        }

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
        {
            // Reuse the loop HUD explanation for the wrong phase or a completed catch.
            loop.TryCollectDayPrepStock(_stockPoint, interactor);
            _lastFeedback = loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "오늘 낚시는 마쳤어요."
                : "낚시는 낮 준비 시간에 할 수 있어요.";
            return;
        }

        _isFishing = true;
        _lastFeedback = "낚싯대를 드리웠어요. 찌를 기다리는 중...";
        _castRoutine = StartCoroutine(CastAndCatch(interactor));
    }

    public string GetInteractPrompt()
    {
        if (_isFishing)
            return _lastFeedback;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
            return "해변 낚시터: 이용 불가";

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
            return loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "해변 낚시터: 오늘 낚시 완료"
                : "해변 낚시터: 낮에 다시 오기";

        return "해변 낚시터: 낚싯대 드리우기";
    }

    IEnumerator CastAndCatch(GameObject interactor)
    {
        float firstWait = Mathf.Min(0.35f, castDuration * 0.35f);
        yield return new WaitForSeconds(firstWait);

        _lastFeedback = "찌가 흔들려요...";
        yield return new WaitForSeconds(Mathf.Max(0.05f, castDuration - firstWait));

        _castRoutine = null;
        CompleteCatch(interactor);
    }

    bool CompleteCatch(GameObject interactor)
    {
        _isFishing = false;

        var loop = DayNightShopLoopController.Instance;
        bool caught = loop != null
            && _stockPoint != null
            && loop.TryCollectDayPrepStock(_stockPoint, interactor);

        _lastFeedback = caught
            ? "물고기를 낚아 재고 가방에 담았어요!"
            : "이번에는 낚시를 마치지 못했어요.";
        return caught;
    }

    void CancelCast()
    {
        if (_castRoutine != null)
        {
            StopCoroutine(_castRoutine);
            _castRoutine = null;
        }

        _isFishing = false;
    }

    public void PrepareForStateRestore()
    {
        CancelCast();
    }

#if UNITY_EDITOR
    // Editor smoke tests use the same completion path without waiting on wall-clock time.
    public bool CompleteCatchForValidation(GameObject interactor)
    {
        if (!_isFishing)
            return false;

        CancelCast();
        return CompleteCatch(interactor);
    }
#endif
}
