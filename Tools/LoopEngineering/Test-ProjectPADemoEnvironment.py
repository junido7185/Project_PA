"""Read-only Project P.A. demo configuration/connection check; Python stdlib only.

Never launches Unity, installs packages, invokes gameplay tools or writes approval state.
Exit 0: configuration valid; exit 1: configuration/approval error. Runtime is separate.
"""
import argparse
import json
from pathlib import Path
import subprocess
import sys
import tomllib
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[2]
EXPECTED_ROOT = Path("C:/Users/sdjsd/Desktop/Unity/Project_PA")
ENDPOINT = "http://127.0.0.1:8080/mcp"
TOOLS = {"read_console", "manage_editor", "execute_menu_item", "manage_scene"}
ACTIVE = {"approved_not_started", "preapproved", "active", "in_progress"}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def selection(state, agent):
    """Resolve exactly the selected record, never search old approvals for runnable work."""
    route = state.get("demoProductionRouting", {})
    key = route.get("selectedApproval")
    owner = route.get("unityEditorOwner")
    result = {"approvalKey": key, "unityEditorOwner": owner, "nextTicket": None,
              "disposition": "NO_SELECTED_APPROVAL", "errors": []}
    approval = state.get(key) if isinstance(key, str) else None
    if not isinstance(approval, dict):
        result["errors"].append("Selected approval is missing; historical approvals are not a fallback.")
        return result
    tickets = approval.get("approvedTickets", [])
    definitions = approval.get("ticketDefinitions", {})
    active, through = approval.get("activeTicket"), approval.get("preapprovedThrough")
    if (approval.get("policyId") != "PREAPPROVED_MILESTONE_CONTINUATION"
            or not approval.get("approvalSource") or not approval.get("stopAtMilestone")
            or not isinstance(tickets, list) or not tickets
            or not all(isinstance(ticket, str) and ticket for ticket in tickets)
            or len(set(tickets)) != len(tickets)
            or active not in tickets or through not in tickets
            or tickets.index(active) > tickets.index(through)
            or not isinstance(definitions, dict) or not definitions.get(active)):
        result["disposition"] = "INVALID_APPROVAL"
        result["errors"].append("Selected approval needs source, unique sequence, active scope, definition and stop point.")
        return result
    if approval.get("pushAllowed") is not False:
        result["errors"].append("Selected demo approval must explicitly prohibit push.")
        return result
    if approval.get("status") not in ACTIVE or approval.get("completionReached") is True:
        result["disposition"] = "APPROVAL_NOT_ACTIVE"
        return result
    if route.get("stopAtMilestoneReached") is True:
        result["disposition"] = "MILESTONE_REACHED"
        return result
    if owner not in {"codex", "claude-code"}:
        result["disposition"] = "EDITOR_OWNER_UNASSIGNED"
        return result
    if agent != owner:
        result["disposition"] = "READ_ONLY_OTHER_EDITOR_OWNER"
        return result
    result.update(nextTicket=active, disposition="ELIGIBLE_PENDING_RUNTIME_AND_FILE_REVIEW")
    return result


def rpc(method, params=None, session=None, protocol="2024-11-05", request_id=1):
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream",
               "MCP-Protocol-Version": protocol}
    if session:
        headers["Mcp-Session-Id"] = session
    payload = {"jsonrpc": "2.0", "method": method}
    if not method.startswith("notifications/"):
        payload["id"] = request_id
    if params is not None:
        payload["params"] = params
    with urlopen(Request(ENDPOINT, data=json.dumps(payload).encode(), headers=headers), timeout=3) as response:
        raw = response.read(2_000_001)
        if len(raw) > 2_000_000:
            raise ValueError("MCP response exceeds bounded inspection limit")
        text = raw.decode("utf-8")
        new_session = response.headers.get("Mcp-Session-Id") or session
        if not text.strip():
            return {}, new_session
        if "text/event-stream" in response.headers.get("Content-Type", ""):
            candidates = [json.loads(line[5:].strip()) for line in text.splitlines() if line.startswith("data:")]
            message = next((item for item in candidates if item.get("id") == request_id), {})
        else:
            message = json.loads(text)
        if "error" in message:
            raise ValueError("MCP rejected " + method)
        return message.get("result", {}), new_session


def runtime_check():
    result = {"serverReachable": False, "toolCatalogReady": False, "editorConnectionVerified": False,
              "status": "UNAVAILABLE", "missingTools": sorted(TOOLS)}
    try:
        initialized, session = rpc("initialize", {"protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "project-pa-read-only-doctor", "version": "1"}})
        protocol = initialized.get("protocolVersion", "2024-11-05")
        result["serverReachable"] = True
        rpc("notifications/initialized", session=session, protocol=protocol)
        catalog, _ = rpc("tools/list", session=session, protocol=protocol, request_id=2)
        names = {tool["name"] for tool in catalog.get("tools", [])}
        result["missingTools"] = sorted(TOOLS - names)
        result["toolCatalogReady"] = not result["missingTools"]
        result["status"] = "CATALOG_ONLY_EDITOR_UNVERIFIED" if result["toolCatalogReady"] else "MISSING_TOOLS"
        resources, _ = rpc("resources/list", session=session, protocol=protocol, request_id=3)
        project_resource = next((item.get("uri") for item in resources.get("resources", [])
                                 if "project/info" in item.get("uri", "")
                                 or item.get("name") in {"project_info", "get_project_info"}), None)
        if project_resource:
            contents, _ = rpc("resources/read", {"uri": project_resource}, session=session,
                              protocol=protocol, request_id=4)
            for content in contents.get("contents", []):
                value = json.loads(content.get("text", "{}"))
                info = value.get("data", value)
                if value.get("success") is not False and info.get("projectRoot"):
                    result["editorConnectionVerified"] = Path(info["projectRoot"]).resolve() == ROOT
                    result["status"] = ("PROJECT_CONNECTION_VERIFIED_PLAY_UNVERIFIED"
                                        if result["editorConnectionVerified"] else "WRONG_EDITOR_PROJECT")
    except (OSError, ValueError, KeyError) as exc:
        result["reason"] = type(exc).__name__ + ": " + str(exc)[:180]
    return result


def configuration(agent):
    errors = []
    git_root = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=ROOT,
                              text=True, capture_output=True, check=True).stdout.strip()
    if Path(git_root).resolve() != ROOT or ROOT != EXPECTED_ROOT.resolve():
        errors.append("Script must reside in the actual Project_PA Git root.")
    for folder in ("Assets", "Packages", "ProjectSettings"):
        if not (ROOT / folder).is_dir():
            errors.append("Missing Unity folder: " + folder)
    state = read_json(ROOT / "Automation/LoopEngineering/State/loop-state.json")
    route = selection(state, agent)
    errors.extend(route.pop("errors"))
    codex = tomllib.loads((ROOT / ".codex/config.toml").read_text(encoding="utf-8-sig"))
    claude = read_json(ROOT / ".mcp.json")
    for label, server in (("Codex", codex.get("mcp_servers", {}).get("unityMCP", {})),
                          ("Claude", claude.get("mcpServers", {}).get("unityMCP", {}))):
        if server.get("url") != ENDPOINT or server.get("enabled") is False:
            errors.append(label + " Unity MCP endpoint is missing/disabled or differs from the local endpoint.")
    codex_tools = set(codex.get("mcp_servers", {}).get("unityMCP", {}).get("enabled_tools", []))
    if TOOLS - codex_tools:
        errors.append("Codex is missing production tools: " + ", ".join(sorted(TOOLS - codex_tools)))
    skill = ROOT / ".agents/skills/pa-demo-factory/SKILL.md"
    adapter = ROOT / ".claude/skills/pa-demo-factory/SKILL.md"
    if not skill.is_file() or not adapter.is_file():
        errors.append("Shared demo skill or Claude entrypoint is missing.")
    elif ".agents/skills/pa-demo-factory/SKILL.md" not in adapter.read_text(encoding="utf-8"):
        errors.append("Claude adapter must reference the canonical skill.")
    manifest = read_json(ROOT / "Packages/manifest.json")
    if "com.coplaydev.unity-mcp" not in manifest.get("dependencies", {}):
        errors.append("Existing Unity MCP package is absent; no package is installed by this check.")
    registry = read_json(ROOT / "Automation/LoopEngineering/validator-registry.json")
    if not registry:
        errors.append("Validator registry is empty.")
    for path in ("Docs/00_CURRENT/CODEX_HANDOFF.md", "Docs/00_CURRENT/INTEGRATION_QUEUE.md"):
        if "demoProductionRouting" not in (ROOT / path).read_text(encoding="utf-8"):
            errors.append(path + " does not acknowledge the selected production routing.")
    return {"configurationReady": not errors, "errors": errors, "projectRoot": str(ROOT), "routing": route}


def self_test():
    import copy
    import unittest
    base = {"demoProductionRouting": {"selectedApproval": "chosen", "unityEditorOwner": "claude-code"},
            "chosen": {"policyId": "PREAPPROVED_MILESTONE_CONTINUATION", "approvalSource": "human",
                       "approvedTickets": ["D1", "D2"], "activeTicket": "D1", "preapprovedThrough": "D2",
                       "stopAtMilestone": "review", "status": "approved_not_started", "pushAllowed": False,
                       "ticketDefinitions": {"D1": "scoring"}}}

    class Boundaries(unittest.TestCase):
        def test_other_owner_cannot_take_editor(self):
            self.assertIsNone(selection(base, "codex")["nextTicket"])
        def test_selected_owner_has_exact_candidate(self):
            self.assertEqual(selection(base, "claude-code")["nextTicket"], "D1")
        def test_no_fallback_to_old_active_approval(self):
            data = copy.deepcopy(base)
            data["demoProductionRouting"]["selectedApproval"] = "missing"
            self.assertIsNone(selection(data, "claude-code")["nextTicket"])
        def test_scope_end_is_enforced(self):
            data = copy.deepcopy(base)
            data["chosen"].update(activeTicket="D2", preapprovedThrough="D1")
            self.assertEqual(selection(data, "claude-code")["disposition"], "INVALID_APPROVAL")
        def test_completed_approval_is_terminal(self):
            data = copy.deepcopy(base)
            data["chosen"]["status"] = "complete"
            self.assertIsNone(selection(data, "claude-code")["nextTicket"])
        def test_milestone_gate_is_terminal(self):
            data = copy.deepcopy(base)
            data["demoProductionRouting"]["stopAtMilestoneReached"] = True
            self.assertEqual(selection(data, "claude-code")["disposition"], "MILESTONE_REACHED")
        def test_source_and_definition_are_required(self):
            for key in ("approvalSource", "ticketDefinitions"):
                data = copy.deepcopy(base)
                data["chosen"].pop(key)
                self.assertTrue(selection(data, "claude-code")["errors"])
        def test_duplicate_tickets_are_invalid(self):
            data = copy.deepcopy(base)
            data["chosen"]["approvedTickets"] = ["D1", "D1"]
            self.assertTrue(selection(data, "claude-code")["errors"])
        def test_corrupt_ticket_values_are_rejected(self):
            data = copy.deepcopy(base)
            data["chosen"]["approvedTickets"] = [["D1"]]
            self.assertTrue(selection(data, "claude-code")["errors"])
        def test_tool_catalog_does_not_prove_editor_connection(self):
            from unittest.mock import patch
            answers = [({"protocolVersion": "2024-11-05"}, "session"), ({}, "session"),
                       ({"tools": [{"name": tool} for tool in TOOLS]}, "session"), ({"resources": []}, "session")]
            with patch(__name__ + ".rpc", side_effect=answers):
                self.assertFalse(runtime_check()["editorConnectionVerified"])
        def test_wrong_editor_project_is_rejected(self):
            from unittest.mock import patch
            answers = [({}, "session"), ({}, "session"),
                       ({"tools": [{"name": tool} for tool in TOOLS]}, "session"),
                       ({"resources": [{"uri": "mcpforunity://project/info"}]}, "session"),
                       ({"contents": [{"text": json.dumps({"success": True, "data": {"projectRoot": str(ROOT / "Other")}})}]}, "session")]
            with patch(__name__ + ".rpc", side_effect=answers):
                self.assertEqual(runtime_check()["status"], "WRONG_EDITOR_PROJECT")
        def test_exact_editor_project_can_be_verified_without_gameplay(self):
            from unittest.mock import patch
            answers = [({}, "session"), ({}, "session"),
                       ({"tools": [{"name": tool} for tool in TOOLS]}, "session"),
                       ({"resources": [{"uri": "mcpforunity://project/info"}]}, "session"),
                       ({"contents": [{"text": json.dumps({"success": True, "data": {"projectRoot": str(ROOT)}})}]}, "session")]
            with patch(__name__ + ".rpc", side_effect=answers):
                self.assertTrue(runtime_check()["editorConnectionVerified"])

    return unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(Boundaries)).wasSuccessful()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--agent", choices=("codex", "claude-code"), default="codex")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return 0 if self_test() else 1
    try:
        report = configuration(args.agent)
        report["runtime"] = runtime_check()
        report["runtimeReady"] = report["runtime"]["toolCatalogReady"] and report["runtime"]["editorConnectionVerified"]
        report["nextAction"] = ("Editor owner verifies Project_PA instance, Console, input and actual GameView before production."
                                if report["configurationReady"] else "Correct reported configuration errors within the authorized environment task.")
    except (OSError, ValueError, subprocess.SubprocessError) as exc:
        report = {"configurationReady": False, "runtimeReady": False,
                  "errors": [type(exc).__name__ + ": " + str(exc)[:200]]}
    # ASCII JSON remains valid through both legacy Windows code pages and UTF-8 pipes.
    print(json.dumps(report, ensure_ascii=True, indent=2))
    return 0 if report["configurationReady"] else 1


if __name__ == "__main__":
    sys.exit(main())
