# 유니티 StreamingAssets/json 동기화 — 프로그래밍 파트 전달

대상 경로: `HumanBartender/Assets/StreamingAssets/json/`
비교 기준: `develop`(유니티, 최종 갱신 7/27 `4d0bbdf`) ↔ `Document`(기획, 최종 갱신 7/30 `2d142a7`)

유니티 쪽 json은 7/24와 7/27 두 번에 걸쳐 파일별로 복사돼서, 파일마다 뒤처진 시점이 다르다.

---

## 1. 요약

| 구분 | 수 | 내용 |
|---|---|---|
| 완전 동일 | 20 | 그대로 두면 된다 |
| 기획 쪽이 최신 | 8 | 덮어쓰기 필요 |
| 값이 다른 것 | 1 | `script/street.json`의 `arg` — 아래 §4 |
| 유니티에만 남은 구파일 | 5 | 신 파이프라인이 만들지 않는다 → 삭제 대상 |
| 기획에만 있고 유니티에 없는 파일 | 0 | 없음 |

**유니티에서 값을 따로 고친 흔적은 없다.** 9개 차이 전부 기획 쪽 갱신이 반영되지 않은 것이거나, 리포에 기획 데이터가 올라오기 전 판본을 받은 것이다.

기획 배포 파일은 23 + `script/` 6 = 29개다. 유니티 `json/` 하위에는 34개가 있고, 차이 5개가 구파일이다.

---

## 2. 삭제 대상 — 유니티에만 남은 구파일 5종

신 파이프라인(`tools/build.py`)이 만들지 않는 파일이다. 지금은 읽히더라도 갱신되지 않으므로 지운다.

| 파일 | 대체 |
|---|---|
| `expressions.json` | `character_anim.json`으로 통합됨 (캐릭터별 표정 세트가 여기 들어있다) |
| `script/common.json` | `script/home.json` + `script/street.json` + `script/cutscene.json`으로 분리 |
| `script/day_1.json` | `script/bar/day1.json` |
| `script/day_2.json` | `script/bar/day2.json` |
| `script/day_3.json` | `script/bar/day3.json` |

`script/` 구성은 `bar/day1~3.json` + `home.json` + `street.json` + `cutscene.json` 6개로 고정이다.
조회 키는 (day, phase, seq)이고 day 0 = 상시 씬이다.

---

## 3. 덮어쓰기 필요 — 기획 쪽이 최신인 8개

### 3.1 `characters.json` — 19 → 18개

- `board`(전광판) 삭제 — 거리에서 제거된 요소다
- `port`에 `event_surprise` 표정 추가

### 3.2 `cocktails.json` — 18 → 17개

- `draft_beer`(생맥주, 100G) **삭제**
- `bottle_beer`(병맥주, 90G) **추가** — 잔은 mug + 뚜껑 + beer pour 구성

생맥주 id를 참조하는 코드가 있으면 `bottle_beer`로 교체해야 한다.

### 3.3 `shelf_items.json` — 해금일 7건 변경

옛 값 3·4·5·6이 데모 범위(1~3일) 밖이라 조정했다. 유니티 → 기획: `3→2`(2건) · `4→1` · `5→2`(2건) · `6→2`(2건).

### 3.4 `balance.json` — config 키 2개 추가

```json
"street_typing_interval_ms": 50,   // 거리 말풍선 타이핑 간격 — 바(typing_interval_ms)와 별도 튜닝
"street_auto_next_delay_sec": 3    // trigger=auto 대사 — 타이핑 종료 후 다음 대사까지 텀
```

### 3.5 `cutscenes.json` — 21 → 23개

`sprite` kind 2개 추가. 포스터 뷰용이다 — `resource_key` 이미지를 화면 중앙에 띄우고 **호출한 스텝의 text가 하단 대사 1줄**이 된다. 상호작용 키로 닫는다.

```json
{ "id": "sp_parttime_poster",   "kind": "sprite", "resource_key": "Poster/parttime" }
{ "id": "sp_experiment_poster", "kind": "sprite", "resource_key": "Poster/experiment" }
```

`timeline` 스텝은 kind `timeline`과 `sprite` 둘 다 부를 수 있다. 이미지는 아직 발주 대기(가칭 키)다.

### 3.6 `interact_points.json` — **필드 2개 신설** + 11 → 7개

거리 지점 스키마가 바뀌었다. 파싱 코드 수정이 필요한 유일한 항목이다.

```json
{
  "id": "p_shiba",
  "spot": "alley_in",
  "kind": "npc",
  "actor": "shiba",          // ★신설 — 그 지점에 서 있는 캐릭터. 같은 actor 지점이 여럿이면 위치 이동
  "phase": "both",
  "trigger": "interact",     // ★신설 — interact(E키) / proximity(접근 시 자동)
  "when": "day >= 2",
  "scene_or_shop": "group:np_shiba",
  "selection": "conditional"
}
```

삭제된 지점 4개: `p_news_d2` · `p_news_d3`(전광판) · `p_ob_vending`(자판기) · `p_ob_toilet`.
자판기는 사양 확정 후 다시 넣을 예정이다.

현재 지점 7개 구성 — kind: object 4 · npc 2 · shop 1 / phase: commute_in 2 · both 3 · commute_out 2 / selection: once 3 · repeat 3 · conditional 1.

### 3.7 `spots.json` — 9 → 8개

`street_board`(전광판 앵커) 삭제.

### 3.8 `script/bar/day1.json` — `end_part` 위치 이동

| | 씬 | seq |
|---|---|---|
| 유니티(구) | `d1_tutorial_wrap` | 27 |
| 기획(현행) | `d1_port` | 15 (그 씬의 마지막 스텝) |

2부 종료 트리거를 대본 마지막 씬으로 옮긴 결과다. `d1_tutorial_wrap`은 26스텝으로 줄고 `end_part`가 없다.

### 3.9 `script/street.json` — 씬 22 → 11개

삭제된 씬 13개 (사유별):

- **전광판 제거**: `d2_news` · `d3_news`
- **자판기·화장실 사양 미확정**: `ob_vending_buy` · `ob_vending_poor` · `ob_toilet_1`
- **오브젝트 3원칙 위반(루나 독백)**: `d2_commute_in` · `d2_commute_out` · `d3_commute_in` · `d3_commute_out` · `ob_parttime_2` · `ob_parttime_3`
- **컷씬으로 이관**: `d3_samho_death` · `d3_samho_rescue` — 삼호 사망은 타임라인 컷씬으로 제작 예정이라 대본 데이터에서 뺐다

추가된 씬 2개: `np_shiba_3` · `np_shiba_treat`(선택지 분기)
선택지 2세트 추가: `ch_st_shiba` · `ch_st_box`

**오브젝트 보기 3원칙** — 거리 오브젝트 씬은 이 규칙을 따른다(빌드가 검사한다):

1. 말풍선은 오브젝트 위에만 뜬다 — **루나 말풍선·독백 금지**(빌드 에러로 차단)
2. 본문은 그 사물에 적힌 정보만 — 관찰·감상·설명체 금지 (전단이면 "사람 구함 010-…"이 그대로)
3. 줍기·구매처럼 행동이 붙으면 선택지로 (상자 = 줍는다/무시한다 — 루나 말풍선 없이 버튼만)

---

## 4. 값이 다른 1건 — `street.json`의 `arg`

`script/street.json`의 `say` 스텝 `arg`가 유니티는 `"default"`(82곳), 기획은 `"idle"`(현행 34곳)이다.

유니티 쪽 `street.json`은 7/27 `6ad6f1c`에서 처음 추가될 때부터 `"default"`였다 — 유니티에서 `idle`을 고친 것이 아니라, 기획 데이터가 리포에 올라오기 전(7/27 이전) 판본을 받은 것으로 보인다.

**기획 데이터의 `"idle"`이 맞다.** 거리 씬의 `arg`는 표정이 아니라 SD 동작이다:

| 씬 phase | `say`/`expr` 스텝의 `arg` 의미 | 참조 파일 |
|---|---|---|
| `bar` · `bar_open` · `home` · `dream` · `intro` | 흉상 표정 | `character_anim.json` |
| `street` · `commute_in` · `commute_out` | **SD 동작** | `field_anims.json → action` |

거리엔 흉상이 없어서 표정 개념이 존재하지 않는다. 해당 스텝의 actor 4명 모두 `field_anims.json`에 `idle`을 갖고 있고, `default`는 아무도 갖고 있지 않다:

| actor | field_anims 보유 action |
|---|---|
| luna | idle · run · walk · walk_w_cat |
| samho | dead · dead_opening · idle · idle_blink · run |
| shiba | idle |
| vendor | idle |

`"default"`를 유지하면 SD 동작을 찾지 못한다. 기획 쪽 빌드는 이 규칙을 검증하고 있어서(`[동작] {씬}#{seq}: {actor}에 동작 '...' 없음`) `default`를 넣으면 빌드가 실패한다.

**지금은 어느 값이든 동작에 영향이 없다.** 현재 엔진 상태를 확인했다:

- `NewDialogueStepData.Arg`는 파싱만 되고 소비하는 로직이 없다
- `field_anims.json`은 `NewFieldAnimDataSO`로 로드만 되고 재생 로직이 없다 (SO 에셋도 빈 배열)
- 흉상 경로(`DialogueSceneDirector` → `SetCharacterAsync`)는 구 `DayDataSO.Expression`을 쓰고 있어 새 `arg`와 연결되지 않았다
- 설령 `arg`가 흉상 표정 조회로 들어가도 `CharacterAnimSO`가 `default`로 폴백하며 경고 로그만 남긴다(크래시 없음)

즉 거리 SD 동작 재생을 구현하는 시점에 **phase로 참조 테이블을 갈라주면 된다** — 바 계열은 `character_anim`, 거리 계열은 `field_anims`. 그때 `arg` 값은 기획 데이터(`idle`)를 그대로 쓰면 맞는다.

---

## 5. 다음 빌드에 들어가는 신설 항목 2건

7/30 확정분으로, 다음 커밋에 포함된다. 미리 파싱 준비가 필요하다.

### 5.1 `personalities.json` → `think_chance` 필드 신설

코스터를 놓으면 주문을 받는 대사 3개가 `ask_order`(루나 질문) → `order_think`(손님 고민) → `order`(주문) 순서로 재생된다. 세 대사 전부 `barks.json`에 있고 `situation` 값으로 구분한다.

`think_chance`는 그중 `order_think`를 재생할 확률(0~1)이다. 코스터 드롭 시 1회 굴려서 실패하면 고민 대사를 건너뛰고 `ask_order` 다음 바로 `order`로 넘어간다.

```json
{ "id": "rough", "name": {...}, "tip_mult": 1.1, "patience_mult": 0.8, "think_chance": 0.3 }
```

| id | think_chance |
|---|---|
| gentle | 1.0 |
| rough | 0.3 |
| touchy | 0.8 |
| quiet | 0.5 |
| chatty | 1.0 |

카메오(단골)는 이 값과 무관하게 전용 `order_think` 행을 항상 재생한다.

### 5.2 `balance.json → config.order_bark_gap_sec` = 1.5

주문을 받는 대사 3개(`ask_order` · `order_think` · `order`) 사이의 대기 시간(초)이다. 앞 대사 타이핑이 끝난 뒤 이 시간만큼 기다렸다가 다음 대사를 띄운다. `order_think`가 재생되면 대기가 두 번, 생략되면 한 번 생긴다.

---

## 6. 옮기는 방법

기획 쪽 산출물 위치: `Document/데이터/json/` (23파일 + `script/` 6파일)

옮기는 시점은 프로그래밍 파트가 정한다. 기획 파트는 `Document/` 밖을 건드리지 않으므로 복사 작업은 프로그래밍 파트에서 진행하고, 필요하면 기획 파트가 파일을 준비해 전달한다.

`Document/데이터/json/`은 빌드 산출물이라 직접 수정하면 다음 빌드에서 사라진다. 값을 고쳐야 하면 엑셀(`LUNA_System.xlsx` · `LUNA_Narrative.xlsx`) 쪽에 알려주면 기획에서 반영한다.
