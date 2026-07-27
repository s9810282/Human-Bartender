# -*- coding: utf-8 -*-
"""
구엔진 실대본(바 2부) — converted/dayN_bar.py 병합 + 정규화 (26.07.18 이식)

  원본: 프로젝트 정보/…/StreamingAssets/day0·1·2.json (+dayN_crafts.json)
  넘버링: 구 day0→새 day1 / day1→새 day2 / day2→새 day3. 바(2부) 페이즈만 이식 —
  출퇴근·집·꿈·거리 오브젝트·1부 슬롯은 가안 유지(PD 확정).

  여기서 하는 정규화:
  - 씬 7필드 → 9필드 (skippable=False, group="")
  - 스텝 10필드 → 11필드 (sync="" 삽입)
  - 구 판정 어휘 → 신 등급 축 (perfect→excellent / normal→decent / bad→sewage / miss→poor)
  - 구 컷씬 id → Cutscenes 매니페스트 id 매핑
"""
import os, re, sys

_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "converted")

# 구 등급 어휘 → 신 GradeCuts 축 (excellent/good/decent/poor/sewage)
_GRADE_MAP = {"perfect": "excellent", "normal": "decent", "bad": "sewage", "miss": "poor"}
# 구 컷씬 id → 신 Cutscenes 매니페스트 id
_CUT_MAP = {
    "Intro_lab_Sprite": "tl_intro_lab",
    "day1_finish_default": "tl_finished_martini",
    "day1_bubi_fetch": "tl_bubi_meet",
    "day1_bubi_drink_reaction": "tl_catmilk",
}


def _fix_when(w):
    if not w: return w
    return re.sub(r"\b(perfect|normal|bad|miss)\b", lambda m: _GRADE_MAP[m.group(1)], w)


def _load(fname):
    ns = {}
    with open(os.path.join(_DIR, fname), encoding="utf-8") as f:
        exec(compile(f.read(), fname, "exec"), ns)
    return ns


BAR_SCENES, BAR_STEPS, BAR_CHOICES, CONFLICTS = [], [], [], []
for _f in ("day1_bar.py", "day2_bar.py", "day3_bar.py"):
    _ns = _load(_f)
    for s in _ns["SCENES"]:                       # 7필드 → 9필드
        BAR_SCENES.append(tuple(list(s) + [False, ""]))
    for st in _ns["STEPS"]:                       # 10필드 → 11필드 (sync 삽입) + 정규화
        st = list(st) + [""] * (10 - len(st))
        st[4] = _CUT_MAP.get(st[4], st[4]) if st[2] in ("timeline", "gif") else st[4]
        st[7] = _fix_when(st[7])
        BAR_STEPS.append(tuple(st[:9] + [""] + [st[9]]))
    for c in _ns["CHOICES"]:
        c = list(c)
        c[4] = _fix_when(c[4])
        BAR_CHOICES.append(tuple(c))
    CONFLICTS.extend(f"[{_f}] {c}" for c in _ns["CONFLICTS"])

# 이 씬들의 가안(내가 창작한 임시 대본)을 실대본으로 대체한다
REPLACED_SCENES = {
    "d1_tutorial",                                        # → d1_tutorial_chris/_wrap
    "d2_port_chris", "d2_shiba", "d2_chris_return",       # → d2_open/…/d2_close
    "d3_aili_bubi", "d3_samho",                           # → d3_chris_open/…/d3_chris_close
}
# 대체된 씬에서만 쓰이던 선택지 그룹 (ch_d3_deal은 벌꿀 퀘스트 데모 — 재배치 대기로 함께 보류)
REPLACED_CHOICES = {"ch_d3_cat", "ch_d3_samho", "ch_d3_deal"}
