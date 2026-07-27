# LUNA 인수인계 — 데이터·기획 작업 현황 총정리

작성: 2026-07-27 · 이 문서 하나로 "지금까지 한 것 / 지금 상태 / 앞으로 할 것"을 파악할 수 있게 쓴 인수인계다.
문서 안의 모든 수치·파일명·개수는 작성 시점의 **실데이터 기준**이다. 이 문서와 실데이터가 다르면 실데이터가 정답이다.

---

## 1. 프로젝트 개요

- **게임**: Project L.U.N.A — 사이버펑크 바텐더 내러티브 게임 (Unity). 안드로이드 바텐더 '루나'가 바 '언노운'에서 13일을 보내는 이야기.
- **하루 구조(엔진 고정 순서)**: home(기상) → commute_in(출근길) → bar_open(개점 대화) → **바 1부**(랜덤 손님 실시간 응대) → **바 2부**(단골 비주얼 노벨) → 정산 → commute_out(퇴근길) → home(테라스) → dream(꿈) → 다음날.
- **일정**: **8/31 넥스타 페스트 데모 제출(크리티컬 패스, Day1~3 범위, EN 포함 Unity 빌드)** → 8/25 텀블벅 사전예약 → 10월 페스트 → 11월 크로니클 전시 → **27년 1월 말 스팀 EA**. EA 범위는 day4 저작 실측 후 9월 말 결정.
- **팀 11인**:
  - 기획 4 — 이준서(PD·연출·데이터·서브 시나리오), 고현정(시스템·컨텐츠 — System 엑셀 담당), 이기현(시나리오 작가 — Draft 엑셀 담당, day4 실측), 임수진(컨텐츠·텀블벅·SNS)
  - 프로그래밍 2 — 최용근(게임플레이), 한치우(코어)
  - 그래픽 5 — 민수현(AD·배경), 전지민(서브 배경·UI), 정유빈(칵테일·외부 SD·애니), 한소윤(바 내부 캐릭터·흉상), 신서연(서포터·UI)

### 폴더 지도

| 위치 | 내용 |
|---|---|
| `Project/Human-Bartender/` | 팀 Unity 리포 (github.com/s9810282/Human-Bartender) |
| `Project/Human-Bartender/Document/데이터/` | **데이터 파이프라인 본진** — 엑셀 3개, tools/, json/ (`클로드/데이터`는 여기로 가는 심링크) |
| `Project/Human-Bartender/Document/이준서/` | PD 기획 문서 (이 파일, 깃 안내 2종, 데이터구조 안내서, 산출물/) |
| `Project/Human-Bartender/HumanBartender/` | Unity 프로젝트 (구엔진 리소스 `Assets/08.AddressableResource/`) |
| `클로드/junseo874.github.io/` | 웹 프로토타입 — **v2.3.1에서 동결**, 유지보수 종료. 참고용으로만 볼 것 |

---

## 2. 데이터 파이프라인 — 파일·도구·명령

```
엑셀 원본 (기획 저작) → tools/build.py (검증 — 실패 시 json 미출력) → json/ → 게임이 로드
```

### 저작 파일 3개 (소유자 1명 원칙)

시트 수는 모든 파일에서 **INFO 시트 제외** 기준이다(세 파일 모두 첫 탭이 안내용 INFO).

| 파일 | 소유 | 내용 |
|---|---|---|
| `LUNA_System.xlsx` (9시트) | 고현정 | Cocktails(17)·RecipeLines(41)·ShelfItems(40)·Personalities(5)·**GuestBodies(14)**·RandomWaves(7)·Config(31)·GradeCuts(5)·GradePayout(5) |
| `LUNA_Narrative.xlsx` (24시트) | 이준서 | Scenes(51)·Steps(818)·Choices(10)·Barks(169)·BarkSituations(24)·Characters(19)·Expressions·ExpressionParts·Cutscenes(21)·FieldAnims(18)·Days(3)·RegularSlots(1)·Spots(9)·InteractPoints(11)·Tastes(10)·Dossier(20)·Quests·QuestStages·Endings(5)·OrderRules(4)·AffinityMatrix(5)·UIStrings(35)·TextTags(7)·ResourceMap(17, 배포 제외 검수 대장) |
| `LUNA_Draft.xlsx` (6시트) | 이기현 | 대사 초안 원고지 — 대본 4탭(**Bar/Home/Street/Cutscene** — script/ 구획과 1:1, 분량 커지면 Bar_Day4 식 밑줄 탭 확장 가능) + Barks(상황 드롭다운 24종 완비) + NPC. 전달완료 행만 PD가 가져간다 |

### 배포 (json/ — 23파일 + script/ 6파일)

- 마스터 14: cocktails·shelf_items·characters·character_anim(구 expressions)·field_anims·cutscenes·personalities·**guest_bodies**·barks·bark_situations·tastes·dossier·ui_strings·text_tags
- 밸런스 1: balance (config 31키 + grade_cuts + grade_payout + affinity_matrix)
- 스케줄 5: days·random_waves·regular_slots·spots·interact_points(구 points)
- 서사 3: quests·endings·order_rules(구 orders)
- 대본: `script/bar/day1~3.json`(일차별) + `home.json` + `street.json` + `cutscene.json`. 로딩은 시작 시 home·street·cutscene, 일차 진입 시 그날 bar/dayN. 조회 키 (day, phase, seq), day 0 = 상시 씬.

### 도구·명령

```bash
cd Project/Human-Bartender/Document/데이터
python3 tools/build.py            # 시트 → json (평소 쓰는 것. 검증 실패 시 json 미출력)
python3 tools/draft_tools.py import   # Draft 전달완료 행 → Narrative 반영
python3 tools/gen_luna_data.py    # ⚠ 코드 시드 → 시트 "재생성" — 팀이 시트를 저작하기 시작하면 실행 금지(시트가 덮인다)
```

### 절대 규칙

1. **json은 빌드 산출물 — 직접 수정 금지.** 다음 빌드에서 사라진다. 실제로 `script/home.json`이 에디터에 열려 있다가 오타가 자동 저장돼 두 번 깨졌다. 열람만 하고 편집 모드로 두지 말 것.
2. **웹 프로토(junseo874.github.io)는 동결.** `bundle_proto.py`는 신구조와 비호환(의도됨) — 재실행 금지. 구현 대상은 유니티다.
3. **엑셀 신규 컬럼 추가 시 gen의 COL_DOCS에 설명도 추가**(안 하면 빌드가 경고).
4. 깃: 기획은 공용 `plan` 브랜치 1개(개인 브랜치 금지), PD가 빌드 확인 후 plan→develop 병합. 파이프라인 파일은 `Document/데이터/` 한 곳에만 — 개인 폴더에 xlsx 흩뿌리지 않기.

### 빌드가 보증하는 것 (구현자가 방어 코드를 줄여도 되는 목록)

모든 id 참조 유효 / L10N en 항상 비어있지 않음(미번역=ko 복사) / (day,seq) 유일 / 손님 주문 tier 풀 비지 않음 / DSL 문법 유효 / cutscene kind·스텝 type 일치 / barks 무결(situation FK·표정 유효·태그 문법·카메오 전 상황 보유) / **end_part가 그날 마지막 bar 씬에 존재** / **대본 exact: 주문은 그날 반드시 해금돼 있음** / 거리 씬 say arg는 field_anims 실재 action / guest_bodies 성별(2)×성격(5)×파트(4) 40개 풀 전부 ≥1. 검증 실패 시 json이 아예 안 나온다.

---

## 3. 핵심 확정 사양 (수치 포함 요약 — 상세는 노션 시스템 문서)

- **1부 시작**: 개점 대화(phase bar_open) 종료 시 **자동 시작**. day1은 bar_open 씬이 없어 바 진입 즉시(슬롯도 0이라 그대로 2부). OPEN 간판 수동 클릭은 제거 — 추후 도입 시 "오픈 전 대화 종료를 트리거로 간판 활성화" 방식(§7 백로그).
- **인내심(비가시 — 대사 4단계로만 전달)**: 코스터 = max(12, 22−tier×1.5)×성격 patience_mult / 서빙 = 제한시간+max(10, 40−tier×3), **성격 미적용**. 50%/80%에 urge/final 대사. 제조 중엔 전 좌석 인내심+스폰 타이머 정지.
- **정산**: 매출 = price×min(revenue_mult,1.0), 배율 1.2/1.0/0.7/0.3/−1.0. **팁 = 초과분×tip_mult (excellent만, 버림)**. **단골은 tip_mult 없이 일괄 1.0**(excellent=술값 20%). sewage·오제조 = 전액 배상. 골드 음수 허용. 서빙 성공 평판 변화 없음(이탈만 −1/−2). 최종 골드 = 누계+매출+팁−upkeep_gold(days.json, 현 시드 0).
- **카메오(regular_slots)**: 전용 대사만(공용 폴백 금지, 빌드 보증). `must_serve`=true면 인내심 없음·응대가 곧 1부 진행 조건(소프트락 아님). `serve_effects`=서빙 시 1회 DSL. 1부 카메오엔 취향·호감도 자동 적용 없음(serve_effects로만) — 자동 적용은 2부 serve 스텝 전용.
- **2부**: order→craft→serve 3종 세트(각 12회). 제조 게이트는 **craft 스텝**(arg: order=일반/tutorial:id=코치마크). **serve 대상 = 직전 order 스텝의 actor**(d2_aili_craft 둘째 잔은 부비가 주문 — 씬 주인공으로 추론하면 틀림). 인내심 없음. 종료는 마지막 bar 씬의 end_part 스텝.
- **표정**: 결정 = barks.expression(행 커스텀) → bark_situations 상황 기본(24종) → default. 카메오는 자기 세트+common. **랜덤 손님의 화면 반영 = guest_bodies의 파트별 emotions 맵**(표정→교체 스프라이트, 예: 오제조→화난 눈·excellent→웃는 눈) — 현재 전 항목 null이라 표정 고정으로 동작, 감정 눈 아트가 나오면 eyes 행의 emotions 값만 채우면 켜진다(코드·스키마 변경 없음). emotions 키는 common 세트 FK·default 등록 금지(빌드 검증).
- **랜덤 손님 외형(guest_bodies.json)**: 조합형 — 바디 2(남녀 각 1, 얼굴·코·입·패널 라인 한 장) × 의상 4(각 2: 민소매·아우터) × 눈 4(각 2) × 헤어 4(각 2) = **조합 16종**. 추첨 = 성별 → 성격 확정 → 성격 허용 항목 필터(personalities, 비면 공용) → weight 랜덤. 겹침 바디→의상→눈→헤어, 동시 착석 동일 조합 회피. 각 항목은 character_anim과 같은 이원 구조(mode=sprite 현행/parts_anim 예약) — 애니 전환 시 값만 바꾸면 됨. **감정 전환 연출은 감정 표정 아트가 나올 때까지 표정 고정**(표정 결정 데이터는 유지).
- **취함 분기**: max_rounds 3 + branch_choice=true 손님 전용, drunk_vomit_chance 0.4 — **현재 시드에 대상 손님 0명**(발동 안 함, 밸런스 확정 때 지정).
- **L10N**: 영어 번역 8월 중후반 착수. 그때까지 en 누락=경고(ko 폴백), 이후 `build.py --strict`.
- **day4~13 대본**: 9월 초 착수 시 자체 대본 텍스트 포맷+변환기 도입 예정(시트 직저작 아님, Ink/Yarn 비채택).

---

## 4. 노션 — 정본 문서와 링크

| 문서 | 링크 | 역할 |
|---|---|---|
| **노션 문서 작성 규칙** | https://app.notion.com/p/3a81612298dc80b8a979c3e946a808a3 | 모든 기획 문서의 형식 규격 (아래 §5에 전문 수록) |
| **데이터 구조 작성** (사양서) | https://app.notion.com/p/39f1612298dc80e89029fb739290812d | json 23파일 전 스키마 — 파일별 토글(실데이터 예시+필드별 스니펫). 프로그래머가 보는 정본 |
| **바 내부 시스템** (기획서) | https://app.notion.com/p/3a61612298dc807d8936efe5bde60468 | 규칙·흐름·공식 — §1 시스템 맵 / §2 공용 / §3 1부 / §4 2부·정산 / §5 예외 / §6 데이터 총괄 / §7 남은 할 일 / §8 검수 체크리스트. 하단 「구버전 기록」 토글은 폐기 사양(참고용) |
| **바 내부 UI** | https://app.notion.com/p/3a81612298dc803eb7f5de1d9e2abbb4 | 화면 요소별 블록(스크린샷 자리) + 연결 데이터 매핑 |
| 플머 표정 문서 | 노션 3441612298dc80a5bb68c99d55650e3f | 「캐릭터 표정 데이터 \| character_anim.json」 — loop 5종·클립 명명의 **정본은 이쪽** |
| 개발 마일스톤 | 노션 3a21612298dc806d8765d380f3613faf | 일정 정본 |

**역할 분담(중복 서술 금지)**: 시스템 문서=규칙·흐름·공식 / 데이터 문서=스키마·값 / UI 문서=화면 요소·데이터 매핑. 같은 내용을 두 문서에 쓰면 한쪽이 반드시 낡는다 — 상호 참조로 연결.

---

## 5. 노션 문서 작성 규칙 (규칙 페이지 전문 요약 — 새 문서는 무조건 이 기준)

### 공통 8칙 (모든 문서)

1. **보고 구현할 수 있게 쓴다.** 읽는 사람(주로 프로그래머)이 이 문서만 들고 질문 없이 코드를 짤 수 있는가가 완성 기준. "값 이름만 띡" 적는 것 금지 — 그 값이 뭘 의미하고 어떻게 소비되는지까지.
2. **데이터 참조는 정확한 위치로**: `파일.json → 필드` 형식. "데이터에서 가져옴" 같은 두루뭉술한 서술 금지. 수치는 실데이터 값을 인용(하드코딩 서술 금지 — 값이 바뀌면 문서가 거짓말이 된다).
3. **JSON 예시는 실데이터에서 가져오고 실제 파일처럼 줄바꿈**(키 한 줄씩 pretty-print). 한 줄 압축 금지, 가짜 예시 금지.
4. **이력·확정 표기 금지**: "PD 확정", "26.07.24 결정", "✅ 확정" 안 쓴다. 결정된 건 본문에 사실로 녹이고, 안 된 것만 "남은 할 일"로.
5. **부정형 상기 반복 금지**: "~는 없다"는 규칙이 정의되는 자리에 한 번만.
6. **기호는 두 개만**: ❗ = 데이터/리소스 아직 없음 / ❓ = 기획 결정 대기.
7. **한글은 리터럴로**, 명칭은 실데이터 표기 그대로(과묵형이지 조용형이 아님).
8. **문서끼리 역할 분담 + 상호 참조** (위 §4 표 참고).

### 데이터 문서 규격 (파일 하나 = 토글 하나, 구성 순서)

1. **한 줄 정체** + 전체 구조(`{...}` 형태)
2. **전체 실데이터 예시** — 항목 하나 통째로, **주석 없이 데이터만** pretty JSON
3. **필드별 설명** — 필드 하나마다 블록 하나: 위 예시에서 **그 필드의 데이터 라인만 떼어낸 스니펫 + `//` 주석**(들어갈 수 있는 값들), 더 필요한 설명은 스니펫 아래 문장으로
   - enum 필드는 반드시 4종 세트: ①들어갈 수 있는 값 전부 ②각 값의 의미 ③어떤 로직이 소비하는지 ④실사용 범위(미사용이면 "예약" 명시)
   - 값이 많으면(phase 8종처럼) 스니펫 아래 **【값】 전용 표**
   - 설명이 길거나 복잡한 필드는 표·주석에 때려박지 말고 **예시·가상 상황으로 풀어쓴다**
   - null·빈 문자열의 의미 명시
4. **하위 구조도 똑같이**(스텝·파츠 — 건너뛰기 금지)
5. **소비 로직** — 게임이 언제 읽어서 뭘 하는지 한 문단
6. 빌드가 보증하는 것과 예약 값(실사용 0)은 구분 표기

### 시스템 문서 규격 (절 구성)

시스템 맵(+전이 표) → 공용 기반 → 파트별 상세(상태머신은 표, **수치는 공식+실데이터 대입 계산 예시**, 연출은 시퀀스 표) → 예외 총정리 표 → 데이터 참조 총괄표(역인덱스) → 남은 할 일 → 구현 검수 체크리스트. 추상 서술("적절히 처리") 금지 — if문 쓸 수 있는 수준까지.

### UI 문서 규격

화면=대제목(#), **UI 요소 하나=### 블록 하나**(제목→설명 문단→연결 데이터 불릿). 표로 요소 합치기 금지(블록마다 스크린샷 붙일 수 있어야), "(이미지 첨부)" 자리표시 금지. 데이터 아닌 값은 "런타임" 표기. 문서 끝에 참조 데이터 역인덱스.

---

## 6. 지금까지 한 것 (주요 변경 타임라인)

- **7/17~19**: 신규 데이터 설계 확정(구 StreamingAssets 폐기). 제조=플레이어 선택 주도(선반 4단계, 가니시 채점 편입, 강제 논리 순서), ShelfItems 통합, GradePayout 도입, bar_open 페이즈 신설.
- **7/20**: 넥스타 데모 범위 Day1~3 확정. 데모 흐름 전면 구현(인트로·쪽지·포트·엘리베이터·삼호 조우·day3 생사 분기 3씬·시바·TV), 집 사이드스크롤, 외부 NPC 4모드(once/repeat/sequential/conditional), 단골 수첩(Dossier)+백로그.
- **7/22~23**: 저작 엑셀 2파일 분리(System/Narrative — 수정 권한 축), GuestSlots→RandomWaves+RegularSlots, Draft 파이프라인(이기현 원고지+import), 데이터 폴더를 팀 리포 `Document/데이터/`로 이사, 깃 전략(plan 브랜치), JSON 사양서 노션 게시, Orders→OrderRules·Points→InteractPoints 개명.
- **7/24**: 「바 내부 시스템」 완성(고현정 기획서 2종+UI PPT 병합). v2.3 일괄 반영 — 게이지류 UI 전면 제거(대사로만), 등급 배율 고현정안(1.2/1.0/0.7/0.3/−1.0), 난이도=슬롯 tier, 이름표 '손님' 데이터화, TextTags 신설+태그 전수 검증, 카메오 공용 폴백 금지, 조기 마감 기능 제거, must_serve/serve_effects 신설, 랜덤 손님 표정 2층 구조(BarkSituations+Barks.expression), **웹 프로토 동결 선언**.
- **7/25**: 구현 검수 결정 10건(서빙 patience 미적용·마지막 잔 판정·제조 중 스폰 정지·코스터 무제한·upkeep_gold 신설·골드 음수 등). 대본 배포 장소별 재편(bar/dayN+home+street+cutscene, d1_walk_home 삭제). expressions→character_anim 개명(플머 정합, loop 5종). 「바 내부 UI」 신설. **「노션 문서 작성 규칙」 페이지 신설**.
- **7/26**: OPEN 간판 수동 개점 제거(자동 시작). 데이터 문서 v3 전면 개편(필드별 스니펫+주석 형식). **전체 검수 대수정(v2.7)** — 생맥주 제거(17종)·병맥주 mug+beer pour, 2부 미해금 주문 8건 해소(재료 입고일 조정: 버무스 d1·보드카/라임/쿠앵트로/크랜베리/칼루아/우유 d2), day1 end_part를 d1_port 끝으로, 거리 arg default 82건→idle/빈값, 검증기 3종 신설(end_part 위치·주문 해금·거리 arg 엄격). 노션 3문서 실데이터 정합(Seat→L/M/R, choices 객체 구조, craft 게이트, serve 대상 규칙 등). Draft 대본 탭 4분할+상황 드롭다운 24종. 단골 팁=일괄 1.0 확정.
- **7/26~27**: **guest_bodies.json 신설(v2.8)** — SO 대신 JSON, 조합형 카탈로그(바디2·눈4·헤어4·의상4=14항목·16조합), sprite/parts_anim 이원 구조, personalities 필드(성격별 외형 예약, 40개 풀 검증). 노션 3문서 반영 완료.

---

## 7. 앞으로 할 일 (구체적 백로그 — 시스템 문서 §7과 동기)

### 데이터·기획 (담당 명시)

| 항목 | 내용 | 담당 |
|---|---|---|
| bark 시드 퇴고 | 현재 169행은 시드 문안. **재생성된 LUNA_Draft.xlsx를 이기현에게 새로 전달**하고(Barks 상황 드롭다운 24종·루나 화자 추가된 버전) 퇴고 → import | 이기현 |
| 3잔째 '돌려보낸다' 결과 | 기획 미정 ❓ | 고현정 |
| 취함 연출·손실 수치 | drunk_vomit_chance(0.4)만 존재 — 토함/청소 연출과 수치 정의 ❓ | 고현정·PD |
| branch_choice 대상 지정 | random_waves 전부 false — 밸런스 확정 때 max_rounds 3 손님 포함해 지정 (현재 취함 분기 데드 콘텐츠) | PD |
| 유지비 값 | days.upkeep_gold 구조 완성, 값은 전부 0 — 일차별 값 입력 | PD·고현정 |
| 부정 퇴장 골드 차감 | 도입 여부 추후 — 현행 평판만(−1/−2) ❓ | PD |
| day4~13 대본 | 9월 초 착수, 대본 텍스트 포맷+변환기 설계(이기현 데모 경험 반영) | PD·이기현 |
| 톰·하루·선하 취향 규칙 | Tastes 0줄 — 뭘 줘도 ok 판정인데 엔딩은 affinity 100 요구. day4+ 저작 때 필수 | PD |
| 미완결 플래그 2건 | `q_samho_honey_started`(벌꿀 퀘스트 수주 플래그 — 세우는 씬 없음, 비즈 니즈 잠김)·`rios_accepted`(bad_1 엔딩 조건) — day4+ 대본에서 세울 것 | PD |

### 아트·사운드 발주 연동

| 항목 | 내용 |
|---|---|
| 랜덤 손님 외형 | 제작 중(바디2·눈4·헤어4·의상4) — 완성 시 GuestBodies 시트의 sprite 가칭 키(`Guest/body_m` 등)를 실키로 교체, status=OK |
| 감정 표정 아트 | 데이터 구조 완성(guest_bodies.emotions — 표정별 교체 스프라이트 맵, 현재 전부 null=표정 고정). **감정 눈 아트 제작이 남은 전부** ❗ — 눈 4종 발주에 감정 변형(웃음·화남 등)을 얹으면 eyes 행 값만 채워 켠다 |
| 사운드 5종 | config에 키만 존재: sfx_guest_in/out(공용 입퇴장), sfx_drink_high/mid/low(마시는 소리 — excellent·good/decent/poor·sewage). 오디오 파일 발주·연결 ❗ |
| BGM 3종 | bgm_street_night·bgm_bar_calm(day1)·bgm_bar_main ❗ |
| 캐릭터 전용 SFX | SFX_chris_enter/exit·SFX_port_enter/exit·SFX_cat_meow ❗ |
| shelf_items 스프라이트 | 40종 중 25종 sprite=null(색 대체 표시 중) ❗ |
| 공용 표정 스프라이트 | character_anim common 8종 중 실물은 annoyingB/R 파츠 애니 2종뿐(ResourceMap 기준) |

### 프로그래밍 협의·전달

- 세 노션 문서(§4 표)가 구현 정본 — 특히 데이터 사양서의 "빌드가 보증하는 것" 목록(방어 코드 절감)과 tolerant reader 원칙(모르는 필드 무시) 전달.
- 텔레메트리는 필수 아닌 권장(마일스톤 W4 한치우 행 메모).
- 8월 중후반 번역 착수 후 `build.py --strict` 상시화.

### 인프라

- **`Document/` 폴더 git 커밋** — 아래 §8 참고. 이게 안 되면 팀이 데이터를 못 받는다.
- CI(GitHub Actions에서 build.py 자동 실행), 번역 export/import 도구, C# 코드젠+테스트 벡터 — M0 이후 백로그.

---

## 8. 중요 문제점 (지금 알아야 하는 것만)

1. **`Document/` 전체가 git 미추적 (치명)** — 데이터 파이프라인·기획 문서 전부(약 2.2MB)가 아직 커밋 전이라 팀 공유가 안 된다. 커밋 시: plan 브랜치 생성(origin/develop 기반) → Document/ 추가 → `Document.zip` 잔재 삭제 → 기존 추적된 .DS_Store는 `git rm --cached`. 아트 파일 3개가 삭제 상태로 미스테이징인 것도 같이 정리.
2. **레포 안 산출물 md 5종이 구스펙** — `Document/이준서/산출물/`의 설계서·JSON 설명서 등은 v2.0 이전 기준(expressions.json·orders.json·조기 마감·OPEN 간판 클릭이 그대로 적혀 있음). **팀에게는 노션 3문서가 정본이라고 안내할 것.** 레포 md는 참고 이력으로만.
3. **json 편집기 오염 사고 2회** — §2 절대 규칙 1 참고. 빌드 산출물을 편집 모드로 열어두지 말 것.

---

## 9. 작업할 때 주의 (경험으로 배운 것)

- **노션 MCP로 문서 수정 시**: 배치 중 일부 op만 조용히 실패해도 성공처럼 반환된다. **수정 후 반드시 fetch로 문서·블록 단위 재검증**. old_str의 한글은 리터럴로 쓸 것(유니코드 이스케이프 수기는 오타 사고 다발 — 곱/곳/곷, 컬/컴, 찡/징 등 실제 사례).
- **대형 노션 교체는 분할**: replace_content 한 방은 타임아웃 남 — replace + insert_content(end) 3~4조각.
- **gen_luna_data.py는 시드→시트 재생성 도구**: 지금은 Claude/PD가 스키마 바꿀 때 쓰지만, 팀이 시트에 직접 저작을 시작하는 순간부터 실행 금지(시트가 덮임). 그 시점에 검증 로직을 build.py로 분리하고 gen을 아카이브할 것.
- **검증기를 믿되, 새 규칙을 넣으면 고의 위반을 주입해 실효성 확인**(end_part 위치·주문 해금·guest_bodies 풀 검증 전부 이렇게 확인했다).
- Draft 구버전 백업: `구버전_데이터시트/LUNA_Draft_통짜Scenes_백업_0726.xlsx` (통짜 Scenes 탭 시절).
