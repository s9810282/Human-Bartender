#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
L.U.N.A 정식 빌드 — 저작 xlsx 2파일 → 검증 → json/(도메인별 분할 배포)

  사용:  python3 데이터/tools/build.py                # 기본: LUNA_System.xlsx + LUNA_Narrative.xlsx
         python3 데이터/tools/build.py <xlsx…>        # 다른 파일(들) 지정
         python3 데이터/tools/build.py --strict       # 영어 누락 = 에러 (번역 완료 후 사용)

  v2.0 확정: 저작은 '수정 권한' 축으로 2파일 분리 (xlsx는 git 머지 불가 → 파일 1개 = 소유자 1명)
    LUNA_System.xlsx    — 제조·운영: 칵테일/선반/성격/취향/밸런스/RandomWaves
    LUNA_Narrative.xlsx — 대사·서사: 대본/Barks/단골/퀘스트/연출/캐스트/RegularSlots
  규칙: 한 시트는 정확히 한 파일에만 존재한다. 두 파일에 같은 시트가 있으면 빌드 에러
        (참고용 복사가 병합돼 유령 데이터가 되는 사고 차단).
  배포는 구엔진처럼 도메인별 JSON 분할. 검증 실패 시 JSON을 내보내지 않는다.
  gen_luna_data.py는 "코드 시드 → 시트 재생성" 전용. 팀 저작 시작 후엔 build.py만 사용(gen은 시트를 덮는다).
"""
import os
import sys
from openpyxl import load_workbook

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_luna_data as G

# Config 값 타입은 시트의 type 컬럼이 정본 (v2.2) — 엑셀이 3.0을 3으로 뭉개도 되돌린다.
# 시드 키 기억 방식(SEED_FLOAT_CONFIG)은 신규 키를 못 지켜서 폐지.
def _config_cast(v, t):
    if t == "float" and isinstance(v, int) and not isinstance(v, bool): return float(v)
    if t == "int" and isinstance(v, float) and v == int(v): return int(v)
    if t == "bool" and isinstance(v, str): return v.upper() == "TRUE"
    if t == "str": return "" if v is None else str(v)
    return v

# 컬럼 타입 명세 — 엑셀 왕복 시 타입이 뭉개지는 컬럼만 명시 (그 외는 그대로)
NULLABLE = {   # 빈 칸을 ""가 아니라 None(널)로
    "Cocktails": {"glass", "fill", "garnish", "unlock_day_override", "tier_override"},
    "ShelfItems": {"shop_price", "category", "sprite"},
    "Characters": {"alive_flag", "enter_sfx", "exit_sfx"},
}
BOOL_COLS = {  # TRUE/FALSE 문자열 → 불리언
    "Scenes": {"skippable"},
    "RegularSlots": {"branch_choice", "must_serve"},
    "RandomWaves": {"branch_choice"},
    "Characters": {"affinity"},
}
FLOAT_COLS = { # 8.0이 8로 읽히는 문제 → float 강제
    "Cocktails": {"abv"},
    "GradePayout": {"revenue_mult"},
    "Personalities": {"tip_mult", "patience_mult", "think_chance"},
}

# 시트 → (전역 이름, 읽을 컬럼 수). Cocktails의 (파생) 컬럼과 Characters의 base_body는 별도 처리.
SHEET_SPEC = {
    "Cocktails": ("COCKTAILS", len(G.CK_COLS)),
    "RecipeLines": (None, 6),
    "ShelfItems": ("SHELF_ITEMS", len(G.SHELF_COLS)),
    "Characters": (None, len(G.CHAR_COLS) + 1),
    "Expressions": ("EXPRESSIONS", len(G.EXPR_COLS)),
    "ExpressionParts": ("EXPRESSION_PARTS", len(G.EXPRPART_COLS)),
    "Cutscenes": ("CUTSCENES", len(G.CUT_COLS)),
    "ResourceMap": ("RESOURCE_MAP", len(G.RESMAP_COLS)),
    "FieldAnims": ("FIELD_ANIMS", len(G.FIELD_COLS)),
    "Personalities": ("PERSONALITIES", len(G.PERS_COLS)),
    "GuestBodies": ("GUEST_BODIES", len(G.GBODY_COLS)),
    "Barks": ("BARKS", len(G.BARK_COLS)),
    "Tastes": ("TASTES", len(G.TASTE_COLS)),
    "Dossier": ("DOSSIER", len(G.DOSSIER_COLS)),
    "Days": ("DAYS", len(G.DAY_COLS)),
    "RandomWaves": ("RANDOM_WAVES", len(G.WAVE_COLS)),
    "RegularSlots": ("REGULAR_SLOTS", len(G.RSLOT_COLS)),
    "Spots": ("SPOTS", len(G.SPOT_COLS)),
    "InteractPoints": ("POINTS", len(G.POINT_COLS)),
    "Scenes": ("SCENES", len(G.SCENE_COLS)),
    "Steps": ("STEPS", len(G.STEP_COLS)),
    "Choices": ("CHOICES", len(G.CHOICE_COLS)),
    "OrderRules": ("ORDERS", len(G.ORDER_COLS)),
    "Quests": ("QUESTS", len(G.QUEST_COLS)),
    "QuestStages": ("QUEST_STAGES", len(G.QSTAGE_COLS)),
    "Endings": ("ENDINGS", len(G.END_COLS)),
    "Config": ("CONFIG", len(G.CONFIG_COLS)),
    "GradeCuts": ("GRADE_CUTS", 2),
    "GradePayout": ("GRADE_PAYOUT", 2),
    "AffinityMatrix": ("AFFINITY_MATRIX", 6),
    "UIStrings": ("UI_STRINGS", len(G.UI_COLS)),
    "TextTags": ("TEXT_TAGS", len(G.TEXTTAG_COLS)),
    "BarkSituations": ("BARK_SITUATIONS", len(G.BARKSIT_COLS)),
}
REQUIRED = set(SHEET_SPEC)


def rows_of(ws, ncols, sheet):
    """시트 → 튜플 리스트. 완전 빈 행은 스킵, 빈 셀은 ""(NULLABLE 컬럼만 None)."""
    nul = NULLABLE.get(sheet, set())
    bools = BOOL_COLS.get(sheet, set())
    floats = FLOAT_COLS.get(sheet, set())
    hdr = [c.value for c in ws[1][:ncols]]
    out = []
    for r in ws.iter_rows(min_row=2, values_only=True):
        vals = r[:ncols]
        if all(v is None or v == "" for v in vals):
            continue
        row = []
        for i in range(ncols):
            v = vals[i] if i < len(vals) else None
            col = hdr[i]
            if v is None:
                v = None if col in nul else ""
            elif isinstance(v, str):
                v = v.strip()
                if col in bools:
                    v = v.upper() == "TRUE"
            if col in bools and isinstance(v, (int, float)) and not isinstance(v, bool):
                v = bool(v)
            if col in floats and isinstance(v, int) and not isinstance(v, bool):
                v = float(v)
            row.append(v)
        out.append(tuple(row))
    return out


def collect(paths):
    """파일들을 읽어 시트를 수집. v2.0: 한 시트는 한 파일에만 — 중복이면 dup에 기록(빌드 에러).
    참고용으로 남의 시트를 복사해 두면 행이 조용히 섞여 '유령 데이터'가 되므로 병합하지 않는다."""
    tables, origin, unknown, dup = {}, {}, [], []
    for p in paths:
        wb = load_workbook(p, data_only=True)
        base = os.path.basename(p)
        for name in wb.sheetnames:
            if name == "INFO":
                continue
            if name not in SHEET_SPEC:
                unknown.append(f"{base}:{name}")
                continue
            if name in origin:
                dup.append(f"{name} ({origin[name]} ↔ {base})")
                continue
            origin[name] = base
            tables[name] = rows_of(wb[name], SHEET_SPEC[name][1], name)
    return tables, origin, unknown, dup


def load_into_globals(tables):
    # 특수 1 — RecipeLines → RECIPES 딕셔너리 (레시피 없는 칵테일도 빈 키 유지)
    G.COCKTAILS = tables["Cocktails"]
    rec = {}
    for cid, seq, action, ing, qty, unit in sorted(tables["RecipeLines"], key=lambda x: (str(x[0]), x[1])):
        rec.setdefault(cid, []).append((action, ing, qty, unit))
    for c in G.COCKTAILS:
        rec.setdefault(c[0], [])
    G.RECIPES = rec
    # 특수 2 — Characters: 마지막 base_body 컬럼 → BASE_BODY
    ch = tables["Characters"]
    G.CHARACTERS = [tuple(r[:len(G.CHAR_COLS)]) for r in ch]
    G.BASE_BODY = {r[0]: r[len(G.CHAR_COLS)] for r in ch if r[len(G.CHAR_COLS)]}
    # 나머지 1:1
    for name, (gname, _) in SHEET_SPEC.items():
        if gname:
            setattr(G, gname, tables[name])
    # 특수 3 — Config 값 타입 복원: 시트의 type 컬럼대로 (4컬럼 → 내부 3튜플)
    G.CONFIG = [(k, _config_cast(v, t), n) for k, v, t, n in G.CONFIG]


def main():
    args = sys.argv[1:]
    if "--strict" in args:
        G.STRICT_L10N = True   # 영어 누락을 에러로 승격 (8월 번역 완료 후)
        args.remove("--strict")
    paths = args if args else [os.path.join(G.OUT, f) for f in ("LUNA_System.xlsx", "LUNA_Narrative.xlsx")]
    src = " + ".join(os.path.basename(p) for p in paths)
    for p in paths:
        if not os.path.exists(p):
            print(f"❌ 시트 파일 없음: {p}"); sys.exit(1)

    tables, origin, unknown, dup = collect(paths)
    if dup:
        print("❌ 같은 시트가 두 파일에 존재 — 한 시트는 한 파일에만 있어야 합니다 (참고용 복사 금지):")
        for d in dup:
            print(f"   · {d}")
        sys.exit(1)
    missing = REQUIRED - set(origin)
    if missing:
        print(f"❌ 시트 누락: {', '.join(sorted(missing))}"); sys.exit(1)

    load_into_globals(tables)
    derived = G.derive()
    errors, report = G.validate(derived)

    lines = [f"===== L.U.N.A 시트 빌드 리포트 (build.py · 원본: {src}) ====="]
    if unknown:
        lines.append(f"⚠ 알 수 없는 시트(무시됨): {', '.join(unknown)}")
    by_file = {}   # 파일별 시트·행수 — "남의 파일 안 당겨오고 빌드" 사고를 눈으로 잡는 용도
    for name, base in origin.items():
        by_file.setdefault(base, []).append(f"{name}({len(tables[name])})")
    for base, items in sorted(by_file.items()):
        lines.append(f"· {base} — 시트 {len(items)}개: " + ", ".join(sorted(items)))
    lines += report + [""]
    if errors:
        lines.append(f"❌ 오류 {len(errors)}건 — JSON 미출력:")
        lines += ["  " + e for e in errors]
    else:
        lines.append("✅ 검증 통과 — 참조 무결성 · 파생 규칙 · DSL 문법 · 플래그 · L10N · 루나 대사 규칙")
    lines.append(f"칵테일 {len(G.COCKTAILS)} / 선반 {len(G.SHELF_ITEMS)} / 캐릭터 {len(G.CHARACTERS)} / "
                 f"씬 {len(G.SCENES)} / 스텝 {len(G.STEPS)} / 선택지 {len(G.CHOICES)} / "
                 f"웨이브 {len(G.RANDOM_WAVES)} / 단골슬롯 {len(G.REGULAR_SLOTS)}")
    print("\n".join(lines))
    with open(os.path.join(G.OUT, "빌드리포트.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    if errors:
        sys.exit(1)
    G.emit_json(derived)
    print("→ json/ 출력 완료 (시트가 원본입니다 — xlsx는 재생성하지 않음)")


if __name__ == "__main__":
    main()
