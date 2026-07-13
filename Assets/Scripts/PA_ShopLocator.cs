using UnityEngine;

// S2 — 상점 앵커 로케이터.
// 실내 상점(PA_StoreInterior, Y+100)이 Shop 컴포넌트로 등록되면서
// FindFirstObjectByType<Shop>() 이 실내를 집어 간판/드레싱/마을변화 앵커가
// 하늘 위로 가버릴 수 있다 → "광장(지상) 상점"을 결정적으로 고르는 단일 경로.
public static class PA_ShopLocator
{
    const float InteriorYThreshold = 50f; // BuildingEntrance Y+100 실내 관례 (Docs/08)

    // 지상(y<50) 상점 중 플레이어와 가장 가까운 것. 없으면 아무 상점이나.
    public static Shop FindPlazaShop()
    {
        var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Vector3 anchor = player != null ? player.transform.position : Vector3.zero;

        Shop best = null;
        float bestSqr = float.MaxValue;
        Shop anyFallback = null;

        foreach (var shop in Object.FindObjectsByType<Shop>(FindObjectsSortMode.None))
        {
            if (shop == null) continue;
            if (anyFallback == null) anyFallback = shop;
            if (shop.transform.position.y > InteriorYThreshold) continue; // 실내 제외

            float sqr = (shop.transform.position - anchor).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = shop;
            }
        }

        return best != null ? best : anyFallback;
    }
}
