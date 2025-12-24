using UnityEngine;
using TMPro; // 텍스트 매쉬 프로(글자)를 쓰기 위해 필요

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int money = 0;       // 현재 소지금
    public TextMeshProUGUI moneyText; // 화면에 돈을 표시할 텍스트 UI

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        UpdateMoneyUI(); // 시작할 때 0원 표시
    }

    // 돈을 더하거나 빼는 함수
    public void AddMoney(int amount)
    {
        money += amount;
        Debug.Log("💰 돈이 변경됨: " + money + "G");
        UpdateMoneyUI();
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            // "1,000 G" 처럼 쉼표를 찍어주는 포맷 (N0)
            moneyText.text = money.ToString("N0") + " G"; 
        }
    }
}