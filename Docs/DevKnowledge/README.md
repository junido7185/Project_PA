# 게임 개발 기술 지식 라이브러리 (DevKnowledge)

공개 영상·게시물에서 찾은 개발 기법을 **공식 문서·공개 소스로 확인해** 재사용할 수 있게 모은 곳. 연구 가설과 같아서 Canon·현재 구현·실제 검증을 대신하지 않는다.

## 찾기 (Claude Code · Codex 공용)
```bash
python Tools/DevKnowledge/devk.py search "navmesh avoidance"   # 기술 노트(FTS) + 출처 제목
python Tools/DevKnowledge/devk.py show navmesh-avoidance-prediction-time
python Tools/DevKnowledge/devk.py stats --topic shop-npc-movement
```
grep으로 `Docs/DevKnowledge/techniques/*.md`를 바로 읽어도 된다. 출처 전체는 `SOURCES.md`(자동 생성).

## 구조
| 위치 | 내용 | 정본 |
|---|---|---|
| `techniques/<slug>.md` | 기술 하나 = 파일 하나. 앞머리(status·pa_fit·value·applied·sources) + 요약·원리(사실/추론 구분)·코드 예제·적용 조건·한계·P.A. 적용·검증 줄·출처 | md |
| `devknowledge.sqlite` | 검색어·출처·증거(프레임/캡션/코드/자막)·계정·기술 색인(FTS5)·검증 | 수집 기록은 DB, 기술은 `index`로 md에서 다시 읽음 |
| `SOURCES.md` | 출처 원장(`devk.py export`) | DB에서 생성 |
| `Logs/DevKnowledge/cache/` | 프레임·이미지 모음(공개 저장소 밖, 지워도 됨) | — |

검증 줄 형식(색인이 읽음): `- [confirmed|partial|contradicted|not_found] official_doc|source_code|paper|talk: URL — 확인한 내용`

## 탐색 절차(작게 반복)
1. 주제 하나를 정하고(`devk.py`의 `TOPICS`에 관련 어휘) 검색어를 넣어 돌린다 → `query-add`(결과 수·남긴 수).
2. 후보를 `source-add`로 넣는다(같은 영상·게시물은 id로, 재업로드는 제목·길이로 자동 중복 처리). 관련도(어휘)·품질(출처 성격)로 점수. 점수 높은 작성자는 `accounts.promising` → 다음 검색어 확장.
3. 분석: 영상은 공식 oEmbed(`meta`) + 공개 자동 프레임(`frames`, 유튜브 hq1~3 = 약 1/4·1/2·3/4 지점)을 모아 본다. 게시물은 캡션·이미지. 결과는 `evidence-add`(성공/실패 모두).
4. 기법을 추론해 노트를 쓰고, 공식 문서·공개 코드로 확인해 검증 줄을 단다. 같은 기법은 한 노트로 합친다.
5. `index` → `export` → `stats`. 게임에 쓸 만하면 실제 게임에서 A/B로 재고 `applied`에 결과를 적는다.

## 비용·약관 경계(2026-11-09 확인)
- 쓰는 것: 구독에 포함된 웹 검색·웹 읽기, YouTube 공식 oEmbed, 공개 썸네일 프레임, 로그아웃 상태의 Instagram 공개 해시태그·게시물 페이지, 공식 문서·GitHub 공개 저장소. **유료 API·결제 없음.**
- 쓰지 않는 것: Instagram 로그인 수집(약관상 자동 수집 금지, 계정 정지 위험), 영상 원본의 공개 저장소 저장.
- 보류: 영상 음성·자막·세밀한 프레임 분석(yt-dlp·faster-whisper)은 설치가 자동 권한 검사에서 거절됨. 허용하더라도 YouTube 약관은 다운로드·자동 접근을 제한하므로, 쓸 때는 개인 학습용 소규모·`Logs` 캐시·분석 뒤 삭제·링크와 짧은 요약만 남긴다.
- 원본 문장 인용은 짧게, 코드 예제는 라이선스(MIT 등)를 적고 필요한 줄만.

## 주제
- `shop-npc-movement` (2026-11-09 시범): 가게 안 NPC의 자연스러운 움직임 — 회피 예측 시간, 우선순위, 멈춘 NPC 깎기, 스마트 오브젝트.
