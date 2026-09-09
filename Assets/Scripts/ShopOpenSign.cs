using UnityEngine;

// CDN-002 — 플레이어가 밤 영업을 시작하는 가게 간판.
//
// 설계 의도 (PROJECT_PA_CREATIVE_NORTH_STAR.md "Shop Operation Fantasy"):
// - 밤에 ShopOpen 단계가 되면 플레이어가 직접 간판으로 가게를 "연다".
// - 영업을 시작해야 손님 NPC 구매가 활성화된다(실제 게이트는 DayNightShopLoopController).
// - Day 1 튜토리얼은 항상 열림으로 처리되어 기존 첫 판매 루트가 보존된다.
// - 기존 IInteractable / PlayerInteraction 구조를 그대로 재사용한다(새 입력 키 없음).
public class ShopOpenSign : MonoBehaviour, IInteractable
{
    PrototypeWorldLabel _label;
    float _nextLabelRefresh;

    void Start()
    {
        EnsureLabel();
        RefreshLabel();
    }

    void Update()
    {
        if (Time.unscaledTime < _nextLabelRefresh) return;
        _nextLabelRefresh = Time.unscaledTime + 0.5f;
        RefreshLabel();
    }

    public void Interact(GameObject interactor)
    {
        if (DayNightShopLoopController.Instance == null)
        {
            Debug.LogWarning("[ShopOpenSign] DayNightShopLoopController is missing.");
            return;
        }

        if (DayNightShopLoopController.Instance.CanPlayerStartNextDay)
            DayNightShopLoopController.Instance.TryStartNextDay();
        else
            DayNightShopLoopController.Instance.TryOpenShop();
        RefreshLabel();
    }

    public string GetInteractPrompt()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop == null) return "가게 간판";
        if (loop.CanPlayerStartNextDay) return "하루 마무리하고 다음 날 시작";
        if (loop.IsTutorialAlwaysOpen) return "가게 간판 (튜토리얼 영업 중)";
        if (loop.PlayerHasOpenedShopToday) return "가게 간판 (영업 중)";
        if (loop.CanPlayerOpenShop) return "영업 시작하기";
        return "가게 간판 (해 지면 영업 가능)";
    }

    void EnsureLabel()
    {
        if (_label != null) return;

        Transform existing = transform.Find("Label");
        GameObject labelGo = existing != null ? existing.gameObject : new GameObject("Label");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        _label = labelGo.GetComponent<PrototypeWorldLabel>() ?? labelGo.AddComponent<PrototypeWorldLabel>();
    }

    void RefreshLabel()
    {
        EnsureLabel();
        if (_label == null) return;

        var loop = DayNightShopLoopController.Instance;
        string status;
        Color color;

        if (loop == null)
        {
            status = "가게 간판";
            color = Color.white;
        }
        else if (loop.CanPlayerStartNextDay)
        {
            status = "정산 완료 · 다음 날 시작";
            color = new Color(0.65f, 0.88f, 1f);
        }
        else if (loop.IsShopOpenForCustomers)
        {
            status = "영업 중";
            color = new Color(0.55f, 1f, 0.6f);
        }
        else if (loop.CanPlayerOpenShop)
        {
            status = "영업 시작 가능";
            color = new Color(1f, 0.9f, 0.5f);
        }
        else
        {
            status = "영업 준비 중";
            color = new Color(0.82f, 0.82f, 0.86f);
        }

        _label.Set($"가게 간판\n{status}", color, 1.3f);
    }
}
