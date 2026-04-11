using UnityEngine;
using TMPro;

// 역할:
// - 돈 보관/수정 권한은 EconomyService로 이관되었다.
// - GameManager는 이제 기존 코드 호환(AddMoney / instance.money)을 유지하는 얇은 래퍼이자,
//   돈 변경 이벤트를 받아 UI(moneyText)를 갱신하는 표현 레이어 역할만 담당한다.
// - 멀티 전환 시 여기 코드는 그대로 두고, EconomyService 내부만 NetworkVariable 로 바꾸면 된다.
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // 화면에 돈을 표시할 텍스트 UI (Inspector 연결 유지)
    public TextMeshProUGUI moneyText;

    // 기존 코드들이 GameManager.instance.money 로 읽어 가던 것을 깨지 않기 위한 호환 프로퍼티.
    public int money => EconomyService.Instance != null ? EconomyService.Instance.Money : 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnEnable()
    {
        // EconomyService가 아직 생성되지 않았을 수 있으므로 Start에서 본격 구독한다.
    }

    void Start()
    {
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(EconomyService.Instance.Money);
        }
        else
        {
            Debug.LogError("❌ EconomyService 가 씬에 존재하지 않습니다. 씬 루트에 EconomyService 컴포넌트를 추가하세요.");
            UpdateMoneyUI(0);
        }
    }

    void OnDestroy()
    {
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
    }

    // 기존 코드 호환 — 내부적으로 EconomyService 단일 경로를 탄다.
    // 호출 규약상 실패(잔액 부족)는 false 로 보고되지만, 기존 void 시그니처를 유지한다.
    public void AddMoney(int amount)
    {
        if (EconomyService.Instance == null) return;
        EconomyService.Instance.TryModifyMoney(amount, "GameManager.AddMoney");
    }

    private void HandleMoneyChanged(int newValue)
    {
        UpdateMoneyUI(newValue);
    }

    private void UpdateMoneyUI(int value)
    {
        if (moneyText != null)
        {
            moneyText.text = value.ToString("N0") + " G";
        }
    }
}
