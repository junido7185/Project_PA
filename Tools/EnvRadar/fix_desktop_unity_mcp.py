"""데스크탑 앱 설정에서 stdio unityMCP 항목을 지운다 (2026-11-09).

데스크탑 앱은 켜져 있는 동안 설정을 메모리에 들고 있다가 파일을 다시 쓴다.
그래서 앱이 켜진 채로 지우면 되살아난다(11-09 15:25:23에 실제로 복원됨).
앱을 트레이까지 완전히 종료한 뒤, 앱 밖의 PowerShell에서 실행한다.

    C:/Users/sdjsd/AppData/Local/Programs/Python/Python312/python.exe Tools/EnvRadar/fix_desktop_unity_mcp.py [--dry-run]
"""
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

CONFIG = Path(os.environ["APPDATA"]) / "Claude" / "claude_desktop_config.json"
BACKUP_DIR = Path(__file__).resolve().parents[2] / "Logs" / "EnvRedesign-20261109" / "backup"


def desktop_app_running() -> bool:
    # CLI claude.exe와 구분하려고 스토어 설치 경로(WindowsApps\Claude_*)만 본다.
    ps = (
        "Get-Process -Name Claude -ErrorAction SilentlyContinue | "
        "Where-Object { $_.Path -like '*\\WindowsApps\\Claude_*' } | "
        "Measure-Object | Select-Object -ExpandProperty Count"
    )
    out = subprocess.run(["powershell", "-NoProfile", "-Command", ps], capture_output=True, text=True)
    return out.stdout.strip() not in ("", "0")


def main() -> int:
    dry = "--dry-run" in sys.argv
    if not CONFIG.exists():
        print(f"설정 파일이 없습니다: {CONFIG}")
        return 1
    data = json.loads(CONFIG.read_text(encoding="utf-8"))
    servers = data.get("mcpServers", {})
    if "unityMCP" not in servers:
        print("unityMCP 항목이 이미 없습니다. 할 일 없음.")
        return 0
    print("지울 항목:", json.dumps(servers["unityMCP"], ensure_ascii=False))
    if dry:
        print("--dry-run: 파일을 바꾸지 않았습니다.")
        return 0
    if desktop_app_running():
        print("데스크탑 앱이 아직 켜져 있습니다. 트레이 아이콘에서 완전히 종료한 뒤 다시 실행하세요.")
        return 2
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    backup = BACKUP_DIR / f"claude_desktop_config.before-unity-mcp-fix-{datetime.now():%Y%m%d-%H%M%S}.json"
    shutil.copy2(CONFIG, backup)
    del servers["unityMCP"]
    CONFIG.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"완료. 백업: {backup}")
    print("이제 데스크탑 앱을 켜고, 다시 이 스크립트를 --dry-run으로 돌려 항목이 없는지 확인하세요.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
