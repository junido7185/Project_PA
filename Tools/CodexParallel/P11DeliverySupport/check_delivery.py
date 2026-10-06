"""Read-only Unity candidate/declared-input audit. Does not build or run a game."""
from __future__ import annotations

import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import sys
from urllib.parse import unquote

WORKSPACE = Path(__file__).resolve().parents[3]
OUTPUT_ROOT = WORKSPACE / "Logs/CodexParallel/P11DeliverySupport"
HASH_PATTERN = re.compile(r"[0-9a-fA-F]{64}\Z")


class InputError(ValueError):
    pass


def inside(path, parent):
    return path == parent or parent in path.parents


def signature(path):
    try:
        s = path.stat()
        return (s.st_dev, s.st_ino, s.st_size, s.st_mtime_ns, s.st_ctime_ns, s.st_mode)
    except FileNotFoundError:
        return None


class Reader:
    def __init__(self, root, hook=None):
        self.root = Path(root).resolve()
        if not inside(self.root, WORKSPACE):
            raise InputError("Project root must stay inside the Project_PA workspace")
        if not self.root.is_dir():
            raise InputError("Project root is not a directory")
        self.observed = {}
        self.unstable = set()
        self.hashes = {}
        self.hook = hook

    def path(self, value, base=None):
        if not isinstance(value, str) or not value.strip():
            raise InputError("Expected a nonempty local path")
        text = unquote(value).replace("\\", "/")
        if text.startswith("//") or "://" in text or "\0" in text:
            raise InputError("Network/URL paths are not supported: " + value)
        # Reject NTFS streams and drive-relative paths, including on non-Windows hosts.
        drive = re.match(r"^[A-Za-z]:/", text)
        tail = text[2:] if drive else text
        if ":" in tail:
            raise InputError("Drive-relative/stream path is not allowed: " + value)
        p = Path(text)
        p = p if p.is_absolute() else (base or self.root) / p
        p = p.resolve()
        if not inside(p, self.root):
            raise InputError("Path leaves the project through traversal/link: " + value)
        return p

    def relative(self, path):
        return path.relative_to(self.root).as_posix()

    def observe(self, path):
        path = self.path(str(path))
        current = signature(path)
        if path not in self.observed:
            self.observed[path] = current
        elif self.observed[path] != current:
            self.unstable.add(path)
        return current

    def digest(self, path):
        before = self.observe(path)
        if before is None or not path.is_file():
            return None
        if path in self.hashes and path not in self.unstable:
            return self.hashes[path]
        h = hashlib.sha256()
        try:
            with path.open("rb") as stream:
                opened = os.fstat(stream.fileno())
                for block in iter(lambda: stream.read(1024 * 1024), b""):
                    h.update(block)
                closed = os.fstat(stream.fileno())
        except FileNotFoundError:
            self.unstable.add(path)
            return None
        for st in (opened, closed):
            if (st.st_dev, st.st_ino, st.st_size, st.st_mtime_ns, st.st_ctime_ns, st.st_mode) != before:
                self.unstable.add(path)
        if self.hook is not None:
            self.hook("after_hash", path)
        self.observe(path)
        value = {"path": self.relative(path), "bytes": before[2], "sha256": h.hexdigest()}
        self.hashes[path] = value
        return value

    def text(self, path):
        self.observe(path)
        raw = path.read_bytes()
        self.observe(path)
        try:
            # Existing PowerShell captures may have an explicit UTF-16 BOM.
            encoding = "utf-16" if raw.startswith((b"\xff\xfe", b"\xfe\xff")) else "utf-8-sig"
            return raw.decode(encoding)
        except UnicodeError as exc:
            raise InputError("Invalid text encoding: " + self.relative(path)) from exc

    def metadata(self, evidence, name):
        path = self.path(name, evidence)
        self.observe(path)
        if not path.is_file():
            return None
        try:
            return json.loads(self.text(path))
        except json.JSONDecodeError as exc:
            raise InputError("Malformed JSON: " + self.relative(path)) from exc

    def tree(self, directory):
        files = []
        self.observe(directory)
        if not directory.is_dir():
            return files
        with os.scandir(directory) as entries:
            for entry in sorted(entries, key=lambda x: x.name):
                resolved = self.path(entry.path)
                # Internal links are also observed by resolved path and parent-directory metadata.
                if resolved.is_dir():
                    if entry.is_symlink() or getattr(entry, "is_junction", lambda: False)():
                        raise InputError("Linked directories in candidates are unsupported: " + entry.path)
                    files.extend(self.tree(resolved))
                elif resolved.is_file():
                    self.observe(resolved)
                    files.append(resolved)
        return files

    def finish(self):
        for path, before in list(self.observed.items()):
            if self.path(str(path)) != path or signature(path) != before:
                self.unstable.add(path)


def expected_hash(value):
    if not isinstance(value, str) or not HASH_PATTERN.fullmatch(value):
        raise InputError("Invalid SHA-256 declaration")
    return value.lower()


def records(value, kind):
    if value is None:
        return []
    if isinstance(value, list) and all(isinstance(v, dict) for v in value):
        return value
    if isinstance(value, dict) and isinstance(value.get("files"), list):
        return records(value["files"], kind)
    raise InputError("Unsupported manifest structure: " + kind)


def compare_manifest(reader, value, kind, candidate=None):
    results = []
    seen = set()
    for entry in records(value, kind):
        name = entry.get("path") or entry.get("resolved") or entry.get("source")
        path = reader.path(name)
        if path in seen:
            raise InputError("Duplicate manifest path: " + str(name))
        seen.add(path)
        expected = expected_hash(entry.get("compiledSha256") or entry.get("expected") or entry.get("sha256"))
        algorithm = entry.get("algorithm", entry.get("algorithmGuid", "SHA256"))
        supported = str(algorithm).lower() in ("sha256", "8829d00f-11b8-4213-878b-770e8597ac16")
        if candidate is not None and not inside(path, candidate):
            results.append({"path": reader.relative(path), "status": "MISMATCH", "reason": "Declaration belongs to a different candidate"})
            continue
        actual = reader.digest(path)
        state = "UNVERIFIED" if not supported else "MISSING" if actual is None else "MATCH" if actual["sha256"] == expected else "MISMATCH"
        if actual and "bytes" in entry and (not isinstance(entry["bytes"], int) or isinstance(entry["bytes"], bool)):
            raise InputError("Invalid manifest byte count: " + str(name))
        if actual and "bytes" in entry and actual["bytes"] != entry["bytes"]:
            state = "MISMATCH"
        results.append({"path": reader.relative(path), "status": state, "expectedSha256": expected, "actual": actual})
    return results


def group(rows, available=True):
    counts = dict(Counter(row["status"] for row in rows))
    status = next((s for s in ("UNSTABLE", "MISMATCH", "MISSING", "UNVERIFIED") if counts.get(s)),
                  "MATCH" if rows and available else "UNVERIFIED")
    return {"status": status, "counts": counts, "files": rows}


def inspect(root, candidate_dir, evidence_dir, hook=None):
    reader = Reader(root, hook)
    candidate = reader.path(str(candidate_dir))
    evidence = reader.path(str(evidence_dir))
    files = reader.tree(candidate)
    reader.observe(evidence)
    if not evidence.is_dir():
        raise InputError("Evidence directory is missing")
    exes = [p for p in files if p.parent == candidate and p.suffix.lower() == ".exe" and not p.name.lower().startswith("unitycrashhandler")]
    data_dirs = [p for p in candidate.glob("*_Data") if reader.path(str(p)).is_dir()] if candidate.is_dir() else []
    data = reader.path(str(data_dirs[0])) if len(data_dirs) == 1 else None
    required = []
    def artifact(path, reason):
        actual = reader.digest(path)
        required.append({"path": reader.relative(path), "status": "MATCH" if actual else "MISSING", "reason": reason, "actual": actual})
    if len(exes) == 1:
        artifact(exes[0], "Product executable present; not executed")
        if data is not None and data.name != exes[0].stem + "_Data":
            required.append({"path": reader.relative(data), "status": "MISMATCH", "reason": "EXE/data directory names differ"})
    else:
        required.append({"path": reader.relative(candidate), "status": "MISSING" if not exes else "UNVERIFIED", "reason": "Exactly one product EXE could not be identified"})
    artifact(reader.path(str(candidate / "UnityPlayer.dll")), "Unity player binary")
    if data is None:
        required.append({"path": reader.relative(candidate), "status": "MISSING", "reason": "Exactly one *_Data directory could not be identified"})
    else:
        reader.observe(data)
    game_assembly = reader.path(str(candidate / "GameAssembly.dll"))
    reader.observe(game_assembly)
    mono = reader.path(str(candidate / "MonoBleedingEdge"))
    reader.observe(mono)
    assembly = reader.path(str(data / "Managed/Assembly-CSharp.dll")) if data else None
    if assembly:
        reader.observe(assembly)
    if game_assembly.is_file():
        backend = "IL2CPP marker"
        artifact(game_assembly, "Backend marker; managed Mono files are not required")
    elif mono.is_dir() or (assembly and assembly.is_file()):
        backend = "Mono marker"
        if assembly:
            artifact(assembly, "Known Mono candidate authored assembly")
        artifact(reader.path(str(mono / "EmbedRuntime/mono-2.0-bdwgc.dll")), "Known local Mono runtime layout")
    else:
        backend = "UNVERIFIED"
        required.append({"path": reader.relative(candidate), "status": "UNVERIFIED", "reason": "Backend not identified; no extra files inferred"})

    candidate_manifest = reader.metadata(evidence, "candidate-files-sha256.json")
    candidate_rows = compare_manifest(reader, candidate_manifest, "candidate files", candidate)
    declared_paths = {row["path"] for row in candidate_rows}
    unlisted = sorted(reader.relative(p) for p in files if reader.relative(p) not in declared_paths)
    if unlisted:
        required.append({"path": reader.relative(candidate), "status": "UNVERIFIED", "reason": "Files not covered by candidate manifest", "unlisted": unlisted})
    if not candidate_rows:
        required.append({"path": reader.relative(candidate), "status": "UNVERIFIED", "reason": "No candidate checksum manifest"})
    compiled = reader.metadata(evidence, "compiled-source-checksums.json")
    inputs = reader.metadata(evidence, "build-input-files-sha256.json")
    source_rows = compare_manifest(reader, compiled, "compiled sources")
    input_rows = compare_manifest(reader, inputs, "declared build inputs")

    binding = reader.metadata(evidence, "compiled-assembly-binding.json")
    source_state = reader.metadata(evidence, "source-state.json")
    provenance_rows = []
    if source_state is not None:
        if not isinstance(source_state, dict):
            raise InputError("source-state.json must be an object")
        if "candidatePath" in source_state:
            recorded_candidate = reader.path(source_state["candidatePath"])
            provenance_rows.append({"path": reader.relative(recorded_candidate), "status": "MATCH" if recorded_candidate == candidate else "MISMATCH", "reason": "Declared candidate path"})
    if binding is not None:
        if not isinstance(binding, dict):
            raise InputError("compiled-assembly-binding.json must be an object")
        shipped_path = reader.path(binding["shippedAssembly"]) if "shippedAssembly" in binding else assembly
        if shipped_path and not inside(shipped_path, candidate):
            provenance_rows.append({"path": reader.relative(shipped_path), "status": "MISMATCH", "reason": "Binding points at another candidate"})
        elif shipped_path:
            actual = reader.digest(shipped_path)
            expected = expected_hash(binding.get("shipped") or binding.get("sha256"))
            provenance_rows.append({"path": reader.relative(shipped_path), "status": "MISSING" if not actual else "MATCH" if actual["sha256"] == expected else "MISMATCH", "reason": "Rehashed shipped assembly versus declared binding", "actual": actual, "expectedSha256": expected})
            compiler_hash = binding.get("compilerOutput")
            if isinstance(compiler_hash, str):
                provenance_rows.append({"path": reader.relative(shipped_path), "status": "MATCH" if expected_hash(compiler_hash) == expected else "MISMATCH", "reason": "Recorded compiler checksum equals shipped checksum; no historical compiler re-extraction"})
            elif "cachedCompilerOutputs" in binding:
                cache_rows = compare_manifest(reader, binding["cachedCompilerOutputs"], "cached compiler outputs")
                matches = [r for r in cache_rows if r["status"] == "MATCH" and r["actual"]["sha256"] == expected]
                provenance_rows.append({"path": reader.relative(shipped_path), "status": "MATCH" if matches else "UNVERIFIED", "reason": "At least one preserved compiler output still matches the shipped assembly", "cacheComparisons": cache_rows})
            else:
                provenance_rows.append({"path": reader.relative(shipped_path), "status": "UNVERIFIED", "reason": "No compiler checksum chain"})
        else:
            provenance_rows.append({"path": reader.relative(candidate), "status": "UNVERIFIED", "reason": "No shipped path available for this backend"})
    else:
        provenance_rows.append({"path": reader.relative(evidence), "status": "UNVERIFIED", "reason": "No compiled assembly binding"})
    if not source_rows:
        provenance_rows.append({"path": reader.relative(evidence), "status": "UNVERIFIED", "reason": "No declared compiler source checksums"})

    evidence_rows = []
    evidence_manifest = reader.metadata(evidence, "evidence-paths.json")
    for entry in records(evidence_manifest, "evidence paths"):
        name = entry.get("path") or entry.get("windowsPath")
        path = reader.path(name)
        actual = reader.digest(path)
        expected = expected_hash(entry["sha256"]) if "sha256" in entry else None
        evidence_rows.append({"path": reader.relative(path), "status": "MISSING" if not actual else "MISMATCH" if expected and actual["sha256"] != expected else "MATCH", "actual": actual})
    report = reader.path("DELIVERY_REPORT.md", evidence)
    reader.observe(report)
    if report.is_file():
        text = reader.text(report)
        links = re.findall(r"!?\[[^\]]*\]\((<[^>]*>|[^)]*)\)", text)
        paths = re.findall(r"`((?:[A-Za-z]:[\\/]|Assets[\\/]|Logs[\\/]|Builds[\\/])[^`\r\n]+)`", text)
        checked = set()
        for raw in links + paths:
            raw = raw.strip().strip("<>")
            if re.match(r"^[A-Za-z][A-Za-z0-9+.-]*://", raw) or raw.startswith("#"):
                continue  # No network/remote content is fetched.
            raw = raw.split("#", 1)[0]
            if raw.startswith(("Assets/", "Logs/", "Builds/", "Packages/", "ProjectSettings/")):
                path = reader.path(raw)
            else:
                path = reader.path(raw, evidence)
            if path in checked:
                continue
            checked.add(path)
            sig = reader.observe(path)
            evidence_rows.append({"path": reader.relative(path), "status": "MATCH" if sig is not None and path.is_file() else "MISSING", "reason": "Local evidence reference exists; content/acceptance not reviewed"})
        if not evidence_rows:
            evidence_rows.append({"path": reader.relative(report), "status": "UNVERIFIED", "reason": "Report contains no supported local evidence references"})
    else:
        evidence_rows.append({"path": reader.relative(report), "status": "MISSING", "reason": "Delivery report missing"})

    reader.finish()
    collections = [required, candidate_rows, source_rows, input_rows, provenance_rows, evidence_rows]
    metadata_names = ["candidate-files-sha256.json", "compiled-source-checksums.json", "build-input-files-sha256.json", "compiled-assembly-binding.json", "source-state.json", "evidence-paths.json", "DELIVERY_REPORT.md"]
    metadata_changed = any(reader.path(n, evidence) in reader.unstable for n in metadata_names)
    for rows in collections:
        for row in rows:
            if reader.path(row["path"]) in reader.unstable or metadata_changed:
                row["status"] = "UNSTABLE"
    if any(inside(p, candidate) for p in reader.unstable):
        required.append({"path": reader.relative(candidate), "status": "UNSTABLE", "reason": "Candidate changed at inspection boundaries"})
    result = {
        "inspectedAtUtc": datetime.now(timezone.utc).isoformat(),
        "projectRoot": str(reader.root), "candidateDirectory": str(candidate), "evidenceDirectory": str(evidence),
        "backend": backend, "artifactIntegrity": group(required + candidate_rows),
        "declaredSourceMatch": group(source_rows + input_rows),
        "sourceComparisons": {"compilerChecksums": group(source_rows), "declaredInputs": group(input_rows)},
        "provenanceBinding": group(provenance_rows), "evidenceCompleteness": group(evidence_rows),
        "unstablePaths": sorted(reader.relative(p) for p in reader.unstable),
        "scope": {"candidateManifestEntries": len(candidate_rows), "candidateFilesObserved": len(files), "compilerSourceEntries": len(source_rows), "declaredInputEntries": len(input_rows)},
        "limits": [
            "MATCH applies only to the named files/recorded checksum chain, not a release or gameplay PASS.",
            "No Unity, EXE, build commands, scripts, metadata code, URLs, or save operations are executed.",
            "Current hashes do not become historical build inputs. Metadata booleans such as matches are ignored.",
            "No PDB re-extraction/rebuild; declared source coverage is not all scenes/assets/settings.",
            "Evidence existence does not prove input, performance, sound, GameView quality, candidate identity or human acceptance.",
            "File metadata checks detect observed changes; this is not an atomic snapshot and cannot exclude changes reverted between boundaries.",
        ],
    }
    states = [result[k]["status"] for k in ("artifactIntegrity", "declaredSourceMatch", "provenanceBinding", "evidenceCompleteness")]
    return result, 3 if reader.unstable else 0 if all(s == "MATCH" for s in states) else 1


def write_report(result, output):
    with (output / "report.json").open("x", encoding="utf-8") as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    lines = ["# Windows candidate read-only audit", "", "Candidate: " + result["candidateDirectory"], "",
             "Evidence: " + result["evidenceDirectory"], "", "Backend: " + result["backend"], "",
             "| Dimension | Result |", "|---|---|"]
    for name in ("artifactIntegrity", "declaredSourceMatch", "provenanceBinding", "evidenceCompleteness"):
        section = result[name]
        lines.append(f"| {name} | {section['status']} |")
    lines.extend(["", "Compared entries: " + json.dumps(result["scope"], ensure_ascii=False), ""])
    for name in ("artifactIntegrity", "declaredSourceMatch", "provenanceBinding", "evidenceCompleteness"):
        rows = [r for r in result[name]["files"] if r["status"] != "MATCH"]
        if rows:
            lines.extend([name + " differences:", ""])
            lines.extend(f"- {r['status']}: {r['path']} — {r.get('reason', 'declared/current checksum comparison')}" for r in rows)
            lines.append("")
    lines.extend(["Limits:", "", *["- " + x for x in result["limits"]], ""])
    with (output / "REPORT.md").open("x", encoding="utf-8") as stream:
        stream.write("\n".join(lines))


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    for arg in ("project-root", "candidate-dir", "evidence-dir", "output-dir"):
        parser.add_argument("--" + arg, required=True)
    args = parser.parse_args(argv)
    try:
        reader = Reader(args.project_root)
        candidate = reader.path(args.candidate_dir)
        evidence = reader.path(args.evidence_dir)
        output = reader.path(args.output_dir)
        if not inside(output, OUTPUT_ROOT.resolve()) or output == OUTPUT_ROOT.resolve():
            raise InputError("Output must be a new run directory under Logs/CodexParallel/P11DeliverySupport")
        if inside(output, candidate) or inside(output, evidence):
            raise InputError("Output must not be inside the candidate/evidence input")
        if output.exists():
            raise InputError("Output directory already exists; previous results are preserved")
        result, code = inspect(args.project_root, candidate, evidence)
        output.mkdir(parents=True, exist_ok=False)
        write_report(result, output)
        print(json.dumps({k: result[k]["status"] for k in ("artifactIntegrity", "declaredSourceMatch", "provenanceBinding", "evidenceCompleteness")}))
        print("Report: " + str(output / "REPORT.md"))
        return code
    except (InputError, OSError) as exc:
        print("INPUT/IO ERROR: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    raise SystemExit(main())
