# Project L.U.N.A — 기획 파트 작업 컨텍스트

이 폴더에서 클로드를 켜면 이 파일이 자동으로 읽힌다. **기획 파트 전원이 공유하는 파일**이라,
누가 켜든 프로젝트 상황과 규칙을 같은 기준으로 알게 하는 것이 목적이다.

- 사람이 읽는 정식 인수인계: `이준서/LUNA_인수인계.md`
- 깃·엑셀 사용법(비개발자용): 노션 「깃 구조 변경」 `3a61612298dc80109a58cfadd24fe179`
- 이 파일과 실데이터가 다르면 **실데이터가 정답**이다. 발견하면 이 파일을 고칠 것.

## 게임

사이버펑크 바텐더 내러티브 게임(Unity). 안드로이드 바텐더 '루나'가 바 '언노운'에서 13일을 보낸다.

**하루 구조(엔진 고정 순서)**: home(기상) → commute_in(출근길) → bar_open(개점 대화) → **1부**(랜덤 손님 실시간 응대)
→ **2부**(단골 비주얼 노벨) → 정산 → commute_out(퇴근길) → home(테라스) → dream → 다음날

**일정**: 8/31 넥스타 데모 제출(Day 0~3, 총 4일·EN 포함) ← 크리티컬 패스 / 8/25 텀블벅 사전예약 / 10월 페스트 /
11월 크로니클 전시 / 27년 1월 말 스팀 EA

**팀 11인** — 기획: 이준서(PD·연출·데이터)·고현정(시스템·컨텐츠)·이기현(시나리오)·임수진(컨텐츠·텀블벅·SNS) /
프로그래밍: 최용근(게임플레이)·한치우(코어) / 그래픽: 민수현(AD·배경)·전지민(서브배경·UI)·정유빈(칵테일·SD·애니)·한소윤(바 캐릭터·흉상)·신서연(서포터·UI)

## 이 폴더 구조

경로는 전부 이 폴더(`Document/`) 기준이다. 사람마다 리포를 받아둔 위치가 다르니 절대 경로를 쓰지 않는다.

```
Document/
├─ 데이터/                     데이터 파이프라인 본진
│  ├─ LUNA_System.xlsx         제조·운영 (주인: 고현정)
│  ├─ LUNA_Narrative.xlsx      대사·서사 (주인: 이준서)
│  ├─ LUNA_Draft.xlsx          대사 초안 (주인: 이기현)
│  ├─ json/                    빌드 산출물 — 게임이 읽는 23파일 + script/ 7파일
│  ├─ tools/                   build.py · draft_tools.py · gen_luna_data.py
│  └─ 구버전_데이터시트/          예전 엑셀 백업
└─ 고현정/ 이기현/ 이준서/ 임수진/   개인 작업 폴더
```

셸에서 한글 경로가 안 잡히면 유니코드 정규화(NFC/NFD) 문제다. python `glob`으로 실제 경로를 찾아 쓸 것.

## 데이터 파이프라인

```
LUNA_System.xlsx(10시트) ┐
LUNA_Narrative.xlsx(24시트) ┼→ build.py(96가지 검사) → json/ 23파일 + script/ 7파일 → 유니티
LUNA_Draft.xlsx(6시트) ─┘      검사 실패 시 json을 아예 만들지 않는다
```

```bash
cd 데이터
python3 tools/build.py               # 시트 → json (평소 쓰는 것)
python3 tools/draft_tools.py import  # Draft 전달완료 행 → Narrative (PD만)
```

script 구성: `script/bar/day0~3.json` + `home.json` + `street.json` + `cutscene.json`. 조회 키 (day, phase, seq), day null = 상시 씬.
Day 3은 선택 결과를 확인하는 최종일이라 현재 `script/bar/day3.json`은 빈 장면 배열이며, 실제 Day 3 장면은 `street.json`·`home.json`·`cutscene.json`에 들어간다.

## 절대 규칙

1. **`Document/` 밖은 건드리지 않는다.** 엔진(`HumanBartender/`)·아트(`ArtResources/`)·리포 루트 설정 파일 전부 포함.
   파일 하나 지우거나 `.gitignore` 한 줄 고치는 것도 안 된다. 배포 json을 유니티 `StreamingAssets/`로 옮기는 작업은
   **프로그래밍 파트가 허락하는 시점에** 따로 한다.
2. **`json/`은 빌드 산출물 — 직접 수정 금지.** 다음 빌드에서 사라진다. 에디터로 열어두지도 말 것
   (자동 저장 오염으로 `script/home.json`이 두 번 깨졌다).
3. **`gen_luna_data.py` 실행 금지.** 코드 시드로 시트를 재생성하는 도구라, 지금 실행하면 팀이 작업한 엑셀이 초기값으로 덮인다.
4. **엑셀은 파일마다 주인이 한 명.** 남의 파일은 열어보는 것까지만, 저장하지 않는다.
5. **엑셀에 컬럼을 새로 추가하면 `gen_luna_data.py`의 `COL_DOCS`에 설명도 추가**(안 하면 빌드가 경고).
6. **노션 문서를 수정하면 반드시 fetch로 재검증.** 배치 op 중 일부만 조용히 실패해도 성공처럼 반환된다.
   교체 문자열의 한글은 **리터럴로** 쓴다(유니코드 이스케이프 수기 작성은 오타 사고 다발).
   **수정은 부분 교체(update_content)로만** — 전체 교체(replace_content)는 빈 페이지 최초 작성에만.
   전체 교체는 사용자가 붙인 이미지·코멘트를 날린다(실사고 1회). 긴 신규 작성은 replace + insert 3~4조각(타임아웃 회피).
   검증은 fetch 1회 + 문자열 검색으로 충분 — 서브에이전트 전수 분석은 토큰 낭비.
7. **검증 규칙을 새로 넣으면 고의 위반을 주입해 실효성을 확인**한 뒤 원복한다.

## 깃

- 기획 브랜치 = **`Document`**. 구조: `main → develop → Document`
- 개인 브랜치 금지(엑셀은 병합이 안 된다). PD가 빌드 통과를 확인한 뒤 develop으로 병합.
- **커밋 제목**: `2026 0728 [분류] 무엇을 어떻게` — 분류 5종 `[데이터]/[대본]/[초안]/[문서]/[정리]`,
  숫자는 수치로 적고(180→200G), 한 커밋에 한 작업.
- `Document/.gitignore` 있음(엑셀 락파일·`__pycache__`·`.idea`·OS 찌꺼기). **유니티 `.meta`는 절대 무시하지 않는다.**

## 핵심 확정 사양

- **1부 시작** = 개점 대화(bar_open) 종료 시 자동. day1은 bar_open 씬이 없어 바 진입 즉시. OPEN 간판 클릭은 제거됨.
- **인내심(비가시 — 대사 4단계로만 전달, 티어 폐지)**: 코스터 = 22(고정)×성격 patience_mult, 하한 12·상한 60 /
  서빙 = 제한시간+max(10, 40−칵테일 해금일×3), 성격 미적용. 50%/80%에 urge/final.
  제조 정지 구간 = 칵테일 메뉴 진입 ~ 제공/버리기 선택 후 테이블 복귀(레시피 열람은 제조 시간 미포함, 버리기 재제조도 계속 정지).
- **주문 대사 3개**: 코스터 드롭 → ask_order(루나)→order_think→order 순서. 대사 사이 텀 = config `order_bark_gap_sec`(1.5).
  랜덤 손님은 personalities `think_chance`(0~1) 굴림 실패 시 order_think 생략, **카메오는 항상 재생**.
- **정산(제조 개편)**: `balance.json → settlement_rules` — 매출 = 가격×sale_rate + 가격×tip_rate×성격 tip_mult, 배상 = 가격×refund_rate.
  excellent만 팁 20%, sewage만 판매가 전액 배상(sale 0). **단골은 tip_mult 없이 일괄 1.0**. 골드 음수 허용.
  **주문 ≠ 제공 칵테일이면 점수 무관 강제 sewage**(config `force_sewage_on_order_mismatch`), 핵심 재료(is_core) 누락도 강제 sewage.
- **바의 루나 = 1인칭.** 초상·스탠딩 없이 화면에 나오지 않고, 대사창엔 이름+본문만. character_anim의 luna 항목은 예약(미사용).
- **2부**: order→craft→serve 3종 세트. 제조 게이트 = **craft 스텝**(arg `order`/`tutorial:id`).
  **serve 대상 = 직전 order 스텝의 actor**(씬 주인공으로 추론하면 틀린다).
- **표정**: 결정 = barks.expression → bark_situations(24상황) → default.
  **랜덤 손님 화면 반영 = guest_bodies의 파트별 `emotions` 맵**(현재 전부 null = 표정 고정, 감정 눈 아트가 오면 값만 채우면 켜진다).
- **랜덤 손님 외형**: 조합형 슬롯 9종 — 필수 6(body·outfit·eyes·eyebrows·mouth·hair) + 선택 3(outerwear·necklace·arm_accessory, '없음' 후보 상시).
  성격 = 웨이브 지정(필수), 성별 = 50/50 → (성별×성격) 유효 조합 풀(금지 쌍 제거, 사전 생성)에서 weight 곱 추첨.
  금지 쌍 = GuestBodyExclusions 시트(현 팔 액세서리×소매 상의 6행), 기본 조합 = GuestBodies `is_default` 마킹 → json `defaults`.
  선택 슬롯 '없음' 가중치 = config `guest_acc_none_weight`(1). 정본 = 노션 「1부 일반 손님 외형 랜덤 생성 시스템」(3bf1612298dc8004b2d3c766e8e3892b).
  남자 파츠는 실물 반영(의상 8·눈 3·눈썹 3·입 3·헤어 2·액세서리 3) — 여자 눈썹·입은 자리 행(아트 발주 ❗), 엔진 렌더러 구현 대기.
- **랜덤 손님은 day2부터** 등장(random_waves에 day1 없음). day1 바 = 크리스 튜토리얼 + 포트 첫 잔.
- **1부 지정 이벤트 = regular_slots 카메오.** cameo_scene은 서빙 후 재생, 씬 안 choice 스텝으로 선택지 가능.
  씬 재생 중(선택지 포함) 전 좌석 타이머 정지(제조 정지와 동일). 주문 칵테일 = `order` 컬럼 직접 지정(공란=그날 해금 풀 추첨,
  RandomWaves도 동일 — 티어 폐지), 받은 술 분기는 order_rules.json(현재 day3 삼호 가안 4행).
- 칵테일 29종(기존 16 + 신규 13 `status=tbd` — 수량 확정 파일 수령 시 qty 채우고 confirmed 전환). 병맥주 = mug + 뚜껑 + beer pour.
  카메오는 전용 대사만(공용 폴백 금지). 제조 정본 = 노션 「칵테일 제조 시스템」(3ae1612298dc800cb633f45c4ac4aec7) —
  제조법 표시는 recipe_desc 수동 문안(ko/en), 채점 정답표는 recipe 라인(is_core/auto_apply/scored 플래그, fill_up이 구 fill 컬럼 흡수)·glass·mix·prep·얼음 2필드(mixing/serving).
  잔 6종(cocktail/long_drink/old_fashioned/sour/wine/mug)·도구 2종(셰이커·믹싱글라스, 와인 오프너는 데모 제외 — 인수인계 §8-6).
  설탕·스퀴즈·얼음 = 데모 자동 적용·비채점(config `powder_demo_behavior`·`squeeze_demo_behavior`·`ice_demo_behavior`), 셰이킹 = 성공+실패 합 20스택 자동 종료,
  채점 = 종류별 대표 점수의 정규화 가중치 + 고정 감점, 구간 = `balance.json → score_bands`(수량 오차·시간 초과).
  Fill-up = 순서만 다른 Pour(`weight_pour` 공유). 레시피 밖 재료 = `shelf_items.json → default_action`·기본 목표량으로 기믹 생성 후 UNEXPECTED 감점만 적용.
  해금 = unlock_day 완전 수동(0일차 5종 시작, 파생 폐지), **대본 지정 제조(craft 스텝)는 해금 무시 + 필요 재료 노출**.
  이미지 키 = 파생(`Finished_{Pascal}`·`Serve_{Pascal}`), 태그는 Tags 시트(ko/en) 사전 강제 + **category 2분류(taste 맛 8종 / feel 느낌 8종)**, 등급 텍스트 = ui_grade_* 5키(언어 불문 영어).
- **선택지**: 조건(when) 미충족 항목은 숨기지 않고 **회색(비활성) + 부족 사유 문구**로 표시(사유 컬럼은 Choices 시트 열 추가 대기).
  세트 2~4개 + 세트마다 when 빈 항목 최소 1개(빌드 검증). 서사 스포일러는 선택지 잠금이 아니라 씬 단위 when으로 분기.
- **씬 day는 auto 씬에서만 재생 일차를 정한다** — interact 씬의 재생 일차는 interact_points.when이 정하고 day는 참고 표기.
  검증 4종(v3.2): 선택지 무조건 항목·kind↔scene_or_shop 접두사 일치·Personalities/Characters id 겹침 금지·지점 when(day==N)과 연결 씬 day 모순 금지.
- **거리(외부)**: 정본 = 노션 「외부 거리 시스템 모음」(3aa1612298dc80d18899ecca42884c41). 상호작용 5종, E 아이콘=발동 가능할 때만+최근접 1개,
  말풍선 이름형(씬에 루나가 화자로 참여할 때만 — NPC간 대화 관전도 무명형)/무명형, `interact_points.trigger`=interact/proximity,
  `actor`=지점에 선 캐릭터(같은 actor 지점 여럿=위치 이동), 소비 상태 세이브 저장(once 소비=씬 마지막 스텝 완료 시점·지점 id 기준,
  재개방은 conditional+when으로 설계), conditional 다중 매치=정의 순서 첫 번째, sequential 끝=마지막 씬 반복,
  자동 재생 동시 1개·직접 상호작용/기믹/포스터/phase 전환 시 즉시 소멸, phase 전이=문 진입. 시뮬레이터: `이준서/거리_시뮬레이터.html`.
- **오브젝트 보기 3원칙(거리)**: ① 말풍선은 오브젝트 위에만 — **루나 말풍선·독백 금지**(빌드 에러로 차단)
  ② 본문은 **그 사물에 적힌 정보만** — 관찰·감상·설명체 금지(전단이면 "사람 구함 010-…"이 그대로)
  ③ 줍기·구매처럼 **행동이 붙으면 선택지로**(상자=줍는다/무시한다, 자판기=뽑는다/그만둔다 — 루나 말풍선 없이 버튼만, 배치 기준 ❓).
  정보도 서사도 없는 오브젝트는 배치하지 않는다(전봇대 예시). 현재 지점 6·앵커 8, sequential 실사용 0.
- **거리 데이터 범위(PD 확정)**: 거리엔 외부에서 등장하는 장면만 넣는다. Day 3 결과 장면인 삼호 사망 목격·구출은 `street.json`에 포함하며, 집·꿈 장면은 각각 `home.json`·`cutscene.json`으로 분리한다.

## 설명 요령 (권장 — 규칙 아님)

기획자에게 시스템·데이터를 설명할 때 이해가 잘 됐던 방식. 강제가 아니라 참고용이다.

- 프로젝트에 이미 있는 이름으로만 부른다 — 시트명·컬럼명·trigger 값·`파일.json → 필드`. 새 용어를 만들어 부르면("조사형 씬" 등) 대화가 끊긴다.
- 문제는 개념 설명보다 **실데이터 행을 인용**해서 보여준다 — "p_lost_box의 when은 플래그뿐인데 연결 씬 day는 2" 식으로.
- 그 데이터가 실제 게임 화면에서 어떻게 되는지(버그라면 무슨 버그인지)까지 이어서 말한다.
- 결정을 요청할 때는 안을 2~3개로 나누고, 각 안마다 "무엇을 고쳐야 하는지"를 붙인다. 한 번에 결정 하나씩.

## 노션 정본 문서

| 문서 | ID | 역할 |
|---|---|---|
| 노션 문서 작성 규칙 | 3a81612298dc80b8a979c3e946a808a3 | 모든 기획 문서의 형식 규격 |
| 데이터 구조 작성 | 39f1612298dc80e89029fb739290812d | json 23파일 스키마(파일별 토글) |
| 바 내부 시스템 | 3a61612298dc807d8936efe5bde60468 | 규칙·흐름·공식 |
| 바 내부 UI | 3a81612298dc803eb7f5de1d9e2abbb4 | 화면 요소별 + 데이터 매핑 |
| 칵테일 제조 시스템 | 3ae1612298dc800cb633f45c4ac4aec7 | 제조 화면별 규칙·UI·데이터 연결 + §13 미결 설계 문제 |
| 깃 구조 변경 | 3a61612298dc80109a58cfadd24fe179 | 기획 파트 깃·엑셀 사용 안내 |
| 플머 표정 문서 | 3441612298dc80a5bb68c99d55650e3f | character_anim.json 정본 |
| 개발 마일스톤 | 3a21612298dc806d8765d380f3613faf | 일정 정본 |

**역할 분담**: 시스템=규칙·흐름·공식 / 데이터=스키마·값 / UI=화면 요소. 같은 내용을 두 문서에 쓰면 한쪽이 반드시 낡는다.

**문서 작성 규칙 요약**: 이력 표기 금지("PD 확정"·날짜·✅) · 수치는 실데이터 인용 · 데이터 참조는 `파일.json → 필드` 형식 ·
기호는 ❗(리소스 없음)·❓(결정 대기) 둘만 · 한글은 리터럴 · 데이터 문서는 필드별로 그 줄만 떼어낸 스니펫 + `//` 주석 ·
문장은 한 문장 한 규칙(— 부연·질문형·구어체 금지, 단정 표현은 검증 기준 있을 때만, 같은 규칙은 한 곳에만).

## 지금 열린 것

- **1일차 완성 최우선**: 바 내부 배경 팬 방식 결정 ❓ (1일차 재료 스프라이트 9종은 제작 진행 중)
- 사운드 전 항목 미발주(BGM 2·SFX 공용 5·전용 4), 선반 40종 중 25종 sprite 없음, 칵테일 이미지 신규 13종×2(대표·컷씬) 발주 필요(구엔진 재사용 4종 제외)
- 유니티 StreamingAssets/json은 7/30 배포본(제조 개편 **이전**) — 이번 개편 json과 다르다. 재배포는 플머가 신 스키마
  파싱을 갖춘 뒤 PD가 진행. 구파일 5종(expressions·script/common·day_1~3)도 그때 삭제
- 데드 콘텐츠: branch_choice 대상 0명, Tastes에 톰·하루·선하 0줄, 미완결 플래그 1건(`rios_accepted`).
  bees_knees·꿀 퀘스트(samho_honey)·상자 퀘스트(lost_box)·크리스 쪽지(d1_note)는 삭제 완료 — Quests 시트 현재 0행
- **일차 표기(0일차 스타트 적용 완료)**: 표시 = 데이터, Day 0(튜토리얼)부터. 상시 씬 센티널은 day 0 → **공란(json null)**.
  gen 시드는 구표기(1-based)로 적고 로드 시 `_shift_day` 블록이 일괄 −1(99 컨벤션은 유지, when 문자열의 day 비교도 함께 변환).
  표시 Day 3(선택 결과 최종일)까지 `Days`와 결과 장면 반영 완료. when 문법에 `meta.endings`(수집 엔딩 수, 세이브 밖 메타 저장) 있음 — 다회차 튜토리얼 스킵 조건용
- **제조 개편 데이터 반영 완료(08/18)** — 신규 시트 SettlementRules·ScoreBands(구 GradePayout 폐지), RecipeLines 9컬럼(플래그),
  Cocktails 22컬럼(status·color2·ice 2필드·unlock_day·time_limit_sec 수동), ShelfItems 4컬럼(default_action·prep_action·기본 목표량·단위),
  Config 계약 2.1.0(미니게임 초기값·정규화 점수 공식·자동 처리 3종), Tags category, RandomWaves/RegularSlots order 컬럼.
  플머에게 전달 대기: 신 스키마 파싱 + 0-based 일차 + `script/bar/day0~3.json` 파일명 + 셰이킹 20스택 + 제조/서빙 등급 분리 + 지정 제조 해금 무시
- 제조 잔여 결정: 신규 13종 수량(status=tbd — 확정 파일 수령 시 반영) ❓, 튜토리얼 칵테일 교체(크리스 dry_martini — 나중에 논의) ❓,
  「칵테일 제조 시스템」 §11·§13 미결 중 이번 개편으로 해소 안 된 항목 정리 필요
- **이펙터 인력 합류 확정** — 합류 시점에 맞춰 대본에서 이펙트를 부를 `Effects` 시트 신설 필요(현재 fx 실사용 3종·7회뿐)
- 유지비 미납 배드엔딩 — 규칙 확정: 정산(유지비 차감 포함) 확정 직후 골드 음수면 즉시 `bad_gold`(when `money < 0`) →
  씬 `ed_bad_gold` 재생(톰이 코라테크에 루나 정보를 팔아 가게를 살리는 텍스트 엔딩). bad_gold만 정산 시 판정, 나머지 엔딩은 최종일.
  남은 것: PD가 `이준서/유지비_배드엔딩_행추가.md`의 행을 Narrative 엑셀에 붙여넣기 + 일차별 유지비 금액 결정 ❓(현 전부 0)
- 거리 잔여 ❓: 자판기·전화 부스 사양(데모 범위), 포스터 표시 기준, 배경 어둡게 연출. 발주 ❗: 전단 포스터 2종·E키 아이콘
- Choices 시트에 잠금 사유 열(`lock_reason_ko`/`lock_reason_en`) 추가 대기 — PD가 엑셀에서 직접 추가(스크립트 저장은 헤더 메모 파손 위험).
  열이 생기면 스키마·검증(when↔사유 상호 필수)·json emit·문서 반영은 클로드가 이어서 한다
