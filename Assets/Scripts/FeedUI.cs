using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §5 FeedUI — SmartphoneUI 피드 탭(index 2) 에 붙이는 컴포넌트.
// OnEnable 때마다 SalesLogManager 최신 10건을 ScrollRect 에 표시한다.
public class FeedUI : MonoBehaviour
{
    [Header("설정")]
    public int displayCount = 10;

    [Header("직접 연결 (선택)")]
    public ScrollRect scrollRect;
    public Transform  cardParent;

    readonly List<GameObject> _cards = new List<GameObject>();
    bool _built;

    void OnEnable()
    {
        if (!_built) { BuildScrollView(); _built = true; }
        Refresh();
    }

    void OnDisable() => ClearCards();

    public void Refresh()
    {
        ClearCards();
        if (SalesLogManager.Instance == null) return;

        List<SaleRecord> records = SalesLogManager.Instance.GetRecent(displayCount);
        Transform parent = cardParent != null ? cardParent
                         : scrollRect != null ? scrollRect.content
                         : transform;

        foreach (var r in records)
            _cards.Add(BuildCard(parent, r));
    }

    void ClearCards()
    {
        foreach (var go in _cards)
            if (go != null) Destroy(go);
        _cards.Clear();
    }

    GameObject BuildCard(Transform parent, SaleRecord r)
    {
        var card = new GameObject("FeedCard", typeof(RectTransform), typeof(Image));
        var rt   = (RectTransform)card.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(0f, 60f);
        card.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

        // 아이템명 + 가격
        var topGO  = new GameObject("TopLine", typeof(TextMeshProUGUI));
        var topRT  = (RectTransform)topGO.transform;
        topRT.SetParent(card.transform, false);
        topRT.anchorMin        = new Vector2(0f, 0.5f);
        topRT.anchorMax        = new Vector2(1f, 1f);
        topRT.offsetMin        = new Vector2(10f, 2f);
        topRT.offsetMax        = new Vector2(-10f, -2f);
        var topTmp             = topGO.GetComponent<TextMeshProUGUI>();
        topTmp.text            = $"{r.itemName}  {r.price:N0} G";
        topTmp.fontSize        = 17;
        topTmp.fontStyle       = FontStyles.Bold;
        topTmp.color           = Color.white;
        topTmp.raycastTarget   = false;

        // 구매자 + 날짜
        var botGO  = new GameObject("BotLine", typeof(TextMeshProUGUI));
        var botRT  = (RectTransform)botGO.transform;
        botRT.SetParent(card.transform, false);
        botRT.anchorMin        = new Vector2(0f, 0f);
        botRT.anchorMax        = new Vector2(1f, 0.5f);
        botRT.offsetMin        = new Vector2(10f, 2f);
        botRT.offsetMax        = new Vector2(-10f, -2f);
        var botTmp             = botGO.GetComponent<TextMeshProUGUI>();
        botTmp.text            = $"{r.buyerName}  Day {r.gameDay} {r.gameHour:00}:00";
        botTmp.fontSize        = 14;
        botTmp.color           = new Color(0.7f, 0.7f, 0.7f);
        botTmp.raycastTarget   = false;

        return card;
    }

    void BuildScrollView()
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var vpRT     = (RectTransform)viewport.transform;
        vpRT.SetParent(transform, false);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = Color.clear;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        var cRT     = (RectTransform)content.transform;
        cRT.SetParent(viewport.transform, false);
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing   = 6f;
        vlg.padding   = new RectOffset(6, 6, 6, 6);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect          = gameObject.GetComponent<ScrollRect>() ?? gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = vpRT;
        scrollRect.content  = cRT;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;

        cardParent = cRT;
    }
}
