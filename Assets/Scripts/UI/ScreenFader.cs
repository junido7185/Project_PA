using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 🚪 Docs/08 §건물 진입 — 검은 화면 페이드 유틸
// 씬에 단일 인스턴스로 존재하며, BuildingEntrance 같은 워프 상황에서
// FadeOut(검게) → onMidpoint 콜백(텔레포트 수행) → FadeIn(밝아짐) 순서로 호출된다.
// Canvas · Image 자동 생성 — 프리팹을 만들지 않고도 싱글톤 GameObject만 배치하면 동작.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] float fadeDuration = 0.25f; // 한쪽 방향 지속시간 (초)
    [SerializeField] Color fadeColor    = Color.black;

    private CanvasGroup _group;
    private bool _isFading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildOverlay();
    }

    // ── Canvas + CanvasGroup + 전체화면 Image 자동 구성 ─────────────────
    private void BuildOverlay()
    {
        // 이미 자식에 Canvas가 있으면 재사용
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // 항상 최상단
            canvasGo.AddComponent<CanvasScaler>();

            GameObject imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            Image img  = imgGo.AddComponent<Image>();
            img.color  = fadeColor;
            img.raycastTarget = false;

            RectTransform rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;

            _group = canvasGo.AddComponent<CanvasGroup>();
        }
        else
        {
            _group = canvas.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
        }

        _group.alpha        = 0f;
        _group.blocksRaycasts = false;
    }

    // ── 외부 API: 페이드 아웃 → 콜백 → 페이드 인 시퀀스 ──────────────
    public void PlayWarpFade(Action onMidpoint)
    {
        if (_isFading) return;
        StartCoroutine(Co_WarpFade(onMidpoint));
    }

    private IEnumerator Co_WarpFade(Action onMidpoint)
    {
        _isFading = true;
        _group.blocksRaycasts = true;

        yield return Co_FadeTo(1f);
        onMidpoint?.Invoke();
        yield return null; // 텔레포트 프레임 1틱 확정 후 밝아지도록
        yield return Co_FadeTo(0f);

        _group.blocksRaycasts = false;
        _isFading = false;
    }

    private IEnumerator Co_FadeTo(float target)
    {
        float start = _group.alpha;
        float t     = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // Time.timeScale=0 상황에서도 동작
            _group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        _group.alpha = target;
    }
}
