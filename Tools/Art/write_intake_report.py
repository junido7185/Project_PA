"""Generate ART-000 review tables from measured evidence, without rerunning intake."""
import collections
import json
from pathlib import Path
import statistics
import struct
import re
import asset_intake as intake

ROOT, DOCS = intake.ROOT, intake.DOCS
inventory = json.loads((DOCS / "asset-inventory.json").read_text(encoding="utf-8"))
assets = json.loads((DOCS / "selected-assets.json").read_text(encoding="utf-8"))["assets"]
blender = json.loads((DOCS / "blender-validation.json").read_text(encoding="utf-8"))
unity = json.loads((DOCS / "unity-validation.json").read_text(encoding="utf-8-sig"))
reopen = json.loads((DOCS / "library-reopen-validation.json").read_text(encoding="utf-8"))
assert blender["status"] == unity["status"] == reopen["status"] == "PASS"
bm = {a["id"]: a for a in blender["assets"]}
um = {a["id"]: a for a in unity["assets"]}
vm = {a["id"]: a for a in reopen["assets"]}
roles = {"MiniMarket": "상점·판매대·수납 가구의 형태 기준", "FoodKit": "기존 상품·식재료·가공 결과의 시각 대응",
         "MiniArcade": "후반 Culture/Luxury 성장 참고; 이번 Unity 입고 없음",
         "CuteFish": "기존 Fish/낚시의 물고기·낚싯대·물가 형태",
         "UltimateNature": "기존 WorldGrid 위의 나무·바위·작물·풀·꽃",
         "CubeWorldKit": "보관함·도구의 파생 부품; terrain/캐릭터 대체 없음"}

def write(name, text):
    path = DOCS / name
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip()+"\n", encoding="utf-8")

registry = ["# ART-000 External Asset Registry", "", "2026-09-06 · 로컬 원본 기준 · CC0 팩 6개 · 신규 gameplay/씬 적용 없음.", "",
    "[전체 파일·모델 측정](asset-inventory.json), [선정 모델 계약](selected-assets.json), [모델 분류표](MODEL_INVENTORY.md), [Unity 실측](unity-validation.json), [Blender 실측](blender-validation.json).",
    "", "원본은 Downloads에 그대로 있다. ZIP은 COPY→SHA256→경로/CRC/크기 검사→프로젝트 staging 압축 해제→선별 복사 순서로 처리했다. 실행파일·URL·HTML은 에셋으로 실행하지 않았다.", "",
    "## 팩별 출처", ""]
for p in inventory["packs"]:
    pack = p["packName"]
    selected = [a for a in assets if a["pack"] == pack]
    registry += [f"### {p['creator']} / {pack}", "",
        f"- 공식 출처: [{pack}]({p['sourceUrl']})",
        f"- 다운로드 원본: `{p['downloadedArchiveName']}` ({p['archiveBytes']:,} bytes)",
        f"- Archive SHA256: `{p['archiveSHA256']}`",
        f"- 압축 해제 위치: `{p['extractedPath']}`",
        f"- 확인된 버전/날짜: {p['detectedVersionDate']} — Google Drive ZIP의 2026-09-06 날짜는 팩 버전이 아니다.",
        f"- 라이선스: {p['licenseDesignation']}; " + (", ".join(f"[{Path(f).name}]({f.removeprefix('Docs/AssetProvenance/')})" for f in p["licenseFiles"]) or "ZIP 안 라이선스 파일 없음. 공식 Cube World 페이지의 CC0 표기와 사용자 제공 출처로 확인; 허위 License.txt를 만들지 않았다."),
        "- 원본 형식·파일 수: " + ", ".join(f"`{ext}` {count}" for ext,count in p["originalFormats"].items()),
        f"- 선택 형식: Blender {p['selectedBlenderFormat']} / Unity FBX만. 논리 모델 {p['logicalModelCount']}종; Blender 선정 {len(selected)}종; Unity {sum(a['importToUnity'] for a in selected)}종.",
        f"- 입고일: 2026-09-06. 역할: {roles[pack]}.",
        "- 파생 여부: 현재 원본 사본과 transform/material 래퍼만 생성. PA 신규/파생 mesh는 후속 제작 계획이며 아직 생성하지 않음.",
        "- 다른 다운로드: " + ("; ".join(f"`{d['name']}` SHA256 `{d['sha256']}`" for d in p["duplicateDownloads"]) or "없음"), ""]
registry += ["## 중복·Git·검증 범위", "",
    "Kenney의 `(1)` ZIP과 기존 ZIP은 각각 SHA256이 같다. Nature 두 ZIP은 SHA256이 다르지만 내부 파일 경로별 내용 SHA256이 전부 같다. 기존 Nature FBX 8종도 원본과 byte-identical이라 기존 GUID/경로를 재사용했다.", "",
    f"Git 시작 크기: loose 39.22 MiB + pack 855.04 MiB. 원본 ZIP 총 68,946,695 bytes, 에셋 압축 해제 파일 총 315,283,534 bytes는 ExternalAssetSources 안에 두고 Git에서 제외한다. 공식 Blender 휴대용 도구도 같은 제외 영역이다. 최종 선정 Blender library {(ROOT/'Blender/Library/ProjectPA_AssetLibrary.blend').stat().st_size:,} bytes는 packed/appended 상태로 추적하고, Assets 최종 사용 후보와 .meta도 추적한다.", "",
    "Unity 새 FBX는 20개/1,070,452 bytes이며 기존 Nature 8개를 더해 28개 래퍼를 검증했다. source-look URP material 20개를 공유한다. MiniArcade 2종, Koi·mushroom·Cart는 Blender 참고 전용이다.", "",
    "Unity 6000.3.2f1 D3D11: 28개 FBX hash, mesh, bounds, bottom-centered wrapper, scale, shader, dependencies, prefab reload PASS (`Logs/ART000/Unity_Intake_01.log`). Runtime/Editor compile 오류 0; 기존 CS8785/CS0414 경고는 남는다. Blender 4.5.13: 33개 library와 packed texture를 재개방하고 source reference render PASS.", "",
    "상품이 이미 붙은 display-bread/display-fruit/shelf-end는 DERIVATIVE_SOURCE다. 실제 ShopSlot 재고처럼 표시하지 않는다. Tuna의 Attack/Death clip은 원본 보존 대상일 뿐, 새 전투 기능에 연결하지 않는다.", "",
    "기존 캐릭터/씬/WorldGrid/chunk/terraform/경제/저장 권위는 변경하지 않았다. 상점 루프·save/load·Day 30·최종 demo·Unity 실제 day/night 시각 승인까지 통과했다는 의미가 아니다.", "",
    "## 다음 작업", "", "ART-001 Material Palette and Unity Import Normalization. 이번 ART-000에서는 ART-001~010 자동 구현을 시작하지 않는다. [제작 계획](DEMO_ASSET_REQUIREMENTS_AND_PRODUCTION_PLAN.md)을 따른다."]
write("EXTERNAL_ASSET_REGISTRY.md", "\n".join(registry))

model_lines = ["# ART-000 Model Inventory", "", "원본 형식 중복을 합친 550종. Cube World의 같은 이름 Block_Blank 두 종류는 원본 경로로 구분한다. 전체 파일 SHA256·MTL·texture·format별 목록은 asset-inventory.json의 files 배열이다.", "",
    "USE_DIRECT=무보정 사용, USE_WITH_NORMALIZATION=크기/재질 보정 후보, DERIVATIVE_SOURCE=부품/구성 변경 필요, REFERENCE_ONLY=연구 전용, NOT_NEEDED=이번 데모 범위 밖. 현재 USE_DIRECT는 0종이며, 분류는 배치 승인이나 최종 품질 PASS가 아니다.", "",
    "| Pack | Source model path | Class | OBJ triangles | Source dimensions XYZ | Purpose |", "|---|---|---|---:|---|---|"]
for model in inventory["models"]:
    dims = " × ".join(f"{d:.3f}" for d in model["metrics"]["dimensionsSource"])
    model_lines.append(f"| {model['pack']} | `{model['sourceKey']}` | {model['classification']} | {model['metrics']['triangles']} | {dims} | {model['purpose']} |")
write("MODEL_INVENTORY.md", "\n".join(model_lines))

# Complete format-level scan. Animations are recorded from glTF and FBX metadata;
# a name in source metadata is not runtime animation validation.
format_audit = []
for p in inventory["packs"]:
    entries = [f for f in inventory["files"] if f["pack"] == p["packName"]]
    animated, textures, materials = [], [], []
    for item in entries:
        path = ROOT / item["path"]
        if item["extension"] in (".glb", ".gltf"):
            m = intake.gltf_metrics(path)
            if m["animations"]:
                animated.append({"path": item["path"], "clips": m["animations"], "evidence": "glTF animation array"})
        elif item["extension"] == ".fbx":
            # Blender FBX binary format stores object type suffix after a NUL separator.
            data = path.read_bytes()
            names = sorted({v.decode("utf-8", "replace") for v in re.findall(rb"([A-Za-z0-9_ |.:-]+)\x00\x01AnimStack", data)})
            if names:
                animated.append({"path": item["path"], "clips": names, "evidence": "FBX AnimStack object-name metadata; not playback proof"})
        elif item["extension"] == ".png":
            data = path.read_bytes()
            if data[:8] == b"\x89PNG\r\n\x1a\n":
                w,h = struct.unpack(">II", data[16:24])
                textures.append({"path": item["path"], "width": w, "height": h,
                                 "role": "texture" if "Textures/" in item["path"] or path.name in ("Atlas.png", "Blocks_PixelArt.png") else "preview",
                                 "sha256": item["sha256"]})
        elif item["extension"] == ".mtl":
            text = path.read_text(encoding="utf-8-sig")
            materials.append({"path": item["path"], "names": re.findall(r"^newmtl\s+(.+)$",text,re.M),
                              "textureRefs": re.findall(r"^map_\w+\s+(.+)$",text,re.M)})
    format_audit.append({"pack":p["packName"],"animationMetadata":animated,"pngInventory":textures,"materialInventory":materials,
                         "prefabFiles": [f["path"] for f in entries if f["extension"] == ".prefab"],
                         "readmeFiles": [f["path"] for f in entries if "readme" in Path(f["path"]).name.lower()]})
intake.write_json(DOCS / "format-audit.json", {"packs": format_audit})

specs = ["# ART-000 Selected Asset Specifications", "", "Stable ID는 참고 에셋 ID다. 저장 오브젝트의 새 ID는 발급하지 않았다. owner는 기존 gameplay API/data의 연결 대상이며, Intake prefab에 경제·재고·저장 컴포넌트를 복제하지 않는다.", "",
    "현재 source look 래퍼의 회전은 원본 방향 유지, root pivot은 bottom center다. interaction face 목표는 Unity -Z이며 실제 배치면 보정은 ART-001 이후 소유자 wrapper에서 한다. 2m grid로 계산한 footprint는 시각 bounds의 최소 cell 수이며 기존 BuildingData 점유를 변경하지 않는다.", "",
    "| Stable ID | Purpose / owner | Unity dimensions X/Y/Z (m) | Minimum visual cells X/Z | Mobility / collider / NavMesh | Material |", "|---|---|---|---|---|---|"]
import math
for a in assets:
    row = um.get(a["id"])
    dims = " / ".join(f"{row['wrapperSize'][axis]:.3f}" for axis in "xyz") if row else "Blender reference only"
    footprint = " / ".join(str(max(1,math.ceil(row["wrapperSize"][axis]/2))) for axis in "xz") if row else "N/A"
    specs.append(f"| `{a['id']}` | {a['purpose']} / `{a['gameplayOwner']}` | {dims} | {footprint} | {a['mobility']}; {a['collider']}; {a['navmesh']} | {a['materialFamily']} |")
specs += ["", "전 행 공통: 접근 clearance는 1m 잠정값이다. 충돌/상호작용/Save identity는 기존 owner가 갖는다. 상품은 dynamic, 풀·꽃은 static dressing, 가구·나무·바위는 기존 배치/자원 수명을 따른다. Clearance는 후속 실제 NavMesh 통행에서 검증한다."]
write("SELECTED_ASSET_SPECIFICATIONS.md", "\n".join(specs))

measurements = ["# ART-000 Style Measurements", "", "같은 원본도 FBX/OBJ/BLEND의 triangulation과 split normals 때문에 vertex/poly 수가 다를 수 있다. 아래 polygon은 Blender mesh polygon, triangle은 OBJ fan-triangulation, surface density는 source local mesh area 기준이다. 단위/축이 다른 팩을 source density만으로 순위 매기지 않는다.", "",
    "| ID | Blender polygons | OBJ triangles | tri / source area | smooth polygons % | live bevel modifiers | colors sampled | mean saturation | roughness / metallic |",
    "|---|---:|---:|---:|---:|---:|---:|---:|---|"]
for a in assets:
    b,v = bm[a["id"]],vm[a["id"]]
    obj = next(m for m in inventory["models"] if m["pack"]==a["pack"] and m["name"]==a["name"])
    props = ", ".join(sorted({f"{m['roughness']:.2f}/{m['metallic']:.2f}" for m in b["materials"]}))
    measurements.append(f"| `{a['id']}` | {b['polygons']} | {obj['metrics']['triangles']} | {obj['metrics']['trianglesPerSourceArea']:.2f} | {100*b['smoothPolygons']/b['polygons']:.1f} | {b['bevelModifiers']} | {v['sampledColorCount']} | {v['meanSampledSaturation']:.3f} | {props} |")
measurements += ["", "## 팩별 선정 표본 평균", "", "| Pack | n | mean Blender polygons | mean OBJ triangles | mean tri/source area |", "|---|---:|---:|---:|---:|"]
for p in inventory["packs"]:
    sel = [a for a in assets if a["pack"] == p["packName"]]
    objs = [next(m for m in inventory["models"] if m["pack"]==a["pack"] and m["name"]==a["name"]) for a in sel]
    measurements.append(f"| {p['packName']} | {len(sel)} | {statistics.mean(bm[a['id']]['polygons'] for a in sel):.1f} | {statistics.mean(o['metrics']['triangles'] for o in objs):.1f} | {statistics.mean(o['metrics']['trianglesPerSourceArea'] for o in objs):.2f} |")
write("STYLE_MEASUREMENTS.md", "\n".join(measurements))

previews = ["# ART-000 Source Preview Review", "", "Blender Cycles CPU 16 samples, 512×512/model, 동일 조명·카메라, 각 모델 longest extent를 2로 정규화했다. Unity 실제 크기 비교가 아니며 최종 아트/그림자 승인도 아니다. 2026-09-06 세 contact sheet를 직접 검토했다.", ""]
for page in range(3):
    previews += [f"## Sheet {page+1}", "", f"![원본 비교 {page+1}](Previews/contact-sheet-{page+1}.png)", "", "왼쪽→오른쪽, 윗줄→아랫줄:", ""]
    for a in assets[page*12:(page+1)*12]:
        previews.append(f"- [{a['pack']}/{a['name']}](Previews/{a['id']}.png) — {a['classification']}")
previews += ["", "## 관찰과 적용 판단", "",
    "MiniMarket는 큰 chamfer와 두꺼운 판·기둥, FoodKit은 6~12각 원통과 얇은 식기 구조가 읽힌다. 원본 진열대에 상품이 고정되어 있으므로 빈 슬롯용 파생 작업이 필요하다. 수납함/계산대의 형태는 읽히지만 슈퍼마켓 회색·초록색을 그대로 마을의 최종 팔레트로 쓰지 않는다.", "",
    "Nature는 나뭇잎을 작은 잎 대신 큰 2~4 덩어리로 표현하고, 줄기/가지의 비대칭과 flat facets를 유지한다. Fish는 둥근 큰 눈과 얇은 지느러미가 특징이다. 긴 낚싯대와 Wheat/Grass는 작은 게임 화면에서 얇아질 수 있어 후속 silhouette/scale 확인이 필요하다.", "",
    "Cube Chest는 부드러운 덮개·장식 프레임과 잠금쇠가 강해 생활 수납함으로 쓸 때 잠금 기능을 암시하지 않도록 파생한다. Cart는 상자 형태 참고로만 쓴다. 광석·새 종의 물고기·새 게임 아이템을 모델 이름 때문에 만들지 않는다.", "",
    "Source render에서 접지 그림자·가는 지느러미 그림자·부두 기둥 분리는 보인다. Unity의 실제 태양·밤 조명·distance shadow/LOD는 여기서 확인 못 함. ART-001의 격리 preview와 ART-002~004 적용 시 확인한다."]
write("PREVIEW_REVIEW.md", "\n".join(previews))
print("ART000 measured reports generated")
