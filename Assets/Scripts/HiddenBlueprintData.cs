using UnityEngine;

// 히든 레시피 — 친밀도가 높은 NPC 에게서 "전수" 받는 가공법.
// Docs/05 §2.3 "관계 기반 해금" 을 구현하는 데이터 단위.
//
// 사용법:
//   Assets > Create > P.A. System > Hidden Blueprint Data 로 에셋을 만들고
//   FriendshipService.hiddenBlueprints 에 등록한다.
//   friendshipId 가 일치하는 NPC 와 requiredFriendshipLevel 이상에 도달하면
//   unlockRecipe 가 FriendshipService 의 잠금 해제 목록에 추가된다.
//
// 설계:
// - RecipeData 자체는 건들지 않는다 (잠금 상태를 외부에서 관리).
// - 잠금 해제 확인은 FriendshipService.IsRecipeUnlocked(recipe) 한 줄로 검사 가능.
// - 같은 recipe 를 여러 NPC 가 별개의 조건으로 풀 수도 있다 (중복 등록 허용).
[CreateAssetMenu(fileName = "New Hidden Blueprint", menuName = "P.A. System/Hidden Blueprint Data")]
public class HiddenBlueprintData : ScriptableObject
{
    [Header("잠금 대상")]
    [Tooltip("이 블루프린트로 해금되는 레시피")]
    public RecipeData unlockRecipe;

    [Header("해금 조건")]
    [Tooltip("전수해줄 NPC 의 friendshipId (NpcDialogue.friendshipId 와 동일)")]
    public string friendshipId = "";

    [Tooltip("이 단계 이상의 친밀도에서 해금된다 (FriendshipService 의 MaxLevel 기준)")]
    [Range(1, 5)] public int requiredFriendshipLevel = 3;

    [Header("UI / 로그")]
    [Tooltip("해금 안내 문구 (비어 있으면 기본 메시지 사용)")]
    [TextArea] public string unlockMessage = "";
}
