# 🗄️ Project L.U.N.A — 데이터 구조 설계서

| 작성일 | 문서 버전 | 작성자 | 변경 사항 |
| --- | --- | --- | --- |
| 26.07.23 | 2.2.0 | Claude (이준서 세션) | **검증 구멍 4종 봉합 + 직관 개명 (PD 지시)** — ① **goto 검증 신설**(선택지·goto 스텝의 대상 씬 존재 — 문서에만 있고 코드에 없던 검사를 실구현) ② **배선 대기 리포트**: trigger=manual인데 어디서도 참조 안 되는 씬 목록(초안 반영 후 배선 누락 감시) ③ **Config에 `type` 컬럼**(int/float/str/bool 명시 — 신규 키도 엑셀 왕복 타입 보존, build의 시드 기억 방식 폐지) ④ branch_choice=TRUE & max_rounds<3 경고 + **카메오 voice 커버리지**(1부 카메오 단골도 bark 공백 검사 — port×order 등 6건 실검출: 카메오 포트가 주문 시 침묵 중이었음, 대사 작성 필요❓) ⑤ **개명 2건**: `Orders`→`OrderRules`(orders.json→order_rules.json — "주문 데이터"로 오독해 소유자 혼동 위험) / `Points`→`InteractPoints`(points.json→interact_points.json — 점수로 오독). 나머지 18파일·필드명은 유지 판정. 웹 런타임·번들·문서 동기화, 라운드트립·브라우저 검증 완료 |
| 26.07.23 | 2.1.1 | Claude (이준서 세션) | **초안 3탭 + 표정/동작 분리 (PD 확정)** — ① Draft를 '작성 모드' 3탭으로: `Scenes`(선형 장면 대화) / `Barks`(1부 랜덤 손님 한 마디 — 성격·상황 한국어 드롭다운→코드 매핑, **작가가 Barks를 쓸 통로가 없던 갭 해소**) / `NPC`(반복 조사 반응 — 방식 드롭다운→Points selection, 그룹 씬 골격 생성·기존 그룹 seq 뒤에 이어붙임, 사물은 루나 독백+obj 슬러그). 장소별 4분할(내부1부/2부/외부NPC/특정장소) 안은 "2부와 테라스는 같은 글쓰기 형태"라 모드 3분할로 조정 ② **say arg 의미가 phase로 갈림**: 바·집·꿈·인트로=흉상 표정(Expressions) / 거리(street·commute_*)=SD 동작(FieldAnims.action) — 검증기 신설([동작] 에러), 기존 거리 대사는 전부 default라 무손실 ③ **캐릭터 내부/외부 분리는 비채택** — 삼호·크리스가 경계를 넘음(정체성 1 + 표상 2벌 모델 유지) ④ 예시 데이터: Scenes 47줄(day1~3 실대본 역변환, 동작 예시 포함)·Barks 7줄(gentle×react_sewage 공백 채움 예시)·NPC 5줄(시바 순차·자판기 조건) — 전부 작성중 상태. 3탭 왕복 검증 오류 0 |
| 26.07.22 | 2.1.0 | Claude (이준서 세션) | **대사 초안 파이프라인 (PD 확정)** — 신규 `LUNA_Draft.xlsx`(작가 이기현 전용 원고지: day·장소(한국어 드롭다운→phase 매핑)·scene(가칭 허용)·speaker(Characters 드롭다운)·expression·text·지문·상태) + `tools/draft_tools.py`(init/import). import: 상태=전달완료 행만 → Narrative Scenes(trigger=**manual**, 배선 전 미실행)·Steps(지문→note, 발화자 없는 행→앞 스텝 note 병합, 없는 표정→기본 강등) 삽입. **사본 2개 원칙**: 초안(원고)↔Narrative(최종) — 중간 검토 시트는 두지 않음(동기화 지옥 방지), 전달 후 수정은 Narrative에서만. 중복 실행 안전(씬 id + day·가칭 이중 검사). 빌드는 Draft를 읽지 않음(입력은 여전히 System+Narrative 2파일). 검증 오류 0 왕복 테스트 완료 |
| 26.07.22 | 2.0.1 | Claude (이준서 세션) | **Tastes·AffinityMatrix → Narrative 이동 (PD 확정)** — 호감도는 단골 전용(랜덤 손님은 호감도 변화 없음)이고 취향도 '누구의 취향인가'라는 인물 정보라, 단골 관계를 설계하는 서사 담당의 파일로. System 8시트 / Narrative 22시트. 배포 JSON 무변(라운드트립 검증) — 시트 재배치는 배포와 무관함도 확인 |
| 26.07.22 | 2.0.0 | Claude (이준서 세션) | **저작 2파일 분리 — 시스템 vs 내러티브 (PD 확정)** — ① 분리 축은 '수정 권한'(xlsx는 git 머지 불가 → 파일 1개=소유자 1명): `LUNA_System.xlsx`(제조·운영 — Cocktails·RecipeLines·ShelfItems·Personalities·Tastes·RandomWaves·Config·GradeCuts·GradePayout·AffinityMatrix, 10시트) / `LUNA_Narrative.xlsx`(대사·서사·연출 — 대본·Barks(랜덤 손님 대사 포함)·단골·퀘스트·캐스트 등 20시트). 구 통짜본은 `_archive/`로 ② **GuestSlots 폐지 → RandomWaves(랜덤 웨이브, personality 기반) + RegularSlots(단골 슬롯, character 필수) 분리** — seq는 하루 공용 번호(스폰 병합 기준), 겹치면 빌드 에러. 배포도 `random_waves.json`+`regular_slots.json`으로 분리(**JSON 20파일**) ③ build.py: 기본 입력 2파일, **같은 시트가 두 파일에 있으면 에러**(참고용 복사→유령 데이터 차단), 빌드리포트에 파일별 시트·행수 표기 ④ **Barks 커버리지 검사** 신설 — 성격×핵심 상황에 전용·공용 대사가 모두 0줄이면 경고(분리로 안 보이게 된 System↔Narrative 의존을 빌드가 감시; 첫 실행에서 gentle×react_sewage 실검출) ⑤ INFO 시트를 파일별 맞춤(표지색 System=녹/Narrative=보라, '옆 파일과의 약속' 절) ⑥ 웹 런타임은 두 테이블을 seq 병합 — 라운드트립 완전 일치 검증 |
| 26.07.20 | 1.9.7 | Claude (이준서 세션) | **엑셀 시트 셀 메모 전면 보강 (PD 요청)** — ① COL_DOCS를 **전 컬럼(197개) 100% 커버**로 확장(기존 82개/41%). 메모 없던 9시트(Personalities·Dossier·Days·Spots·Orders·Endings·GradeCuts·GradePayout·AffinityMatrix·UIStrings) 포함 ② **SHEET_DOCS 신설** — 각 시트 첫 컬럼 메모에 "이 시트가 무엇인지"를 함께 표시 ③ 메모 상자 크기를 내용 길이에 맞춰 자동 조절(잘림 방지), (파생) 컬럼은 전용 안내문 ④ **INFO 시트 재작성** — 사용법 5단계·시트 지도("무엇을 고치고 싶은가요?" → 해당 시트 안내)·규칙 5개·when/effects 빠른 참고·탭 색상 범례. 헤더 스타일링 적용 ⑤ 빌드에 "설명 없는 컬럼" 경고 추가(신규 컬럼 추가 시 메모 누락 방지) ⑥ **버그 수정**: Config의 float 값이 엑셀 왕복에서 정수로 뭉개지던 문제(-1.0→-1) — build.py가 시드 타입을 기억해 복원. JSON 왕복 완전 일치 검증 |
| 26.07.20 | 1.9.6 | Claude (이준서 세션) | **넥스타 데모 흐름 전면 구현 (PD 확정 흐름)** — ① 신규 씬 12종: `d1_intro`(검은 화면 인트로, phase=intro 신설)·`d1_note`(쪽지 — 읽어야 출근)·`d1_port`(포트 첫 잔 진피즈)·`d1_elevator`(퇴근길 라디오 — 구엔진 elevator_radio 이식, radio 캐릭터 신설)·`d2_meet`(출근길 삼호·시바 조우)·`d2_home_talk`·**day3 분기 3씬**(사망 목격/구출/크리스 목격 — 데모 양갈래 엔딩)·시바 NPC 그룹 2씬(구엔진 shiba.json 이식, p_shiba 포인트)·홀로그램 TV 2씬(집) ② **effects DSL `alive.x = true/false`** 신설(검증기+런타임) ③ Days.start_phase 전 일차 home(집 기상 시작) ④ **집 내부 사이드스크롤**(PDF 배치도: 침실→현관→TV→소파→창→테라스, 아침/저녁 모드, 소파 '하루 마치기', 테라스 강제 대화 이동) ⑤ 거리 축소(1640→1230)+Shift 달리기+day1 비 연출 ⑥ **튜토리얼 정합**: 구 리듬게임 설명(타이밍 커서·히트 노드·좌클릭) → 선반 4단계 설명으로 교체(미결 #5 해소), craft 스텝 tutorial: 전환, **제조 코치마크 11종**(선반 4+기믹 7 — 튜토리얼 한정 1회) ⑦ runDay 재구성: 인트로→집→출근→바→정산→퇴근(엘리베이터)→분기엔딩/테라스→꿈. 씬 52·스텝 822 |
| 26.07.20 | 1.9.5 | Claude (이준서 세션) | **단골 수첩(Dossier) + 대화 백로그 (PD 확정)** — ① 신규 시트 `Dossier`{character_id, min_affinity, kind(desc/taste/history/secret/recent), when, text ko/en}: 바 UI 탭 '단골 수첩'. **만난 인물만 노출(미만남=???), 호감도 10 이하=이름만, min_affinity 단계마다 항목 해금**(잠긴 항목은 "🔒 호감도 N" 티저 — 호감도 시스템 체감 장치). recent는 when으로 진행 연동(삼호 생사 루트별 근황 데모 수록). 배포 `dossier.json`(19파일) ② 런타임 `S.met`(만남 — 2부 등장·카메오 서빙 시 기록, 세이브 포함) ③ **대화 백로그**: 재생된 say를 세션 로그(200줄)로 — 데이터 구조 변경 불필요 확인. HUD에 📒수첩·💬기록 버튼 |
| 26.07.20 | 1.9.4 | Claude (이준서 세션) | **외부 NPC 대화 4모드 런타임 구현** — Points.selection(once/repeat/sequential/conditional)이 데이터에만 있고 프로토가 구 필드(repeatable)를 참조하던 갭 수정. `scene_or_shop="group:이름"` → 그룹 씬(seq 순)에서 해석: sequential=볼 때마다 다음 씬·소진 후 마지막 반복(구엔진 revisit_repeat), conditional=when first-match(구엔진 시바 flow), once=영구 소진(usedPoints 키를 day별→영구로), repeat=상시. `S.groupProgress` 세이브 편입. 구엔진 shiba.json 패턴(1회성 조우→조건부 flow→반복 폴백·set_flag 트리거)이 Points+Scenes(group·when)+Steps(effects)로 전부 표현됨을 검증 |
| 26.07.19 | 1.9.3 | Claude (이준서 세션) | **2부 좌석 인접 강제 + 선택 가시성 (PD 지시)** — ① 2부 스탠디는 **항상 붙어 앉는다**: 둘째 손님이 대본상 반대편(L↔R)이라도 기존 손님의 옆자리로 스냅(dialogue.castAdd — 요청 방향 우선, 재등장 시 자기 좌석 제외). 카메라는 2명일 때 **딱 2좌석 창 고정 줌**(TWO_SEAT_SPAN=1420, zoom≈0.90) — 3좌석 와이드 프레이밍 제거로 배경 리소스 절약. 대본의 L/M/R은 '의도'로 유지(엔진이 보정) ② 제조 선반 선택 셀에 금색 발광+✓배지+확대(.sel 공통) — 담김 상태 가시성 |
| 26.07.19 | 1.9.2 | Claude (이준서 세션) | **L10N 단계 완화 (PD 확정 — 번역은 8월 중후반 착수)** — en 누락이 기본 빌드에서 에러→**경고 집계**로(건수+샘플을 리포트에 표시). emit 시 en 공란은 **ko로 폴백**(EN 모드에서도 빈 텍스트 없음, 번역 입력 시 자연 치환). `build.py --strict`가 기존 에러 동작 — 번역 완료 후 상시 사용 |
| 26.07.19 | 1.9.1 | Claude (이준서 세션) | **선반 슬롯 UI + 개점 간판 (PD 지시)** — ① 제조 선반 4종을 **좌우 슬롯식**으로 전환: 상단에 슬롯 탭(잔·도구·가니시·재료, 클릭 시 직접 점프), 하단 **◀▶ 화살표로 좌우 이동**, 마지막 슬롯에서만 '제조 시작' 노출. 첫 슬롯의 ◀는 메뉴로 복귀 ② **개점 페이즈(`bar_open`) 신설** — 바에 들어가면 바로 손님이 오지 않고, **크리스와 짧은 대화 → 플레이어가 OPEN 간판을 클릭**해야 1부가 시작된다(자동 시작 금지). day2 "어제 배운 대로 할 수 있겠지?"·day3 "어제는 나쁘지 않았다" 대사 신설(씬 39·스텝 720). 간판은 CLOSED('준비 중')→클릭→플립 애니메이션→OPEN('영업 시작') ③ UIStrings 7종 추가(간판·슬롯 라벨) |
| 26.07.19 | 1.9.0 | Claude (이준서 세션) | **선반 4단계 제조 + 스키마 6종 개편 (PD 확정)** — ① **제조 화면 = 종류별 선반 4단계**(①잔 →②도구 →③가니시 →④재료 → 제조 시작). 각 화면 ◀이전 되돌아가기 가능(선택 유지, **제한 시간은 계속 흐름**). 실행은 기존 강제 순서(개봉→따르기→스퀴즈→파우더→믹스→필업) 유지 ② **가니시가 비인터랙티브 연출 → 플레이어 선택·채점 항목으로 승격**(scoring_items +1). 가니시 없는 칵테일도 화면을 띄우고 "없음"을 직접 고르게 한다(건너뛰면 정답 힌트가 되므로). 완성 화면은 정답이 아닌 **플레이어가 고른** 가니시를 보여줌 ③ **Ingredients + Items → `ShelfItems` 한 테이블 통합**(`kind`=ingredient/glass/tool/garnish). 넷 다 '선반에서 고르는 것'이 되었고 unlock_day·unlock_when·shop_price가 전 kind 공통이 됨(가니시도 입고 대상). 입고 화면엔 소모품만 표시. **kind 오배치 검증** 신설 ④ **`unlock_day_override` 완전 수동화**(기존 max() → 그 값 그대로, 앞당김 가능) + 재료보다 이른 해금 시 경고. **`tier_override` 신설**(기믹 수 ≠ 체감 난이도 보정) + **일차별 티어 풀 0종 검증** 신설 ⑤ **`TipRates` → `GradePayout`**(revenue_mult·tip_mult): sewage·오제조만 −전액 배상, poor는 배상 없음(PD 확정). 미사용 `wrong_cocktail_money` 제거 → `wrong_cocktail_revenue_mult` ⑥ **ResourceMap 배포 제외**(런타임 미사용 에셋 검수 대장 — 시트에만 유지). 폐지 파일 자동 정리 로직 추가. **배포 JSON 20 → 18파일** |
| 26.07.19 | 1.8.2 | Claude (이준서 세션) | **제조 실행 모델 정정 (PD 피드백) — "집는 즉시 발동" → "담기→시작→순서 실행"** — 선반에서 재료·도구를 먼저 다 담고(토글 큐, 트레이에 실행 순서 번호 미리보기) '제조 시작'을 누르면 담은 것들이 **강제된 논리 순서**(①개봉→②따르기(베이스 먼저)→③스퀴즈→④파우더→⑤믹스→⑥필업)로 차례차례 실행. 담은 순서와 무관하게 재정렬 → **술을 안 따르고 젓는 상황이 원천 불가능**. 코르크/뚜껑 병을 담고 따개를 안 담으면 시작 차단(개봉은 그 병 따르기 직전 자동 삽입). §3 제조 원칙·RecipeLines 설명 정정, craft.js 재작성(buildRunList/toggleQueue/startRun/runNext), 웹 프로토 실동작 검증(순서 재정렬·코르크 선행·체인 완주) |
| 26.07.18 | 1.8.1 | Claude (이준서 세션) | **문서 정합화** — §2 테이블 맵을 실제 30시트·배포 20파일 기준으로 갱신(누락 8시트 보강, 시트↔JSON 대응 열 추가), **§2-A 배포 JSON 스키마 신설**(전 파일 실제 형태·공통 규칙·엔진 로딩 계약). 구 master.json 통합본 설명 제거 |
| 26.07.18 | 1.8.0 | Claude (이준서 세션) | **저작=통짜 1파일, 배포=도메인별 JSON 분할 (PD 확정)** — ① 저작 원본은 `데이터/LUNA_Data.xlsx` 단일 파일 유지(관리 단순) ② 배포 JSON을 구엔진처럼 **도메인별 낱개 파일**로 분할: 통합 master/schedule 폐기 → 마스터 12종 + balance(튜닝 계수 4종 묶음) + days/guest_slots/spots/points + quests(스테이지 포함)/endings/orders = **20파일** + script/day_N.json(대본만 하루 단위) — 분할 기준은 "같이 로드·같이 튜닝·단독 무의미"만 묶기. 엔진이 필요한 파일만 로드·부분 패치 가능 ③ **전 시트 기본키 중복 검사** 신설(복붙 실수 차단, 실험 검증) ④ 프로토 번들러 정식 스크립트화(`tools/bundle_proto.py` — 분할 JSON을 런타임 메모리 구조로 조립, 배포 구성이 바뀌어도 런타임 무수정) ⑤ script/day_N 출력의 일차 하드코딩 제거(데이터에 있는 일차 자동 출력) |
| 26.07.18 | 1.7.0 | Claude (이준서 세션) | **제조 = 플레이어 선택 주도 확립 + 구엔진 실대본 이식** — ① **기믹은 레시피가 아니라 플레이어의 선반 선택으로 발동**(재료를 집으면 그 재료의 기믹, 도구를 집으면 그 도구의 기믹). RecipeLines/mix/prep은 실행 순서가 아니라 '채점 정답'으로 의미 확정 ② Cocktails에 **prep 컬럼**(공란/cap/cork) 신설, mix의 bottle_open 폐기 → prep=cap. 와인·샴페인 prep=cork ③ 도구 통합: 병따개+와인오프너=**따개**, 바스푼→**믹싱 글라스&바 스푼**(Items) ④ **코르크 기믹** 신설 — 천천히 돌려 2바퀴, 과속 시 코르크 부스러짐(품질 하락이 채점 반영) ⑤ 셰이크·스터 자유형 전환(플레이어가 멈춤, 정답량 근접도 채점: shake 8회/stir 3바퀴/build 1바퀴) ⑥ 채점 개편: prep 항목·도구 정오·불필요 행동 감점 추가, 티어·채점항목 파생식에 prep 반영(기존 티어 불변) ⑦ **대본 지정 제조는 해금 무시**(메뉴·선반에 타겟 칵테일/재료 노출 — 구 대본의 마티니 튜토리얼 등 대응) ⑧ **구엔진 실대본(day0·1·2.json) 634스텝을 바 2부에 이식**, 가안 대체(day_bar_scripts.py + converted/). 리치텍스트 태그(order/name/world) 렌더 지원. 벌꿀 퀘스트 데모는 실대본 재배치 대기로 보류 |
| 26.07.18 | 1.6.0 | Claude (이준서 세션) | **취향 시스템(Tastes) 신설** — 캐릭터별 first-match 취향 규칙 시트(when DSL 재사용). 칵테일 조건만 쓰면 고정 취향, flag 등을 섞으면 **상황부 취향**("그때그때 다른" 선호)이 같은 문법으로 표현됨. 서빙 호감 = AffinityMatrix[취향][등급], 2부 스토리·1부 카메오 서빙 공통. 우선순위 사다리(장면 정답 > 상황부 > 기본 > ok) 명문화, 검증 규칙 19-1, 호감도 시뮬레이션이 취향 기반으로 정밀화. 웹 프로토 실동작 검증(아일리 샴페인 love×good=+4) |
| 26.07.18 | 1.5.0 | Claude (이준서 세션) | **정식 시트 빌드 확립 + 검증 4종 추가** — ① `데이터/tools/build.py` 신설: LUNA_Data.xlsx를 직접 파싱해 검증→JSON 출력(기획자 저작 개시 가능). 시드 JSON과의 라운드트립 완전 일치 검증, 타입 명세(NULLABLE/BOOL/FLOAT 컬럼) 포함. gen_luna_data.py는 시드 재생성 전용으로 강등 ② when DSL 전수 문법·참조 검사 ③ effects DSL 전수 문법 검사(오타 효과의 조용한 무시 차단) ④ 플래그 교차 검사(참조-미설정 경고) ⑤ 호감도 상한 시뮬레이션 리포트(엔딩컷 100 대비 일차별 %) — §12 검증 규칙 16~19 |
| 26.07.18 | 1.4.0 | Claude (이준서 세션) | **퀘스트 시스템 명세 확립 (§10 전면 재작성)** — ① 수명주기 모델(수주 플래그 → goal → stage → 보상 자동 1회)과 Quests/QuestStages 전 컬럼 상세 ② goal 문법 확정: `interact:<포인트>` / `serve:<칵테일>`(정상 제공 판정 — 오제조·Sewage 제외, 1부·2부 공통 훅) + 자동/수동(advance) 진행 2경로 구분 ③ 보상 설계 패턴: 레시피=unlock_recipe(즉시) / **재료=unlock_day 99+unlock_when(다음날 stock_in 입고 연출)** 컨벤션 명문화 ④ 연계 패턴(`quest.<id>.stage`·완료 플래그) ⑤ 검증 규칙 4종 추가(§12 #12~15: 퀘스트 구조·goal 문법·effects 전수 스캔·해금 경로) ⑥ 실데이터 데모 `samho_honey`(day3 삼호 — 진피즈↔벌꿀 거래 → 히든 레시피 '비즈 니즈') 워크스루 수록, 웹 프로토에서 전 구간 실동작 검증 |
| 26.07.18 | 1.3.0 | Claude (이준서 세션) | **에셋 검수 반영 + 시트 가독성** — ① Assets 전수 검수(스프라이트 900·표정 프레임 216·컷씬 649프레임·playable 17) 결과: 신 시스템 전환에 따른 리소스 손실 사실상 없음. 유일 결정 필요 = 바 내부 BG 1280 ↔ 포커스 카메라(줌인 팬 vs 좌우 연장) ② **ResourceMap 시트 신설** — 데이터 키↔실제 에셋 경로 공식 매핑(Ily=aili, 3=samho, common=Default 등) + 검수 상태(OK/일부/확인필요/결정필요/폐기/보관) ③ Cutscenes에 실존 playable 17종 실등록(+예약 4) ④ 엑셀 가독성: 그룹별 탭 색상(🔵마스터/🟢스케줄/🟣대본/🟠밸런스), 전 시트 헤더 셀 메모(컬럼 설명 툴팁), 짝수행 줄무늬, 자동필터, (파생) 컬럼 회색 표시, 긴 텍스트 줄바꿈 |
| 26.07.17 | 1.2.0 | Claude (이준서 세션) | **표정·연출·외부 오브젝트 확장** — ① Expressions/ExpressionParts 시트 신설(구엔진 character_anim.json 이식: 표정=스프라이트 1장 ↔ 파츠 7종 애니메이션 혼용, 루프 모드 always/always_on_dialogue(입 움직임)/once/none/special_on_dialogue, common=랜덤 손님 공용 세트) ② Cutscenes 매니페스트(timeline 주력+스파인 GIF) ③ Steps 확장: expr/anim/emote/timeline/gif 타입 + sync(no_wait 병렬) ④ Scenes: trigger 확장(pass:/event:/manual)+skippable+group, day 0=상시 공용 씬(script/common.json) ⑤ Points: repeatable→selection(once/repeat/sequential/conditional) — 구엔진 outside_objects.json 4종 이식·검증 완료. 상세 근거는 [시스템흐름·컷씬 검토서](LUNA_시스템흐름_컷씬확장_검토서_v1.0.md) |
| 26.07.17 | 1.1.0 | Claude (이준서 세션) | PD 피드백 반영 — ① L10N ko/en 동시 저작(전 텍스트 쌍 필수, UIStrings 시트 신설) ② 재료 조건부 해금(unlock_when)·칵테일 자체 해금(unlock_day_override) ③ 입고(stock_in) 페이즈를 바 오픈 직전으로 이동 ④ 1부 스토리 카메오 슬롯(character/cameo_scene) ⑤ 루나 대사 규칙(행동 직전 금지) 명문화+검증 ⑥ 시간대 고정(출근 19:00/퇴근 02:00, 밤 단일 배경) ⑦ 저장 이원화(집 수동 저장 + 크래시 복구 스냅숏) |
| 26.07.17 | 1.0.0 | Claude (이준서 세션) | 최초 작성 — 시트 기반 저작/JSON 배포 이원화, 하루 페이즈 모델, 전 테이블 스키마, when/effects DSL, 파생 규칙, 빌드 파이프라인 |

---

## 0. 🎯 설계 목표와 반영된 결정

### 이 문서가 대체하는 것
기존 StreamingAssets 구조(day1.json 94KB 단일 중첩 덩어리, 칵테일별 flavor_text에 리치텍스트 하드코딩, 해금/대본/밸런스 혼재)를 **전면 폐기**하고 새로 설계한다.

### 확정 결정 (26.07.17 세션)
| # | 결정 |
| --- | --- |
| A | **day 0과 day 1은 해금상 같은 날** — Day1 입고분(진·맥주·레드와인·샴페인)은 `unlock_day = 0`으로 지정, 튜토리얼(day 0)부터 사용 가능 |
| B | **테라스 대화는 강제 이벤트** — 집에서 "하루 마치기"(침대/소파) 선택 시, 그날 home 씬이 존재하면 수면 전에 강제 실행 |
| C | **정산 화면은 2부 마감 후, 바에서 나가기 직전** — 골드 자체는 서빙 즉시 실시간 반영(퇴근길 상점 대비) |
| D | 꿈(연구소) 씬 배치는 스토리 문서 원안 유지 (day1,2,3 습격 → day5=이전-3, day6=이전-4, day9=이전-2, day11=이전-1) |
| E | 칵테일은 **일단 17종**(Day1~5 해금분)만 데이터화, 30종 확장 전제 |
| F | **일차 넘버링 변경** — 1=튜토리얼(구 day0), 2=구 day1 … 13=구 day12. 해금은 구 Day0/1 통합 → 새 Day1 배치 |
| G | **해금의 주체는 재료** — 입고 재료가 만들 수 있는 칵테일을 파생시킨다. 입고는 플레이어 행동으로 추가·변경 가능(`unlock_when`). 재료가 있어도 이벤트/일차 전엔 못 만드는 칵테일은 `unlock_day_override`/`unlock_when`으로 |
| H | **입고(stock_in) 화면은 그날 바 오픈 직전** — 출근길 다음. day1은 초기 재고라 생략 |
| I | **L10N은 영어 먼저** — UI·대사·칵테일 정보 전부 ko/en 쌍으로 저작. 영어 누락 = 빌드 에러 |
| J | **1부 랜덤 손님에 스토리 인물 카메오 가능** — 짧게 마시고 짧게 말하고 퇴장, 비중 최소 |
| K | **시간대** — 출근 오후 7시 / 퇴근 새벽 2시. 배경 리소스는 밤 고정 1벌(시간대별 교체 없음) |
| L | **저장** — 수동 저장은 집의 저장 오브젝트에서만. 강제 종료 대비 크래시 복구 스냅숏 별도(§11) |

### 설계 원칙 6개
1. **모든 저작 데이터는 평평한 행(row)이다.** 엑셀 1시트 = 테이블 1개. 깊은 중첩 금지. 기획자는 JSON을 절대 만지지 않는다.
2. **저작 포맷 ≠ 배포 포맷.** 시트(저작) → 변환기 → JSON(배포, 엔진이 먹기 좋은 형태로 재조립). 둘을 잇는 것은 빌드 스크립트 하나.
3. **단일 진실 원천.** 파생 가능한 값(티어, 칵테일 해금일, 채점 항목, 제한시간)은 손으로 쓰지 않고 빌드 타임에 계산해서 굽는다. 손값과 계산값이 다르면 빌드가 경고한다.
4. **서사는 전부 하나의 문법.** 2부 대사·출퇴근 이벤트·테라스 대화·꿈·엔딩·튜토리얼까지 전부 동일한 **씬-스텝** 스키마. `phase` 컬럼이 하루 중 언제 꽂히는지, `when` 컬럼이 분기(생사/호감/플래그)를 결정한다. 엔진의 서사 실행기는 1개면 된다.
5. **조건과 효과는 미니 DSL 문자열 한 칸.** 어휘를 고정하고(§7) 파서 1개(ConditionEvaluator)로 전 시스템(대본 분기·주문 규칙·해금·엔딩)을 처리한다. 인수인계 문서 §22의 when 시스템 계승.
6. **텍스트는 전부 키가 될 수 있게.** 시트에는 ko 원문을 쓰되 변환기가 `{시트}_{행ID}` 규칙으로 키를 자동 생성 → L10N(EN/JP) 확장 시 테이블만 추가.

---

## 1. 📅 하루 페이즈 모델 (엔진 고정 순서)

하루는 아래 페이즈를 **고정 순서로** 통과한다. 각 페이즈는 "그 day + 그 phase에 등록된 씬 중 when을 통과한 것"을 실행하고, 없으면 조용히 넘어간다. **day 0의 튜토리얼도, day 12의 리오스도 전부 이 규칙 하나로 처리된다** — 특수 분기 코드가 필요 없다.

```
1. commute_in    출근길 19:00 (횡스크롤·밤) — auto 씬 실행 + interact 포인트 활성
2. stock_in      입고 화면 — 오늘자(unlock_day == day) 신규 재료 + 새로 가능해진 칵테일 (없으면 스킵, day1은 초기 재고라 생략)
2.5 bar_open     개점 전 — 크리스와 짧은 대화(phase=bar_open) → **플레이어가 OPEN 간판을 걸어야** 1부 시작
                 (자동 시작 금지. 슬롯이 있는 날만. v1.9)
3. bar_part1     일반 영업 — 1부 슬롯(random_waves+regular_slots 병합) 소진까지 (0개면 즉시 통과)
4. bar_story     2부 스토리 손님 — bar 씬 순차 실행 (day1 튜토리얼 씬도 여기)
5. settlement    정산 화면 (결정 C: 바에서 나가기 직전. 골드 자체는 서빙 즉시 반영)
6. commute_out   퇴근길 02:00 (횡스크롤·밤) — auto 씬 + interact 포인트
7. home          집 — "하루 마치기" 선택 → home 씬 있으면 강제 실행 (결정 B)
8. sleep         수면 — dream 씬 있으면 실행 → day+1
```

- **day 1**: 1부 슬롯 없음 → part1 스킵. bar_story에 튜토리얼 씬(크리스, craft 스텝 포함). Days.start_phase=bar(출근길 없음).
- **day 13**: 1부 슬롯 없음. bar_story에 리오스 씬(craft 1건 포함) → `ending` 특수 페이즈로 폭포 판정(§10).
- 조기 마감(푯말): part1을 강제 종료하는 입력일 뿐, 페이즈 흐름은 동일.
- ③문서의 "정산 후 해금 화면" 순서는 폐기 — 입고는 당일 아침(stock_in)이 정본.

---

## 2. 🗂️ 전체 테이블 맵

**저작 = 엑셀 2파일(30시트 + INFO×2) / 배포 = 도메인별 JSON 20파일**(구엔진식). 빌드(`tools/build.py`)가 두 시트 파일을 검증→JSON으로 변환한다. (v2.0 확정)

| 저작 파일 | 담당 축 | 시트 |
| --- | --- | --- |
| `LUNA_System.xlsx` (8시트) | 제조·운영 시스템 | Cocktails · RecipeLines · ShelfItems · Personalities · RandomWaves · Config · GradeCuts · GradePayout |
| `LUNA_Narrative.xlsx` (22시트) | 대사·서사·연출 | Scenes · Steps · Choices · Barks · Orders · Quests · QuestStages · Endings · Dossier · RegularSlots · **Tastes · AffinityMatrix**(호감도는 단골 전용) · Characters · Expressions · ExpressionParts · Cutscenes · FieldAnims · ResourceMap · Days · Spots · Points · UIStrings |

분리 축은 **수정 권한**이다 — xlsx는 바이너리라 git 머지가 안 되므로 파일 1개 = 소유자 1명. 랜덤 손님 **대사**(Barks)가 Narrative에 있는 이유: 대사는 시스템 담당이 아니라 글 쓰는 사람이 만진다. 규칙: **한 시트는 정확히 한 파일에만** — 두 파일에 같은 시트가 있으면 빌드 에러.

| 그룹(탭색) | 시트 | 내용 | → 배포 JSON | 주 편집자 |
| --- | --- | --- | --- | --- |
| 🔵마스터 | `Cocktails` + `RecipeLines` | 칵테일 1종 / 레시피 투입 1줄 | `cocktails.json`(레시피·파생 내장) | 시스템 기획 |
| 🔵마스터 | `ShelfItems` | **재료 + 잔·도구·가니시 통합** + 해금일 | `shelf_items.json` | 시스템 기획·그래픽 |
| 🔵마스터 | `Characters` | 스토리 인물(손님 8 + 루나 + NPC) | `characters.json` | 내러티브 |
| 🔵마스터 | `Expressions` + `ExpressionParts` | 표정(스프라이트↔파츠 애니) | `expressions.json`(중첩) | 그래픽·시스템 |
| 🔵마스터 | `FieldAnims` | SD(필드) 동작 클립 | `field_anims.json` | 그래픽 |
| 🔵마스터 | `Cutscenes` | 연출 리소스 매니페스트 | `cutscenes.json` | 연출 |
| ⚫검수 | `ResourceMap` | 데이터 키↔에셋 경로·검수 상태 | **배포 안 함**(런타임 미사용 — 에셋 검수 대장) | 그래픽·시스템 |
| 🔵마스터 | `Personalities` | 1부 성격유형 5종 | `personalities.json` | 시스템 기획 |
| 🔵마스터 | `Barks` | 1부 상황별 대사 풀 | `barks.json` | 내러티브 |
| 🔵마스터 | `Tastes` | 손님 취향(서빙 호감 판정) | `tastes.json` | 시스템·내러티브 |
| 🔵마스터 | `Dossier` | 단골 수첩(호감도 단계별 공개 정보) | `dossier.json` | 내러티브·시스템 |
| 🟠밸런스 | `Config` `GradeCuts` `GradePayout` `AffinityMatrix` | 튜닝 계수 4종 | **`balance.json`**(4종 묶음) | 시스템 기획 |
| ⚪기타 | `UIStrings` | UI 고정 문구 | `ui_strings.json` | 기획 공통 |
| 🟢스케줄 | `Days` | 일차 메타(BGM) | `days.json` | 기획 공통 |
| 🟢스케줄 | `RandomWaves` ⚙System | 1부 랜덤 손님 페이스(day별) | `random_waves.json` | 레벨 디자인 |
| 🟢스케줄 | `RegularSlots` 📖Narrative | 단골 방문 슬롯(카메오·분기) | `regular_slots.json` | 내러티브 |
| 🟢스케줄 | `Spots` | 위치 프리셋(좌표 대신 이름) | `spots.json` | 레벨 디자인 |
| 🟢스케줄 | `InteractPoints` | 출퇴근길 인터랙션 포인트 | `interact_points.json` | 레벨·내러티브 |
| 🟣대본 | `Scenes` + `Steps` + `Choices` | 씬·스텝·선택지 | **`script/day_N.json`**(하루 단위) | 내러티브 |
| 🟣대본 | `OrderRules` | 규칙형 주문 판정 | `order_rules.json` | 내러티브·시스템 |
| 🟣대본 | `Quests` + `QuestStages` | 사이드퀘·히든 레시피 | `quests.json`(스테이지 포함) | 내러티브 |
| 🟣대본 | `Endings` | 폭포 판정 순서+조건 | `endings.json` | 내러티브 |

**배포 JSON 20파일** (`데이터/json/`):

```
콘텐츠(계속 자람)  cocktails · shelf_items(재료+잔+도구+가니시) · characters · expressions
                  barks · personalities · tastes · dossier(단골 수첩) · cutscenes · field_anims
고정 소형          days · spots · endings · ui_strings
스케줄·서사        random_waves · regular_slots · interact_points · order_rules
묶음 2개           balance(=config+grade_cuts+grade_payout+affinity_matrix) · quests(=quests+stages)
※ ResourceMap은 에셋 검수 대장이라 배포하지 않는다(런타임 미사용)
대본(하루 단위)    script/common.json · script/day_1.json … day_N.json
```

> 📌 **분할 기준: "항상 같이 로드되고, 같이 튜닝되고, 혼자서는 의미가 없는 것"만 묶는다.** 그래서 튜닝 계수 4종(각 0.1~0.6KB)만 balance.json으로, 퀘스트 스테이지는 quests.json 안으로 묶고 나머지는 전부 낱개. 저작은 정규화(중복 없음)·배포는 로딩 편의형이며 재조립을 빌드가 담당. **전 파일이 한 빌드에서 함께 나오므로 파일 간 버전 어긋남이 원천 불가능**(구엔진 분할의 약점 제거). 상세 스키마는 §2-A.

## 2-A. 📤 배포 JSON 스키마 (프로그래밍 소비 계약)

**공통 규칙 (전 파일 적용)**
- 플레이어 노출 텍스트는 전부 `{"ko": "...", "en": "..."}` 객체 — 언어 설정대로 한 키만 읽는다.
- **`null` = "없음"이라는 유효한 값** (`""` 아님). 예: 병맥주 `glass: null`(병째로), 파츠 없는 표정 `parts: null`.
- **파생값은 이미 구워져 있음** (tier·unlock_day·scoring_items·time_limit_sec·talk_anim) — 런타임 재계산 금지. tier·unlock_day는 시트에서 수동 지정했을 수 있다(§9) — 어느 쪽이든 **JSON의 값이 정답**이다.
- 파일 간 참조는 전부 `id`. 빌드가 참조 무결성을 검증했으므로 런타임 방어 코드 불필요.
- `when`/`effects`는 공용 미니 언어 문자열(§8) — 엔진에 조건평가기·효과적용기 각 1개만.

**cocktails.json** (배열) — 레시피와 파생값이 항목에 내장:
```json
{ "id":"dry_martini", "name":{"ko":"드라이 마티니","en":"Dry Martini"}, "price":300, "abv":30.0,
  "glass":"cocktail",          // Items의 glass id. null=병째로
  "mix":"stir",                // none/build/stir/shake — 채점 정답(도구: shake=셰이커, stir=믹싱글라스)
  "prep":null,                 // 병 개봉 정답: null / "cap"(병뚜껑) / "cork"(코르크)
  "fill":null, "garnish":"olive", "color":"230,229,201", "tags":["씁쓸한","클래식","독한"],
  "flavor":{"ko":"…","en":"…"}, "unlock_when":null,
  "recipe":[ {"action":"pour","ingredient":"gin","qty":6,"unit":"oz"}, … ],  // 채점표(실행 순서 아님)
  "gimmick_count":3, "tier":2, "unlock_day":4, "scoring_items":5, "time_limit_sec":44 }  // ← 파생
```

**shelf_items.json** (배열, 40행) — 재료·잔·도구·가니시 통합(v1.9). `kind`로 필터해 각 선반 화면을 만든다:
```json
{ "id":"gin",    "kind":"ingredient", "name":{…}, "category":"base", "color":"230,235,240",
  "sprite":null, "unlock_day":1, "unlock_when":null, "shop_price":null, "desc":{…} }
{ "id":"shaker", "kind":"tool",       "name":{…}, "category":null,   "color":"",
  "sprite":"tool_shaker", "unlock_day":1, "unlock_when":null, "shop_price":null, "desc":{…} }
```
- `kind`: `ingredient`(25) / `glass`(7) / `tool`(3) / `garnish`(5) — **어느 선반 화면에 놓일지**
- `category`는 **재료일 때만** 유효(어떤 기믹인지 결정), 그 외 `null`
- `unlock_day:99` = 일차 미해금(퀘스트 전용, `unlock_when` 필수)
- 도구 3종: `shaker` / `mixing_glass` / `opener`(따개, cap·cork 겸용)
- 입고 화면에는 **소모품(`ingredient`·`garnish`)만** 표시한다 — 잔·도구는 상시 비치
**characters.json** (배열) `{id, name, name_color, role, affinity(호감 대상 bool), alive_flag, expressions[], base_body, enter_sfx, exit_sfx}`.
**expressions.json** (중첩 딕셔너리) `{캐릭터id: {표정명: {mode("sprite"/"parts_anim"), sprite, talk_anim(파생 bool), parts:{부위:{clip,loop}}}}}` — `common` 키 = 랜덤 손님 공용 세트(캐릭터에 없는 표정은 여기서 폴백).
**cutscenes.json** (배열) `{id, kind("timeline"/"gif"), resource_key, note}` — 대본의 timeline/gif 스텝이 id로 참조.
**field_anims.json** (배열) `{character_id, action, resource_key, status}` — 거리·집·꿈의 SD 스프라이트(바 흉상은 expressions).
**personalities.json** (배열, 5종) `{id, name, tip_mult, patience_mult}`.
**barks.json** (배열) `{voice_id(성격·캐릭터, null=공용 폴백), gender, situation, text, weight}` — 폴백 체인: 캐릭터→성격→공용. `{cocktail}` 치환자.
**tastes.json** (배열) `{character_id, seq, when, tier}` — **first-match**: 첫 일치 tier(love/good/ok/dislike), 없으면 ok.
**ui_strings.json** (딕셔너리) `{key: {ko, en}}`.

**balance.json** (유일한 계수 묶음):
```json
{ "config":{"gold_start":300, "coaster_base_sec":22, …},
  "grade_cuts":[{"grade":"excellent","min_pct":95,"tier_override":""}, …],
  "grade_payout":{"excellent":{"revenue_mult":1.0,"tip_mult":0.2}, "sewage":{"revenue_mult":-1.0,"tip_mult":0.0}, …},  // 음수=배상
  "affinity_matrix":[{"taste_tier":"love","excellent":6,"good":4,…}, …] }   // 취향×등급 → 호감 증감
```

**days.json** `{day, label, start_phase, bgm_street, bgm_bar, note}` · **random_waves.json** `{day, seq, tier, personality, delay_sec, max_rounds}` · **regular_slots.json** `{day, seq, character, tier, delay_sec, max_rounds, branch_choice, cameo_scene}` · **spots.json** `{id, area, desc}` · **interact_points.json** `{id, spot, kind, phase, when, scene_or_shop, selection}`.
**quests.json** `{quests:[{id, title, kind, reward_effects}], stages:[{quest_id, stage, goal, when, on_complete}]}` · **endings.json** (배열, priority 순 first-match) `{priority, id, when, scene_id}` · **order_rules.json** (배열, first-match) `{order_id, seq, when, verdict, effects, react}`.

**script/day_N.json** (대본, 로딩 단위=하루) — day 0 = `script/common.json`(상시 공용 씬):
```json
{ "day":1,
  "scenes":[ { "id":"d1_tutorial_chris", "day":1, "phase":"bar", "seq":1, "trigger":"auto",
               "when":"", "title":"…", "skippable":false, "group":"",
               "steps":[ {"seq":3, "type":"say", "actor":"chris", "arg":"default",   // say의 arg=표정
                          "text":{"ko":"루나.","en":"Luna."}, "when":null, "effects":null,
                          "sync":"wait"}, … ] }, … ],
  "choices":{ "ch_d2_aili_lady":[ {"seq":1, "text":{…}, "when":null, "effects":null, "goto":"d2_aili_lady_q"}, … ] } }
```
스텝 type: `say/enter/exit/order/craft/serve/choice/effect/fx/sfx/bgm/move/wait/timeline/gif/expr/anim/emote/end_part/end_day`. 텍스트 내 `<order>/<name>/<world>` = 리치텍스트 강조(TMP 스타일 매핑).

**엔진 로딩 요약**
```
게임 시작 1회:  마스터 11종 + balance + days/spots + quests/endings/order_rules
일차 진입:      script/day_N.json  (random_waves/regular_slots/points는 이미 메모리)
구현 파서 3개:  ① when 조건평가기 ② effects 효과적용기 ③ 씬-스텝 실행기 — 전 파일 공유
```
웹 프로토(`tools/bundle_proto.py`)가 이 파일들을 런타임 메모리 구조(`master`/`balance`/`schedule`/`scripts`…)로 조립 중 — Unity 로더의 참조 구현.

---

## 3. 🍸 Cocktails / RecipeLines

### Cocktails (1행 1종)

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `id` | str | snake_case. 예: `gin_tonic` |
| `name_ko` / `name_en` | str | 표시명 |
| `price` | int | 정가 |
| `abv` | float | 도수 % — **2부 규칙 주문·삼호 분기의 판정 축.** ②문서에 없던 신규 필수 컬럼 |
| `glass` | ref→Items | 정답 잔 |
| `mix` | enum | `none` `build` `stir` `shake` `bottle_open` |
| `fill` | ref→Ingredients, nullable | 필업 아이템 (토닉워터 등) |
| `garnish` | ref→Items, nullable | 가니시 (§3.6 설명 화면 발동 여부) |
| `color` | str | `"255,255,255"` 또는 `"grad:0,151,182>212,72,45"` |
| `tags` | str | `상큼한;클래식;청량한` — 2부 craving 주문 매칭용 |
| `flavor_ko` / `flavor_en` | text | 메뉴 설명 ko/en 쌍 (리치텍스트 마크업 금지 — 스타일은 엔진 담당) |
| `unlock_day_override` | int, nullable | **해금일 완전 수동 지정**(v1.9). 비우면 재료에서 파생. 재료 입고일보다 이르면 빌드 경고(대본 지정 제조용이면 정상) |
| `unlock_when` | DSL, nullable | 이벤트 조건부 해금 (예: `flag.quest_x_done`) |
| `tier_override` | int, nullable | **티어 완전 수동 지정**(v1.9). 비우면 기믹 수 공식. 기믹 개수 ≠ 체감 난이도인 경우를 사람이 보정 |
| *(파생)* `tier` | T1~T5 | `tier_override` 우선, 없으면 기믹 개수 공식 (§9) |
| *(파생)* `unlock_day` | int | `unlock_day_override` 우선, 없으면 max(재료 unlock_day) (§9) |

### 제조 실행 원칙 — 선반 4단계 선택 → 시작 → 강제 순서 실행 (v1.7 확정 / v1.9 화면 구조 확정)

> **기믹은 레시피가 실행하는 게 아니라 플레이어가 실행한다.** 단, "집는 즉시 발동"이 아니라 **선반에서 필요한 것을 먼저 다 '고르고', '제조 시작'을 누르면 정해진 논리 순서로 차례차례 실행**된다. 레시피(RecipeLines·mix·prep·garnish)는 **채점 정답표**일 뿐이다.

**화면 흐름 (v1.9) — 종류별 선반을 차례로 지난다**

```
레시피(메뉴) 선택
   → ① 잔 선반      (kind=glass)    · "병째로" 선택지 포함
   → ② 도구 선반    (kind=tool)     · 복수 선택. 믹스 도구(셰이커↔믹싱글라스)는 배타
   → ③ 가니시 선반  (kind=garnish)  · "없음" 선택지 포함
   → ④ 재료 선반    (kind=ingredient) · 담기 토글, 실행 순서 번호 미리보기
   → [제조 시작] → 기믹 실행 → 완성 화면 → 판정
```

- **각 화면에서 ◀이전으로 돌아가 다시 고를 수 있다.** 선택은 유지된다.
- **되돌아가도 제한 시간은 계속 흐른다** — 무한정 왔다갔다하며 고민하는 걸 막기 위해.
- **가니시 없는 칵테일도 ③ 화면을 띄운다.** 화면을 건너뛰면 "이 술은 가니시가 없구나"라는 정답 힌트가 되기 때문. 플레이어가 "없음"을 직접 골라야 맞다.
- **완성 화면은 정답이 아니라 플레이어가 고른 가니시를 보여준다.** 맞았는지는 판정에서 알게 된다.

**실행 순서(강제) — 담은 순서와 무관하게 항상 이 순서로 재정렬된다:**

```
① 병 개봉(따개)  →  ② 따르기(베이스 먼저)  →  ③ 스퀴즈  →  ④ 파우더  →  ⑤ 믹스(셰이크/스터)  →  ⑥ 필업(탄산·믹서)
```

> 이유: **아직 술을 따르지도 않았는데 젓는 상황이 물리적으로 불가능해야 한다.** 큐를 이 카테고리 순서로 정렬해 실행하므로, 플레이어가 셰이커를 먼저 담아도 믹스는 항상 모든 재료 투입 뒤(⑤)에 돈다. 병 개봉은 항상 따르기보다 앞(①).

- 담기 UI: 선반 셀을 누르면 큐에 담기고(다시 누르면 뺀다), 상단 트레이에 **실제 실행될 순서대로 번호가 매겨져 미리보기**된다. 믹스 도구(셰이커↔믹싱글라스)는 서로 배타(하나만).
- **코르크/뚜껑 병을 담았는데 따개를 안 담으면 '제조 시작'이 막힌다**(병을 못 여니까 — 토스트 안내). 담으면 개봉이 그 병 따르기 직전에 자동 삽입된다.

| 도구 (Items type=tool) | 발동 기믹 | 정답 기준 |
| --- | --- | --- |
| 셰이커 | 셰이킹(자유형 — 흔들다 멈춤) | mix=shake, 8회 근접 |
| 믹싱 글라스 & 바 스푼 | 스터(자유형 — 젓다 멈춤) | mix=stir 3바퀴 / build 1바퀴 근접 |
| **따개** (병따개+와인오프너 통합) | 병뚜껑(cap)=3연타 / **코르크(cork)=천천히 2바퀴 — 과속 시 부스러져 품질 하락** | prep 값과 일치 + cork는 품질(0.2~1.0) |

- 대본 지정 제조(주문·튜토리얼)는 **해금 무시** — 메뉴와 선반에 타겟 칵테일·그 레시피 재료가 잠겨 있어도 노출된다 (구 대본이 미해금 칵테일을 시키는 케이스 대응, 엔진 규칙).
- 채점(§9 채점표): 시간·잔·prep(코르크는 품질 배수)·믹스(올바른 도구 + 양 근접)·레시피 라인별 수량·필업·**가니시(v1.9 신설)** + **불필요 행동 감점**(레시피 밖 투입·불필요 도구/개봉, 건당 −50%).

### RecipeLines (1행 1투입 — '채점 정답표'. 실행 순서는 위 카테고리 순서로 자동 결정, seq는 채점/표시용)

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `cocktail_id` | ref | |
| `seq` | int | 투입 순서 |
| `action` | enum | `pour`(따르기) `squeeze`(스퀴즈) `powder`(파우더) |
| `ingredient_id` | ref | |
| `qty` / `unit` | float / enum | `oz` `ml` `tsp` |

> 📌 **이 테이블 하나가 두 가지 진실을 겸한다**: ① 난이도 티어 산출 근거(라인 수 + mix + bottle) ② ⑧등급 산출서의 채점 항목 목록(시간 + 잔 + mix + 각 라인 + fill). 레시피를 고치면 둘이 동시에 맞게 갱신된다. **기믹 실행 순서는 RecipeLines seq가 아니라 위 카테고리 강제 순서**(개봉→따르기→스퀴즈→파우더→믹스→필업)를 따른다.

**예시 — 진토닉:**

| cocktail_id | seq | action | ingredient | qty | unit |
| --- | --- | --- | --- | --- | --- |
| gin_tonic | 1 | pour | gin | 1.5 | oz |

Cocktails 행: `mix=build, fill=tonic_water, garnish=lime_wedge` → 기믹 수 = 따르기1 + 믹스1 = 2 → **T1** (자동). 채점 항목 = 시간·잔·믹스·진 수량·필 일치 = 5개 (자동).

---

## 4. 🧪 ShelfItems (재료 · 잔 · 도구 · 가니시 통합)

> **v1.9 통합.** 제조가 **잔 선반 → 도구 선반 → 가니시 선반 → 재료 선반** 4단계 선택으로 바뀌면서
> 넷 다 "선반에서 고르는 것"이 되었다. 성격이 같아졌으므로 한 테이블로 관리한다.
> `kind`가 **어느 선반 화면에 놓일지**를 결정한다.

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `id` | str | 고유 id. 다른 시트가 이 값으로 참조 |
| `kind` | enum | **`ingredient` / `glass` / `tool` / `garnish`** — 소속 선반 |
| `name_ko` / `name_en` | str | 표시명 (L10N 필수) |
| `category` | enum, nullable | **재료일 때만.** `base` `liqueur` `wine_beer` `juice` `dairy` `syrup`(따르기) / `fruit`(스퀴즈) / `powder`(파우더) / `mixer`(필업) — **어떤 기믹이 나올지를 결정** |
| `color` | str | 액체 렌더 RGB (스퀴즈·파우더·잔·도구는 공란 가능) |
| `sprite` | str, nullable | 잔·도구·가니시의 리소스 키 (재료는 공란) |
| `unlock_day` | int | **1 = day1부터 보유**, N = 해당 일차 stock_in부터, **99 = 일차로는 영원히 안 풀림**(퀘스트/이벤트 전용 — `unlock_when` 필수, 없으면 빌드 에러) |
| `unlock_when` | DSL, nullable | 조건부 입고. 충족되는 순간 해금되며 **다음 stock_in 화면에 신규 입고로 표시** |
| `shop_price` | int, nullable | 노점 구매가 (nullable = 비매품) |
| `desc_ko` / `desc_en` | text | 해금 화면·제조 화면 마우스오버 설명 |

**kind별로 실제 쓰이는 컬럼**

| kind | 개수 | category | sprite | shop_price | 비고 |
| --- | --- | --- | --- | --- | --- |
| `ingredient` | 25 | ✓ | — | ✓ | 레시피가 `qty`와 함께 참조하는 소모품 |
| `glass` | 7 | — | ✓ | — | 제조 ① 단계. 틀리면 채점 0점 항목 |
| `tool` | 3 | — | ✓ | — | 셰이커 / 믹싱글라스&바스푼 / 따개. 담으면 그 기믹이 실행 순서에 편입 |
| `garnish` | 5 | — | ✓ | ✓ | 제조 ③ 단계. **v1.9부터 플레이어 선택·채점 대상** |

> 💡 **해금·상점가가 전 kind 공통인 이유**: 가니시(올리브·체리)는 실제 바에서 소모품이므로 **"4일차에 올리브 입고"** 같은 설계가 가능해야 한다. 잔·도구도 스토리상 나중에 들어오는 연출을 못 할 이유가 없다. 다만 **입고 화면(stock_in)에는 소모품(`ingredient`·`garnish`)만 표시**한다 — 잔·도구는 상시 비치 개념.

> ⚠️ **통합의 대가 — kind 오배치 검사.** 한 테이블이 되면서 `Cocktails.glass` 칸에 재료 id를 넣는 실수가 문법상 가능해졌다. 빌드가 **참조 대상의 kind까지 대조**해 잡는다(§12).

> ⚠️ **서브재료(라임·레몬·설탕·소다수·토닉워터 등)의 해금일**은 ③문서가 정의하지 않았다. 규칙: **그 재료를 쓰는 가장 이른 칵테일의 해금일과 같게** 역산해 채운다. 검증기가 칵테일 해금표를 재생성해 대조하므로 실수는 빌드에서 잡힌다.

> 📌 **상점(shop_price)의 현재 위치**: 재료 입고는 **바 오픈 시 자동 입고가 기본**이고, 노점 구매는 **구조만 유지**한 보조 경로다(PD 확정 26.07.19). 히든 레시피 재료를 찾아다니는 용도 등으로 되살릴 수 있게 데이터는 남겨둔다.

---

## 5. 👥 Characters / Personalities / Barks

### Characters (스토리 인물 — 1부 일반 손님은 여기 없음)

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `id` | str | `luna` `chris` `aili` `port` `tom` `samho` `bubi` `haru` `sunha` `hina` `shiba` `rios` `volts` + 외부 NPC |
| `name_ko` / `name_color` | str | 대사창 표기 |
| `role` | enum | `player` `master` `guest_multi` `guest_twice` `guest_once` `npc_street` |
| `affinity` | bool | 호감도 트래킹 대상 여부 (다회성 8인 = true) |
| `alive_flag` | str, nullable | 생사 변수 보유자만 (`samho`, `haru`) |
| `expressions` | str | `default;joy;anger;...` 표정 리스트 |
| `enter_sfx` / `exit_sfx` | str | |

### Expressions / ExpressionParts — 표정 = 상태 (v1.2, 구엔진 character_anim.json 이식)

표정 하나는 **두 모드 중 하나**다. 리소스 사정에 따라 모드만 바꾸면 되고, 대본은 표정 이름만 안다:

| 모드 | 내용 | 현재 적용 |
| --- | --- | --- |
| `sprite` | 단일 스프라이트 1장 | 랜덤 손님 공용 세트(common 8종), 신규 스토리 캐릭터 전원, 기존 6인의 success/fail/drunk류 |
| `parts_anim` | 파츠 7종(eyes/eyebrows/upper_face/lower_face/body/extra/etc) 클립 조합 — 상세는 ExpressionParts에 1행 1파츠 | 기존 애니 리소스 보유 6인(루나·크리스·포트·부비·아일리·삼호)의 default 계열 |

| ExpressionParts 컬럼 | 설명 |
| --- | --- |
| `character_id`, `expression`, `part` | 소속 (행 없는 파츠 = 비활성/기본 유지) |
| `clip` | 애니메이션 클립 키 |
| `loop` | `always`(상시 루프) / **`always_on_dialogue`(대화 외 idle, 본인 대사 출력 중 Dialogue 클립 전환 = 입 움직임, 끝나면 복귀)** / `once`(1회 재생 후 마지막 프레임 정지) / `none` / `special_on_dialogue`(구엔진 특수 모드 계승) |

- **입 움직임 규칙**: `lower_face`의 loop가 `always_on_dialogue`면, 그 캐릭터 **본인의 대사가 출력되는 동안만** 입 애니메이션 재생. 배포 JSON에 `talk_anim`으로 파생되어 구워진다.
- 대본 연계: `say`의 arg = 표정(전환+대사), **`expr` 스텝** = 대사 없이 표정만 전환(구 대사 노드의 표정 변경 값 대응).
- 폴백: 캐릭터에 해당 표정이 없으면 `common` 세트 → 그것도 없으면 default. 검증기가 대본의 표정 참조를 전수 검사.
- 확장 경로: 스프라이트 1장으로 시작한 표정에 애니가 생기면 **mode만 parts_anim으로 바꾸고 파츠 행 추가** — 대본 무수정.

### Cutscenes — 연출 리소스 매니페스트 (v1.2, v1.3에서 실등록)

`id, kind(timeline/gif), resource_key, note`. **timeline**(Unity Timeline 에셋, 주력)과 **gif**(스파인→GIF 삽입)의 등록부. Steps의 `timeline`/`gif` 스텝은 반드시 이 표의 id를 참조(검증기 대조). v1.3에서 **실존 playable 17종을 실등록**(INTRO/Intro lab/FindLuna/Bubi_meet/Catmilk/삼호 3종/Finished·Serve 계열 9종) + 예약 4종(tl_raid_1·2, tl_d13_rios, gif_bubi_headjump). Finished/Serve 계열은 절차적 합성 방침에 따라 4앵커만 유지, Serve_Unknown은 2부 자유조합 실패 연출 전용 후보로 보관. 트리거·스텝 확장의 설계 근거는 [컷씬 검토서](LUNA_시스템흐름_컷씬확장_검토서_v1.0.md) §3 참고.

### ResourceMap — 데이터 키 ↔ 실제 에셋 경로 (v1.3, 26.07.18 에셋 검수 결과)

데이터의 id/clip 키와 엔진 리소스 폴더명이 **다른 지점의 공식 매핑표**. 엔진의 리소스 로더는 이 표를 거쳐 Addressable 경로를 해석한다.

| 핵심 매핑 | 내용 |
| --- | --- |
| `aili` → `Aniamtations/Ily`, `samho` → `Aniamtations/3`, `common` → `Aniamtations/Default` | 폴더명 불일치의 공식 해석 |
| 표정 상태 폴더 | `default`(idle) / `talk`(입 애니 = always_on_dialogue의 Dialogue 클립) / `Intro`+`Loop`(anger의 special 구조) |
| status 컬럼 | `OK` / `일부`(common은 annoyingB·R만 존재) / `확인필요`(**luna 표정 폴더 미발견**) / `결정필요`(**바 내부 BG 1280 ↔ 포커스 카메라 — 줌인 팬 vs 좌우 연장 택1**) / `폐기`(MethodButton 9장 — 기존 결정) / `보관`(ICE 버튼) |

> 에셋 검수 총평(26.07.18): 스프라이트 900장·표정 프레임 216·컷씬 프레임 649·playable 17 전수 대조 결과, **신 시스템 전환으로 못 쓰게 되는 리소스는 사실상 없음.** 외부 거리(2880px 패럴랙스·출입구·엘리베이터·인터랙트 오브젝트)는 Spots 프리셋과 그대로 대응하고, 제조 미니게임 UI 풀세트도 신 기믹에서 재사용된다.

### FieldAnims — 비주얼 이원화: 흉상(바) ↔ SD(필드) (v1.3)

캐릭터 비주얼은 **도메인 2벌**이고, 어느 벌을 쓰는지는 **씬의 phase가 자동 결정**한다 — 대본은 도메인을 모른다.

| 도메인 | 사용 phase | 스타일 | 리소스 시스템 |
| --- | --- | --- | --- |
| **bar** | `bar` | 고해상도 흉상/스탠딩 (커피톡) | Expressions / ExpressionParts (표정·입 애니) |
| **field** | `commute_in` `commute_out` `home` `dream` `street` | SD 픽셀 (산나비/우산금지) | **FieldAnims** — `character_id, action(idle/walk/run/dead…), resource_key, status` |

- **양쪽에 등장하는 캐릭터**(삼호·시바·부비 등): 두 시스템에 각각 행이 있으면 끝. 대본의 `enter/move/anim` 스텝은 같은 문법 그대로 — 엔진이 phase를 보고 흉상/SD를 갈아끼운다. **추후 추가 = 행 추가일 뿐, 대본·코드 무수정.**
- **한쪽 전용도 정상**: 랜덤 손님 = bar 전용(common 흉상 세트, 밖에 안 나감) / 거리 NPC(완 등) = field 전용(Expressions 불필요).
- 현 에셋 현황: 루나(idle·walk·run·walk_w_cat)·삼호(run·DEAD 포함 5종)·부비·시바 = **OK**, 꿈 컷씬의 유나·경비병·하운드 = 습격 프레임에 포함 **OK**. **신규 제작 필요 4건**: 크리스(테라스 — 초상 대화로 처리하면 불필요, 연출 결정 대기)·완·송하루(노말엔딩1)·리오스(배드엔딩).
- 검증기: field 도메인 씬에서 `enter/move/anim`을 쓰는 캐릭터가 FieldAnims에 없으면 리포트에 ⚠ 경고(제작 목록 자동 추출).
- SD 씬에서의 대화 초상(흉상 축소판 vs 얼굴 아이콘)은 연출 미확정 — §10 열린 이슈.

### Dossier — 단골 수첩 (v1.9.5, 호감도 체감 장치)

| 컬럼 | 설명 |
| --- | --- |
| `character_id` | 대상 인물 (affinity=true 인물만 수첩에 실림) |
| `min_affinity` | 이 호감도 이상일 때 공개. **0=만나면 즉시**. 잠긴 항목은 "🔒 호감도 N" 티저 |
| `kind` | `desc`(한 줄 소개) / `taste`(취향 소개문 — 판정은 Tastes가 함) / `history`(과거사) / `secret`(숨겨진 이야기) / `recent`(근황 — when으로 진행 연동) |
| `when` | 조건부 공개(주로 recent). 예: 삼호 생사 루트별 근황 |
| `text_ko/en` | 표시 문구 |

> 해금 규칙: **미만남 = ??? 실루엣 → 만남 = 이름만 → min_affinity 단계마다 한 줄씩.** 만남(`S.met`)은 2부 등장(enter)·1부 카메오 서빙 시 기록. 시드 단계: desc 10 / taste 20 / history 35 / secret 50 — 시트에서 자유 조정(밸런싱 포인트).

### Personalities (1부 성격유형, ~5종 TBD)

`id`, `name_ko`, `tip_mult`, `patience_mult` — 배율 컬럼은 예약(①문서 열린이슈). 확정 전까지 1.0.

### Barks (1부 상황별 대사 풀 — 1행 1대사)

| 컬럼 | 설명 |
| --- | --- |
| `voice_id` | ref (공란 = 전 유형 공용 폴백). **성격유형 id 또는 캐릭터 id** — 캐릭터 id를 쓰면 1부 카메오 손님 전용 대사(결정 J) |
| `gender` | 공란=공용, `m`/`f` = 성별 전용. 스폰 시 랜덤 성별에 맞춰 필터 (PD 대사 시트의 남/여 구분 대응 — 현재는 전부 공용) |
| `situation` | enum (PD 대사 시트 분류 채택): `call`(첫 호출) `call_urge`(재촉·인내심 50%) `call_final`(최종 재촉·80%) `order` `reorder`(2차 주문) `serve_urge` `serve_final`(서빙 대기 경고) `react_excellent`~`react_sewage`(5등급) `wrong_receive` `wrong_drink` `leave_coaster` `leave_serve` `drunk_enter`(3잔째) `bye_good` `bye_bad` |
| `text_ko` / `text_en` | 대사 쌍 (`{cocktail}` 치환자 지원) |
| `weight` | 랜덤 가중치, 기본 1 |

> 선택 규칙: `voice_id + situation`(+gender) 일치 대사 중 가중치 랜덤 → 없으면 공용(voice 공란) 폴백. 성격유형 5종은 PD 시트 기준 **온화형(gentle)·거친형(rough)·예민형(touchy)·과묵형(quiet)·수다형(chatty)** 확정 — "짜증난&예민한"은 예민형으로 통합.

---

## 6. 🪑 RandomWaves / RegularSlots / Days / Points

### RandomWaves (⚙System — 구 GuestSlots에서 v2.0 분리)

1부 **이름 없는 랜덤 손님**의 페이스 테이블. 운영 난이도 튜닝은 전부 여기서.

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `day` | int | 1~13 |
| `seq` | int | 스폰 순서 — **RegularSlots와 하루 공용 번호** (겹치면 빌드 에러) |
| `tier` | 1~5 | 그 시점까지 해금된 해당 티어 풀에서 스폰 시 추첨 |
| `personality` | ref, 공란=랜덤 | 성격(팁·인내 배율은 Personalities) |
| `delay_sec` | float | 앞 손님과의 스폰 간격 — **1부 실질 난이도 레버** |
| `max_rounds` | 1~3 | 다회 주문 |

### RegularSlots (📖Narrative — 단골 방문 슬롯)

**단골이 언제 오는지**의 서사 테이블. 카메오·분기 선택지가 여기 붙는다.

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `day` / `seq` | int | seq는 RandomWaves와 하루 공용 번호 |
| `character` | ref→Characters, **필수** | 누가 오는지. 초상·전용 Barks(voice_id=캐릭터) 사용. 랜덤 손님은 RandomWaves에 |
| `tier` | 1~5 | 1부 카메오로 왔을 때 주문할 칵테일 티어 |
| `delay_sec` / `max_rounds` | | RandomWaves와 동일 |
| `branch_choice` | bool | 3잔째 제공/거절 선택지 발동 (max_rounds=3일 때만 유효) |
| `cameo_scene` | ref→Scenes, nullable | 서빙 직후 재생되는 짧은 씬(2~3줄) |

> 검증기: 양 시트 공통 — tier가 그 day에 빈 풀이면(해금 전 티어 지정) **에러**. (day,seq)가 두 시트에 겹치면 **에러**. RegularSlots의 character 공란은 **에러**. 런타임은 두 테이블을 합쳐 seq로 정렬해 스폰 순서를 복원한다.

### Days

`day`(0~12), `label`, `bgm_day`/`bgm_night`, `note`. 페이즈 온오프는 데이터에서 파생되므로 저장하지 않는다 (part1 = 슬롯 존재 여부).

### Spots (위치 프리셋 — 좌표 대신 이름 붙은 지점)

외부 맵은 항상 같은 1벌이므로 **픽셀 좌표를 데이터에 쓰지 않는다.** 대신 의미 있는 이름의 지점 프리셋을 정의하고, Unity 씬에 같은 이름의 앵커 오브젝트(SpotAnchor)를 1회 배치한다. 배경 아트가 바뀌면 앵커만 옮기면 되고 데이터는 불변. 이름은 그리드(`a_0`)가 아니라 **의미 기반**(`street_stall`)으로 — 시트에서 읽자마자 어디인지 알 수 있게.

| 컬럼 | 설명 |
| --- | --- |
| `id` | `bar_door` `street_board` `street_stall` `street_mid` `home_door` `alley_in` `alley_deep` `elevator` (초기 8종) |
| `area` | `street` / `alley` — 논리 구역 |
| `desc_ko` | 기획용 설명 |

`move` 스텝의 목적지도 `spot:street_mid` 형식으로 동일 프리셋을 쓴다.

### Points (출퇴근길 인터랙션 포인트 — 외부 NPC·오브젝트 대화의 진입점)

> **구엔진 NPC 대화(shiba.json류)의 신구조 대응**: 구엔진의 flow 1개 = 씬 1개(`trigger=interact`, `group=NPC명`). flow의 condition(AND/NOT) = 씬 `when`(`&&`·`!` 지원). triggers.set_flag = 스텝 `effects`. **1회성 대사 = 씬 when에 `!flag.본플래그` + 마지막 스텝 effects로 플래그 셋**, **반복 폴백 대사 = when 공란 씬을 그룹 맨 뒤 seq에**. 퀘스트 수주 선택지는 choice 스텝(수락 → `flag.q_x_started = true`) — 벌꿀·상자 퀘스트가 실동작 예시.

| 컬럼 | 설명 |
| --- | --- |
| `id`, `spot`(ref→Spots) | 배치 — 좌표 대신 위치 프리셋 참조 |
| `kind` | `npc` `object` `shop` |
| `phase` | `commute_in` / `commute_out` / `both` |
| `when` | 노출 조건 (day 범위·플래그·퀘스트 단계) |
| `scene_or_shop` | 실행 대상: 씬 id / `group:<그룹id>` / `shop:<상점id>` |
| `selection` | 재조사 규칙 (v1.2, 구엔진 outside_objects 이식): `once`(1회 소모) / `repeat`(매번 동일) / `sequential`(볼 때마다 group의 다음 씬, 마지막에서 멈춤) / `conditional`(매번 when 통과하는 첫 씬) |

> 상시 오브젝트(전단·자판기 등)는 **day 0 공용 씬**(`script/common.json`)에 group으로 묶고, Points의 when으로 노출 시기를 제어한다. 구엔진 `outside_objects.json` 4종(알바 전단·자판기·임상시험 전단·쓰레기통)을 이 방식으로 이식·검증 완료. 거리 NPC(구 shiba.json 등의 day·route·조건 대화)도 동일 구조(Points + when + group)로 흡수된다.

> 📌 출근길/퇴근길은 **같은 거리 리소스를 방향만 바꿔 재사용**한다(대표님 씬 제한 피드백 반영). Points의 when으로 아침/저녁 노출을 구분한다: `phase == commute_in` 등.

---

## 7. 🎬 Scenes / Steps / Choices / Orders — 통합 서사 문법

### Scenes

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `id` | str | `d3_samho_serve` 식 |
| `day` | int | 0~12 |
| `phase` | enum | `intro`(검은 화면 오프닝) · `home`(집) · `commute_in`(출근길) · `bar_open`(개점 전 대화) · `bar`(바 2부) · `commute_out`(퇴근길) · `dream`(꿈) · `street`(거리 공용) — **이 값이 비주얼을 자동 결정**(bar 계열=고해상도 흉상 / 나머지=SD 픽셀) |
| `seq` | int | 같은 day+phase 내 실행 순서 |
| `trigger` | enum | `auto`(페이즈 진입 시 자동) / `interact`(Points에서 호출) |
| `when` | DSL | 통과 못 하면 이 씬은 그날 존재하지 않음 |
| `title` | str | 작업용 메모 (엔진 미사용) |

### Steps (1행 1스텝)

| 컬럼 | 설명 |
| --- | --- |
| `scene_id`, `seq` | 소속·순서 |
| `type` | 아래 표 |
| `actor` | 화자/대상 캐릭터 |
| `arg` | 타입별 주 인자 |
| `text_ko` | 대사/지문 |
| `when` | 스텝 단위 분기 (통과 못 하면 건너뜀) |
| `effects` | DSL — 이 스텝 실행 시 적용 |
| `note` | 연출 메모 |

| type | actor | arg | 용도 |
| --- | --- | --- | --- |
| `say` | 화자 | 표정 | 대사 (`text_ko`) |
| `enter` / `exit` | 캐릭터 | 좌석(L/M/R) | 입퇴장 |
| `order` | 손님 | `want:칵테일id` 또는 `rules:주문id` + `mode:exact/craving/vague` | 주문 발생 |
| `craft` | - | `target:칵테일id` / `free`(자유조합) / `tutorial:칵테일id` | 제조 화면 진입 |
| `serve` | 손님 | - | 서빙 대기 → 드래그 |
| `choice` | - | choice_id | 선택지 (Choices 참조) |
| `effect` | - | - | effects만 실행 (플래그·호감·골드) |
| `fx` / `sfx` / `bgm` | - | 리소스 키 | 연출 |
| `camera` | - | `focus:L` `pair:L,M` `free` | 카메라 |
| `move` | 캐릭터 | 목표 x | 횡스크롤 이동 연출 |
| `wait` | - | 초 | 대기 |
| `end_day` / `end_part` | - | - | 흐름 제어 |

### Choices

| 컬럼 | 설명 |
| --- | --- |
| `choice_id`, `seq` | 선택지 그룹·옵션 순서 |
| `text_ko` | 옵션 문구 |
| `when` | 노출 조건 (숨김 옵션) |
| `effects` | 선택 시 효과 |
| `goto` | 점프할 scene_id (공란 = 다음 스텝 계속) |

### Orders (규칙형 주문 — first-match)

| 컬럼 | 설명 |
| --- | --- |
| `order_id`, `seq` | 그룹·평가 순서 |
| `when` | `cocktail.*` 어휘로 서빙된 잔 판정 |
| `verdict` | `fulfill` `care` `partial` `miss` |
| `effects` | 플래그·호감 |
| `react` | 반응 대사 오버라이드 키 (공란 = 프리셋) |

**예시 — day 3 삼호 딜레마 (Orders, order_id = `d3_samho`):**

| seq | when | verdict | effects | react |
| --- | --- | --- | --- | --- |
| 1 | `cocktail.abv >= 20` | fulfill | `flag.samho_drunk = true` | d3_samho_heavy |
| 2 | `cocktail.abv <= 5 && grade >= good` | care | `flag.samho_calmed = true; affinity.samho += 2` | d3_samho_calm |
| 3 | `cocktail.abv <= 5` | partial | | |
| 4 | *(default)* | miss | | |

**예시 — day 8 부비 결석 (Scenes):**

| id | day | phase | seq | when |
| --- | --- | --- | --- | --- |
| d8_bubi_haru | 8 | bar | 3 | `alive.haru` |
| d8_no_bubi | 8 | bar | 3 | `!alive.haru` |

**예시 — 집 강제 대화 (결정 B는 데이터가 아니라 엔진 규칙):** "하루 마치기" 선택 → `phase == home` 씬 중 when 통과분이 있으면 전부 실행 후 수면. home 씬이 없는 날은 바로 수면. 별도 플래그 불필요.

### Tastes — 손님 취향과 서빙 호감 판정 (v1.6)

> "단골에게는 고정 취향이 있지만, 같은 손님이라도 그날 상황에 따라 반기는 잔이 다르다" — 이 요구를 시트 하나로 처리한다.

**Tastes 시트** (1행 1규칙, 캐릭터별 first-match):

| 컬럼 | 설명 |
| --- | --- |
| `character_id` | 대상 캐릭터 (호감 대상이 아니어도 행은 가능하나 호감 반영은 affinity=true만) |
| `seq` | 평가 순서 — **위에서부터 첫 일치 행의 tier 적용**, 매치 없으면 기본 `ok` |
| `when` | 판정 조건 — `cocktail.id / cocktail.tag() / cocktail.abv` + **flag·day·affinity 등 전체 when DSL**. 칵테일 외 어휘를 쓰면 그 행이 "상황부 취향"이 된다 |
| `tier` | `love / good / ok / dislike` — AffinityMatrix의 행과 연결 (`miss`는 오제조 전용이라 금지, 빌드 에러) |

**서빙 호감 반영**: 정상 제공 시 `AffinityMatrix[tier][grade]` 만큼 호감 증감. 2부 스토리 서빙과 1부 **카메오** 서빙에 공통 적용(일반 랜덤 손님은 호감 대상이 아니므로 미적용).

**취향의 3층 구조** — 우선순위 사다리:

| 층 | 무엇 | 어디 | 예 (실데이터) |
| --- | --- | --- | --- |
| ① 이 장면의 정답 | `order exact:` / Orders 규칙(verdict) | Steps·Orders | day4 삼호 딜레마 — 이 순간엔 "저도수+good"이 정답, 취향과 무관 |
| ② 상황부 취향 | Tastes 행 중 when에 flag 등이 붙은 것 | Tastes | `flag.samho_calmed && abv>=20 → dislike` — 배려 루트 이후 독주를 밀어냄 |
| ③ 기본 취향 | Tastes 행 중 칵테일 조건만 있는 것 | Tastes | 삼호 `abv>=20 → love`, 포트 `gin_fizz → love`, 부비 `kahlua_milk → love` |

같은 캐릭터의 "그때그때 다른" 선호는 ②를 ③보다 위 seq에 두는 것으로 표현한다 — 상황 플래그가 서 있으면 상황부가 먼저 걸리고, 아니면 기본 취향으로 떨어진다. **별도 시스템이 아니라 행 순서다.**

⚠ 호감도 상한 시뮬레이션(§12 규칙 19)은 상황부 행(비칵테일 조건 포함)을 비활성으로 보고 기본 취향만 평가한다 — 상한치가 상황 플래그에 오염되지 않게.

### 대본 저작 규칙 — 루나 대사 배치 (결정 ⑤, 빌드 검증 대상)

루나(플레이어 캐릭터)도 대화에 참여하지만, **플레이어가 행동해야 하는 시점에 루나 대사가 뜨면 흐름이 끊긴다.** 아래 4개 규칙을 지킨다:

1. **행동 스텝(`craft`/`serve`/`choice`) 직전 스텝에 루나 say 금지** — 빌드 에러로 강제.
2. **행동 유도는 항상 상대 캐릭터의 대사로 끝낸다.** ("샴페인 한 잔!" → [제조]. "…이 집은 지낼 만하고?" → [선택지])
3. **루나의 리액션·독백은 행동의 결과 뒤에 배치한다.** (서빙 완료 후, 판정 후, 선택 후)
4. 루나의 괄호 독백으로 씬을 끝내는 것은 자유이동 구간(출퇴근길)에서만 허용 — 독백 후 이동 재개는 행동 유도가 아니므로.

### 텍스트 저작 규칙 — L10N (결정 I · v1.9.2 단계 완화)

> **현행(번역 착수 전)**: ko만 필수. en 공란은 빌드가 건수를 집계해 리포트에 표시하고, 배포 JSON에는 ko가 폴백으로 들어간다(빈 텍스트 방지). **8월 중후반 번역 완료 후 `build.py --strict`를 상시 사용** — 그때부터 en 누락은 다시 빌드 에러다.

- 플레이어에게 노출되는 모든 텍스트 컬럼은 **`_ko`/`_en` 쌍**으로 저작한다 (대사, 선택지, 칵테일/재료/아이템 이름·설명, 대사 풀, 퀘스트 제목).
- UI 고정 문구는 **UIStrings 시트**(key/ko/en)에 모은다.
- ko가 있는데 en이 비면 **빌드 에러** — 번역 누락이 런타임까지 새지 않게 한다.
- 배포 JSON에서 로컬라이즈 필드는 `{"ko": "...", "en": "..."}` 객체로 구워진다 — 엔진은 언어 설정에 따라 한 필드만 읽으면 된다. 제3언어 추가 = 컬럼 1개 + 객체 키 1개.

---

## 8. 🔤 when / effects DSL 사양

### when — 조건식 (참/거짓)

```
어휘(읽기): day, money, reputation, grade,
          flag.<id>, affinity.<캐릭터>, alive.<캐릭터>,
          cocktail.id / cocktail.abv / cocktail.tag(<태그>),
          quest.<id>.stage, phase
연산:      == != >= <= > <   /  ! (부정)  /  && (AND)
OR 없음 — 행을 나눠라 (first-match·다중 씬으로 표현)
예:  !alive.samho && affinity.aili >= 50
     cocktail.tag(달콤한) && cocktail.abv < 10
```

### effects — 효과 나열 (세미콜론 구분, 순차 적용)

```
affinity.samho += 2        호감 증감
flag.samho_saved = true    플래그
money += 300               골드
reputation -= 1            평판
give(lime, 3)              재료 지급 (인벤토리 직접 추가 — '해금'과 다름)
unlock_recipe(bees_knees)  히든 레시피 해금 (즉시 메뉴 등록)
quest(samho_honey).advance 퀘스트 단계 +1 (수동 진행 — §10.4)
```

`quest(x).advance`의 정확한 의미론: 스테이지 +1 → 그 스테이지의 `on_complete` 발동 → 마지막 스테이지면 `reward_effects` 1회 발동. goal 훅(자동 진행)과 최종 동작이 완전히 같다 — 엔진에 진행 함수는 하나만 있고 goal 훅도 이 함수를 부른다.

파서는 엔진에 **1개**(ConditionEvaluator/EffectApplier)만 구현하고 대본·주문·해금·엔딩·포인트 노출이 전부 공유한다.

---

## 9. ⚙️ 파생 규칙 (빌드 타임 계산 — 손으로 쓰지 않는 값)

| 파생값 | 공식 | 출처 |
| --- | --- | --- |
| `tier` | **`tier_override`가 있으면 그 값**(v1.9). 없으면 기믹 수 = RecipeLines 행수 + (mix≠none?1:0) + (prep?1:0) → 1~2:T1 / 3:T2 / 4:T3 / 5:T4 / 6+:T5 | ②문서 §3 |
| `unlock_day`(칵테일) | **`unlock_day_override`가 있으면 그 값**(v1.9, 앞당김도 가능·경고). 없으면 max(재료들의 unlock_day) | ③문서 §1 |
| 채점 항목 | 시간 + 잔 + (mix≠none?믹스:0) + (prep?1:0) + 각 RecipeLine + (fill?1:0) + **가니시 1**(v1.9) — 항목 단순 평균 | ⑧문서 §2·3 |
| `time_limit` | `config.time_base + 기믹수 × config.time_per_gimmick` (계수는 Config에서 튜닝) | ⑧문서 TBD 해소 |
| 인내심 임계 | `coaster = max(12, 22 − tier×1.5)` / `serve = max(TL+10, TL+40 − tier×3)` — 계수 전부 Config | ①문서 §3.7 |

| 정산(등급별) | `매출 = 가격 × grade_payout[등급].revenue_mult` / `팁 = 가격 × tip_mult × 성격 tip_mult` (v1.9) | PD 확정 26.07.19 |

> 계산값을 JSON에 **구워서** 배포한다(런타임 계산 아님).
> **v1.9부터 `tier`·`unlock_day`는 "파생 or 수동"이다** — override 칸이 비어 있으면 계산, 채워져 있으면 그 값이 그대로 정답이다. 기믹 개수 ≠ 체감 난이도인 경우, 재료보다 먼저/나중에 열어야 하는 경우를 사람이 직접 잡을 수 있게 한 것.

---

## 10. 🏁 Quests / Endings

### 10.1 퀘스트 시스템 — 개념 모델 (v1.4)

퀘스트는 **별도의 대본 문법이 아니다.** 이미 있는 부품(씬-스텝, Choices, when/effects DSL, 해금 시스템)을 묶는 **진행 상태 장부**다. 수명주기는 4단계:

```
수주(플래그)  →  목표 수행(goal)  →  스테이지 진행(stage +1)  →  보상(reward_effects 자동 1회)
  Choices/Steps    QuestStages         엔진 훅 or 수동 advance      Quests 시트
```

- 퀘스트의 **진행도**는 `stage` 정수 하나다. **0 = 미시작/미진행**, N = N번째 스테이지까지 완료. 저장 데이터에는 `quests: {퀘스트id: stage}` 맵과 보상 지급 여부(`questRewarded`)만 들어간다.
- **"수주"라는 엔진 개념은 없다.** 수주는 그냥 관례다 — 선택지/스텝의 effects로 `flag.q_<퀘스트id>_started = true`를 세우고, 스테이지의 `when`이 그 플래그를 요구하게 한다. 수주 전에 목표 행동을 해도 when이 막아서 진행되지 않는다.
- 퀘스트 UI(저널)는 선택 사항이다. 데이터는 UI 없이도 성립하며, 저널을 붙일 때 Quests 시트의 title/note가 그대로 표시 원천이 된다.

### 10.2 Quests 시트 (1행 1퀘스트)

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `id` | str | when DSL에서 `quest.<id>.stage`로 참조되는 값. **변경 금지** |
| `title_ko` / `title_en` | str | 완료 토스트·저널 표기 (L10N 쌍 필수 — 누락 시 빌드 에러) |
| `kind` | enum | `main`(메인 스토리) / `side`(사이드). 그 외 값은 빌드 에러 |
| `reward_effects` | DSL | **마지막 스테이지 달성 '순간' 자동으로 1회만** 발동. 재발동 없음 (엔진이 지급 여부를 별도 기록) |
| `note` | str | 기획 메모 (배포 JSON에도 실리지만 게임에는 미표시) |

### 10.3 QuestStages 시트 (1행 1스테이지)

| 컬럼 | 타입 | 설명 |
| --- | --- | --- |
| `quest_id` | str | 소속 퀘스트 |
| `stage` | int | **1..N 연속 필수** (건너뛰면 빌드 에러). 판정 대상은 항상 '현재 스테이지'(=완료 스테이지+1) **하나뿐** |
| `goal` | str | 목표 문법(아래 표). 문법 밖 문자열은 빌드 에러 |
| `when` | DSL | **목표 활성 조건.** 보통 수주 플래그. 미통과 상태에서는 목표 행동을 해도 진행 안 됨 |
| `on_complete` | DSL | 이 스테이지를 **통과하는 시점**의 효과. 보상(reward_effects)과 별개로, 스테이지마다 발동 — 중간 스테이지의 플래그·연출 트리거용 |

**goal 문법** (엔진 훅이 자동 판정):

| 문법 | 달성 시점 | 훅 위치 |
| --- | --- | --- |
| `interact:<포인트id>` | 해당 Points 오브젝트와 상호작용했을 때 | 출퇴근길 상호작용 처리 |
| `serve:<칵테일id>` | 그 칵테일을 **정상 제공**했을 때 — 정확한 주문 일치 + Sewage 등급 아님. 1부 서빙과 2부 스토리 서빙 **공통** | 1부 serve / 2부 storyServe 직후 |
| *(예정)* `craft_match:<칵테일id>` | 자유 조합으로 해당 레시피를 재현(복원 퀘스트) — **아직 미구현, 쓰면 빌드 에러** | — |

> goal 판정과 별개로, 어떤 스텝에서든 effects의 `quest(<id>).advance`로 **수동 진행**할 수 있다(§10.4). goal 문법에 없는 목표("삼호와 3번 대화" 같은 것)는 당장은 수동 advance로 표현하면 된다.

### 10.4 진행 경로 2가지 — 언제 무엇을 쓰나

| 경로 | 쓰는 곳 | 예 (실데이터) |
| --- | --- | --- |
| **자동 (goal 훅)** | 목표가 "플레이어의 시스템 행동"일 때 — 제공·상호작용. 스텝을 어디에도 안 심어도 1부 자유 플레이 중 달성 가능 | `samho_honey`: `serve:gin_fizz` — 씬 안 서빙 순간 자동 진행 |
| **수동 (`quest(x).advance`)** | 목표가 "서사적 사건"일 때 — 특정 씬 도달, 대화 완료, 아이템 전달 연출 등 | `lost_box`: 상자 발견 씬의 effect 스텝에서 advance |

혼용 가능하다(스테이지 1은 자동, 스테이지 2는 수동 등). 단 **같은 스테이지를 양쪽에서 진행시키지 마라** — advance는 무조건 +1이라 이중 발동하면 스테이지를 건너뛴다.

### 10.5 보상 설계 패턴 — 무엇을 어디에 쓰나

| 보상 종류 | 쓰는 곳 | 이유 |
| --- | --- | --- |
| 호감도·골드·평판 | `reward_effects`에 `affinity.x += n` / `money += n` | 즉시 지급이 자연스러움 (토스트로 표시) |
| 소모 아이템 지급 | `reward_effects`에 `give(재료, n)` | 인벤토리 직접 추가 |
| **레시피(히든 칵테일) 해금** | `reward_effects`에 `unlock_recipe(칵테일id)` | **즉시** 메뉴에 추가. 재료가 아직 없으면 메뉴에는 보이되 못 만드는 상태도 허용됨 |
| **재료 해금** | reward_effects가 아니라 **Ingredients의 `unlock_when`** (완료 플래그 참조) | 재료는 '입고'라는 연출 단계가 있다 — 완료 플래그를 세워두면 **다음날 stock_in 화면에 자동으로 등장**한다. 즉시 지급하고 싶으면 give()를 병용 |
| 후속 이벤트 개방 | 후속 씬/선택지/슬롯의 `when`에 `quest.<id>.stage` 또는 완료 플래그 | §10.7 연계 패턴 |

**퀘스트 전용 재료 컨벤션 (빌드 강제):** 일차 해금이 아닌 재료는 `unlock_day = 99` + `unlock_when` **필수**. 99인데 when이 없으면 영구 미해금이므로 빌드 에러. 이 재료를 쓰는 칵테일은 파생 해금일도 99가 되는데, 그 칵테일에 `unlock_when`도 없고 어디에서도 `unlock_recipe()`로 해금해주지 않으면 **"해금 경로 없음" 빌드 에러**가 난다 — 도달 불가능한 콘텐츠가 데이터에 숨는 것을 원천 차단.

### 10.6 when DSL 연동 — `quest.<id>.stage`

```
quest.samho_honey.stage >= 1     한 스테이지라도 완료했나 (단일 스테이지 퀘스트 = 완료 판정)
quest.samho_honey.stage == 0     아직 시작 전이거나 미진행
quest.<id>.stage >= <총단계수>    다단계 퀘스트의 완료 판정
```

미등록 퀘스트를 참조하면 0으로 평가된다(에러 아님). 완료 판정은 stage로도, 보상에서 세운 완료 플래그(`flag.q_<id>_done`)로도 할 수 있다 — **플래그 쪽이 의도를 더 잘 드러내므로 보상에 완료 플래그를 반드시 하나 세우는 것을 표준으로 한다.**

### 10.7 저작 패턴 3종

**① 수주 제안 + 수락/거절** — 제안은 반드시 상대 대사로 끝내고(루나 규칙) 선택지를 단다. 거절에도 플래그(`_declined`)를 세워두면 뒷날 "그때 그 얘긴데—" 재제안 씬을 when으로 노출할 수 있다. 수락 이후의 스텝(order/craft/serve/반응)은 전부 `when: flag.q_<id>_started`로 게이트 — 거절 시 자동으로 건너뛰어져 한 씬 안에서 분기가 끝난다.

**② 연계(후속 확인)** — 완료 여부를 다른 씬이 들여다본다. 실데이터: 퇴근길 독백이 `quest.samho_honey.stage >= 1`, 테라스의 크리스 반응이 `flag.q_samho_honey_done`으로 게이트. 후속 퀘스트의 수주 씬 자체를 `when: flag.q_<선행>_done`으로 걸면 퀘스트 체인이 된다.

**③ 다단계** — 수주(day N) → 수행(day N, 자유 플레이) → 납품(day N+1 씬에서 수동 advance) 같은 구조는 스테이지를 나눠 표현한다. 중간 스테이지 통과 연출은 `on_complete`로.

### 10.8 실데이터 워크스루 — `samho_honey` "진짜 벌꿀" (day 3)

> 시트 6곳에 걸친 행들이 어떻게 맞물리는지 전체 추적. 이 퀘스트가 시스템 데모의 정본이다.

**참여 행 전체:**

| 시트 | 행 | 역할 |
| --- | --- | --- |
| Steps | `d3_samho` #16~17 | 제안 대사 (선택지 직전 = 손님 대사, 루나 규칙 준수) |
| Steps | `d3_samho` #18 | `choice: ch_d3_deal` |
| Choices | `ch_d3_deal` #1 | "좋아요…" → `flag.q_samho_honey_started = true` (수주) |
| Choices | `ch_d3_deal` #2 | "사양할게요" → `flag.q_samho_honey_declined = true` (거절) |
| Steps | `d3_samho` #19~23 | `when: flag.q_samho_honey_started` 게이트 — order(exact:gin_fizz) → craft → serve → 성사 대사 2줄 |
| Steps | `d3_samho` #24 | `when: flag.q_samho_honey_declined` — 거절 반응 1줄 |
| QuestStages | `samho_honey` #1 | `goal: serve:gin_fizz`, `when: flag.q_samho_honey_started` |
| Quests | `samho_honey` | `reward_effects: affinity.samho += 10; unlock_recipe(bees_knees); flag.q_samho_honey_done = true` |
| Ingredients | `honey_syrup` | `unlock_day 99` + `unlock_when: flag.q_samho_honey_done` → **day 4 stock_in에 등장** |
| Cocktails/RecipeLines | `bees_knees` | 진 2oz + 벌꿀 0.75oz + 레몬 스퀴즈 0.75oz, shake → 파생 T3, 파생 해금일 99 (unlock_recipe로만 해금) |
| Steps | `d3_commute_out` #2 | 연계: `when: quest.samho_honey.stage >= 1` 독백 |
| Steps | `d3_home_talk` #8 | 연계: `when: flag.q_samho_honey_done` 크리스 반응 |

**실행 타임라인 (수락 시):**

```
선택 "좋아요"        → flag.q_samho_honey_started = true            (Choices.effects)
진피즈 제조·서빙     → serve 훅: goal serve:gin_fizz 일치
                      + when(started) 통과 + Sewage 아님
                      → stage 0→1 (= 마지막 스테이지)
                      → reward_effects 즉시 1회 발동:
                          삼호 호감 +10 (토스트)
                          비즈 니즈 메뉴 등록 (즉시)
                          flag.q_samho_honey_done = true
같은 씬 뒷스텝       → "거래 성립" / "레시피 하나 —" 대사 (started 게이트)
퇴근길               → quest.stage 게이트 독백 출력
테라스               → 크리스 반응 출력 (done 게이트)
다음날 stock_in      → 벌꿀 원액 입고 카드 등장 (unlock_when 충족분)
                      → 이때부터 비즈 니즈 실제 제조 가능
```

거절 시: started가 없으므로 #19~23 스킵 → #24 거절 대사만 → 퀘스트는 stage 0으로 남고, 벌꿀·비즈 니즈는 영영 미해금(이번 회차). 재제안을 원하면 뒷날 씬을 `when: flag.q_samho_honey_declined && quest.samho_honey.stage == 0`으로 파면 된다.

**대조용 — `lost_box` (수동 advance 경로):** day2 완 씬에서 started 플래그 → 퇴근길 `p_lost_box` 포인트의 상자 발견 씬에서 `quest(lost_box).advance` (effect 스텝) → 보상 `money += 50; give(lime, 3)`. goal은 `interact:p_lost_box`로 등록되어 있어 어느 쪽 경로로도 성립하는 구조.

### 10.9 저작 체크리스트 & 빌드 검증

퀘스트 하나를 넣을 때 확인할 것:

1. Quests 1행 + QuestStages 1..N행 (연속 번호)
2. 수주 지점(Choices 또는 Steps effects)에서 `_started` 플래그
3. 각 스테이지 `when`에 수주 플래그 (수주 없이 진행되면 안 되는 경우)
4. `reward_effects`에 **완료 플래그** 포함 (연계·재료 해금의 기준점)
5. 재료 보상이면 Ingredients에 `99 + unlock_when(완료 플래그)` 행
6. 레시피 보상이면 `unlock_recipe()` — 그 칵테일의 재료가 전부 해금되는 경로도 함께 확인
7. 연계 지점(후속 씬/선택지)의 when 작성

빌드가 자동으로 잡는 것: 스테이지 없는 퀘스트 / 번호 불연속 / goal 문법 위반·참조 대상 부재 / `quest(x).advance`의 x 미등록 / `unlock_recipe`·`give` 참조 대상 부재 / kind 오타 / 제목 en 누락 / **99 재료의 when 누락** / **해금 경로 없는 칵테일**.

### Endings (폭포 — priority 순 first-match)

| priority | id | when |
| --- | --- | --- |
| 1 | bad_1 (병기화) | `flag.rios_accepted` |
| 2 | happy_1 (고발) | `affinity.aili >= 100 && affinity.tom >= 100 && affinity.port >= 100 && affinity.sunha >= 100` |
| 3 | happy_2 (이사) | `affinity.aili >= 100 && affinity.tom >= 100 && affinity.port >= 100` |
| 4 | normal_1 (하루 루트) | `alive.haru && affinity.haru >= 100` |
| 5 | bad_2 (납치) | *(default)* |

각 행은 연출용 `scene_id`를 갖는다 (엔딩 연출도 같은 씬-스텝 문법).

---

## 11. 💾 세이브 스키마 — 이원화 (결정 L)

### 11.1 수동 저장 (집의 저장 오브젝트에서만)

하루 단위의 안정된 스냅숏. 저장 위치가 집으로 고정이므로 **phase를 저장할 필요가 없다** — 항상 "그날 밤 집" 시점.

```jsonc
{
  "saveVersion": 1,
  "day": 3,                              // 다음 기상 = day4
  "gold": 1240, "reputation": 6,
  "affinity": { "samho": 42, "aili": 30 },
  "alive": { "samho": true, "haru": true },
  "flags": ["samho_calmed", "knows_chris_smokes"],
  "boughtIngredients": ["lime"],        // unlock_day 외 추가 획득분만
  "extraRecipes": [],                   // 퀘스트/이벤트 해금분만
  "quests": { "lost_box": 1 },
  "stats": { "served": 14, "angryLeft": 2, "bestGrade": "excellent" },
  "settlements": [ { "day": 2, "sales": 800, "tips": 120 } ]
}
```

### 11.2 크래시 복구 스냅숏 (자동, 강제 종료 대비 전용)

수동 저장과 **별개 파일**. 스텝 진행·페이즈 전환마다 수시 갱신하고, 정상적으로 집에서 저장하면 삭제한다. 부팅 시 이 파일이 남아 있으면(= 비정상 종료) "이어서 진행할까요?"(`ui_crash_resume`)를 띄우고 해당 지점으로 복원한다.

```jsonc
{
  "base": { /* 11.1과 동일한 상태 블록 — 그 시점의 전체 상태 */ },
  "resume": {
    "phase": "bar_story",               // 어느 페이즈였나
    "sceneId": "d3_samho",              // 바 내부(대본 중)면: 씬 + 스텝
    "stepSeq": 9,
    "posX": null,                        // 외부(출퇴근길)면: 플레이어 좌표 (+ phase로 방향 복원)
    "part1": {                           // 1부 도중이면: 좌석·대기열 상태
      "queueIndex": 3,
      "seats": [ { "state": "waitingServe", "slotSeq": 2, "cocktail": "gin_tonic",
                   "round": 1, "discontent": 12.5, "result": {"pct": 91, "grade": "good"} } ],
      "servedCount": 2, "angryLeftCount": 0
    }
  }
}
```

> 복원 단위 원칙: **바 대본 중 = 그 대사 스텝부터 / 외부 = 그 좌표부터 / 1부 = 좌석 상태 통째로 / 제조 미니게임 도중 = 제조 시작 직전으로 롤백**(기믹 중간 상태까지 저장하는 건 과설계 — 그 잔만 다시 만들게 한다).

> 공통 원칙: **파생 가능한 것은 저장하지 않는다.** 해금 재료 목록은 `unlock_day <= day` + unlock_when 플래그 + 구매분으로 복원된다. saveVersion + 마이그레이션 훅은 스팀 요구사항(인수인계 §36) 계승.

---

## 12. 🔧 빌드 파이프라인 & 검증 규칙

```
LUNA_System.xlsx(제조·운영 10시트) + LUNA_Narrative.xlsx(대사·서사 20시트) → 데이터/tools/build.py(검증) → json/(도메인별 분할 배포) + 빌드리포트.txt

json/cocktails.json shelf_items.json characters.json                 ← 구엔진처럼 도메인별 낱개 파일 (20종)
     expressions.json cutscenes.json field_anims.json
     personalities.json barks.json tastes.json dossier.json ui_strings.json
     balance.json(=config·grade_cuts·grade_payout·affinity_matrix — 튜닝 계수는 한 파일)
     days.json random_waves.json regular_slots.json spots.json interact_points.json
     quests.json(스테이지 포함) endings.json order_rules.json
     script/common.json script/day_N.json                            ← 대본만 하루 단위(로딩 단위)
```

분할 기준: **"항상 같이 로드되고, 같이 튜닝되고, 혼자서는 의미가 없는 것"만 묶는다** — 그래서 밸런스 계수 4종(각 0.1~0.6KB)은 balance.json 하나, 퀘스트 스테이지는 quests.json 안, 나머지는 전부 낱개.

**저작은 1파일, 배포는 잘게 (PD 확정)** — 기획자는 엑셀 하나만 관리하고, 엔진은 필요한 도메인 파일만 로드한다(부분 패치·Addressable 번들 단위에도 유리). 같은 검증을 통과한 한 빌드에서 모든 파일이 함께 나오므로 구엔진 시절의 "파일별 버전 어긋남"은 없다. 웹 프로토는 `tools/bundle_proto.py`가 분할 JSON을 런타임 메모리 구조로 조립한다.

v2.0부터 **한 시트는 정확히 한 파일에만 존재해야 한다** — 같은 시트가 두 파일에 있으면 빌드가 에러를 낸다(참고용 복사가 조용히 병합돼 유령 데이터가 되는 사고 차단). 두 파일을 합친 뒤 검증은 전체 기준이라 **분할은 저작 편의일 뿐 정합 보장은 통짜와 동일**하다. 빌드리포트 상단에 파일별 시트·행수가 찍히므로 "남의 파일 안 당겨오고 빌드"도 눈으로 걸린다.

**v1.5부터 시트 파싱 build.py가 정식 파이프라인이다** — 기획자는 엑셀만 고치고 `python3 데이터/tools/build.py`를 돌리면 된다. 검증 실패 시 JSON을 내보내지 않는다. 검증·파생·출력 로직은 gen_luna_data.py의 것을 공유하므로(규칙 이원화 방지) 두 경로의 산출물이 완전히 같다는 것이 라운드트립 테스트로 보증된다. gen_luna_data.py는 "코드 시드 → xlsx 재생성" 용도로만 남는다 — **팀 저작이 시작된 뒤에 gen을 돌리면 시트 수정분이 시드로 덮이니 금지.**

빌드가 잡아주는 것 (전부 자동):

1. **참조 무결성** — 존재하지 않는 ingredient/cocktail/scene/choice/character id 인용 → 에러
2. **티어 공식 대조** — 시트 수기 tier ≠ 계산 tier → 경고
3. **해금표 재생성** — "Day N: 입고 재료 → 해금 칵테일" 표를 자동 출력, ③문서 표와 눈으로 대조
4. **슬롯 빈 풀 검사** — RandomWaves·RegularSlots의 tier가 그 시점 해금 풀에 없으면 에러 (③문서 주의사항의 자동화). (day,seq)가 두 시트에 겹치면 에러
4-1. **Barks 커버리지** — 성격×핵심 상황(call·order·react_*·bye_*·idle)에 전용·공용 대사가 모두 0줄이면 경고 (2파일 분리로 System↔Narrative 의존이 안 보이게 된 것을 빌드가 감시)
5. **씬 그래프** — goto 대상 존재, day×phase 커버리지 리포트(예: "day7 home 씬 없음 → 자동 수면" 목록 출력)
6. **생사 인과** — `alive.samho` 참조가 day3 이전 씬에 있으면 경고, `alive.haru`는 day7 이전이면 경고
7. **엔딩 폭포** — 도달 불가능한 행(위 순위가 항상 가로채는 조건) 경고
8. **텍스트** — 빈 대사, 중복 id, 금칙 마크업(리치텍스트 태그) 검출
9. **L10N 완결성** — ko가 있는데 en이 빈 대사/선택지/대사풀 → 에러 (결정 I)
10. **루나 대사 규칙** — craft/serve/choice 직전 스텝이 루나 say면 → 에러 (§7 저작 규칙)
11. **카메오 정합성** — cameo_scene에 character 미지정, 존재하지 않는 캐릭터/씬 참조 → 에러
12. **퀘스트 구조** (v1.4) — 스테이지 없는 퀘스트, 번호 1..N 불연속, kind 오타(main/side 외), 제목 en 누락 → 에러
13. **goal 문법** (v1.4) — `interact:`/`serve:` 외 문자열, 참조하는 포인트/칵테일 부재 → 에러
14. **effects 전수 스캔** (v1.4) — 모든 effects 문자열(스텝·선택지·주문·퀘스트 보상·on_complete)에서 `quest()`/`unlock_recipe()`/`give()`의 참조 대상 부재 → 에러
15. **해금 경로** (v1.4) — unlock_day 99 재료에 unlock_when 없음 / 파생 해금일 99인 칵테일에 unlock_when도 unlock_recipe() 발동처도 없음(도달 불가 콘텐츠) → 에러
16. **when DSL 전수 검사** (v1.5) — 모든 when 문자열(씬·스텝·선택지·주문·포인트·퀘스트단계·엔딩·해금)의 **문법**(어휘·연산자·비교값 타입)과 **참조**(affinity/alive 캐릭터, quest id, cocktail.id, 등급명) → 에러
17. **effects DSL 전수 검사** (v1.5) — 문법에 없는 효과 절 → 에러. **런타임은 모르는 효과를 조용히 무시하므로 오타는 빌드에서만 잡을 수 있다** — 이 검사가 그 방어선
18. **플래그 교차 검사** (v1.5) — 참조되는데 어디서도 세워지지 않는 플래그(오타 or 미작성 일차) ⚠ 경고 / 세워지는데 참조가 없는 플래그 ℹ 정보 — 에러는 아님(미래 일차용 플래그가 정상 존재)
19-1. **취향(Tastes) 검증** (v1.6) — 캐릭터 참조, tier가 AffinityMatrix 행에 존재(miss 금지), when 비어 있음, (캐릭터,seq) 중복 → 에러. when은 규칙 16의 전수 검사에 포함
19. **호감도 상한 시뮬레이션** (v1.5) — 스텝·선택지(그룹당 캐릭터별 최대 1개)·퀘스트 보상의 명시 가산 + 씬 서빙 잠재치(love×excellent)를 일차 누적으로 합산해 **엔딩컷 100 대비 %를 리포트** — 밸런스 설계의 기준선. 데이터가 day13까지 차면 "산술적 도달 불가" 여부가 숫자로 드러난다

---

## 13. 🌱 확장 포인트 (이렇게 들어온다)

| 추가될 것 | 작업 |
| --- | --- |
| 칵테일 18~30종 | Cocktails·RecipeLines에 행 추가 + 재료 unlock_day 조정. 코드 변경 0 |
| 새 사이드퀘스트 | Quests·QuestStages 행 + 수주 선택지 + (보상이면) 재료/레시피 행. §10.9 체크리스트. 코드 변경 0 |
| 새 goal 타입 (`craft_match:` 등) | goal 문법 enum + 엔진 훅 1곳 + 검증 규칙 1줄 |
| 새 기믹 종류 | RecipeLines.action enum 값 추가 + 제조 시스템 구현 + 티어 공식 항 추가 (1곳) |
| 새 스토리 손님 | Characters 행 + Scenes/Steps 행. 코드 변경 0 |
| L10N (EN/JP) | Strings 시트 추가 (`key, ko, en, ja`) — text_ko 컬럼이 키로 치환됨 |
| 2부 craving/vague 주문 | Orders의 when에 `cocktail.tag()` 이미 지원 — 데이터만 작성 |
| 1부 성격유형별 배율 | Personalities의 예약 컬럼(tip_mult/patience_mult)에 값만 |
| 리듬게임 기믹(보류 중) | action enum + 티어 공식 — 위 "새 기믹"과 동일 경로 |

---

## 14. 🚧 열린 이슈 (데이터 채우면서 확정 필요)

- [ ] **17종 abv 값** — ②문서에 없음. 삼호 분기(day3)가 abv에 걸리므로 필수. 실제 도수 기준 제안값을 데이터 작성 시 함께 제출 예정
- [ ] **time_limit 계수** (`time_base`, `time_per_gimmick`) — 초기값 제안 후 플레이테스트
- [ ] **personalityType 5종 목록** — Barks 작성 전 확정 필요 (⑥문서와 협의)
- [ ] **T1 Excellent 역설** — GradeCuts를 티어별 오버라이드 가능하게 시트를 파두었으나 값은 미정
- [ ] **서브재료 해금일 역산 결과** 검토 (빌드 리포트로 확인)
- [ ] 노점 상점의 품목·가격 (Points의 `shop:` + Ingredients.shop_price)
