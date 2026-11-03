// 도구 단계 표(코드 데이터) — Docs/01_GAME_DESIGN/Content/TOOL_PROGRESSION.md.
// 힘: 한 번 휘두를 때 바위·나무를 깎는 양. 바위는 (힘 − 단단함 + 1), 나무는 힘만큼 깎인다. 힘이 단단함보다 낮으면 튕긴다.
// 전문 조건: 철 단계 도구는 해당 전문 분야를 고른 개척자만 만든다. 구리 단계는 누구나.
public static class ToolPower
{
    public const int CopperPickaxe = 2022, IronAxe = 2026, IronOre = 2028, CopperBar = 2029, CopperAxe = 2030, IronPickaxe = 2031;

    public static int For(Item item)
    {
        if (item == null || item.toolType == ToolType.None) return 0;
        switch (item.id)
        {
            case CopperPickaxe: case CopperAxe: return 2;
            case IronPickaxe: case IronAxe: return 3;
            default: return 1;
        }
    }

    // 이 단단함의 바위를 캘 수 있는 가장 약한 곡괭이 이름(튕길 때 안내).
    public static string PickaxeFor(int hardness) => hardness <= 1 ? "곡괭이" : hardness == 2 ? "구리 곡괭이" : "철 곡괭이";

    public static DemoSpecialization RequiredRoot(Item output)
    {
        if (output == null) return DemoSpecialization.None;
        switch (output.id)
        {
            case IronPickaxe: return DemoSpecialization.Mining;
            case IronAxe: return DemoSpecialization.Forestry;
            default: return DemoSpecialization.None;
        }
    }
}
