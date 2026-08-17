# Project L.U.N.A 칵테일 시스템 데이터 명세 v2

## 문서 상태

- 상태: **제안본 / 구현 계약 초안**
- 기준 파일: `LUNA_Cocktail_System_Proposed_v2.xlsx`
- 적용 범위: 칵테일·재료·레시피·기믹·점수·정산·영문 현지화 데이터
- 비적용 범위: 엔진 코드, 픽셀 스프라이트, UI 공통 문구, 실제 JSON 빌드 결과물
- 중요한 원칙: 현재 운영 중인 `LUNA_System.xlsx`와 JSON을 바로 덮어쓰지 않는다. v2 빌드와 회귀 검증이 끝난 뒤 전환한다.

이 문서는 기존 임시 테이블을 그대로 코드에 맞추는 문서가 아니다. 기획자가 읽고 수정할 수 있으면서도 프로그래머가 별도의 해석 없이 검증·변환할 수 있는 구조를 새로 정의한다.

---

## 1. 설계 목표

### 하나의 값은 한 곳에서만 관리한다

- 칵테일의 핵심 재료는 `Cocktails.core_ingredients` 같은 묶음 문자열로 중복 보관하지 않는다.
- 핵심 여부는 `RecipeSteps → requirement=CORE`로만 관리한다.
- 태그는 `Cocktails.tags` 같은 세미콜론 문자열로 보관하지 않는다.
- 태그 목록은 `Tags`, 연결 관계는 `CocktailTags`에서 관리한다.
- 도구를 사용하지 않는 칵테일은 가짜 `Tools.none` 행을 만들지 않고 `mix_tool_id=null`로 표현한다.

### 기획 데이터와 플레이 기록을 분리한다

- 엑셀은 게임 규칙과 마스터 데이터를 작성하는 원본이다.
- 플레이 중 만들어지는 Actual Craft는 런타임 JSON 또는 세이브 데이터에 기록한다.
- `RuntimeActualCraft` 같은 플레이 기록 정의를 엑셀의 마스터 행처럼 섞지 않는다.

### 이름만 보고 의미를 알 수 있게 한다

- 식별자는 단수형 `snake_case`를 사용한다. 예: `cocktail_id`, `recipe_step_id`.
- 열거형 값은 `UPPER_SNAKE_CASE`를 사용한다. 예: `MANUAL_SELECT`, `FILL_TO_CAPACITY`.
- 시간에는 `_sec`, 비율에는 `_ratio`, 퍼센트 수치에는 `_pct`, 밀리리터에는 `_ml`을 붙인다.
- 화면에 보이는 문자열은 `_ko`, `_en` 쌍으로 관리한다.
- `status`, `type`, `mode`처럼 의미가 넓은 이름은 단독으로 사용하지 않고 `roster_status`, `quantity_mode`처럼 범위를 붙인다.

---

## 2. 워크북 구성

| 시트 | 역할 | 주요 사용자 |
| --- | --- | --- |
| `README` | 구조·작성 원칙·Day 0·영문 현지화 기준 | 전 팀 |
| `Cocktails` | 칵테일 기본 정보와 표시 정보 | 시스템·UI·시나리오 기획 |
| `RecipeSteps` | 칵테일별 재료·순서·수량·입력 방식 | 시스템 기획·프로그래머 |
| `Ingredients` | 재료 마스터 | 시스템·UI 기획 |
| `Glasses` | 잔 마스터와 용량 | 시스템·그래픽 작업자 |
| `Tools` | 혼합 도구 마스터 | 시스템·그래픽 작업자 |
| `Garnishes` | 가니시 마스터 | 시스템·그래픽 작업자 |
| `Tags` | 필터 태그 명칭과 분류 | UI·UX 기획 |
| `CocktailTags` | 칵테일과 태그의 N:M 연결 | UI·프로그래머 |
| `SystemConfig` | Day·단위·인내심·공통 규칙 | 시스템 기획·프로그래머 |
| `GimmickRules` | 기믹 생성·종료·점수 입력 규칙 | 시스템 기획·프로그래머 |
| `ScoreRules` | 점수 구성요소와 가중치 | 밸런스 기획·프로그래머 |
| `ScoreBands` | 수량 오차·시간 초과 구간 | 밸런스 기획·프로그래머 |
| `GradeRules` | 최종 등급 구간과 표시명 | 밸런스·UI 기획 |
| `SettlementRules` | 등급별 판매·팁·환불 | 밸런스 기획 |
| `Enums` | 허용되는 열거형 값과 의미 | 전 팀 |
| `MigrationMap` | 기존 ID·열·삭제 항목 변환표 | 프로그래머 |
| `LocalizationQA` | 한·영 쌍 누락 검사 | 현지화·QA |
| `QA_Summary` | 오류와 미확정 데이터 요약 | 전 팀 |

---

## 3. 핵심 데이터 정의

### Cocktails

`cocktail_id`가 칵테일의 영구 식별자다. 화면 이름이 바뀌어도 ID는 바꾸지 않는 것을 원칙으로 한다.

| 필드 | 형식 | 필수 | 설명 |
| --- | --- | --- | --- |
| `cocktail_id` | string | 필수 | 칵테일 고유 ID |
| `roster_status` | enum | 필수 | `ACTIVE`, `HIDDEN`, `RETIRED` |
| `unlock_story_day` | int | 필수 | 튜토리얼을 0으로 보는 스토리 일차 |
| `menu_order` | int | 필수 | 레시피 목차 정렬 순서 |
| `order_weight` | float | 필수 | 같은 조건에서 주문 후보가 될 상대 가중치 |
| `name_ko`, `name_en` | string | 필수 | 화면 표시 이름 |
| `price` | int | 필수 | 기본 판매가 |
| `abv_pct` | float | 필수 | 완성 칵테일의 도수 표시값 |
| `glass_id` | FK | 필수 | `Glasses → glass_id` |
| `mix_tool_id` | FK/null | 선택 | `Tools → tool_id`; 빌드·직접 제조는 null |
| `garnish_id` | FK/null | 선택 | `Garnishes → garnish_id` |
| `mixing_ice` | enum | 필수 | 혼합 과정의 얼음 |
| `serving_ice` | enum | 필수 | 완성 잔에 남기는 얼음 |
| `liquid_color_start_hex` | color | 필수 | 내용물 시작 색상 |
| `liquid_color_end_hex` | color | 필수 | 그라데이션 끝 색상; 단색이면 시작값과 같음 |
| `time_limit_sec` | float | 필수 | 제조 권장 제한시간 |
| `flavor_ko`, `flavor_en` | string | 필수 | 칵테일 상세 화면의 맛 설명 |
| `recipe_summary_ko`, `recipe_summary_en` | string | 필수 | 칵테일 상세 화면의 제조 요약 |
| `menu_sprite_key` | string | 배포 전 필수 | 목차용 이미지 키 |
| `serve_sprite_key` | string | 배포 전 필수 | 완성품 표시 이미지 키 |
| `data_status` | enum | 필수 | `CONFIRMED`, `NEEDS_RECIPE`, `NEEDS_ASSET` 등 |

`unlock_story_day`는 메뉴 해금과 주문 풀 진입 기준이다. 엔진 내부의 날짜 숫자가 다르더라도 기획 원본의 값을 바꾸지 않는다.

### RecipeSteps

한 행은 한 칵테일의 한 제조 단계다. 재료 목록, 핵심 재료, 수량 판정, 기믹 호출의 기준이 모두 이 시트에 모인다.

| 필드 | 형식 | 필수 | 설명 |
| --- | --- | --- | --- |
| `recipe_step_id` | string | 필수 | `{cocktail_id}_{두 자리 순서}` |
| `cocktail_id` | FK | 필수 | `Cocktails → cocktail_id` |
| `step_order` | int | 필수 | 1부터 시작하는 실행·표시 순서 |
| `action` | enum | 필수 | `POUR`, `SQUEEZE`, `POWDER`, `FILL_UP` |
| `ingredient_id` | FK | 필수 | `Ingredients → ingredient_id` |
| `requirement` | enum | 필수 | `CORE`, `REQUIRED`, `OPTIONAL` |
| `input_mode` | enum | 필수 | `MANUAL_SELECT`, `GUIDED_GIMMICK`, `AUTO_APPLY` |
| `quantity_mode` | enum | 필수 | `EXACT_VOLUME`, `FILL_TO_CAPACITY`, `TBD` |
| `target_value` | float | 조건부 | 목표 수량 |
| `target_unit` | enum | 조건부 | `ml`, `oz`, `tsp`, `ratio` |
| `target_ml` | formula/output | 조건부 | 비교용 ml 환산값 |
| `is_scored` | bool | 필수 | 해당 단계를 제조 점수에 넣는지 여부 |
| `score_weight` | float | 필수 | 같은 기믹 안에서 단계별 가중치 |
| `show_in_recipe` | bool | 필수 | 레시피 화면에 표시할지 여부 |
| `data_status` | enum | 필수 | 단계 확정 상태 |

기본 순서는 `POUR → SQUEEZE → POWDER → MIX → FILL_UP`이다. `MIX`는 재료 행으로 만들지 않고 `Cocktails → mix_tool_id`에서 한 번 생성한다.

`quantity_mode=TBD`인 단계는 점수 계산에서 조용히 제외하지 않는다. 빌드 검증에서 오류 또는 배포 차단 경고로 올려 기획자가 수량을 확정하게 한다.

### Ingredients

| 필드 | 형식 | 필수 | 설명 |
| --- | --- | --- | --- |
| `ingredient_id` | string | 필수 | 재료 고유 ID |
| `roster_status` | enum | 필수 | 메뉴·선반 사용 상태 |
| `unlock_story_day` | int | 필수 | 재료가 처음 필요한 스토리 일차 |
| `name_ko`, `name_en` | string | 필수 | 화면 표시 이름 |
| `category` | enum | 필수 | 재료 선반·필터 분류 |
| `requires_open` | bool | 필수 | Open 기믹 선행 여부 |
| `open_type` | enum/null | 조건부 | `requires_open=true`일 때만 필요 |
| `color_hex` | color | 필수 | 혼합 색상 계산용 기준색 |
| `sprite_key`, `icon_key` | string | 배포 전 필수 | 월드·UI 리소스 키 |
| `description_ko`, `description_en` | string | 필수 | 플레이어에게 표시하는 설명 |

### Glasses, Tools, Garnishes

- 각 마스터는 ID, 한·영 이름, 리소스 키, 사용 상태를 가진다.
- `Glasses → capacity_ml`은 `FILL_TO_CAPACITY` 계산의 기준이다.
- `Tools → mix_action`은 `SHAKE` 또는 `STIR` 기믹 생성 기준이다.
- 사용하지 않는 도구와 가니시는 null 참조로 표현한다.

### Tags와 CocktailTags

- `Tags`는 태그 이름·영문명·카테고리·정렬 순서를 보관한다.
- `CocktailTags`는 `cocktail_id`, `tag_id`, `sort_order`만 가진다.
- 잠긴 칵테일은 태그 필터 결과에서 제외한다.
- 한 칵테일에 같은 태그를 두 번 연결할 수 없다.

---

## 4. Day 0 기준

- 튜토리얼 시작일은 공식적으로 `story day 0`이다.
- 모든 신규 데이터와 문서는 `unlock_story_day=0`을 튜토리얼 기준으로 사용한다.
- 현행 엔진이 Day 1부터 시작한다면 빌드 출력 단계에서만 `legacy_engine_day_offset=1`을 더한다.
- 기획 원본에 엔진 보정값을 미리 더하지 않는다. 그렇지 않으면 차후 엔진을 수정했을 때 날짜가 두 번 보정된다.

---

## 5. 주문 후보와 인내심

### 티어 없는 주문 후보

칵테일 티어는 사용하지 않는다. 주문 후보는 다음 조건으로 만든다.

```text
roster_status == ACTIVE
AND unlock_story_day <= current_story_day
AND 현재 시나리오 또는 손님 규칙이 허용함
```

조건을 통과한 후보 안에서 `order_weight`를 상대 가중치로 사용한다. `order_weight=2`는 같은 조건의 `order_weight=1`보다 약 두 배 자주 뽑힐 수 있다는 뜻이지, 반드시 두 번 등장한다는 뜻은 아니다.

### 코스터 인내심

```text
coaster_patience_sec = clamp(
  coaster_base_sec × guest_personality_multiplier,
  coaster_min_sec,
  coaster_max_sec
)
```

- 기본 제안값: 22초
- 최소 제안값: 12초
- 최대 제안값: 60초
- `guest_personality_multiplier`는 손님 성격 데이터에서 가져온다.

### 서빙 인내심

```text
serve_patience_sec = selected_cocktail.time_limit_sec + serve_grace_sec
```

기본 여유시간 제안값은 20초다. 제조 화면에 들어간 동안 코스터·서빙·손님 스폰 타이머는 모두 정지한다. 칵테일 메뉴 진입 시 정지하고, 서빙 선택 후 테이블 화면으로 복귀했을 때 재개한다.

---

## 6. 제조 큐와 기믹 생성

### 제조 시작 시점

1. 플레이어가 레시피 화면에서 칵테일을 확정한다.
2. 확정한 값은 `selected_cocktail_id`로 기록한다.
3. 해당 칵테일과 연결된 `RecipeSteps`를 스냅샷으로 복사한다.
4. 플레이어의 실제 재료·잔·도구 선택과 기믹 결과는 Actual Craft에 별도로 기록한다.

레시피를 고른 뒤 원본 엑셀이나 JSON이 바뀌어도 진행 중 제조의 정답이 바뀌면 안 된다. 따라서 제조 시작 시점의 목표 레시피를 스냅샷으로 보관한다.

### 기믹 큐 생성 순서

1. 실제 선택 재료 중 `requires_open=true`인 재료에 `OPEN`을 생성한다.
2. 실제 직접 선택한 `POUR` 재료마다 `POUR`을 생성한다.
3. 선택 레시피의 `GUIDED_GIMMICK + SQUEEZE` 단계마다 `SQUEEZE`를 생성한다.
4. 선택 레시피의 `AUTO_APPLY + POWDER`를 자동 처리한다.
5. 실제 선택 도구에 따라 `SHAKE` 또는 `STIR`을 한 번 생성한다.
6. `FILL_UP` 단계는 혼합 기믹 뒤에 생성한다.

### 데모 확정 규칙

- Shake: 성공과 실패를 합쳐 20회 판정하면 자동 종료한다.
- Stir: 성공과 실패를 합쳐 10회 판정하면 자동 종료한다.
- Stir 한 바퀴 제한시간은 2초다.
- Stir에서 오답 또는 시간 초과는 실패 1회로 기록하고, 실패한 위치를 새 한 바퀴의 기준점으로 사용한다.
- 판정 직후 입력 잠금이나 애니메이션 대기 시간을 두지 않는다.
- 키를 누르고 있는 동안 발생하는 반복 입력은 무시한다.
- 여러 입력이 동시에 들어오면 입력 이벤트가 들어온 순서대로 처리한다.
- 게임 일시정지 또는 창 포커스 이탈 시 기믹 타이머도 정지한다.
- 완료 이후에는 해당 기믹의 입력 이벤트를 해제한다.
- 데모의 Powder는 레시피에 표시하되 자동 투입하고 채점하지 않는다.
- 병맥주는 병뚜껑을 연 뒤 플레이어가 12oz를 직접 따르며 수량 채점에 포함한다.

---

## 7. 점수와 최종 등급

### 구성요소 점수

각 기믹은 0~100점의 구성요소 점수를 반환한다. 해당 제조에서 실제로 발생한 구성요소만 분모에 포함한다.

```text
craft_score_base =
  SUM(component_score × component_weight)
  / SUM(applicable_component_weight)
```

`applicable_component_weight`는 다음 조건을 모두 만족한 항목만 포함한다.

- 해당 제조에 기믹 또는 비교 항목이 존재한다.
- `enabled_in_demo=true`다.
- 필요한 목표 수량이 확정되어 있다.

### 수량 점수

```text
quantity_error_ratio = ABS(actual_ml - target_ml) / target_ml
```

`ScoreBands`는 구간의 최솟값·최댓값과 포함 여부를 별도 열로 가진다. 따라서 정확히 5%, 10%, 20%, 35%인 값이 어느 구간인지 코드에서 임의로 해석하지 않는다.

### 누락·추가·시간 규칙

- `CORE` 재료가 하나라도 누락되면 서빙 단계의 최종 등급을 `sewage`로 강제한다.
- `REQUIRED` 재료 누락은 해당 단계 점수를 0점으로 한 번만 반영한다.
- 같은 누락을 재료 불일치와 기믹 미실행으로 중복 감점하지 않는다.
- 정답 레시피에 없는 직접 선택 재료는 1개당 `extra_ingredient_penalty`를 차감한다.
- 제조 제한시간 초과는 `ScoreBands → metric=OVERTIME_RATIO`에서 감점값을 가져온다.
- 모든 가감산 뒤 제조 점수는 0~100으로 제한한다.

```text
craft_score = clamp(
  craft_score_base
  - extra_ingredient_penalty_total
  - overtime_penalty,
  0,
  100
)
```

### 주문과 다른 칵테일을 서빙한 경우

`order_cocktail_id`와 `selected_cocktail_id`의 불일치는 제조 조작 점수와 분리한다.

- 제조 점수는 플레이어가 선택한 레시피를 얼마나 정확하게 만들었는지 평가한다.
- 서빙 시 손님 주문과 선택 레시피가 다르면 최종 서빙 등급을 `sewage`로 강제한다.
- 이렇게 해야 “다른 칵테일을 완벽하게 만들었지만 주문에는 실패한 상황”을 기록과 연출에서 구분할 수 있다.

### 등급 경계

| 등급 | 영문 | 점수 범위 |
| --- | --- | --- |
| `excellent` | Excellent | 95 이상 100 이하 |
| `good` | Good | 80 이상 95 미만 |
| `decent` | Decent | 60 이상 80 미만 |
| `poor` | Poor | 35 이상 60 미만 |
| `sewage` | Sewage | 0 이상 35 미만 또는 강제 실패 |

등급 명칭은 플레이어에게 보이므로 한국어와 영어를 함께 관리한다.

---

## 8. 런타임 Actual Craft 계약

### CraftSession

Actual Craft는 엑셀 행이 아니라 한 번의 제조 플레이를 나타내는 런타임 객체다.

```json
{
  "schema_version": 2,
  "craft_session_id": "craft_00001234",
  "service_slot_id": "bar_seat_02",
  "order_cocktail_id": null,
  "selected_cocktail_id": "godfather",
  "target_recipe_snapshot": {
    "glass_id": "old_fashioned_glass",
    "mix_tool_id": "mixing_glass",
    "mixing_ice": "CUBED",
    "serving_ice": "CUBED",
    "time_limit_sec": 45,
    "steps": [
      {
        "recipe_step_id": "godfather_01",
        "ingredient_id": "whiskey",
        "action": "POUR",
        "requirement": "CORE",
        "quantity_mode": "EXACT_VOLUME",
        "target_value": 45,
        "target_unit": "ml",
        "target_ml": 45
      }
    ]
  },
  "actual": {
    "glass_id": "old_fashioned_glass",
    "tool_id": "mixing_glass",
    "selected_ingredient_ids": [
      "whiskey",
      "amaretto"
    ],
    "ingredient_selection_order": [
      "whiskey",
      "amaretto"
    ],
    "mixing_ice": "CUBED",
    "serving_ice": "CUBED"
  },
  "gimmick_results": [
    {
      "instance_id": "gimmick_0001",
      "gimmick_id": "POUR",
      "recipe_step_id": "godfather_01",
      "ingredient_id": "whiskey",
      "target": {
        "value": 45,
        "unit": "ml",
        "normalized_ml": 45
      },
      "actual": {
        "value": 46,
        "unit": "ml",
        "normalized_ml": 46
      },
      "success_count": null,
      "failure_count": null,
      "attempt_count": null,
      "score": 100,
      "completed_at_sec": 8.42,
      "completion_type": "MANUAL_NEXT"
    },
    {
      "instance_id": "gimmick_0003",
      "gimmick_id": "STIR",
      "recipe_step_id": null,
      "ingredient_id": null,
      "target": null,
      "actual": null,
      "success_count": 9,
      "failure_count": 1,
      "attempt_count": 10,
      "score": 90,
      "completed_at_sec": 31.18,
      "completion_type": "AUTO_TARGET"
    }
  ],
  "elapsed_total_sec": 31.18,
  "craft_score": 92.5,
  "craft_grade_id": "good",
  "score_status": "CALCULATED"
}
```

공통 제조 단계처럼 제조 시점에 특정 손님의 주문이 정해지지 않았다면 `order_cocktail_id`는 null일 수 있다. 이후 서빙 시점에 손님 주문과 제조 결과를 결합한다.

### ServeResult

```json
{
  "schema_version": 2,
  "serve_result_id": "serve_0000456",
  "craft_session_id": "craft_00001234",
  "guest_id": "guest_regular_03",
  "order_cocktail_id": "godfather",
  "selected_cocktail_id": "godfather",
  "craft_grade_id": "good",
  "order_matched": true,
  "core_ingredient_missing": false,
  "final_grade_id": "good",
  "sale_amount": 120,
  "tip_amount": 0,
  "refund_amount": 0
}
```

`score_status`는 정상 계산 시 `CALCULATED`, 필수 데이터 누락으로 신뢰할 수 없는 경우 `DATA_ERROR`를 사용한다. `DATA_ERROR`를 임의로 0점 처리해 정상적인 Sewage와 섞지 않는다.

---

## 9. 빌드 JSON 권장 분리

한 파일에 모든 데이터를 넣지 않는다. 변경 주기와 사용 위치가 비슷한 데이터끼리 나눈다.

| 출력 파일 | 포함 내용 |
| --- | --- |
| `cocktails.json` | 칵테일 기본 정보와 한·영 표시문구 |
| `recipe_steps.json` | 칵테일별 정규화된 제조 단계 |
| `ingredients.json` | 재료 마스터와 한·영 표시문구 |
| `cocktail_lookups.json` | 잔·도구·가니시·태그·열거형 |
| `cocktail_tags.json` | 칵테일과 태그 연결 |
| `cocktail_rules.json` | 시스템·기믹·점수·정산 규칙 |

권장 `cocktails.json` 한 행 예시는 다음과 같다.

```json
{
  "cocktail_id": "dry_martini",
  "roster_status": "ACTIVE",
  "unlock_story_day": 0,
  "menu_order": 1,
  "order_weight": 1,
  "localized": {
    "ko": {
      "name": "드라이 마티니",
      "flavor": "...",
      "recipe_summary": "..."
    },
    "en": {
      "name": "Dry Martini",
      "flavor": "...",
      "recipe_summary": "..."
    }
  },
  "presentation": {
    "glass_id": "cocktail_glass",
    "garnish_id": "olive",
    "mixing_ice": "CUBED",
    "serving_ice": "NONE",
    "liquid_color_start_hex": "#E8E4D7",
    "liquid_color_end_hex": "#E8E4D7",
    "menu_sprite_key": "",
    "serve_sprite_key": ""
  },
  "mix_tool_id": "mixing_glass",
  "price": 100,
  "abv_pct": 31.5,
  "time_limit_sec": 45
}
```

엑셀은 편집 편의를 위해 `_ko`, `_en` 열을 사용하고, JSON 빌드 시 `localized.ko`, `localized.en` 구조로 묶는다. 새로운 언어를 추가할 가능성이 커지면 별도 문자열 테이블 또는 로컬라이징 키 방식으로 이전할 수 있다.

---

## 10. 영어 현지화 계약

### 이번 v2에서 검사하는 범위

- 칵테일 이름, 맛 설명, 제조 요약
- 재료 이름과 설명
- 잔, 도구, 가니시 이름
- 태그 이름
- 최종 등급 이름

현재 제안 워크북은 위 범위에서 한국어와 영어 197쌍을 검사하며 빈 영문값은 0건이다.

### 영문값이 있어도 확정이 아닌 경우

13종 칵테일은 목표 재료 순서나 수량이 확정되지 않아 한·영 제조 요약에 동일한 의미의 검토용 문구가 들어 있다. 이는 영문 누락은 아니지만 플레이어에게 배포할 최종 문장은 아니다.

### 이 워크북 밖에서 확인할 범위

다음 문구는 칵테일 마스터가 아니라 공통 UI 문자열이므로 별도 `ui_strings.json` 또는 현행 로컬라이징 시스템에서 확인해야 한다.

- 필터, 정렬, 레시피 확정, 뒤로 가기
- 성공, 실패, 남은 시간, 제조 완료
- 잠긴 칵테일의 `???`
- 기믹 조작 안내와 키보드·게임패드 안내
- 오류·경고·접근성 문구

빌드는 현재 언어의 문자열이 없을 때 조용히 한국어로 대체하지 않는다. 개발 빌드에서는 누락 키를 오류로 기록하고, QA가 알아볼 수 있는 대체 표식을 보여 준다.

---

## 11. 기존 데이터 마이그레이션

### ID 변환

`MigrationMap`의 변환을 먼저 적용한다. 주요 변경은 다음과 같다.

- `long_island` → `long_island_iced_tea`
- `green_peppermint` → `green_creme_de_menthe`
- `white_cacao` → `white_creme_de_cacao`
- 잔 ID에 `_glass`를 붙이고 `beer`는 `beer_mug`로 변경
- `bees_knees`, `honey_syrup` 제거
- `Tools.none` 제거 후 null 참조로 변환

세이브 데이터가 기존 ID를 보관하고 있다면 로드 시 한 번 변환하고, 저장할 때는 새 ID만 사용한다.

### 빌드 스크립트 변경 순서

1. 시트를 열 번호가 아니라 헤더 이름으로 읽는다.
2. 중복 ID와 필수값을 검사한다.
3. 모든 FK를 검사한다.
4. 열거형 값이 `Enums`에 있는지 검사한다.
5. `RecipeSteps → quantity_mode=TBD`를 배포 차단 경고로 올린다.
6. 한·영 필수 문자열 쌍을 검사한다.
7. `MigrationMap`을 적용한다.
8. 엑셀의 평면 열을 권장 JSON 구조로 변환한다.
9. JSON 스키마 검증과 회귀 테스트를 실행한다.
10. 모든 검증이 통과한 뒤에만 현행 JSON을 교체한다.

현행 `build.py`가 예전 시트명과 열 순서를 전제로 한다면 v2를 바로 입력하지 않는다. 먼저 v2 전용 빌드 경로를 추가하고, 기존 출력과 비교한 뒤 전환한다.

---

## 12. 현재 남은 경고

| 항목 | 수량 | 의미 | 처리 기준 |
| --- | ---: | --- | --- |
| 목표 수량 미확정 단계 | 36 | `quantity_mode=TBD` | 실제 레시피 수량을 정하기 전 배포 불가 |
| 제조 요약 검토 필요 칵테일 | 13 | 한·영 검토용 문구 사용 중 | 레시피 확정 후 플레이어 문장으로 교체 |
| 잔 용량 | 6 | 현재 값은 제안치 | 실제 게임 용량·표현과 대조 후 확정 |
| 리소스 키 | 다수 | 스프라이트·아이콘 키 공란 | 아트 리소스명 확정 후 입력 |
| 점수·인내심 상수 | 다수 | `PROPOSED` 상태 | 플레이테스트 후 확정 |

영어 현지화 필수쌍 누락, 중복 ID, 잘못된 FK는 현재 0건이다. 미확정 값을 임의로 완성하지 않았으며 `NEEDS_RECIPE`, `NEEDS_ASSET`, `PROPOSED`로 구분했다.

---

## 13. 구현 완료 조건

- 모든 JSON은 스키마 검증을 통과한다.
- 중복 ID와 끊어진 FK가 없다.
- `ACTIVE` 데이터의 플레이어 표시문구는 한국어와 영어가 모두 존재한다.
- Day 0 칵테일과 재료가 튜토리얼에서 정상 해금된다.
- 잠긴 칵테일은 메뉴 클릭과 태그 필터 결과에서 제외된다.
- 동일 제조에서 필수 재료 누락이 두 번 감점되지 않는다.
- 핵심 재료 누락과 주문 불일치가 각각 최종 Sewage를 강제한다.
- Powder가 데모에서 자동 적용되고 점수 분모에 들어가지 않는다.
- 병맥주가 Open 뒤 12oz Pour로 이어지고 수량 점수에 들어간다.
- Shake는 20회, Stir는 10회에서 자동 종료한다.
- 제조 중 운영 타이머가 정지하고 테이블 복귀 후 재개한다.
- `order_cocktail_id`, `selected_cocktail_id`, Actual Craft가 서로 덮어쓰이지 않는다.
- 한국어와 영어에서 레시피 화면이 동일한 데이터 의미를 보여 준다.

---

## 14. 파일 소유와 수정 원칙

- 기획자는 엑셀의 마스터·규칙·한영 문구를 수정한다.
- 프로그래머는 엑셀을 직접 런타임에서 읽지 않고 검증된 JSON을 사용한다.
- 런타임 기록은 엑셀로 역수정하지 않는다.
- 기획값을 코드 상수로 다시 복제하지 않는다.
- `CONFIRMED` 값 변경, ID 변경, 행 삭제는 변경 기록을 남긴다.
- 현재 제안본은 별도 파일로 유지하며 원본 승인 없이 기존 데이터 파일을 덮어쓰지 않는다.
