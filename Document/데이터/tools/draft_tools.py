#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
L.U.N.A 대사 초안 도구 v2 — 작가(이기현) 전용 3탭 워크북과 PD 반영 스크립트

  python3 데이터/tools/draft_tools.py init            # LUNA_Draft.xlsx 생성 (이미 있으면 중단)
  python3 데이터/tools/draft_tools.py import          # 상태=전달완료 행 → Narrative로 반영
  python3 데이터/tools/draft_tools.py import <draft> <narrative>   # 경로 지정 (테스트용)

대본 4탭 + Barks + NPC — 대본 탭은 게임 대본 파일(script/)과 같은 구획으로 나눈다:
  Bar       바 안 이야기(개점 전·1부 카메오·2부 단골) → script/bar/dayN.json  ※ day 칸으로 일차 구분
  Home      집·테라스                               → script/home.json
  Street    출근길·퇴근길·거리 반복                   → script/street.json
  Cutscene  꿈·인트로 연출                           → script/cutscene.json
  Barks     1부 랜덤 손님 한 마디(성격×상황 풀)        → Narrative Barks
  NPC       반복 조사 반응(시바견·자판기 등)           → Narrative Scenes(group) — PD가 Points 배선
  분량이 커지면 Bar_Day4 처럼 밑줄 탭을 늘려도 된다 — Bar·Home·Street·Cutscene 으로 시작하는 탭은 전부 인식.

표정/동작 규칙 (v2.1): 바·집 계열 장면의 '표정·동작' 칸은 흉상 표정(Expressions),
거리 계열(출근길·퇴근길·거리)은 SD 동작(FieldAnims의 action). 거리엔 흉상이 없다.

사본 2개 원칙: 초안(원고, 기현 소유) ↔ Narrative(최종, PD 소유). 중간 검토 시트는 두지 않는다.
전달 완료된 내용의 수정은 Narrative에서만 — 요청은 노션 코멘트로.
"""
import os
import re
import sys

from openpyxl import Workbook, load_workbook
from openpyxl.comments import Comment
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.worksheet.datavalidation import DataValidation

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.dirname(HERE)
DRAFT_PATH = os.path.join(DATA, "LUNA_Draft.xlsx")
NARR_PATH = os.path.join(DATA, "LUNA_Narrative.xlsx")

SCENE_D = ["day", "장소", "scene", "speaker", "표정·동작", "text", "지문", "상태"]
BARK_D = ["성격", "상황", "대사", "메모", "상태"]
NPC_D = ["NPC", "방식", "순서·조건", "대사", "지문", "상태"]

PLACE_TO_PHASE = {"바": "bar", "바(개점 전)": "bar_open", "집": "home",
                  "출근길": "commute_in", "퇴근길": "commute_out",
                  "거리(공용)": "street", "꿈": "dream", "인트로": "intro"}
# 대본 탭 = script/ 파일 구획과 1:1. (탭명, 색, 장소 후보, 장소 기본값)
SCENE_TABS = [
    ("Bar",      "C98F4E", ["바", "바(개점 전)"],            "바"),
    ("Home",     "B5651D", ["집"],                          "집"),
    ("Street",   "6E8B3D", ["출근길", "퇴근길", "거리(공용)"], "거리(공용)"),
    ("Cutscene", "5B4E8B", ["꿈", "인트로"],                 "꿈"),
]
PLACE_TO_TAB = {p: t for t, _, pl, _ in SCENE_TABS for p in pl}
FIELD_PLACES = {"출근길", "퇴근길", "거리(공용)"}      # 이곳의 '표정·동작' = SD 동작
STATUS = ["작성중", "전달완료"]

SIT = {  # 작가용 한국어 라벨 → Barks situation 코드 (bark_situations 24종 전부, 하루 흐름 순)
    "입장 인사": "call", "입장 재촉": "call_urge", "입장 최후통첩": "call_final",
    "주문 권유(루나)": "ask_order", "주문 고민": "order_think", "주문": "order", "추가 주문": "reorder",
    "잔 받을 때 인사": "serve_thanks",
    "반응 — 최고": "react_excellent", "반응 — 좋음": "react_good", "반응 — 무난": "react_decent",
    "반응 — 별로": "react_poor", "반응 — 하수도": "react_sewage",
    "잘못된 잔 받음": "wrong_receive", "잘못된 잔 마심": "wrong_drink",
    "서빙 재촉": "serve_urge", "서빙 최후통첩": "serve_final",
    "기분 좋게 퇴장": "bye_good", "화나서 퇴장": "bye_bad",
    "이탈 — 주문 못 받아서": "leave_coaster", "이탈 — 술 못 받아서": "leave_serve",
    "잡담·혼잣말": "idle", "취함 (3잔째)": "drunk_enter", "취함 — 구토": "drunk_vomit",
}
MODE = {"볼 때마다 다음": "sequential", "조건에 맞는 것 하나": "conditional",
        "항상 같음": "repeat", "한 번만": "once"}

DOC = {
    ("Scenes", "day"): "몇 일차 이야기인지 (1~13)",
    ("Scenes", "장소"): "장면이 벌어지는 곳 — 드롭다운. 바·집 계열이면 '표정·동작' 칸이 표정, 거리 계열이면 SD 동작이 됩니다",
    ("Scenes", "scene"): "장면 이름. 같은 값의 연속된 줄이 한 장면.\n영문 id를 알면 그대로, 모르면 한국어 가칭 — PD가 id로 바꿔 가져갑니다",
    ("Scenes", "speaker"): "누가 말하는지 — 드롭다운. 비우면 '지문' 줄이 되어 앞 대사의 연출 메모에 붙습니다",
    ("Scenes", "표정·동작"): "바·집 장면 = 흉상 표정(smile, angry…) / 거리 장면 = SD 동작(idle, wave…).\n비우면 기본. 없는 값이면 PD가 가져갈 때 기본으로 바뀝니다",
    ("Scenes", "text"): "대사 본문. 줄바꿈(Alt+Enter) 자유",
    ("Scenes", "지문"): "연출 메모 — '잔을 내려놓으며', '여기서 선택지 갈림: A/B' 같은 것. 게임엔 안 나가고 PD가 배선할 때 참고",
    ("Scenes", "상태"): "작성중 = PD가 안 가져감 / 전달완료 = 가져감.\n⚠ 전달 후 수정은 여기서 하지 말고 노션 코멘트로!",
    ("Barks", "성격"): "누가 할 법한 말인지 — 드롭다운. '공용(누구나)'는 성격 무관 아무나 씀",
    ("Barks", "상황"): "언제 나오는 말인지 — 드롭다운. 같은 성격×상황에 여러 줄 쓰면 게임이 무작위로 하나 고릅니다",
    ("Barks", "대사"): "한 마디. 짧을수록 좋습니다 — 말풍선 한 줄",
    ("Barks", "메모"): "참고 메모 (선택)",
    ("Barks", "상태"): "작성중 / 전달완료 — Scenes 탭과 같은 규칙",
    ("NPC", "NPC"): "누구/무엇인지 — 드롭다운(등장인물) 또는 직접 입력(자판기 같은 사물). 사물이면 루나의 독백으로 처리됩니다",
    ("NPC", "방식"): "반응이 나오는 방식 — 드롭다운.\n볼 때마다 다음 = 조사할 때마다 1→2→3… / 조건에 맞는 것 하나 = 상황에 맞는 반응 하나",
    ("NPC", "순서·조건"): "'볼 때마다 다음'이면 순서 숫자(1, 2, 3…),\n'조건에 맞는 것 하나'면 조건을 말로 (예: 골드 부족하면) — 배선은 PD가",
    ("NPC", "대사"): "반응 대사. 같은 NPC·같은 순서(조건)에 여러 줄 쓰면 한 반응에서 이어서 나옵니다",
    ("NPC", "지문"): "연출 메모 (선택)",
    ("NPC", "상태"): "작성중 / 전달완료 — Scenes 탭과 같은 규칙",
}

BARK_SAMPLE = [
    ("온화형", "반응 — 하수도", "이건… 조금, 힘든 맛이네요…", "※ 실제로 비어 있는 조합 — 이런 걸 채우면 됩니다", "작성중"),
    ("온화형", "반응 — 하수도", "죄송해요, 저… 물 한 잔만 주시겠어요?", "", "작성중"),
    ("거친형", "반응 — 최고", "크— 이거지! 한 잔 더 말아봐!", "", "작성중"),
    ("수다형", "입장 인사", "자리 있죠? 아 다행이다, 오늘 진짜 별일이 다 있었거든요.", "", "작성중"),
    ("과묵형", "주문 고민", "……. (메뉴판을 오래 들여다본다)", "", "작성중"),
    ("예민형", "반응 — 별로", "…주문한 거랑 좀 다른 것 같은데요.", "", "작성중"),
    ("공용(누구나)", "잡담·혼잣말", "(창밖의 네온을 멍하니 본다)", "성격 무관 — 아무나 쓸 수 있는 줄", "작성중"),
]
NPC_SAMPLE = [
    ("시바", "볼 때마다 다음", "1", "멍.", "꼬리를 천천히 흔든다", "작성중"),
    ("시바", "볼 때마다 다음", "2", "…멍?", "고개를 갸웃한다", "작성중"),
    ("시바", "볼 때마다 다음", "3", "(더 할 말이 없다는 눈빛이다)", "마지막 반응은 이후 계속 반복됨", "작성중"),
    ("자판기", "조건에 맞는 것 하나", "골드 10 이상", "(음료수 하나 뽑아 마실까.)", "사물이라 루나 독백 처리", "작성중"),
    ("자판기", "조건에 맞는 것 하나", "골드 부족", "(주머니 사정이… 그냥 가자.)", "", "작성중"),
]


# ─────────────────────────────────────────────── 공용
def load_refs(narr_path):
    """Narrative에서 참조 목록 로드: 이름→id, 캐릭터별 표정, 캐릭터별 동작"""
    wb = load_workbook(narr_path, data_only=True)
    ws = wb["Characters"]
    hdr = [c.value for c in ws[1]]
    i_id, i_ko, i_ex = hdr.index("id"), hdr.index("name_ko"), hdr.index("expressions")
    names, exprs, anims = {}, {}, {}
    for r in ws.iter_rows(min_row=2, values_only=True):
        if not r[i_id]:
            continue
        cid = str(r[i_id]).strip()
        names[str(r[i_ko]).strip()] = cid
        names[cid] = cid
        exprs[cid] = [e.strip() for e in str(r[i_ex] or "").split(";") if e.strip()]
    ws = wb["FieldAnims"]
    for r in ws.iter_rows(min_row=2, values_only=True):
        if r and r[0]:
            anims.setdefault(str(r[0]).strip(), []).append(str(r[1]).strip())
    return names, exprs, anims


def read_tab(wb, name, cols):
    if name not in wb.sheetnames:
        return []
    out = []
    for r in wb[name].iter_rows(min_row=2, values_only=True):
        vals = list(r[:len(cols)]) + [None] * max(0, len(cols) - len(r))
        if all(v is None or str(v).strip() == "" for v in vals):
            continue
        out.append({k: (str(v).strip() if v is not None else "") for k, v in zip(cols, vals)})
    return out


def style_tab(ws, name, cols, color, samples, wide_cols, widths):
    ws.append(cols)
    for idx, h in enumerate(cols):
        c = ws.cell(1, idx + 1)
        c.font = Font(bold=True, color="FFFFFF"); c.fill = PatternFill("solid", fgColor=color)
        doc = DOC.get((name, h))
        if doc:
            c.comment = Comment(doc + "\n\n💡 헤더에 마우스를 올리면 이 설명이 보입니다.", "LUNA 초안 가이드")
            c.comment.width = 340; c.comment.height = 150
    wrap = Alignment(vertical="top", wrap_text=True)
    for row in samples:
        ws.append(list(row))
        for ci in wide_cols:
            ws.cell(ws.max_row, ci).alignment = wrap
    for col, w in widths.items():
        ws.column_dimensions[col].width = w
    ws.freeze_panes = "A2"
    ws.sheet_properties.tabColor = color


# ─────────────────────────────────────────────── init
def scene_samples():
    """실대본(day1~3)에서 대표 장면 7개를 초안 형식으로 역변환 — '내가 쓰면 이렇게 된다' 예시"""
    sys.path.insert(0, HERE)
    import gen_luna_data as G
    PH2KO = {v: k for k, v in PLACE_TO_PHASE.items()}
    NAME = {c[0]: c[1] for c in G.CHARACTERS}
    SC = {s[0]: dict(zip(G.SCENE_COLS, s)) for s in G.SCENES}
    PICK = [
        ("d1_note", "크리스의 쪽지", 99, ""),
        ("d1_port", "포트 첫 잔", 6, "(여기서 진피즈 제조 들어감 — 제조·배선은 PD가 함)"),
        ("d1_elevator", "퇴근길 엘리베이터 라디오", 99, ""),
        ("d2_meet", "출근길 — 삼호와 시바견", 99, ""),
        ("d2_bar_open", "d2_bar_open", 99, "(실제 씬 id를 알면 이렇게 그대로 써도 됨)"),
        ("d3_dream", "꿈 — 습격 2", 99, ""),
        ("d3_samho_death", "골목의 삼호 (사망 목격)", 99,
         "(직전에 선택지 갈림: 부축한다/먼저 간다 — 이런 건 지문에 말로 적으면 PD가 배선)"),
    ]
    by_tab = {t[0]: [] for t in SCENE_TABS}
    for sid, label, lim, tail in PICK:
        sc = SC[sid]; day, place = sc["day"], PH2KO[sc["phase"]]
        rows = by_tab[PLACE_TO_TAB[place]]
        n = 0
        for st in sorted([x for x in G.STEPS if x[0] == sid], key=lambda x: x[1]):
            d = dict(zip(G.STEP_COLS, st))
            if d["type"] != "say" or n >= lim:
                continue
            n += 1
            arg = d["arg"] if d["arg"] not in ("", "default") else ""
            # 거리 장면 첫 줄에 SD 동작 예시를 하나 심는다 (표정·동작 칸의 거리 용법 시연)
            if sid == "d2_meet" and n == 1 and d["actor"] == "samho":
                arg = "idle_blink"
            rows.append([day, place, label, NAME.get(d["actor"], d["actor"]), arg,
                         d["text_ko"], d["note"] or "", "작성중"])
        if tail:
            rows.append([day, place, label, "", "", "", tail, "작성중"])
    return by_tab


def cmd_init():
    if os.path.exists(DRAFT_PATH):
        print(f"❌ 이미 존재: {DRAFT_PATH} — 작가 작업물이 덮일 수 있어 중단합니다. 새로 만들려면 파일을 직접 지우세요.")
        sys.exit(1)
    names, exprs, anims = load_refs(NARR_PATH)
    ko_names = sorted({k for k in names if not re.fullmatch(r"[a-z][a-z0-9_]*", k)})
    sys.path.insert(0, HERE)
    import gen_luna_data as G
    pers_labels = [p[1] for p in G.PERSONALITIES] + ["공용(누구나)", "루나(바텐더)"]

    wb = Workbook(); wb.remove(wb.active)

    info = wb.create_sheet("INFO")
    LINES = [
        ("h1", "L.U.N.A 대사 초안 — 이기현 전용"),
        ("", "이 파일은 기현님의 원고지입니다. 구조·조건·배선은 신경 쓰지 말고 대사만 편하게 쓰면 됩니다."),
        ("", ""),
        ("h2", "대본 탭 4개 = 장소 4곳 (게임 대본 파일과 같은 구획)"),
        ("", "Bar       바 안 이야기 — 개점 전 대화, 1부 카메오, 2부 단골. day 칸으로 몇 일차인지 구분"),
        ("", "Home      집·테라스 이야기"),
        ("", "Street    출근길·퇴근길·거리에서 벌어지는 이야기"),
        ("", "Cutscene  꿈·인트로 같은 연출 장면"),
        ("", "그리고 형태가 다른 2탭 — Barks(1부 랜덤 손님의 한 마디, 한 줄이면 완결), NPC(시바견·자판기 같은 반복 조사 반응)"),
        ("", "→ 헷갈리면: 이야기가 흐르면 장소 탭, 상황에 던지는 한 마디면 Barks, 조사 반응이면 NPC"),
        ("", ""),
        ("h2", "표정과 동작 — 장소에 따라 다릅니다"),
        ("", "바·집 장면: 인물 흉상이 나오므로 '표정·동작' 칸 = 표정 (smile, angry …)"),
        ("", "거리 장면(출퇴근길): 흉상 없이 SD 캐릭터만 있으므로 = 동작 (idle, wave …)"),
        ("", "모르겠으면 비워두세요 — 기본값으로 나가고, 연출은 PD가 다듬습니다."),
        ("", ""),
        ("h2", "규칙 3개"),
        ("", "1. 다 쓴 묶음은 상태를 '전달완료'로 — PD가 그것만 가져갑니다. 전달 후 수정은 노션 코멘트로 요청."),
        ("", "2. 이 파일은 기현님만 수정합니다. PD는 읽기만 합니다."),
        ("", "3. 선택지·조건 분기는 지문 칸에 말로 적어주세요. 배선은 PD가 합니다."),
        ("", ""),
        ("", "각 탭의 헤더(맨 윗줄)에 마우스를 올리면 칸별 설명이 뜹니다. 지금 들어있는 줄들은 전부 예시(작성중)입니다 — 지우고 쓰세요."),
    ]
    for kind, text in LINES:
        info.append([text])
        c = info.cell(row=info.max_row, column=1)
        if kind == "h1":
            c.font = Font(bold=True, size=16, color="FFFFFF"); c.fill = PatternFill("solid", fgColor="8A5A2E")
        elif kind == "h2":
            c.font = Font(bold=True, size=12, color="4A2C10"); c.fill = PatternFill("solid", fgColor="F5E6D3")
    info.column_dimensions["A"].width = 110
    info.sheet_view.showGridLines = False

    # 대본 탭 4개 — script/ 파일 구획(bar·home·street·cutscene)과 1:1
    samples_by_tab = scene_samples()
    for tab, color, places, _default in SCENE_TABS:
        ws = wb.create_sheet(tab)
        style_tab(ws, "Scenes", SCENE_D, color, samples_by_tab.get(tab, []), (6, 7),
                  {"A": 6, "B": 12, "C": 22, "D": 12, "E": 12, "F": 80, "G": 34, "H": 10})
        dv_sp = DataValidation(type="list", formula1='"' + ",".join(ko_names) + '"', allow_blank=True, showErrorMessage=False)
        dv_pl = DataValidation(type="list", formula1='"' + ",".join(places) + '"', allow_blank=True)
        dv_st = DataValidation(type="list", formula1='"' + ",".join(STATUS) + '"', allow_blank=True)
        for dv in (dv_sp, dv_pl, dv_st): ws.add_data_validation(dv)
        dv_pl.add("B2:B3000"); dv_sp.add("D2:D3000"); dv_st.add("H2:H3000")

    # Barks 탭
    ws = wb.create_sheet("Barks")
    style_tab(ws, "Barks", BARK_D, "5A7D2E", BARK_SAMPLE, (3,),
              {"A": 14, "B": 18, "C": 66, "D": 34, "E": 10})
    dv_pe = DataValidation(type="list", formula1='"' + ",".join(pers_labels) + '"', allow_blank=True)
    dv_si = DataValidation(type="list", formula1='"' + ",".join(SIT) + '"', allow_blank=True)
    dv_s2 = DataValidation(type="list", formula1='"' + ",".join(STATUS) + '"', allow_blank=True)
    for dv in (dv_pe, dv_si, dv_s2): ws.add_data_validation(dv)
    dv_pe.add("A2:A3000"); dv_si.add("B2:B3000"); dv_s2.add("E2:E3000")

    # NPC 탭
    ws = wb.create_sheet("NPC")
    style_tab(ws, "NPC", NPC_D, "2E6E7D", NPC_SAMPLE, (4, 5),
              {"A": 12, "B": 20, "C": 16, "D": 60, "E": 30, "F": 10})
    dv_np = DataValidation(type="list", formula1='"' + ",".join(ko_names) + '"', allow_blank=True, showErrorMessage=False)
    dv_md = DataValidation(type="list", formula1='"' + ",".join(MODE) + '"', allow_blank=True)
    dv_s3 = DataValidation(type="list", formula1='"' + ",".join(STATUS) + '"', allow_blank=True)
    for dv in (dv_np, dv_md, dv_s3): ws.add_data_validation(dv)
    dv_np.add("A2:A3000"); dv_md.add("B2:B3000"); dv_s3.add("F2:F3000")

    wb.save(DRAFT_PATH)
    n_scene = sum(len(v) for v in samples_by_tab.values())
    print(f"✅ 초안 워크북 생성(대본 4탭 + Barks + NPC): {DRAFT_PATH}")
    print(f"   대본 예시 {n_scene}줄(day1~3 실대본, Bar·Home·Street·Cutscene 분배) · Barks 예시 {len(BARK_SAMPLE)}줄 · NPC 예시 {len(NPC_SAMPLE)}줄 — 전부 작성중")


# ─────────────────────────────────────────────── import
def import_scenes(rows, nwb, names, exprs, anims):
    ws_sc, ws_st = nwb["Scenes"], nwb["Steps"]
    exist_scenes, exist_titles = set(), set()
    for r in ws_sc.iter_rows(min_row=2, values_only=True):
        if r and r[0]:
            exist_scenes.add(str(r[0]).strip())
            exist_titles.add((r[1], str(r[6] or "").strip()))
    groups, order = {}, []
    for r in rows:
        key = (r["day"], r["scene"])
        if key not in groups:
            groups[key] = []; order.append(key)
        groups[key].append(r)
    used_ids = set(exist_scenes)
    report, skipped, n_steps = [], [], 0
    for day, scene in order:
        lines = groups[(day, scene)]
        if re.fullmatch(r"[a-z][a-z0-9_]*", scene):
            sid, title = scene, scene
        else:
            base = f"d{day or 'x'}_draft"; n = 1
            while f"{base}{n}" in used_ids: n += 1
            sid, title = f"{base}{n}", scene
        place = lines[0]["장소"]
        phase = PLACE_TO_PHASE.get(place, "bar")
        day_n = int(day) if str(day).isdigit() else 0
        if sid in exist_scenes or (day_n, title) in exist_titles:
            skipped.append(f"{title} (이미 Narrative에 있음 — 초안 수정은 반영 안 됨, Narrative에서 직접)")
            continue
        used_ids.add(sid); exist_titles.add((day_n, title))
        ws_sc.append([sid, day_n, phase, 99, "manual", "", title, "TRUE", ""])
        seq, warns = 0, []
        for ln in lines:
            if not ln["speaker"]:
                memo = " / ".join(x for x in (ln["text"], ln["지문"]) if x)
                if seq and memo:
                    prev = ws_st.cell(ws_st.max_row, 11)
                    prev.value = (str(prev.value) + " / " if prev.value else "") + memo
                continue
            actor = names.get(ln["speaker"])
            if not actor:
                warns.append(f"발화자 '{ln['speaker']}' 미등록 — 행 건너뜀"); continue
            arg = ln["표정·동작"]
            if arg:   # 장소에 따라 표정(흉상) / 동작(SD) 검증이 갈린다
                pool = anims.get(actor, []) if place in FIELD_PLACES else exprs.get(actor, [])
                kind = "동작" if place in FIELD_PLACES else "표정"
                if arg not in pool:
                    warns.append(f"{kind} '{arg}'는 {actor}에게 없음 — 기본으로"); arg = ""
            seq += 1; n_steps += 1
            dialogue_id = f"dlg_{sid}_{seq:03d}"
            ws_st.append([sid, seq, "say", actor, arg, ln["text"], "", "", "", "", ln["지문"], dialogue_id])
        report.append((sid, title, phase, day_n, seq, warns))
    return report, skipped, n_steps


def import_barks(rows, nwb, pers_map):
    ws = nwb["Barks"]
    exist = {(str(r[0] or "").strip(), str(r[2] or "").strip(), str(r[3] or "").strip())
             for r in ws.iter_rows(min_row=2, values_only=True) if r and r[3]}
    added, skipped, warns = 0, 0, []
    for r in rows:
        voice = pers_map.get(r["성격"])
        if voice is None:
            warns.append(f"성격 '{r['성격']}' 미등록 — 행 건너뜀"); continue
        sit = SIT.get(r["상황"])
        if not sit:
            warns.append(f"상황 '{r['상황']}' 미등록 — 행 건너뜀"); continue
        if (voice, sit, r["대사"]) in exist:
            skipped += 1; continue
        ws.append([voice, "", sit, r["대사"], "", 1])
        exist.add((voice, sit, r["대사"])); added += 1
    return added, skipped, warns


def import_npc(rows, nwb, names):
    ws_sc, ws_st = nwb["Scenes"], nwb["Steps"]
    exist_titles, exist_ids, grp_seq = set(), set(), {}
    for r in ws_sc.iter_rows(min_row=2, values_only=True):
        if not (r and r[0]):
            continue
        exist_titles.add((r[1], str(r[6] or "").strip()))
        exist_ids.add(str(r[0]).strip())
        if r[8]:   # 그룹 내 seq는 고유해야 함 — 기존 그룹의 최대 seq 뒤에 이어붙인다
            grp_seq[str(r[8]).strip()] = max(grp_seq.get(str(r[8]).strip(), 0), int(r[3] or 0))
    groups, order = {}, []
    for r in rows:
        key = (r["NPC"], r["순서·조건"])
        if key not in groups:
            groups[key] = []; order.append(key)
        groups[key].append(r)
    # 사물(캐릭터 아님)은 id에 한글을 못 쓰므로 obj1, obj2… 슬러그 부여 (같은 사물 = 같은 슬러그)
    obj_slug, obj_n = {}, 0
    for npc, _ in order:
        if not names.get(npc) and npc not in obj_slug:
            obj_n += 1; obj_slug[npc] = f"obj{obj_n}"
    report, skipped = [], []
    for npc, cond in order:
        lines = groups[(npc, cond)]
        cid = names.get(npc)                     # 사물(자판기 등)이면 루나 독백 처리
        gslug = cid or obj_slug[npc]
        base = f"np_{gslug}_draft"; n = 1
        while f"{base}{n}" in exist_ids: n += 1
        sid = f"{base}{n}"
        title = f"{npc} 반응 — {cond or lines[0]['방식']}"
        if (0, title) in exist_titles:
            skipped.append(f"{title} (이미 반영됨)"); continue
        exist_ids.add(sid); exist_titles.add((0, title))
        grp = f"np_{gslug}"
        grp_seq[grp] = grp_seq.get(grp, 0) + 1
        ws_sc.append([sid, 0, "street", grp_seq[grp], "manual", "", title, "TRUE", grp])
        seq = 0
        for ln in lines:
            seq += 1
            dialogue_id = f"dlg_{sid}_{seq:03d}"
            ws_st.append([sid, seq, "say", cid or "luna", "", ln["대사"], "", "", "", "", ln["지문"], dialogue_id])
        sel = MODE.get(lines[0]["방식"], "?")
        report.append((sid, title, seq, sel, cid is None))
    return report, skipped


def cmd_import(draft_path=DRAFT_PATH, narr_path=NARR_PATH):
    names, exprs, anims = load_refs(narr_path)
    sys.path.insert(0, HERE)
    import gen_luna_data as G
    pers_map = {p[1]: p[0] for p in G.PERSONALITIES}
    pers_map["공용(누구나)"] = ""
    pers_map["루나(바텐더)"] = "luna"

    dwb = load_workbook(draft_path, data_only=True)
    sc_rows = []
    tab_default = {t[0]: t[3] for t in SCENE_TABS}
    for sheet in dwb.sheetnames:
        base = sheet.split("_")[0]
        if sheet == "Scenes":          # 구버전 통짜 탭도 계속 인식
            base = None
        elif base not in tab_default:
            continue
        for r in read_tab(dwb, sheet, SCENE_D):
            if r["상태"] != "전달완료":
                continue
            if not r["장소"] and base:
                r["장소"] = tab_default[base]
            sc_rows.append(r)
    bk_rows = [r for r in read_tab(dwb, "Barks", BARK_D) if r["상태"] == "전달완료"]
    np_rows = [r for r in read_tab(dwb, "NPC", NPC_D) if r["상태"] == "전달완료"]
    if not (sc_rows or bk_rows or np_rows):
        print("가져올 행 없음 — 상태가 '전달완료'인 줄이 없습니다."); return

    nwb = load_workbook(narr_path)
    total = []
    if sc_rows:
        rep, skip, n = import_scenes(sc_rows, nwb, names, exprs, anims)
        total.append(("Scenes", rep, skip, n))
    if bk_rows:
        added, skipped, warns = import_barks(bk_rows, nwb, pers_map)
        total.append(("Barks", added, skipped, warns))
    if np_rows:
        rep, skip = import_npc(np_rows, nwb, names)
        total.append(("NPC", rep, skip, None))
    nwb.save(narr_path)

    print(f"===== 초안 반영 → {os.path.basename(narr_path)} =====")
    for kind, a, b, c in total:
        if kind == "Scenes":
            print(f"[Scenes] 씬 {len(a)}개 · 대사 {c}줄")
            for sid, title, phase, day_n, seq, warns in a:
                tag = f" ('{title}' 가칭→자동 id)" if sid != title else ""
                print(f"  · {sid}{tag} — day{day_n} {phase} {seq}줄, trigger=manual")
                for w in warns: print(f"      ⚠ {w}")
            for s in b: print(f"  · 건너뜀: {s}")
        elif kind == "Barks":
            print(f"[Barks] 추가 {a}줄" + (f" · 중복 스킵 {b}" if b else ""))
            for w in c: print(f"      ⚠ {w}")
        elif kind == "NPC":
            print(f"[NPC] 그룹 씬 {len(a)}개")
            for sid, title, seq, sel, is_obj in a:
                extra = " (사물→루나 독백)" if is_obj else ""
                print(f"  · {sid} '{title}' {seq}줄, group·manual{extra} — InteractPoints selection={sel} 배선 필요")
            for s in b: print(f"  · 건너뜀: {s}")
    print("다음 할 일(PD): ① Scenes seq·trigger·when 배선 ② NPC는 InteractPoints 연결 ③ build.py로 검증")


if __name__ == "__main__":
    args = sys.argv[1:]
    if args[:1] == ["init"]:
        cmd_init()
    elif args[:1] == ["import"]:
        cmd_import(*args[1:3]) if len(args) > 1 else cmd_import()
    else:
        print(__doc__)
