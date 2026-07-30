# 🎬 Project L.U.N.A — 시스템 흐름 총정리 & 컷씬 확장 검토서

| 작성일 | 문서 버전 | 작성자 | 변경 사항 |
| --- | --- | --- | --- |
| 26.07.17 | 1.0.0 | Claude (이준서 세션) | 현행 데이터 구조 기준 시스템 흐름 전체 정리, 구조의 한계 분석, 연출 컷씬 수용성 검토(트리거·스텝 확장안·Unity 매핑) |

관련: [LUNA_데이터구조_설계서_v1.0.md](LUNA_데이터구조_설계서_v1.0.md) (v1.1) · 웹 프로토타입 `luna-proto/` (실증)

---

## 1. 🔄 시스템 흐름 — 데이터가 게임을 굴리는 순서

모든 단계는 "**어떤 데이터를 읽고 → 어떤 규칙으로 진행되는가**"로 서술한다. 이 흐름 전체가 웹 프로토타입에서 실동작으로 검증됐다.

### 0단계. 부팅

```
master.json(칵테일·재료·아이템·캐릭터·성격·대사풀·UI문자열) + balance.json(계수) 로드
→ 세이브 있으면 상태 복원: day, gold, reputation, affinity{}, alive{}, flags, quests, 구매재료, 추가레시피
→ 없으면 Config의 gold_start/reputation_start로 새 시작
```

### 1단계. 하루 시작

- `Days[day]` 메타를 읽는다: `start_phase`(day1만 bar — 출근길 없음), BGM 키.
- 오늘의 집계 버퍼(today: 매출/팁/서빙/이탈/평판Δ) 초기화.

### 2단계. 출근길 — commute_in (19:00, 밤 고정 배경)

- **자동 씬**: `Scenes(day, phase=commute_in, trigger=auto)` 중 `when` 통과분을 seq 순으로 실행 (예: 루나 독백).
- **인터랙션**: `Points` 중 `phase` 일치 + `when` 통과분이 해당 `spot`(위치 프리셋)에 마커로 노출. 조사하면 연결 씬 실행 또는 `shop:` 상점. 비반복 포인트는 소모 처리.
- 여기서 세운 플래그가 그날 밤 대화 선택지까지 흘러간다 (예: `flag.d3_cat_seen` → 아일리 선택지 해금 — 실증됨).
- 바 문 도달 → 다음 페이즈.

### 3단계. 입고 — stock_in (바 오픈 직전, 결정 H)

- `Ingredients` 중 `unlock_day == day`(+`unlock_when` 통과)를 입고 화면에 표시.
- **파생**: 새로 가능해진 칵테일 = `unlock_day(칵테일) == day` — 시트에 안 쓰고 빌드가 계산.
- 신규 입고 없으면(예: day2) 화면 자체가 스킵. day1은 초기 재고라 생략.

### 4단계. 1부 일반 영업 — bar_part1 (시스템 구동, 대본 없음)

```
guest_slots(day)를 seq 순으로 소비:
  delay_sec 경과 + 빈 좌석 → 스폰
  slot.tier → "그 시점까지 해금된" 해당 티어 칵테일 풀에서 추첨
  slot.personality → 대사 보이스 / slot.character → 스토리 카메오(전용 초상·전용 대사)
좌석 상태머신(좌석마다 독립):
  빈좌석 → 입장중 → 코스터대기 → [주문 대사 3개: 루나 질문→손님 고민→주문] → 제조대기
        → 제조중(전 좌석 인내심 정지 §3.5) → 한줄평(제공/버리기) → 서빙대기 → 반응 → 퇴장
인내심: Config 공식(coaster/serve 임계, 성격별 patience_mult) — 50% 재촉, 80% 최종재촉, 초과 시 이탈(평판 -1/-2)
대사: Barks에서 voice_id(캐릭터>성격>공용 폴백) × situation × 가중치 추첨, {cocktail} 치환
판정: 메뉴 확정 순간 올바른/틀린 고정(§3.9) → 채점은 레시피 파생 항목 단순 평균(⑧) → 결과는 서빙 시점까지 보류
정산: 올바름 = +정가(+등급 팁), 틀림/Sewage = -정가 / 다회 주문은 코스터 없이 재주문 / 카메오는 서빙 후 짧은 씬
슬롯 전원 소진(또는 조기 마감) = "새벽 1시" → 2부
```

- 연출: 포커스 카메라(한 화면 1손님, 이웃은 실루엣), 슬롯 인디케이터(회색/초록/노랑/빨강점멸), idle 잡담.

### 5단계. 2부 스토리 — bar_story (대본 구동)

- `Scenes(day, phase=bar, trigger=auto)`를 seq 순 실행. **씬 자체의 `when`으로 통째 분기** (예: day8 `alive.haru` ↔ `!alive.haru` 대체 씬).
- 스텝 실행기가 한 줄씩: `say`(표정) / `enter·exit`(좌석 L/M/R) / `order`(exact/craving/vague) / `craft`(제조 화면 호출) / `serve` / `choice`(when 필터·effects·goto) / `effect` / `fx·sfx·bgm` / `camera` / `move` / `wait`.
- 스텝 `when`으로 줄 단위 분기 (예: `grade >= good` 크리스 반응), `effects`로 호감·플래그·골드 적용.
- 카메라: 등장 인원에 맞춰 자동 — 1명 포커스 줌, 2명 줌아웃 페어 프레이밍(최대 2명 제한).

### 6단계. 정산 — settlement (바에서 나가기 직전, 결정 C)

- today 버퍼 집계 표시. 골드 자체는 서빙 순간 실시간 반영이라 화면은 "보고서"일 뿐.

### 7단계. 퇴근길 — commute_out (02:00)

- 2단계와 완전히 같은 구조, `phase` 필터만 다름 (예: 상자 회수 퀘스트 포인트).

### 8단계. 집 — home (결정 B: 강제 대화)

- "하루 마치기" 선택 → `Scenes(day, phase=home)` 존재 시 **강제 실행**(테라스 대화·선택지·호감) → 수면.

### 9단계. 꿈 — dream

- `Scenes(day, phase=dream)` 실행 — 글리치/암전 fx로 카타나 제로식 조각 컷씬. 이미 **컷씬이 대본 문법으로 굴러가는 실증 사례**다.

### 10단계. 마감

- 저장(집 수동 저장 시점) → day+1 → 1단계로. day13은 `guest_slots` 0개 = 1부 자동 스킵 → 리오스 씬 → `ending` 페이즈에서 `Endings`를 priority 순 first-match(폭포) 판정.

### 크로스커팅 (모든 단계 공통)

- **when DSL**: `day/money/reputation/grade/flag.x/affinity.x/alive.x/quest.x.stage/cocktail.abv·id·tag()` — 씬·스텝·선택지·포인트·해금·엔딩이 전부 이 하나의 조건 언어를 쓴다.
- **effects DSL**: `affinity ±/flag/money/reputation/give/unlock_recipe/quest.advance`.
- **텍스트**: 전부 `{ko, en}` 쌍. **파생값**(티어·해금일·채점표·제한시간)은 빌드 타임 계산.

---

## 2. ⚠️ 현행 데이터 구조의 한계 — 정직한 목록

구조 자체(씬-스텝 단일 문법 + when/effects)는 **분기·대화·경제·해금에는 충분**하다. 한계는 전부 "연출"과 "트리거" 쪽에 몰려 있다.

| # | 한계 | 내용 | 심각도 |
| --- | --- | --- | --- |
| L1 | **연출 어휘 빈약** | `fx`는 "id 하나 던지기"라, 연출의 내용(누가 어떻게 움직이고 화면이 어떻게 변하는지)이 전부 엔진 코드에 하드코딩됨. 애니메이션 클립 재생, 이모트, 지속시간·강도 파라미터가 스키마에 없음 | 🔴 컷씬 도입 전 해결 필수 |
| L2 | **순차 실행만 가능** | 스텝은 한 줄씩 완료 후 다음 줄. "크리스가 걸어오는 **동안** 루나가 말한다" 같은 병렬 연출 표현 불가 | 🔴 |
| L3 | **트리거 3종뿐** | 씬 발동이 auto(페이즈 진입)/interact(조사)/cameo(1부 서빙 후) 뿐. "거리의 특정 지점을 **지나가면**", "1부에서 N번째 서빙 직후", "골드가 X 이상이 된 순간" 같은 임의 시점 발동 없음 | 🟡 |
| L4 | **스텝 레벨 점프 없음** | 씬 점프는 선택지 goto뿐. 씬 중간에서 조건 점프·루프 불가 — when으로 줄 스킵만 가능. 복잡 분기는 씬 분리로 우회(씬 수 증가) | 🟡 |
| L5 | **1부 도중 스토리 개입 불가** | 1부는 시스템 구동이라 대본이 끼어들 틈이 카메오 슬롯 하나뿐. "1부 중간에 밖에서 소란이 들린다" 같은 개입 이벤트 스키마 없음 | 🟡 |
| L6 | **when에 OR 없음** | 설계상 의도(행 분리)지만, 복합 조건 컷씬에서 씬 중복을 유발할 수 있음 | 🟢 낮음 |
| L7 | **사운드 동기화 규칙 없음** | sfx는 발사 후 잊기. 대사 타이핑·연출과의 타이밍 동기화 없음 | 🟢 |
| L8 | **입력 제어/스킵 정책 없음** | 컷씬 중 입력 잠금, 스킵 가능 여부가 씬 속성으로 존재하지 않음 | 🟡 컬럼 1개로 해결 |

**한계가 아닌 것(확인됨)**: 조건 분기(생사·호감·플래그·등급) ✓ / 공간 간 상태 전파 ✓ / 다국어 ✓ / 저장·복구(컷씬도 스텝 단위라 §11 크래시 복구 모델이 그대로 커버) ✓ / 30종·day13까지 확장 ✓

---

## 3. 🎬 컷씬은 들어갈 수 있는가 — 결론: **들어간다. 스키마 소확장(v1.2)이면 충분**

핵심 관점: **컷씬은 별도 시스템이 아니라 "씬의 특수한 형태"다.** 꿈 컷씬(글리치→유나 대사→총성→암전)이 이미 지금 문법으로 굴러가고 있다. 부족한 것은 ①트리거 어휘 ②연출 스텝 어휘 ③병렬성 — 셋 다 컬럼/enum 추가로 해결되고, 기존 데이터는 하나도 안 깨진다.

### 3.1 트리거 확장 — "언제 컷씬이 발동하는가"

`Scenes.trigger`를 3종 → 6종으로:

| trigger | 발동 시점 | 장소 | 비고 |
| --- | --- | --- | --- |
| `auto` | 페이즈 진입 시 (현행) | 전부 | when 통과분 seq 순 |
| `interact` | 포인트 조사 시 (현행) | 거리 | Points 경유 |
| `cameo` | 1부 카메오 서빙 후 (현행) | 바 | GuestSlots 경유 |
| **`pass:<spot>`** 🆕 | 거리에서 해당 위치 프리셋을 **지나가는 순간** | 거리 | 강제 조우·매복 연출. when과 조합 |
| **`event:<키>`** 🆕 | 엔진이 쏘는 게임 이벤트 순간 | 바 | 아래 이벤트 사전 참고 |
| **`manual`** 🆕 | 다른 씬/effects가 `play_scene(id)`로 호출할 때만 | 전부 | 재사용 연출 조각·엔딩 씬 |

**event 키 사전(초안)** — 1부 도중 개입(L5)을 여는 열쇠:

```
part1_serve:N     N번째 정상 서빙 직후          guest_angry     손님이 화나서 이탈한 직후
part1_start       1부 시작 직후                  part1_end       슬롯 소진 직후(새벽 1시 연출)
craft_grade:X     등급 X 이하/이상 판정 직후      gold_reach:N    보유 골드가 N 도달 순간
```

발동 규칙: 이벤트 발생 → 해당 trigger의 씬 중 when 통과분을 **1부 진행을 일시정지하고**(인내심 정지 §3.5와 동일 규칙) 실행 → 복귀. 1회성은 effects로 플래그를 세워 when에서 걸러낸다(기존 패턴 그대로).

### 3.2 스텝 어휘 확장 — "컷씬 안에서 무엇을 하는가"

| 스텝 | 인자 | 용도 | Unity 구현 |
| --- | --- | --- | --- |
| **`anim`** 🆕 | actor, 클립키 | 캐릭터 애니메이션 재생 (앉기·쓰러지기·담배 등) | Animator.SetTrigger |
| **`emote`** 🆕 | actor, 아이콘키 | 머리 위 이모트(💢💦❗…) | 프리팹 1개 |
| **`camera` 확장** | `shake:강도,초` `zoom:대상,배율,초` `fade:in/out,초` `pan:spot,초` | 화면 연출 — fx에 흩어진 걸 camera 인자로 통합 | Cinemachine Impulse/블렌드 |
| **`timeline`** 🆕 | 타임라인 에셋 id | **복잡 연출의 탈출구** — 아래 3.4 참고 | PlayableDirector.Play |
| **`sync` 컬럼** 🆕 | 스텝 공통 컬럼: `wait`(기본)/`no_wait` | `no_wait`면 완료를 안 기다리고 다음 스텝 진행 → **병렬 연출(L2) 해결** | 실행기 한 줄 수정 |

`sync` 하나가 병렬 문제의 90%를 해결한다:

```
| seq | type | actor | arg          | sync    | text_ko                  |
| 1   | anim | chris | walk_to:M    | no_wait |                          |  ← 걷기 시작하자마자
| 2   | say  | luna  |              | wait    | …크리스 씨? 무슨 일이에요? |  ← 걷는 동안 대사
| 3   | camera | | shake:0.4,0.6    | no_wait |                          |  ← 대사와 동시에 흔들림
```

### 3.3 조건별 컷씬 예시 — 지금 when DSL로 이미 표현되는 것들

| 원하는 연출 | Scenes 행 (trigger + when) |
| --- | --- |
| 바 2부 진입 시, 삼호가 죽었고 호감 낮으면 크리스가 무거운 표정 | `bar` / `auto` / `!alive.samho && affinity.chris < 30` |
| 거리에서 골목 앞을 지나면 갱단 조우 (day6 이후, 1회) | `commute_out` / `pass:alley_in` / `day >= 6 && !flag.gang_met` |
| 1부에서 3번째 서빙 직후 밖에서 총소리 | `bar` / `event:part1_serve:3` / `day == 9` |
| Sewage를 낸 직후 크리스가 개입하는 지도 컷씬 | `bar` / `event:craft_grade:sewage` / `day <= 3 && !flag.chris_lesson` |
| 골드 2,000 달성 순간 축하 연출 | `bar` / `event:gold_reach:2000` / `!flag.rich_once` |
| 엔딩 분기 연출 | `ending` / `manual` / — (Endings 폭포가 scene_id 호출) |

즉 **조건 시스템은 새로 만들 게 없다** — 트리거 어휘만 늘리면 기존 when이 그대로 컷씬 조건이 된다.

### 3.4 Unity 엔진 관점 실현성 — 가능하고, 절반은 이미 있다

구 엔진 자산 대조(03.Scripts 실사 기준):

| 필요한 것 | 구 엔진에 있는 것 | 판단 |
| --- | --- | --- |
| 씬-스텝 실행기 | day0~2 대본 실행기(이벤트 타입 0~9 + next 체인), DialogueManager | 구조 동일 — 스텝 enum만 교체 |
| 컷씬 재생 | **CutsceneManager + cutscenes.json + OustideTimelineManager(Timeline 이미 사용 중)** | `timeline` 스텝은 브릿지 1개면 연결 |
| 조건 판정 | (신규) ConditionEvaluator 1개 | 인수인계 §22부터 계획된 것 |
| 바 카메라 | ICameraControl.CameraMove(ESlotType)+Zoom | 프로토의 포커스/줌 로직과 대응. Cinemachine 가상카메라 3개+블렌드 권장 |
| 거리 컷씬 | OutSide 씬 + IInteractable + 횡스크롤 루나 | `pass:` 트리거 = SpotAnchor에 트리거 콜라이더 |
| 입력 잠금/스킵 | (신규) 씬 속성 | `Scenes.skippable` 컬럼 1개 + 실행기 처리 |

**역할 분담이 핵심**: 일상 연출(대사·등퇴장·이모트·흔들림)은 **시트의 스텝**으로 기획자가 직접 쓰고, 고밀도 연출(습격 회상 풀신, 엔딩 무비급)은 연출 담당이 **Unity Timeline 에셋**으로 만들어 `timeline` 스텝으로 호출한다. 데이터는 "무엇을 언제"를, Timeline은 "어떻게"를 담당 — 서로의 영역을 침범하지 않는다.

### 3.5 스키마 v1.2 변경 요약 (전부 하위 호환)

```
Scenes:  trigger enum 확장(pass:/event:/manual) + skippable 컬럼(bool, 기본 false)
Steps:   type 추가(anim/emote/timeline) + camera 인자 확장 + sync 컬럼(기본 wait)
effects: play_scene(id) 추가
검증기:  anim 클립키·timeline id 존재 검사(리소스 매니페스트 대조), event 키 오타 검사,
         no_wait 남용 검사(씬의 마지막 스텝이 no_wait면 경고)
```

기존 시트/JSON은 컬럼 기본값으로 전부 그대로 동작 — **마이그레이션 비용 0**.

---

## 4. 🚧 남은 결정 (v1.2 착수 전 확정 필요)

- [ ] event 키 사전 최종 목록 — 위 초안 8종에서 가감
- [ ] 컷씬 스킵 정책 — 전부 스킵 가능? 최초 1회는 강제? (`skippable` + `flag.seen_x` 조합으로 데이터 표현 가능)
- [ ] anim 클립키 네이밍 규칙 — 그래픽 파트(⑦문서)와 합의 필요
- [ ] Timeline 에셋 목록 초안 — 습격 회상 4종(꿈), 엔딩 5종, day13 리오스 등 후보 산정
- [ ] `pass:` 트리거의 재발동 정책 기본값 (1회성 기본 + repeatable 옵트인 권장)
