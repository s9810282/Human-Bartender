# -*- coding: utf-8 -*-
# day99_test.py — QA 테스트 전용 대본 (day 99 = '날짜로는 안 열림' 컨벤션, _shift_day 미적용)
#
# 일반 진행에서는 절대 열리지 않는다. 개발 진입(엔진이 day=99로 하루를 시작)에서만 재생.
# 구조: bar_open 허브(대사·분기·연출 + "1부 시작" 항목) → 1부(day99 테스트 웨이브 2팀)
#      → bar 허브(주문 세트·등급 분기·2인 연속 주문·end_part).
# 허브 순환 규칙: 모든 테스트 씬은 마지막 스텝이 choice(goto 허브)다 — goto 없이 씬이 끝나면
# auto 씬이 소진돼 phase가 넘어가 버린다. 의도적으로 phase를 넘기는 항목은
# ch_t99_main의 "1부 시작"(goto 공란 + 마지막 스텝 = 씬 완료 → 개점 종료)뿐이다.
# 삭제할 때는 이 파일과 gen_luna_data.py의 병합 3줄만 지우면 된다.

# SCENE_COLS: id, day, phase, seq, trigger, when, title (skippable/group은 gen이 패딩)
TEST_SCENES = [
    ("t99_open",         99, "bar_open", 1,  "auto",   "", "테스트 허브 — 개점(대사·분기·연출·1부 진입)"),
    ("t99_dialogue",     99, "bar_open", 2,  "manual", "", "테스트 — 대사·표정·태그·읽음 스킵"),
    ("t99_branch_menu",  99, "bar_open", 3,  "manual", "", "테스트 — 분기·효과 메뉴"),
    ("t99_flag_test",    99, "bar_open", 4,  "manual", "", "테스트 — 플래그 심고 when 분기"),
    ("t99_effect_check", 99, "bar_open", 5,  "manual", "", "테스트 — 효과 적용 확인"),
    ("t99_stage_menu",   99, "bar_open", 6,  "manual", "", "테스트 — 좌석·연출 메뉴"),
    ("t99_seats",        99, "bar_open", 7,  "manual", "", "테스트 — 좌석 등장·퇴장·카메라 전환"),
    ("t99_fxsfx",        99, "bar_open", 8,  "manual", "", "테스트 — fx·sfx·컷씬"),
    ("t99_hub",          99, "bar_open", 10, "manual", "", "테스트 허브 — 메인 메뉴 재표시"),
    ("t99_bar",          99, "bar",      1,  "auto",   "", "테스트 허브 — 2부(주문·제조·정산)"),
    ("t99_order_normal", 99, "bar",      2,  "manual", "", "테스트 — 주문 세트 정상 흐름(진토닉)"),
    ("t99_order_grade",  99, "bar",      3,  "manual", "", "테스트 — 등급 5분기·실패 시 재제조(진피즈)"),
    ("t99_two_guests",   99, "bar",      4,  "manual", "", "테스트 — 2인 연속 주문·서빙 대상 분리"),
    ("t99_bar_hub",      99, "bar",      5,  "manual", "", "테스트 허브 — 2부 메뉴 재표시"),
    ("t99_end",          99, "bar",      9,  "manual", "", "테스트 — end_part·일일 매출 정산"),
]

# STEP_COLS: scene_id, seq, type, actor, arg, text_ko, text_en, when, effects, sync, note
TEST_STEPS = [
    # ── 개점 허브: 첫 스텝이 곧 메뉴 (안내 대사를 넣으면 루나 say 직전 choice 금지에 걸린다) ──
    ("t99_open", 1, "choice", "", "ch_t99_main", "", "", "", "", "", "테스트 메인 메뉴"),

    # ── 대사·표정·태그·읽음 스킵 ──
    ("t99_dialogue", 1, "enter", "port", "M", "", "", "", "", "", ""),
    ("t99_dialogue", 2, "say", "port", "default", "테스트 씬이다. 지금부터 표정을 바꾼다. 이건 default.",
     "This is a test scene. Watch my expression change. This is default.", "", "", "", ""),
    ("t99_dialogue", 3, "say", "port", "joy", "이건 joy.", "This is joy.", "", "", "", ""),
    ("t99_dialogue", 4, "say", "port", "anger", "이건 anger.", "This is anger.", "", "", "", ""),
    ("t99_dialogue", 5, "say", "port", "serious", "이건 serious. 표정이 즉시 교체되면 정상.",
     "This is serious. Instant swap means it works.", "", "", "", ""),
    ("t99_dialogue", 6, "say", "port", "default", "<name>진토닉</name> — 이 단어가 강조되어 보이면 텍스트 태그 정상.",
     "<name>Gin & Tonic</name> — if this word is highlighted, text tags work.", "", "", "", ""),
    ("t99_dialogue", 7, "say", "luna", "default", "(루나 대사는 초상 없이 이름과 본문만 보이면 정상. 이 씬을 다시 열면 읽음 스킵이 동작해야 한다.)",
     "(Luna shows name and text only, no portrait. Reopen this scene to check read-skip.)", "", "", "", "읽음 스킵 확인용"),
    ("t99_dialogue", 8, "exit", "port", "", "", "", "", "", "", "루나 say 직전 choice 금지 회피 겸 정리"),
    ("t99_dialogue", 9, "choice", "", "ch_t99_back", "", "", "", "", "", ""),

    # ── 분기·효과 메뉴 ──
    ("t99_branch_menu", 1, "choice", "", "ch_t99_branch", "", "", "", "", "", "2번 항목 자체가 effects 적용 테스트"),

    # ── 플래그 분기 ──
    ("t99_flag_test", 1, "effect", "", "", "", "", "", "flag.t99_probe = true", "", "플래그 심기"),
    ("t99_flag_test", 2, "say", "luna", "default", "(t99_probe 참 — 이 대사가 보이면 when 분기 정상.)",
     "(t99_probe is true — if you see this, when-branching works.)", "flag.t99_probe", "", "", ""),
    ("t99_flag_test", 3, "say", "luna", "default", "(이 대사가 보이면 when 분기 버그다.)",
     "(If you see this, when-branching is broken.)", "!flag.t99_probe", "", "", "안 보여야 정상"),
    ("t99_flag_test", 4, "effect", "", "", "", "", "", "flag.t99_probe = false; flag.t99_probe_done = true",
     "", "리셋(재테스트 가능) + 분기 메뉴 조건부 항목 해금"),
    ("t99_flag_test", 5, "choice", "", "ch_t99_back2", "", "", "", "", "", ""),

    # ── 효과 적용 확인 ──
    ("t99_effect_check", 1, "say", "luna", "default", "(골드 +100 · 평판 +1 · 포트 호감 +2 적용됨 — 좌측 패널과 수첩에서 확인.)",
     "(Gold +100, Reputation +1, Port affinity +2 applied — check the side panel and dossier.)", "flag.t99_paid", "", "", ""),
    ("t99_effect_check", 2, "effect", "", "", "", "", "", "flag.t99_paid = false", "", "리셋 — 재선택 시 다시 확인 가능"),
    ("t99_effect_check", 3, "choice", "", "ch_t99_back3", "", "", "", "", "", ""),

    # ── 좌석·연출 메뉴 ──
    ("t99_stage_menu", 1, "choice", "", "ch_t99_stage", "", "", "", "", "", ""),

    # ── 좌석·카메라 (인접 강제: L·M — L+R 양 끝은 빌드 에러) ──
    ("t99_seats", 1, "enter", "port", "L", "", "", "", "", "", ""),
    ("t99_seats", 2, "say", "port", "default", "1인 프레임(960×540). 카메라가 나를 중앙에 잡으면 정상.",
     "One-guest frame (960×540). Camera should center on me.", "", "", "", ""),
    ("t99_seats", 3, "enter", "aili", "M", "", "", "", "", "", "1→2인 축소 전환"),
    ("t99_seats", 4, "say", "aili", "default", "2인 프레임(1280×720)으로 부드럽게 축소됐으면 정상. 인접 좌석 L·M.",
     "Smooth zoom-out to the two-guest frame (1280×720). Adjacent seats L·M.", "", "", "", ""),
    ("t99_seats", 5, "exit", "port", "", "", "", "", "", "", "2→1인 확대 전환"),
    ("t99_seats", 6, "say", "aili", "default", "다시 1인 프레임. 남은 나를 중앙으로.",
     "Back to the one-guest frame, centered on me.", "", "", "", ""),
    ("t99_seats", 7, "exit", "aili", "", "", "", "", "", "", ""),
    ("t99_seats", 8, "choice", "", "ch_t99_back4", "", "", "", "", "", ""),

    # ── fx·sfx·컷씬 ──
    ("t99_fxsfx", 1, "fx", "", "glitch_in", "", "", "", "", "", "화면 효과"),
    ("t99_fxsfx", 2, "sfx", "", "alarm_distant", "", "", "", "", "", "효과음"),
    ("t99_fxsfx", 3, "fx", "", "hard_cut", "", "", "", "", "", ""),
    ("t99_fxsfx", 4, "timeline", "", "tl_catmilk", "", "", "", "", "", "실제 컷씬 재생 확인"),
    ("t99_fxsfx", 5, "choice", "", "ch_t99_back5", "", "", "", "", "", ""),

    # ── 메인 허브 재표시 ──
    ("t99_hub", 1, "choice", "", "ch_t99_main", "", "", "", "", "", "같은 선택지 세트 재사용"),

    # ── 2부 허브 ──
    ("t99_bar", 1, "choice", "", "ch_t99_bar", "", "", "", "", "", "2부 테스트 메뉴"),

    # ── 주문 세트 정상 흐름 (order→craft→serve + order_match·grade 분기) ──
    ("t99_order_normal", 1, "enter", "port", "M", "", "", "", "", "", ""),
    ("t99_order_normal", 2, "say", "port", "default", "주문 세트 테스트다. 주문 대사 직후 내 앞에 코스터가 깔리면 정상.",
     "Order-set test. A coaster should appear in front of me right after I order.", "", "", "", ""),
    ("t99_order_normal", 3, "order", "port", "exact:gin_tonic", "진토닉 한 잔.", "One Gin & Tonic.", "", "", "", ""),
    ("t99_order_normal", 4, "craft", "", "order", "", "", "", "", "", ""),
    ("t99_order_normal", 5, "serve", "port", "", "", "", "", "", "", ""),
    ("t99_order_normal", 6, "say", "port", "joy", "주문한 진토닉이 맞군. (order_match 참 분기)",
     "That's the Gin & Tonic I ordered. (order_match true branch)", "order_match == true", "", "", ""),
    ("t99_order_normal", 7, "say", "port", "anger", "이건 주문한 게 아닌데. (order_match 거짓 — 강제 Sewage 확인)",
     "This isn't what I ordered. (order_match false — forced Sewage check)", "order_match == false", "", "", ""),
    ("t99_order_normal", 8, "say", "port", "default", "good 이상 분기다.", "Grade good-or-better branch.", "grade >= good", "", "", ""),
    ("t99_order_normal", 9, "say", "port", "default", "good 미만 분기다.", "Below-good branch.", "grade < good", "", "", ""),
    ("t99_order_normal", 10, "exit", "port", "", "", "", "", "", "", ""),
    ("t99_order_normal", 11, "choice", "", "ch_t99_bar_back", "", "", "", "", "", ""),

    # ── 등급 5분기 + 실패 시 재제조 (크리스 튜토리얼 패턴) ──
    ("t99_order_grade", 1, "enter", "chris", "R", "", "", "", "", "", ""),
    ("t99_order_grade", 2, "say", "chris", "default", "등급 분기 테스트다. 진피즈를 만들어 봐.",
     "Grade-branch test. Make me a Gin Fizz.", "", "", "", ""),
    ("t99_order_grade", 3, "order", "chris", "exact:gin_fizz", "진피즈.", "Gin Fizz.", "", "", "", ""),
    ("t99_order_grade", 4, "craft", "", "order", "", "", "", "", "", ""),
    ("t99_order_grade", 5, "serve", "chris", "", "", "", "", "", "", ""),
    ("t99_order_grade", 6, "say", "chris", "success", "excellent 분기.", "Excellent branch.", "grade >= excellent", "", "", ""),
    ("t99_order_grade", 7, "say", "chris", "default", "good 분기.", "Good branch.", "grade >= good && grade < excellent", "", "", ""),
    ("t99_order_grade", 8, "say", "chris", "default", "decent 분기.", "Decent branch.", "grade >= decent && grade < good", "", "", ""),
    ("t99_order_grade", 9, "say", "chris", "fail", "poor 분기.", "Poor branch.", "grade >= poor && grade < decent", "", "", ""),
    ("t99_order_grade", 10, "say", "chris", "fail", "sewage 분기.", "Sewage branch.", "grade < poor", "", "", ""),
    ("t99_order_grade", 11, "say", "chris", "default", "good 미만이니 한 잔 더. 재제조 게이트 테스트다.",
     "Below good — one more. Re-craft gate test.", "grade < good", "flag.t99_retry = true", "", "직전 결과를 지속 플래그로 넘긴다"),
    ("t99_order_grade", 12, "order", "chris", "exact:gin_fizz", "같은 걸로 다시.", "Same again.", "flag.t99_retry", "", "", "재주문 수락 뒤 직전 ServeResult 폐기"),
    ("t99_order_grade", 13, "craft", "", "order", "", "", "flag.t99_retry", "", "", ""),
    ("t99_order_grade", 14, "serve", "chris", "", "", "", "flag.t99_retry", "flag.t99_retry = false", "", ""),
    ("t99_order_grade", 15, "say", "chris", "success", "good 이상 — 통과.", "Good or better — pass.", "grade >= good", "", "", "1차 또는 재제조 결과"),
    ("t99_order_grade", 16, "say", "chris", "default", "등급 테스트 끝.", "Grade test done.", "", "", "", ""),
    ("t99_order_grade", 17, "exit", "chris", "", "", "", "", "", "", ""),
    ("t99_order_grade", 18, "choice", "", "ch_t99_bar_back2", "", "", "", "", "", ""),

    # ── 2인 연속 주문 — 서빙 대상 = 직전 order.actor 분리 확인 ──
    ("t99_two_guests", 1, "enter", "port", "L", "", "", "", "", "", ""),
    ("t99_two_guests", 2, "enter", "aili", "M", "", "", "", "", "", "인접 L·M"),
    ("t99_two_guests", 3, "say", "port", "default", "연속 주문 테스트. 내가 먼저다.",
     "Back-to-back order test. Me first.", "", "", "", ""),
    ("t99_two_guests", 4, "order", "port", "exact:gin_tonic", "진토닉.", "Gin & Tonic.", "", "", "", ""),
    ("t99_two_guests", 5, "craft", "", "order", "", "", "", "", "", ""),
    ("t99_two_guests", 6, "serve", "port", "", "", "", "", "", "", "포트 코스터에만 유효 드롭"),
    ("t99_two_guests", 7, "say", "aili", "joy", "다음은 나! 코스터가 내 앞에 새로 깔리는지 봐.",
     "My turn! Watch the coaster appear in front of me.", "", "", "", ""),
    ("t99_two_guests", 8, "order", "aili", "exact:gin_fizz", "진피즈!", "Gin Fizz!", "", "", "", ""),
    ("t99_two_guests", 9, "craft", "", "order", "", "", "", "", "", ""),
    ("t99_two_guests", 10, "serve", "aili", "", "", "", "", "", "", "아일리 코스터에만 유효 드롭 — 포트 쪽은 스냅백"),
    ("t99_two_guests", 11, "say", "port", "default", "서빙 대상이 안 섞였으면 정상이다.",
     "If the serves didn't cross, it works.", "", "", "", ""),
    ("t99_two_guests", 12, "exit", "port", "", "", "", "", "", "", ""),
    ("t99_two_guests", 13, "exit", "aili", "", "", "", "", "", "", ""),
    ("t99_two_guests", 14, "choice", "", "ch_t99_bar_back3", "", "", "", "", "", ""),

    # ── 2부 허브 재표시 ──
    ("t99_bar_hub", 1, "choice", "", "ch_t99_bar", "", "", "", "", "", ""),

    # ── 종료 — end_part → 일일 매출 정산 (마지막 bar 씬이어야 빌드 통과) ──
    ("t99_end", 1, "say", "luna", "default", "(end_part — 일일 매출 정산 화면이 한 번만 떠야 정상.)",
     "(end_part — the daily sales settlement should appear exactly once.)", "", "", "", ""),
    ("t99_end", 2, "end_part", "", "", "", "", "", "", "", ""),
]

# CHOICE_COLS: choice_id, seq, text_ko, text_en, when, effects, goto, note, lock_reason_ko, lock_reason_en
TEST_CHOICES = [
    # 메인 허브 — 4번 항목만 goto 공란: 씬 완료 → auto 소진 → 개점 종료 → 1부 시작
    ("ch_t99_main", 1, "[테스트] 대사 · 표정 · 태그 · 읽음 스킵", "[Test] Dialogue · expression · tags · read-skip",
     "", "", "t99_dialogue", ""),
    ("ch_t99_main", 2, "[테스트] 분기 · 효과 (when / effects)", "[Test] Branching · effects (when / effects)",
     "", "", "t99_branch_menu", ""),
    ("ch_t99_main", 3, "[테스트] 좌석 · 카메라 · 연출", "[Test] Seats · camera · presentation",
     "", "", "t99_stage_menu", ""),
    ("ch_t99_main", 4, "[테스트] 1부 시작 — 랜덤 손님 2팀 (개점 종료)", "[Test] Start Part 1 — 2 random guests (ends bar-open)",
     "", "", "", "goto 공란 + 마지막 스텝 = 씬 완료 → 1부 진입"),

    ("ch_t99_back", 1, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),
    ("ch_t99_back", 2, "다시 보기 (읽음 스킵 확인)", "Replay (check read-skip)", "", "", "t99_dialogue", ""),

    ("ch_t99_branch", 1, "플래그 심고 when 분기 확인", "Set a flag and check when-branching", "", "", "t99_flag_test", ""),
    ("ch_t99_branch", 2, "효과 적용 — 골드+100 · 평판+1 · 포트 호감+2", "Apply effects — Gold +100 · Rep +1 · Port +2",
     "", "money += 100; reputation += 1; affinity.port += 2; flag.t99_paid = true", "t99_effect_check",
     "선택지 effects 원자 적용 테스트"),
    ("ch_t99_branch", 3, "조건부 항목 — 플래그 테스트를 통과하면 활성화", "Conditional — unlocks after the flag test",
     "flag.t99_probe_done", "", "t99_flag_test", "회색(비활성) 표시 테스트",
     "플래그 테스트를 먼저 완료해야 합니다.", "Complete the flag test first."),
    ("ch_t99_branch", 4, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),

    ("ch_t99_back2", 1, "분기 메뉴로", "Back to branch menu", "", "", "t99_branch_menu", ""),
    ("ch_t99_back2", 2, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),

    ("ch_t99_back3", 1, "분기 메뉴로", "Back to branch menu", "", "", "t99_branch_menu", ""),
    ("ch_t99_back3", 2, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),

    ("ch_t99_stage", 1, "좌석 등장·퇴장 (1↔2인 카메라 전환)", "Seat enter/exit (1↔2 guest camera)", "", "", "t99_seats", ""),
    ("ch_t99_stage", 2, "fx · sfx · 컷씬", "fx · sfx · cutscene", "", "", "t99_fxsfx", ""),
    ("ch_t99_stage", 3, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),

    ("ch_t99_back4", 1, "좌석·연출 메뉴로", "Back to stage menu", "", "", "t99_stage_menu", ""),
    ("ch_t99_back4", 2, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),

    ("ch_t99_back5", 1, "좌석·연출 메뉴로", "Back to stage menu", "", "", "t99_stage_menu", ""),
    ("ch_t99_back5", 2, "메인 허브로", "Back to main hub", "", "", "t99_hub", ""),

    ("ch_t99_bar", 1, "[테스트] 주문 세트 정상 흐름 (진토닉)", "[Test] Order set, normal flow (Gin & Tonic)",
     "", "", "t99_order_normal", ""),
    ("ch_t99_bar", 2, "[테스트] 등급 5분기 · 실패 시 재제조 (진피즈)", "[Test] 5 grade branches · re-craft on fail (Gin Fizz)",
     "", "", "t99_order_grade", ""),
    ("ch_t99_bar", 3, "[테스트] 2인 연속 주문 · 서빙 대상 분리", "[Test] Two guests, back-to-back orders",
     "", "", "t99_two_guests", ""),
    ("ch_t99_bar", 4, "[테스트] 2부 종료 → 일일 매출 정산", "[Test] End Part 2 → daily settlement",
     "", "", "t99_end", ""),

    ("ch_t99_bar_back", 1, "2부 허브로", "Back to Part 2 hub", "", "", "t99_bar_hub", ""),
    ("ch_t99_bar_back", 2, "한 번 더", "Once more", "", "", "t99_order_normal", ""),

    ("ch_t99_bar_back2", 1, "2부 허브로", "Back to Part 2 hub", "", "", "t99_bar_hub", ""),
    ("ch_t99_bar_back2", 2, "한 번 더", "Once more", "", "", "t99_order_grade", ""),

    ("ch_t99_bar_back3", 1, "2부 허브로", "Back to Part 2 hub", "", "", "t99_bar_hub", ""),
    ("ch_t99_bar_back3", 2, "한 번 더", "Once more", "", "", "t99_two_guests", ""),
]

# WAVE_COLS: day, seq, order, personality, delay_sec, max_rounds, branch_choice
# "1부 시작" 항목이 개점을 끝내면 이 웨이브가 재생된다. 1번 = 지정 주문(예측 가능), 2번 = 풀 추첨 + 다회 주문.
TEST_WAVES = [
    (99, 1, "gin_tonic", "gentle", 0,  1, False),
    (99, 2, "",          "chatty", 20, 2, False),
]
