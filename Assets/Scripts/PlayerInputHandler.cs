using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;

// 프로젝트 전체 입력의 단일 진입점.
//
// 설계 의도:
// - 레거시 Input.GetKey/GetAxis 를 완전히 제거하고 Unity Input System 으로 통일한다.
// - 다른 스크립트는 이 클래스의 이벤트에만 구독한다 → 키 리바인딩/게임패드 지원 시
//   이 파일 하나만 수정하면 된다.
// - OnBuildPlace 는 UI 위 클릭을 내부에서 필터링하여 발행한다.
// - 이동(MoveInput)은 이벤트(OnMoveChanged)와 프로퍼티(MoveInput) 두 방식 모두 제공한다.
//
// 현재 바인딩:
//   Move       — WASD / 게임패드 좌스틱
//   Interact   — Space
//   Inventory  — I
//   Phone      — P
//   Craft      — C
//   BuildRotate— R
//   BuildPlace — 마우스 좌클릭 (UI 위 제외)
//   HotbarDirect — 숫자키 1~9 (0-based 인덱스 전달)
//   HotbarScroll — 마우스 휠 (양수=아래→다음, 음수=위→이전)
//   Save       — F5
//   Load       — F9
[DefaultExecutionOrder(-50)]
public class PlayerInputHandler : MonoBehaviour
{
    public static PlayerInputHandler Instance { get; private set; }

    // -------- 현재 이동 벡터 (폴링 방식으로도 읽을 수 있도록 프로퍼티 제공) --------
    public Vector2 MoveInput { get; private set; }

    // -------- 이벤트 --------
    // 각 이벤트에 구독하면 키/버튼 입력 시 콜백을 받는다.

    /// <summary>WASD / 스틱 값 변경 시. Vector2(x=수평, y=수직). 정지 시 Vector2.zero 로 호출된다.</summary>
    public event Action<Vector2> OnMoveChanged;

    /// <summary>Space 키 눌림</summary>
    public event Action OnInteractPressed;

    /// <summary>I 키 — 인벤토리 패널 토글</summary>
    public event Action OnInventoryToggle;

    /// <summary>P 키 — 📱 스마트폰 패널 토글 (Docs §레퍼런스.html)</summary>
    public event Action OnPhoneToggle;

    /// <summary>C 키 — 제작 패널 토글</summary>
    public event Action OnCraftToggle;

    /// <summary>R 키 — 건설 고스트 회전</summary>
    public event Action OnBuildRotate;

    /// <summary>마우스 좌클릭 — UI 위 클릭은 내부에서 이미 필터링되어 발행되지 않는다.</summary>
    public event Action OnBuildPlace;

    /// <summary>마우스 휠 스크롤. 값: 양수 = 아래(Next), 음수 = 위(Previous).</summary>
    public event Action<float> OnHotbarScroll;

    /// <summary>숫자키 1~9 눌림. 전달값: 0-based 슬롯 인덱스.</summary>
    public event Action<int> OnHotbarDirectSelect;

    /// <summary>F5 — 저장</summary>
    public event Action OnSave;

    /// <summary>F9 — 로드</summary>
    public event Action OnLoad;

    /// <summary>ESC — 일시정지 토글</summary>
    public event Action OnPauseToggle;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        // -------- 이동 (WASD / 방향키 폴링) --------
        Vector2 move = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1f;
        if (move != MoveInput)
        {
            MoveInput = move;
            OnMoveChanged?.Invoke(MoveInput);
        }

        // -------- 버튼 입력 감지 --------
        if (kb.spaceKey.wasPressedThisFrame)  OnInteractPressed?.Invoke();
        if (kb.iKey.wasPressedThisFrame)      OnInventoryToggle?.Invoke();
        if (kb.pKey.wasPressedThisFrame)      OnPhoneToggle?.Invoke(); // 📱 스마트폰
        if (kb.cKey.wasPressedThisFrame)      OnCraftToggle?.Invoke();
        if (kb.rKey.wasPressedThisFrame)      OnBuildRotate?.Invoke();
        if (kb.f5Key.wasPressedThisFrame)        OnSave?.Invoke();
        if (kb.f9Key.wasPressedThisFrame)        OnLoad?.Invoke();
        if (kb.escapeKey.wasPressedThisFrame)    OnPauseToggle?.Invoke();

        // -------- 핫바 숫자키 1~9 --------
        CheckDigitKey(kb.digit1Key, 0);
        CheckDigitKey(kb.digit2Key, 1);
        CheckDigitKey(kb.digit3Key, 2);
        CheckDigitKey(kb.digit4Key, 3);
        CheckDigitKey(kb.digit5Key, 4);
        CheckDigitKey(kb.digit6Key, 5);
        CheckDigitKey(kb.digit7Key, 6);
        CheckDigitKey(kb.digit8Key, 7);
        CheckDigitKey(kb.digit9Key, 8);

        if (mouse == null) return;

        // -------- 핫바 스크롤 --------
        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) > 0.01f)
            OnHotbarScroll?.Invoke(scrollY);

        // -------- 건설 배치 좌클릭 (UI 위 클릭 제외) --------
        if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            OnBuildPlace?.Invoke();
    }

    // -------- 헬퍼 --------

    private void CheckDigitKey(KeyControl key, int index)
    {
        if (key.wasPressedThisFrame) OnHotbarDirectSelect?.Invoke(index);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
