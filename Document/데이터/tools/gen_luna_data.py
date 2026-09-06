#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Project L.U.N.A — Day 1~3 가안 데이터 생성기 v2 (build.py 프로토타입)
시트(LUNA_Data.xlsx)와 배포 JSON을 동일 원천에서 동시 출력 + 검증 리포트
설계 근거: 산출물/LUNA_데이터구조_설계서_v1.0.md (v1.1 개정 반영)

v2 변경 (26.07.17 피드백):
 - L10N: 모든 노출 텍스트 ko/en 쌍 (UIStrings 시트 신설)
 - 재료 조건부 해금(unlock_when) + 칵테일 자체 해금(unlock_day_override/unlock_when)
 - 입고(stock_in) 페이즈: 출근길 직후·바 오픈 시점으로 이동
 - 1부 스토리 카메오: GuestSlots.character + cameo_scene
 - 루나 대사 규칙: craft/serve/choice 직전 루나 say 금지 (검증기 에러)
 - 시간대: 출근 19:00 / 퇴근 02:00, 배경은 밤 고정 단일 리소스
"""
import json, os, re, sys
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.comments import Comment

OUT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # tools/의 부모 = 데이터 폴더 (위치 무관)

# ============================================================
# 1. 마스터 — Ingredients
# ============================================================
SHELF_BASE_COLS = ["id","kind","name_ko","name_en","category","color","sprite",
                   "unlock_day","unlock_when","shop_price","desc_ko","desc_en"]
SHELF_COLS = SHELF_BASE_COLS + ["default_action","prep_action","default_target_qty","default_target_unit","shelf_group","liquid_alpha"]
# v1.9: 재료(Ingredients)와 잔·도구·가니시(Items)를 한 테이블로 통합.
#   데모 제조 준비는 "잔 선반 → 도구 선반 → 재료 선반" 순서다. 가니시 데이터는 정식 버전용으로
#   보존하되 데모 화면과 판정에서는 사용하지 않는다. kind는 항목 역할, shelf_group은 재료 화면 위치를 정한다.
#   category는 재료일 때만 의미(어떤 기믹인지), sprite는 잔·도구·가니시용.
#   unlock_day/unlock_when/shop_price는 전 kind 공통 — 가니시도 입고·해금 대상이 될 수 있다.
SHELF_ITEMS = [
    # ── 재료 (소모품 — 수량·해금·상점가를 가진다) ──
    # 해금일 원칙(제조 개편): 재료 unlock_day = 그 재료를 쓰는 칵테일 unlock_day의 최솟값.
    # 설탕·벌꿀 원액은 제거 — 파우더 기믹 데모 제외 + bees_knees·꿀 퀘스트 삭제.
    ("gin"            , "ingredient", "진"            , "Gin"                     , "base"     , "230,235,240", ""              , 1 , ""                       , None, "주니퍼베리 향의 증류주. 칵테일의 기본기."        , "A juniper-scented spirit. The foundation of cocktails."),
    ("beer"           , "ingredient", "맥주"           , "Beer"                    , "wine_beer", "202,162,4"  , ""              , 2 , ""                       , None, "차갑게. 그게 전부이자 진리."               , "Serve it cold. That's the whole truth."),
    ("red_wine"       , "ingredient", "레드와인"         , "Red Wine"                , "wine_beer", "98,15,11"   , ""              , 2 , ""                       , None, "잔에 따르는 순간부터 분위기가 달라진다."         , "The mood changes the moment it hits the glass."),
    ("champagne"      , "ingredient", "샴페인"          , "Champagne"               , "wine_beer", "255,245,225", ""              , 2 , ""                       , None, "축하할 일이 없어도 축하하게 만드는 술."         , "Makes you celebrate even with nothing to celebrate."),
    ("soda_water"     , "ingredient", "탄산수"          , "Carbonated Water"        , "mixer"    , "245,250,255", ""              , 1 , ""                       , None, "맑고 톡 쏘는 탄산수. 진토닉과 진피즈에 공통으로 사용한다.", "Clear, sparkling carbonated water used in both Gin & Tonic and Gin Fizz."),
    # 설탕 — 데모는 자동 투입(powder_demo_behavior)이라 선반 조작은 없지만, 레시피 라인이 참조하는 재료 데이터는 필요
    ("sugar"          , "ingredient", "설탕"            , "Sugar"                   , "powder"   , "250,250,245", ""              , 1 , ""                       , None, "달콤함의 기본값. 데모에선 손대지 않아도 알아서 들어간다.", "Sweetness by default. In the demo it adds itself."),
    ("lemon"          , "ingredient", "레몬"           , "Lemon"                   , "fruit"    , ""           , ""              , 1 , ""                       , 20  , "스퀴즈용. 새콤함의 표준."                 , "For squeezing. The standard of sour."),
    ("tequila"        , "ingredient", "데킬라"          , "Tequila"                 , "base"     , "240,235,220", ""              , 2 , ""                       , None, "아가베의 태양을 병에 담은 것."              , "The agave sun, bottled."),
    ("vodka"          , "ingredient", "보드카"          , "Vodka"                   , "base"     , "240,245,250", ""              , 2 , ""                       , None, "무색무취. 그래서 어디에나 스며든다."           , "Colorless and odorless — that's how it blends in anywhere."),
    ("rum"            , "ingredient", "럼"            , "Rum"                     , "base"     , "180,120,60" , ""              , 2 , ""                       , None, "사탕수수와 항해의 술."                   , "The spirit of sugarcane and long voyages."),
    ("orange_juice"   , "ingredient", "오렌지주스"        , "Orange Juice"            , "juice"    , "253,180,60" , ""              , 2 , ""                       , 25  , "아침의 맛. 새벽의 바에서는 위장약."           , "The taste of morning. At a bar past midnight, it's medicine."),
    ("ginger_ale"     , "ingredient", "진저에일"         , "Ginger Ale"              , "mixer"    , "230,200,120", ""              , 2 , ""                       , 25  , "알싸한 생강 탄산."                     , "Spicy ginger fizz."),
    ("cola"           , "ingredient", "콜라"           , "Cola"                    , "mixer"    , "60,30,20"   , ""              , 2 , ""                       , 25  , "무엇을 섞어도 콜라 맛이 이긴다. 그게 무기다."     , "Mix in anything — cola wins. That's its weapon."),
    ("lime"           , "ingredient", "라임"           , "Lime"                    , "fruit"    , ""           , ""              , 2 , ""                       , 20  , "스퀴즈용. 레몬보다 한 톤 낮은 산미."          , "For squeezing. One tone lower than lemon."),
    ("whiskey"        , "ingredient", "위스키"          , "Whiskey"                 , "base"     , "190,120,40" , ""              , 3 , ""                       , None, "오크통에서 잠들었다 깨어난 시간."             , "Time that slept in an oak barrel and woke up."),
    ("dry_vermouth"   , "ingredient", "드라이 버무스"      , "Dry Vermouth"            , "liqueur"  , "220,220,190", ""              , 3 , ""                       , None, "마티니를 마티니로 만드는 한 방울."            , "The one drop that makes a martini a martini."),
    ("grenadine"      , "ingredient", "그레나딘"         , "Grenadine"               , "syrup"    , "180,30,50"  , ""              , 3 , ""                       , None, "석류빛 붉은 시럽. 노을 담당."              , "Pomegranate-red syrup. In charge of sunsets."),
    ("sour_mix"       , "ingredient", "사워믹스"         , "Sour Mix"                , "mixer"    , "230,220,160", ""              , 3 , ""                       , 25  , "새콤한 필업 베이스."                    , "A tangy fill-up base."),
    ("cointreau"      , "ingredient", "쿠앵트로"         , "Cointreau"               , "liqueur"  , "235,200,140", ""              , 4 , ""                       , None, "오렌지 리큐르의 귀족."                   , "The aristocrat of orange liqueurs."),
    ("cranberry_juice", "ingredient", "크랜베리주스"       , "Cranberry Juice"         , "juice"    , "170,30,60"  , ""              , 4 , ""                       , 25  , "붉고 떫고 세련된 맛."                   , "Red, tart, and sophisticated."),
    ("triple_sec"     , "ingredient", "트리플섹"         , "Triple Sec"              , "liqueur"  , "235,225,200", ""              , 5 , ""                       , None, "오렌지 리큐르의 실용주의자."                , "The pragmatist of orange liqueurs."),
    ("kahlua"         , "ingredient", "칼루아"          , "Kahlua"                  , "liqueur"  , "60,35,25"   , ""              , 5 , ""                       , None, "커피를 술로 번역한 것."                  , "Coffee, translated into liquor."),
    ("milk"           , "ingredient", "우유"           , "Milk"                    , "dairy"    , "245,245,240", ""              , 5 , ""                       , 15  , "고양이와 초보 손님의 선택."                , "The choice of cats and cautious customers."),
    # ── 제조 개편 신규 재료 14종 (day6+; 스프라이트 발주 필요) ──
    ("brandy"         , "ingredient", "브랜디"          , "Brandy"                  , "base"     , "170,90,35"  , ""              , 6 , ""                       , None, "포도를 증류한 온기. 잔을 데우는 술."          , "Distilled warmth of the grape. A spirit that warms the glass."),
    ("amaretto"       , "ingredient", "아마레토"         , "Amaretto"                , "liqueur"  , "135,75,35"  , ""              , 6 , ""                       , None, "살구씨의 달콤쌉싸름한 향."                , "Bittersweet aroma of apricot kernels."),
    ("cream"          , "ingredient", "크림"           , "Cream"                   , "dairy"    , "240,228,205", ""              , 6 , ""                       , 15  , "무엇이든 부드럽게 감싸는 마무리."             , "A finish that softens everything it touches."),
    ("green_peppermint","ingredient", "그린페퍼민트"      , "Green Peppermint"        , "liqueur"  , "45,160,110" , ""              , 7 , ""                       , None, "초록빛 박하. 한 모금의 냉기."              , "Green mint. A sip of cold air."),
    ("white_cacao"    , "ingredient", "화이트카카오"       , "White Cacao"             , "liqueur"  , "232,220,188", ""              , 7 , ""                       , None, "색을 지운 초콜릿 향."                   , "Chocolate aroma with the color removed."),
    ("coffee"         , "ingredient", "커피"           , "Coffee"                  , "other"    , "70,45,30"   , ""              , 7 , ""                       , 15  , "새벽 바의 두 번째 연료."                 , "The bar's second fuel after midnight."),
    ("campari"        , "ingredient", "캄파리"          , "Campari"                 , "liqueur"  , "200,45,45"  , ""              , 8 , ""                       , None, "선명한 붉은색의 쓴맛. 어른의 입문 시험."        , "A vivid red bitterness. An entrance exam for adults."),
    ("blue_curacao"   , "ingredient", "블루큐라소"        , "Blue Curacao"            , "liqueur"  , "0,145,200"  , ""              , 8 , ""                       , None, "오렌지 향의 파랑. 바다를 접어 넣은 병."        , "Orange-scented blue. A bottle with the sea folded in."),
    ("pineapple_juice", "ingredient", "파인애플주스"       , "Pineapple Juice"         , "juice"    , "235,200,75" , ""              , 8 , ""                       , 25  , "트로피컬의 기본값."                     , "The default setting of tropical."),
    ("coconut_milk"   , "ingredient", "코코넛우유"        , "Coconut Milk"            , "dairy"    , "243,238,220", ""              , 8 , ""                       , 15  , "야자수 그늘의 맛."                     , "Tastes like the shade of a palm tree."),
    ("peach_brandy"   , "ingredient", "피치브랜디"        , "Peach Brandy"            , "liqueur"  , "225,145,100", ""              , 9 , ""                       , None, "복숭아의 단내를 증류한 리큐르."              , "A liqueur distilled from the sweetness of peaches."),
    ("melon_liqueur"  , "ingredient", "멜론리큐르"        , "Melon Liqueur"           , "liqueur"  , "130,190,55" , ""              , 10, ""                       , None, "초록 멜론의 단맛."                     , "The sweetness of green melon."),
    ("malibu"         , "ingredient", "말리부"          , "Malibu"                  , "liqueur"  , "242,238,220", ""              , 10, ""                       , None, "코코넛 향 럼. 휴양지의 지름길."             , "Coconut rum. A shortcut to the beach."),
    ("banana_liqueur" , "ingredient", "바나나리큐르"       , "Banana Liqueur"          , "liqueur"  , "235,205,65" , ""              , 10, ""                       , None, "노란 단맛의 마무리 담당."                 , "A yellow sweetness for the finishing touch."),
    # ── 잔 (제조 1단계 선반 — 제조 개편으로 6종 체계) ──
    # 통합 매핑: highball·collins→long_drink / rocks→old_fashioned / flute→wine(통합) / mug=맥주잔(유지) / sour 신설
    ("cocktail"       , "glass"     , "칵테일잔"         , "Cocktail Glass"          , ""         , ""           , "glass_cocktail", 1 , ""                       , None, "역삼각형의 그 잔. 격식의 상징."             , "The inverted triangle. A symbol of formality."),
    ("long_drink"     , "glass"     , "롱드링크잔"        , "Long Drink Glass"        , ""         , ""           , "glass_longdrink", 1, ""                       , None, "길고 늘씬한 잔. 탄산 롱드링크의 표준."         , "Tall and slim. The standard for fizzy long drinks."),
    ("old_fashioned"  , "glass"     , "올드패션잔"        , "Old Fashioned Glass"     , ""         , ""           , "glass_oldfashioned", 1, ""                    , None, "낮고 두꺼운 잔. 얼음과 독주의 자리."          , "Low and thick. A seat for ice and strong spirits."),
    ("sour"           , "glass"     , "사워잔"          , "Sour Glass"              , ""         , ""           , "glass_sour"    , 1 , ""                       , None, "사워 칵테일 전용의 짧은 스템 잔."            , "A short-stemmed glass reserved for sours."),
    ("wine"           , "glass"     , "와인잔"          , "Wine Glass"              , ""         , ""           , "glass_wine"    , 1 , ""                       , None, "다리가 긴 잔. 와인도 샴페인도 여기에."         , "A long-stemmed glass — for wine and champagne alike."),
    ("mug"            , "glass"     , "맥주잔"          , "Beer Mug"                , ""         , ""           , "glass_mug"     , 1 , ""                       , None, "맥주·뮬 담당의 묵직한 잔."                , "A heavy glass for beer and mules."),
    # ── 도구 (제조 2단계 선반 — 집으면 그 기믹) ──
    # 따개(opener)는 제거 — 병따기(Open)는 병뚜껑형(병맥주)만, 와인 오프너는 데모 제외(추후 재논의)
    ("shaker"         , "tool"      , "셰이커"          , "Shaker"                  , ""         , ""           , "tool_shaker"   , 1 , ""                       , None, "셰이킹 기믹용."                       , "For the shaking gimmick."),
    ("mixing_glass"   , "tool"      , "믹싱 글라스 & 바 스푼", "Mixing Glass & Bar Spoon", ""         , ""           , "tool_mixing"   , 1 , ""                       , None, "스터 기믹용 — 잔에 붓기 전 여기서 젓는다."      , "For the stirring gimmick — stir here before pouring."),
    # ── 가니시 (제조 3단계 선반 — v1.9부터 플레이어 선택·채점 대상) ──
    ("lime_wedge"     , "garnish"   , "라임 웨지"        , "Lime Wedge"              , ""         , ""           , "gn_lime"       , 1 , ""                       , None, "라임 조각 장식."                      , "A lime garnish."),
    ("lemon_slice"    , "garnish"   , "레몬 슬라이스"      , "Lemon Slice"             , ""         , ""           , "gn_lemon"      , 1 , ""                       , None, "레몬 슬라이스 장식."                    , "A lemon slice garnish."),
    ("olive"          , "garnish"   , "올리브"          , "Olive"                   , ""         , ""           , "gn_olive"      , 1 , ""                       , None, "마티니의 완성."                       , "What completes a martini."),
    ("cherry"         , "garnish"   , "체리"           , "Cherry"                  , ""         , ""           , "gn_cherry"     , 1 , ""                       , None, "사워 칵테일의 마침표."                   , "The period at the end of a sour."),
    ("orange_slice"   , "garnish"   , "오렌지 슬라이스"     , "Orange Slice"            , ""         , ""           , "gn_orange"     , 1 , ""                       , None, "선라이즈의 태양."                      , "The sun of a sunrise."),
]

# D-06 — 레시피 밖 재료도 실제 선택 재료에 맞는 기믹을 생성한다.
# 기본 목표량은 미니게임을 정상 종료하기 위한 값이며, 정답 레시피의 점수 기준으로 사용하지 않는다.
_DEFAULT_ACTION_BY_CATEGORY = {
    "base": "pour", "liqueur": "pour", "wine_beer": "pour", "juice": "pour",
    "dairy": "pour", "syrup": "pour", "other": "pour",
    "mixer": "fill_up", "fruit": "squeeze", "powder": "powder",
}
_DEFAULT_TARGET_BY_ACTION = {
    "pour": (1.0, "oz"), "fill_up": (3.0, "oz"),
    "squeeze": (0.5, "oz"), "powder": (1.0, "tsp"),
}
# 재료 선반의 실제 배치 화면. category는 기믹 분류이고 shelf_group은 UI 배치 분류라
# 서로 대신할 수 없다. 데모에서 자동 투입하는 squeeze/powder 재료는 화면에 놓지 않으므로 공란이다.
_FRIDGE_INGREDIENT_IDS = {
    "beer", "soda_water", "orange_juice", "ginger_ale", "cola",
    "sour_mix", "cranberry_juice", "milk", "cream", "coffee", "pineapple_juice",
    "coconut_milk",
}
_LIQUOR_INGREDIENT_IDS = {
    "gin", "red_wine", "champagne", "tequila", "vodka", "rum", "whiskey",
    "dry_vermouth", "grenadine", "cointreau", "triple_sec", "kahlua", "brandy",
    "amaretto", "green_peppermint", "white_cacao", "campari", "blue_curacao",
    "peach_brandy", "melon_liqueur", "malibu", "banana_liqueur",
}
# 따르기·필업 기믹에서 재료별 액체 투명도를 조절한다. 0.0=완전 투명, 1.0=완전 불투명.
# 데모 비조작 재료(squeeze/powder)와 비재료 항목은 None으로 배출한다.
_CLEAR_LIQUID_IDS = {
    "gin", "champagne", "soda_water", "tequila", "vodka", "dry_vermouth",
    "cointreau", "triple_sec", "white_cacao", "malibu",
}
_OPAQUE_LIQUID_IDS = {"milk", "cream", "coconut_milk"}

def _liquid_alpha_for(item_id, action):
    if action not in ("pour", "fill_up"):
        return None
    if item_id in _CLEAR_LIQUID_IDS:
        return 0.35
    if item_id in _OPAQUE_LIQUID_IDS:
        return 1.0
    return 0.75

_expanded_shelf_items = []
for _row in SHELF_ITEMS:
    _base = dict(zip(SHELF_BASE_COLS, _row))
    if _base["kind"] == "ingredient":
        _action = _DEFAULT_ACTION_BY_CATEGORY.get(_base["category"], "")
        _qty, _unit = _DEFAULT_TARGET_BY_ACTION.get(_action, (None, ""))
        _prep = "open" if _base["id"] == "beer" else ""
        _shelf_group = ("fridge" if _base["id"] in _FRIDGE_INGREDIENT_IDS else
                        "liquor" if _base["id"] in _LIQUOR_INGREDIENT_IDS else "")
        _alpha = _liquid_alpha_for(_base["id"], _action)
        _expanded_shelf_items.append(tuple(_row) + (_action, _prep, _qty, _unit, _shelf_group, _alpha))
    else:
        _expanded_shelf_items.append(tuple(_row) + ("", "", None, "", "", None))
SHELF_ITEMS = _expanded_shelf_items


# SHELF_ITEMS 접근 헬퍼 — 호출 시점에 전역을 읽는다(build.py가 시트 데이터로 갈아끼워도 동작)
def shelf_rows(kind=None):
    return [r for r in SHELF_ITEMS if kind is None or r[1] == kind]

def shelf_dicts(kind=None):
    return [dict(zip(SHELF_COLS, r)) for r in shelf_rows(kind)]

def shelf_ids(kind=None):
    return {r[0] for r in shelf_rows(kind)}


# ============================================================
# 2. 마스터 — Cocktails + RecipeLines
# ============================================================
# 제조 개편(26.08.17) — Order/Selected/Actual Craft 3층 판정 체계 반영.
#   · tier 폐지: 코스터 인내심은 고정(coaster_base_sec), 서빙 여유는 해금일 기반,
#     주문 풀은 RandomWaves/RegularSlots의 order 직접 지정(공란=그날 해금 풀 추첨).
#   · fill 컬럼 폐지: 필업은 RecipeLines의 action=fill_up 라인으로 흡수.
#   · unlock_day = 완전 수동(파생 폐지). time_limit_sec = 완전 수동(기믹 수 공식 폐지).
#   · status: confirmed=수치 확정 / tbd=레시피 수치 대기(수량 공란 허용, 확정 파일 수령 후 채움).
#   · color2: 완성 색이 그라데이션인 칵테일만 끝색 기록(현재 oasis_sunset 1종). 공란=단색.
#   · prep: cap(병뚜껑)만 사용 — cork(와인 오프너)는 데모 제외(추후 재논의, 인수인계 §8-6).
#   · mixing_ice/serving_ice: 얼음 데이터(none/cubed/crushed) — 레시피 화면 얼음 행·제조 연출용.
#   · 일차는 0일차 스타트(표시=데이터) — 시드는 구표기(1-based)로 적고 아래 _shift_day가 로드 시 일괄 변환한다.
CK_COLS = ["id","name_ko","name_en","status","price","abv","glass","mix","prep","garnish","color","color2","tags",
           "flavor_ko","flavor_en","unlock_day","unlock_when","time_limit_sec",
           "mixing_ice","serving_ice","recipe_desc_ko","recipe_desc_en"]
COCKTAILS = [
    # id, ko, en, status, price, abv, glass, mix, prep, garnish, color, color2, tags, flavor_ko, flavor_en, unlock, when, tlimit
    ("gin_tonic",       "진토닉",             "Gin & Tonic",     "confirmed", 180, 8.0,  "long_drink",    "build", "",    "lime_wedge",   "255,255,255", "", "상큼한;청량한;클래식", "진과 탄산수. 가장 단순해서 가장 정직한 칵테일.", "Gin and carbonated water. The simplest, and therefore the most honest.", 1, "", 36),
    ("gin_fizz",        "진피즈",             "Gin Fizz",        "confirmed", 220, 8.0,  "long_drink",    "shake", "",    "lemon_slice",  "255,255,255", "", "상큼한;클래식;청량한", "진과 레몬, 그리고 '피즈' 하는 탄산 소리.", "Gin, lemon, and that 'fizz' of carbonation.", 1, "", 52),
    ("bottle_beer",     "병맥주",             "Bottled Beer",    "confirmed", 90,  4.5,  "mug",           "none",  "cap", None,           "202,162,4",   "", "청량한;가벼운",        "뚜껑 따는 소리가 안주다. 잔은 곁들여서.", "The pop of the cap is the appetizer. Served with a glass.", 2, "", 28),
    ("red_wine",        "레드와인",           "Red Wine",        "confirmed", 220, 13.0, "wine",          "none",  "",    None,           "98,15,11",    "", "묵직한;클래식",        "말이 필요 없는 잔. 향이 절반이다.", "A glass that needs no words. Half of it is the aroma.", 2, "", 36),
    ("champagne",       "샴페인",             "Champagne",       "confirmed", 350, 12.0, "wine",          "none",  "",    None,           "255,245,225", "", "화사한;달콤한",        "기포가 올라오는 동안은 누구나 주인공.", "While the bubbles rise, everyone's the main character.", 2, "", 36),
    ("screwdriver",     "스크류드라이버",     "Screwdriver",     "confirmed", 190, 12.0, "long_drink",    "build", "",    None,           "236,223,95",  "", "달콤한;부드러운",      "보드카와 오렌지주스. 이름은 공구, 맛은 과일.", "Vodka and orange juice. Named after a tool, tastes like fruit.", 2, "", 44),
    ("moscow_mule",     "모스코뮬",           "Moscow Mule",     "confirmed", 210, 10.0, "mug",           "build", "",    "lime_wedge",   "215,163,2",   "", "청량한;알싸한",        "생강의 알싸함이 노새의 뒷발차기 같다고 해서 뮬.", "The ginger kick they say feels like a mule's hind leg.", 2, "", 44),
    ("tequila_sunrise", "데킬라 선라이즈",    "Tequila Sunrise", "confirmed", 200, 12.0, "long_drink",    "build", "",    "orange_slice", "253,161,48",  "", "달콤한;화사한",        "잔 속에서 해가 뜬다. 새벽의 바에서 제일 잘 팔리는 아침.", "A sunrise inside a glass. The best-selling morning at a late-night bar.", 2, "", 44),
    ("long_island",     "롱아일랜드 아이스티","Long Island Iced Tea", "confirmed", 380, 22.0, "long_drink", "build", "",   "lemon_slice",  "214,122,13",  "", "독한;달콤한",          "홍차는 한 방울도 안 들어간다. 그게 함정이다.", "Not a drop of tea in it. That's the trap.", 2, "", 68),
    ("whiskey_sour",    "위스키 사워",        "Whiskey Sour",    "confirmed", 240, 15.0, "sour",          "shake", "",    "cherry",       "243,208,144", "", "새콤한;묵직한",        "위스키의 무게에 레몬의 균형.", "The weight of whiskey, balanced by lemon.", 3, "", 52),
    ("dry_martini",     "드라이 마티니",      "Dry Martini",     "confirmed", 300, 30.0, "cocktail",      "stir",  "",    "olive",        "230,229,201", "", "씁쓸한;클래식;독한",   "젓지 말고 흔들어서, 라고 말하는 손님을 조심할 것.", "Beware the customer who says 'shaken, not stirred.'", 3, "", 44),
    ("bacardi",         "바카디",             "Bacardi",         "confirmed", 230, 20.0, "cocktail",      "shake", "",    None,           "219,96,89",   "", "새콤한;클래식",        "럼과 그레나딘과 라임. 이름을 건 칵테일.", "Rum, grenadine, lime. A cocktail that bears the name.", 3, "", 52),
    ("cosmopolitan",    "코스모폴리탄",       "Cosmopolitan",    "confirmed", 250, 20.0, "cocktail",      "shake", "",    "lemon_slice",  "219,109,92",  "", "상큼한;부드러운",      "도시적인 붉은 빛. 유행은 지나가도 맛은 남는다.", "Urban red. Trends pass; the taste stays.", 4, "", 60),
    ("margarita",       "마가리타",           "Margarita",       "confirmed", 260, 25.0, "cocktail",      "shake", "",    "lime_wedge",   "210,218,109", "", "새콤한;독한",          "데킬라의 태양과 레몬의 번개.", "Tequila's sun and lemon's lightning.", 5, "", 52),
    ("white_lady",      "화이트레이디",       "White Lady",      "confirmed", 270, 25.0, "cocktail",      "shake", "",    None,           "247,241,214", "", "상큼한;우아한",        "새하얀 드레스처럼 우아하고, 도수는 우아하지 않다.", "Elegant as a white dress. The proof is not elegant.", 5, "", 52),
    ("kahlua_milk",     "깔루아 밀크",        "Kahlua Milk",     "confirmed", 150, 7.0,  "old_fashioned", "build", "",    None,           "159,162,119", "", "달콤한;부드러운",      "커피와 우유와 약간의 알코올. 고양이도 탐내는 맛.", "Coffee, milk, a little alcohol. Even cats covet it.", 5, "", 44),
    # ── 제조 개편 신규 13종 — status=tbd: 레시피 수량 확정 파일 수령 후 confirmed로 전환 ──
    ("godfather",       "갓파더",             "Godfather",       "tbd", 230, 28.0, "old_fashioned", "stir",  "", None,           "222,150,52",  "", "묵직한;클래식;달콤한", "위스키와 아마레토. 짧고 묵직한 두 재료의 대화.", "Whiskey and amaretto. A short, weighty conversation between two spirits.", 6, "", 44),
    ("godmother",       "갓마더",             "Godmother",       "tbd", 220, 22.0, "old_fashioned", "stir",  "", None,           "232,188,102", "", "부드러운;달콤한;클래식", "보드카의 깨끗함에 아마레토의 단맛을 얹는다.", "Clean vodka softened by the sweetness of amaretto.", 6, "", 44),
    ("french_connection","프렌치 커넥션",     "French Connection", "tbd", 240, 30.0, "old_fashioned", "build", "", None,          "200,120,40",  "", "묵직한;달콤한;클래식", "브랜디와 아마레토가 만드는 깊고 느린 단맛.", "Brandy and amaretto create a deep, lingering sweetness.", 6, "", 44),
    ("brandy_sour",     "브랜디 사워",        "Brandy Sour",     "tbd", 250, 15.0, "sour",          "shake", "", "cherry",       "212,146,0",   "", "새콤한;묵직한",        "브랜디의 온기에 레몬의 균형을 잡는다.", "Warm brandy balanced with lemon.", 6, "", 52),
    ("pink_lady",       "핑크 레이디",        "Pink Lady",       "tbd", 300, 18.0, "cocktail",      "shake", "", None,           "254,97,126",  "", "달콤한;부드러운;화사한", "진과 그레나딘, 크림이 만드는 부드러운 분홍빛.", "Gin, grenadine, and cream in a soft shade of pink.", 6, "", 60),
    ("espresso_martini","에스프레소 마티니",  "Espresso Martini", "tbd", 280, 20.0, "cocktail",     "shake", "", None,           "66,42,28",    "", "달콤한;묵직한;커피",   "보드카와 커피가 새벽을 한 잔 더 연장한다.", "Vodka and coffee extend the night by one more glass.", 7, "", 52),
    ("grasshopper",     "그래스호퍼",         "Grasshopper",     "tbd", 300, 10.0, "cocktail",      "shake", "", None,           "118,183,161", "", "달콤한;부드러운;민트", "민트와 카카오, 크림이 만드는 차갑고 달콤한 디저트 칵테일.", "Mint, cacao, and cream make a cool, sweet dessert cocktail.", 7, "", 60),
    ("irish_coffee",    "아이리시 커피",      "Irish Coffee",    "tbd", 300, 12.0, "mug",           "build", "", None,           "74,48,32",    "", "달콤한;부드러운;커피", "위스키와 뜨거운 커피 위에 크림을 띄운 따뜻한 한 잔.", "Whiskey and hot coffee topped with cream for a warming drink.", 7, "", 60),
    ("old_pal",         "올드 팔",            "Old Pal",         "tbd", 280, 25.0, "cocktail",      "stir",  "", None,           "212,72,45",   "", "씁쓸한;묵직한;클래식", "위스키와 버무스, 캄파리의 쌉쌀한 악수.", "A bitter handshake of whiskey, vermouth, and Campari.", 8, "", 52),
    ("blue_hawaii",     "블루 하와이",        "Blue Hawaii",     "tbd", 320, 14.0, "long_drink",    "shake", "", None,           "0,151,182",   "", "달콤한;열대과일;화사한", "럼과 블루큐라소 위로 파인애플과 코코넛이 펼쳐진다.", "Rum and blue curaçao carried by pineapple and coconut.", 8, "", 60),
    ("oasis_sunset",    "오아시스 선셋",      "Oasis Sunset",    "tbd", 400, 18.0, "long_drink",    "shake", "", "orange_slice", "0,153,139",   "219,96,89", "달콤한;열대과일;화사한", "푸른 오아시스 위로 붉은 노을이 번지는 바의 시그니처 칵테일.", "The bar's signature: a red sunset spreading over a blue oasis.", 8, "", 68),
    ("blue_sapphire",   "블루 사파이어",      "Blue Sapphire",   "tbd", 320, 15.0, "long_drink",    "shake", "", None,           "0,153,139",   "", "달콤한;열대과일;상큼한", "블루큐라소와 복숭아, 코코넛이 만드는 푸른 보석.", "Blue curaçao, peach, and coconut form a liquid blue gem.", 9, "", 60),
    ("june_bug",        "준 벅",              "June Bug",        "tbd", 320, 12.0, "long_drink",    "shake", "", None,           "186,205,2",   "", "달콤한;열대과일;화사한", "멜론과 코코넛, 바나나와 파인애플이 겹치는 초록빛 트로피컬 칵테일.", "A green tropical mix of melon, coconut, banana, and pineapple.", 10, "", 60),
]

# v3.6 — 태그 사전. 칵테일 tags와 Tastes의 cocktail.tag(...)이 쓰는 한글 태그의 정본 목록 + 영어 표기.
# 여기 없는 태그를 쓰면 빌드 에러(오타 차단). 표시: 정보 화면 키워드가 ko/en을 함께 배포받는다.
# 제조 개편: category 신설 — 레시피 화면 필터가 태그를 두 구분으로 묶는다. taste=맛 / feel=느낌.
#   민트·열대과일·커피 3종 추가(신규 칵테일용). '비밀'은 bees_knees 삭제로 사용처가 없어 제거.
TAG_COLS = ["tag_ko","tag_en","category","note"]
TAGS = [
    ("가벼운",   "Light",          "feel",  "저도수·부담 없음"),
    ("달콤한",   "Sweet",          "taste", ""),
    ("독한",     "Strong",         "feel",  "고도수"),
    ("묵직한",   "Heavy",          "feel",  "바디감"),
    ("민트",     "Mint",           "taste", "박하 계열"),
    ("부드러운", "Smooth",         "feel",  ""),
    ("상큼한",   "Fresh",          "taste", "시트러스"),
    ("새콤한",   "Tangy",          "taste", ""),
    ("씁쓸한",   "Bitter",         "taste", ""),
    ("알싸한",   "Spicy",          "taste", "진저 계열의 톡 쏘는 맛"),
    ("열대과일", "Tropical Fruit", "taste", "파인애플·코코넛 계열"),
    ("우아한",   "Elegant",        "feel",  ""),
    ("청량한",   "Crisp",          "feel",  "탄산감"),
    ("커피",     "Coffee",         "taste", ""),
    ("클래식",   "Classic",        "feel",  ""),
    ("화사한",   "Bright",         "feel",  ""),
]
TAG_EN = {t[0]: t[1] for t in TAGS}
TAG_CATEGORY = {t[0]: t[2] for t in TAGS}
# 호환성 유지형 태그 ID. 기존 Cocktails.tags와 cocktail.tag(한글)는 한 버전 유지하고,
# JSON 표시 객체에는 이 불변 ID를 추가해 엔진이 언어 문자열 대신 ID로 이행할 수 있게 한다.
TAG_IDS = {
    "가벼운": "light", "달콤한": "sweet", "독한": "strong", "묵직한": "heavy",
    "민트": "mint", "부드러운": "smooth", "상큼한": "fresh", "새콤한": "tangy",
    "씁쓸한": "bitter", "알싸한": "spicy", "열대과일": "tropical_fruit",
    "우아한": "elegant", "청량한": "crisp", "커피": "coffee",
    "클래식": "classic", "화사한": "bright",
}

# 얼음 데이터(제조 개편) — mixing_ice: 셰이커·믹싱 글라스에 넣는 얼음 / serving_ice: 완성 잔에 넣는 얼음.
# 값은 none/cubed/crushed. 레시피 화면 얼음 행과 제조·서빙 연출이 이 두 값을 읽는다.
COCKTAIL_ICE = {
    "gin_tonic":       ("none",  "cubed"),
    "gin_fizz":        ("cubed", "cubed"),
    "bottle_beer":     ("none",  "none"),
    "red_wine":        ("none",  "none"),
    "champagne":       ("none",  "none"),
    "screwdriver":     ("none",  "cubed"),
    "moscow_mule":     ("none",  "cubed"),
    "tequila_sunrise": ("none",  "cubed"),
    "long_island":     ("none",  "cubed"),
    "whiskey_sour":    ("cubed", "none"),
    "dry_martini":     ("cubed", "none"),
    "bacardi":         ("cubed", "none"),
    "cosmopolitan":    ("cubed", "none"),
    "margarita":       ("cubed", "none"),
    "white_lady":      ("cubed", "none"),
    "kahlua_milk":     ("none",  "cubed"),
    "godfather":       ("cubed", "cubed"),
    "godmother":       ("cubed", "cubed"),
    "french_connection": ("none", "cubed"),
    "brandy_sour":     ("cubed", "none"),
    "pink_lady":       ("cubed", "none"),
    "espresso_martini": ("cubed", "none"),
    "grasshopper":     ("cubed", "none"),
    "irish_coffee":    ("none",  "none"),
    "old_pal":         ("cubed", "none"),
    "blue_hawaii":     ("cubed", "cubed"),
    "oasis_sunset":    ("cubed", "cubed"),
    "blue_sapphire":   ("cubed", "cubed"),
    "june_bug":        ("cubed", "cubed"),
}

# v3.7 — 제조법 설명은 레시피 데이터에서 조립하지 않고 사람이 직접 쓴다(칵테일 설명과 같은 방식).
# RecipeLines는 채점 정답표로 그대로 두고, 이 문안은 정보 화면·레시피 팝업의 표시 전용이다.
# 수치를 고치면 이 문안도 같이 고쳐야 한다 — 빌드는 빈 칸만 잡고 내용 일치는 검사하지 못한다.
# status=tbd 칵테일은 수량이 미정이라 수량 없는 서술로 쓴다 — 수량 확정 파일 수령 시 문안에 수치를 더한다.
RECIPE_DESCS = {
    "gin_tonic": (
        "롱드링크 잔에 진 1.5oz를 붓고, 탄산수 4oz로 잔을 채워 가볍게 젓는다. 라임 웨지를 걸친다.",
        "Pour 1.5oz gin into a long drink glass, top with 4oz carbonated water, and stir gently. Garnish with a lime wedge."),
    "gin_fizz": (
        "진 1.5oz, 레몬 반 개의 즙, 설탕 1tsp을 셰이킹해 롱드링크 잔에 따른다. 탄산수 3oz로 채우고 레몬 슬라이스를 올린다.",
        "Shake 1.5oz gin, the juice of half a lemon, and 1 tsp sugar, then strain into a long drink glass. Top with 3oz carbonated water and add a lemon slice."),
    "bottle_beer": (
        "뚜껑을 딴 뒤 맥주잔에 12oz를 천천히 따른다. 거품이 잔 위로 넘치지 않게 기울여서.",
        "Pop the cap and pour 12oz slowly into a beer mug, tilted so the head doesn't spill over."),
    "red_wine": (
        "와인잔에 8oz를 따른다. 잔을 흔들지 않는다.",
        "Pour 8oz into a wine glass. Do not swirl."),
    "champagne": (
        "와인잔에 8oz를 따른다. 기포가 가라앉기 전에 낸다.",
        "Pour 8oz into a wine glass. Serve before the bubbles settle."),
    "screwdriver": (
        "롱드링크 잔에 보드카 1.5oz와 오렌지주스 6oz를 넣고 젓는다.",
        "Build 1.5oz vodka and 6oz orange juice in a long drink glass and stir."),
    "moscow_mule": (
        "맥주잔에 보드카 1.5oz와 라임 반 개의 즙을 넣고 젓는다. 진저에일 4oz로 채우고 라임 웨지를 걸친다.",
        "Build 1.5oz vodka and the juice of half a lime in a mug and stir. Top with 4oz ginger ale and garnish with a lime wedge."),
    "tequila_sunrise": (
        "롱드링크 잔에 데킬라 1oz와 오렌지주스 6oz를 넣고 젓는다. 오렌지 슬라이스를 올린다.",
        "Build 1oz tequila and 6oz orange juice in a long drink glass and stir. Garnish with an orange slice."),
    "long_island": (
        "롱드링크 잔에 진·보드카·럼·데킬라를 1oz씩 넣고 레몬 반 개의 즙을 더해 젓는다. 콜라 1.5oz로 채우고 레몬 슬라이스를 올린다.",
        "Build 1oz each of gin, vodka, rum, and tequila in a long drink glass, add the juice of half a lemon, and stir. Top with 1.5oz cola and add a lemon slice."),
    "whiskey_sour": (
        "위스키 1.5oz, 레몬 반 개의 즙, 설탕 1tsp을 셰이킹해 사워 잔에 따른다. 사워믹스 2oz로 채우고 체리를 올린다.",
        "Shake 1.5oz whiskey, the juice of half a lemon, and 1 tsp sugar, then strain into a sour glass. Top with 2oz sour mix and garnish with a cherry."),
    "dry_martini": (
        "믹싱 글라스에 진 6oz와 드라이 버무스 1.5oz를 넣고 스터한 뒤 칵테일 잔에 따른다. 올리브를 넣는다.",
        "Stir 6oz gin with 1.5oz dry vermouth in a mixing glass, then strain into a cocktail glass. Drop in an olive."),
    "bacardi": (
        "럼 4.5oz, 그레나딘 0.75oz, 라임즙 1.5oz를 셰이킹해 칵테일 잔에 따른다.",
        "Shake 4.5oz rum, 0.75oz grenadine, and 1.5oz lime juice, then strain into a cocktail glass."),
    "cosmopolitan": (
        "보드카 4oz, 쿠앵트로 1.5oz, 크랜베리주스 3oz, 라임즙 1.5oz를 셰이킹해 칵테일 잔에 따른다. 레몬 슬라이스를 올린다.",
        "Shake 4oz vodka, 1.5oz cointreau, 3oz cranberry juice, and 1.5oz lime juice, then strain into a cocktail glass. Add a lemon slice."),
    "margarita": (
        "데킬라 3.5oz, 트리플섹 2oz, 레몬즙 1.5oz를 셰이킹해 칵테일 잔에 따른다. 라임 웨지를 걸친다.",
        "Shake 3.5oz tequila, 2oz triple sec, and 1.5oz lemon juice, then strain into a cocktail glass. Garnish with a lime wedge."),
    "white_lady": (
        "진 40ml, 트리플섹 30ml, 레몬즙 20ml를 셰이킹해 칵테일 잔에 따른다.",
        "Shake 40ml gin, 30ml triple sec, and 20ml lemon juice, then strain into a cocktail glass."),
    "kahlua_milk": (
        "올드패션 잔에 칼루아 1.5oz를 붓고 우유 0.75oz를 위에 얹듯 천천히 따른다.",
        "Pour 1.5oz kahlua into an old fashioned glass, then layer 0.75oz milk slowly on top."),
    "godfather": (
        "믹싱 글라스에 위스키와 아마레토를 넣고 스터한 뒤 올드패션 잔에 따른다.",
        "Stir whiskey and amaretto in a mixing glass, then strain into an old fashioned glass."),
    "godmother": (
        "믹싱 글라스에 보드카와 아마레토를 넣고 스터한 뒤 올드패션 잔에 따른다.",
        "Stir vodka and amaretto in a mixing glass, then strain into an old fashioned glass."),
    "french_connection": (
        "올드패션 잔에 브랜디와 아마레토를 넣고 가볍게 젓는다.",
        "Build brandy and amaretto in an old fashioned glass and stir gently."),
    "brandy_sour": (
        "브랜디, 레몬즙, 설탕을 셰이킹해 사워 잔에 따른다. 사워믹스로 채우고 체리를 올린다.",
        "Shake brandy, lemon juice, and sugar, then strain into a sour glass. Top with sour mix and garnish with a cherry."),
    "pink_lady": (
        "진, 그레나딘, 크림을 셰이킹해 칵테일 잔에 따른다.",
        "Shake gin, grenadine, and cream, then strain into a cocktail glass."),
    "espresso_martini": (
        "보드카, 칼루아, 커피를 셰이킹해 칵테일 잔에 따른다.",
        "Shake vodka, kahlua, and coffee, then strain into a cocktail glass."),
    "grasshopper": (
        "그린페퍼민트, 화이트카카오, 크림을 셰이킹해 칵테일 잔에 따른다.",
        "Shake green peppermint, white cacao, and cream, then strain into a cocktail glass."),
    "irish_coffee": (
        "맥주잔에 위스키와 뜨거운 커피를 넣고 저은 뒤 크림을 위에 띄운다.",
        "Build whiskey and hot coffee in a mug, stir, and float cream on top."),
    "old_pal": (
        "믹싱 글라스에 위스키, 드라이 버무스, 캄파리를 넣고 스터한 뒤 칵테일 잔에 따른다.",
        "Stir whiskey, dry vermouth, and Campari in a mixing glass, then strain into a cocktail glass."),
    "blue_hawaii": (
        "럼, 블루큐라소, 코코넛우유를 셰이킹해 롱드링크 잔에 따른다. 파인애플주스로 채운다.",
        "Shake rum, blue curaçao, and coconut milk, then strain into a long drink glass. Top with pineapple juice."),
    "oasis_sunset": (
        "데킬라와 블루큐라소를 셰이킹해 롱드링크 잔에 따르고, 그레나딘을 천천히 흘려 노을 층을 만든다. 오렌지 슬라이스를 올린다.",
        "Shake tequila and blue curaçao, strain into a long drink glass, then slowly pour grenadine to form a sunset layer. Garnish with an orange slice."),
    "blue_sapphire": (
        "블루큐라소, 피치브랜디, 코코넛우유를 셰이킹해 롱드링크 잔에 따른다.",
        "Shake blue curaçao, peach brandy, and coconut milk, then strain into a long drink glass."),
    "june_bug": (
        "멜론리큐르, 말리부, 바나나리큐르를 셰이킹해 롱드링크 잔에 따른다. 파인애플주스와 사워믹스로 채운다.",
        "Shake melon liqueur, Malibu, and banana liqueur, then strain into a long drink glass. Top with pineapple juice and sour mix."),
}
COCKTAILS = [tuple(list(r) + list(COCKTAIL_ICE[r[0]]) + list(RECIPE_DESCS[r[0]])) for r in COCKTAILS]

# 레시피 라인(채점 정답표) — 5번째 요소는 플래그 문자열(생략 가능):
#   core    = 핵심 재료. 누락 시 강제 Sewage (Order/Selected/Actual 판정 체계)
#   auto    = 데모 자동 투입·비채점 (파우더 설탕 — powder_demo_behavior 참조)
#   noscore = 수량 채점 제외 (현재 사용 0 — 예약)
#   fill_up도 pour처럼 목표량 대비 수량 오차로 채점한다(루프 기획서 확정). 구 fill 컬럼을 흡수했다.
#   confirmed 5종의 fill_up 목표량은 [가안] — 수량 확정 파일 수령 시 함께 갱신.
RECIPES = {
    "gin_tonic":       [("pour","gin",1.5,"oz","core"), ("fill_up","soda_water",4,"oz")],
    "gin_fizz":        [("pour","gin",1.5,"oz","core"), ("squeeze","lemon",0.5,"oz","auto"), ("powder","sugar",1,"tsp","auto"), ("fill_up","soda_water",3,"oz")],
    "bottle_beer":     [("pour","beer",12,"oz","core")],
    "red_wine":        [("pour","red_wine",8,"oz","core")],
    "champagne":       [("pour","champagne",8,"oz","core")],
    "screwdriver":     [("pour","vodka",1.5,"oz","core"), ("pour","orange_juice",6,"oz")],
    "moscow_mule":     [("pour","vodka",1.5,"oz","core"), ("squeeze","lime",0.5,"oz","auto"), ("fill_up","ginger_ale",4,"oz")],
    "tequila_sunrise": [("pour","tequila",1,"oz","core"), ("pour","orange_juice",6,"oz")],
    "long_island":     [("pour","gin",1,"oz","core"), ("pour","vodka",1,"oz","core"), ("pour","rum",1,"oz","core"), ("pour","tequila",1,"oz","core"), ("squeeze","lemon",0.5,"oz","auto"), ("fill_up","cola",1.5,"oz")],
    "whiskey_sour":    [("pour","whiskey",1.5,"oz","core"), ("squeeze","lemon",0.5,"oz","auto"), ("powder","sugar",1,"tsp","auto"), ("fill_up","sour_mix",2,"oz")],
    "dry_martini":     [("pour","gin",6,"oz","core"), ("pour","dry_vermouth",1.5,"oz")],
    "bacardi":         [("pour","rum",4.5,"oz","core"), ("pour","grenadine",0.75,"oz"), ("squeeze","lime",1.5,"oz","auto")],
    "cosmopolitan":    [("pour","vodka",4,"oz","core"), ("pour","cointreau",1.5,"oz"), ("pour","cranberry_juice",3,"oz"), ("squeeze","lime",1.5,"oz","auto")],
    "margarita":       [("pour","tequila",3.5,"oz","core"), ("pour","triple_sec",2,"oz"), ("squeeze","lemon",1.5,"oz","auto")],
    "white_lady":      [("pour","gin",40,"ml","core"), ("pour","triple_sec",30,"ml"), ("squeeze","lemon",20,"ml","auto")],
    "kahlua_milk":     [("pour","kahlua",1.5,"oz","core"), ("pour","milk",0.75,"oz")],
    # ── 신규 13종 — 수량 tbd(공란). 확정 파일 수령 시 qty·unit을 채우고 Cocktails.status를 confirmed로 ──
    "godfather":       [("pour","whiskey",None,None,"core"), ("pour","amaretto",None,None)],
    "godmother":       [("pour","vodka",None,None,"core"), ("pour","amaretto",None,None)],
    "french_connection": [("pour","brandy",None,None,"core"), ("pour","amaretto",None,None)],
    "brandy_sour":     [("pour","brandy",None,None,"core"), ("squeeze","lemon",None,None,"auto"), ("powder","sugar",None,None,"auto"), ("fill_up","sour_mix",None,None)],
    "pink_lady":       [("pour","gin",None,None,"core"), ("pour","grenadine",None,None), ("pour","cream",None,None)],
    "espresso_martini": [("pour","vodka",None,None,"core"), ("pour","kahlua",None,None), ("pour","coffee",None,None,"core")],
    "grasshopper":     [("pour","green_peppermint",None,None,"core"), ("pour","white_cacao",None,None,"core"), ("pour","cream",None,None)],
    "irish_coffee":    [("pour","whiskey",None,None,"core"), ("pour","coffee",None,None,"core"), ("pour","cream",None,None)],
    "old_pal":         [("pour","whiskey",None,None,"core"), ("pour","dry_vermouth",None,None), ("pour","campari",None,None)],
    "blue_hawaii":     [("pour","rum",None,None,"core"), ("pour","blue_curacao",None,None,"core"), ("pour","coconut_milk",None,None), ("fill_up","pineapple_juice",None,None)],
    "oasis_sunset":    [("pour","tequila",None,None,"core"), ("pour","blue_curacao",None,None,"core"), ("pour","grenadine",None,None)],
    "blue_sapphire":   [("pour","blue_curacao",None,None,"core"), ("pour","peach_brandy",None,None), ("pour","coconut_milk",None,None)],
    "june_bug":        [("pour","melon_liqueur",None,None,"core"), ("pour","malibu",None,None), ("pour","banana_liqueur",None,None), ("fill_up","pineapple_juice",None,None), ("fill_up","sour_mix",None,None)],
}

# 레시피 라인 정규화 헬퍼 — (action, ingredient, qty, unit, is_core, scored, auto_apply)
def recipe_lines(cid):
    out = []
    for l in RECIPES.get(cid, []):   # 키 없음 = 0줄 — 검증이 에러로 잡되 여기서 죽지 않는다
        a, ing, q, u = l[0], l[1], l[2], l[3]
        flags = set((l[4] if len(l) > 4 else "").split("|")) - {""}
        auto = "auto" in flags
        scored = not ("noscore" in flags or auto)   # fill_up도 pour처럼 수량 오차 채점 (루프 기획서 확정)
        out.append({"action": a, "ingredient": ing, "qty": q, "unit": u,
                    "is_core": "core" in flags, "scored": scored, "auto_apply": auto})
    return out

# ============================================================
# 3. 마스터 — Characters / Personalities / Barks
# ============================================================
CHAR_COLS = ["id","name_ko","name_en","name_color","role","affinity","alive_flag","expressions","enter_sfx","exit_sfx","note"]
CHARACTERS = [
    ("luna",   "루나",     "Luna",       "#5e9fe1", "player",      False, None,    "default",                    None, None, "주인공. 인조인간 바텐더"),
    ("chris",  "크리스",   "Chris",      "#8b5e2f", "master",      True,  None,    "default;success;fail",       "SFX_chris_enter", "SFX_chris_exit", "바 언노운의 마스터. 前 연구원"),
    ("aili",   "아일리",   "Aili",       "#fcfe57", "guest",       True,  None,    "default;success;fail;joy",   "SFX_guest_door_in", "SFX_guest_door_out", "의사. 루나를 치료한 인물"),
    ("port",   "포트",     "Port",       "#3c3cca", "guest",       True,  None,    "default;anger;joy;serious;event_surprise",  "SFX_port_enter", "SFX_port_exit", "루나를 데려온 인물. 前 기자"),
    ("tom",    "톰 거너",  "Tom Gunner", "#7a4a2b", "guest",       True,  None,    "default;serious",            "SFX_guest_door_in", "SFX_guest_door_out", "갱 두목. 前 벡터 용병 대대장"),
    ("samho",  "삼호",     "Samho",      "#ea3a3a", "guest",       True,  "samho", "default;success;fail;drunk", "SFX_guest_door_in", "SFX_guest_door_out", "갱단원. day4 생사 분기(구 day3)"),
    ("bubi",   "부비",     "Bubi",       "#fdd48e", "guest",       True,  None,    "default",                    "SFX_cat_meow", "SFX_guest_door_out", "고양이"),
    ("haru",   "송하루",   "Song Haru",  "#9fd08f", "guest",       True,  "haru",  "default;sad",                "SFX_guest_door_in", "SFX_guest_door_out", "취준생. day8 생사 분기(구 day7)"),
    ("sunha",  "유선하",   "Yu Sunha",   "#c58bd6", "guest",       True,  None,    "default;serious",            "SFX_guest_door_in", "SFX_guest_door_out", "기자"),
    ("hina",   "히나",     "Hina",       "#f2a0b5", "guest",       False, None,    "default",                    "SFX_guest_door_in", "SFX_guest_door_out", "2회 등장(구 day5·10)"),
    ("shiba",  "시바",     "Shiba",      "#d9c58a", "guest",       False, None,    "default",                    "SFX_guest_door_in", "SFX_guest_door_out", "말하는 시바견. 크리스 앞에선 멍멍"),
    ("rios",   "리오스",   "Rios",       "#b03060", "guest",       False, None,    "default",                    "SFX_guest_door_in", "SFX_guest_door_out", "루나와 같은 실험체. 최종일 등장"),
    ("volts",  "볼츠",     "Volts",      "#708090", "guest",       False, None,    "default",                    "SFX_guest_door_in", "SFX_guest_door_out", "1회 등장(구 day8)"),
    ("yuna",   "유나",     "Yuna",       "#a8d8ff", "cutscene",    False, None,    "default",                    None, None, "꿈/과거 회상 전용. 루나의 은인"),
    ("soldier","경비병",   "Guard",      "#888888", "cutscene",    False, None,    "default",                    None, None, "꿈 컷씬 전용"),
    ("radio",  "아나운서", "Announcer",  "#9ad0a0", "npc_street",  False, None,    "default",                    None, None, "TV·홀로그램·라디오 방송 전용 화자. 오브젝트 자동 대사에만 사용"),
    ("hound",  "하운드",   "Hound",      "#888888", "cutscene",    False, None,    "default",                    None, None, "연구소·꿈 컷씬 전용 하운드. FieldAnims.character_id 정식 참조 대상"),
    ("street_citizen_a", "행인 A", "Passerby A", "#b9c4d0", "npc_street", False, None, "default", None, None, "[QA/Day 99] 플레이어가 E로 시작하는 NPC 2인 대화의 화자 A"),
    ("street_citizen_b", "행인 B", "Passerby B", "#c7b8a8", "npc_street", False, None, "default", None, None, "[QA/Day 99] 플레이어가 E로 시작하는 NPC 2인 대화의 화자 B"),
    ("street_citizen_c", "행인 C", "Passerby C", "#aeb9a8", "npc_street", False, None, "default", None, None, "[QA/Day 99] 플레이어 상호작용형 1인 독백 검증용"),
    ("street_citizen_d", "행인 D", "Passerby D", "#b5a9c8", "npc_street", False, None, "default", None, None, "[QA/Day 99] 거리 선택지 검증용"),
]

# 파츠 애니메이션 캐릭터의 베이스 바디 (구엔진 character_anim.json base_body 이식)
BASE_BODY = { "luna": "luna_body", "chris": "chris_body", "port": "port_body",
              "bubi": "bubi_body", "aili": "aili_body", "samho": "samho_body" }

# ============================================================
# 3b. 마스터 — Expressions / ExpressionParts (표정 = 상태)
# 구엔진 character_anim.json 이식. 표정 1개 = mode 택1:
#   sprite     — 단일 스프라이트 1장 (현재 랜덤 손님·신규 캐릭터·추가 표정의 기본 방침)
#   parts_anim — 파츠 7종(eyes/eyebrows/upper_face/lower_face/body/extra/etc) 클립 조합
#                → 상세는 ExpressionParts에 1행 1파츠
# 입 움직임 규칙: lower_face의 loop가 always_on_dialogue면 "본인 대사 출력 중"에만
#                Dialogue 클립으로 전환, 끝나면 Loop 복귀 (구엔진 규칙 그대로)
# common = 랜덤 손님 공용 표정 세트(구엔진의 'default' 캐릭터). 지금은 전부 스프라이트 1장,
#          애니 리소스가 준비되면 mode만 parts_anim으로 바꾸고 파츠 행을 추가하면 됨.
# ============================================================
EXPR_COLS = ["character_id","expression","mode","sprite_key","note"]
EXPRESSIONS = [
    # --- 파츠 애니메이션 보유 6인 (기존 리소스 그대로 사용) ---
    ("luna",  "default",        "parts_anim", "",              ""),
    ("chris", "default",        "parts_anim", "",              ""),
    ("chris", "success",        "sprite",     "chris_success", "구엔진부터 스프라이트 1장"),
    ("chris", "fail",           "sprite",     "chris_fail",    ""),
    ("port",  "default",        "parts_anim", "",              ""),
    ("port",  "joy",            "parts_anim", "",              "파츠 일부만 교체(∅ 파츠는 default 유지)"),
    ("port",  "serious",        "parts_anim", "",              ""),
    ("port",  "anger",          "parts_anim", "",              "전 파츠 on_dialogue"),
    ("port",  "event_surprise", "sprite",     "port_surprise", ""),
    ("bubi",  "default",        "parts_anim", "",              ""),
    ("aili",  "default",        "parts_anim", "",              ""),
    ("aili",  "success",        "sprite",     "aili_success",  ""),
    ("aili",  "fail",           "sprite",     "aili_fail",     ""),
    ("aili",  "joy",            "sprite",     "aili_joy",      "[신규 표정 — 스프라이트 1장 방침]"),
    ("samho", "default",        "parts_anim", "",              ""),
    ("samho", "success",        "sprite",     "samho_success", ""),
    ("samho", "fail",           "sprite",     "samho_fail",    ""),
    ("samho", "drunk",          "sprite",     "samho_drunk",   ""),
    # --- 신규 스토리 캐릭터 — 전부 스프라이트 1장으로 시작 (추후 애니 전환 가능) ---
    ("tom",    "default", "sprite", "tom_default",    ""), ("tom",    "serious", "sprite", "tom_serious", ""),
    ("haru",   "default", "sprite", "haru_default",   ""), ("haru",   "sad",     "sprite", "haru_sad",    ""),
    ("sunha",  "default", "sprite", "sunha_default",  ""), ("sunha",  "serious", "sprite", "sunha_serious", ""),
    ("hina",   "default", "sprite", "hina_default",   ""),
    ("shiba",  "default", "sprite", "shiba_default",  ""),
    ("rios",   "default", "sprite", "rios_default",   ""),
    ("volts",  "default", "sprite", "volts_default",  ""),
    ("yuna",   "default", "sprite", "yuna_default",   "꿈 전용"),
    ("soldier","default", "sprite", "soldier_default","꿈 전용"),
    ("street_citizen_a", "default", "sprite", "qa_street_citizen_a_default", "[QA 플레이스홀더] 기본 전신 1장"),
    ("street_citizen_b", "default", "sprite", "qa_street_citizen_b_default", "[QA 플레이스홀더] 기본 전신 1장"),
    ("street_citizen_c", "default", "sprite", "qa_street_citizen_c_default", "[QA 플레이스홀더] 기본 전신 1장"),
    ("street_citizen_d", "default", "sprite", "qa_street_citizen_d_default", "[QA 플레이스홀더] 기본 전신 1장"),
    # --- common: 랜덤 손님 공용 세트 (구엔진 'default' 캐릭터 8종 — 현재는 스프라이트 1장) ---
    ("common", "default",   "sprite", "guest_default",   "추후 parts_anim 전환(구엔진 클립 default_* 존재)"),
    ("common", "joy",       "sprite", "guest_joy",       ""),
    ("common", "sadness",   "sprite", "guest_sadness",   ""),
    ("common", "anger",     "sprite", "guest_anger",     "구엔진은 on_dialogue 애니"),
    ("common", "surprise",  "sprite", "guest_surprise",  "구엔진은 body만 once(1회 재생 후 정지)"),
    ("common", "fear",      "sprite", "guest_fear",      ""),
    ("common", "annoyingB", "sprite", "guest_annoyingB", "재촉(파랑) — 인내심 50% 연출 후보"),
    ("common", "annoyingR", "sprite", "guest_annoyingR", "재촉(빨강) — 인내심 80% 연출 후보"),
]

# 1행 1파츠 — 구엔진 character_anim.json 클립·루프 그대로 이식 (∅ 파츠는 행 없음 = 비활성/기본 유지)
# loop 5종(플머 character_anim 구현 정합): always(상시 Intro→Loop 무한) / always_on_dialogue(평소 Loop=쉬는 입, 대화 중 Intro=말하는 입)
#   / on_dialogue(평소 정지, 대화 시작 시에만 재생) / once(1회 재생 후 마지막 프레임 고정 — 이벤트 표정) / none(파츠 숨김)
EXPRPART_COLS = ["character_id","expression","part","clip","loop"]
EXPRESSION_PARTS = [
    ("luna","default","eyes","luna_eyes_default","always"),
    ("luna","default","eyebrows","luna_eyebrows_default","always"),
    ("luna","default","upper_face","luna_upper_face_default","always"),
    ("luna","default","lower_face","luna_lower_face_default","always_on_dialogue"),
    ("luna","default","body","luna_body_default","always"),
    ("chris","default","eyes","chris_eyes_default","always"),
    ("chris","default","eyebrows","chris_eyebrows_default","always"),
    ("chris","default","upper_face","chris_upper_face_default","always"),
    ("chris","default","lower_face","chris_lower_face_default","always_on_dialogue"),
    ("chris","default","body","chris_body_default","always"),
    ("port","default","eyes","port_eyes_default","always"),
    ("port","default","eyebrows","port_eyebrows_default","always"),
    ("port","default","upper_face","port_upper_face_default","always"),
    ("port","default","lower_face","port_lower_face_default","always_on_dialogue"),
    ("port","default","body","port_body_default","always"),
    ("port","default","extra","port_extra_default","always"),
    ("port","default","etc","port_etc_default","always"),
    ("port","joy","eyebrows","port_eyebrows_joy","always"),
    ("port","joy","lower_face","port_lower_face_joy","always_on_dialogue"),
    ("port","joy","extra","port_extra_default","always"),
    ("port","joy","etc","port_etc_default","always"),
    ("port","serious","eyes","port_eyes_serious","always"),
    ("port","serious","eyebrows","port_eyebrows_serious","always"),
    ("port","serious","lower_face","port_lower_face_serious","always_on_dialogue"),
    ("port","serious","extra","port_extra_default","always"),
    ("port","serious","etc","port_etc_default","always"),
    ("port","anger","eyes","port_eyes_anger","on_dialogue"),
    ("port","anger","eyebrows","port_eyebrows_anger","on_dialogue"),
    ("port","anger","upper_face","port_upper_face_anger","on_dialogue"),
    ("port","anger","lower_face","port_lower_face_anger","on_dialogue"),
    ("port","anger","body","port_body_anger","on_dialogue"),
    ("port","anger","extra","port_extra_anger","on_dialogue"),
    ("port","anger","etc","port_etc_default","on_dialogue"),
    ("bubi","default","eyes","bubi_eyes_default","always"),
    ("bubi","default","upper_face","bubi_upper_face_default","always"),
    ("bubi","default","lower_face","bubi_lower_face_default","always_on_dialogue"),
    ("bubi","default","body","bubi_body_default","always"),
    ("aili","default","eyes","aili_eyes_default","always"),
    ("aili","default","eyebrows","aili_eyebrows_default","always"),
    ("aili","default","upper_face","aili_upper_face_default","always"),
    ("aili","default","lower_face","aili_lower_face_default","always_on_dialogue"),
    ("aili","default","body","aili_body_default","always"),
    ("samho","default","eyes","samho_eyes_default","always"),
    ("samho","default","eyebrows","samho_eyebrows_default","always"),
    ("samho","default","upper_face","samho_upper_face_default","always"),
    ("samho","default","lower_face","samho_lower_face_default","always_on_dialogue"),
    ("samho","default","body","samho_body_default","always"),
]

# ============================================================
# 3c. 마스터 — Cutscenes (연출 리소스 매니페스트)
# timeline = Unity Timeline 에셋(주력) / gif = 스파인 → GIF/시퀀스 삽입
# Steps의 timeline/gif 스텝은 반드시 이 표의 id를 참조 (검증기 대조)
# ============================================================
CUT_COLS = ["id","kind","resource_key","note"]
CUTSCENES = [
    # ===== 실존 타임라인 17종 (Assets/03.Scripts/Cutscene/UI Timeline/Playable — 26.07.18 에셋 검수 실등록) =====
    ("tl_intro",           "timeline", "INTRO",              "타이틀/도입 연출"),
    ("tl_intro_lab",       "timeline", "Intro lab",          "연구소 도입 — 꿈(습격) 연출 재료"),
    ("tl_find_luna",       "timeline", "FindLuna",           "루나 발견(구조) 회상"),
    ("tl_bubi_meet",       "timeline", "Bubi_meet",          "부비 첫 만남 — d3_aili_bubi 승격 후보"),
    ("tl_catmilk",         "timeline", "Catmilk",            "깔루아밀크 × 고양이 연출"),
    ("tl_samho_meet",      "timeline", "SmahoMeet",          "거리 시스템 제외 — 삼호·시바 첫 만남 별도 연출 컷신"),
    ("tl_samho_arrive",    "timeline", "SmahoArrive",        "거리 시스템 제외 — 삼호 생존 결과·도움 요청 별도 연출 컷신"),
    ("tl_samho_dead",      "timeline", "SamhoDead",          "거리 시스템 제외 — 삼호 사망 결과 별도 연출 컷신"),
    ("tl_finished_cosmo",  "timeline", "Finished_Cosmo",     "완성 연출 — 앵커 4종만 유지(절차적 합성 방침, 30종 전부 제작 안 함)"),
    ("tl_finished_martini","timeline", "Finished_DryMarthini",""),
    ("tl_finished_ginfizz","timeline", "Finished_Ginfizz",   ""),
    ("tl_finished_kahlua", "timeline", "Finished_kalua",     ""),
    ("tl_serve_cosmo",     "timeline", "Serve_Cosmopolitan", "서빙 연출 — 앵커 4종"),
    ("tl_serve_martini",   "timeline", "Serve_Dry",          ""),
    ("tl_serve_ginfizz",   "timeline", "Serve_Ginfizz",      ""),
    ("tl_serve_kahlua",    "timeline", "Serve_kalua",        ""),
    ("tl_serve_unknown",   "timeline", "Serve_Unknown",      "[보관] 'unknown 칵테일' 개념 폐지 — 2부 자유조합 실패 연출 전용 후보"),
    # ===== 신규 제작 예약 =====
    ("tl_raid_1",        "timeline", "TL_Dream_Raid1",      "[예약] 꿈 습격 1 — CutSceneLab 649프레임 재료"),
    ("tl_raid_2",        "timeline", "TL_Dream_Raid2",      "[예약] 꿈 습격 2"),
    ("tl_d13_rios",      "timeline", "TL_Day13_Rios",       "[예약] day13 리오스 등장"),
    ("gif_bubi_headjump","gif",      "Spine_Bubi_HeadJump", "[예약] 부비 머리 점프(스파인→GIF) — 현 fx cat_on_head 승격 후보"),
    # ===== 포스터 뷰 (kind=sprite — 이미지 1장 + 하단 텍스트) =====
    ("sp_parttime_poster",  "sprite", "Poster/parttime",   "알바 전단 포스터 뷰 — 이미지 발주 대기(가칭 키)"),
    ("sp_experiment_poster","sprite", "Poster/experiment", "임상시험 전단 포스터 뷰 — 이미지 발주 대기(가칭 키)"),
]

# ============================================================
# 3d. 마스터 — ResourceMap (데이터 키 ↔ 실제 에셋 경로, 26.07.18 검수 결과)
# 데이터의 id/clip 키와 엔진 리소스 폴더명이 다른 지점의 공식 매핑.
# status: OK(그대로 사용) / 일부(부분 존재) / 확인필요 / 결정필요
# ============================================================
RESMAP_COLS = ["kind","data_key","resource_path","status","note"]
RESOURCE_MAP = [
    ("expr_char", "chris",  "08.AddressableResource/Aniamtations/Chris",   "OK",       "Idle=Chris_idle·Default / 입 애니=Talk"),
    ("expr_char", "port",   "08.AddressableResource/Aniamtations/Port",    "OK",       "Idle·port_joy·port_serious·port_anger(Intro→Loop)·surprise 전부 존재"),
    ("expr_char", "aili",   "08.AddressableResource/Aniamtations/Ily",     "OK",       "폴더명 Ily ↔ 데이터 id aili 매핑. drink/success·fail.png = sprite 표정"),
    ("expr_char", "samho",  "08.AddressableResource/Aniamtations/3",       "OK",       "폴더명 3 ↔ 데이터 id samho 매핑"),
    ("expr_char", "bubi",   "08.AddressableResource/Aniamtations/bubi",    "OK",       ""),
    ("expr_char", "common", "08.AddressableResource/Aniamtations/Default", "일부",     "annoyingB/annoyingR만 존재 — 나머지 6표정은 스프라이트 1장 방침(미제작)"),
    ("expr_char", "luna",   "(없음)",                                      "미사용",   "바 내부는 1인칭 — 루나는 초상·스탠딩 없이 이름+대사만 출력. character_anim의 luna 항목은 예약"),
    ("bg",   "바 내부",     "01.Sprites/BG/Inside (Back/Middle/Fore 1280×720)", "결정필요", "포커스 카메라 팬 대응 — A: 줌인 팬(아트 추가 0) / B: Middle 좌우 연장(소폭). 리소스 폐기 아님"),
    ("bg",   "외부 거리",   "01.Sprites/BG/OutsideBG/Last Last (2880px 패럴랙스)", "OK", "Bar/House Entrance·Elevator·IntractObjects가 Spots 프리셋과 대응"),
    ("walk", "루나·삼호·부비·시바", "01.Sprites/Ch (노멀맵·마스크 포함)",   "OK",       "횡스크롤·컷씬용. SAMHO_DEAD 시트 = day4 사망 연출"),
    ("cutscene", "습격 회상 프레임", "01.Sprites/CutSceneLab (649프레임)",  "OK",       "tl_raid_1/2 제작 재료(hound_kill·yuna_attack·luna_move 등)"),
    ("cutscene", "제조·서빙 연출", "Resources/Cutscenes + Playable 17종",   "OK",       "Cutscenes 시트에 실등록 완료"),
    ("ui",   "제조 미니게임", "01.Sprites/UI (Shaking·Stur·Ingrediant·recipe 등)", "OK", "신 기믹도 동일 조작 — 풀세트 재사용"),
    ("ui",   "MethodButton", "01.Sprites/UI/MethodButton (9장)",            "폐기",     "제조 방식 선택 UI — 레시피 고정으로 불필요(인수인계 기폐기 결정)"),
    ("ui",   "ICE 버튼",     "01.Sprites/UI/Ingrediant/ICE_*.png",          "보관",     "얼음 기믹 보류 — 삭제 금지(스팀 확장 후보)"),
    ("icon", "재료 아이콘",  "Resources/UI/Ingredient (17종)",              "OK",       "disaronno→amaretto, coffee_liqueur→kahlua로 재사용. mint는 미사용(모히또 탈락). 신규분은 ⑤문서 제작 목록"),
    ("icon", "완성 칵테일",  "Resources/UI/Cocktail (6종)",                 "OK",       "4앵커+갓파더 유효, mojito는 로스터 탈락으로 미사용"),
]

# ============================================================
# 3e. 마스터 — FieldAnims (SD 필드 스프라이트 — 비주얼 이원화)
# 캐릭터 비주얼은 도메인 2벌: 씬의 phase가 자동 결정한다.
#   bar 도메인(바 내부)            → 고해상도 흉상/스탠딩 + Expressions/ExpressionParts (커피톡 스타일)
#   field 도메인(거리·집·꿈·컷씬)  → SD 픽셀 스프라이트 + 이 표의 동작 클립 (산나비/우산금지 스타일)
# 같은 캐릭터가 양쪽에 등장하면(삼호·시바·부비 등) 두 시스템에 각각 행이 존재하면 끝 —
# 대본(Steps)은 도메인을 모르므로 어느 쪽이든 같은 문법으로 쓴다.
# ============================================================
FIELD_COLS = ["character_id","action","resource_key","status","note"]
FIELD_ANIMS = [
    ("luna",  "idle",       "Ch/luna/luna_v2-idle_Sheet",       "OK",     "노멀맵·마스크 포함"),
    ("luna",  "walk",       "Ch/luna/luna_v2_walk_Sheet",       "OK",     ""),
    ("luna",  "run",        "Ch/luna/luna_v2-run_Sheet",        "OK",     ""),
    ("luna",  "walk_w_cat", "Ch/luna/luna_v2-walk_w_cat-Sheet", "OK",     "부비 안고 걷기"),
    ("samho", "idle",       "Ch/3/samho-IDLE_Sheet",            "OK",     ""),
    ("samho", "idle_blink", "Ch/3/samho-IDLE_BLINK_Sheet",      "OK",     ""),
    ("samho", "run",        "Ch/3/samho_RUN3_-Sheet",           "OK",     "day4 생존루트 — 꽃 들고 달려옴"),
    ("samho", "dead",       "Ch/3/SAMHO_DEAD-Sheet",            "OK",     "day4 사망 연출"),
    ("samho", "dead_opening","Ch/3/SAMHO_DEAD-OPNING-Sheet",    "OK",     ""),
    ("bubi",  "idle",       "Ch/Bubi/cat_animation-Sheet",      "OK",     "거리 고양이 전신 표시·배회"),
    ("shiba", "idle",       "Ch/Shiba/shi_Bar-Sheet",           "OK",     "거리 NPC 겸용"),
    ("yuna",  "raid_set",   "CutSceneLab/08.yuna_attack 등",    "OK",     "꿈 컷씬 — 습격 시퀀스 프레임에 포함"),
    ("soldier","raid_set",  "CutSceneLab/04.soilder_idle",      "OK",     "꿈 컷씬"),
    ("hound", "raid_set",   "CutSceneLab/03·05·06·07 (hound_*)", "OK",    "꿈 컷씬 — 하운드(캐릭터 시트엔 없음, 컷씬 전용)"),
    ("chris", "idle",       "(미제작)",                          "신규필요", "테라스 대화·day1 퇴근길 동행에 필요 — 대화를 초상만으로 처리하면 불필요(연출 결정 대기)"),
    ("haru",  "idle",       "(미제작)",                          "신규필요", "노말엔딩1 퇴근길 연출에 필요"),
    ("rios",  "idle",       "(미제작)",                          "신규필요", "배드엔딩 엘리베이터 연출에 필요"),
    ("street_citizen_a", "idle", "QA/Street/citizen_a_idle", "플레이스홀더", "Day 99 NPC 대화 구현 검증용"),
    ("street_citizen_b", "idle", "QA/Street/citizen_b_idle", "플레이스홀더", "Day 99 NPC 대화 구현 검증용"),
    ("street_citizen_c", "idle", "QA/Street/citizen_c_idle", "플레이스홀더", "Day 99 NPC 독백 구현 검증용"),
    ("street_citizen_d", "idle", "QA/Street/citizen_d_idle", "플레이스홀더", "Day 99 선택지 구현 검증용"),
]

# ── 랜덤 손님 공용 외형 — 조합형 카탈로그 (성별 일치 조합) ──
# 각 항목은 character_anim 항목과 같은 이원 구조: 지금은 mode=sprite(한 장),
# 애니 전환 시 그 행의 mode를 parts_anim으로 바꾸고 파츠 시트를 추가하면 된다(스키마 변경 없음).
# sprite 키는 가칭 — 완성 아트 임포트 시 실제 리소스 키로 치환. note에 실물 파일명 매핑을 적는다.
# emotions(v2.9) — 표정별 교체 스프라이트 맵 "표정:키; 표정:키" (예: "joy:Guest/eyes_m_1_joy; anger:Guest/eyes_m_1_anger").
#   랜덤 손님의 표정이 정해지면(barks.expression → bark_situations 기본 → default) 각 파트에서 그 표정 키를 찾아
#   있으면 그 스프라이트로 교체, 없으면 기본 sprite 유지. 전부 공란 = 현행 '표정 고정'과 동일 동작.
# v3.3 — 슬롯 9종 확장(실물 남자 파츠 기준) + is_default(기본 조합 마킹) + GuestBodyExclusions(금지 쌍).
#   슬롯 종류·레이어 순서는 아래 GB_SLOT_ORDER가 정본 — build 검증과 엔진 렌더러가 같은 목록을 쓴다.
#   필수 슬롯은 성별·성격마다 후보 1개 이상 필요, 선택 슬롯은 추첨 시 '없음' 후보가 항상 붙는다.
GB_SLOT_ORDER = ["body", "outfit", "outerwear", "necklace", "eyes", "eyebrows", "mouth", "hair", "arm_accessory"]
GB_REQUIRED   = ["body", "outfit", "eyes", "eyebrows", "mouth", "hair"]          # 하나 필수
GB_OPTIONAL   = ["outerwear", "necklace", "arm_accessory"]                        # 없음 가능
GB_JSON_KEY   = {"body": "bodies", "outfit": "outfits", "outerwear": "outerwears",
                 "necklace": "necklaces", "eyes": "eyes", "eyebrows": "eyebrows",
                 "mouth": "mouths", "hair": "hairs", "arm_accessory": "arm_accessories"}
GBODY_COLS = ["part","id","gender","personalities","mode","sprite","emotions","weight","status","note","is_default"]
GUEST_BODIES = [
    ("body",    "body_m",      "m", "", "sprite", "Guest/body_m",      "", 1, "완료",   "실물 body.png — 얼굴·몸통·코·패널 라인 한 장", True),
    ("body",    "body_f",      "f", "", "sprite", "Guest/body_f",      "", 1, "제작중", "여성 바디 — 남자 body.png와 같은 규격으로 제작", True),
    ("outfit",  "outfit_m_1",  "m", "", "sprite", "Guest/outfit_m_1",  "", 1, "완료",   "실물 Shirt_1 — 프린트 티(반소매)", True),
    ("outfit",  "outfit_m_2",  "m", "", "sprite", "Guest/outfit_m_2",  "", 1, "완료",   "실물 Shirt_1(2) — 색 변형", ""),
    ("outfit",  "outfit_m_3",  "m", "", "sprite", "Guest/outfit_m_3",  "", 1, "완료",   "실물 Shirt_1(3) — 색 변형", ""),
    ("outfit",  "outfit_m_4",  "m", "", "sprite", "Guest/outfit_m_4",  "", 1, "완료",   "실물 Shirt_1(4) — 색 변형", ""),
    ("outfit",  "outfit_m_5",  "m", "", "sprite", "Guest/outfit_m_5",  "", 1, "완료",   "실물 Shirt_1(5) — 색 변형", ""),
    ("outfit",  "outfit_m_6",  "m", "", "sprite", "Guest/outfit_m_6",  "", 1, "완료",   "실물 Shirt_2 — 소매 있음", ""),
    ("outfit",  "outfit_m_7",  "m", "", "sprite", "Guest/outfit_m_7",  "", 1, "완료",   "실물 Shirt_3 — 민소매(팔 액세서리 허용)", ""),
    ("outfit",  "outfit_m_8",  "m", "", "sprite", "Guest/outfit_m_8",  "", 1, "완료",   "실물 Shirt_3(2) — 민소매 색 변형(팔 액세서리 허용)", ""),
    ("outfit",  "outfit_f_1",  "f", "", "sprite", "Guest/outfit_f_1",  "", 1, "제작중", "민소매", True),
    ("outfit",  "outfit_f_2",  "f", "", "sprite", "Guest/outfit_f_2",  "", 1, "제작중", "아우터", ""),
    ("outerwear", "outerwear_m_1", "m", "", "sprite", "Guest/outerwear_m_1", "", 1, "완료", "실물 Acc_2 — 어깨 재킷", ""),
    ("necklace",  "necklace_m_1",  "m", "", "sprite", "Guest/necklace_m_1",  "", 1, "완료", "실물 Acc_3 — 목걸이", ""),
    ("eyes",    "eyes_m_1",    "m", "", "sprite", "Guest/eyes_m_1",    "", 1, "완료",   "실물 Eye_1", True),
    ("eyes",    "eyes_m_2",    "m", "", "sprite", "Guest/eyes_m_2",    "", 1, "완료",   "실물 Eye_2", ""),
    ("eyes",    "eyes_m_3",    "m", "", "sprite", "Guest/eyes_m_3",    "", 1, "완료",   "실물 Eye_3", ""),
    ("eyes",    "eyes_f_1",    "f", "", "sprite", "Guest/eyes_f_1",    "", 1, "제작중", "", True),
    ("eyes",    "eyes_f_2",    "f", "", "sprite", "Guest/eyes_f_2",    "", 1, "제작중", "", ""),
    ("eyebrows", "eyebrows_m_1", "m", "", "sprite", "Guest/eyebrows_m_1", "", 1, "완료",   "실물 Eyebrow_1", True),
    ("eyebrows", "eyebrows_m_2", "m", "", "sprite", "Guest/eyebrows_m_2", "", 1, "완료",   "실물 Eyebrow_2", ""),
    ("eyebrows", "eyebrows_m_3", "m", "", "sprite", "Guest/eyebrows_m_3", "", 1, "완료",   "실물 Eyebrow_3", ""),
    ("eyebrows", "eyebrows_f_1", "f", "", "sprite", "Guest/eyebrows_f_1", "", 1, "신규필요", "여성 눈썹 — 아트 대기", True),
    ("mouth",   "mouth_m_1",   "m", "", "sprite", "Guest/mouth_m_1",   "", 1, "완료",   "실물 mouth_1", True),
    ("mouth",   "mouth_m_2",   "m", "", "sprite", "Guest/mouth_m_2",   "", 1, "완료",   "실물 mouth_2", ""),
    ("mouth",   "mouth_m_3",   "m", "", "sprite", "Guest/mouth_m_3",   "", 1, "완료",   "실물 mouth_3", ""),
    ("mouth",   "mouth_f_1",   "f", "", "sprite", "Guest/mouth_f_1",   "", 1, "신규필요", "여성 입 — 아트 대기", True),
    ("hair",    "hair_m_1",    "m", "", "sprite", "Guest/hair_m_1",    "", 1, "완료",   "실물 Hair_1", True),
    ("hair",    "hair_m_2",    "m", "", "sprite", "Guest/hair_m_2",    "", 1, "완료",   "실물 Hair_2", ""),
    ("hair",    "hair_f_1",    "f", "", "sprite", "Guest/hair_f_1",    "", 1, "제작중", "", True),
    ("hair",    "hair_f_2",    "f", "", "sprite", "Guest/hair_f_2",    "", 1, "제작중", "", ""),
    ("arm_accessory", "arm_accessory_m_1", "m", "", "sprite", "Guest/arm_accessory_m_1", "", 1, "완료", "실물 Acc_1 — 팔 액세서리, 민소매 전용(GuestBodyExclusions 참조)", ""),
]

# ── 랜덤 손님 외형 금지 조합 — 한 행 = 금지 쌍 하나, (a,b)=(b,a) ──
# 팔 액세서리는 민소매(outfit_m_7·8)에서만 노출 — 소매 있는 상의 전부와 금지.
# 새 상의를 추가할 때 민소매가 아니면 여기에도 행을 추가한다(이미지 판단이라 자동 검증 불가).
GBEXCL_COLS = ["part_a", "part_b", "note"]
GUEST_BODY_EXCLUSIONS = [
    ("arm_accessory_m_1", "outfit_m_1", "민소매 아님 — 소매가 팔 액세서리를 가린다"),
    ("arm_accessory_m_1", "outfit_m_2", "민소매 아님"),
    ("arm_accessory_m_1", "outfit_m_3", "민소매 아님"),
    ("arm_accessory_m_1", "outfit_m_4", "민소매 아님"),
    ("arm_accessory_m_1", "outfit_m_5", "민소매 아님"),
    ("arm_accessory_m_1", "outfit_m_6", "민소매 아님"),
]

def parse_emotions(raw):
    """'joy:키; anger:키' → dict. 공란 → {}. 형식이 깨진 조각은 ('!bad', 조각)으로 반환해 검증에서 잡는다."""
    out, bad = {}, []
    for piece in str(raw or "").split(";"):
        piece = piece.strip()
        if not piece: continue
        if ":" not in piece:
            bad.append(piece); continue
        k, v = piece.split(":", 1)
        out[k.strip()] = v.strip()
    return out, bad

PERS_COLS = ["id","name_ko","name_en","tip_mult","patience_mult","think_chance","note"]
PERSONALITIES = [  # 26.07.17 PD 시트('랜덤 (일반) 손님 대사') 기준 5종. 배율은 가안
    # think_chance: 코스터 드롭 후 order_think를 재생할 확률(0~1). 실패하면 ask_order → 바로 order.
    # 카메오(단골)는 전용 order_think 행이 대사 그 자체이므로 이 값과 무관하게 항상 재생.
    ("gentle", "온화형", "Gentle", 1.0, 1.2, 1.0, "예의 바르고 참을성 많음. 부정 반응도 조심스러움"),
    ("rough",  "거친형", "Rough",  1.1, 0.8, 0.3, "입이 거침. 빨리 안 오면 폭발, 맛있으면 화끈하게 리액션. 주문도 대부분 즉답"),
    ("touchy", "예민형", "Touchy", 1.0, 0.9, 0.8, "(구)짜증난&예민한 손님. 응대 하나하나에 민감"),
    ("quiet",  "과묵형", "Quiet",  1.1, 1.1, 0.5, "말수 최소. '...'가 대사의 절반"),
    ("chatty", "수다형", "Chatty", 1.0, 1.0, 1.0, "혼잣말·너스레 많음. 대사가 김"),
]

# voice_id = 성격유형 id 또는 캐릭터 id(카메오 전용). 공란 = 전 유형 공용 폴백
# situation: call(첫 호출) call_urge(재촉·인내심50%) call_final(최종 재촉·80%) order reorder(2차 주문)
#            serve_urge/serve_final(서빙 대기 경고) react_*(5등급) wrong_receive/wrong_drink
#            leave_coaster/leave_serve(이탈) drunk_enter(3잔째) drunk_vomit(토함) bye_good/bye_bad(퇴장)
#            serve_thanks(수령 감사 — 마시기 전, 명세서 §3.7 ①단계)
BARK_COLS = ["voice_id","situation","expression","text_ko","text_en","weight"]
BARKS = [
    # ===== 온화형 (PD 시트 원문) =====
    ("gentle", "call", "",       "한 명이요, 자리 있을까요?", "Table for one, please?", 1),
    ("gentle", "call", "",       "안녕하세요!", "Hello!", 1),
    ("gentle", "call", "",       "마스터, 자리 있나요?", "Master, got a seat?", 1),
    ("gentle", "call", "",       "안녕하세요.", "Good evening.", 1),
    ("gentle", "call", "",       "실례합니다...", "Excuse me...", 1),
    ("gentle", "call_urge", "",  "저기요...", "Um, excuse me...", 1),
    ("gentle", "call_urge", "",  "음... 자리 없나요?", "Hm... no seats?", 1),
    ("gentle", "call_urge", "",  "마스터?", "Master?", 1),
    ("gentle", "call_final", "", "저기요, 마스터.", "Excuse me, master.", 1),
    ("gentle", "call_final", "", "왜 안 오시지...", "Why isn't anyone coming...", 1),
    ("gentle", "call_final", "", "자리가 없나?", "No seats, maybe?", 1),
    ("gentle", "order", "",      "{cocktail} 부탁드립니다.", "A {cocktail}, please.", 1),
    ("gentle", "order", "",      "음, {cocktail} 주문할게요.", "Hm, I'll order a {cocktail}.", 1),
    ("gentle", "order", "",      "{cocktail} 주세요.", "One {cocktail}, please.", 1),
    ("gentle", "react_excellent", "", "맛있다... 감사합니다.", "Delicious... thank you.", 1),
    ("gentle", "react_excellent", "", "맛있네요! 감사합니다.", "Tastes great! Thank you.", 1),
    ("gentle", "react_good", "", "아, 이거 좋네요.", "Oh, this is nice.", 1),
    ("gentle", "react_poor", "", "맛이 좀, ... 이상한 것 같은데요.", "The taste is a little... off, I think.", 1),
    ("gentle", "react_poor", "", "... 주문이 혹시 잘못 들어갔나요?", "...Did my order get mixed up?", 1),
    ("gentle", "wrong_drink", "","저 {cocktail} 시켰는데... 맞다고요?", "I ordered a {cocktail}... this is it?", 1),
    ("gentle", "reorder", "",    "이번에는 {cocktail} 주문할게요.", "I'll order a {cocktail} this time.", 1),
    ("gentle", "reorder", "",    "{cocktail} 하나 더 부탁드립니다.", "One more {cocktail}, please.", 1),
    ("gentle", "reorder", "",    "이번에는 {cocktail} 하나 주세요.", "This time, a {cocktail}, please.", 1),
    ("gentle", "bye_good", "",   "감사합니다, 잘 마셨어요!", "Thank you, that was lovely!", 1),
    # ===== 거친형 (PD 시트 원문 — 콘텐츠 등급 검토 필요 표현 포함) =====
    ("rough", "call", "",        "어이, 자리 있어?", "Oi, got a seat?", 1),
    ("rough", "call", "",        "이봐! 마스터!", "Hey! Master!", 1),
    ("rough", "call", "",        "한 명.", "One.", 1),
    ("rough", "call_urge", "",   "마스터!!!", "MASTER!!!", 1),
    ("rough", "call_urge", "",   "야!!! 손님 무시해?!", "HEY!!! You ignoring a customer?!", 1),
    ("rough", "call_urge", "",   "자리 있냐고 물었잖아. 대답 안 해?", "I asked if there's a seat. No answer?", 1),
    ("rough", "call_final", "",  "빠져가지고... 쯧.", "Tch... useless.", 1),
    ("rough", "call_final", "",  "술이나 파는 새끼가 건방지게...", "A booze peddler with an attitude...", 1),
    ("rough", "call_final", "",  "...", "...", 1),
    ("rough", "order", "",       "{cocktail} 한 잔 말아와.", "Mix me a {cocktail}.", 1),
    ("rough", "order", "",       "{cocktail} 하나.", "One {cocktail}.", 1),
    ("rough", "order", "",       "{cocktail}.", "{cocktail}.", 1),
    ("rough", "order", "",       "어이, 마스터! {cocktail} 하나!", "Oi, master! One {cocktail}!", 1),
    ("rough", "react_excellent", "", "크하! 이거 맛 죽이네!", "HAH! This hits!", 1),
    ("rough", "react_excellent", "", "이게 술이지! 크흐, 좀 살겠다!", "Now THAT's a drink! Whew, I'm alive again!", 1),
    ("rough", "react_good", "",  "이 맛에 돈 쓰지. 오늘 좀 괜찮은데?", "This is what I pay for. Not bad tonight?", 1),
    ("rough", "react_poor", "",  "야, 돈 도로 뱉어. 돈 내고 이딴 건 못 마셔.", "Hey, spit my money back. I'm not paying for this.", 1),
    ("rough", "react_sewage", "","장난해?! 이걸 어떻게 마시라는 거야!!", "You kidding?! How am I supposed to drink this?!", 1),
    ("rough", "react_sewage", "","아이 씹, 이딴 걸 술이라고 내와?", "The hell — you call this booze?", 1),
    ("rough", "react_sewage", "","쳐맞고 싶어서 환장했나... 맛이 왜 이래?", "You asking for a beating... why does it taste like this?", 1),
    ("rough", "reorder", "",     "이봐, 마스터! {cocktail} 한 잔 말아줘!", "Oi, master! Mix me another {cocktail}!", 1),
    ("rough", "reorder", "",     "{cocktail} 하나 더!", "One more {cocktail}!", 1),
    ("rough", "reorder", "",     "{cocktail} 주쇼!", "{cocktail}, now!", 1),
    # ===== 예민형 ((구)짜증난&예민한 손님 — PD 시트 원문) =====
    ("touchy", "call", "",       "안녕하세요.", "Hello.", 1),
    ("touchy", "call", "",       "1명이요.", "One, please.", 1),
    ("touchy", "call", "",       "...", "...", 1),
    ("touchy", "call_urge", "",  "사람 무시하나 지금...", "Am I invisible or something...", 1),
    ("touchy", "call_urge", "",  "여기 손님 응대가 왜 이래?", "What's with the service here?", 1),
    ("touchy", "call_urge", "",  "이봐요, 마스터.", "Hey, master.", 1),
    ("touchy", "call_final", "", "지금 뭐 하자는 거야? 사람 안 보여?", "What is this? Can't you see me?", 1),
    ("touchy", "call_final", "", "뭐 어쩌라는 건지...", "What am I even supposed to do...", 1),
    ("touchy", "call_final", "", "마스터! 손님 응대 안 해요?", "Master! No service here?", 1),
    ("touchy", "order", "",      "{cocktail} 하나 줘봐요.", "Give me a {cocktail}.", 1),
    ("touchy", "order", "",      "{cocktail}.", "{cocktail}.", 1),
    ("touchy", "order", "",      "{cocktail} 하나.", "One {cocktail}.", 1),
    ("touchy", "react_excellent", "", "좋네요, 고마워요.", "It's good. Thanks.", 1),
    ("touchy", "react_good", "", "... 괜찮네.", "...Not bad.", 1),
    ("touchy", "react_good", "", "나쁘지 않네요.", "Not bad at all.", 1),
    ("touchy", "react_poor", "", "이걸 칵테일이라고 내온 건 아니죠?", "You're not calling this a cocktail, are you?", 1),
    ("touchy", "react_sewage", "","무슨 이딴 걸 칵테일이라고...", "What kind of cocktail is this...", 1),
    ("touchy", "react_sewage", "","시간이랑 돈 다 버렸네요, 이걸로 장사가 돼요?", "Wasted my time and money. How is this place in business?", 1),
    ("touchy", "react_sewage", "","물 있어요? 입 헹구려고요. 뭔 이런 걸 팔고 있어...", "Got water? I need to rinse my mouth. Unbelievable...", 1),
    ("touchy", "reorder", "",    "{cocktail} 하나 줘봐요.", "Give me a {cocktail}.", 1),
    ("touchy", "reorder", "",    "추가 주문할게요, {cocktail}으로.", "I'll add an order — {cocktail}.", 1),
    ("touchy", "reorder", "",    "{cocktail} 하나 더.", "One more {cocktail}.", 1),
    # ===== 과묵형 (PD 시트 원문) =====
    ("quiet", "call", "",        "...", "...", 1),
    ("quiet", "call", "",        "마스터.", "Master.", 1),
    ("quiet", "call", "",        "한 명.", "One.", 1),
    ("quiet", "call_urge", "",   "...", "...", 1),
    ("quiet", "call_urge", "",   "마스터, 자리 있나?", "Master, any seats?", 1),
    ("quiet", "call_urge", "",   "마스터?", "Master?", 1),
    ("quiet", "call_final", "",  "...", "...", 1),
    ("quiet", "call_final", "",  "마스터, 안내 좀 해줬으면 하는데.", "Master, I'd like to be seated.", 1),
    ("quiet", "call_final", "",  "마스터, 지금 바쁩니까?", "Master, are you busy?", 1),
    ("quiet", "order", "",       "{cocktail} 하나.", "One {cocktail}.", 1),
    ("quiet", "order", "",       "{cocktail}으로.", "{cocktail}, then.", 1),
    ("quiet", "order", "",       "{cocktail}.", "{cocktail}.", 1),
    ("quiet", "react_excellent", "", "잘 마셨습니다.", "That was good.", 1),
    ("quiet", "react_good", "",  "맛있네요.", "Tasty.", 1),
    ("quiet", "react_good", "",  "감사합니다.", "Thank you.", 1),
    ("quiet", "react_poor", "",  "... 맛이 좀, 그렇군요.", "...The taste is, well.", 1),
    ("quiet", "react_poor", "",  "... 뭔가 맛이 이상한데.", "...Something's off.", 1),
    ("quiet", "react_sewage", "","...", "...", 1),
    ("quiet", "reorder", "",     "{cocktail} 하나 부탁드립니다.", "One {cocktail}, please.", 1),
    ("quiet", "reorder", "",     "이번에는 {cocktail}.", "This time, {cocktail}.", 1),
    ("quiet", "reorder", "",     "추가로 {cocktail} 한 잔 부탁합니다.", "Another {cocktail}, please.", 1),
    # ===== 수다형 (PD 시트 원문) =====
    ("chatty", "call", "",       "마스터! 한 명이요~", "Master! One, please~", 1),
    ("chatty", "call", "",       "아이, 나 한 잔만 걸치고 갈게. 마스터!", "C'mon, just one quick drink. Master!", 1),
    ("chatty", "call", "",       "혼자 왔는데 자리 있을까요? 다리가 좀 아파서.", "Alone tonight — got a seat? My legs are killing me.", 1),
    ("chatty", "call_urge", "",  "마스터? 여기 손님 받아주세요.", "Master? Customer over here.", 1),
    ("chatty", "call_urge", "",  "... 왜 응대를 안 해주신담?", "...Why is no one taking me?", 1),
    ("chatty", "call_urge", "",  "마스터? 못 들었나...", "Master? Maybe he didn't hear...", 1),
    ("chatty", "call_final", "", "...", "...", 1),
    ("chatty", "call_final", "", "마스터!", "Master!", 1),
    ("chatty", "call_final", "", "아직도 자리 없어요? 아니, 사람을 원래 기다리게 하고 그래?", "Still no seat? Do you always keep people waiting?", 1),
    ("chatty", "order", "",      "보자보자, 뭘 마시는 게 좋을까... 일단 {cocktail} 하나!", "Let's see, what should I drink... one {cocktail} for now!", 1),
    ("chatty", "order", "",      "저번에 그거 괜찮았는데. {cocktail}? 그거 하나 주세요.", "That one was good last time. {cocktail}? One of those.", 1),
    ("chatty", "order", "",      "{cocktail}! {cocktail} 하나 주세요.", "{cocktail}! One {cocktail}, please.", 1),
    ("chatty", "react_excellent", "", "아, 이거지! 맛이 좋은데요? 오늘 술이 잘 받네...", "That's the stuff! Tastes great! Drinks are landing tonight...", 1),
    ("chatty", "react_good", "", "이거만한 게 또 없다니까? 고마워요.", "Nothing beats this, I'm telling you. Thanks.", 1),
    ("chatty", "react_poor", "", "음, 오늘 컨디션이 별로인가? 맛이 이상하네...", "Hm, am I off today? Tastes weird...", 1),
    ("chatty", "react_poor", "", "저기, 마스터? 마스터! 이거 잘못 만들었어요?", "Excuse me, master? Master! Did you make this wrong?", 1),
    ("chatty", "react_sewage", "","이거 맛이 왜 이래?... 마스터, 제대로 만든 거 맞죠?", "Why does it taste like this?... You made it right, right?", 1),
    ("chatty", "reorder", "",    "이번에는 {cocktail} 주문할게요~", "This time I'll order a {cocktail}~", 1),
    ("chatty", "reorder", "",    "{cocktail} 맛 괜찮아요? 궁금한데. 이거 추가로 주문할게요.", "Is the {cocktail} good? I'm curious. Add that one.", 1),
    ("chatty", "reorder", "",    "{cocktail} 이거 괜찮다고 하던데, 이거 하나 줘요.", "Heard the {cocktail}'s decent — give me one.", 1),
    ("chatty", "bye_good", "",   "감사합니다, 잘 마셨어요! 다음에도 잘 좀 부탁할게요.", "Thanks, that was great! Take care of me next time too.", 1),
    # ===== 공용 폴백 (성격별 대사가 없는 상황 커버 — 추후 성격별로 채우면 자동 대체) =====
    ("", "serve_urge", "",   "음료 아직인가요...?", "Is the drink coming...?", 1),
    ("", "serve_urge", "sadness", "목이 타는데...", "I'm parched...", 1),   # 행 단위 표정 예시 — 지친 표정
    ("", "serve_final", "",  "만들어 놓고 왜 안 줘요?", "It's made — why aren't you serving it?", 1),
    ("", "serve_final", "",  "이제 그냥 갈까...", "Maybe I should just go...", 1),
    ("", "react_decent", "", "뭐... 마실 만하네요.", "Well... it's drinkable.", 1),
    ("", "leave_coaster", "","됐어요. 다른 데 갑니다.", "Forget it. I'm going somewhere else.", 1),
    ("", "leave_serve", "",  "만들어 놓고 안 주는 건 무슨 경우야!", "You made it and won't serve it?!", 1),
    ("", "wrong_receive", "","...내가 시킨 게 맞나?", "...Is this what I ordered?", 1),
    ("", "wrong_drink", "",  "역시 내가 시킨 게 아니잖아!!", "I knew it — this is NOT what I ordered!!", 1),
    ("", "drunk_enter", "",  "히끅... 한 잔 더어...", "Hic... one more roundsh...", 1),
    # ===== v2.3 신설 (26.07.24 명세서 대응 — 시드 초안, 이기현 검수 대상) =====
    ("", "drunk_vomit", "",   "우웁... 미안... 해요...", "Urp... s-sorry...", 1),
    ("", "drunk_vomit", "",   "속이... 안 좋아...", "My stomach... not good...", 1),
    ("gentle", "serve_thanks", "", "감사합니다!", "Thank you!", 1),
    ("rough", "serve_thanks", "",  "어, 왔군.", "'Bout time.", 1),
    ("touchy", "serve_thanks", "", "...흘리신 건 아니죠?", "...You didn't spill any, right?", 1),
    ("quiet", "serve_thanks", "",  "고맙소.", "Thanks.", 1),
    ("chatty", "serve_thanks", "", "오~ 드디어! 기다렸다구요!", "Ooh, finally! Been waiting!", 1),
    ("gentle", "react_sewage", "fear", "저기... 이거 상한 건 아니죠...?", "Um... this isn't spoiled, is it...?", 1),   # 겁먹은 표정
    # ===== 포트 카메오 전용 풀 세트 (v2.3 — 카메오는 공용 폴백 금지, 명세서 §2.2) =====
    ("port", "call_urge", "",    "바쁜가? 천천히 해도 되네.", "Busy night? Take your time.", 1),
    ("port", "call_final", "",   "허허... 오늘은 글렀나 보군.", "Well... perhaps not tonight.", 1),
    ("port", "order_think", "",  "뭘 마실지는 정해져 있네만.", "As if I'd order anything else.", 1),
    ("port", "order", "",        "{cocktail}, 정석대로 부탁하네.", "{cocktail}, by the book.", 1),
    ("port", "serve_urge", "",   "재촉은 않겠네. 다만 잊진 말게나.", "No rush. Just don't forget me.", 1),
    ("port", "serve_final", "",  "슬슬 걱정되는군.", "Now I'm getting worried.", 1),
    ("port", "serve_thanks", "", "고맙네.", "Much obliged.", 1),
    ("port", "react_excellent", "", "...크리스보다 낫군. 이건 비밀일세.", "...Better than Chris's. Our secret.", 1),
    ("port", "react_good", "",   "좋군. 정석이야.", "Good. By the book.", 1),
    ("port", "react_decent", "", "무난하군.", "Passable.", 1),
    ("port", "react_poor", "",   "...연습이 더 필요하겠어.", "...You need more practice.", 1),
    ("port", "react_sewage", "serious", "이건 기사감이군. '신입 바텐더, 손님을 독살하려 들다'.", "This is front-page material: 'Rookie bartender attempts poisoning.'", 1),   # 카메오는 자기 표정 세트 사용 가능
    ("port", "wrong_receive", "","흠? 내가 시킨 게 이거였나.", "Hm? Is this what I ordered?", 1),
    ("port", "wrong_drink", "",  "역시 아니군. 나는 {cocktail}을 시켰네만.", "As I thought. I ordered a {cocktail}.", 1),
    ("port", "bye_good", "",     "잘 마셨네. 크리스에게 안부 전해주게.", "A fine drink. Give Chris my regards.", 1),
    ("port", "bye_bad", "",      "오늘은 운이 없었다고 해두지.", "Let's call it an off night.", 1),
    ("port", "leave_coaster", "","다음에 다시 오지.", "I'll come back another time.", 1),
    ("port", "leave_serve", "",  "시간이 다 됐군. 아쉽지만.", "I'm out of time. A pity.", 1),
    ("port", "idle", "",         "이 바는 변한 게 없군.", "This bar never changes.", 1),
    ("", "bye_good", "",     "다음에 또 올게요!", "See you next time!", 1),
    ("", "bye_bad", "",      "뭐 이딴 가게가 다 있어?!", "What kind of bar is this?!", 1),
    # ===== 카메오(캐릭터 voice) =====
    ("port", "call", "",     "크리스는 안에 있나? ...아, 자네한테 시키면 되지.", "Is Chris in? ...Ah, never mind. I can order from you.", 1),
    # ===== 루나(바텐더) 응대 라인 — 주문 순서: 루나 질문(ask_order) → 손님 고민(order_think) → 주문(order) =====
    ("luna", "ask_order", "",  "주문하시겠습니까?", "May I take your order?", 1),
    ("luna", "ask_order", "",  "무엇으로 드릴까요?", "What can I get you?", 1),
    ("luna", "ask_order", "",  "메뉴, 보시겠어요?", "Would you like the menu?", 1),
    # ===== 주문 고민 (order_think) =====
    ("", "order_think", "", "흠... 잠시만요.", "Hmm... one moment.", 1),
    ("", "order_think", "", "어디 보자...", "Let's see...", 1),
    ("gentle", "order_think", "", "고민되네요... 뭐가 좋을까.", "Tough choice... what's good tonight?", 1),
    ("rough", "order_think", "", "뭐가 있더라...", "What do you even got...", 1),
    ("quiet", "order_think", "", "....", "....", 1),
    ("chatty", "order_think", "", "아~ 뭐 마시지? 이럴 때가 제일 행복하다니까요.", "Ahh, what to drink? Honestly the best part of the night.", 1),
    # ===== 대기·음용 중 잡담 (idle — 한적한 바의 공기) =====
    ("", "idle", "", "(창밖을 물끄러미 바라본다)", "(stares out the window)", 1),
    ("", "idle", "", "오늘도 밤이 기네...", "Long night again...", 1),
    ("", "idle", "", "이 동네도 많이 변했지...", "This block's changed a lot...", 1),
    ("gentle", "idle", "", "여긴 음악이 참 좋네요.", "The music here is really nice.", 1),
    ("rough", "idle", "", "...조용해서 좋군, 여긴.", "...Quiet. I like that.", 1),
    ("touchy", "idle", "", "(손끝으로 카운터를 두드린다)", "(taps the counter)", 1),
    ("quiet", "idle", "", ".......", "......", 1),
    ("chatty", "idle", "", "요즘 위쪽 구역은 난리라던데, 들었어요?", "Uptown's a mess these days — you heard?", 1),
    ("chatty", "idle", "", "혼잣말이에요, 신경 쓰지 마세요.", "Just talking to myself, don't mind me.", 1),
]

# ============================================================
# 4. 스케줄 — Days / GuestSlots / Points
# ============================================================
DAY_COLS = ["day","label_ko","label_en","start_phase","bgm_street","bgm_bar","upkeep_gold","note"]
DAYS = [
    (1, "튜토리얼",      "Tutorial",     "home",       "bgm_street_night", "bgm_bar_calm", 0, "인트로→집 기상→쪽지→비 오는 출근길(행인 없음)→튜토리얼+포트 첫 잔→엘리베이터 컷씬→테라스"),
    (2, "첫 영업",       "First Night",  "home", "bgm_street_night", "bgm_bar_main", 0, "첫 풀 사이클. 신규 입고 없음(day1 배치 공유). 밤: 습격의 꿈 1"),
    (3, "삼호와 아일리", "Samho & Aili", "home", "bgm_street_night", "bgm_bar_main", 0, "삼호·아일리 영업. 선택 결과는 Day 3 퇴근길·집·꿈 장면에서 확인"),
    (4, "선택의 결과", "Consequences", "commute_out", "bgm_street_night", "bgm_bar_main", 0, "데모 최종일. 삼호 사망·구출·일반 경로 결과를 확인한 뒤 엔딩으로 연결"),
]

# v2.0 저작 분리 — 구 GuestSlots를 둘로 쪼갬. 1부 랜덤 손님(시스템 튜닝)과 단골 슬롯(서사)은
# 만지는 사람이 다르다. seq는 두 시트가 '하루 공용 번호'를 나눠 쓴다(스폰 순서 병합 기준) —
# 같은 날 같은 seq가 양쪽에 있으면 빌드 에러.
# 제조 개편: tier 컬럼 폐지 → order(칵테일 id 직접 지정). 공란 = 그날 해금된 칵테일 풀에서 추첨.
#   구 tier 지정의 의도(어려운 술 프리뷰)는 칵테일 직접 지정으로 보존한다.
WAVE_COLS = ["day","seq","order","personality","delay_sec","max_rounds","branch_choice"]
# 26.07.17 무드 확정 — 사이버펑크 슬럼가의 한적한 바: 하루 최대 4명, 스폰 텀 35~50초로 느슨하게
RANDOM_WAVES = [
    # day2 — 일반×3 + 진피즈 프리뷰(구 T3 지정 의도)
    (2, 1, "",         "gentle", 0,  1, False),
    (2, 2, "",         "chatty", 35, 1, False),
    (2, 3, "gin_fizz", "quiet",  50, 1, False),
    (2, 4, "",         "touchy", 40, 1, False),
    # day3 — 일반 + 롱아일랜드 프리뷰(구 T5 지정 의도) + 일반(다회주문)
    (3, 1, "",            "chatty", 0,  1, False),
    (3, 2, "long_island", "rough",  45, 1, False),
    (3, 3, "",            "touchy", 50, 2, False),
]

RSLOT_COLS = ["day","seq","character","order","delay_sec","max_rounds","branch_choice","cameo_scene","must_serve","serve_effects"]
REGULAR_SLOTS = [
    # day3 — 포트 카메오(1부 말미에 한 잔 하러 들른다). order=gin_fizz: 수십 년 진피즈 한 우물(Tastes와 부합)
    # must_serve=True: 인내심 타이머 없음 — 응대할 때까지 좌석 유지(2부 연결 손님 보호). False면 일반 손님처럼 이탈
    (3, 4, "port", "gin_fizz", 40, 1, False, "d3_cameo_port", True, "flag.port_served_d3 = true"),
]

# 위치 프리셋 — 항상 같은 맵이므로 좌표 대신 이름 붙은 지점을 쓴다.
# Unity 씬에 같은 이름의 앵커 오브젝트(SpotAnchor)를 배치하고, 데이터는 이름만 참조.
# 배경 아트가 바뀌어도 앵커만 옮기면 되고, 데이터는 불변.
SPOT_COLS = ["id","area","desc","note"]
SPOTS = [
    ("bar_door",     "street", "바 '언노운' 정문 앞",   "출근 도착점 / 퇴근 출발점"),
    ("street_mid",   "street", "거리 중간",             "걷기 연출 기본 목적지"),
    ("street_wall",  "street", "전단이 붙은 벽",        "구엔진 outside_objects 이식(전단·포스터류)"),
    ("home_door",    "street", "집 현관 앞",            "퇴근 도착점 / 출근 출발점"),
    ("alley_in",     "alley",  "뒷골목 입구",           "골목 이벤트 얕은 쪽 + 임상시험 전단"),
    ("alley_deep",   "alley",  "뒷골목 안쪽",           "골목 이벤트 깊은 쪽(상자·고양이·쓰레기통)"),
    ("elevator",     "street", "엘리베이터 앞",         "배드엔딩2 연출 예약"),
    ("qa_pair_left", "qa", "QA 거리 좌측 NPC 자리", "Day 99: 행인 A·B 대화 배치"),
    ("qa_pair_right", "qa", "QA 거리 우측 NPC 자리", "Day 99: 행인 A·B 대화 배치"),
    ("qa_text_object", "qa", "QA 일반 오브젝트", "Day 99: E 조사 말풍선"),
    ("qa_sequence_object", "qa", "QA 순차 조사 오브젝트", "Day 99: sequential 선택"),
    ("qa_probe_object", "qa", "QA 조건 검증 오브젝트", "Day 99: 스텝 when·effects"),
    ("qa_monologue", "qa", "QA 1인 NPC 독백 자리", "Day 99: E로 시작하는 단일 NPC 독백"),
    ("qa_choice_npc", "qa", "QA 선택지 NPC 자리", "Day 99: continue·goto·조건 잠금 선택지"),
]

# v2.7.0 공용 필드 계약 — interact_points.json 하나가 집·외부의 배치(spot_id·facing·phase·spawn_when)와
# 상호작용(activation_mode·interact_when·action)을 함께 소유한다. 구 FieldEntities와 HomeInteractable 분리 계약은 소멸.
# actor는 Characters.id를 그대로 source_id로 사용하고 기본 동작은 FieldAnims의 idle을 쓴다.
# facing은 actor에만 적는다(object는 공란). spawn/move/despawn 스텝은 거리 런타임 범위가 아니다.
# Day 99 거리 QA도 운영과 동일한 단일 interact_points 계약을 사용한다.
# 거리 QA 씬·지점은 시트(Scenes/Steps/Choices/InteractPoints의 day99·p_qa_* 행)가 정본이다.

# transition은 플레이어가 지정 문·오브젝트에서 직접 상호작용했을 때만 실행한다.
# 경계를 걸어서 넘거나 proximity에 들어와 자동으로 장소가 바뀌는 기능은 지원하지 않는다.
TRANSITION_COLS = ["id","target_location","target_phase","target_spot","effect","note"]
TRANSITIONS = [
    ("enter_bar", "bar", "bar", "", "fade", "바 정문에서 E → 바 입장. bar_open·1부·2부 분기는 BarController가 현재 일차 데이터로 결정"),
    ("enter_home", "home", "home", "home_spawn_entry", "fade", "집 현관에서 E → 집 내부 입장"),
]

# action_type: scene(단일 씬) / scene_group(when을 통과하는 첫 씬) / transition(장소 전환) / 공란(순수 배치)
# activation_mode: interact(E키) / proximity(라디오·TV·홀로그램 같은 비캐릭터 자동 방송 전용)
# 시드의 spawn_when·interact_when은 구표기(1-based) — 로드 시 _shift_when이 일괄 −1.
POINT_COLS = ["id","kind","source_id","spot_id","facing","phase","spawn_when",
              "activation_mode","interact_when","priority","action_type","action_ref","note"]
POINTS = [
    ("p_elevator_radio", "object", "elevator_radio", "elevator", "", "commute_out", "day == 1",
     "proximity", "", 0, "scene", "d1_elevator", "라디오 범위 진입 시 아나운서 대사 자동 출력·자동 진행"),
    ("p_alley_cat", "actor", "bubi", "alley_in", "right", "commute_in", "day == 3",
     "interact", "!flag.d3_cat_seen", 100, "scene", "d3_alley_cat", "고양이에게 E 상호작용할 때만 대사 시작"),
    ("p_shiba", "actor", "shiba", "alley_in", "right", "both", "day >= 2",
     "interact", "", 100, "scene_group", "np_shiba", "씬 when을 통과하는 첫 시바 씬을 실행"),
    ("p_ob_parttime", "object", "sp_parttime_poster", "street_wall", "", "both", "day >= 2",
     "interact", "", 50, "scene", "ob_parttime_1", "몇 번이든 다시 읽을 수 있는 알바 전단"),
    ("p_ob_experiment", "object", "sp_experiment_poster", "alley_in", "", "commute_out", "",
     "interact", "!flag.seen_coratech_ad", 50, "scene", "ob_experiment_1", "임상시험 전단은 계속 보이고 한 번 읽은 뒤 상호작용만 비활성"),
    ("p_enter_bar", "object", "street_bar_door", "bar_door", "", "commute_in", "day <= 3",
     "interact", "", 100, "transition", "enter_bar", "자동 경계 이동 없이 문에서 E를 눌러 입장"),
    ("p_enter_home", "object", "street_home_door", "home_door", "", "commute_out", "day <= 3",
     "interact", "", 100, "transition", "enter_home", "자동 경계 이동 없이 문에서 E를 눌러 입장"),
]

# ============================================================
# 5. 대본 — Scenes / Steps / Choices / Orders
# ============================================================
# trigger는 bar/home/cutscene 기존 실행기의 발동 방식이다. 거리에서는 v2.6 start_mode를 정본으로 쓴다.
# start_mode: referenced(InteractPoints가 호출) / manual(choice.goto 등 다른 씬이 호출)
# on_complete_effects: 거리 씬의 마지막 스텝까지 정상 완료한 뒤 한 번 적용하는 상태 변화.
# skippable: 컷씬 스킵 허용 여부 / group: scene_group 선택 단위
# day 0 = 일차 무관 상시 씬(반복 NPC·오브젝트) — phase에 따라 street/home 등으로 배포
SCENE_COLS = ["id","day","phase","seq","trigger","when","title","skippable","group","start_mode","on_complete_effects"]
SCENES = [
    ("d1_tutorial",    1, "bar",         1, "auto",     "", "튜토리얼 — 크리스의 진토닉 강습"),
    ("d1_home_talk",   1, "home",        1, "auto",     "", "테라스 — 바텐더의 태도 + 근황"),
    ("d1_intro",       1, "intro",       1, "auto",     "", "인트로 — 검은 화면, 루나가 처음 눈뜨던 밤"),
    ("d1_port",        1, "bar",         3, "auto",     "", "포트 첫 잔 — 진피즈 (튜토리얼 두 번째 제조)"),
    ("d1_elevator",    1, "commute_out", 2, "interact", "", "퇴근길 엘리베이터 — 범위 진입 시 자동 출력되는 아나운서 뉴스"),
    ("d2_home_talk",   2, "home",        1, "auto",     "", "테라스 — 이틀째 밤"),
    ("d2_bar_open",    2, "bar_open",    1, "auto",     "", "개점 전 — 크리스의 확인, OPEN 간판을 걸기까지"),
    ("d2_port_chris",  2, "bar",         1, "auto",     "", "포트 첫 등장 — 루나 구조의 진실 일부"),
    ("d2_shiba",       2, "bar",         2, "auto",     "", "시바 첫 등장 — 개똥철학 (크리스 부재)"),
    ("d2_chris_return",2, "bar",         3, "auto",     "", "크리스 복귀 — 시바는 멍멍"),
    ("d2_dream",       2, "dream",       1, "auto",     "", "꿈 — 습격 1: 유나의 목소리와 총성"),
    ("d3_alley_cat",   3, "commute_in",  0, "interact", "", "골목의 고양이"),
    ("d3_bar_open",    3, "bar_open",    1, "auto",     "", "개점 전 — 어제 손님 이야기, OPEN"),
    ("d3_aili_bubi",   3, "bar",         1, "auto",     "", "아일리 첫 대면 + 부비 등장"),
    ("d3_samho",       3, "bar",         2, "auto",     "", "삼호 첫 등장 — 고도수 2연속"),
    ("d3_cameo_port",  3, "bar",         0, "cameo",    "", "1부 카메오 — 포트가 짧게 들름 (서빙 후 재생)"),
    # 표시 Day 3(시드 day4) — Day 2 제조 선택의 결과를 확인하는 데모 최종일.
    ("d3_chris_witness",4,"home",        2, "auto",     "flag.samho_refused_drink", "크리스의 목격 — 데모 컷"),
    ("d3_home_talk",   4, "home",        1, "auto",     "!flag.samho_death_route && !flag.samho_refused_drink", "테라스 — 은인들 (데모에선 분기 씬이 대체)"),
    ("d3_dream",       4, "dream",       1, "auto",     "!flag.samho_death_route && !flag.samho_refused_drink", "꿈 — 습격 2 (데모에선 분기 엔딩이 대체)"),
    # --- 엔딩 (day 0 상시, endings.json이 scene_id로 호출 — trigger=manual) ---
    # --- 구엔진 outside_objects.json 이식 (day 0 = 상시 공용) ---
    ("np_shiba_1",      0, "street", 1, "interact", "!flag.shiba_met", "[이식] 시바 첫 조우 (구 first_encounter)", False, "np_shiba"),
    ("np_shiba_3",      0, "street", 2, "interact", "flag.shiba_met && day >= 3", "[더미] 시바 — 거리 선택지 데모 (조건 항목·goto 포함)", False, "np_shiba"),
    ("np_shiba_2",      0, "street", 3, "interact", "", "[이식] 시바 반복 대사 (구 revisit_repeat)", False, "np_shiba"),
    ("np_shiba_treat",  0, "street", 4, "manual",   "", "[더미] 시바 — 간식 goto 결과 씬", False, ""),
    ("np_tv_1",         0, "home",   1, "interact", "!flag.tv_seen1", "홀로그램 TV — 실종 뉴스 (구 radio 이식)", False, "home_tv"),
    ("np_tv_2",         0, "home",   2, "interact", "", "홀로그램 TV — 토크쇼 (구 radio 이식)", False, "home_tv"),
    ("ob_parttime_1",   0, "street", 1, "interact", "", "전단 — 알바 공고 (포스터 뷰, 적힌 정보만)", False, ""),
    ("ob_experiment_1", 0, "street", 1, "interact", "", "임상시험 전단 — 코라테크 복선 (포스터 뷰)", False, ""),
    # --- Day 99 거리 전용 QA 시나리오 ---
    ("qa_np_conversation", 99, "street", 1, "interact", "", "[QA] E로 시작하는 NPC A·B 대화", False, ""),
    ("qa_object_text", 99, "street", 2, "interact", "", "[QA] 일반 사물 말풍선", False, ""),
    ("qa_step_probe", 99, "street", 3, "interact", "", "[QA] 스텝 when·effects", False, ""),
    ("qa_npc_monologue", 99, "street", 4, "interact", "", "[QA] E로 시작하는 1인 NPC 독백", False, ""),
    ("qa_choice", 99, "street", 5, "interact", "", "[QA] 선택지 continue·goto·조건 잠금", False, ""),
    ("qa_choice_result", 99, "street", 6, "manual", "", "[QA] 선택지 goto 도착 씬", False, ""),
    ("qa_sequence_1", 99, "street", 10, "interact", "", "[QA] 순차 조사 1단계", False, "qa_street_sequence"),
    ("qa_sequence_2", 99, "street", 11, "interact", "", "[QA] 순차 조사 2단계", False, "qa_street_sequence"),
    ("qa_sequence_3", 99, "street", 12, "interact", "", "[QA] 순차 조사 3단계·마지막 고정", False, "qa_street_sequence"),
]
# 구형 7필드 행은 skippable/group을 채운 뒤, 거리 씬에만 start_mode를 부여한다.
_STREET_PHASES = {"street", "commute_in", "commute_out"}
_SCENE_WHEN_OVERRIDES = {
    "qa_sequence_1": "!flag.qa_sequence_1_done",
    "qa_sequence_2": "flag.qa_sequence_1_done && !flag.qa_sequence_2_done",
    "qa_sequence_3": "flag.qa_sequence_2_done",
}
_SCENE_COMPLETE_EFFECTS = {
    "d3_alley_cat": "flag.d3_cat_seen = true",
    "ob_experiment_1": "flag.seen_coratech_ad = true",
    "qa_sequence_1": "flag.qa_sequence_1_done = true",
    "qa_sequence_2": "flag.qa_sequence_2_done = true",
}
def _normalize_scene(row):
    vals = list(row)
    if len(vals) == 7:
        vals += [False, ""]
    scene_id, phase, trigger = vals[0], vals[2], vals[4]
    if scene_id in _SCENE_WHEN_OVERRIDES:
        vals[5] = _SCENE_WHEN_OVERRIDES[scene_id]
    start_mode = "" if phase not in _STREET_PHASES else ("manual" if trigger == "manual" else "referenced")
    vals += [start_mode, _SCENE_COMPLETE_EFFECTS.get(scene_id, "")]
    return tuple(vals)
SCENES = [_normalize_scene(r) for r in SCENES]

# type v1.2 추가: expr(대사 없이 표정 전환) / anim(1회성 동작 클립) / emote(머리 위 이모트)
#                 timeline(Unity Timeline 재생) / gif(스파인→GIF 삽입) — timeline/gif는 Cutscenes 표 참조
# 원본 Steps와 비거리 JSON sync: ""(=wait) / no_wait(병렬 연출).
# 거리 배포 JSON에서는 build_street_runtime_contract가 say 전용 auto/player_input으로 정규화한다.
# say의 arg = 표정(Expressions 참조). 본인 대사 출력 중 입 애니메이션은 표정 데이터의 lower_face 규칙이 담당
STEP_COLS = ["scene_id","seq","type","actor","arg","text_ko","text_en","when","effects","sync","note","dialogue_id"]
STEPS = [
    # ---------- 개점 전 대화 (phase=bar_open) — 끝나면 1부가 자동 시작된다 ----------
    ("d2_bar_open", 1, "enter", "chris", "M", "", "", "", "", "카운터 안쪽"),
    ("d2_bar_open", 2, "say",   "chris", "default", "오늘부터는 손님을 직접 받는다. 어제 배운 대로 할 수 있겠지?", "From tonight you take the guests yourself. You can do it the way I taught you, right?", "", "", ""),
    ("d2_bar_open", 3, "say",   "luna",  "default", "코스터를 먼저 내고, 주문을 받고, 만들어서 코스터 위에 올린다.", "Coaster first, then take the order, make it, and set it on the coaster.", "", "", ""),
    ("d2_bar_open", 4, "say",   "chris", "default", "…외우긴 잘 외웠군. 손이 따라오는지는 해봐야 알겠지만.", "...You memorized it well enough. Whether your hands follow is another matter.", "", "", ""),
    ("d2_bar_open", 5, "say",   "luna",  "default", "틀리면 어떻게 하죠?", "What if I get it wrong?", "", "", ""),
    ("d2_bar_open", 6, "say",   "chris", "default", "틀린 잔은 내가 물어준다. 두 번 틀리지만 마라.", "I'll cover the wrong ones. Just don't make the same mistake twice.", "", "", "배상 규칙을 대사로 안내"),
    ("d2_bar_open", 7, "say",   "chris", "default", "좋아, 시작해보자. 간판 걸어라.", "All right. Let's open. Go flip the sign.", "", "", "루나 대사 규칙 — 행동 유도는 상대 대사로"),

    ("d3_bar_open", 1, "enter", "chris", "M", "", "", "", "", ""),
    ("d3_bar_open", 2, "say",   "chris", "default", "어제는 나쁘지 않았다. 손이 조금 덜 떨더군.", "Yesterday wasn't bad. Your hands shook a little less.", "", "", ""),
    ("d3_bar_open", 3, "say",   "luna",  "default", "…칭찬인가요?", "...Is that praise?", "", "", ""),
    ("d3_bar_open", 4, "say",   "chris", "default", "사실 확인이다. 오늘은 술이 좀 더 들어왔으니 주문도 다양해질 거다.", "It's an observation. More bottles came in today, so the orders will get more varied.", "", "", "입고 연동 안내"),
    ("d3_bar_open", 5, "say",   "chris", "default", "모르는 주문이 오면 손님을 봐라. 잔보다 사람이 먼저다.", "If an order stumps you, look at the guest. The person comes before the glass.", "", "", "2부 유추 플레이 복선"),
    ("d3_bar_open", 6, "say",   "chris", "default", "됐다. 열자.", "That's enough. Let's open.", "", "", ""),

    # ---------- 인트로 (검은 화면 — 루나가 처음 눈뜨던 밤, 데모 오프닝) ----------
    ("d1_intro", 1, "say", "chris", "default", "…정신이 드나.", "...You're awake.", "", "", "검은 화면, 텍스트만"),
    ("d1_intro", 2, "say", "luna",  "default", "…여기는.", "...Where is this.", "", "", ""),
    ("d1_intro", 3, "say", "chris", "default", "내 집이다. 포트라는 노인이 널 업어왔지.\n골목에 쓰러져 있었다더군.", "My home. An old man named Port carried you in.\nSaid you were collapsed in an alley.", "", "", ""),
    ("d1_intro", 4, "say", "luna",  "default", "…당신은, 누구죠.", "...And you are?", "", "", ""),
    ("d1_intro", 5, "say", "chris", "default", "크리스. 바 '언노운'의 마스터다.", "Chris. Master of Bar Unknown.", "", "", ""),
    ("d1_intro", 6, "say", "luna",  "default", "저는… 기억이, 없습니다.\n이름 말고는, 아무것도.", "I... don't remember anything.\nNothing but my name.", "", "", ""),
    ("d1_intro", 7, "say", "chris", "default", "…그런 것 같더군.", "...So it seems.", "", "", ""),
    ("d1_intro", 8, "say", "chris", "default", "당분간 여기 있어라.\n마침 가게에 일할 손이 필요하던 참이다.", "Stay here for now.\nAs it happens, I need an extra pair of hands at the bar.", "", "", ""),
    ("d1_intro", 9, "say", "luna",  "default", "(…그렇게, 이 집의 소파가 내 자리가 되었다.)", "(...And so, the sofa in this house became my place.)", "", "", "페이드 아웃 → 소파 기상"),

    # ---------- 크리스의 쪽지 (day1 아침 — 읽어야 출근 가능) ----------

    # ---------- 포트 첫 잔 (day1 — 튜토리얼 두 번째 제조: 진피즈) ----------
    ("d1_port", 1,  "enter", "port", "R", "", "", "", "", ""),
    ("d1_port", 2,  "say", "port",  "default", "…새 얼굴이군.", "...A new face.", "", "", ""),
    ("d1_port", 3,  "say", "chris", "default", "포트. 인사해라, 루나다. 오늘부터 일한다.", "Port. Meet Luna. She starts today.", "", "", ""),
    ("d1_port", 4,  "say", "port",  "default", "루나… 그래. 잘 부탁하네.", "Luna... I see. Good to meet you.", "", "", ""),
    ("d1_port", 5,  "say", "chris", "default", "마침 잘 왔어. 루나, 연습 삼아 한 잔 더 만들어봐라.", "Good timing. Luna — one more glass, for practice.", "", "", ""),
    ("d1_port", 6,  "say", "port",  "default", "그럼, 늘 마시던 걸로.", "Then I'll have my usual.", "", "", ""),
    ("d1_port", 7,  "order", "port", "exact:gin_fizz", "진피즈, 정석대로.", "Gin Fizz. By the book.", "", "", ""),
    ("d1_port", 8,  "craft", "", "order", "", "", "", "", ""),
    ("d1_port", 9,  "serve", "port", "", "", "", "", "", ""),
    ("d1_port", 10, "say", "port",  "default", "…손끝이 좋군. 오늘이 처음이라고?", "...Good hands. First day, you said?", "", "", ""),
    ("d1_port", 11, "say", "chris", "default", "오늘이 처음이다.", "First day.", "", "", ""),
    ("d1_port", 12, "say", "port",  "default", "허. …이 가게, 오래 다녀야겠어.", "Hah. ...I'll be coming here a long while yet.", "", "", ""),
    ("d1_port", 13, "exit", "port", "", "", "", "", "", ""),
    ("d1_port", 14, "say", "chris", "default", "오늘은 여기까지. 정리는 내가 한다.\n먼저 들어가라.", "That's enough for today. I'll close up.\nHead home first.", "", "", "루나 혼자 퇴근 → 엘리베이터 컷씬"),
    ("d1_port", 15, "end_part", "", "", "", "", "", "", "그날 bar 마지막 씬의 종료 스텝 → 정산"),

    # ---------- 엘리베이터 라디오 (day1 퇴근 — 구엔진 elevator_radio d1 commute_out 이식) ----------
    ("d1_elevator", 2, "say", "radio", "", "…다음 뉴스입니다.", "...In other news.", "", "", "구 radio_d1_out_001"),
    ("d1_elevator", 3, "say", "radio", "", "뉴런 트롤프 주니어가 대통령 8연임에 성공하며,\n신미합중국의 제67대 대통령으로 다시 한번 당선되었습니다.", "Newron Trolph Jr. has won his eighth term,\nre-elected as the 67th President of the New United States.", "", "", ""),
    ("d1_elevator", 4, "say", "radio", "", "트롤프 대통령은 당선 직후 연설에서", "In his victory speech, President Trolph declared:", "", "", ""),
    ("d1_elevator", 5, "say", "radio", "", "\"위대한 국가 재건은 아직 끝나지 않았다.\"\n\"신미합중국은 다시 한 번 세계의 중심에 설 것이다.\"", "\"The great national rebuilding is not over.\"\n\"The New United States will stand at the center of the world once more.\"", "", "", ""),
    ("d1_elevator", 6, "say", "radio", "", "라고 밝혔습니다.", "— he stated.", "", "", ""),

    # ---------- day2 테라스 ----------
    ("d2_home_talk", 1, "fx", "", "terrace_night", "", "", "", "", ""),
    ("d2_home_talk", 2, "say", "chris", "default", "…이틀째다. 오늘은 어땠나.", "...Day two. How was it.", "", "", ""),
    ("d2_home_talk", 3, "say", "luna",  "default", "손님이 많았습니다. …사람들은, 전부 다르네요.", "There were many guests. ...People are all different.", "", "", ""),
    ("d2_home_talk", 4, "say", "chris", "default", "그래. 같은 잔을 시켜도 이유는 전부 다르다.", "Right. Even when they order the same glass, the reasons are never the same.", "", "", ""),
    ("d2_home_talk", 5, "say", "chris", "default", "그걸 읽는 게 이 일의 절반이야.", "Reading that is half of this job.", "", "", ""),
    ("d2_home_talk", 6, "say", "luna",  "default", "…나머지 절반은요?", "...And the other half?", "", "", ""),
    ("d2_home_talk", 7, "say", "chris", "default", "기다리는 거다. 손님이 먼저 말할 때까지.", "Waiting. Until the guest speaks first.", "", "", ""),
    ("d2_home_talk", 8, "say", "luna",  "default", "(기다린다… 기록해 둔다.)", "(Waiting... noted.)", "", "", "이후 습격의 꿈 1"),

    # ---------- day3 분기 — 크리스의 목격 (rescue route, 집) ----------
    ("d3_chris_witness", 1, "say", "luna",  "default", "(소파에 삼호를 앉혔다. 상처는 깊지 않다.)", "(I sat Samho on the sofa. The wound isn't deep.)", "", "", ""),
    ("d3_chris_witness", 2, "say", "chris", "default", "…루나. 그건 뭐냐.", "...Luna. What is that.", "", "", "문 열고 들어온 크리스"),
    ("d3_chris_witness", 3, "say", "samho", "default", "아… 안녕하세요! 삼호라고 합니다!", "Ah... hello! Samho, sir!", "", "", ""),
    ("d3_chris_witness", 4, "say", "chris", "default", "………", ".........", "", "", ""),
    ("d3_chris_witness", 5, "say", "luna",  "default", "사정이 있었습니다. 오늘 하루만—", "There were circumstances. Just for tonight—", "", "", ""),
    ("d3_chris_witness", 6, "say", "chris", "default", "………", ".........", "", "", ""),
    ("d3_chris_witness", 7, "say", "chris", "default", "…구급상자는 선반 위에 있다.", "...The first-aid kit is on the shelf.", "", "", ""),
    ("d3_chris_witness", 8, "say", "samho", "default", "…!", "...!", "", "", ""),
    ("d3_chris_witness", 9, "say", "luna",  "default", "(…화내지 않았다.)", "(...He didn't get angry.)", "", "", ""),
    ("d3_chris_witness", 10, "say", "chris", "default", "얘기는 내일 듣지. …길어질 것 같으니까.", "We'll talk tomorrow. ...It sounds like a long story.", "", "", "→ 데모 엔딩(구출)"),

    # ---------- 시바 NPC — 구엔진 shiba.json 이식 (first_encounter / revisit_repeat) ----------
    ("np_shiba_1", 1,  "say", "luna",  "idle", "…", "...", "", "", "구 first_encounter_x1"),
    ("np_shiba_1", 2,  "say", "shiba", "idle", "뭘 봐? 시바.", "What're you looking at? Shiba.", "", "", ""),
    ("np_shiba_1", 3,  "say", "luna",  "idle", "안녕하세요.", "Hello.", "", "", ""),
    ("np_shiba_1", 4,  "say", "shiba", "idle", "하. 이 동네에서 인사하고\n돌아다니는 놈이 있네.", "Ha. Someone in this neighborhood\nactually goes around greeting people.", "", "", ""),
    ("np_shiba_1", 5,  "say", "luna",  "idle", "혹시 인간입니까?", "Are you, by any chance, human?", "", "", ""),
    ("np_shiba_1", 6,  "say", "shiba", "idle", "뭐래, 너 머리 나쁘냐?", "What? Are you slow or something?", "", "", ""),
    ("np_shiba_1", 7,  "say", "luna",  "idle", "육안으로 봤을 때 강아지로 판단됩니다.", "Visual assessment indicates: dog.", "", "", ""),
    ("np_shiba_1", 8,  "say", "shiba", "idle", "사람을 얼굴로 판단하지 말라고.\n못생긴 놈 같으니.", "Don't judge people by their faces,\nyou ugly mug.", "", "", ""),
    ("np_shiba_1", 9,  "say", "luna",  "idle", "…그럼 사람이 맞는 건가요?", "...So you ARE a person?", "", "", ""),
    ("np_shiba_1", 10, "say", "shiba", "idle", "아니?", "Nope?", "", "", ""),
    ("np_shiba_1", 11, "say", "shiba", "idle", "딱 보면 시바잖아, 시바.", "One look and you can tell I'm a shiba, shiba.", "", "", ""),
    ("np_shiba_1", 12, "say", "luna",  "idle", "아하… 확인했습니다.", "I see... confirmed.", "", "flag.shiba_met = true", ""),
    ("np_shiba_2", 1,  "say", "shiba", "idle", "이제 좀 가, 시바.", "Go away already, shiba.", "", "", "구 revisit_repeat"),
    ("np_shiba_2", 2,  "say", "luna",  "idle", "왜죠?", "Why?", "", "", ""),
    ("np_shiba_2", 3,  "say", "shiba", "idle", "이제 더 대화할 대사 데이터도 없단 말이야.", "I'm out of dialogue data, that's why.", "", "", ""),
    ("np_shiba_2", 4,  "say", "luna",  "idle", "…?", "...?", "", "", ""),
    ("np_shiba_2", 5,  "say", "shiba", "idle", "불만 있으면 이런 정신 나간 게임을 기획한\nPD한테 가서 따져, 시바.", "Got complaints? Take it up with the PD\nwho designed this insane game, shiba.", "", "", "구엔진 4번째 벽 개그 유지"),

    # ---------- 홀로그램 TV — 구엔진 elevator_radio 이식 (집) ----------
    ("np_tv_1", 1, "say", "radio", "default", "최근 B구역 일대에서 실종 사건이\n급격히 증가하고 있습니다.", "Disappearances are rising sharply\nacross District B.", "", "", "구 radio_d1_in"),
    ("np_tv_1", 2, "say", "radio", "default", "지난 일주일에만 30명이 넘는 실종자가 발생하며\n전례 없는 연쇄 실종 사건으로 번지고 있습니다.", "With over thirty people missing in the past week alone,\nit is escalating into an unprecedented serial-disappearance case.", "", "", ""),
    ("np_tv_1", 3, "say", "radio", "default", "KCPD는 추가 인력을 급파해\n집중 수사에 착수했다고 밝혔습니다.", "The KCPD has dispatched additional officers\nand launched a focused investigation.", "", "", ""),
    ("np_tv_1", 4, "say", "luna",  "default", "(…실종. 기록해 둔다.)", "(...Disappearances. Noted.)", "", "flag.tv_seen1 = true", "중막 복선"),
    ("np_tv_2", 1, "say", "radio", "default", "몇 달 전 북한산 저수지의 큰불, 기억하시죠?", "Remember that huge fire at the Bukhansan reservoir a few months back?", "", "", "구 radio_d2_in 토크쇼"),
    ("np_tv_2", 2, "say", "radio", "default", "그게 단순한 산불이 아니었다는\n소문이 돌고 있습니다.", "Word is going around that it was\nno ordinary wildfire.", "", "", ""),
    ("np_tv_2", 3, "say", "luna",  "default", "(…이 도시는 소문이 많다.)", "(...This city is full of rumors.)", "", "", ""),

    # ---------- DAY 1 ----------
    ("d1_tutorial", 1,  "enter",  "chris", "M",  "", "", "", "", "카운터 안쪽에서 등장"),
    ("d1_tutorial", 2,  "say",    "chris", "default", "…왔나. 일 시작 전에 기본부터 가르쳐주지.", "...You're here. Before we open, let's start with the basics.", "", "", ""),
    ("d1_tutorial", 3,  "say",    "luna",  "default", "…네.", "...Okay.", "", "", "서먹한 거리감"),
    ("d1_tutorial", 4,  "say",    "chris", "default", "손님이 앉으면 코스터부터 내준다. 그래야 주문을 말해. 주문을 받고, 만들고, 코스터 위에 올려 건넨다. 그게 전부다.", "When a guest sits down, the coaster comes first — that's what gets them talking. Take the order, make it, set it on the coaster. That's all there is.", "", "", "코스터/서빙 튜토리얼 안내"),
    ("d1_tutorial", 5,  "say",    "chris", "default", "제일 단순한 걸로 시작하자. 진토닉. 레시피 노트를 봐도 좋다.", "We'll start with the simplest one. A gin and tonic. You may look at the recipe note.", "", "", "행동 유도는 크리스 대사로 종료(루나 대사 규칙)"),
    ("d1_tutorial", 6,  "craft",  "",      "tutorial:gin_tonic", "", "", "", "", "제조 화면 진입(가이드 강조 ON)"),
    ("d1_tutorial", 7,  "say",    "chris", "success", "…나쁘지 않군. 손이 빠르다.", "...Not bad. Your hands are quick.", "grade >= good", "affinity.chris += 1", "판정 분기 상"),
    ("d1_tutorial", 8,  "say",    "chris", "default", "…뭐, 처음이니까. 내일까지는 감을 잡아라.", "...Well, it's your first day. Get the feel of it by tomorrow.", "grade < good", "", "판정 분기 하"),
    ("d1_tutorial", 9,  "say",    "chris", "default", "오늘은 여기까지. 내일부터 진짜 손님을 받는다.", "That's it for today. Tomorrow, we take real customers.", "", "", ""),
    ("d1_tutorial", 10, "end_part","",     "",   "", "", "", "", ""),
    ("d1_home_talk", 1, "fx",     "",      "terrace_night", "", "", "", "", "테라스 야경"),
    ("d1_home_talk", 2, "say",    "chris", "default", "…안 자고 있었나.", "...Still up?", "", "", "마감 후 귀가한 크리스"),
    ("d1_home_talk", 3, "say",    "luna",  "default", "잠이 안 와서요.", "Couldn't sleep.", "", "", ""),
    ("d1_home_talk", 4, "say",    "chris", "default", "바텐더는 별별 손님을 다 만난다. 취한 놈, 우는 놈, 말 없는 놈. 무슨 일이 있어도 잔을 만드는 손은 흔들리지 마라.", "A bartender meets every kind of guest. Drunk ones, crying ones, silent ones. Whatever happens, the hand that makes the drink must not shake.", "", "", "구 day0 테라스 주제"),
    ("d1_home_talk", 5, "say",    "chris", "default", "…이 집은 지낼 만하고?", "...Is this place livable, by the way?", "", "", "근황 토크 — 선택지 직전은 크리스 대사"),
    ("d1_home_talk", 6, "choice", "",      "ch_d1_home", "", "", "", "", ""),
    ("d1_home_talk", 7, "say",    "chris", "default", "그래. 천천히 하면 된다. 늦잠 자지 마라.", "Good. Take it slow. And don't oversleep.", "", "", "수면으로"),
    # ---------- DAY 2 ----------
    ("d2_port_chris", 1,  "enter", "port",  "L", "", "", "", "", ""),
    ("d2_port_chris", 2,  "say",   "port",  "joy", "크리스! 이 영감, 아직 살아있었네.", "Chris! You old man, still alive!", "", "", ""),
    ("d2_port_chris", 3,  "say",   "chris", "default", "…영감은 너다, 포트.", "...You're the old man, Port.", "", "", ""),
    ("d2_port_chris", 4,  "say",   "port",  "joy", "하하! 그래, 이 친구가 그 소문의 신입인가.", "Haha! So this is the famous new hire.", "", "", ""),
    ("d2_port_chris", 5,  "order", "port",  "exact:gin_fizz", "진피즈 하나 부탁하지. 옛날부터 그것만 마셔서 말이야.", "One gin fizz, if you would. It's the only thing I've drunk for decades.", "", "", "포트 취향: 진피즈 good — 행동(제조) 직전은 손님 대사"),
    ("d2_port_chris", 6,  "craft", "",      "order", "", "", "", "", ""),
    ("d2_port_chris", 7,  "serve", "port",  "", "", "", "", "", ""),
    ("d2_port_chris", 8,  "say",   "port",  "joy", "…크. 그래, 이 맛이지.", "...Ahh. Yes, that's the one.", "", "", "등급 반응 프리셋과 별개의 고정 대사"),
    ("d2_port_chris", 9,  "say",   "port",  "serious", "루나라고 했나. 사실 널 여기 데려온 게 나다. 그날 밤, 고물상 컨테이너 밑에서.", "Luna, was it? Truth is, I'm the one who brought you here. That night, from under a container at the scrapyard.", "", "", "구조 사실 공개"),
    ("d2_port_chris", 10, "say",   "port",  "serious", "숨도 안 쉬는 줄 알았지. 치료는 아일리가 했고. …내일쯤 올 테니 인사해 둬.", "Thought you weren't breathing. Aili patched you up. ...She'll drop by tomorrow — say hello.", "", "", "아일리 언급"),
    ("d2_port_chris", 11, "say",   "chris", "default", "…그 얘긴 거기까지 하지.", "...That's far enough on that story.", "", "", "크리스 회피 복선"),
    ("d2_port_chris", 12, "say",   "port",  "joy", "알았어, 알았어. 잘 부탁한다, 신입.", "Alright, alright. Take care of the place, rookie.", "", "affinity.port += 2", "첫 만남 보정"),
    ("d2_port_chris", 13, "exit",  "port",  "", "", "", "", "", ""),
    ("d2_port_chris", 14, "say",   "chris", "default", "…바람 좀 쐬고 오마.", "...I'll get some air.", "", "", "시바 씬을 위한 자리 비움"),
    ("d2_port_chris", 15, "exit",  "chris", "", "", "", "", "", ""),
    ("d2_shiba", 1,  "enter", "shiba", "M", "", "", "", "", "문 틈으로 들어옴"),
    ("d2_shiba", 2,  "say",   "shiba", "default", "…주인장 나갔지? 좋아. 이제 말할 수 있겠군.", "...The owner's out, right? Good. Now I can talk.", "", "", ""),
    ("d2_shiba", 3,  "say",   "luna",  "default", "…개가, 말을.", "...A dog. Talking.", "", "", ""),
    ("d2_shiba", 4,  "say",   "shiba", "default", "개가 아니라 시바견이다. 그리고 목소리 낮춰. 인간들은 말하는 개한테 관대하지 않거든.", "Not a dog — a Shiba. And keep your voice down. Humans aren't generous with talking dogs.", "", "", ""),
    ("d2_shiba", 5,  "order", "shiba", "exact:bottle_beer", "병맥주 하나. 병째로 다오.", "One bottled beer. In the bottle, please.", "", "", "병따기 기믹 소개 겸"),
    ("d2_shiba", 6,  "craft", "",      "order", "", "", "", "", ""),
    ("d2_shiba", 7,  "serve", "shiba", "", "", "", "", "", ""),
    ("d2_shiba", 8,  "say",   "shiba", "default", "행복이 뭔지 아나, 신입? 산책이다. 같은 길도 매일 냄새가 달라. 인간은 그걸 몰라서 불행한 거다.", "You know what happiness is, rookie? Walks. Same street, different smell every day. Humans don't know that — that's why they're unhappy.", "", "", "개똥철학 1"),
    ("d2_shiba", 9,  "say",   "luna",  "default", "(…반박할 수 없다. 이상하다.)", "(...I can't refute that. Strange.)", "", "", ""),
    ("d2_shiba", 10, "say",   "shiba", "default", "아, 그리고. 너네 주인장, 옥상에서 담배 다시 피우더라. …비밀이다. 멍.", "Oh, and — your boss started smoking again. On the rooftop. ...It's a secret. Woof.", "", "flag.knows_chris_smokes = true", "크리스 흡연 언급(구 day1 명세)"),
    ("d2_chris_return", 1, "enter", "chris", "R", "", "", "", "", "복귀"),
    ("d2_chris_return", 2, "say",   "shiba", "default", "멍! 멍멍!", "Woof! Woof woof!", "", "", "크리스 앞에서는 멍멍만"),
    ("d2_chris_return", 3, "say",   "chris", "default", "…웬 개가 카운터에 앉아 있나. 루나, 손님 개는 받지 마라.", "...Why is there a dog at my counter. Luna, no canine customers.", "", "", ""),
    ("d2_chris_return", 4, "say",   "luna",  "default", "(방금까지 분명히 말을…)", "(It was just talking. I'm certain of it...)", "", "", ""),
    ("d2_chris_return", 5, "exit",  "shiba", "", "", "", "", "", "당당하게 꼬리 흔들며 퇴장"),
    ("d2_chris_return", 6, "say",   "chris", "default", "오늘은 슬슬 정리하지.", "Let's start closing up.", "", "", ""),
    ("d2_chris_return", 7, "end_part", "",  "", "", "", "", "", ""),
    ("d2_dream", 1, "fx",  "",     "glitch_in", "", "", "", "", "노이즈 인 — 카타나 제로식 조각 연출"),
    ("d2_dream", 2, "say", "yuna", "default", "루나. 눈 감아. 무슨 소리가 나도, 뜨면 안 돼.", "Luna. Close your eyes. Whatever you hear — don't open them.", "", "", "구 day1 꿈: 유나의 대사 재생"),
    ("d2_dream", 3, "sfx", "",     "alarm_distant", "", "", "", "", "멀리서 경보음"),
    ("d2_dream", 4, "say", "yuna", "default", "괜찮아. …넌 사람을 돕는 아이야. 그렇지?", "It's okay. ...You're a child who helps people. Aren't you?", "", "", ""),
    ("d2_dream", 5, "sfx", "",     "gunshot", "", "", "", "", "총성 — 여기서 컷"),
    ("d2_dream", 6, "fx",  "",     "hard_cut", "", "", "", "", "강제 암전"),
    ("d2_dream", 7, "say", "luna", "default", "…!!", "...!!", "", "flag.dream_raid_1 = true", "침대에서 깨어남"),
    # ---------- DAY 3 ----------
    ("d3_alley_cat", 1, "say",    "bubi", "", "냐옹.", "Meow.", "", "", "고양이 전신 캐릭터 위 말풍선 — 플레이어 상호작용 후 냐옹만 출력"),
    ("d3_aili_bubi", 1,  "enter",  "aili", "L", "", "", "", "", ""),
    ("d3_aili_bubi", 2,  "say",    "aili", "default", "네가 루나구나! 얘기 많이 들었어. 포트가 입이 싸거든.", "So you're Luna! I've heard all about you — Port has a big mouth.", "", "", ""),
    ("d3_aili_bubi", 3,  "say",    "luna", "default", "…어제 그분 말로는, 절 치료해주셨다고.", "...He said you were the one who fixed me.", "", "", ""),
    ("d3_aili_bubi", 4,  "order",  "aili", "exact:champagne", "일 얘기는 나중에! 목부터 축이자. 샴페인 한 잔!", "Work talk later! Thirst first. One champagne!", "", "", "행동(제조) 직전은 손님 대사"),
    ("d3_aili_bubi", 5,  "craft",  "",     "order", "", "", "", "", ""),
    ("d3_aili_bubi", 6,  "serve",  "aili", "", "", "", "", "", ""),
    ("d3_aili_bubi", 7,  "say",    "aili", "success", "캬. 새벽술은 무죄야.", "Ahh. Dawn drinking is not a crime.", "", "", ""),
    ("d3_aili_bubi", 8,  "say",    "aili", "default", "아 맞다, 오는 길에 부비 못 봤어? 노란 고양이. 밥때가 지났는데 코빼기도 안 보여.", "Oh, right — did you see Bubi on your way? Yellow cat. Way past mealtime and not a whisker in sight.", "", "", "선택지 직전은 아일리 대사"),
    ("d3_aili_bubi", 9,  "choice", "",     "ch_d3_cat", "", "", "", "", "출근길 목격 여부로 옵션 분기"),
    ("d3_aili_bubi", 10, "say",    "aili", "success", "역시! 근처에 있었구나. 고마워, 루나.", "I knew it! She's close by. Thanks, Luna.", "flag.d3_cat_reported", "", "목격 보고 시에만"),
    ("d3_aili_bubi", 11, "enter",  "bubi", "M", "", "", "", "", "창틈으로 들어와 아일리 쪽으로"),
    ("d3_aili_bubi", 12, "fx",     "",     "cat_on_head", "", "", "", "", "머리에 얹는 애니메이션(구 명세)"),
    ("d3_aili_bubi", 13, "say",    "bubi", "default", "냐아.", "Meow.", "", "", ""),
    ("d3_aili_bubi", 14, "say",    "aili", "joy", "부비!! 아이고, 왔네 왔어~ 이따 밥 줄게.", "Bubi!! There you are~ I'll feed you later.", "", "affinity.bubi += 1", ""),
    ("d3_samho", 1,  "enter",  "samho", "R", "", "", "", "", ""),
    ("d3_samho", 2,  "say",    "samho", "default", "여어— 크리스가 알바 뽑았다더니. 뭐야, 이 허여멀건 애는.", "Well, well — heard Chris hired help. What's this pale little thing?", "", "", "구 day2 명세: 으스댐"),
    ("d3_samho", 3,  "say",    "aili",  "default", "삼호. 시비 걸 거면 나가.", "Samho. Pick a fight and you're out.", "", "", ""),
    ("d3_samho", 4,  "say",    "samho", "default", "인사잖아, 인사. …어이, 신입. 여기서 제일 독한 거.", "It's a greeting, a greeting. ...Hey, rookie. Strongest thing you've got.", "", "", ""),
    ("d3_samho", 5,  "order",  "samho", "exact:long_island", "롱아일랜드 아이스티. 있지? 그걸로.", "Long Island Iced Tea. You have it, right? That one.", "", "", "고도수 반복 주문 1잔째"),
    ("d3_samho", 6,  "craft",  "",      "order", "", "", "", "", ""),
    ("d3_samho", 7,  "serve",  "samho", "", "", "", "", "", ""),
    ("d3_samho", 8,  "say",    "samho", "success", "…크으. 동네 물이 좋아졌네.", "...Whew. The water in this town got better.", "", "", ""),
    ("d3_samho", 9,  "order",  "samho", "exact:long_island", "한 잔 더.", "One more.", "", "", "2잔째 — 취함 복선(day4 분기 예열)"),
    ("d3_samho", 10, "craft",  "",      "order", "", "", "", "", ""),
    ("d3_samho", 11, "serve",  "samho", "", "", "", "", "", ""),
    ("d3_samho", 12, "say",    "samho", "drunk", "이 맛에 산다니까. 슬럼가 출신은 위장이 강철이거든.", "This is what I live for. Slum-born stomachs are made of steel.", "", "", "출신 떡밥 — 선택지 직전은 삼호 대사"),
    ("d3_samho", 13, "choice", "",      "ch_d3_samho", "", "", "", "", ""),
    ("d3_samho", 14, "say",    "samho", "default", "…뭐야, 신입 주제에 잔소리는. (싫지 않은 눈치다)", "...What, a rookie nagging me? (He doesn't seem to hate it)", "flag.d3_samho_worry", "", ""),
    ("d3_samho", 15, "say",    "samho", "success", "하! 장사꾼 다 됐네. 마음에 들어.", "Ha! A born merchant. I like you.", "flag.d3_samho_sell", "", ""),
    # ('진짜 벌꿀' 사이드퀘스트 가안은 제조 개편에서 삭제 — bees_knees·허니시럽 폐기와 함께 (26.08.17 PD 확정))
    ("d3_samho", 25, "say",    "samho", "default", "간다. 계산은 달아놔. …농담이다, 여기.", "I'm off. Put it on my tab. ...Kidding. Here.", "", "", ""),
    ("d3_samho", 26, "exit",   "samho", "", "", "", "", "", ""),
    ("d3_samho", 27, "say",    "aili",  "default", "…쟤 저래 봬도 나쁜 애는 아니야. 나중에 알게 될 거야.", "...He acts tough, but he's not a bad kid. You'll see.", "", "", "day4 분기 감정 밑작업"),
    ("d3_samho", 19, "exit",   "aili",  "", "", "", "", "", "부비를 안고 퇴장"),
    ("d3_samho", 20, "end_part","",     "", "", "", "", "", ""),
    ("d3_cameo_port", 1, "say", "port", "joy", "…역시. 어제 그 맛이 아니었으면 어쩌나 했지.", "...Right. I'd have worried if it wasn't yesterday's taste.", "", "", "1부 서빙 직후 재생되는 짧은 카메오"),
    ("d3_cameo_port", 2, "say", "port", "default", "배우는 속도가 빠르군. 크리스한테 칭찬해두지.", "You learn fast. I'll put in a good word with Chris.", "", "affinity.port += 1", "카메오는 2~3줄로 짧게 — 비중 최소"),
    ("d3_home_talk", 1,  "fx",     "",      "terrace_night", "", "", "", "", ""),
    ("d3_home_talk", 2,  "say",    "chris", "default", "아일리는 만났나.", "Did you meet Aili.", "", "", ""),
    ("d3_home_talk", 3,  "say",    "luna",  "default", "네. …포트 씨가 절 데려왔고, 아일리 씨가 고쳤다고 들었어요.", "Yes. ...I heard Port brought me in, and Aili fixed me.", "", "", ""),
    ("d3_home_talk", 4,  "say",    "chris", "default", "그래. 그 둘한테는 빚이 있다. 나도, 너도.", "Right. We owe those two. Both of us.", "", "", "구 day2 테라스 주제"),
    ("d3_home_talk", 5,  "say",    "luna",  "default", "…크리스 씨는, 왜 절 받아준 거예요?", "...Chris. Why did you take me in?", "", "", ""),
    ("d3_home_talk", 6,  "say",    "chris", "default", "……。", "......", "", "", "침묵"),
    ("d3_home_talk", 7,  "say",    "chris", "default", "…그 얘긴 아직이다. 때가 되면 말해주지.", "...Not yet. When it's time, I'll tell you.", "", "", "회피 — day10(구9)까지 이어짐"),
    ("d3_home_talk", 9,  "say",    "chris", "default", "일은 좀 어떠냐. 사흘 해보니.", "How's the work? Three days in.", "", "", "선택지 직전은 크리스 대사"),
    ("d3_home_talk", 10, "choice", "",      "ch_d3_home", "", "", "", "", ""),
    ("d3_home_talk", 11, "say",    "chris", "default", "…그래. 그거면 됐다.", "...Good. That's enough for me.", "", "", ""),
    ("d3_dream", 1, "fx",  "",        "glitch_in", "", "", "", "", ""),
    ("d3_dream", 2, "say", "soldier", "default", "3구역 뚫렸다! 하운드다!! 전원—", "District 3 is breached! It's Hound!! All units—", "", "", "구 day2 꿈: 하운드·벡터·코라테크 충돌"),
    ("d3_dream", 3, "sfx", "",        "explosion", "", "", "", "", ""),
    ("d3_dream", 4, "say", "yuna",    "default", "이쪽이야. 뛰지 말고, 걸어. 카메라는 뛰는 것만 쫓아.", "This way. Don't run — walk. Cameras only chase what runs.", "", "", "유나가 루나를 데리고 이동"),
    ("d3_dream", 5, "say", "luna",    "default", "유나, 뒤에—", "Yuna, behind you—", "", "", "하운드 추격 — 여기서 컷"),
    ("d3_dream", 6, "fx",  "",        "hard_cut", "", "", "", "", ""),
    ("d3_dream", 7, "say", "luna",    "default", "…!!", "...!!", "", "flag.dream_raid_2 = true", "깨어남"),
    # ---------- 구엔진 outside_objects 이식 (11필드 신형식 — sync 포함) ----------
    # 오브젝트 보기 규칙: 말풍선은 오브젝트 상단에만 — 루나 말풍선·독백 없음 (v3.3)
    ("ob_parttime_1", 1, "timeline", "", "sp_parttime_poster", "아르바이트 구함 · Bc25 편의점 · 야간 3교대 · 연락처 154*455*587", "PART-TIMER WANTED · Bc25 convenience store · night shifts · 154*455*587", "", "", "", "전단에 적힌 정보만 — 감상·서술 금지"),
    ("ob_experiment_1", 1, "timeline", "", "sp_experiment_poster", "임상시험 참가자 모집 · 인공 신경망 분석 · 주관 (주)코라테크 · 문의 070*8812*0031", "CLINICAL TRIAL PARTICIPANTS WANTED · neural network analysis · CoraTech Inc. · 070*8812*0031", "", "", "", "전단 정보 표시. 소비 플래그는 씬 완료 후 on_complete_effects에서 적용"),
    # ---------- [더미] NPC간 대화 · 거리 선택지 · proximity (v3.3 거리 기능 전수 데모) ----------
    ("np_shiba_3", 1, "say", "shiba", "idle", "또 왔냐, 시바.", "You again, shiba.", "", "", "", "거리 선택지 데모 씬"),
    ("np_shiba_3", 2, "say", "luna",  "idle", "네. 지나가던 길이에요.", "Yes. Just passing by.", "", "", "", "루나 참여 — 이름형 판정"),
    ("np_shiba_3", 3, "say", "shiba", "idle", "…뭐, 볼일 있으면 빨리 말해, 시바.", "...If you want something, spit it out, shiba.", "", "", "", ""),
    ("np_shiba_3", 4, "choice", "", "ch_st_shiba", "", "", "", "", "", "거리 첫 선택지 — 조건 항목·goto 포함"),
    ("np_shiba_3", 5, "say", "shiba", "idle", "손대면 문다, 시바.", "Touch me and I bite, shiba.", "", "", "", "goto 없는 항목을 고르면 이어지는 줄"),
    ("np_shiba_treat", 1, "say", "shiba", "idle", "…흥. 뭐, 못 먹을 건 아니네, 시바.", "...Hmph. Well, it's not inedible, shiba.", "", "", "", "goto 결과 씬"),
    # ---------- [Day 99/거리] 구조적으로 지원하는 상황 전수 QA ----------
    ("qa_np_conversation", 1, "say", "street_citizen_a", "idle",
     "너 그거 들었어? 애니멀 갱단 보스 그 새끼가 갑자기 프로이트 갱 놈들을 싹 쓸어버렸대.",
     "Did you hear? That bastard running the Animal gang suddenly wiped out a whole crew of Freud gangsters.",
     "", "", "", "E 상호작용 후 시작. 이동·다른 상호작용 잠금"),
    ("qa_np_conversation", 2, "say", "street_citizen_b", "idle",
     "뭐? 좀 잠잠하다 싶더니 또 개지랄이군.",
     "What? Things finally seemed quiet, and now they're raising hell again.",
     "", "", "", "다음 대사 입력으로만 진행"),
    ("qa_np_conversation", 3, "say", "street_citizen_a", "idle",
     "그러게 말이야. 하여간 짐승 새끼들 두목답다니까.",
     "Exactly. Figures their boss would act like the animal he is.",
     "", "", "", "마지막 줄 완료 후에만 이동·상호작용 잠금 해제"),
    ("qa_object_text", 1, "say", "", "", "벽에 '오늘은 새벽 3시에 전력이 끊깁니다.'라는 공지가 붙어 있다.",
     "A notice on the wall reads, 'Power will be cut at 3:00 a.m. today.'", "", "", "", "사물 위 공용 말풍선"),
    ("qa_step_probe", 1, "effect", "", "", "", "", "", "flag.qa_street_probe = true", "", "먼저 플래그 생성"),
    ("qa_step_probe", 2, "say", "", "", "플래그가 참이므로 이 줄은 보여야 한다.",
     "This line must appear because the flag is true.", "flag.qa_street_probe", "", "", "보여야 정상"),
    ("qa_step_probe", 3, "say", "", "", "이 줄이 보이면 스텝 when 건너뛰기가 고장 난 것이다.",
     "If this line appears, step-level when skipping is broken.", "!flag.qa_street_probe", "", "", "보이면 안 됨"),
    ("qa_step_probe", 4, "effect", "", "", "", "", "", "flag.qa_street_probe = false", "", "반복 테스트용 리셋"),
    ("qa_npc_monologue", 1, "say", "street_citizen_c", "idle", "어제부터 골목 자판기가 또 먹통이네.",
     "That alley vending machine has been broken again since yesterday.", "", "", "", "E로 시작하는 1인 NPC 독백"),
    ("qa_npc_monologue", 2, "say", "street_citizen_c", "idle", "이 동네엔 멀쩡한 게 하나도 없어.",
     "Nothing in this neighborhood works the way it should.", "", "", "", "마지막 줄 후 입력 잠금 해제"),
    ("qa_choice", 1, "say", "street_citizen_d", "idle", "선택지 실행 방식을 하나 골라 봐.",
     "Choose one of the choice-flow tests.", "", "", "", "선택지 QA 안내"),
    ("qa_choice", 2, "choice", "", "ch_qa_street_choice", "", "", "", "", "", "continue·goto·조건 잠금 선택지 세트"),
    ("qa_choice", 3, "say", "street_citizen_d", "idle", "goto가 비어 있으니 원래 씬의 다음 줄로 이어졌어.",
     "Because goto was empty, execution continued to the next line of the current scene.", "", "", "", "goto null 결과"),
    ("qa_choice_result", 1, "say", "street_citizen_d", "idle", "조건이 걸린 선택지의 effects를 적용한 뒤 goto 씬으로 이동했어.",
     "The conditional choice applied its effects before moving to the goto scene.", "flag.qa_choice_route", "", "", "조건 선택지 goto 결과"),
    ("qa_choice_result", 2, "say", "street_citizen_d", "idle", "조건 없는 선택지에서 바로 goto 씬으로 이동했어.",
     "The unconditional choice moved directly to the goto scene.", "!flag.qa_choice_route", "", "", "무조건 선택지 goto 결과"),
    ("qa_choice_result", 3, "effect", "", "", "", "", "", "flag.qa_choice_unlocked = false; flag.qa_choice_route = false", "", "반복 검증을 위해 QA 플래그 초기화"),
    ("qa_sequence_1", 1, "say", "", "", "첫 번째 조사: 기기 표시창이 깜빡인다.",
     "First inspection: the device display flickers.", "", "", "", "sequential 1단계"),
    ("qa_sequence_2", 1, "say", "", "", "두 번째 조사: 표시창에 암호화된 숫자가 떠오른다.",
     "Second inspection: encrypted numbers appear on the display.", "", "", "", "sequential 2단계"),
    ("qa_sequence_3", 1, "say", "", "", "세 번째 조사: '접근 권한 없음.' 더 이상 변하지 않는다.",
     "Third inspection: 'Access denied.' It no longer changes.", "", "", "", "sequential 마지막 장면에서 고정"),
    # --- ed_bad_gold: 유지비 미납 엔딩 (데모용 텍스트 엔딩 — 연출 없음) ---
]
# 구형 10필드 행 정규화 (sync="" 를 effects 뒤에 삽입).
# dialogue_id는 BAR_STEPS까지 합친 뒤 한 번만 넣는다.
STEPS = [r if len(r) == 11 else tuple(list(r[:9]) + [""] + [r[9]]) for r in STEPS]

CHOICE_COLS = ["choice_id","seq","text_ko","text_en","when","effects","goto","note","lock_reason_ko","lock_reason_en"]
CHOICES = [
    ("ch_d1_home",  1, "지낼 만해요.",                  "It's livable.",                          "", "affinity.chris += 2", "", ""),
    ("ch_d1_home",  2, "…아직 모르겠어요.",             "...I don't know yet.",                   "", "affinity.chris += 1", "", "솔직함도 나쁘지 않음"),
    ("ch_d3_cat",   1, "골목에서 노란 꼬리를 봤어요.",  "I saw a yellow tail in the alley.",      "flag.d3_cat_seen", "flag.d3_cat_reported = true; affinity.aili += 2", "", "출근길 목격 시에만 노출"),
    ("ch_d3_cat",   2, "못 봤어요.",                    "No, I haven't.",                          "", "", "", ""),
    ("ch_d3_samho", 1, "몸 생각도 하면서 마셔요.",      "Mind your body while you're at it.",      "", "flag.d3_samho_worry = true; affinity.samho += 1", "", "걱정 루트"),
    ("ch_d3_samho", 2, "얼마든지요. 재고는 많으니까.",  "As many as you like. We're well stocked.", "", "flag.d3_samho_sell = true; affinity.samho += 2", "", "장사꾼 루트"),
    ("ch_d3_home",  1, "만드는 건 재밌어요.",           "Making drinks is fun.",                   "", "affinity.chris += 2", "", ""),
    ("ch_d3_home",  2, "사람이… 어렵네요.",             "People are... difficult.",                "", "affinity.chris += 2", "", "크리스: 그건 나도 그렇다"),
    # 퀘스트 데모 — 수락은 started 플래그만 세움. 진행/완료는 QuestStages의 serve: 목표가 담당
    # [더미] 거리 첫 선택지 세트 — 무조건 항목 + 조건 항목(회색 표시 데모) + goto
    ("ch_st_shiba", 1, "아뇨, 그냥 지나갈게요.",        "No, I'll just be on my way.",             "", "", "", "무조건 항목 — 씬 계속 진행"),
    ("ch_st_shiba", 2, "간식 좀 드릴까요?",             "Want a little treat?",                    "flag.has_snack", "flag.has_snack = false; flag.shiba_fed = true", "np_shiba_treat", "간식 보유 시 활성·선택 시 간식 소비", "줄 수 있는 간식이 없습니다.", "You don't have a treat to give."),
    # [Day 99/거리] continue·goto·effects·조건 잠금·lock_reason 전수 QA
    ("ch_qa_street_choice", 1, "현재 씬을 계속 본다.", "Continue the current scene.", "", "flag.qa_choice_unlocked = true", "", "goto null·effects 검증"),
    ("ch_qa_street_choice", 2, "숨겨진 통로를 묻는다.", "Ask about the hidden passage.", "flag.qa_choice_unlocked", "flag.qa_choice_route = true", "qa_choice_result", "조건 활성·goto 검증", "먼저 '현재 씬을 계속 본다'를 선택해야 합니다.", "Choose 'Continue the current scene' first."),
    ("ch_qa_street_choice", 3, "바로 결과 씬으로 간다.", "Go straight to the result scene.", "", "", "qa_choice_result", "무조건 goto 검증"),
    # 오브젝트 행동 선택지 — 루나 말풍선 없이 버튼만 뜬다(항목 문구 = 플레이어 행동)
]

# ── 구엔진 실대본(바 2부) 병합 — 가안을 실제 게임 대본으로 대체 (26.07.18) ──
# 원본: StreamingAssets/day0·1·2.json → converted/dayN_bar.py (자세한 것은 day_bar_scripts.py)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from day_bar_scripts import BAR_SCENES, BAR_STEPS, BAR_CHOICES, REPLACED_SCENES, REPLACED_CHOICES
SCENES = [s for s in SCENES if s[0] not in REPLACED_SCENES] + BAR_SCENES
STEPS = [s for s in STEPS if s[0] not in REPLACED_SCENES] + BAR_STEPS
CHOICES = [c for c in CHOICES if c[0] not in REPLACED_CHOICES] + BAR_CHOICES

# ── QA 테스트 대본 병합 (day 99 = 일반 진행에서 안 열림) — 제거 시 이 블록과 converted/day99_test.py만 삭제 ──
from converted.day99_test import TEST_SCENES, TEST_STEPS, TEST_CHOICES, TEST_WAVES
# 거리 QA(씬·스텝·선택지·지점)는 전부 시트가 정본이다 — InteractPoints 시트의 p_qa_* 행이
# qa/interact_points_day99.json으로, Scenes/Steps의 day 99 street 씬이 script/qa/street_day99.json으로 분리 배포된다.

# ── 거리 v2.6 씬·스텝 시드 정합 (구 build.py 오버레이를 시드로 흡수 — 시트·시드·json 3자 일치 유지) ──
# start_mode: 거리 phase 씬만 사용 — referenced(InteractPoints가 호출)/manual(choice.goto 등이 호출)
# on_complete_effects: 씬 마지막 스텝 완료 시 1회 적용. 스텝 effects로 흉내내지 않는다(중복 적용 방지).
def _street_v26_scene_step_migration(scenes, steps):
    when_overrides = {
        "qa_sequence_1": "!flag.qa_sequence_1_done",
        "qa_sequence_2": "flag.qa_sequence_1_done && !flag.qa_sequence_2_done",
        "qa_sequence_3": "flag.qa_sequence_2_done",
    }
    complete_effects = {
        "d3_alley_cat": "flag.d3_cat_seen = true",
        "ob_experiment_1": "flag.seen_coratech_ad = true",
        "qa_sequence_1": "flag.qa_sequence_1_done = true",
        "qa_sequence_2": "flag.qa_sequence_2_done = true",
    }
    street_phases = {"street", "commute_in", "commute_out"}
    out_scenes = []
    for row in scenes:
        vals = list(row[:9]) + [None] * max(0, 9 - len(row))
        scene_id, phase, trigger = vals[0], vals[2], vals[4]
        if scene_id in when_overrides:
            vals[5] = when_overrides[scene_id]
        start_mode = "" if phase not in street_phases else ("manual" if trigger == "manual" else "referenced")
        out_scenes.append(tuple(vals[:9] + [start_mode, complete_effects.get(scene_id, "")]))
    out_steps = []
    for row in steps:
        d = dict(zip(STEP_COLS[:len(row)], row))
        if d["scene_id"] == "d3_alley_cat" and d["type"] == "effect" and d.get("effects") == "flag.d3_cat_seen = true":
            continue   # 씬 on_complete_effects로 이동 — 스텝 중복 적용 방지
        vals = list(row)
        if d["scene_id"] == "ob_experiment_1" and d.get("effects") == "flag.seen_coratech_ad = true":
            vals[STEP_COLS.index("effects")] = ""
        if d["scene_id"] == "qa_npc_monologue" and d.get("actor") == "street_citizen_a":
            vals[STEP_COLS.index("actor")] = "street_citizen_c"   # 동시 배치 복제 방지 — 독백 전용 화자
        if d["scene_id"] in ("qa_choice", "qa_choice_result") and d.get("actor") == "street_citizen_b":
            vals[STEP_COLS.index("actor")] = "street_citizen_d"   # 선택지 전용 화자
        out_steps.append(tuple(vals))
    return out_scenes, out_steps
SCENES += [r if len(r) == len(SCENE_COLS) else tuple(list(r) + [False, ""]) for r in TEST_SCENES]   # skippable·group 패딩(1013행과 동일)
STEPS += TEST_STEPS
CHOICES += TEST_CHOICES
RANDOM_WAVES += TEST_WAVES

SCENES, STEPS = _street_v26_scene_step_migration(SCENES, STEPS)

# Choices v2.3: 기존 8필드 대본은 잠금 사유 2필드를 공란으로 패딩한다.
# 조건부 선택지는 아래 validate()에서 잠금 사유 ko/en을 모두 요구한다.
CHOICES = [r if len(r) == len(CHOICE_COLS) else tuple(list(r) + [""] * (len(CHOICE_COLS) - len(r)))
           for r in CHOICES]

# gen_luna_data.py의 내장 시드 전용 이관값. 실제 저작/빌드는 LUNA_Narrative.xlsx에 저장된
# dialogue_id를 그대로 읽으며 build.py가 ID를 자동 생성하거나 재번호하지 않는다.
_dialogue_serial = {}
_steps_with_dialogue_id = []
for _step in STEPS:
    if len(_step) != 11:
        raise ValueError(f"Steps 시드 행 길이 오류: {_step}")
    _row = list(_step) + [""]
    if _row[2] == "say" or (_row[2] == "order" and (_row[5] or _row[6])):
        _dialogue_serial[_row[0]] = _dialogue_serial.get(_row[0], 0) + 1
        _row[11] = f"dlg_{_row[0]}_{_dialogue_serial[_row[0]]:03d}"
    _steps_with_dialogue_id.append(tuple(_row))
STEPS = _steps_with_dialogue_id

ORDER_COLS = ["order_id","seq","when","verdict","effects","react","note"]
ORDERS = [
    ("d4_samho_dilemma", 1, "cocktail.abv >= 20",                 "fulfill", "flag.samho_drunk = true",                       "", "[후속 일차 가안] 독한 술 → 취함 → 사망 루트"),
    ("d4_samho_dilemma", 2, "cocktail.abv <= 5 && grade >= good", "care",    "flag.samho_calmed = true; affinity.samho += 2", "", "[후속 일차 가안] 배려 정답"),
    ("d4_samho_dilemma", 3, "cocktail.abv <= 5",                  "partial", "",                                              "", "[후속 일차 가안]"),
    ("d4_samho_dilemma", 4, "",                                   "miss",    "",                                              "", "[후속 일차 가안] default"),
]

QUEST_COLS = ["id","title_ko","title_en","kind","reward_effects","note"]
QUESTS = [
    # serve: 목표 + 재료/레시피 해금 데모 — 재료는 Ingredients.unlock_when(다음날 입고), 레시피는 unlock_recipe(즉시)
]
QSTAGE_COLS = ["quest_id","stage","goal","when","on_complete"]
QUEST_STAGES = [
]

END_COLS = ["priority","id","when","scene_id","note"]
ENDINGS = [
    # bad_gold만 매일 정산 확정(유지비 차감 포함) 직후 판정, 나머지는 최종일 판정
    (1, "bad_1",    "flag.rios_accepted",                                                                           "ed_bad1",    "[예약] 최종일 리오스 제안 수락 선택지에서 flag.rios_accepted = true 설정 후 사용 — 현재 데모 미도달"),
    (2, "happy_1",  "affinity.aili >= 100 && affinity.tom >= 100 && affinity.port >= 100 && affinity.sunha >= 100", "ed_happy1",  "벡터 고발, 전체 생존"),
    (3, "happy_2",  "affinity.aili >= 100 && affinity.tom >= 100 && affinity.port >= 100",                          "ed_happy2",  "언노운 이사, 전체 생존"),
    (4, "normal_1", "alive.haru && affinity.haru >= 100",                                                           "ed_normal1", "하루 루트, 크리스 사망"),
    (5, "bad_2",    "",                                                                                             "ed_bad2",    "폴백 — 납치"),
]

CONFIG_COLS = ["key","value","type","note"]   # type: 엑셀 왕복에서 3.0→3으로 뭉개지는 것을 막는 명시 타입 (v2.2)
CONFIG = [
    ("data_schema_version",     "2.7.0", "제조·운영 데이터 계약 버전. 집·외부 공용 InteractPoint와 수동 Transition 계약 포함"),
    ("gold_start",             300,     "시작 골드"),
    ("reputation_start",       0,       "시작 평판"),
    ("commute_in_time",        "19:00", "출근 시각(연출 표기용). 배경은 밤 고정 단일 리소스"),
    ("commute_out_time",       "02:00", "퇴근 시각(연출 표기용)"),
    # ── 인내심 (제조 개편: 티어 폐지 — 코스터는 고정, 서빙 여유는 해금일 기반) ──
    ("coaster_base_sec",       22,      "코스터 인내심 기본 × 성격 patience_mult"),
    ("coaster_min_sec",        12,      "코스터 인내심 하한"),
    ("coaster_max_sec",        60,      "코스터 인내심 상한"),
    ("serve_bonus_sec",        40,      "서빙 여유 기본 — 서빙 인내심 = 제한시간 + max(하한, 기본 − 해금일×감소)"),
    ("serve_grace_per_day_sec", 3,      "칵테일 해금일(0일차 스타트)당 여유 감소 [가안]"),
    ("serve_min_bonus_sec",    10,      "서빙 여유 하한"),
    ("warn_yellow_ratio",      0.5,     "노란 경고 임계"),
    ("warn_red_ratio",         0.8,     "빨간 점멸 임계"),
    # ── 제조 흐름 정지·판정 (제조 개편 확정) ──
    ("craft_pause_start",      "cocktail_menu_enter", "칵테일 메뉴 진입 시 코스터·서빙·스폰 타이머 정지 — 레시피 열람은 제조 시간 미포함"),
    ("craft_pause_end",        "return_to_table_after_serve_choice", "제공하기/버리기 선택 후 테이블 복귀 시 재개"),
    ("discard_keeps_pause",    True,    "버리기→재선택→재제조 동안 계속 정지"),
    ("force_sewage_on_order_mismatch", True, "서빙 시 주문과 제공 칵테일이 다르면 final_grade만 Sewage로 확정. craft_score·craft_grade는 보존"),
    ("force_sewage_on_missing_core", True, "핵심 재료(is_core) 누락 시 Sewage 확정"),
    ("craft_score_formula",     "weighted_representative_v1", "종류별 평균 → 현재 채점 기믹 가중치 정규화 → 고정 감점 차감"),
    ("score_aggregation_mode",  "mean_per_action_then_weighted", "같은 기믹 종류의 개별 점수를 먼저 평균낸 뒤 종류별 가중치를 적용"),
    ("normalize_present_gimmick_weights", True, "현재 제조에서 실제로 채점한 기믹 종류의 가중치 합으로 정규화"),
    ("fill_up_weight_source",   "weight_pour", "Fill-up은 별도 가중치 없이 Pour 종류에 포함하여 weight_pour를 공유"),
    ("craft_timer_scope",       "manual_input_active_only", "수동 기믹의 입력 활성 시간만 전체 제조시간에 합산"),
    # ── 기믹 파라미터 (제조 개편 확정: 셰이킹 20·스터 10, 기믹별 제한시간 없음) ──
    ("shake_target_stacks",    20,      "셰이킹 목표 = 성공+실패 합 20스택 자동 종료, 조기 실패 종료 없음"),
    ("open_approach_sec",      1.6,     "조여드는 고리가 시작 반지름에서 목표 반지름까지 접근하는 시간 [가안]"),
    ("open_judge_window_px",   10,      "목표 반지름과의 차이를 성공으로 인정하는 픽셀 범위 [가안]"),
    ("open_start_radius_px",   165,     "병따기 조여드는 고리의 시작 반지름 [가안]"),
    ("open_target_radius_px",  44,      "병따기 목표 고리 반지름 [가안]"),
    ("open_input_enabled_after_sec", 0.0, "기믹 진입 후 입력을 허용하기까지의 대기시간 [가안]"),
    ("open_transition_delay_sec", 0.9,  "병따기 성공 연출 후 따르기 전환까지의 시간. 수동 입력 비활성 구간이므로 전체 제조시간에서 제외 [가안]"),
    ("open_penalty_per_failure", 15,    "병따기 점수 = max(0, 100 − 실패×15) [가안]"),
    ("powder_demo_behavior",   "auto_apply_unscored", "데모: 파우더(설탕) 자동 투입·비채점 — 정식 버전에서 기믹 복원"),
    ("squeeze_demo_behavior",  "auto_apply_unscored", "데모 스퀴즈는 자동 적용·비채점. 기믹 큐·제조시간·가중치 정규화에서 제외"),
    ("ice_demo_behavior",      "auto_apply_unscored", "얼음은 자동 적용·비채점. UI에는 완성 잔의 얼음 여부만 표시"),
    ("pour_start_angle_deg",   95.0,    "액체 방출을 시작하는 병 각도 [가안]"),
    ("pour_max_tilt_angle_deg", 150.0,  "병이 기울어질 수 있는 최대 각도 [가안]"),
    ("pour_tilt_speed_deg_per_sec", 95.0, "입력 유지 중 병 각도 변화 속도 [가안]"),
    ("pour_emit_rate_ml_per_sec", 70.0, "유체 프로토타입의 초당 방출량(ml/s) [가안]"),
    ("pour_quantity_update_interval_sec", 0.05, "Actual 수량을 화면과 런타임에 갱신하는 주기 [가안]"),
    ("shake_bpm",              60.0,    "스트라이커가 지그재그 경로를 왕복하는 기준 BPM [가안]"),
    ("shake_judge_radius_units", 1.0,   "Shake 입력 성공 판정 반경. 엔진 월드 단위 [가안]"),
    ("shake_node_lifetime_sec", 2.0,    "패턴 노드가 판정 대상으로 유지되는 시간 [가안]"),
    ("shake_pattern_node_min_count", 1, "한 패턴에 생성하는 패턴 노드 최소 개수 [가안]"),
    ("shake_pattern_node_max_count", 3, "한 패턴에 생성하는 패턴 노드 최대 개수 [가안]"),
    # ── 채점 (제조 개편: 감점·가중치 — 오차 구간은 ScoreBands 시트) ──
    ("glass_mismatch_penalty", 10,      "정답 잔 불일치 감점 [가안]"),
    ("tool_mismatch_penalty",  10,      "정답 도구 불일치·미선택 감점 [가안]"),
    ("missing_ingredient_penalty", 15,  "일반(비핵심) 정답 재료 누락 1개당 감점 — 누락 기믹 0점과 이중 적용 금지 [가안]"),
    ("extra_ingredient_penalty", 10,    "정답에 없는 직접 선택 재료 1개당 감점 [가안]"),
    ("weight_open",            10,      "기믹 가중치 — 병따기 [가안]"),
    ("weight_pour",            20,      "기믹 가중치 — 따르기 [가안]"),
    ("weight_squeeze",         10,      "기믹 가중치 — 스퀴즈 [가안]"),
    ("weight_powder",          5,       "기믹 가중치 — 파우더(데모 제외, 정식용 예약) [가안]"),
    ("weight_shake",           20,      "기믹 가중치 — 셰이킹 [가안]"),
    ("weight_stir",            20,      "기믹 가중치 — 스터 [가안]"),
    ("unit_oz_to_ml",          30,      "수량 계산용 온스 환산(게임 단순화 값)"),
    ("unit_tsp_to_ml",         5,       "수량 계산용 티스푼 환산"),
    ("spawn_delay_default_sec", 25,     "슬롯 delay 공란 시 기본"),
    ("next_round_delay_sec",   3,       "다회 주문 다음 잔 텀"),
    ("drunk_vomit_chance",     0.4,     "3잔째 제공 시 토함 확률 [TBD]"),
    ("leave_coaster_rep",      -1,      "코스터 미제공 이탈 평판"),
    ("leave_serve_rep",        -2,      "서빙 지연 이탈 평판"),
    # ── 저장·불러오기 (고정 전환점 정책 — Day 0 구현 기준) ──
    ("save_checkpoint_policy", "fixed_transitions_v1", "자동 저장은 지정된 네 전환점에서만 수행. 대사·손님·기믹 단위 스냅숏은 사용하지 않음"),
    ("manual_save_allowed_phase", "home", "수동 저장 허용 구간. 집(home)에서만 가능"),
    ("manual_save_slot_count", 5,        "플레이어가 관리하는 수동 저장 슬롯 수"),
    ("autosave_slot_count",    1,        "게임이 자동으로 갱신하는 자동 저장 슬롯 수"),
    ("autosave_on_commute_in_enter", True, "집을 나서 출근길(commute_in)에 진입할 때 자동 저장"),
    ("autosave_on_bar_enter",  True,     "바에 처음 들어가는 시점에 자동 저장"),
    ("autosave_on_part2_start", True,    "바 운영 2부 시작 시 자동 저장"),
    ("autosave_on_daily_sales_settlement_complete", True, "2부 종료 후 일일 매출 정산 커밋이 완료된 시점에 자동 저장"),
    ("commute_in_resume_spot_id", "home_door", "출근길 자동 저장을 불러올 때 사용하는 외부 거리 위치 앵커"),
    ("commute_out_resume_spot_id", "bar_door", "일일 매출 정산 완료 자동 저장을 불러올 때 사용하는 퇴근길 위치 앵커"),
    ("bar_mid_session_autosave_enabled", False, "1부·2부 진행 중 자동 저장과 중간 복구를 사용하지 않음"),
    # ── v2.3 바 내부 명세서 확정분 (26.07.24) ──
    ("typing_interval_ms",     50,      "대사 타이핑 문자당 간격(ms). 태그로 구간 오버라이드(TextTags)"),
    ("first_spawn_delay_sec",  5,       "1부 시작 후 첫 손님 스폰까지 텀(명세서 §3.3)"),
    ("reseat_delay_sec",       10,      "만석 해제 후 보류 손님 입장까지 텀(명세서 §3.3)"),
    ("sfx_guest_in",           "SFX_guest_door_in",   "공용 손님 입장 사운드(문+발소리) 키"),
    ("sfx_guest_out",          "SFX_guest_door_out",  "공용 손님 퇴장 사운드 키"),
    ("sfx_drink_high",         "SFX_drink_satisfied", "마시는 중 사운드 — Excellent·Good"),
    ("sfx_drink_mid",          "SFX_drink_neutral",   "마시는 중 사운드 — Decent"),
    ("sfx_drink_low",          "SFX_drink_grim",      "마시는 중 사운드 — Poor·Sewage"),
    ("idle_min_sec",           8,       "1부 대기 중 혼잣말(idle) 최소 간격"),
    ("idle_max_sec",           13,      "1부 대기 중 혼잣말 최대 간격"),
    ("sfx_serve",              "SFX_glass_slide", "제공 컷씬 공용 사운드(잔 미는 소리) — 구엔진 이식, 전 칵테일 공용"),
    ("order_bark_gap_sec",     1.5,     "주문 대사 사이의 텀 — ask_order→order_think→order 순서로 재생할 때, 앞 대사 타이핑이 끝나고 다음 대사까지 기다리는 초 [가안]"),
    # ── v3.0 거리 시스템 — 말풍선 상수는 바와 분리해 따로 튜닝한다 ──
    ("street_typing_interval_ms",   50, "거리 말풍선 타이핑 문자당 간격(ms) — 바(typing_interval_ms)와 별도 튜닝"),
    ("street_auto_next_delay_sec",  3,  "auto 재생 대사 — 타이핑 종료 후 다음 대사까지 텀"),
    # ── 스터 기믹 — 전 칵테일 공통, 차등이 필요해지면 tier 파생으로 전환 ──
    ("stir_target_stacks",     10,      "스터 목표 스택 수. 성공+실패 합계가 10이 되면 자동 종료"),
    ("stir_inputs_per_circle", 4,       "현재 위치에서 시계 방향 한 바퀴를 완성하는 정답 입력 수"),
    ("stir_circle_limit_sec",  2,       "현재 위치에서 한 바퀴를 완성할 제한시간. 초과하면 실패 스택 1개 [가안]"),
    ("stir_warning_ratio",     0.34,    "한 바퀴 게이지가 경고색으로 바뀌는 남은 시간 비율 [가안]"),
    ("stir_input_cooldown_sec", 0.0,    "판정 직후 다음 입력까지의 잠금 시간. 0이면 즉시 연속 입력 허용"),
    # ── 랜덤 손님 외형 ──
    ("guest_acc_none_weight",  1,       "선택 슬롯(아우터·목걸이·팔 액세서리)의 '없음' 후보 가중치 — 클수록 미착용이 흔하다. 없음 1·파츠 합 3이면 미착용 25% [가안]"),
    ("random_order_sampling_mode", "uniform_with_replacement", "order가 공란인 일반 손님 주문은 당일 해금 confirmed 풀에서 매 회차 동일 확률로 독립 추첨. 직전 주문도 다시 나올 수 있음"),
    ("serve_timeout_same_frame_priority", "timeout", "유효 코스터 드롭과 서빙 인내심 종료가 같은 프레임이면 timeout을 먼저 커밋하고 손님을 퇴장시킴"),
    ("data_error_safe_exit_after_same_code", 2, "같은 CraftAttempt에서 같은 error_code가 연속 2회 발생하면 재시도 대신 안전 이탈을 우선 안내"),
]

GRADE_CUTS = [("excellent",95),("good",80),("decent",60),("poor",35),("sewage",0)]
# 제조 개편 정산 — 구 GradePayout(등급 배율 1.2/1.0/0.7/0.3/-1.0)을 폐기하고 판매가+팁+배상 구조로 교체.
#   매출 = 가격 × sale_rate + 가격 × tip_rate × 성격 tip_mult(단골은 tip_mult 없이 일괄 1.0).
#   배상 = 가격 × refund_rate. 주문 불일치는 등급 자체가 Sewage(force_sewage_on_order_mismatch).
SETTLE_COLS = ["grade","sale_rate","tip_rate","refund_rate","note"]
SETTLEMENT_RULES = [
    ("excellent", 1.0, 0.2, 0.0, "판매가 전액 + 팁 20%"),
    ("good",      1.0, 0.0, 0.0, "판매가 전액 [팁 가안]"),
    ("decent",    1.0, 0.0, 0.0, "판매가 전액 [팁 가안]"),
    ("poor",      1.0, 0.0, 0.0, "판매가 전액 [팁 가안]"),
    ("sewage",    0.0, 0.0, 1.0, "판매가만큼 배상"),
]
# 수량 오차·시간 초과 구간표 — 위에서부터 첫 일치. min/max 포함 여부를 값으로 명시(경계 모호성 제거).
#   quantity: score_or_penalty = 그 기믹의 점수 / overtime: score_or_penalty = 전체 감점.
SCOREBAND_COLS = ["band_type","min_ratio","max_ratio","min_inclusive","max_inclusive","score_or_penalty","note"]
SCORE_BANDS = [
    ("quantity", 0.00, 0.05, True,  False, 100, "오차 0% 이상 5% 미만"),
    ("quantity", 0.05, 0.10, True,  False, 90,  "5% 이상 10% 미만"),
    ("quantity", 0.10, 0.20, True,  False, 75,  "10% 이상 20% 미만"),
    ("quantity", 0.20, 0.35, True,  False, 50,  "20% 이상 35% 미만"),
    ("quantity", 0.35, None, True,  False, 0,   "35% 이상"),
    ("overtime", 0.00, 0.00, True,  True,  0,   "제한시간 이내"),
    ("overtime", 0.00, 0.10, False, True,  5,   "0% 초과 10% 이하"),
    ("overtime", 0.10, 0.25, False, True,  10,  "10% 초과 25% 이하"),
    ("overtime", 0.25, 0.50, False, True,  20,  "25% 초과 50% 이하"),
    ("overtime", 0.50, None, False, False, 30,  "50% 초과"),
]
AFFINITY_MATRIX = [
    ("love",6,4,1,-1,-2),("good",4,3,1,-1,-2),("ok",2,1,0,-1,-3),("dislike",1,0,-1,-2,-4),("miss",-4,-4,-4,-4,-4),
]

# 취향 — 서빙 호감 판정용 (first-match: 위에서부터 첫 일치, 매치 없으면 ok)
# when에 cocktail.* 외에 flag/day/affinity도 쓸 수 있다 → "그때그때 달라지는" 상황부 취향까지 데이터로 표현
TASTE_COLS = ["character_id","seq","when","tier","note"]
TASTES = [
    ("samho", 1, "flag.samho_calmed && cocktail.abv >= 20", "dislike", "[후속 일차 가안] 배려 루트 이후엔 독주를 밀어냄 — 상황부 취향 데모"),
    ("samho", 2, "cocktail.abv >= 20",        "love",    "독한 술이 기본 취향"),
    ("samho", 3, "cocktail.tag(달콤한)",       "dislike", "장사꾼 입맛에 단 건 안 맞음"),
    ("port",  1, "cocktail.id == gin_fizz",   "love",    "수십 년 진피즈 한 우물"),
    ("port",  2, "cocktail.tag(클래식)",       "good",    "옛날 사람"),
    ("aili",  1, "cocktail.id == champagne",  "love",    "새벽술은 무죄"),
    ("aili",  2, "cocktail.tag(화사한)",       "good",    ""),
    ("aili",  3, "cocktail.abv >= 25",        "dislike", "의사로서 독주는 사양"),
    ("chris", 1, "cocktail.tag(클래식)",       "good",    "말 없는 클래식파"),
    ("bubi",  1, "cocktail.id == kahlua_milk","love",    "고양이도 탐내는 맛"),
]

# 단골 수첩(Dossier) — 바 내부 UI 탭. 만난 인물만 노출, 호감도가 오를수록 항목이 한 줄씩 열린다 (v1.9.5 PD 확정)
#   min_affinity: 이 호감도 이상일 때 공개 (0=만나면 즉시). 잠긴 항목은 "🔒 호감도 N" 티저로 표시
#   kind: desc(한 줄 소개) / taste(취향 표기 — 사람이 쓰는 소개문, 판정은 Tastes가 함) / history(과거사) / secret(숨겨진 이야기) / recent(최근 근황 — when으로 진행에 따라 갱신)
#   when: 조건부 공개(주로 recent). 같은 kind에 when이 여럿 매치되면 전부 표시(근황 누적) — 단 seq 역순 아님, min_affinity·순서대로
DOSSIER_COLS = ["character_id","min_affinity","kind","when","text_ko","text_en","note"]
DOSSIER = [
    # ── 크리스 ──
    ("chris", 10,  "desc",    "", "바 '언노운'의 마스터. 말수가 적다.", "Master of Bar Unknown. A man of few words.", ""),
    ("chris", 20, "taste",   "", "클래식 칵테일을 조용히 비운다.", "Quietly empties classic cocktails.", ""),
    ("chris", 35, "history", "", "전직은 아무도 모른다. 손님들도 묻지 않는다.", "Nobody knows what he did before. The regulars don't ask.", ""),
    ("chris", 50, "secret",  "", "루나를 받아준 이유를 아직 말하지 않았다. '때가 되면'이라고만 한다.", "He still hasn't said why he took Luna in. Only: 'when it's time.'", ""),
    # ── 포트 ──
    ("port", 10,  "desc",    "", "고물 로봇과 함께 다니는 노인. 진피즈 한 우물.", "An old man with a scrap robot. Gin Fizz, always.", ""),
    ("port", 20, "taste",   "", "수십 년째 진피즈. '정석대로'가 주문의 전부다.", "Decades of Gin Fizz. 'By the book' is his whole order.", ""),
    ("port", 35, "history", "", "골목에 쓰러져 있던 루나를 처음 발견해 데려온 사람.", "The one who first found Luna collapsed in an alley and carried her in.", ""),
    ("port", 50, "secret",  "", "그의 로봇에는 지워지지 않는 이름 하나가 저장되어 있다고 한다.", "They say one name is stored in his robot that can never be erased.", ""),
    # ── 아일리 ──
    ("aili", 10,  "desc",    "", "새벽에 나타나는 의사. 루나를 '예쁜이'라고 부른다.", "A doctor who appears at dawn. Calls Luna 'sweetie.'", ""),
    ("aili", 20, "taste",   "", "샴페인 애호가. 독주는 의사로서 사양한다.", "A champagne lover. Declines hard liquor, doctor's orders.", ""),
    ("aili", 35, "history", "", "루나의 수리에 참여했다. 몸의 흉터 위치를 전부 알고 있다.", "She took part in Luna's repairs. She knows where every scar is.", ""),
    ("aili", 50, "secret",  "", "면허가 왜 정지됐는지는 술이 꽤 들어가야 나오는 이야기다.", "Why her license was suspended is a story that takes quite a few drinks.", ""),
    # ── 삼호 ──
    ("samho", 10,  "desc",    "", "애니멀 갱의 말단. 시끄럽고, 정이 많다.", "Animal Gang's newest grunt. Loud, and soft-hearted.", ""),
    ("samho", 20, "taste",   "", "독한 술을 동경한다. 보스가 마시는 마티니처럼.", "Admires hard liquor. Like the martini his boss drinks.", ""),
    ("samho", 35, "history", "", "부모가 데려온 일곱 동생을 혼자 돌보고 있다.", "Raising the seven siblings his parents took in — alone now.", ""),
    ("samho", 50, "secret",  "", "전 재산을 털어 산 아머는 동생들 앞에서 멋있어 보이고 싶어서였다.", "The armor he bought with everything he had — he just wanted to look cool for his siblings.", ""),
    ("samho", 0,  "recent",  "flag.samho_death_route", "취한 채로 꽃을 가지러 나갔다. …밖이 소란스럽다.", "Went out drunk to fetch a flower. ...It's loud outside.", "취함 루트 근황"),
    ("samho", 0,  "recent",  "flag.samho_refused_drink", "내일 노란 꽃을 보여주겠다고 약속했다.", "Promised to show a yellow flower tomorrow.", "거절 루트 근황"),
    # ── 부비 ──
    ("bubi", 10,  "desc",    "", "아일리를 따라다니는 고양이. 잘 잔다.", "The cat that follows Aili around. Sleeps well.", ""),
    ("bubi", 20, "taste",   "", "깔루아 밀크가 나오면 눈이 떠진다.", "Eyes open the moment Kahlua Milk appears.", ""),
]

UI_COLS = ["key","ko","en"]
UI_STRINGS = [
    ("ui_grade_excellent", "Excellent", "Excellent"),
    ("ui_grade_good",      "Good",      "Good"),
    ("ui_grade_decent",    "Decent",    "Decent"),
    ("ui_grade_poor",      "Poor",      "Poor"),
    ("ui_grade_sewage",    "Sewage",    "Sewage"),
    ("ui_make",        "제조하기",           "Make"),
    ("ui_serve",       "제공하기",           "Serve"),
    ("ui_discard",     "버리기",             "Discard"),
    ("ui_recipe_note", "레시피 노트",        "Recipe Note"),
    ("ui_menu",        "메뉴",               "Menu"),
    ("ui_settlement",  "정산",               "Settlement"),
    ("ui_stock_in",    "재료 입고",          "New Stock"),
    ("ui_end_day",     "하루 마치기",        "End the Day"),
    ("ui_wait_chris",  "크리스를 기다린다",  "Wait for Chris"),
    ("ui_sleep",       "잠에 든다",          "Go to Sleep"),
    ("ui_next",        "다음",               "Next"),
    ("ui_confirm",     "확인",               "OK"),
    ("ui_cancel",      "취소",               "Cancel"),
    ("ui_save_prompt", "저장하시겠습니까?",  "Save your progress?"),
    ("ui_continue",    "이어하기",           "Continue"),
    ("ui_load",        "불러오기",           "Load"),
    ("ui_autosave",    "자동 저장",          "Autosave"),
    ("ui_manual_save", "수동 저장",          "Manual Save"),
    ("ui_empty_slot",  "빈 슬롯",            "Empty Slot"),
    ("ui_save_home_only", "수동 저장은 집에서만 가능합니다.", "Manual saving is only available at home."),
    ("ui_home_exit", "밖으로 나간다", "Go Outside"),
    ("ui_home_exit_blocked", "지금은 집을 나갈 수 없습니다.", "You cannot leave home right now."),
    ("ui_home_sleep_confirm", "하루를 종료하시겠습니까?", "End the day?"),
    ("ui_save_overwrite_confirm", "이 슬롯에 덮어쓰시겠습니까?", "Overwrite this save slot?"),
    ("ui_save_success", "저장되었습니다.", "Game saved."),
    ("ui_autosave_failed", "자동 저장에 실패했습니다. 이전 자동 저장은 유지됩니다.", "Autosave failed. Your previous autosave has been preserved."),
    ("ui_home_map_transition_failed", "이동하지 못했습니다. 잠시 후 다시 시도해 주세요.", "Unable to move to the next area. Please try again."),
    ("ui_new_game",    "새 게임",            "New Game"),
    ("ui_sales",       "매출",               "Sales"),
    ("ui_tips",        "팁",                 "Tips"),
    ("ui_reputation",  "평판",               "Reputation"),
    ("ui_give_drink",  "제공한다",           "Serve it"),
    ("ui_refuse_drink","돌려보낸다",         "Send them home"),
    ("ui_crash_resume","지난 세션이 비정상 종료되었습니다. 마지막 자동 저장에서 다시 시작할까요?", "The last session ended unexpectedly. Restart from the latest autosave?"),
    ("ui_open_sign",   "영업 시작",              "OPEN"),
    ("ui_closed_sign", "준비 중",                "CLOSED"),
    ("ui_open_hint",   "간판을 걸면 손님이 들어온다", "Flip the sign to let the guests in"),
    ("ui_shelf_glass", "잔",                     "Glass"),
    ("ui_shelf_tool",  "도구",                   "Tools"),
    ("ui_shelf_garnish","가니시",                "Garnish"),
    ("ui_shelf_ing",   "재료",                   "Ingredients"),
    # ── v2.3 바 내부 명세서 확정분 (26.07.24) ──
    ("ui_guest_name",        "손님",                          "Guest"),
    ("ui_toast_no_guest",    "응대할 손님이 없습니다",        "No guest to serve"),
    ("ui_toast_not_ordered", "아직 주문을 받지 않았습니다",   "No order taken yet"),
    ("ui_toast_serve_first", "먼저 만든 칵테일을 서빙하세요", "Serve the finished drink first"),
    ("ui_guest_list",        "손님 리스트",                   "Guest List"),
    ("ui_sales_report",      "매출 현황",                     "Sales Report"),
    ("ui_data_error_title",       "제조 데이터를 처리할 수 없습니다", "Craft data could not be processed"),
    ("ui_data_error_body",        "제조 데이터를 처리하는 중 오류가 발생했습니다. 같은 제조 내용으로 다시 시도하거나 이번 제조를 취소할 수 있습니다.", "An error occurred while processing the craft data. Retry with the same craft setup or cancel this craft."),
    ("ui_data_error_retry",       "다시 시도", "Retry"),
    ("ui_data_error_cancel",      "제조 취소", "Cancel Craft"),
    ("ui_data_error_repeat_body", "같은 오류가 다시 발생했습니다. 진행 데이터 보호를 위해 안전하게 나간 뒤 이어하기로 다시 시작하는 것을 권장합니다.", "The same error occurred again. To protect your progress, we recommend exiting safely and resuming from Continue."),
    ("ui_data_error_safe_exit",   "안전하게 나가기", "Exit Safely"),
    ("ui_return_to_title",        "타이틀로 돌아가기", "Return to Title"),
]

# v2.3 텍스트 태그 확장 (26.07.24 확정) — <태그>…</태그> 연출의 값 정의 (명세서 §2.2).
# kind: color(강조색 hex) / speed_ms(구간 문자당 출력 간격 ms) / size_pct(구간 글자 크기 %)
# 숫자 태그 <2000>은 등록 없이 '2000ms 대기'로 예약. {cocktail}은 치환 변수(태그 아님).
TEXTTAG_COLS = ["tag","kind","value","note"]
TEXT_TAGS = [
    ("world", "color",    "#6EC1FF", "세계관 키워드 강조 (기존 <world> 태그)"),
    ("name",  "color",    "#FFD479", "칵테일·용어 이름 강조 (튜토리얼 대본에서 사용 중)"),
    ("order", "color",    "#8FE388", "주문·조작 지시 강조 (튜토리얼 대본에서 사용 중)"),
    ("slow",  "speed_ms", 120,       "천천히 말하기"),
    ("fast",  "speed_ms", 20,        "빠르게 몰아치기"),
    ("big",   "size_pct", 130,       "글자 확대 (외침)"),
    ("small", "size_pct", 80,        "글자 축소 (속삭임)"),
]

# v2.5 상황 사전 + 기본 표정 — barks.situation의 정본 목록. 여기 없는 상황을 쓰면 빌드 에러(오타 차단).
# default_expression: 그 상황에서 손님이 짓는 기본 표정(공용 common 세트). bark 행의 expression이 비어 있을 때 사용.
BARKSIT_COLS = ["situation","default_expression","note"]
BARK_SITUATIONS = [
    ("call",            "default",   "첫 호출"),
    ("call_urge",       "annoyingB", "재촉(인내심 50%)"),
    ("call_final",      "annoyingR", "최종 재촉(80%)"),
    ("ask_order",       "default",   "루나가 주문을 물음(루나 전용)"),
    ("order_think",     "default",   "주문 고민"),
    ("order",           "default",   "주문 확정"),
    ("reorder",         "joy",       "다회 주문 재주문"),
    ("serve_urge",      "annoyingB", "서빙 재촉(50%)"),
    ("serve_final",     "annoyingR", "서빙 최종 재촉(80%)"),
    ("serve_thanks",    "joy",       "수령 감사(마시기 전)"),
    ("react_excellent", "joy",       "판정 반응"),
    ("react_good",      "joy",       "판정 반응"),
    ("react_decent",    "default",   "판정 반응"),
    ("react_poor",      "annoyingB", "판정 반응 — 찡그림"),
    ("react_sewage",    "annoyingR", "판정 반응 — 강한 찡그림"),
    ("wrong_receive",   "surprise",  "오제조 수령 — 의아"),
    ("wrong_drink",     "anger",     "오제조 확정 — 항의"),
    ("bye_good",        "joy",       "긍정 퇴장"),
    ("bye_bad",         "anger",     "부정 퇴장"),
    ("leave_coaster",   "anger",     "코스터 미제공 이탈"),
    ("leave_serve",     "anger",     "서빙 지연 이탈"),
    ("idle",            "default",   "대기 혼잣말"),
    ("drunk_enter",     "joy",       "3잔째 취함 진입"),
    ("drunk_vomit",     "fear",      "토함"),
]

# ============================================================
# 0일차 스타트 일괄 변환 (제조 개편 확정 — 표시 일차 = 데이터 일차)
# 시드는 전부 구표기(1일차 스타트)로 적혀 있고, 여기서 한 번에 −1 시프트한다.
#   · 씬의 '상시' 표식: 구표기 day 0 → None (json에선 null). 0은 이제 진짜 첫날(튜토리얼)이다.
#   · when 문자열의 day 비교("day >= 2" 등)도 같은 규칙으로 숫자만 −1.
#   · build.py는 시트(이미 시프트된 값)를 주입하므로 이중 시프트가 없다 — 이 블록은 시드 전용.
def _sd(v, sentinel=False):
    if v in (None, ""): return None
    if v == 0: return None if sentinel else 0
    if v >= 99: return v          # 99 = '날짜로는 안 열림' 컨벤션 — 시프트 대상 아님
    return v - 1
def _shift_col(rows, idx, sentinel=False):
    return [tuple(list(r)[:idx] + [_sd(r[idx], sentinel)] + list(r)[idx + 1:]) for r in rows]
def _shift_when(rows, idx):
    def f(s):
        return re.sub(r"(day\s*(?:==|>=|<=|>|<)\s*)(\d+)",
                      lambda m: m.group(1) + str(int(m.group(2)) if int(m.group(2)) >= 99 else int(m.group(2)) - 1), s) if s else s
    return [tuple(list(r)[:idx] + [f(r[idx])] + list(r)[idx + 1:]) for r in rows]

COCKTAILS     = _shift_col(COCKTAILS, CK_COLS.index("unlock_day"))
SHELF_ITEMS   = _shift_col(SHELF_ITEMS, SHELF_COLS.index("unlock_day"))
DAYS          = _shift_col(DAYS, 0)
SCENES        = _shift_col(SCENES, 1, sentinel=True)
RANDOM_WAVES  = _shift_col(RANDOM_WAVES, 0)
REGULAR_SLOTS = _shift_col(REGULAR_SLOTS, 0)
SCENES        = _shift_when(SCENES, SCENE_COLS.index("when"))
# ⚠ 시드 신선도 경고(v2.6 시점): 아래 도메인은 시트가 시드보다 최신이다 — Tags(tag_id 열),
#   Cutscenes(tl_samho_* 2종), UIStrings(집 UI 확장 59키), Dossier, Characters(hound) 등.
#   재생성(gen 실행)은 이 최신분을 전수 포팅하기 전엔 금지. 평소엔 build.py만 사용한다.
#   거리 QA(씬 15종·지점 15행·간식 선택지)도 시트에만 있다 — 시드는 구세대.
POINTS = _shift_when(POINTS, POINT_COLS.index("spawn_when"))
POINTS = _shift_when(POINTS, POINT_COLS.index("interact_when"))
STEPS         = _shift_when(STEPS, STEP_COLS.index("when"))
CHOICES       = _shift_when(CHOICES, CHOICE_COLS.index("when"))
ENDINGS       = _shift_when(ENDINGS, END_COLS.index("when"))
QUEST_STAGES  = _shift_when(QUEST_STAGES, QSTAGE_COLS.index("when"))
ORDERS        = _shift_when(ORDERS, ORDER_COLS.index("when"))
TASTES        = _shift_when(TASTES, TASTE_COLS.index("when"))
COCKTAILS     = _shift_when(COCKTAILS, CK_COLS.index("unlock_when"))
SHELF_ITEMS   = _shift_when(SHELF_ITEMS, SHELF_COLS.index("unlock_when"))

# ============================================================
# 파생 계산
# ============================================================
def derive():
    # 제조 개편: 티어·해금·제한시간·채점 항목 파생 전부 폐지 — 시트가 수동 정본.
    # 남은 파생은 이미지 키뿐: 구엔진 에셋 명명 규칙 이식 — Finished_{Pascal}(대표)·Serve_{Pascal}(제공 컷씬).
    # 구엔진과 겹치는 4종(gin_fizz 등)은 기존 에셋을 그대로 재사용하고, 나머지는 같은 규칙으로 신규 발주.
    derived = {}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        pascal = "".join(w.capitalize() for w in d["id"].split("_"))
        derived[d["id"]] = dict(sprite=f"Finished_{pascal}", serve_sprite=f"Serve_{pascal}")
    return derived

# ============================================================
# 검증
# ============================================================
PLAYER_ACTION_STEPS = {"craft", "serve", "choice"}

# L10N 정책 (26.07.19 PD 확정): 영어 번역은 8월 중후반 착수 — 그때까지 en 누락은 '경고 집계'만.
# 번역 완료 후 build.py --strict 로 빌드하면 기존처럼 에러로 승격된다.
STRICT_L10N = False

def validate(derived):
    errors, report = [], []
    l10n_missing = []   # en 누락 — 기본은 경고 집계, --strict면 에러 (8월 번역 전까지)

    # 병합 안전장치 (v1.8) — 전 시트 기본키 중복 검사.
    # 분할 파일이 겹치거나 개편 전 파일이 폴더에 남아 있으면 병합 때 행이 이중으로 들어온다 → 여기서 잡힘
    def check_dup(name, rows, keyfn):
        seen_k = set()
        for r in rows:
            k = keyfn(r)
            if k in seen_k: errors.append(f"[중복] {name}: {k} — 분할 파일 간 중복 행(개편 전 파일이 폴더에 남았는지 확인)")
            seen_k.add(k)
    check_dup("Cocktails", COCKTAILS, lambda r: r[0])
    check_dup("ShelfItems", SHELF_ITEMS, lambda r: r[0])
    check_dup("Characters", CHARACTERS, lambda r: r[0])
    check_dup("Scenes", SCENES, lambda r: r[0])
    check_dup("Steps", STEPS, lambda r: (r[0], r[1]))
    check_dup("Choices", CHOICES, lambda r: (r[0], r[1]))
    check_dup("RandomWaves", RANDOM_WAVES, lambda r: (r[0], r[1]))
    check_dup("RegularSlots", REGULAR_SLOTS, lambda r: (r[0], r[1]))
    # seq는 두 시트가 하루 공용 번호 — 같은 (day,seq)가 양쪽에 있으면 스폰 순서가 모호해진다
    check_dup("RandomWaves+RegularSlots(day,seq 공용)",
              [(r[0], r[1]) for r in RANDOM_WAVES] + [(r[0], r[1]) for r in REGULAR_SLOTS], lambda r: r)
    check_dup("Spots", SPOTS, lambda r: r[0])
    check_dup("InteractPoints", POINTS, lambda r: r[0])
    check_dup("Transitions", TRANSITIONS, lambda r: r[0])
    check_dup("Cutscenes", CUTSCENES, lambda r: r[0])
    check_dup("Quests", QUESTS, lambda r: r[0])
    check_dup("QuestStages", QUEST_STAGES, lambda r: (r[0], r[1]))
    check_dup("Endings", ENDINGS, lambda r: r[1])
    check_dup("Config", CONFIG, lambda r: r[0])
    check_dup("UIStrings", UI_STRINGS, lambda r: r[0])
    check_dup("TextTags", TEXT_TAGS, lambda r: r[0])
    check_dup("BarkSituations", BARK_SITUATIONS, lambda r: r[0])
    check_dup("GuestBodies", GUEST_BODIES, lambda r: r[1])
    check_dup("GuestBodyExclusions", GUEST_BODY_EXCLUSIONS, lambda r: tuple(sorted((r[0], r[1]))))
    check_dup("Expressions", EXPRESSIONS, lambda r: (r[0], r[1]))
    check_dup("ExpressionParts", EXPRESSION_PARTS, lambda r: (r[0], r[1], r[2]))
    check_dup("FieldAnims", FIELD_ANIMS, lambda r: (r[0], r[1]))
    check_dup("Tastes", TASTES, lambda r: (r[0], r[1]))
    check_dup("Tags", TAGS, lambda r: r[0])
    _tag_keys = {t[0] for t in TAGS}
    if set(TAG_IDS) != _tag_keys:
        errors.append(f"[Tags] tag_id 매핑 대상 불일치 — 누락 {sorted(_tag_keys - set(TAG_IDS))}, 초과 {sorted(set(TAG_IDS) - _tag_keys)}")
    _tag_id_values = list(TAG_IDS.values())
    if len(_tag_id_values) != len(set(_tag_id_values)):
        errors.append("[Tags] tag_id 중복 — 언어 비종속 식별자는 전역 고유해야 한다")
    for _tag_ko, _tag_id in TAG_IDS.items():
        if not re.fullmatch(r"[a-z][a-z0-9_]*", str(_tag_id or "")):
            errors.append(f"[Tags] {_tag_ko}: tag_id '{_tag_id}' 형식 오류 — snake_case 영문 소문자만 허용")
    check_dup("Dossier", DOSSIER, lambda r: (r[0], r[2], r[1], r[3]))

    # 데모 일차 계약 — Day 0·1·2·3 네 일차를 보존한다.
    _demo_days = {d[0] for d in DAYS}
    for _day in sorted({0, 1, 2, 3} - _demo_days):
        errors.append(f"[Days] 데모 필수 Day {_day} 행 없음 — 데모는 Day 0~3 총 4일")
    _scene_day = {s[0]: s[1] for s in SCENES}
    # 삼호 사망·생존 결과는 거리 대본이 아니라 별도 연출 컷신으로 제작한다.
    for _scene_id in ("d3_chris_witness", "d3_home_talk", "d3_dream"):
        if _scene_day.get(_scene_id) != 3:
            errors.append(f"[Scenes] {_scene_id}는 Day 3 결과 장면이어야 함")

    # 제조 데이터 계약 검증 — D-01/D-02/D-04/D-09/D-12/D-14.
    _cfg = {k: v for k, v, _ in CONFIG}
    _required_cfg = {
        "data_schema_version", "craft_score_formula", "score_aggregation_mode",
        "normalize_present_gimmick_weights", "fill_up_weight_source", "craft_timer_scope",
        "squeeze_demo_behavior", "powder_demo_behavior", "ice_demo_behavior",
        "shake_target_stacks", "open_approach_sec", "open_judge_window_px",
        "open_start_radius_px", "open_target_radius_px", "open_input_enabled_after_sec",
        "open_transition_delay_sec", "pour_start_angle_deg", "pour_max_tilt_angle_deg",
        "pour_tilt_speed_deg_per_sec", "pour_emit_rate_ml_per_sec",
        "pour_quantity_update_interval_sec", "shake_bpm", "shake_judge_radius_units",
        "shake_node_lifetime_sec", "shake_pattern_node_min_count",
        "shake_pattern_node_max_count", "stir_target_stacks", "stir_inputs_per_circle",
        "stir_circle_limit_sec", "stir_warning_ratio", "stir_input_cooldown_sec",
        "random_order_sampling_mode", "serve_timeout_same_frame_priority",
        "data_error_safe_exit_after_same_code", "save_checkpoint_policy",
        "manual_save_allowed_phase", "manual_save_slot_count", "autosave_slot_count",
        "autosave_on_commute_in_enter", "autosave_on_bar_enter",
        "autosave_on_part2_start", "autosave_on_daily_sales_settlement_complete",
        "commute_in_resume_spot_id", "commute_out_resume_spot_id",
        "bar_mid_session_autosave_enabled",
    }
    for _key in sorted(_required_cfg - set(_cfg)):
        errors.append(f"[Config] 필수 키 '{_key}' 없음 — 제조·운영 데이터 계약 2.5.0")
    if _cfg.get("data_schema_version") != "2.7.0":
        errors.append("[Config] data_schema_version은 2.7.0이어야 한다")
    if "weight_fill_up" in _cfg:
        errors.append("[Config] weight_fill_up은 사용하지 않음 — Fill-up은 weight_pour를 공유한다")
    if _cfg.get("craft_score_formula") != "weighted_representative_v1":
        errors.append("[Config] craft_score_formula는 weighted_representative_v1이어야 한다")
    if _cfg.get("score_aggregation_mode") != "mean_per_action_then_weighted":
        errors.append("[Config] score_aggregation_mode는 mean_per_action_then_weighted여야 한다")
    if _cfg.get("normalize_present_gimmick_weights") is not True:
        errors.append("[Config] normalize_present_gimmick_weights는 TRUE여야 한다")
    if _cfg.get("fill_up_weight_source") != "weight_pour":
        errors.append("[Config] fill_up_weight_source는 weight_pour여야 한다")
    for _key in ("squeeze_demo_behavior", "powder_demo_behavior", "ice_demo_behavior"):
        if _key in _cfg and _cfg[_key] != "auto_apply_unscored":
            errors.append(f"[Config] {_key}={_cfg[_key]!r} — 데모는 auto_apply_unscored만 허용")
    if _cfg.get("craft_timer_scope") != "manual_input_active_only":
        errors.append("[Config] craft_timer_scope는 manual_input_active_only이어야 한다")
    if _cfg.get("random_order_sampling_mode") != "uniform_with_replacement":
        errors.append("[Config] random_order_sampling_mode는 uniform_with_replacement여야 한다")
    if _cfg.get("serve_timeout_same_frame_priority") != "timeout":
        errors.append("[Config] serve_timeout_same_frame_priority는 timeout이어야 한다")
    if "autosave_interval_step" in _cfg:
        errors.append("[Config] autosave_interval_step은 사용하지 않음 — fixed_transitions_v1 네 전환점만 자동 저장")
    if _cfg.get("save_checkpoint_policy") != "fixed_transitions_v1":
        errors.append("[Config] save_checkpoint_policy는 fixed_transitions_v1이어야 한다")
    if _cfg.get("manual_save_allowed_phase") != "home":
        errors.append("[Config] manual_save_allowed_phase는 home이어야 한다")
    if _cfg.get("manual_save_slot_count") != 5 or isinstance(_cfg.get("manual_save_slot_count"), bool):
        errors.append("[Config] manual_save_slot_count는 정수 5여야 한다")
    if _cfg.get("autosave_slot_count") != 1 or isinstance(_cfg.get("autosave_slot_count"), bool):
        errors.append("[Config] autosave_slot_count는 정수 1이어야 한다")
    for _key in (
        "autosave_on_commute_in_enter", "autosave_on_bar_enter",
        "autosave_on_part2_start", "autosave_on_daily_sales_settlement_complete",
    ):
        if _cfg.get(_key) is not True:
            errors.append(f"[Config] {_key}는 TRUE여야 한다")
    if _cfg.get("bar_mid_session_autosave_enabled") is not False:
        errors.append("[Config] bar_mid_session_autosave_enabled는 FALSE여야 한다")
    _spot_ids = {s[0] for s in SPOTS}
    for _key, _expected in (
        ("commute_in_resume_spot_id", "home_door"),
        ("commute_out_resume_spot_id", "bar_door"),
    ):
        _spot_id = _cfg.get(_key)
        if _spot_id != _expected:
            errors.append(f"[Config] {_key}는 {_expected}여야 한다")
        elif _spot_id not in _spot_ids:
            errors.append(f"[Config] {_key}가 존재하지 않는 Spots.id '{_spot_id}'를 참조한다")
    _safe_exit_after = _cfg.get("data_error_safe_exit_after_same_code")
    if not isinstance(_safe_exit_after, int) or isinstance(_safe_exit_after, bool) or _safe_exit_after < 2:
        errors.append("[Config] data_error_safe_exit_after_same_code는 2 이상의 정수여야 한다")
    _positive_cfg = (
        "shake_target_stacks", "open_approach_sec", "open_judge_window_px",
        "open_start_radius_px", "open_target_radius_px", "pour_max_tilt_angle_deg",
        "pour_tilt_speed_deg_per_sec", "pour_emit_rate_ml_per_sec",
        "pour_quantity_update_interval_sec", "shake_bpm", "shake_judge_radius_units",
        "shake_node_lifetime_sec", "shake_pattern_node_min_count",
        "shake_pattern_node_max_count", "stir_target_stacks", "stir_inputs_per_circle",
        "stir_circle_limit_sec",
    )
    for _key in _positive_cfg:
        if _key in _cfg and (not isinstance(_cfg[_key], (int, float)) or isinstance(_cfg[_key], bool) or _cfg[_key] <= 0):
            errors.append(f"[Config] {_key}={_cfg[_key]!r} — 0보다 큰 숫자여야 한다")
    for _key in ("open_input_enabled_after_sec", "open_transition_delay_sec", "stir_input_cooldown_sec"):
        if _key in _cfg and (not isinstance(_cfg[_key], (int, float)) or isinstance(_cfg[_key], bool) or _cfg[_key] < 0):
            errors.append(f"[Config] {_key}={_cfg[_key]!r} — 0 이상의 숫자여야 한다")
    if "stir_warning_ratio" in _cfg and not (isinstance(_cfg["stir_warning_ratio"], (int, float))
                                                and not isinstance(_cfg["stir_warning_ratio"], bool)
                                                and 0 < _cfg["stir_warning_ratio"] < 1):
        errors.append(f"[Config] stir_warning_ratio={_cfg.get('stir_warning_ratio')!r} — 0 초과 1 미만이어야 한다")
    if all(k in _cfg for k in ("pour_start_angle_deg", "pour_max_tilt_angle_deg")):
        _start, _maximum = _cfg["pour_start_angle_deg"], _cfg["pour_max_tilt_angle_deg"]
        if not (isinstance(_start, (int, float)) and not isinstance(_start, bool)
                and isinstance(_maximum, (int, float)) and not isinstance(_maximum, bool)
                and 0 <= _start < _maximum <= 180):
            errors.append("[Config] Pour 각도는 숫자이며 0 <= 시작각 < 최대각 <= 180이어야 한다")
    if all(k in _cfg for k in ("shake_pattern_node_min_count", "shake_pattern_node_max_count")):
        _minimum, _maximum = _cfg["shake_pattern_node_min_count"], _cfg["shake_pattern_node_max_count"]
        if (isinstance(_minimum, (int, float)) and isinstance(_maximum, (int, float))
                and not isinstance(_minimum, bool) and not isinstance(_maximum, bool)
                and _minimum > _maximum):
            errors.append("[Config] Shake 패턴 노드 최소 개수가 최대 개수보다 큼")

    # v3.6 — 태그는 사전(Tags)에 있는 것만. 자유 문자열이면 오타가 취향 판정을 조용히 비껴간다
    import re as _re
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        # v3.7 — 제조법 문안은 사람이 쓰는 칸이라 비면 정보 화면이 빈칸으로 뜬다
        if not (d["recipe_desc_ko"] or "").strip():
            errors.append(f"[칵테일] {d['id']}: recipe_desc_ko가 비어 있음 — 제조법 설명은 손으로 적는 칸이다")
        elif not (d["recipe_desc_en"] or "").strip():
            l10n_missing.append(f"칵테일 {d['id']} 제조법 설명")
        for t in d["tags"].split(";"):
            if t not in TAG_EN:
                errors.append(f"[태그] 칵테일 {d['id']}: '{t}' — Tags 시트에 없음(오타? 신규면 사전에 먼저 추가)")
    for t_row in TASTES:
        for m in _re.findall(r"cocktail\.tag\(([^)]+)\)", t_row[2]):
            if m not in TAG_EN:
                errors.append(f"[태그] Tastes {t_row[0]}#{t_row[1]}: cocktail.tag({m}) — Tags 시트에 없음")

    ing_ids = shelf_ids("ingredient")
    glass_ids = shelf_ids("glass")
    garnish_ids = shelf_ids("garnish")
    tool_ids = shelf_ids("tool")
    item_ids = glass_ids | garnish_ids | tool_ids
    # kind 오배치 검사 — 한 테이블이 되면서 "잔 칸에 재료 id" 같은 실수가 가능해졌다 (v1.9)
    for r in shelf_rows():
        if r[1] not in ("ingredient", "glass", "tool", "garnish"):
            errors.append(f"[선반] {r[0]}: kind '{r[1]}' 은(는) 허용되지 않음 (ingredient/glass/tool/garnish)")
        d = dict(zip(SHELF_COLS, r))
        if d["kind"] == "ingredient":
            if d["default_action"] not in ("pour", "fill_up", "squeeze", "powder"):
                errors.append(f"[선반] {d['id']}: default_action '{d['default_action']}' 불가 또는 공란")
            if d["prep_action"] not in (None, "", "open"):
                errors.append(f"[선반] {d['id']}: prep_action '{d['prep_action']}' 불가 (공란/open)")
            if d["default_action"] in ("pour", "fill_up", "squeeze", "powder"):
                if not isinstance(d["default_target_qty"], (int, float)) or isinstance(d["default_target_qty"], bool) or d["default_target_qty"] <= 0:
                    errors.append(f"[선반] {d['id']}: 수량형 default_action인데 default_target_qty가 0보다 큰 숫자가 아님")
                if d["default_target_unit"] not in ("oz", "ml", "tsp"):
                    errors.append(f"[선반] {d['id']}: default_target_unit '{d['default_target_unit']}' 불가 (oz/ml/tsp)")
            if d["shelf_group"] not in (None, "", "liquor", "fridge"):
                errors.append(f"[선반] {d['id']}: shelf_group '{d['shelf_group']}' 불가 (liquor/fridge/공란)")
            if d["default_action"] in ("pour", "fill_up") and d["shelf_group"] not in ("liquor", "fridge"):
                errors.append(f"[선반] {d['id']}: 수동 재료인데 shelf_group이 공란 — liquor/fridge 중 하나 필요")
            if d["default_action"] in ("pour", "fill_up"):
                _color = d["color"]
                _valid_rgb = False
                if isinstance(_color, str):
                    try:
                        _rgb = [int(v.strip()) for v in _color.split(",")]
                        _valid_rgb = len(_rgb) == 3 and all(0 <= v <= 255 for v in _rgb)
                    except ValueError:
                        _valid_rgb = False
                if not _valid_rgb:
                    errors.append(f"[선반] {d['id']}: 수동 액체 재료 color는 0~255의 R,G,B 형식이어야 함")
                _alpha = d["liquid_alpha"]
                if (not isinstance(_alpha, (int, float)) or isinstance(_alpha, bool)
                        or not 0.0 <= _alpha <= 1.0):
                    errors.append(f"[선반] {d['id']}: liquid_alpha는 0.0~1.0 숫자여야 함")
            elif d["liquid_alpha"] not in (None, ""):
                errors.append(f"[선반] {d['id']}: default_action={d['default_action']}에는 liquid_alpha를 지정할 수 없음")
        elif d["shelf_group"] not in (None, ""):
            errors.append(f"[선반] {d['id']}: kind={d['kind']}에는 shelf_group을 지정할 수 없음")
        elif d["liquid_alpha"] not in (None, ""):
            errors.append(f"[선반] {d['id']}: kind={d['kind']}에는 liquid_alpha를 지정할 수 없음")
    if "tonic_water" in ing_ids:
        errors.append("[선반] tonic_water는 폐기된 ID — 탄산수 정본 soda_water로 통합해야 한다")
    if "soda_water" not in ing_ids:
        errors.append("[선반] Day 0 탄산수 정본 soda_water가 없음")
    _day0_confirmed = {c[0] for c in COCKTAILS
                       if dict(zip(CK_COLS, c))["status"] == "confirmed"
                       and dict(zip(CK_COLS, c))["unlock_day"] == 0}
    if _day0_confirmed != {"gin_tonic", "gin_fizz"}:
        errors.append(f"[Day 0] confirmed 해금 칵테일은 gin_tonic·gin_fizz만 허용 — 현재 {sorted(_day0_confirmed)}")
    _day0_manual_ids = {line["ingredient"] for cid in _day0_confirmed
                        for line in recipe_lines(cid) if not line["auto_apply"]}
    if _day0_manual_ids != {"gin", "soda_water"}:
        errors.append(f"[Day 0] 수동 재료는 gin·soda_water만 허용 — 현재 {sorted(_day0_manual_ids)}")
    char_ids = {c[0] for c in CHARACTERS}
    for _field in FIELD_ANIMS:
        if _field[0] not in char_ids:
            errors.append(f"[FieldAnims] character_id '{_field[0]}' — Characters에 없는 참조")
    scene_ids = {s[0] for s in SCENES}
    choice_ids = {c[0] for c in CHOICES}
    cocktail_ids = {c[0] for c in COCKTAILS}
    pers_ids = {p[0] for p in PERSONALITIES}
    voice_ids = pers_ids | char_ids
    # v3.2 — Barks.voice_id는 이름만 보고 성격/캐릭터를 구분한다. 두 시트의 id가 겹치면
    # 그 이름의 대사가 어느 쪽 풀로 가는지 정할 수 없으므로 겹침 자체를 금지.
    _vid_clash = pers_ids & char_ids
    if _vid_clash:
        errors.append(f"[id 충돌] Personalities와 Characters에 같은 id가 있음: {sorted(_vid_clash)} — Barks.voice_id가 성격 대사인지 캐릭터 전용 대사인지 구분할 수 없게 된다")

    # v3.5 — think_chance는 확률이라 0~1 밖이면 엔진이 해석할 수 없다
    for p in PERSONALITIES:
        d = dict(zip(PERS_COLS, p))
        if not (isinstance(d["think_chance"], (int, float)) and 0 <= d["think_chance"] <= 1):
            errors.append(f"[성격] {d['id']}: think_chance {d['think_chance']!r} — 0~1 사이 확률이어야 한다")

    # 표정 시스템 검증
    scene_phase = {s[0]: dict(zip(SCENE_COLS, s))["phase"] for s in SCENES}   # 표정/동작 검증용
    anims_by_char = {}
    for f in FIELD_ANIMS:
        anims_by_char.setdefault(f[0], set()).add(f[1])
    expr_by_char = {}
    for e in EXPRESSIONS:
        d = dict(zip(EXPR_COLS, e))
        if d["character_id"] != "common" and d["character_id"] not in char_ids:
            errors.append(f"[표정] 캐릭터 {d['character_id']} 없음")
        if d["mode"] not in ("sprite", "parts_anim"): errors.append(f"[표정] {d['character_id']}.{d['expression']}: mode {d['mode']} 불가")
        if d["mode"] == "sprite" and not d["sprite_key"]: errors.append(f"[표정] {d['character_id']}.{d['expression']}: sprite 모드인데 sprite_key 없음")
        expr_by_char.setdefault(d["character_id"], {})[d["expression"]] = d["mode"]
    LOOP_MODES = {"always", "always_on_dialogue", "once", "none", "on_dialogue"}
    for p in EXPRESSION_PARTS:
        d = dict(zip(EXPRPART_COLS, p))
        if expr_by_char.get(d["character_id"], {}).get(d["expression"]) != "parts_anim":
            errors.append(f"[표정파츠] {d['character_id']}.{d['expression']}: Expressions에 parts_anim으로 등록되지 않음")
        if d["part"] not in ("eyes","eyebrows","upper_face","lower_face","body","extra","etc"):
            errors.append(f"[표정파츠] {d['character_id']}.{d['expression']}: 파츠 {d['part']} 불가")
        if d["loop"] not in LOOP_MODES: errors.append(f"[표정파츠] {d['character_id']}.{d['expression']}.{d['part']}: loop {d['loop']} 불가")
    cut_ids = {c[0]: c[1] for c in CUTSCENES}
    for c in CUTSCENES:  # v3.0 — kind enum + sprite(포스터 뷰)는 이미지 키 필수
        if c[1] not in ("timeline", "gif", "sprite"):
            errors.append(f"[컷씬] {c[0]}: kind '{c[1]}' 불가 (timeline/gif/sprite)")
        if c[1] == "sprite" and not c[2]:
            errors.append(f"[컷씬] {c[0]}: sprite 컷씬은 resource_key(포스터 이미지) 필수")

    # 레시피 라인 검증 (제조 개편 — 플래그 체계)
    ck_ids_set = {c[0] for c in COCKTAILS}
    for cid in RECIPES:
        if cid not in ck_ids_set: errors.append(f"[레시피] {cid}: Cocktails에 없는 id")
    for cid in ck_ids_set:
        if cid not in RECIPES: errors.append(f"[레시피] {cid}: RecipeLines 0줄 — 제조 불가")
    VALID_ACTIONS = {"open", "pour", "squeeze", "powder", "shake", "stir", "fill_up"}
    for cid, lines in RECIPES.items():
        st = next((dict(zip(CK_COLS, c))["status"] for c in COCKTAILS if c[0] == cid), "tbd")
        for l in recipe_lines(cid):
            if l["action"] not in VALID_ACTIONS:
                errors.append(f"[레시피] {cid}: action '{l['action']}' 불가")
            if l["ingredient"] not in ing_ids:
                errors.append(f"[레시피] {cid}: 재료 {l['ingredient']} 없음")
            if l["auto_apply"] and l["action"] not in ("powder", "squeeze"):
                errors.append(f"[레시피] {cid}: auto 플래그는 데모 자동 처리인 powder/squeeze에만 허용")
            if l["action"] == "squeeze" and (not l["auto_apply"] or l["scored"]):
                errors.append(f"[레시피] {cid}: 데모 squeeze는 auto_apply=TRUE, scored=FALSE여야 한다")
            if l["action"] == "powder" and (not l["auto_apply"] or l["scored"]):
                errors.append(f"[레시피] {cid}: 데모 powder는 auto_apply=TRUE, scored=FALSE여야 한다")
            if l["unit"] not in (None, "", "oz", "ml", "tsp"):
                errors.append(f"[레시피] {cid}: 단위 '{l['unit']}' 불가 (oz/ml/tsp)")
            # 수량 확정 규칙: status=confirmed면 전 라인 수량 필수(fill_up 포함 — pour와 동일하게 수량 오차 채점).
            # tbd(신규 13종)는 수량 공란 허용
            if st == "confirmed" and l["scored"] and (l["qty"] is None or not l["unit"]):
                errors.append(f"[레시피] {cid}: status=confirmed인데 {l['action']} {l['ingredient']} 수량/단위 공란")
    VALID_ICE = ("none", "cubed", "crushed")
    _prep_by_ing = {d["id"]: d["prep_action"] for d in shelf_dicts() if d["kind"] == "ingredient"}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        if d["status"] not in ("confirmed", "tbd"):
            errors.append(f"[칵테일] {d['id']}: status '{d['status']}' 불가 (confirmed/tbd)")
        if d["mix"] not in ("none", "build", "stir", "shake"):
            errors.append(f"[칵테일] {d['id']}: mix '{d['mix']}' 불가 — bottle_open은 폐기(prep=cap으로)")
        if d["prep"] not in ("", "cap"):
            errors.append(f"[칵테일] {d['id']}: prep '{d['prep']}' 불가 (공란/cap — cork는 와인 오프너와 함께 데모 제외)")
        _recipe_requires_open = any(_prep_by_ing.get(line["ingredient"]) == "open" for line in recipe_lines(d["id"]))
        if (d["prep"] == "cap") != _recipe_requires_open:
            errors.append(f"[칵테일] {d['id']}: prep과 RecipeLines→ShelfItems.prep_action 불일치 — 병 개봉 정답을 두 경로가 다르게 말함")
        if d["mixing_ice"] not in VALID_ICE or d["serving_ice"] not in VALID_ICE:
            errors.append(f"[칵테일] {d['id']}: 얼음 '{d['mixing_ice']}/{d['serving_ice']}' 불가 (none/cubed/crushed)")
        # kind까지 대조 — 통합 테이블이라 "잔 칸에 가니시 id"가 문법상 가능해졌다 (v1.9)
        if d["glass"] and d["glass"] not in glass_ids:
            errors.append(f"[칵테일] {d['id']}: 잔 {d['glass']} 없음" if d["glass"] not in shelf_ids()
                          else f"[칵테일] {d['id']}: 잔 칸에 kind=glass가 아닌 {d['glass']} 지정됨")
        if d["garnish"] and d["garnish"] not in garnish_ids:
            errors.append(f"[칵테일] {d['id']}: 가니시 {d['garnish']} 없음" if d["garnish"] not in shelf_ids()
                          else f"[칵테일] {d['id']}: 가니시 칸에 kind=garnish가 아닌 {d['garnish']} 지정됨")
        if not d["recipe_desc_ko"]:
            errors.append(f"[칵테일] {d['id']}: recipe_desc_ko 공란 — 레시피 UI에 표시할 문안 필수")

    # 씬 트리거/그룹 검증 (v1.2)
    VALID_TRIGGERS = {"auto", "interact", "cameo", "manual"}
    group_seqs = {}
    for s in SCENES:
        d = dict(zip(SCENE_COLS, s))
        t = d["trigger"]
        if t not in VALID_TRIGGERS and not t.startswith("pass:") and not t.startswith("event:"):
            errors.append(f"[씬] {d['id']}: trigger {t} 불가")
        if t.startswith("pass:") and t[5:] not in {sp[0] for sp in SPOTS}:
            errors.append(f"[씬] {d['id']}: pass 위치 {t[5:]} 없음")
        if d["group"]:
            key = (d["group"], d["seq"])
            if key in group_seqs: errors.append(f"[씬] 그룹 {d['group']} seq {d['seq']} 중복")
            group_seqs[key] = d["id"]

    # 스텝 참조 + 고정 대사 ID + 루나 대사 규칙(플레이어 행동 직전 루나 say 금지)
    by_scene = {}
    for s in STEPS:
        by_scene.setdefault(s[0], []).append(dict(zip(STEP_COLS, s)))
    dialogue_ids = set()
    for sid, steps in by_scene.items():
        if sid not in scene_ids: errors.append(f"[스텝] 씬 {sid} 없음"); continue
        steps.sort(key=lambda x: x["seq"])
        prev = None
        serve_result_available = False
        serve_result_when_keys = {
            "grade", "craft_grade", "final_grade", "order_match",
            "cocktail.id", "ordered_cocktail.id", "served_cocktail.id",
        }
        for st in steps:
            when_tokens = set(re.findall(r"[a-z_]+(?:\.[a-z_]+)*", str(st["when"] or "")))
            used_result_keys = sorted(when_tokens & serve_result_when_keys)
            if used_result_keys and not serve_result_available:
                errors.append(f"[서빙문맥] {sid}#{st['seq']}: ServeResult 생성 전 결과 키 참조 {used_result_keys}")
            has_dialogue_text = bool(st["text_ko"] or st["text_en"])
            needs_dialogue_id = st["type"] == "say" or (st["type"] == "order" and has_dialogue_text)
            dialogue_id = str(st["dialogue_id"] or "").strip()
            if needs_dialogue_id and not dialogue_id:
                errors.append(f"[대사ID] {sid}#{st['seq']}: 화면에 표시되는 {st['type']} 스텝에 dialogue_id 없음")
            if dialogue_id:
                if not re.fullmatch(r"dlg_[a-z0-9_]+", dialogue_id):
                    errors.append(f"[대사ID] {sid}#{st['seq']}: '{dialogue_id}' 형식 불가 — dlg_ 접두사와 영문 소문자·숫자·밑줄만 사용")
                elif dialogue_id in dialogue_ids:
                    errors.append(f"[대사ID] {sid}#{st['seq']}: '{dialogue_id}' 전역 중복")
                else:
                    dialogue_ids.add(dialogue_id)
            if st["actor"] and st["type"] in ("say","enter","exit","order","serve","move","expr","anim","emote") and st["actor"] not in char_ids:
                errors.append(f"[스텝] {sid}#{st['seq']}: 캐릭터 {st['actor']} 없음")
            if st["type"] == "choice" and st["arg"] not in choice_ids:
                errors.append(f"[스텝] {sid}#{st['seq']}: 선택지 {st['arg']} 없음")
            if st["type"] == "order" and st["arg"].startswith("exact:") and st["arg"][6:] not in cocktail_ids:
                errors.append(f"[스텝] {sid}#{st['seq']}: 칵테일 {st['arg'][6:]} 없음")
            if st["type"] == "craft" and st["arg"].startswith("tutorial:") and st["arg"][9:] not in cocktail_ids:
                errors.append(f"[스텝] {sid}#{st['seq']}: 칵테일 {st['arg'][9:]} 없음")
            if st["type"] == "say" and st["text_ko"] and not st["text_en"]:
                l10n_missing.append(f"{sid}#{st['seq']} 대사")
            # 표정/동작 참조 (v2.1) — arg의 의미가 장소(phase)에 따라 갈린다:
            #   바 계열(bar/bar_open/home/dream/intro) = 흉상 표정(Expressions)
            #   거리 계열(street/commute_*)            = SD 동작(FieldAnims.action)  ※ 거리엔 흉상이 없다
            if st["type"] in ("say", "expr") and st["actor"] and st["arg"]:
                if scene_phase.get(sid) in ("street", "commute_in", "commute_out"):
                    acts = anims_by_char.get(st["actor"], set())
                    if st["arg"] not in acts:
                        errors.append(f"[동작] {sid}#{st['seq']}: {st['actor']}에 동작 '{st['arg']}' 없음 "
                                      f"— 거리 씬의 arg는 표정이 아니라 FieldAnims의 action")
                else:
                    mine = expr_by_char.get(st["actor"], {})
                    if mine and st["arg"] not in mine and st["arg"] not in expr_by_char.get("common", {}):
                        errors.append(f"[표정] {sid}#{st['seq']}: {st['actor']}에 표정 '{st['arg']}' 없음(common에도 없음)")
            # 컷씬 리소스 참조 — timeline 스텝은 kind timeline(연출)과 sprite(포스터 뷰: 이미지 1장+스텝 text가 하단 대사) 둘 다 부른다
            if st["type"] in ("timeline", "gif"):
                if st["arg"] not in cut_ids: errors.append(f"[컷씬] {sid}#{st['seq']}: Cutscenes에 {st['arg']} 없음")
                else:
                    allowed_kinds = ("timeline", "sprite") if st["type"] == "timeline" else ("gif",)
                    if cut_ids[st["arg"]] not in allowed_kinds:
                        errors.append(f"[컷씬] {sid}#{st['seq']}: {st['arg']}의 kind({cut_ids[st['arg']]})는 {st['type']} 스텝에서 호출 불가")
            if st["sync"] not in ("", "wait", "no_wait"): errors.append(f"[스텝] {sid}#{st['seq']}: sync {st['sync']} 불가")   # 공란 = wait 기본값, 명기도 허용
            # 바 계열 한정 — 거리 선택지는 플레이어 말풍선 근처 버튼(§7.1)이라 루나 대사 직전 choice가 정상 패턴
            if (st["type"] in PLAYER_ACTION_STEPS and prev and prev["type"] == "say" and prev["actor"] == "luna"
                    and scene_phase.get(sid) in ("bar", "bar_open")):
                errors.append(f"[루나규칙] {sid}#{st['seq']}: 플레이어 행동({st['type']}) 직전에 루나 대사 — 흐름 끊김")
            # order의 when은 직전 ServeResult로 평가할 수 있지만, order를 수락한 직후에는
            # 그 문맥을 폐기한다. 새 serve가 성공해야 다음 결과 분기에서 다시 사용할 수 있다.
            if st["type"] == "order":
                serve_result_available = False
            elif st["type"] == "serve":
                serve_result_available = True
            prev = st
        # 씬 마지막 스텝이 no_wait면 연출이 잘릴 수 있음
        if steps and steps[-1]["sync"] == "no_wait":
            errors.append(f"[스텝] {sid}: 마지막 스텝이 no_wait — 연출 완료 전에 씬이 끝남")

    spot_ids = {s[0] for s in SPOTS}
    scene_groups = {dict(zip(SCENE_COLS, s))["group"] for s in SCENES if dict(zip(SCENE_COLS, s))["group"]}
    # 비주얼 이원화 — field 도메인 씬(거리·집·꿈)에서 몸을 쓰는 스텝(enter/move/anim)의
    # 캐릭터가 FieldAnims에 없으면 경고 (연출 방식 미확정일 수 있어 에러는 아님)
    FIELD_PHASES = {"commute_in", "commute_out", "home", "dream", "street"}
    field_chars = {f[0] for f in FIELD_ANIMS if f[3] == "OK"}
    field_planned = {f[0] for f in FIELD_ANIMS}
    scene_phase = {dict(zip(SCENE_COLS, s))["id"]: dict(zip(SCENE_COLS, s))["phase"] for s in SCENES}
    for st in STEPS:
        d = dict(zip(STEP_COLS, st))
        if scene_phase.get(d["scene_id"]) in FIELD_PHASES and d["type"] in ("enter", "move", "anim") and d["actor"]:
            if d["actor"] not in field_planned:
                report.append(f"⚠ [SD스프라이트] {d['scene_id']}#{d['seq']}: {d['actor']}의 필드(SD) 스프라이트 미등록 — FieldAnims에 행 추가 필요")
            elif d["actor"] not in field_chars:
                report.append(f"⚠ [SD스프라이트] {d['scene_id']}#{d['seq']}: {d['actor']} 필드 스프라이트 '신규필요' 상태")

    transition_ids = {r[0] for r in TRANSITIONS}
    anims_by_char = {}
    for f in FIELD_ANIMS:
        anims_by_char.setdefault(f[0], set()).add(f[1])

    for tr in TRANSITIONS:
        d = dict(zip(TRANSITION_COLS, tr))
        if not d["target_location"] or not d["target_phase"]:
            errors.append(f"[전환] {d['id']}: target_location과 target_phase는 필수")
        if d["effect"] not in ("fade", "none"):
            errors.append(f"[전환] {d['id']}: effect '{d['effect']}' 불가 (fade/none)")
        # target_spot은 목적 시스템(집 내부 등)의 스팟이라 거리 spots FK를 강제하지 않는다(문서: null이면 목적 시스템이 결정)

    # v2.7.0 통합 지점 검증 — 문서 「데이터 구조 작성」 interact_points 검증 + 공용 필드 실행 계약
    _placement = {}   # (source_id, spot_id) → 같은 대상의 중복 배치 행은 facing·phase·spawn_when이 같아야 함
    for p in POINTS:
        d = dict(zip(POINT_COLS, p))
        at = d["action_type"] or None
        if d["kind"] not in ("actor", "object"):
            errors.append(f"[포인트] {d['id']}: kind '{d['kind']}' 불가 (actor/object — 운영 거리에서 gimmick 미사용)")
            continue
        if d["spot_id"] not in spot_ids:
            errors.append(f"[포인트] {d['id']}: 위치 프리셋 {d['spot_id']} 없음")
        if d["phase"] not in ("commute_in", "commute_out", "both"):
            errors.append(f"[포인트] {d['id']}: phase '{d['phase']}' 불가 (commute_in/commute_out/both)")
        if d["kind"] == "actor":
            if d["source_id"] not in char_ids:
                errors.append(f"[포인트] {d['id']}: actor source_id '{d['source_id']}' — Characters에 없음")
            if d["facing"] not in ("left", "right"):
                errors.append(f"[포인트] {d['id']}: actor는 facing(left/right) 필수")
            if "idle" not in anims_by_char.get(d["source_id"], set()):
                errors.append(f"[포인트] {d['id']}: {d['source_id']}에 FieldAnims.action 'idle' 없음 — 거리 기본 동작")
            if d["activation_mode"] != "interact":
                errors.append(f"[포인트] {d['id']}: NPC 대화는 플레이어 E 상호작용으로만 시작")
        elif d["facing"]:
            errors.append(f"[포인트] {d['id']}: object에는 facing을 적지 않음")
        # p_qa_* 행은 day 99 전용이라 운영 행과 동시 스폰될 수 없어 배치 일관성 비교에서 제외한다
        _key = (d["source_id"], d["spot_id"])
        if d["id"].startswith("p_qa_"):
            _key = None
        _prev = _placement.setdefault(_key, d) if _key else d
        if (_prev["facing"], _prev["phase"], _prev["spawn_when"]) != (d["facing"], d["phase"], d["spawn_when"]):
            errors.append(f"[포인트] {d['id']}: 같은 source_id+spot_id({_key[0]}·{_key[1]}) 행은 facing·phase·spawn_when이 같아야 함")
        if d["activation_mode"] not in ("interact", "proximity"):
            errors.append(f"[포인트] {d['id']}: activation_mode '{d['activation_mode']}' 불가")
        if not isinstance(d["priority"], int) or isinstance(d["priority"], bool):
            errors.append(f"[포인트] {d['id']}: priority는 정수여야 함")
        if at not in ("scene", "scene_group", "transition", None):
            errors.append(f"[포인트] {d['id']}: action_type '{at}' 불가 (scene/scene_group/transition/공란=순수 배치)")
        if at == "scene" and d["action_ref"] not in scene_ids:
            errors.append(f"[포인트] {d['id']}: 씬 {d['action_ref']} 없음")
        elif at == "scene_group" and d["action_ref"] not in scene_groups:
            errors.append(f"[포인트] {d['id']}: 씬 그룹 {d['action_ref']} 없음")
        elif at == "transition" and d["action_ref"] not in transition_ids:
            errors.append(f"[포인트] {d['id']}: 전환 {d['action_ref']} 없음")
        if at is None and d["action_ref"]:
            errors.append(f"[포인트] {d['id']}: action_type 없이 action_ref만 있음")
        if at == "transition" and d["activation_mode"] != "interact":
            errors.append(f"[포인트] {d['id']}: 장소 전환은 문·오브젝트에서 직접 상호작용할 때만 가능")
        if d["activation_mode"] == "proximity":
            if d["kind"] != "object" or at != "scene":
                errors.append(f"[포인트] {d['id']}: proximity는 비캐릭터 자동 방송의 단일 scene만 허용")
            _osc = {d["action_ref"]} if at == "scene" else set()
            for _st in STEPS:
                if _st[0] not in _osc:
                    continue
                if _st[2] == "choice":
                    errors.append(f"[포인트] {d['id']}: 자동 방송 씬 {_st[0]}#{_st[1]}에 choice 금지")
                if _st[2] == "say" and _st[3] not in ("", "radio"):
                    errors.append(f"[포인트] {d['id']}: 자동 방송 씬 actor={_st[3]} — NPC 자동 발화 금지")
        if d["kind"] == "object" and at in ("scene", "scene_group"):
            _osc = ({d["action_ref"]} if at == "scene" else
                    {s[0] for s in SCENES if dict(zip(SCENE_COLS, s))["group"] == d["action_ref"]})
            for _st in STEPS:
                if _st[0] in _osc and _st[2] == "say" and _st[3] == "luna":
                    errors.append(f"[포인트] {d['id']}: 오브젝트 씬 {_st[0]}#{_st[1]}에 루나 독백 금지")

    for sc in SCENES:
        d = dict(zip(SCENE_COLS, sc))
        if d["phase"] in _STREET_PHASES:
            if d["start_mode"] not in ("referenced", "manual"):
                errors.append(f"[거리씬] {d['id']}: start_mode는 referenced/manual이어야 함")
            if d["on_complete_effects"] and not re.fullmatch(
                    r"flag\.[a-z0-9_]+\s*=\s*(?:true|false)(?:\s*;\s*flag\.[a-z0-9_]+\s*=\s*(?:true|false))*",
                    str(d["on_complete_effects"]).strip()):
                errors.append(f"[거리씬] {d['id']}: on_complete_effects는 세미콜론으로 구분한 flag.* = true/false만 허용")
            for _st in STEPS:
                if _st[0] == d["id"] and _st[2] in ("spawn", "move", "despawn"):
                    errors.append(f"[거리씬] {d['id']}#{_st[1]}: { _st[2] } 스텝은 거리 런타임 범위에서 지원하지 않음")
    # v3.1 — 씬 seq 계약: auto 씬의 (day,phase,seq)는 재생 순서라 중복 금지.
    # 단, when 분기(생사 루트처럼 조건으로 하나만 재생)는 같은 자리를 공유하므로 "빈 when끼리의 중복"만 에러.
    _auto_slots = {}
    for sc in SCENES:
        d = dict(zip(SCENE_COLS, sc))
        if d["trigger"] != "auto": continue   # interact·manual·cameo의 seq는 그룹 정렬용 — 중복 허용
        key = (d["day"], d["phase"], d["seq"])
        _auto_slots.setdefault(key, []).append((d["id"], d["when"]))
    for key, lst in _auto_slots.items():
        empty = [i for i, w in lst if not w]
        if len(lst) > 1 and empty:
            errors.append(f"[씬] day{key[0]} {key[1]} seq{key[2]}: auto 씬이 같은 자리를 공유하는데 {empty}의 when이 비어 있음 — 분기와 무관하게 항상 재생돼 겹친다. 전부 when으로 가르거나 seq를 나눌 것")

    # 2부 스탠딩 좌석 시뮬레이션 — 카메라가 최대 1280×720까지만 확장되므로 L+R 양 끝 동시 배치는
    # 한 화면에 안 잡힌다(빌드 차단, 엔진 보정 없음). 동시 재석도 최대 2명.
    for sc in SCENES:
        sd = dict(zip(SCENE_COLS, sc))
        if sd["phase"] not in ("bar", "bar_open"):
            continue
        seats = {}
        for st in sorted([x for x in STEPS if x[0] == sc[0]], key=lambda x: x[1]):
            d = dict(zip(STEP_COLS, st))
            if d["type"] == "enter" and d["arg"] in ("L", "M", "R"):
                if d["arg"] in seats.values():
                    errors.append(f"[좌석] {sc[0]}#{d['seq']}: {d['arg']} 좌석 중복 점유 — 먼저 앉은 인물이 exit하지 않음")
                seats[d["actor"]] = d["arg"]
                if len(seats) > 2:
                    errors.append(f"[좌석] {sc[0]}#{d['seq']}: 동시 재석 {len(seats)}명({', '.join(seats)}) — 2부 스탠딩은 최대 2명")
                elif sorted(seats.values()) == ["L", "R"]:
                    errors.append(f"[좌석] {sc[0]}#{d['seq']}: L+R 양 끝 동시 배치 — 카메라(최대 1280×720)에 함께 안 잡힘. 인접 좌석(L·M / M·R)으로 수정")
            elif d["type"] == "exit":
                seats.pop(d["actor"], None)
    for s in STEPS:  # move 스텝의 spot: 참조 검사
        if s[2] == "move" and str(s[4]).startswith("spot:") and s[4][5:] not in spot_ids:
            errors.append(f"[스텝] {s[0]}#{s[1]}: 위치 프리셋 {s[4][5:]} 없음")
    for g in RANDOM_WAVES:
        d = dict(zip(WAVE_COLS, g))
        # v3.3 — personality는 필수: 비어 있으면 외형 생성이 성격을 정할 수 없다
        if not d["personality"]: errors.append(f"[웨이브] day{d['day']}#{d['seq']}: personality는 필수 — 외형·대사 추첨의 기준값")
        elif d["personality"] not in pers_ids: errors.append(f"[웨이브] day{d['day']}#{d['seq']}: 성격 {d['personality']} 없음")
    # ── 랜덤 손님 외형 카탈로그 (v3.3) — 슬롯 9종 + is_default + 금지 조합 + 유효 조합 풀 ──
    _gb_pool = {}
    for r in GUEST_BODIES:
        d = dict(zip(GBODY_COLS, r))
        if d["part"] not in GB_SLOT_ORDER:
            errors.append(f"[외형] {d['id']}: part '{d['part']}' 불가 — 슬롯 목록({'/'.join(GB_SLOT_ORDER)})에 없음")
        if d["gender"] not in ("m", "f"):
            errors.append(f"[외형] {d['id']}: gender '{d['gender']}' 불가 (m/f)")
        if d["mode"] not in ("sprite", "parts_anim"):
            errors.append(f"[외형] {d['id']}: mode '{d['mode']}' 불가 (sprite/parts_anim)")
        if d["mode"] == "sprite" and not d["sprite"]:
            errors.append(f"[외형] {d['id']}: sprite 모드인데 sprite 키 없음")
        if not isinstance(d["weight"], int) or d["weight"] < 1:
            errors.append(f"[외형] {d['id']}: weight는 1 이상의 정수")
        # v2.9 감정 교체 맵 — 키는 공용 표정 세트(common) FK. 'default'는 기본 sprite 자체이므로 등록 금지
        emo, emo_bad = parse_emotions(d["emotions"])
        for piece in emo_bad:
            errors.append(f"[외형] {d['id']}: emotions 조각 '{piece}' 형식 오류 — '표정:스프라이트키; …'")
        for k, v in emo.items():
            if k == "default":
                errors.append(f"[외형] {d['id']}: emotions에 default 금지 — 기본 표정은 sprite 필드가 담당")
            elif k not in expr_by_char.get("common", {}):
                errors.append(f"[외형] {d['id']}: emotions 표정 '{k}' — common 세트에 없음")
            if not v:
                errors.append(f"[외형] {d['id']}: emotions '{k}'의 스프라이트 키가 비어 있음")
        allow = [x.strip() for x in str(d["personalities"] or "").split(";") if x.strip()]
        for a in allow:
            if a not in pers_ids:
                errors.append(f"[외형] {d['id']}: personalities '{a}' — Personalities에 없음(오타?)")
        for pid in (allow or pers_ids):
            _gb_pool.setdefault((d["gender"], d["part"], pid), 0)
            _gb_pool[(d["gender"], d["part"], pid)] += 1
    # ── 금지 조합(GuestBodyExclusions) — id 존재·성별 일치·슬롯 상이·중복(역순 포함) 금지 ──
    _gb_by = {dict(zip(GBODY_COLS, r))["id"]: dict(zip(GBODY_COLS, r)) for r in GUEST_BODIES}
    _excl, _excl_seen = set(), set()
    for e in GUEST_BODY_EXCLUSIONS:
        a, b = e[0], e[1]
        for x in (a, b):
            if x not in _gb_by: errors.append(f"[외형금지] {a}×{b}: id '{x}' — GuestBodies에 없음")
        if a in _gb_by and b in _gb_by:
            da, db = _gb_by[a], _gb_by[b]
            if da["gender"] != db["gender"]: errors.append(f"[외형금지] {a}×{b}: 성별이 달라 만날 수 없는 조합 — 행 삭제")
            if da["part"] == db["part"]: errors.append(f"[외형금지] {a}×{b}: 같은 슬롯({da['part']})끼리는 동시 적용이 없어 무의미 — 행 삭제")
        key = tuple(sorted((a, b)))
        if key in _excl_seen: errors.append(f"[외형금지] {a}×{b}: 중복 등록(역순 포함) — 같은 규칙은 한 행만")
        _excl_seen.add(key); _excl.add(key)
    # ── 기본 조합(is_default) — 성별 × 필수 슬롯마다 정확히 1개, 전 성격 공용, 금지 쌍 미포함 ──
    _gb_def = {}
    for d in _gb_by.values():
        if d.get("is_default"):
            _gb_def.setdefault((d["gender"], d["part"]), []).append(d["id"])
            if [x.strip() for x in str(d["personalities"] or "").split(";") if x.strip()]:
                errors.append(f"[외형기본] {d['id']}: is_default 파츠는 personalities를 비워 전 성격에서 쓸 수 있어야 함")
            if d["part"] in GB_OPTIONAL:
                errors.append(f"[외형기본] {d['id']}: 선택 슬롯({d['part']})은 기본 조합에서 항상 '없음' — is_default 마킹 제거")
    for g in ("m", "f"):
        for part in GB_REQUIRED:
            n = len(_gb_def.get((g, part), []))
            if n != 1:
                errors.append(f"[외형기본] 성별 '{g}' {part}: is_default 마킹 {n}개 — 정확히 1개여야 함")
        combo = [ids[0] for p in GB_REQUIRED if (ids := _gb_def.get((g, p), []))]
        for i in range(len(combo)):
            for j in range(i + 1, len(combo)):
                if tuple(sorted((combo[i], combo[j]))) in _excl:
                    errors.append(f"[외형기본] 성별 '{g}': 기본 조합에 금지 쌍 {combo[i]}×{combo[j]} 포함")
    # ── 유효 조합 풀 — (성별×성격) 전수 열거, 금지 쌍 포함 조합 제외. 0개 = 데드락(빌드 에러) ──
    import itertools as _it
    _pool_report = []
    for g in ("m", "f"):
        counts = []
        for pid in pers_ids:
            cand, ok = {}, True
            for part in GB_SLOT_ORDER:
                pool = [d["id"] for d in _gb_by.values() if d["part"] == part and d["gender"] == g
                        and (not (al := [x.strip() for x in str(d["personalities"] or "").split(";") if x.strip()]) or pid in al)]
                if part in GB_OPTIONAL: pool = pool + [None]
                cand[part] = pool
                if not pool: ok = False; break
            n = 0
            if ok:
                for combo in _it.product(*(cand[p] for p in GB_SLOT_ORDER)):
                    ids = [c for c in combo if c]
                    if not any(tuple(sorted((ids[i], ids[j]))) in _excl
                               for i in range(len(ids)) for j in range(i + 1, len(ids))):
                        n += 1
            if n == 0:
                errors.append(f"[외형] 성별 '{g}' × 성격 '{pid}': 유효 조합 풀 0개 — 금지 조합·성격 필터·필수 슬롯 확인")
            counts.append(f"{pid} {n}")
        _pool_report.append(f"{g}: " + " · ".join(counts))
    report.append("『외형 풀』 " + " / ".join(_pool_report))
    for g in REGULAR_SLOTS:
        d = dict(zip(RSLOT_COLS, g))
        if not d["character"]: errors.append(f"[단골슬롯] day{d['day']}#{d['seq']}: character는 필수 — 랜덤 손님은 RandomWaves 시트에")
        elif d["character"] not in char_ids: errors.append(f"[단골슬롯] day{d['day']}#{d['seq']}: 캐릭터 {d['character']} 없음")
        if d["cameo_scene"] and d["cameo_scene"] not in scene_ids: errors.append(f"[단골슬롯] day{d['day']}#{d['seq']}: 카메오 씬 {d['cameo_scene']} 없음")
        # branch_choice는 3잔째 제공/거절 선택지 — max_rounds가 3 미만이면 발동 자체가 안 된다 (v2.2)
        if d["branch_choice"] and (d["max_rounds"] or 1) < 3:
            report.append(f"⚠ [단골슬롯] day{d['day']}#{d['seq']}: branch_choice=TRUE인데 max_rounds={d['max_rounds']} — 3잔째 분기가 발동하지 않음 (max_rounds=3 필요)")
    for b in BARKS:
        if b[0] and b[0] not in voice_ids: errors.append(f"[대사풀] voice {b[0]} 없음")
        if b[3] and not b[4]: l10n_missing.append(f"대사풀 '{b[3][:12]}…'")
    for ch in CHOICES:
        if ch[2] and not ch[3]: l10n_missing.append(f"선택지 {ch[0]}#{ch[1]}")
        if ch[4]:
            if not ch[8] or not ch[9]:
                errors.append(f"[선택지] {ch[0]}#{ch[1]}: when이 있으면 lock_reason_ko/en을 모두 작성해야 함")
        elif ch[8] or ch[9]:
            errors.append(f"[선택지] {ch[0]}#{ch[1]}: when이 없는 항목에는 lock_reason_ko/en을 작성하지 않음")
        # goto 대상 검증 (v2.2) — 오타 나면 런타임에서 조용히 점프 실패하므로 여기서 잡는다
        if ch[6] and ch[6] not in scene_ids:
            errors.append(f"[선택지] {ch[0]}#{ch[1]}: goto 씬 {ch[6]} 없음")
    _ch_sizes, _ch_free = {}, {}  # v3.0 — 선택지 세트는 2~4개 (1개짜리 확인용 금지, 5개 이상 UI 초과)
    for ch in CHOICES:
        _ch_sizes[ch[0]] = _ch_sizes.get(ch[0], 0) + 1
        # v3.2 — when이 빈 항목(무조건 선택 가능)의 수. 조건 미충족 항목은 회색으로 표시만 되고
        # 선택되지 않으므로, 무조건 항목이 하나도 없으면 전부 잠겨 진행이 막힐 수 있다.
        if not ch[4]:
            _ch_free[ch[0]] = _ch_free.get(ch[0], 0) + 1
    for cid, n in _ch_sizes.items():
        if not (2 <= n <= 4):
            errors.append(f"[선택지] {cid}: 항목 {n}개 — 세트는 2~4개여야 함")
        if not _ch_free.get(cid):
            errors.append(f"[선택지] {cid}: when이 빈 항목이 없음 — 조건이 전부 거짓이면 고를 수 있는 선택지가 사라진다. 무조건 선택 가능한 항목을 최소 1개 두어야 함")
    for st in STEPS:
        if st[2] == "goto" and st[4] and st[4] not in scene_ids:
            errors.append(f"[스텝] {st[0]}#{st[1]}: goto 씬 {st[4]} 없음")

    # 배선 대기 씬 리포트 (v2.2) — trigger=manual인데 어디서도 참조되지 않는 씬은 게임에 안 나온다.
    # 초안 반영(draft_tools import)이 manual 씬을 만들므로, PD가 배선을 깜빡한 것을 여기서 드러낸다.
    _refs = {c[6] for c in CHOICES if c[6]}
    _refs |= {st[4] for st in STEPS if st[2] == "goto" and st[4]}
    # InteractPoints의 단일 scene 참조만 직접 배선으로 집계한다.
    _refs |= {dict(zip(POINT_COLS, p))["action_ref"] for p in POINTS
              if dict(zip(POINT_COLS, p))["action_type"] == "scene"}
    _refs |= {dict(zip(RSLOT_COLS, r))["cameo_scene"] for r in REGULAR_SLOTS}
    _refs |= {e[3] for e in ENDINGS if e[3]}   # 엔딩이 scene_id로 호출하는 씬도 배선된 것
    _grouped = {s[0] for s in SCENES if dict(zip(SCENE_COLS, s))["group"]}
    _pending = sorted({s[0] for s in SCENES if dict(zip(SCENE_COLS, s))["trigger"] == "manual"}
                      - _refs - _grouped)
    if _pending:
        report.append(f"⚠ [배선 대기] manual 씬 {len(_pending)}개가 어디서도 참조되지 않음 — 배선 전엔 게임에 안 나옴: "
                      + ", ".join(_pending[:8]) + (" …" if len(_pending) > 8 else ""))

    # 퀘스트 검증 (v1.4)
    quest_ids = {q[0] for q in QUESTS}
    point_ids = {p[0] for p in POINTS}
    for q in QUESTS:
        if q[1] and not q[2]: l10n_missing.append(f"퀘스트 제목 {q[0]}")
        if q[3] not in ("main", "side"): errors.append(f"[퀘스트] {q[0]}: kind {q[3]} 불가 (main/side)")
    stages_by_quest = {}
    for s in QUEST_STAGES:
        d = dict(zip(QSTAGE_COLS, s))
        if d["quest_id"] not in quest_ids: errors.append(f"[퀘스트] 스테이지의 quest_id {d['quest_id']} 없음")
        stages_by_quest.setdefault(d["quest_id"], []).append(d["stage"])
        g = d["goal"]
        if g.startswith("interact:"):
            if g[9:] not in point_ids: errors.append(f"[퀘스트] {d['quest_id']}#{d['stage']}: 포인트 {g[9:]} 없음")
        elif g.startswith("serve:"):
            if g[6:] not in cocktail_ids: errors.append(f"[퀘스트] {d['quest_id']}#{d['stage']}: 칵테일 {g[6:]} 없음")
        else:
            errors.append(f"[퀘스트] {d['quest_id']}#{d['stage']}: goal '{g}' 불가 — interact:<포인트>/serve:<칵테일>만 지원")
    for qid in quest_ids:
        st = sorted(stages_by_quest.get(qid, []))
        if not st: errors.append(f"[퀘스트] {qid}: 스테이지가 하나도 없음")
        elif st != list(range(1, len(st) + 1)): errors.append(f"[퀘스트] {qid}: 스테이지 번호가 1..N 연속이 아님 → {st}")

    # effects 문자열 전수 스캔 — quest()/unlock_recipe()/give() 참조 무결성
    all_effects = (
        [(f"스텝 {s[0]}#{s[1]}", s[8]) for s in STEPS if s[8]] +
        [(f"선택지 {c[0]}#{c[1]}", c[5]) for c in CHOICES if c[5]] +
        [(f"주문 {o[0]}#{o[1]}", o[4]) for o in ORDERS if o[4]] +
        [(f"퀘스트보상 {q[0]}", q[4]) for q in QUESTS if q[4]] +
        [(f"퀘스트단계 {s[0]}#{s[1]}", s[4]) for s in QUEST_STAGES if s[4]] +
        [(f"단골슬롯 day{r[0]}#{r[1]}", r[9]) for r in REGULAR_SLOTS if r[9]] +
        [(f"씬완료 {s[0]}", dict(zip(SCENE_COLS, s))["on_complete_effects"])
         for s in SCENES if dict(zip(SCENE_COLS, s))["on_complete_effects"]])
    unlocked_by_effect = set()
    for src, eff in all_effects:
        for m in re.findall(r"quest\((\w+)\)\.advance", eff):
            if m not in quest_ids: errors.append(f"[퀘스트] {src}: quest({m}) 없음")
        for m in re.findall(r"unlock_recipe\((\w+)\)", eff):
            unlocked_by_effect.add(m)
            if m not in cocktail_ids: errors.append(f"[레시피해금] {src}: 칵테일 {m} 없음")
        for m in re.findall(r"give\((\w+)", eff):
            if m not in ing_ids: errors.append(f"[지급] {src}: 재료 {m} 없음")

    # 해금 경로 검증 — 퀘스트 전용 재료 컨벤션(99+when) / 도달 불가 칵테일
    for s in shelf_dicts():
        if (s["unlock_day"] or 0) >= 99 and not s["unlock_when"]:
            errors.append(f"[선반] {s['id']}: unlock_day 99(퀘스트/이벤트 전용)인데 unlock_when 없음 — 영구 미해금")
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        if (d["unlock_day"] or 0) >= 99 and not d["unlock_when"] and d["id"] not in unlocked_by_effect:
            errors.append(f"[칵테일] {d['id']}: 해금 경로 없음 — unlock_when 또는 어딘가의 unlock_recipe() 필요")

    # 해금일이 재료 입고일보다 빠르면 '메뉴에는 뜨는데 못 만드는' 상태 — 의도적일 수 있어 경고
    _ing_day = {r[0]: dict(zip(SHELF_COLS, r))["unlock_day"] for r in shelf_rows()}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        used = [l["ingredient"] for l in recipe_lines(d["id"])]
        need = max([_ing_day.get(u) or 0 for u in used] or [0])
        if (d["unlock_day"] or 0) < need:
            report.append(f"⚠ [해금] {d['id']}: 해금 {d['unlock_day']}일차인데 재료는 {need}일차 입고 "
                          f"— 그 사이엔 메뉴에 보이지만 만들 수 없음(대본 지정 제조용이면 정상)")

    # 주문 풀 검증 (제조 개편: 티어 폐지) — order 공란이면 그날 해금 풀 추첨, 지정이면 존재·해금·status 대조.
    # 풀은 status=confirmed만 — tbd(수량 미확정)가 주문되면 채점이 불가능하다
    _ck_unlock = {c[0]: dict(zip(CK_COLS, c))["unlock_day"] for c in COCKTAILS}
    _ck_status = {c[0]: dict(zip(CK_COLS, c))["status"] for c in COCKTAILS}
    for cols, rows, tag in ((WAVE_COLS, RANDOM_WAVES, "웨이브"), (RSLOT_COLS, REGULAR_SLOTS, "단골슬롯")):
        for sl in rows:
            s = dict(zip(cols, sl))
            if s["order"]:
                if s["order"] not in _ck_unlock:
                    errors.append(f"[{tag}] day{s['day']}#{s['seq']}: 지정 칵테일 {s['order']} 없음")
                elif (99 if _ck_unlock[s["order"]] is None else _ck_unlock[s["order"]]) > s["day"]:
                    errors.append(f"[{tag}] day{s['day']}#{s['seq']}: {s['order']}는 {_ck_unlock[s['order']]}일차 해금 — 그날 손님이 주문 불가")
                elif _ck_status[s["order"]] != "confirmed":
                    errors.append(f"[{tag}] day{s['day']}#{s['seq']}: {s['order']}는 status=tbd — 수량 미확정이라 채점 불가, confirmed 후 지정할 것")
            else:
                pool = [i for i, u in _ck_unlock.items()
                        if (u if u is not None else 99) <= s["day"] and _ck_status[i] == "confirmed"]
                if not pool:
                    errors.append(f"[{tag}] day{s['day']}#{s['seq']}: 그날 주문 가능한(해금+confirmed) 칵테일 0종 — 손님이 주문할 게 없음")

    # 대사 커버리지 (v2.0, 저작 2파일 분리 대응) — 성격은 System 파일, 대사(Barks)는 Narrative 파일이라
    # 서로 어긋나도 각자 화면에선 안 보인다. 성격×핵심 상황에 대사가 0줄이면 손님이 침묵하므로 여기서 잡는다.
    CORE_SITS = ["call", "order", "serve_thanks", "react_excellent", "react_good", "react_decent",
                 "react_poor", "react_sewage", "bye_good", "bye_bad", "idle"]
    if any(dict(zip(WAVE_COLS, w))["max_rounds"] > 1 for w in RANDOM_WAVES):
        CORE_SITS.append("reorder")
    bark_pool = {}
    for b in BARKS:
        bark_pool.setdefault((b[0] or "", b[1]), 0)
        bark_pool[(b[0] or "", b[1])] += 1
    mute = []
    # 성격 5종 + 1부 카메오로 오는 단골(전용 voice) — 카메오도 bark 폴백이 비면 침묵한다 (v2.2)
    cameo_voices = sorted({dict(zip(RSLOT_COLS, r))["character"] for r in REGULAR_SLOTS
                           if dict(zip(RSLOT_COLS, r))["character"]})
    for vid in [p[0] for p in PERSONALITIES]:
        for sit in CORE_SITS:
            if not bark_pool.get((vid, sit)) and not bark_pool.get(("", sit)):
                mute.append(f"{vid}×{sit}")
    if mute:
        report.append(f"⚠ [대사 커버리지] 전용도 공용도 대사가 없는 조합 {len(mute)}건 — 이 상황에서 손님이 침묵함: "
                      + ", ".join(mute[:6]) + (" …" if len(mute) > 6 else ""))
    # ── v2.5 상황 사전 FK + 표정 유효성 ──
    sit_ids = {s[0] for s in BARK_SITUATIONS}
    expr_by_char = {}
    for e in EXPRESSIONS:
        expr_by_char.setdefault(e[0], set()).add(e[1])
    common_exprs = expr_by_char.get("common", set())
    for s in BARK_SITUATIONS:
        if s[1] and s[1] not in common_exprs:
            errors.append(f"[상황사전] {s[0]}: 기본 표정 '{s[1]}' — common 세트에 없음 {sorted(common_exprs)}")
    for b in BARKS:
        if b[1] not in sit_ids:
            errors.append(f"[대사풀] situation '{b[1]}' — BarkSituations 사전에 없음(오타?)")
        if b[2]:
            allowed = (expr_by_char.get(b[0], set()) | common_exprs) if b[0] in char_ids else common_exprs
            if b[2] not in allowed:
                errors.append(f"[대사풀] {b[0] or '공용'}/{b[1]}: 표정 '{b[2]}' — 사용 가능 세트에 없음")

    # v2.3 카메오는 공용 폴백 금지(PD 26.07.24, 명세서 §2.2) — 전용 대사가 없으면 경고가 아니라 에러.
    # 발생 가능한 전 상황을 전용으로 요구한다 (reorder·drunk 계열은 max_rounds가 2 이상일 때만).
    CAMEO_SITS = CORE_SITS + ["call_urge", "call_final", "order_think", "serve_urge",
                              "serve_final", "wrong_receive", "wrong_drink",
                              "leave_coaster", "leave_serve"]
    for r in REGULAR_SLOTS:
        rd = dict(zip(RSLOT_COLS, r))
        vid = rd["character"]
        need = [s for s in CAMEO_SITS if s != "reorder"] + (["reorder"] if rd["max_rounds"] > 1 else [])
        missing = [sit for sit in need if not bark_pool.get((vid, sit))]
        if missing:
            errors.append(f"[카메오 대사] {vid}: 전용 대사 없는 상황 {len(missing)}건(공용 폴백 금지) — "
                          + ", ".join(missing))

    # ── 텍스트 태그 전수 검사 (v2.3, 명세서 §2.2) — {변수} 화이트리스트 + <연출> 문법·짝 ──
    TAG_VARS = {"cocktail"}
    tag_ids = {t[0] for t in TEXT_TAGS}
    TAGTOK = re.compile(r"<(/?)([^<>]+)>")
    VARTOK = re.compile(r"\{([^{}]*)\}")
    def check_text_tags(text, where):
        if not text: return
        s = str(text)
        for m in VARTOK.finditer(s):
            if m.group(1) not in TAG_VARS:
                errors.append(f"[태그] {where}: 치환 변수 '{{{m.group(1)}}}' 불가 (허용: {sorted(TAG_VARS)})")
        stack = []
        for m in TAGTOK.finditer(s):
            closing, name = m.group(1) == "/", m.group(2).strip()
            if not closing and name.isdigit():
                continue   # <2000> = ms 대기 (예약)
            if name not in tag_ids:
                errors.append(f"[태그] {where}: <{name}> — TextTags 시트에 없음 (런타임이 조용히 무시함)")
                continue
            if closing:
                if not stack or stack[-1] != name:
                    errors.append(f"[태그] {where}: </{name}> 짝이 안 맞음")
                else:
                    stack.pop()
            else:
                stack.append(name)
        if stack:
            errors.append(f"[태그] {where}: <{stack[-1]}> 닫히지 않음")
    for b in BARKS:
        check_text_tags(b[3], f"바크 {b[0] or '공용'}/{b[1]}")
        check_text_tags(b[4], f"바크 {b[0] or '공용'}/{b[1]}(en)")
    for s in STEPS:
        if s[2] == "say":
            check_text_tags(s[5], f"스텝 {s[0]}#{s[1]}")
            check_text_tags(s[6], f"스텝 {s[0]}#{s[1]}(en)")
    for c in CHOICES:
        check_text_tags(c[2], f"선택지 {c[0]}#{c[1]}")
        check_text_tags(c[3], f"선택지 {c[0]}#{c[1]}(en)")
        check_text_tags(c[8], f"선택지 {c[0]}#{c[1]} 잠금 사유")
        check_text_tags(c[9], f"선택지 {c[0]}#{c[1]} 잠금 사유(en)")
    _required_ui = {
        "ui_data_error_title", "ui_data_error_body", "ui_data_error_retry",
        "ui_data_error_cancel", "ui_data_error_repeat_body",
        "ui_data_error_safe_exit", "ui_return_to_title",
    }
    _ui_map = {k: (ko, en) for k, ko, en in UI_STRINGS}
    for _key in sorted(_required_ui):
        if _key not in _ui_map:
            errors.append(f"[UIStrings] DATA_ERROR 복구 필수 키 '{_key}' 없음")
        elif not _ui_map[_key][0] or not _ui_map[_key][1]:
            errors.append(f"[UIStrings] {_key}: DATA_ERROR 복구 문구는 ko/en을 모두 작성해야 함")
    _required_home_ui = {
        "ui_home_exit", "ui_home_exit_blocked", "ui_home_sleep_confirm",
        "ui_save_overwrite_confirm", "ui_save_success", "ui_autosave_failed",
        "ui_home_map_transition_failed",
    }
    for _key in sorted(_required_home_ui):
        if _key not in _ui_map:
            errors.append(f"[UIStrings] 집 기능 필수 키 '{_key}' 없음")
        elif not _ui_map[_key][0] or not _ui_map[_key][1]:
            errors.append(f"[UIStrings] {_key}: 집 기능 문구는 ko/en을 모두 작성해야 함")
    for k, ko, en in UI_STRINGS:
        check_text_tags(ko, f"UI {k}")
        check_text_tags(en, f"UI {k}(en)")

    # ── 2부 종료 트리거 검사 (v2.3, 명세서 §1.1) — end_part가 없거나 마지막 씬이 아니면 뒤 씬이 유실된다 ──
    for day in sorted({s[1] for s in SCENES if s[1] is not None}):
        bar_scenes = [dict(zip(SCENE_COLS, s)) for s in SCENES
                      if s[1] == day and dict(zip(SCENE_COLS, s))["phase"] == "bar"]
        if not bar_scenes:
            continue
        owners = {st[0] for st in STEPS if st[2] == "end_part" and st[0] in {b["id"] for b in bar_scenes}}
        if not owners:
            errors.append(f"[종료 트리거] day{day}: bar 씬에 end_part 스텝 없음 — 2부가 끝나지 않아 정산 불가(명세서 §1.1)")
            continue
        last_id = max(bar_scenes, key=lambda b: b["seq"])["id"]
        if last_id not in owners:
            errors.append(f"[종료 트리거] day{day}: end_part가 {sorted(owners)}에 있는데 마지막 bar 씬은 '{last_id}' "
                          f"— 종료 신호 뒤에 남는 씬은 재생되지 않는다")

    # ── 대본 주문 해금 대조 (제조 개편으로 경고 강등) — 대본 지정 제조(craft 스텝)는 메뉴 해금을
    # 무시하고 필요한 재료를 노출하는 규칙이라 진행은 막히지 않는다. 의도 확인용으로만 알린다.
    _ck_unlock2 = {c[0]: dict(zip(CK_COLS, c))["unlock_day"] for c in COCKTAILS}
    _scene_day = {s[0]: s[1] for s in SCENES}
    for st in STEPS:
        if st[2] != "order" or not st[4].startswith("exact:"):
            continue
        cid = st[4][6:]
        day = _scene_day.get(st[0])
        if cid in _ck_unlock2 and day is not None and (99 if _ck_unlock2[cid] is None else _ck_unlock2[cid]) > day:
            report.append(f"⚠ [주문 해금] {st[0]}#{st[1]}: day{day}에 '{cid}' 주문 — 메뉴 해금은 day{_ck_unlock2[cid]}. "
                          f"대본 지정 제조는 해금을 무시하므로 진행되지만, 의도한 지정인지 확인할 것")

    # ── when DSL 전수 검사 (v1.5) — 문법 + 참조 무결성 ─────────────────
    WHEN_TOKEN = re.compile(
        r"^(day|money|reputation|grade|craft_grade|final_grade|order_match|phase"
        r"|meta\.endings"
        r"|flag\.\w+|affinity\.\w+|alive\.\w+|quest\.\w+\.stage"
        r"|cocktail\.id|ordered_cocktail\.id|served_cocktail\.id"
        r"|cocktail\.abv|cocktail\.tag\([^)]+\))$")
    CMP = re.compile(r"^(\S+)\s*(==|!=|>=|<=|>|<)\s*(\S+)$")
    NUM = re.compile(r"^-?\d+(\.\d+)?$")
    GRADES = {"excellent", "good", "decent", "poor", "sewage"}
    # meta.endings = 세이브 슬롯 밖 메타 저장(수집한 엔딩 수) — 다회차 판정용.
    # 데모: 0일차 튜토리얼 스킵 선택지의 when에 meta.endings >= 1 로 사용
    NUMERIC_LHS = ("day", "money", "reputation", "cocktail.abv", "meta.endings")
    alive_chars = {c[0] for c in CHARACTERS if dict(zip(CHAR_COLS, c))["alive_flag"]}
    referenced_flags, set_flags = {}, {}

    def check_token(tok, src):
        if not WHEN_TOKEN.match(tok):
            errors.append(f"[when문법] {src}: 어휘 '{tok}' 불가"); return
        if tok.startswith("flag."):
            referenced_flags.setdefault(tok[5:], src)
        elif tok.startswith("affinity.") and tok[9:] not in char_ids:
            errors.append(f"[when] {src}: 캐릭터 {tok[9:]} 없음")
        elif tok.startswith("alive.") and tok[6:] not in alive_chars:
            errors.append(f"[when] {src}: {tok[6:]}에 생사 변수(alive_flag) 없음")
        elif tok.startswith("quest.") and tok.split(".")[1] not in quest_ids:
            errors.append(f"[when] {src}: 퀘스트 {tok.split('.')[1]} 없음")

    def check_when(expr, src):
        if not expr: return
        for raw in str(expr).split("&&"):
            cl = raw.strip()
            if cl.startswith("!"): cl = cl[1:].strip()
            if not cl:
                errors.append(f"[when문법] {src}: 빈 절"); continue
            m = CMP.match(cl)
            if m:
                lhs, _, rhs = m.groups()
                check_token(lhs, src)
                if lhs in ("grade", "craft_grade", "final_grade") and rhs not in GRADES:
                    errors.append(f"[when] {src}: 등급 '{rhs}' 불가")
                elif lhs in ("cocktail.id", "ordered_cocktail.id", "served_cocktail.id") and rhs not in cocktail_ids:
                    errors.append(f"[when] {src}: 칵테일 {rhs} 없음")
                elif (lhs in NUMERIC_LHS or lhs.startswith(("affinity.", "quest."))) and not NUM.match(rhs):
                    errors.append(f"[when] {src}: '{lhs}' 비교값 '{rhs}'는 숫자가 아님")
            else:
                check_token(cl, src)

    for src, w in (
        [(f"씬 {s[0]}", dict(zip(SCENE_COLS, s))["when"]) for s in SCENES] +
        [(f"스텝 {s[0]}#{s[1]}", s[7]) for s in STEPS] +
        [(f"선택지 {c[0]}#{c[1]}", c[4]) for c in CHOICES] +
        [(f"주문 {o[0]}#{o[1]}", o[2]) for o in ORDERS] +
        [(f"포인트 {p[0]}(spawn)", dict(zip(POINT_COLS, p))["spawn_when"]) for p in POINTS] +
        [(f"포인트 {p[0]}(interact)", dict(zip(POINT_COLS, p))["interact_when"]) for p in POINTS] +
        [(f"퀘스트단계 {s[0]}#{s[1]}", s[3]) for s in QUEST_STAGES] +
        [(f"엔딩 {e[1]}", e[2]) for e in ENDINGS] +
        [(f"칵테일해금 {c[0]}", dict(zip(CK_COLS, c))["unlock_when"]) for c in COCKTAILS] +
        [(f"선반해금 {s2['id']}", s2["unlock_when"]) for s2 in shelf_dicts()] +
        [(f"취향 {t[0]}#{t[1]}", t[2]) for t in TASTES] +
        [(f"수첩 {d[0]}/{d[2]}", d[3]) for d in DOSSIER]):
        check_when(w, src)

    # ── 단골 수첩(Dossier) 검증 (v1.9.5) ──
    DOSSIER_KINDS = ("desc", "taste", "history", "secret", "recent")
    for d in DOSSIER:
        dd = dict(zip(DOSSIER_COLS, d))
        if dd["character_id"] not in char_ids:
            errors.append(f"[수첩] 캐릭터 {dd['character_id']} 없음")
        if dd["kind"] not in DOSSIER_KINDS:
            errors.append(f"[수첩] {dd['character_id']}: kind {dd['kind']} 불가 {DOSSIER_KINDS}")
        if not isinstance(dd["min_affinity"], int) or dd["min_affinity"] < 0:
            errors.append(f"[수첩] {dd['character_id']}/{dd['kind']}: min_affinity는 0 이상 정수")
        if dd["text_ko"] and not dd["text_en"]:
            l10n_missing.append(f"수첩 {dd['character_id']}/{dd['kind']}")

    # ── 취향(Tastes) 검증 (v1.6) ──
    matrix_tiers = {r[0] for r in AFFINITY_MATRIX}
    seen_taste = set()
    for t in TASTES:
        d = dict(zip(TASTE_COLS, t))
        if d["character_id"] not in char_ids: errors.append(f"[취향] 캐릭터 {d['character_id']} 없음")
        if d["tier"] == "miss" or d["tier"] not in matrix_tiers:
            errors.append(f"[취향] {d['character_id']}#{d['seq']}: tier '{d['tier']}' 불가 — AffinityMatrix의 love/good/ok/dislike만(miss는 오제조 전용)")
        if not d["when"]: errors.append(f"[취향] {d['character_id']}#{d['seq']}: when 비어 있음 — 기본값 ok는 행 없이 표현")
        if (d["character_id"], d["seq"]) in seen_taste: errors.append(f"[취향] {d['character_id']}#{d['seq']} 중복")
        seen_taste.add((d["character_id"], d["seq"]))

    # ── effects DSL 전수 검사 (v1.5) — 오타 효과는 런타임이 조용히 무시하므로 빌드에서 차단 ──
    EFF_RULES = [
        (re.compile(r"^affinity\.(\w+)\s*[+\-]=\s*\d+$"), lambda m, src:
            None if m.group(1) in char_ids else errors.append(f"[효과] {src}: 캐릭터 {m.group(1)} 없음")),
        (re.compile(r"^flag\.(\w+)\s*=\s*(true|false)$"), lambda m, src:
            set_flags.setdefault(m.group(1), src)),
        (re.compile(r"^alive\.(\w+)\s*=\s*(true|false)$"), lambda m, src:
            None if m.group(1) in char_ids else errors.append(f"[효과] {src}: 캐릭터 {m.group(1)} 없음")),   # 생사 변경 (v1.9.6)
        (re.compile(r"^(money|reputation)\s*[+\-]=\s*\d+$"), lambda m, src: None),
        (re.compile(r"^give\(\w+\s*,\s*\d+\)$"), lambda m, src: None),      # 대상 존재는 위 전수 스캔에서 검사
        (re.compile(r"^unlock_recipe\(\w+\)$"), lambda m, src: None),
        (re.compile(r"^quest\(\w+\)\.advance$"), lambda m, src: None),
    ]
    for src, eff in all_effects:
        for raw in str(eff).split(";"):
            cl = raw.strip()
            if not cl: continue
            for rx, act in EFF_RULES:
                m = rx.match(cl)
                if m: act(m, src); break
            else:
                errors.append(f"[효과문법] {src}: '{cl}' — 문법에 없음(오타 시 런타임에서 무시됨)")

    # ── 플래그 교차 검사 (v1.5) — 오타·미작성 구간 탐지 ──
    flag_notes = []
    _reserved_unset_flags = {
        "rios_accepted": "최종일 리오스 제안 선택지 데이터 추가 시 설정",
    }
    for fl, src in sorted(referenced_flags.items()):
        if fl not in set_flags:
            if fl in _reserved_unset_flags:
                flag_notes.append(f"ℹ [예약 플래그] '{fl}' — {_reserved_unset_flags[fl]} (현재 데모 미도달)")
            else:
                flag_notes.append(f"⚠ [플래그] '{fl}' — 참조({src})되지만 어디서도 세워지지 않음 (오타 or 미작성 일차)")
    for fl, src in sorted(set_flags.items()):
        if fl not in referenced_flags:
            flag_notes.append(f"ℹ [플래그] '{fl}' — 세워지지만({src}) 아직 아무 조건도 참조하지 않음")
    report.extend(flag_notes)

    # ── 호감도 상한 시뮬레이션 (v1.5) — 최선 플레이 가정 상한치 ──
    scene_day = {s[0]: s[1] for s in SCENES}
    max_day = max((s[1] for s in SCENES if s[1] is not None and s[1] < 99), default=0)   # 99 = 테스트 컨벤션 — 상한 리포트 제외
    choice_day = {}
    for s in STEPS:
        if s[2] == "choice": choice_day[s[4]] = scene_day.get(s[0], 0)
    AFF = re.compile(r"affinity\.(\w+)\s*\+=\s*(\d+)")
    day_gain = {}   # day -> char -> 명시 가산
    def add_gain(day, char, val):
        day_gain.setdefault(day, {}).setdefault(char, 0)
        day_gain[day][char] += val
    for s in STEPS:              # 스텝 효과 (when 게이트는 낙관 통과 가정)
        for m in AFF.finditer(s[8] or ""):
            add_gain(scene_day.get(s[0], 0), m.group(1), int(m.group(2)))
    by_choice = {}               # 선택지: 같은 그룹 안에서는 캐릭터별 최대 옵션 1개만
    for c in CHOICES:
        for m in AFF.finditer(c[5] or ""):
            key = (c[0], m.group(1))
            by_choice[key] = max(by_choice.get(key, 0), int(m.group(2)))
    for (cid, char), val in by_choice.items():
        add_gain(choice_day.get(cid, 0), char, val)
    quest_ref_day = {}           # 퀘스트 보상 → 수주 씬의 일차로 귀속
    for s in STEPS:
        for qid in quest_ids:
            if qid in (s[8] or ""): quest_ref_day.setdefault(qid, scene_day.get(s[0], 0))
    for c in CHOICES:
        for st in QUEST_STAGES:
            if st[3] and st[3].replace("flag.", "") in (c[5] or ""):
                quest_ref_day.setdefault(st[0], choice_day.get(c[0], 0))
    for q in QUESTS:
        for m in AFF.finditer(q[4] or ""):
            add_gain(quest_ref_day.get(q[0], 0), m.group(1), int(m.group(2)))
    # 씬 서빙 잠재치 — 취향(Tastes) first-match 정적 평가 × excellent (flag 등 비칵테일 절은 낙관 통과)
    ck_by_id = {c[0]: dict(zip(CK_COLS, c)) for c in COCKTAILS}
    mat_exc = {r[0]: r[1] for r in AFFINITY_MATRIX}
    taste_rows = {}
    for t in TASTES: taste_rows.setdefault(t[0], []).append(t)
    for v in taste_rows.values(): v.sort(key=lambda t: t[1])

    def static_taste(char, ckid):
        ck = ck_by_id.get(ckid)
        if not ck: return "ok"
        tags = [x.strip() for x in (ck["tags"] or "").split(";")]
        for t in taste_rows.get(char, []):
            hit = True
            for raw in str(t[2]).split("&&"):
                cl = raw.strip(); neg = cl.startswith("!")
                if neg: cl = cl[1:].strip()
                if not cl.startswith("cocktail."):
                    hit = False; break   # 상황부 행(flag 등)은 상한 계산에서 비활성 — 기본 취향만 평가
                val = True
                m2 = CMP.match(cl)
                if m2 and m2.group(1) == "cocktail.id":
                    val = (ck["id"] == m2.group(3)) if m2.group(2) == "==" else (ck["id"] != m2.group(3))
                elif m2 and m2.group(1) == "cocktail.abv":
                    a, op, r = ck["abv"], m2.group(2), float(m2.group(3))
                    val = {"==": a == r, "!=": a != r, ">=": a >= r, "<=": a <= r, ">": a > r, "<": a < r}[op]
                elif cl.startswith("cocktail.tag("):
                    val = cl[13:-1].strip() in tags
                if neg: val = not val
                if not val: hit = False; break
            if hit: return t[3]
        return "ok"

    serve_potential = {}
    for sid, steps in by_scene.items():
        last_ck = None
        for st in steps:
            if st["type"] == "order" and str(st["arg"]).startswith("exact:"): last_ck = st["arg"][6:]
            elif st["type"] == "craft" and str(st["arg"]).startswith("tutorial:"): last_ck = st["arg"][9:]
            elif st["type"] == "serve" and st["actor"]:
                gain = mat_exc.get(static_taste(st["actor"], last_ck), 0)
                d = scene_day.get(sid, 0)
                serve_potential.setdefault(st["actor"], {}).setdefault(d, 0)
                serve_potential[st["actor"]][d] += gain
    multi_chars = [c[0] for c in CHARACTERS if dict(zip(CHAR_COLS, c))["affinity"]]
    report.append(f"=== 호감도 상한 시뮬레이션 (day1~{max_day} 데이터 기준, 최선 플레이 상한) ===")
    for ch in multi_chars:
        cum = sum(day_gain.get(d, {}).get(ch, 0) for d in range(0, max_day + 1))
        pot = sum(serve_potential.get(ch, {}).values())
        if cum or pot:
            report.append(f"{ch}: 명시 +{cum} / 서빙 잠재 +{pot} (취향×excellent 기준) → 합계 상한 {cum + pot} — 엔딩컷 100 대비 {cum + pot}%")
    report.append(f"(day{max_day + 1}~13 데이터 작성 시 자동 갱신. 엔딩 happy 계열은 4인 100 필요 — 일차별 획득 설계의 기준선)")

    _ck_day = {c[0]: dict(zip(CK_COLS, c))["unlock_day"] for c in COCKTAILS}
    report.append("=== 일차별 입고표 (검증용, Day 0 스타트 — 입고 화면은 그날 바 오픈 직전) ===")
    for d in range(0, 11):
        new_ing = [s2["name_ko"] for s2 in shelf_dicts()
                   if s2["unlock_day"] == d and s2["kind"] in ("ingredient", "garnish")]   # 소모품만(잔·도구는 상시 비치)
        new_ck = [c[1] for c in COCKTAILS if _ck_day[c[0]] == d]
        report.append(f"Day {d}: 입고[{', '.join(new_ing) or '-'}] → 신규 가능[{', '.join(new_ck) or '-'}]")
    # 자동 적용 라인은 데이터 참조 때문에 Day 0에 해금돼 있어도 플레이어가 직접 고르는 선반에는 표시하지 않는다.
    _shelf_name_ko = {s2["id"]: s2["name_ko"] for s2 in shelf_dicts()}
    _day0_cocktails = {cid for cid, unlock_day in _ck_day.items() if unlock_day == 0}
    _day0_lines = [line for cid in _day0_cocktails for line in recipe_lines(cid)]
    _day0_manual_ids = {line["ingredient"] for line in _day0_lines if not line["auto_apply"]}
    _day0_auto_ids = {line["ingredient"] for line in _day0_lines if line["auto_apply"]}
    _day0_manual = [s2["name_ko"] for s2 in shelf_dicts() if s2["id"] in _day0_manual_ids]
    _day0_auto = [s2["name_ko"] for s2 in shelf_dicts() if s2["id"] in _day0_auto_ids]
    report.append(f"Day 0 수동 재료 선반[{', '.join(_day0_manual) or '-'}] / 자동 적용[{', '.join(_day0_auto) or '-'}]")
    cond_ing = [f'{s2["name_ko"]}({s2["unlock_when"]})' for s2 in shelf_dicts() if (s2["unlock_day"] or 0) >= 99]
    cond_ck = [c[1] for c in COCKTAILS if (_ck_day[c[0]] or 0) >= 99]
    if cond_ing: report.append(f"퀘스트/이벤트 해금 재료: {', '.join(cond_ing)}")
    if cond_ck: report.append(f"퀘스트/이벤트 해금 칵테일: {', '.join(cond_ck)}")
    report.append("=== 퀘스트 ===")
    for q in QUESTS:
        goals = " → ".join(s[2] for s in sorted((s for s in QUEST_STAGES if s[0] == q[0]), key=lambda s: s[1]))
        report.append(f"[{q[3]}] {q[0]} '{q[1]}': {goals} ⇒ 보상[{q[4]}]")
    merged_slots = ([(dict(zip(WAVE_COLS, g)), "") for g in RANDOM_WAVES]
                    + [(dict(zip(RSLOT_COLS, g)), f" [카메오: {dict(zip(RSLOT_COLS, g))['character']}]") for g in REGULAR_SLOTS])
    _ck_st = {c[0]: dict(zip(CK_COLS, c))["status"] for c in COCKTAILS}
    for d, tag in sorted(merged_slots, key=lambda x: (x[0]["day"], x[0]["seq"])):
        if d["order"]:
            report.append(f"슬롯 day{d['day']}#{d['seq']} 지정 주문: {d['order']}{tag}")
        else:
            pool = [i for i, u in _ck_day.items()
                    if (u if u is not None else 99) <= d["day"] and _ck_st[i] == "confirmed"]
            if pool:
                report.append(f"슬롯 day{d['day']}#{d['seq']} 해금 풀({len(pool)}): {', '.join(pool)}{tag}")
    # L10N 누락 판정 — strict면 에러, 아니면 리포트에 집계만 (en은 emit 시 ko로 폴백돼 빈 화면은 없음)
    if l10n_missing:
        if STRICT_L10N:
            errors += [f"[L10N] {m}: 영어 누락" for m in l10n_missing]
        else:
            report.append(f"⚠ [L10N] 영어 미번역 {len(l10n_missing)}건 — 8월 번역 예정, EN은 임시로 한국어 폴백 (--strict로 검사 가능)")
            for m in l10n_missing[:5]:
                report.append(f"    · {m}")
            if len(l10n_missing) > 5:
                report.append(f"    · … 외 {len(l10n_missing)-5}건")
    return errors, report

# ============================================================
# 출력 — XLSX (작업자 가독성: 탭 색상·헤더 메모·줄무늬·자동필터)
# ============================================================
HDR_FILL = PatternFill("solid", fgColor="2F3437")
HDR_FONT = Font(bold=True, color="FFFFFF")
DERIVED_FILL = PatternFill("solid", fgColor="6B6B7A")   # (파생) 컬럼 — 손대지 말 것
STRIPE_FILL = PatternFill("solid", fgColor="F4F1FA")    # 짝수행 줄무늬

# 시트 그룹별 탭 색상: 마스터=파랑 / 스케줄=초록 / 대본=보라 / 밸런스=주황 / 기타=회색
TAB_COLOR = {
    "master": "4472C4", "schedule": "70AD47", "script": "9E5FC1", "balance": "ED7D31", "etc": "A6A6A6",
}
SHEET_GROUP = {
    "Cocktails": "master", "RecipeLines": "master", "ShelfItems": "master", "Tags": "master",
    "Characters": "master", "Expressions": "master", "ExpressionParts": "master", "Cutscenes": "master",
    "ResourceMap": "audit", "FieldAnims": "master", "Personalities": "master", "Barks": "master",
    "GuestBodies": "master", "Days": "schedule", "RandomWaves": "schedule", "RegularSlots": "schedule", "Spots": "schedule",
    "InteractPoints": "schedule", "Transitions": "schedule",
    "Scenes": "script", "Steps": "script", "Choices": "script", "OrderRules": "script", "TextTags": "master", "BarkSituations": "master",
    "Quests": "script", "QuestStages": "script", "Endings": "script",
    "Config": "balance", "GradeCuts": "balance", "SettlementRules": "balance", "ScoreBands": "balance", "AffinityMatrix": "balance", "Tastes": "balance", "Dossier": "master",
    "UIStrings": "etc",
}

# 헤더 셀 메모(마우스오버 툴팁) — 작업자용 컬럼 설명
COL_DOCS = {
    "Cocktails": {
        "id": "snake_case 고유 id. 다른 시트가 이 값으로 이 칵테일을 가리킨다 — 한 번 정하면 바꾸지 말 것(참조가 전부 깨짐)",
        "name_ko": "메뉴판·주문에 뜨는 이름(한국어). 필수",
        "name_en": "영어 이름. 8월 번역 전까진 비워도 됨(비우면 빌드가 ko로 폴백)",
        "status": "confirmed(수량 확정 — RecipeLines 수량 필수)/tbd(신규 13종 — 수량 공란 허용, 수량 확정 파일 수령 시 confirmed로 전환)",
        "price": "판매가(골드). 정산 매출·팁·배상 계산의 기준값 — SettlementRules 시트의 배율이 이 값에 곱해진다",
        "abv": "도수(%). 손님 취향 판정(cocktail.abv)과 삼호 취함 분기(abv>=20)에 쓰인다",
        "glass": "정답 잔 — ShelfItems에서 kind=glass인 id. 비우면 정답이 '병째로'(병맥주). 플레이어가 ① 잔 선반에서 고른다",
        "mix": "섞는 방식 정답 — none/build/stir/shake. 저작 시트는 기존명 mix를 유지하지만 JSON 신규 코드는 동일 값의 target_mix_method를 사용한다",
        "prep": "병 개봉 정답 — 공란/cap. 저작 시트는 기존명 prep를 유지하지만 JSON 신규 코드는 cap을 open으로 정규화한 target_prep_action을 사용한다",
        "recipe_desc_ko": "제조법 설명(한국어) — 레시피 UI 상세 패널에 그대로 뜨는 문장. 손으로 적는다. RecipeLines는 채점 정답표이고 이 칸은 표시 전용이라, 수치를 고치면 이 문장도 같이 고쳐야 한다",
        "recipe_desc_en": "제조법 설명(영어)",
        "(파생)sprite": "대표 이미지 키 — Finished_{아이디 파스칼표기}. 목차·정보 화면·완성 잔 표시가 쓴다. 구엔진 겹침 4종은 기존 에셋 재사용",
        "(파생)serve_sprite": "제공 컷씬 이미지 키 — Serve_{아이디 파스칼표기}",
        "garnish": "정답 가니시 — ShelfItems에서 kind=garnish인 id. 플레이어가 ③ 가니시 선반에서 고른다. 비우면 정답은 '없음'(플레이어도 '없음'을 골라야 정답)",
        "color": "잔에 채워질 액체 색(R,G,B). 예: 255,255,255",
        "color2": "그라데이션 하단 색(R,G,B). 단색이면 비움 — 현재 oasis_sunset만 사용",
        "tags": "분류 키워드. 세미콜론(;)으로 구분. Tags 시트에 등록된 값만 허용(맛/느낌 2분류) — 레시피 UI 필터와 손님 취향 판정(cocktail.tag)이 이 값을 본다",
        "flavor_ko": "메뉴판에 뜨는 한 줄 설명(한국어). 손님이 고민할 때 분위기를 만드는 문구",
        "flavor_en": "위 설명의 영어판. 비워도 됨(ko 폴백)",
        "unlock_day": "며칠차부터 메뉴에 나오는지(Day 0 스타트 — 0=처음부터). 99=날짜로는 안 열림(unlock_when 필수). ※ 대본 지정 제조(craft 스텝)는 해금을 무시한다",
        "unlock_when": "조건부 해금(when 문법). 예: flag.q_퀘스트id_done — 퀘스트를 깨야 열리는 히든 레시피에 사용",
        "time_limit_sec": "제조 제한시간(초) — 수동 정본. 초과해도 제조는 계속되고 OvertimeBands(ScoreBands 시트)로 감점만 된다",
        "mixing_ice": "믹싱(셰이커·믹싱글라스)에 넣는 얼음 — none/cubed(각얼음)/crushed(크러시드)",
        "serving_ice": "서빙 잔에 넣는 얼음 — none/cubed/crushed. 레시피 UI 얼음 행이 이 두 칸을 표시한다",
    },
    "RecipeLines": {
        "cocktail_id": "이 재료 줄이 속한 칵테일 id (Cocktails 참조)",
        "seq": "표시·채점용 순번. ※ 실행 순서가 아니다 — 실제 기믹은 개봉→따르기→스퀴즈→파우더→믹스→필업 순서로 강제된다",
        "action": "투입 방식 — pour(따르기)/squeeze(스퀴즈)/powder(파우더)/fill_up(필업). 재료의 category가 아니라 이 값이 어떤 기믹이 나올지 결정",
        "ingredient_id": "넣을 재료 — ShelfItems에서 kind=ingredient인 id",
        "qty": "목표량. 오차 비율을 QuantityBands(ScoreBands 시트)에 대조해 그 기믹 점수가 된다 — fill_up도 pour와 동일하게 채점. status=tbd 칵테일만 수량 확정 전까지 비움",
        "unit": "단위 — oz(온스, ×30ml)/ml/tsp(티스푼, ×5ml)",
        "is_core": "핵심 재료(TRUE/FALSE). 핵심 재료를 빼먹으면 감점이 아니라 Sewage 확정",
        "auto_apply": "자동 투입(TRUE/FALSE). 데모의 설탕(powder)과 스퀴즈(squeeze)는 TRUE — 플레이어 조작 없이 적용되고 채점·제조시간에서 빠진다",
        "scored": "채점 대상(TRUE/FALSE) — auto_apply·noscore 플래그면 FALSE. 데모의 powder/squeeze는 FALSE",
    },
    "ShelfItems": {
        "id": "고유 id. 레시피·칵테일의 잔/가니시 칸이 이 값을 참조한다",
        "kind": "항목 역할 — ingredient(재료)/glass(잔)/tool(도구)/garnish(가니시). 데모 제조 준비는 ①잔 →②도구 →③재료 순서이며 가니시 데이터는 화면·판정에 사용하지 않는다",
        "name_ko": "화면에 표시되는 이름(한국어)",
        "name_en": "영어 이름(비워도 됨 — ko 폴백)",
        "category": "재료(kind=ingredient)일 때만 의미 — 어떤 기믹으로 갈지 결정. base·liqueur·wine_beer·juice·dairy·syrup=따르기 / fruit=스퀴즈 / powder=파우더 / mixer=필업",
        "color": "액체 렌더용 RGB. 스퀴즈·파우더 재료나 잔·도구는 비워도 됨",
        "sprite": "잔·도구·가니시의 스프라이트 리소스 키. 재료는 비움 (※ 아트 파일명 = 이 값)",
        "unlock_day": "며칠차부터 선반에 나오는지(Day 0 스타트). 0=처음부터, N=그날 입고, 99=날짜로는 영원히 안 열림(퀘스트 전용 — unlock_when 필수, 없으면 빌드 에러)",
        "unlock_when": "조건부 입고(when 문법). 조건이 참이 되면 다음 입고 화면에 등장한다. 예: flag.q_퀘스트id_done",
        "shop_price": "퇴근길 노점 판매가. 비우면 비매품. ※ 현재는 자동 입고가 기본이고 상점은 히든 재료용 보조 경로",
        "desc_ko": "입고 화면·제조 화면에서 보이는 설명(한국어)",
        "desc_en": "위 설명의 영어판(비워도 됨)",
        "default_action": "레시피 밖에서 이 재료를 선택했을 때 실행할 기본 행동 — pour/fill_up/squeeze/powder. 정답 레시피의 action을 대신하지 않는다",
        "prep_action": "재료 본 동작 전에 필요한 선행 행동 — 공란/open. 현재 beer만 open",
        "default_target_qty": "레시피 밖 수량 기믹을 정상 종료하기 위한 기본 목표량. 정답 점수로 사용하지 않고 UNEXPECTED 재료 기록에만 사용",
        "default_target_unit": "default_target_qty의 단위 — oz/ml/tsp",
        "shelf_group": "재료 선반 UI 배치 — liquor(술 선반)/fridge(냉장고 선반). category와 별개다. 데모에서 자동 투입되는 squeeze/powder 재료와 ingredient 이외 kind는 비운다",
        "liquid_alpha": "따르기·필업 액체 투명도(0.0~1.0). 0.0=완전 투명, 1.0=완전 불투명. pour/fill_up 재료는 필수이며 나머지는 비운다",
    },
    "Characters": {
        "id": "고유 id. 대사(Steps.actor)·취향·수첩이 전부 이 값으로 인물을 가리킨다",
        "name_ko": "대사창에 뜨는 이름(한국어)",
        "name_en": "영어 이름(비워도 됨)",
        "name_color": "대사창 이름 글자색(#RRGGBB). 누가 말하는지 색으로 구분된다",
        "role": "player(루나)/master(크리스)/guest(바에 오는 손님 — 단골·조연 공통, 등장 횟수는 대본이 정한다)/npc_street(거리 NPC)/cutscene(컷씬 전용)",
        "affinity": "호감도를 추적할 인물인가(TRUE/FALSE). TRUE인 인물만 단골 수첩에 실리고 엔딩 조건에 들어간다",
        "alive_flag": "생사 분기가 있는 인물만 기입(예: alive.samho). when 문법의 alive.X와 연동",
        "expressions": "이 인물이 쓸 수 있는 표정 목록(Expressions 시트 참조). 세미콜론 구분",
        "base_body": "파츠 애니메이션 캐릭터의 베이스 바디 리소스 키",
        "enter_sfx": "등장 효과음 키. 바에 오는 인물은 공용 디폴트(SFX_guest_door_in)를 명시하고, 전용음이 있으면 그 키로(크리스·포트·부비). 바 등퇴장이 없는 화자(루나·무생물·컷씬 전용)만 비운다",
        "exit_sfx": "퇴장 효과음 키(비워도 됨)",
        "note": "작업 메모. 게임에 나오지 않는다 — 자유롭게 적어도 됨",
    },
    "Expressions": {
        "character_id": "이 표정이 속한 인물(Characters 참조)",
        "expression": "표정 이름. 대사(Steps)의 arg 칸에 이 값을 적으면 그 표정으로 말한다. 'default'는 필수로 하나 있어야 함",
        "mode": "sprite(그림 한 장 교체) 또는 parts_anim(부위별 애니 조합). 자주 나오는 인물은 parts_anim, 스쳐가는 손님은 sprite",
        "sprite_key": "mode=sprite일 때 쓸 그림 리소스 키. parts_anim이면 비움",
        "note": "작업 메모(게임에 안 나옴)",
    },
    "ExpressionParts": {
        "character_id": "인물(Characters 참조)",
        "expression": "어느 표정에 속한 파츠인지(Expressions의 expression과 짝)",
        "part": "얼굴 부위 — eyes/eyebrows/upper_face/lower_face/body 등",
        "clip": "이 부위에 재생할 애니메이션 클립 키 (※ 아트 파일명 = 이 값)",
        "loop": "재생 방식 — always(계속 반복)/always_on_dialogue(대사 중에만 = 입 움직임)/once(한 번)/none(정지)/on_dialogue",
    },
    "Cutscenes": {
        "id": "컷씬 고유 id. 대사(Steps)의 timeline 타입이 이 값을 부른다",
        "kind": "timeline(Unity 타임라인 — 주력) / gif(스파인 애니를 GIF로 뽑은 것) / sprite(포스터 뷰 — 이미지 1장을 화면 중앙에 띄우고 timeline 스텝의 text가 하단 대사. 현재 실사용 0 = 예약)",
        "resource_key": "실제 리소스를 찾을 이름표",
        "note": "작업 메모(게임에 안 나옴)",
    },
    "ResourceMap": {
        "kind": "리소스 종류 — expr_char(인물 표정)/bg(배경)/cutscene/walk(걷기)/icon/ui",
        "data_key": "데이터에서 쓰는 키(예: 인물 id)",
        "resource_path": "실제 에셋이 있는 경로",
        "status": "검수 상태 — OK/확인필요/결정필요/일부/보관/폐기. 미결 항목 추적용",
        "note": "검수 메모",
    },
    "FieldAnims": {
        "character_id": "인물(Characters 참조)",
        "action": "동작 — idle/idle_blink/walk/run/dead 등",
        "resource_key": "SD 픽셀 스프라이트시트 경로 (※ 아트 파일명 = 이 값)",
        "status": "확보 상태 — OK/신규필요 등. 대본에서 쓰는데 없으면 빌드가 경고",
        "note": "작업 메모",
    },
    "Tags": {
        "tag_ko": "태그 한글 표기(정본 키). Cocktails.tags와 Tastes의 cocktail.tag(...)이 이 값을 그대로 쓴다",
        "tag_en": "영어 표기 — 정보 화면 키워드가 EN 빌드에서 이 값을 표시",
        "category": "분류 — taste(맛)/feel(느낌). 레시피 UI 목차 필터가 두 그룹으로 나눠 보여준다",
        "note": "작업 메모",
        "tag_id": "언어와 무관한 불변 snake_case ID. 새 엔진 로직은 이 값을 사용하고, 기존 한글 태그 조건은 마이그레이션 기간 동안 호환 유지",
    },
    "Personalities": {
        "id": "성격 id. GuestSlots의 personality 칸과 Barks의 voice_id가 이 값을 참조",
        "name_ko": "성격 이름(한국어) — 온화형/거친형 등",
        "name_en": "영어 이름",
        "tip_mult": "팁 배율. 1.0이 기본이고 높을수록 후하다(예: 1.1 = 10% 더 줌)",
        "patience_mult": "인내심 시간 배율. 1.0이 기본이고 **높을수록 오래 기다려준다**(1.2=여유로움, 0.8=성질 급함)",
        "think_chance": "코스터 드롭 후 order_think를 재생할 확률(0~1). 1.0=항상 고민, 0.3=대부분 즉답(ask_order 다음 바로 order). 카메오는 무관하게 항상 재생",
        "note": "작업 메모",
    },
    "Barks": {
        "voice_id": "누구의 대사인지 — Personalities의 id(성격별) 또는 Characters의 id(특정 인물). 비우면 공용 폴백",
        "situation": "언제 나오는 대사인지 — BarkSituations 시트의 목록에서 선택(없는 값 = 빌드 에러)",
        "expression": "이 대사를 칠 때의 표정(선택). 비우면 BarkSituations의 상황 기본 표정. 랜덤 손님=common 세트, 카메오=자기 표정 세트+common",
        "text_ko": "대사 원문(한국어)",
        "text_en": "영어 대사(비워도 됨 — ko 폴백)",
        "weight": "뽑힐 가중치. 클수록 자주 나온다. 보통 1",
    },
    "Tastes": {
        "character_id": "취향의 주인(Characters 참조). affinity=TRUE인 인물만 의미 있음",
        "seq": "판정 순서. **위에서부터 확인해 처음 맞는 줄이 이긴다** — 구체적인 조건(특정 칵테일)을 위에, 넓은 조건(태그·도수)을 아래에 둘 것",
        "when": "이 취향이 적용될 조건(when 문법). cocktail.id/cocktail.abv/cocktail.tag(맛)로 쓰고, flag를 섞으면 '상황에 따라 달라지는 취향'이 된다",
        "tier": "love(정말 좋아함)/good(좋아함)/ok(무난)/dislike(싫어함). 어느 줄에도 안 걸리면 자동으로 ok. ※ miss는 오제조 전용이라 여기 쓰면 빌드 에러",
        "note": "왜 이 취향인지 메모(게임에 안 나옴)",
    },
    "Dossier": {
        "character_id": "단골 수첩에 실릴 인물(Characters에서 affinity=TRUE인 인물만)",
        "min_affinity": "이 호감도 이상일 때 공개된다. 0=만나면 바로 공개. 잠긴 항목은 '🔒 호감도 N에 열림'으로 표시돼 플레이어가 목표를 알게 된다",
        "kind": "항목 종류 — desc(한 줄 소개)/taste(취향 소개문)/history(과거사)/secret(숨겨진 이야기)/recent(최근 근황). ※ 취향 판정은 Tastes 시트가 하고, 여기 taste는 '보여주는 문장'일 뿐",
        "when": "조건부 공개(when 문법). 주로 recent에 사용 — 스토리 진행에 따라 근황이 바뀐다",
        "text_ko": "수첩에 표시될 문장(한국어)",
        "text_en": "영어판(비워도 됨)",
        "note": "작업 메모",
    },
    "Days": {
        "day": "일차 번호. 1=튜토리얼 … 13=마지막",
        "label_ko": "그날의 제목(로딩 화면 등에 표시)",
        "label_en": "영어 제목",
        "start_phase": "그날이 어디서 시작하는지 — home(집에서 기상, 기본)/bar/commute_in",
        "bgm_street": "거리 배경음 키",
        "bgm_bar": "바 배경음 키",
        "upkeep_gold": "그날 정산에서 빠져나가는 유지비(골드). 0 = 차감 없음",
        "note": "그날의 요약 메모(입고 품목·주요 이벤트 등). 게임에 안 나옴",
    },
    "GuestBodies": {
        "part": "파트 종류 — body(바디: 얼굴·몸통·코·입·패널 라인 한 장)/outfit(의상)/eyes(눈)/hair(헤어). 스폰 시 파트별로 하나씩 추첨해 겹쳐 그린다(바디→의상→눈→헤어 순)",
        "id": "snake_case 고유 id",
        "gender": "m/f — 같은 성별 파트끼리만 조합된다",
        "personalities": "이 파트를 쓸 수 있는 성격 — 세미콜론 구분(예: rough;touchy). 비우면 전 성격 공용. 성격별 전용 외형을 만들 때 채운다",
        "mode": "sprite=이미지 한 장(현행) / parts_anim=파츠 애니(추후 전환용 — 단골 표정과 같은 규칙)",
        "sprite": "sprite 모드의 리소스 키. 아트 제작 중이라 지금은 가칭 — 완성 시 실키로 교체",
        "emotions": "표정별 교체 스프라이트 — '표정:키; 표정:키' (예: joy:Guest/eyes_m_1_joy; anger:…). 표정이 정해지면 이 맵에 있는 표정만 교체, 없으면 기본 sprite 유지. 비우면 표정 고정(현행). 표정은 common 세트, default 등록 금지. 감정 눈 아트가 나오면 eyes 행에 채운다",
        "weight": "추첨 가중치(1 이상 정수). 클수록 자주 나온다",
        "status": "완료/제작중/신규필요 — 에셋 검수용, 게임 미사용",
        "note": "기획 메모 — 실물 파일명 매핑 기재 (게임 미사용)",
        "is_default": "TRUE = 그 성별·슬롯의 기본 파츠. 성별×필수 슬롯마다 정확히 1개, personalities 공란 필수. 리소스 로드 실패 시 이 조합으로 대체(선택 슬롯은 항상 미착용)",
    },
    "GuestBodyExclusions": {
        "part_a": "금지 쌍의 한쪽 파츠 id (GuestBodies.id)",
        "part_b": "금지 쌍의 다른쪽 파츠 id — (a,b)와 (b,a)는 같은 규칙, 한 행만 등록",
        "note": "금지 이유 — 검수용, json 미배포 (예: 민소매 아님)",
    },
    "BarkSituations": {
        "situation": "상황 코드 — Barks.situation의 정본 목록. 새 상황은 반드시 여기 먼저 등록(없는 값을 쓰면 빌드 에러)",
        "default_expression": "이 상황의 기본 표정(common 세트: default/joy/sadness/anger/surprise/fear/annoyingB/annoyingR). 개별 대사가 expression을 비우면 이 값",
        "note": "메모",
    },
    "TextTags": {
        "tag": "태그 이름 — 대사에서 <이름>…</이름>으로 감싼다. 숫자 태그 <2000>은 등록 없이 ms 대기",
        "kind": "태그 종류: color(강조색)/speed_ms(문자당 출력 간격)/size_pct(글자 크기 %)",
        "value": "태그 값 — color는 #RRGGBB, speed_ms·size_pct는 숫자",
        "note": "메모",
    },
    "RandomWaves": {
        "day": "몇 일차의 손님인지",
        "seq": "그날 몇 번째로 오는 손님인지. RegularSlots와 번호를 나눠 쓴다 — 같은 날 같은 번호가 양쪽에 있으면 빌드 에러",
        "order": "주문할 칵테일 id 직접 지정. 비우면 그날 해금된 칵테일 전체에서 무작위 추첨 — 해금 전 칵테일을 지정하면 빌드 에러",
        "personality": "손님 성격(Personalities의 id). 비우면 무작위",
        "delay_sec": "앞 손님으로부터 몇 초 뒤에 들어오는지. 35~50초가 '한적한 슬럼가' 기본값",
        "max_rounds": "이 손님이 몇 잔이나 주문하는지(보통 1)",
        "branch_choice": "3잔째 취함 제공/거절 분기 대상인지(TRUE/FALSE, 명세서 §3.9)",
    },
    "RegularSlots": {
        "day": "몇 일차에 들르는지",
        "seq": "그날 몇 번째로 오는지. RandomWaves와 번호를 나눠 쓴다 — 같은 날 같은 번호가 양쪽에 있으면 빌드 에러",
        "character": "누가 오는지(Characters의 id). 필수 — 이름 없는 랜덤 손님은 RandomWaves 시트에",
        "order": "1부 카메오로 왔을 때 주문할 칵테일 id. 비우면 그날 해금 풀에서 추첨",
        "delay_sec": "앞 손님으로부터 몇 초 뒤에 들어오는지",
        "max_rounds": "몇 잔이나 주문하는지(보통 1)",
        "branch_choice": "분기 선택지가 붙는 슬롯인지(TRUE/FALSE)",
        "cameo_scene": "착석 시 재생할 씬 id (없으면 일반 주문 흐름)",
        "must_serve": "TRUE = 인내심 타이머 없음. 시간이 지나도 이탈하지 않고 응대할 때까지 좌석을 지킨다 — 2부·스토리로 이어지는 손님이 1부에서 유실되는 사고 방지. FALSE = 일반 손님처럼 타임아웃",
        "serve_effects": "이 손님을 서빙(정산)한 순간 실행할 효과 — flag.x = true; affinity.x += 1 등. 스토리·서브퀘스트 연결용",
    },
    "Spots": {
        "id": "위치 id. InteractPoints.spot_id가 이 값을 참조한다",
        "area": "구역 — street(큰길)/alley(뒷골목)",
        "desc": "어디인지 설명(작업용 — 게임에 안 나오므로 번역 대상 아님)",
        "note": "용도 메모. 좌표 대신 이름으로 관리하는 이유 = 배경이 수정돼도 안 깨지기 때문",
    },
    "InteractPoints": {
        "id": "지점 id. 퀘스트 목표(interact:이 id)가 참조한다",
        "kind": "actor(캐릭터)/object(사물·배경 오브젝트). 운영 거리에서 gimmick은 사용하지 않는다",
        "source_id": "actor면 Characters.id(그대로 사용), object면 리소스·논리 오브젝트 id",
        "spot_id": "배치 위치(Spots.id)",
        "facing": "actor가 바라보는 방향(left/right). object는 비운다",
        "phase": "등장 구간 — commute_in/commute_out/both",
        "spawn_when": "대상이 존재할 조건(when 문법). 비우면 상시 존재",
        "activation_mode": "interact(E키)/proximity(비캐릭터 자동 방송 전용). NPC 대화와 장소 전환은 interact만 허용",
        "interact_when": "상호작용이 활성화될 조건. once·순차 진행도는 별도 필드가 아니라 flag 조건으로 표현",
        "priority": "호환용 예약값. 데모 거리의 대상 선택은 이 값을 사용하지 않고 가장 가까운 유효 대상 하나를 선택",
        "action_type": "scene(단일 씬)/scene_group(조건을 통과한 첫 씬)/transition(장소 전환). 비우면 순수 배치",
        "action_ref": "action_type에 따른 Scenes.id / Scenes.group / Transitions.id",
        "note": "작업 메모",
    },
    "Transitions": {
        "id": "장소 전환 고유 id. InteractPoints.action_ref가 참조한다",
        "target_location": "도착 장소 컨트롤러 id(bar/home 등)",
        "target_phase": "도착 후 적용할 phase",
        "target_spot": "도착 앵커 id. 도착 컨트롤러가 자체 시작 위치를 정하면 비울 수 있다",
        "effect": "화면 전환 효과(fade/none)",
        "note": "작업 메모. 경계 자동 이동은 지원하지 않고 문에서 E 상호작용으로만 호출",
    },
    "Scenes": {
        "id": "씬 고유 id. Steps가 이 값으로 자기 소속을 밝힌다",
        "day": "몇 일차 씬인지. **0 = 일차 무관 공용 씬**(거리 오브젝트·NPC 등). 재생 일차를 정하는 건 trigger=auto 씬에서만 — interact 씬의 재생 일차는 InteractPoints.when이 정하고, 여기 day는 참고 표기",
        "phase": "어느 구간인지 — bar(바 2부)/bar_open(개점 전 대화)/commute_in/commute_out/home/dream/street/intro/ending(엔딩 씬 — Endings.scene_id로만 호출). **이 값이 비주얼을 자동 결정한다**(bar 계열=고해상도 흉상, 나머지=SD 픽셀)",
        "seq": "같은 phase 안에서의 재생 순서",
        "trigger": "bar/home/cutscene 기존 실행기용 발동 방식. 거리 JSON에서는 start_mode가 정본",
        "when": "이 씬이 재생될 조건(when 문법). 비우면 항상 재생",
        "title": "작업자용 제목(게임에 안 나옴)",
        "skippable": "건너뛰기 허용 여부(TRUE/FALSE). 튜토리얼은 FALSE",
        "group": "거리 scene_group의 묶음 id. seq 순서대로 when을 검사해 처음 참인 씬 하나를 실행",
        "start_mode": "거리 씬 시작 방식 — referenced(InteractPoints가 호출)/manual(choice.goto 등 다른 씬이 호출). 거리 외 씬은 비움",
        "on_complete_effects": "거리 씬 마지막 스텝 정상 완료 후 1회 적용. flag.x = true/false 형식이며 once·순차 진행 상태를 표현",
    },
    "Steps": {
        "scene_id": "이 스텝이 속한 씬(Scenes 참조)",
        "seq": "씬 안에서의 순서. 이 순서대로 한 줄씩 실행된다",
        "type": "무슨 동작인지 — say(대사)/enter·exit(등퇴장)/move(이동)/order(주문)/craft(제조 시작)/serve(서빙)/choice(선택지)/effect(효과만)/timeline(컷씬)/fx·sfx(연출·효과음)/end_part",
        "dialogue_id": "화면에 표시되는 대사의 언어 독립 고정 id. say와 text가 있는 order는 필수. dlg_ 접두사+영문 소문자·숫자·밑줄만 쓰며, 한 번 부여한 값은 seq가 바뀌어도 수정·재번호하지 않는다",
        "actor": "누가 하는지(Characters 참조). 연출용 스텝은 비움",
        "arg": "타입별 추가 정보 — say는 표정 이름, enter는 방향(L/M/R), order는 exact:칵테일id, craft는 order 또는 tutorial:칵테일id, choice는 Choices의 choice_id, timeline은 컷씬 id",
        "text_ko": "대사 원문(한국어). 리치텍스트 태그 사용 가능: <order>주문</order> <name>이름</name> <world>세계관용어</world>",
        "text_en": "영어 대사(비워도 됨 — ko 폴백)",
        "when": "이 스텝만의 조건(when 문법). 조건이 거짓이면 이 줄을 건너뛴다",
        "effects": "이 스텝이 일으키는 변화(effects 문법). 예: affinity.chris += 2; flag.x = true",
        "sync": "원본·비거리: wait/no_wait. 거리 배포 JSON: say 전용 auto/player_input(다른 거리 스텝에는 미출력)",
        "note": "작업 메모(게임에 안 나옴)",
    },
    "Choices": {
        "choice_id": "선택지 묶음 id. Steps의 choice 타입이 arg에 이 값을 적어 호출한다",
        "seq": "선택지가 표시될 순서",
        "text_ko": "선택지 문구(한국어)",
        "text_en": "영어 문구(비워도 됨)",
        "when": "이 선택지가 활성화될 조건(when 문법). 거짓이면 항목을 숨기지 않고 회색 비활성으로 표시한다",
        "effects": "이 선택지를 고르면 일어나는 변화(effects 문법). 호감도가 움직이는 주요 지점",
        "goto": "고른 뒤 점프할 씬 id. 비우면 원래 씬을 이어서 진행",
        "note": "작업 메모",
        "lock_reason_ko": "when이 거짓인 비활성 선택지에 표시할 이유(한국어). when이 있으면 필수",
        "lock_reason_en": "잠금 사유 영문. when이 있으면 필수",
    },
    "OrderRules": {
        "order_id": "이 판정표의 이름. 애매한 주문 하나에 여러 줄이 붙는다",
        "seq": "판정 순서. **위에서부터 확인해 처음 맞는 줄이 이긴다**(취향 시트와 같은 방식)",
        "when": "이 판정이 적용될 조건(when 문법). 예: cocktail.abv >= 20 → 독한 술을 냈을 때",
        "verdict": "판정 결과 — fulfill(요구대로 들어줌)/care(요구 대신 배려)/partial(어중간)/miss(완전히 빗나감)",
        "effects": "이 판정에서 일어나는 변화(effects 문법). 생사 분기 플래그가 여기서 세워진다",
        "react": "판정 직후 손님 반응 대사(비워도 됨)",
        "note": "이 분기가 무슨 의미인지 메모",
    },
    "Quests": {
        "id": "퀘스트 id. QuestStages가 이 값으로 자기 소속을 밝힌다",
        "title_ko": "퀘스트 제목(한국어)",
        "title_en": "영어 제목",
        "kind": "main(메인) 또는 side(서브)",
        "reward_effects": "**마지막 단계를 끝낸 순간 딱 한 번** 실행되는 보상(effects 문법). 예: affinity.samho += 10; unlock_recipe(bees_knees)",
        "note": "퀘스트 흐름 메모",
    },
    "QuestStages": {
        "quest_id": "어느 퀘스트의 단계인지(Quests 참조)",
        "stage": "단계 번호(1부터). 마지막 단계를 끝내면 보상이 나온다",
        "goal": "목표 — serve:칵테일id(그 술을 정상 서빙하면 완료) 또는 interact:포인트id(그 지점을 조사하면 완료). ※ 오제조나 sewage 등급은 인정되지 않는다",
        "when": "이 단계가 활성화될 조건. 보통 수주 플래그(예: flag.q_퀘스트id_started)",
        "on_complete": "이 단계를 넘길 때 실행할 효과(effects 문법). 비워도 됨",
    },
    "Endings": {
        "priority": "판정 순서(작을수록 먼저). **위에서부터 확인해 처음 조건이 맞는 엔딩으로 확정**된다",
        "id": "엔딩 id. **bad_gold만 매일 정산 확정(유지비 차감 포함) 직후 판정**하고, 나머지는 최종일에 판정한다",
        "when": "이 엔딩의 조건(when 문법). **맨 마지막 줄은 비워둔다** — 아무 조건도 못 맞췄을 때의 안전망(엔딩이 안 뜨는 사고 방지)",
        "scene_id": "재생할 엔딩 씬 id",
        "note": "어떤 결말인지 메모",
    },
    "Config": {
        "key": "설정 키. 게임 전역에서 쓰는 상수 이름",
        "value": "값. 이 숫자를 바꾸면 게임 전체 밸런스가 바뀐다",
        "type": "값의 자료형(int/float/str/bool). 새 키를 추가하면 꼭 채울 것 — 엑셀이 3.0을 3으로 바꿔버리는데, float라고 적어두면 빌드가 되돌린다",
        "note": "이 설정이 무엇을 조절하는지 설명",
    },
    "GradeCuts": {
        "grade": "등급 이름 — excellent/good/decent/poor/sewage",
        "min_pct": "이 등급을 받기 위한 최소 점수(%). **위에서부터 확인**해 처음 넘는 등급이 된다(92점이면 95 미달 → 80 이상이므로 good)",
    },
    "SettlementRules": {
        "grade": "등급 이름(GradeCuts와 짝)",
        "sale_rate": "판매가 수취 비율. 1.0=정가 다 받음, 0=못 받음(sewage)",
        "tip_rate": "팁 비율(판매가 대비). 여기에 랜덤 손님은 성격 tip_mult가 한 번 더 곱해진다(단골은 일괄 1.0). 0이면 팁 없음",
        "refund_rate": "배상 비율(판매가 대비). sewage만 1.0 — 주문 불일치도 등급이 sewage로 강제되므로 이 줄을 탄다",
        "note": "작업 메모",
    },
    "ScoreBands": {
        "band_type": "quantity(수량 오차 → 그 기믹 점수) / overtime(제한시간 초과 비율 → 전체 감점)",
        "min_ratio": "구간 하한(비율). 위에서부터 첫 일치 구간이 적용된다",
        "max_ratio": "구간 상한(비율). 비우면 무한대",
        "min_inclusive": "하한 포함 여부(TRUE=이상, FALSE=초과) — 경계값 모호성 제거용",
        "max_inclusive": "상한 포함 여부(TRUE=이하, FALSE=미만)",
        "score_or_penalty": "quantity면 그 기믹의 점수(0~100), overtime이면 전체 점수에서 뺄 감점",
        "note": "작업 메모",
    },
    "AffinityMatrix": {
        "taste_tier": "손님의 취향 등급(Tastes의 tier) — love/good/ok/dislike, 그리고 miss(주문과 다른 술을 냈을 때)",
        "excellent": "그 취향의 술을 excellent로 만들어 줬을 때의 호감도 변화",
        "good": "good 등급일 때의 호감도 변화",
        "decent": "decent 등급일 때의 호감도 변화",
        "poor": "poor 등급일 때의 호감도 변화(보통 음수)",
        "sewage": "sewage 등급일 때의 호감도 변화(가장 큰 감점)",
    },
    "UIStrings": {
        "key": "코드에서 부르는 키 이름. 바꾸면 화면에 키 이름이 그대로 노출되니 변경 금지",
        "ko": "화면에 뜨는 문구(한국어)",
        "en": "영어 문구(비워도 됨 — ko 폴백)",
    },
}

# 시트 자체에 대한 한 줄 설명 — A1 왼쪽 위 모서리 셀에 붙는 메모 대신, 시트 최상단 안내용
SHEET_DOCS = {
    "Cocktails": "칵테일 정의. 한 줄이 칵테일 하나. 레시피 재료는 RecipeLines 시트에 따로 적는다",
    "Tags": "맛 키워드 사전. 칵테일 tags와 Tastes의 cocktail.tag(...)이 여기 적힌 한글 태그만 쓸 수 있고, 영어 표기는 정보 화면 키워드 표시에 쓰인다",
    "RecipeLines": "칵테일에 들어가는 재료 목록. 한 줄이 '재료 하나를 넣는 행동' 하나 — 이건 채점 정답표이지 실행 순서표가 아니다",
    "ShelfItems": "제조 선반에 놓이는 모든 것 — 재료·잔·도구·가니시를 kind로 구분해 한 테이블에 담는다",
    "Characters": "등장인물. affinity=TRUE인 인물만 호감도가 쌓이고 단골 수첩에 실린다",
    "Expressions": "인물별 표정 목록. 대사(Steps)의 arg 칸에 표정 이름을 적어 사용",
    "ExpressionParts": "표정을 부위별 애니로 만들 때의 파츠 구성(mode=parts_anim인 표정만)",
    "Cutscenes": "컷씬 목록. 대사의 timeline 스텝이 여기 id를 부른다",
    "ResourceMap": "데이터 키가 실제 어느 에셋인지 적는 검수 대장. 게임에 배포되지 않는 작업 관리표다",
    "FieldAnims": "거리·집·꿈에 나오는 SD 픽셀 캐릭터의 애니메이션(바 안 흉상은 Expressions가 담당)",
    "Personalities": "1부 랜덤 손님의 성격 5종. 팁과 인내심 시간이 여기서 갈린다",
    "Barks": "1부 랜덤 손님이 상황마다 던지는 대사 창고. 성격×상황으로 골라 쓴다",
    "Tastes": "손님 취향 규칙. 위에서부터 첫 일치가 이기며, 조건에 flag를 섞으면 상황에 따라 취향이 바뀐다",
    "Dossier": "단골 수첩(바 UI 탭)에 뜨는 인물 정보. 호감도가 오를수록 한 줄씩 열린다",
    "Days": "일차별 메타 정보(제목·시작 지점·배경음)",
    "RandomWaves": "1부에 이름 없는 랜덤 손님이 언제 몇 명 오는지 — 운영 페이스 튜닝은 여기서 (System 파일)",
    "RegularSlots": "단골이 언제 어느 자리에 오는지 — 카메오·분기 슬롯 (Narrative 파일). 랜덤 손님은 RandomWaves에",
    "Spots": "거리의 위치 이름표. 좌표 대신 이름으로 관리해 배경이 바뀌어도 안 깨진다",
    "InteractPoints": "거리 배치(누가 어디에 언제)와 상호작용(E키·자동 방송·장소 전환)을 한 행이 소유하는 통합 지점",
    "Transitions": "문·오브젝트를 직접 조사했을 때 실행하는 장소 전환 목적지. 자동 경계 이동은 지원하지 않는다",
    "Scenes": "대본의 씬(대사 한 덩어리). phase가 비주얼과 재생 시점을 결정한다",
    "Steps": "씬 안의 한 줄 한 줄. 대사·등장·주문·제조·선택지가 전부 여기 들어간다",
    "Choices": "선택지 묶음. Steps의 choice 타입이 choice_id로 호출한다",
    "OrderRules": "애매한 주문에 '무엇을 만들어 냈는지'로 판정하는 표. 스토리 분기가 여기서 갈린다",
    "Quests": "퀘스트 기본 정보와 보상",
    "QuestStages": "퀘스트의 단계별 목표. 마지막 단계를 끝내면 Quests의 보상이 나온다",
    "Endings": "엔딩 조건. priority 순으로 확인해 처음 맞는 엔딩으로 확정된다",
    "Config": "게임 전역 상수. 시작 골드·시간대·기믹 가중치·페널티 등",
    "BarkSituations": "1부 대사 상황 사전 + 상황별 기본 표정. Barks.situation의 정본 목록",
    "GuestBodies": "랜덤 손님 공용 외형 카탈로그 — 슬롯 9종(바디·의상·아우터·목걸이·눈·눈썹·입·헤어·팔 액세서리)을 성별 맞춰 조합. 스폰 시 성별 추첨 후 (성별×성격) 유효 조합 풀에서 weight 곱 가중 랜덤",
    "GuestBodyExclusions": "랜덤 손님 외형 금지 조합 — 한 행 = 함께 나오면 안 되는 파츠 쌍 하나. 현재 팔 액세서리×소매 상의 6행",
    "TextTags": "텍스트 연출 태그 정의 — <world> 색·<slow> 속도·<big> 크기 등. 대사에 쓴 태그는 반드시 여기 등록",
    "GradeCuts": "제조 점수(%)를 5등급으로 나누는 기준선",
    "SettlementRules": "등급별 정산 — 판매가·팁·배상 비율. 주문 불일치는 등급이 sewage로 강제된다",
    "ScoreBands": "채점 구간표 — 수량 오차(quantity)와 시간 초과(overtime)를 구간별 점수/감점으로 변환",
    "AffinityMatrix": "취향 × 등급 → 호감도 증감표. 이 게임의 감정 시스템 핵심",
    "UIStrings": "버튼·시스템 문구 모음(대사가 아닌 UI 텍스트)",
}


def add_sheet(wb, name, headers, rows):
    ws = wb.create_sheet(name)
    ws.append(headers)
    docs = COL_DOCS.get(name, {})
    sheet_doc = SHEET_DOCS.get(name, "")
    missing_doc = []
    for idx, c in enumerate(ws[1]):
        c.fill, c.font = HDR_FILL, HDR_FONT
        c.alignment = Alignment(vertical="center")
        h = headers[idx]
        # 메모 본문 — 첫 칸에는 시트 전체 설명을 함께 붙여 "이 시트가 뭔지"부터 보이게 한다
        if str(h).startswith("(파생)"):
            body = ("[자동 계산 컬럼]\n"
                    "빌드가 값을 채우는 칸이다. 손으로 고쳐도 다음 빌드에서 덮어써진다.\n"
                    "이미지 키는 id에서 파생된다 — id를 바꾸면 아트 파일명 대응도 함께 깨진다.")
            c.fill = DERIVED_FILL
        elif h in docs:
            body = docs[h]
        else:
            body = ""
            missing_doc.append(h)
        if idx == 0 and sheet_doc:
            body = f"📋 [{name} 시트]\n{sheet_doc}\n\n── 이 컬럼 ──\n{body}" if body else f"📋 [{name} 시트]\n{sheet_doc}"
        if body:
            c.comment = Comment(body + "\n\n💡 헤더에 마우스를 올리면 이 설명이 보입니다.", "LUNA 데이터 가이드")
            # 내용 길이에 맞춰 메모 상자 크기 조절 (잘려서 안 보이는 일 방지)
            lines = sum(max(1, (len(l) // 30) + 1) for l in body.split("\n")) + 3
            c.comment.width = 340
            c.comment.height = max(110, min(420, lines * 19))
    if missing_doc:
        print(f"  ⚠ {name}: 설명 없는 컬럼 {missing_doc}")
    wrap = Alignment(vertical="top", wrap_text=True)
    for ri, r in enumerate(rows):
        ws.append(["" if v is None else (v if not isinstance(v, bool) else ("TRUE" if v else "FALSE")) for v in r])
        if ri % 2 == 1:
            for c in ws[ws.max_row]:
                c.fill = STRIPE_FILL
    ws.freeze_panes = "A2"
    if rows:
        ws.auto_filter.ref = ws.dimensions
    for idx, h in enumerate(headers, 1):
        maxlen = max([len(str(h))] + [len(str(r[idx-1])) if idx-1 < len(r) and r[idx-1] is not None else 0 for r in rows])
        w = min(max(10, maxlen + 2), 60)
        ws.column_dimensions[ws.cell(1, idx).column_letter].width = w
        if w >= 34:  # 긴 텍스트 컬럼은 줄바꿈
            for row in ws.iter_rows(min_row=2, min_col=idx, max_col=idx):
                for c in row: c.alignment = wrap
    tab = TAB_COLOR.get(SHEET_GROUP.get(name, "etc"))
    if tab: ws.sheet_properties.tabColor = tab
    return ws

# ============================================================
# 저작 = 2파일(System/Narrative), 배포 = 도메인별 JSON 분할 (v2.0 확정)
# ============================================================
# 분리 축은 '수정 권한'이다. xlsx는 바이너리라 git이 머지를 못 하므로 파일 1개 = 소유자 1명.
#   LUNA_System.xlsx    — 1부 제조·운영 담당: 칵테일·선반·성격·취향·밸런스·랜덤 웨이브
#   LUNA_Narrative.xlsx — 대사·서사·연출 담당: 대본·Barks(랜덤 손님 대사 포함)·단골·퀘스트·캐스트
# 규칙: 한 시트는 정확히 한 파일에만 존재한다. 참고용으로 남의 시트를 복사해 넣지 말 것
#       (build.py가 두 파일에 같은 시트가 있으면 에러로 잡는다).
# v2.0.1: Tastes·AffinityMatrix는 Narrative로 — 호감도는 단골 전용(랜덤 손님은 호감도 변화 없음),
# 취향도 '누구의 취향인가'라는 인물 정보라 단골 관계를 설계하는 사람이 같이 만진다 (PD 확정)
WORKBOOK_OF = {
    **{s: "System" for s in [
        "Cocktails", "RecipeLines", "ShelfItems", "Personalities", "GuestBodies",
        "GuestBodyExclusions", "RandomWaves", "Config", "GradeCuts", "SettlementRules", "ScoreBands", "Tags"]},
    **{s: "Narrative" for s in [
        "Scenes", "Steps", "Choices", "Barks", "OrderRules", "Quests", "QuestStages",
        "Endings", "Dossier", "RegularSlots", "Characters", "Expressions",
        "ExpressionParts", "Cutscenes", "FieldAnims", "ResourceMap",
        "Days", "Spots", "InteractPoints", "Transitions", "UIStrings", "Tastes", "AffinityMatrix",
        "TextTags", "BarkSituations"]},
}
WORKBOOK_FILES = {"System": "LUNA_System.xlsx", "Narrative": "LUNA_Narrative.xlsx"}


def emit_xlsx(derived):
    # 1) 전 시트 데이터 구성 (name → (headers, rows))
    sheets = {
        "Cocktails": (CK_COLS + ["(파생)sprite", "(파생)serve_sprite"],
            [list(c) + [derived[c[0]]["sprite"], derived[c[0]]["serve_sprite"]] for c in COCKTAILS]),
        "Tags": (TAG_COLS + ["tag_id"], [tuple(t) + (TAG_IDS[t[0]],) for t in TAGS]),
        "RecipeLines": (["cocktail_id", "seq", "action", "ingredient_id", "qty", "unit", "is_core", "auto_apply", "scored"],
            [[cid, i + 1, l["action"], l["ingredient"], l["qty"], l["unit"],
              l["is_core"], l["auto_apply"], l["scored"]]
             for cid in RECIPES for i, l in enumerate(recipe_lines(cid))]),
        "ShelfItems": (SHELF_COLS, SHELF_ITEMS),
        "Characters": (CHAR_COLS + ["base_body"], [list(c) + [BASE_BODY.get(c[0], "")] for c in CHARACTERS]),
        "Expressions": (EXPR_COLS, EXPRESSIONS),
        "ExpressionParts": (EXPRPART_COLS, EXPRESSION_PARTS),
        "Cutscenes": (CUT_COLS, CUTSCENES),
        "ResourceMap": (RESMAP_COLS, RESOURCE_MAP),
        "FieldAnims": (FIELD_COLS, FIELD_ANIMS),
        "Personalities": (PERS_COLS, PERSONALITIES),
        "Barks": (BARK_COLS, BARKS),
        "Tastes": (TASTE_COLS, TASTES),
        "Dossier": (DOSSIER_COLS, DOSSIER),
        "Days": (DAY_COLS, DAYS),
        "RandomWaves": (WAVE_COLS, RANDOM_WAVES),
        "RegularSlots": (RSLOT_COLS, REGULAR_SLOTS),
        "Spots": (SPOT_COLS, SPOTS),
        "InteractPoints": (POINT_COLS, POINTS),
        "Transitions": (TRANSITION_COLS, TRANSITIONS),
        "Scenes": (SCENE_COLS, SCENES),
        "Steps": (STEP_COLS, STEPS),
        "Choices": (CHOICE_COLS, CHOICES),
        "OrderRules": (ORDER_COLS, ORDERS),
        "Quests": (QUEST_COLS, QUESTS),
        "QuestStages": (QSTAGE_COLS, QUEST_STAGES),
        "Endings": (END_COLS, ENDINGS),
        "Config": (CONFIG_COLS,
            [(k, v, type(v).__name__, n) for k, v, n in CONFIG]),   # type을 시트에 명시 — 신규 키도 타입 보존
        "GradeCuts": (["grade", "min_pct"], GRADE_CUTS),
        "SettlementRules": (SETTLE_COLS, SETTLEMENT_RULES),
        "ScoreBands": (SCOREBAND_COLS, SCORE_BANDS),
        "AffinityMatrix": (["taste_tier", "excellent", "good", "decent", "poor", "sewage"], AFFINITY_MATRIX),
        "UIStrings": (UI_COLS, UI_STRINGS),
        "TextTags": (TEXTTAG_COLS, TEXT_TAGS),
        "BarkSituations": (BARKSIT_COLS, BARK_SITUATIONS),
        "GuestBodies": (GBODY_COLS, GUEST_BODIES),
        "GuestBodyExclusions": (GBEXCL_COLS, GUEST_BODY_EXCLUSIONS),
    }

    # 2) 저작 2파일로 저장 (v2.0 — 시스템/내러티브 분리, 파일 1개 = 소유자 1명)
    BOOK_HEAD = {
        "System": [
            ("h1", "Project L.U.N.A — 시스템 데이터 (제조·운영)"),
            ("", "이 파일은 '게임이 어떻게 굴러가는가'를 담습니다. 칵테일·재료·손님 성격·취향 판정·밸런스·1부 랜덤 손님 페이스가 전부 여기 있습니다."),
            ("", "대사·단골·퀘스트·연출은 LUNA_Narrative.xlsx에 있습니다. 이 파일은 제조·운영 담당 한 명이 관리합니다."),
        ],
        "Narrative": [
            ("h1", "Project L.U.N.A — 내러티브 데이터 (대사·서사·연출)"),
            ("", "이 파일은 '게임이 무엇을 이야기하는가'를 담습니다. 대본·랜덤 손님 대사(Barks)·단골·퀘스트·엔딩·연출·등장인물이 전부 여기 있습니다."),
            ("", "칵테일·밸런스·랜덤 손님 페이스는 LUNA_System.xlsx에 있습니다. 이 파일은 서사 담당 한 명이 관리합니다."),
        ],
    }
    BOOK_MAP = {
        "System": [
            ("h2", "3. 시트 지도 — 무엇을 고치고 싶은가요?"),
            ("", "칵테일을 추가/수정하고 싶다        →  Cocktails + RecipeLines"),
            ("", "재료·잔·도구·가니시를 손보고 싶다  →  ShelfItems"),
            ("", "랜덤 손님이 언제 몇 명 오는지      →  RandomWaves (단골 방문은 Narrative의 RegularSlots)"),
            ("", "손님 성격(팁·인내 배율)            →  Personalities (대사는 Narrative의 Barks)"),
            ("", "난이도·보상을 조절하고 싶다        →  Config + GradeCuts + SettlementRules + ScoreBands"),
            ("", ""),
            ("h2", "3-1. 옆 파일과의 약속"),
            ("", "• Personalities에 성격을 추가하면 → Narrative 담당에게 그 성격의 Barks 대사를 요청하세요. 없으면 손님이 침묵합니다(빌드가 경고)."),
            ("", "• Cocktails의 flavor(맛 설명)·ShelfItems의 desc는 '글'입니다 — 문구는 서사 담당에게 받아서 붙여넣으세요."),
            ("", "• 취향(Tastes)·호감도표(AffinityMatrix)는 단골 전용이라 Narrative에 있습니다 — 랜덤 손님은 호감도가 없습니다."),
        ],
        "Narrative": [
            ("h2", "3. 시트 지도 — 무엇을 고치고 싶은가요?"),
            ("", "대사를 쓰거나 고치고 싶다          →  Scenes(씬 정의) + Steps(대사 한 줄씩) + Choices(선택지)"),
            ("", "랜덤 손님 대사를 쓰고 싶다         →  Barks (성격·팁 배율은 System의 Personalities)"),
            ("", "단골이 언제 오는지 바꾸고 싶다     →  RegularSlots (랜덤 손님 페이스는 System의 RandomWaves)"),
            ("", "단골 수첩 문구를 쓰고 싶다         →  Dossier"),
            ("", "단골 취향·호감도 보상을 정하고 싶다 →  Tastes(판정 규칙) + AffinityMatrix(취향×등급→호감도)"),
            ("", "퀘스트를 만들고 싶다               →  Quests + QuestStages"),
            ("", "단골 주문 판정·연출 이벤트         →  OrderRules + Cutscenes"),
            ("", "거리에 캐릭터·사물을 놓고 싶다     →  Spots(위치) + FieldEntities(배치)"),
            ("", "대화·조사·문 입장을 연결하고 싶다  →  InteractPoints(행동) + Transitions(수동 장소 전환)"),
            ("", "등장인물·표정을 관리하고 싶다      →  Characters + Expressions + ExpressionParts"),
            ("", ""),
            ("h2", "3-1. 옆 파일과의 약속"),
            ("", "• Barks의 voice_id는 System의 Personalities id를 가리킵니다 — 성격이 새로 생기면 그 성격의 대사도 여기서 추가."),
            ("", "• RegularSlots의 seq는 System의 RandomWaves와 '하루 공용 번호'입니다. 같은 날 같은 번호를 쓰면 빌드 에러."),
        ],
    }
    INFO_COMMON = [
        ("", ""),
        ("h2", "1. 이 파일을 어떻게 쓰나요?"),
        ("", "① 아래 시트 탭에서 고칠 곳을 찾아 값을 수정합니다."),
        ("", "② 터미널에서 빌드를 돌립니다:   python3 데이터/tools/build.py"),
        ("", "③ 빌드가 검사에 통과하면 게임용 JSON이 자동으로 만들어집니다."),
        ("", "   ※ 검사에 걸리면 JSON을 만들지 않고 무엇이 틀렸는지 알려줍니다 — 고장난 데이터가 게임에 못 들어갑니다."),
        ("", ""),
        ("h2", "2. 처음이라면 이것만 기억하세요"),
        ("", "• 헤더(맨 윗줄)에 마우스를 올리면 그 컬럼이 무엇인지 설명이 뜹니다. 맨 왼쪽 칸에는 시트 전체 설명이 있습니다."),
        ("", "• 회색 헤더 = (파생) 컬럼. 빌드가 계산해서 채우는 칸이니 손대지 마세요."),
        ("", "• id 칸은 컴퓨터용 이름표입니다. 다른 시트가 이 값으로 서로를 가리키니 함부로 바꾸면 연결이 끊깁니다."),
        ("", "• 영어 칸(_en)은 8월 번역 전까지 비워둬도 됩니다. 비우면 한국어가 대신 나갑니다."),
        ("", "• 확신이 안 서면 그냥 두고 PD에게 물어보세요. 지우는 것보다 물어보는 게 쌉니다."),
        ("", ""),
    ]
    INFO_TAIL = [
        ("", ""),
        ("h2", "4. 꼭 지켜야 하는 규칙 5개"),
        ("", "규칙 1.  System·Narrative 두 엑셀이 원본입니다. json 폴더는 빌드 결과물이라 직접 고치면 다음 빌드에 사라집니다."),
        ("", "규칙 2.  (파생) 회색 컬럼은 빌드가 계산합니다 — 손으로 고치지 마세요."),
        ("", "규칙 3.  id는 영문 소문자+밑줄(snake_case). 한국어 이름은 name_ko 칸에 씁니다."),
        ("", "규칙 4.  제조·서빙·선택지 '직전' 스텝에 루나 대사를 넣으면 빌드 에러입니다."),
        ("", "         → 그 자리는 플레이어가 행동할 차례라, 행동 유도는 상대방 대사로 해야 합니다."),
        ("", "규칙 5.  when(조건)에는 '또는(OR)'이 없습니다. 필요하면 줄을 두 개로 나누세요."),
        ("", ""),
        ("h2", "5. 조건문(when)과 효과문(effects) 빠른 참고"),
        ("", "[when — 언제 일어나나]      day == 2   |   affinity.chris >= 50   |   flag.퀘스트완료"),
        ("", "                            cocktail.abv >= 20   |   cocktail.tag(달콤한)   |   alive.samho"),
        ("", "                            여러 조건은 && 로 연결:  flag.x && cocktail.abv >= 20"),
        ("", "[effects — 무엇이 바뀌나]   affinity.chris += 2   |   money += 50   |   flag.이름 = true"),
        ("", "                            unlock_recipe(칵테일id)   |   give(재료id, 개수)   |   alive.samho = false"),
        ("", "                            여러 개는 ; 로 연결:  affinity.samho += 10; flag.done = true"),
        ("", ""),
        ("h2", "6. 일차 넘버링"),
        ("", "1 = 튜토리얼(구 day0) · 2 = 구 day1 · … · 13 = 마지막 날. 바 2부 대사는 구엔진 실대본을 이식한 것입니다."),
        ("", ""),
        ("h2", "7. 탭 색상"),
        ("", "🔵 파랑 = 마스터(게임의 사전)   🟢 초록 = 스케줄(하루 진행)   🟣 보라 = 대본   🟠 주황 = 밸런스   ⚪ 회색 = 기타"),
        ("", ""),
        ("", "더 자세한 설명은 산출물/LUNA_JSON_데이터_설명서 문서를 보세요. 스키마 정본은 LUNA_데이터구조_설계서입니다."),
    ]
    for book, fname in WORKBOOK_FILES.items():
        wb = Workbook(); wb.remove(wb.active)
        info = wb.create_sheet("INFO")
        for kind, text in BOOK_HEAD[book] + INFO_COMMON + BOOK_MAP[book] + INFO_TAIL:
            info.append([text])
            c = info.cell(row=info.max_row, column=1)
            if kind == "h1":   # 파일 정체성이 한눈에 갈리도록 표지색을 다르게 (System=녹색, Narrative=보라)
                c.font = Font(bold=True, size=16, color="FFFFFF")
                c.fill = PatternFill("solid", fgColor="2E5A46" if book == "System" else "4A3F68")
            elif kind == "h2":
                c.font = Font(bold=True, size=12, color="2C2450")
                c.fill = PatternFill("solid", fgColor="DCEFE4" if book == "System" else "E8E2FF")
            else:
                c.alignment = Alignment(vertical="center")
        info.column_dimensions["A"].width = 118
        info.sheet_properties.tabColor = "FFD966"
        info.sheet_view.showGridLines = False
        for name, (headers, rows) in sheets.items():
            if WORKBOOK_OF[name] == book:
                add_sheet(wb, name, headers, rows)
        wb.save(os.path.join(OUT, fname))
    # 구 통짜 파일은 보관 폴더로 이동 — 저작 원본이 둘로 갈렸는데 남아 있으면 옛 파일을 고치는 사고가 난다
    old = os.path.join(OUT, "LUNA_Data.xlsx")
    if os.path.exists(old):
        arch = os.path.join(OUT, "_archive"); os.makedirs(arch, exist_ok=True)
        os.replace(old, os.path.join(arch, "LUNA_Data_pre_split.xlsx"))
        print("  · 구 통짜 LUNA_Data.xlsx → _archive/LUNA_Data_pre_split.xlsx 이동")


# ============================================================
# 출력 — JSON (로컬라이즈 필드는 {ko, en} 객체로)
# ============================================================
def L(ko, en):
    # en 미번역이면 ko로 폴백 — EN 모드에서도 빈 텍스트는 안 뜬다 (번역 완료 후 자연 치환)
    return {"ko": ko or "", "en": (en or ko) or ""}


def build_street_runtime_contract():
    """거리 저작 데이터와 집 핵심 상호작용을 공용 InteractPoints JSON으로 정규화한다.

    저작 엑셀은 그대로 둔다. 배포 JSON에서 씬의 day/seq/when을
    interact_points.dialogue_flows로 옮기고, 상태 변경은 set_state 지문
    스텝으로 분리하며 선택지는 해당 대사 스텝 안에 포함한다.
    """
    street_phases = {"street", "commute_in", "commute_out"}
    scene_by_id = {
        d["id"]: d for row in SCENES
        if (d := dict(zip(SCENE_COLS, row)))["phase"] in street_phases
    }
    group_members = {}
    for d in scene_by_id.values():
        if d["group"]:
            group_members.setdefault(d["group"], []).append(d)
    for members in group_members.values():
        members.sort(key=lambda d: (d["seq"], d["id"]))

    # 일반적인 대화 진행은 play_type과 flow_seq가 담당한다. 아래 조건은 구 구조에서
    # "앞 대사를 보았는가"를 판별하던 진행용 플래그이므로 배포 계약에서는 제거하고,
    # 실제 콘텐츠 조건인 일차 조건만 남긴다. 플래그 자체는 set_state 예시·다른 시스템
    # 참조를 위해 대본에 유지할 수 있지만 다음 대사를 고르는 용도로 중복 사용하지 않는다.
    flow_when_overrides = {
        "np_shiba_1": None,
        "np_shiba_3": "day >= 2",
        "qa_sequence_1": None,
        "qa_sequence_2": None,
    }

    def flow_of(scene, play_type):
        return {
            "scene_id": scene["id"],
            "day": scene["day"],
            "flow_seq": scene["seq"],
            "play_type": play_type,
            "when": flow_when_overrides.get(scene["id"], scene["when"] or None),
        }

    points = []
    for row in POINTS:
        d = dict(zip(POINT_COLS, row))
        for key in ("facing", "spawn_when", "interact_when", "action_type", "action_ref"):
            d[key] = d[key] or None
        action_type = d["action_type"]
        action_ref = d["action_ref"]
        if action_type in ("scene", "scene_group"):
            if action_type == "scene":
                members = [scene_by_id[action_ref]] if action_ref in scene_by_id else []
                flows = [flow_of(s, "once" if s["on_complete_effects"] else "repeat") for s in members]
            else:
                members = group_members.get(action_ref, [])
                last_unconditional = next((s["id"] for s in reversed(members) if not s["when"]), None)
                flows = [flow_of(s, "repeat" if s["id"] == last_unconditional else "once") for s in members]
            d["action_type"] = "dialogue"
            d.pop("action_ref", None)
            d["dialogue_flows"] = flows
            if d["id"] == "p_shiba":
                d["note"] = "flow_seq 오름차순으로 실행 가능한 첫 대화를 선택하고 once 완료 후 다음 대화로 진행"
            elif d["id"] == "p_qa_sequence":
                d["note"] = "play_type once/repeat와 flow_seq로 1→2→3단계 진행·마지막 대사 반복"
        elif action_type is None:
            d.pop("action_ref", None)
        points.append(d)

    # 집과 외부는 기존 InteractPoint 행 구조를 함께 사용한다. 장소는 spot_id가
    # 참조하는 Spots.area로 구분하며, 집 상황과 입력 방식은 각 엔진 명령이 판단한다.
    # 엘리베이터도 층 ID를 복제하지 않고 현재 위치를 아는 명령 하나만 호출한다.
    points.extend([
        {
            "id": "p_elevator_move", "kind": "object", "source_id": "street_elevator",
            "spot_id": "elevator", "facing": None, "phase": "both", "spawn_when": None,
            "activation_mode": "interact", "interact_when": None, "priority": 0,
            "action_type": "system", "action_ref": "elevator_toggle",
            "note": "상호작용 시 엘리베이터가 현재 위치를 판단해 반대 방향으로 이동",
        },
        {
            "id": "p_home_exit", "kind": "object", "source_id": "home_exit_door",
            "spot_id": "home_exit_door", "facing": None, "phase": "home", "spawn_when": None,
            "activation_mode": "interact", "interact_when": None, "priority": 0,
            "action_type": "transition", "action_ref": "exit_home",
            "note": "집 현관 상호작용. 현재 home_context의 출입 허용·차단은 HomeController가 판단",
        },
        {
            "id": "p_home_sofa", "kind": "object", "source_id": "home_sofa",
            "spot_id": "home_sofa", "facing": None, "phase": "home", "spawn_when": None,
            "activation_mode": "interact", "interact_when": None, "priority": 0,
            "action_type": "system", "action_ref": "home_sofa_interaction",
            "note": "현재 home_context에 맞춰 수동 저장 또는 저장·취침 선택을 HomeController가 제공",
        },
    ])

    # 거리 JSON의 sync는 공용 Steps 저작 필드와 의미가 다르다.
    # say에서만 다음 대사 진행 방식을 나타내며, proximity 방송은 auto,
    # E키로 시작하는 대화는 player_input을 명시한다. 다른 스텝 타입에는
    # sync를 출력하지 않는다.
    auto_scene_ids = {
        flow["scene_id"]
        for point in points
        if point.get("action_type") == "dialogue"
        and point.get("activation_mode") == "proximity"
        for flow in point.get("dialogue_flows", [])
    }

    choices_by_id = {}
    for row in CHOICES:
        d = dict(zip(CHOICE_COLS, row))
        choices_by_id.setdefault(d["choice_id"], []).append(d)
    for rows in choices_by_id.values():
        rows.sort(key=lambda d: d["seq"])

    def output_steps(scene):
        out = []

        def append_step(payload):
            payload["seq"] = len(out) + 1
            out.append(payload)

        for row in sorted((r for r in STEPS if r[0] == scene["id"]), key=lambda r: r[1]):
            d = dict(zip(STEP_COLS, row))
            common = {
                "type": d["type"],
                "actor": d["actor"] or None,
                "dialogue_id": d["dialogue_id"] or None,
                "arg": d["arg"] or None,
                "text": L(d["text_ko"], d["text_en"]) if d["text_ko"] else None,
                "when": d["when"] or None,
            }
            if d["type"] == "say":
                common["sync"] = "auto" if scene["id"] in auto_scene_ids else "player_input"
            if d["type"] == "choice":
                options = []
                for option in choices_by_id.get(d["arg"], []):
                    result_steps = []
                    if option["effects"]:
                        result_steps.append({"type": "set_state", "effects": option["effects"]})
                    if option["goto"]:
                        result_steps.append({"type": "goto", "scene_id": option["goto"]})
                    options.append({
                        "id": f"{d['arg']}_{option['seq']}",
                        "seq": option["seq"],
                        "text": L(option["text_ko"], option["text_en"]),
                        "when": option["when"] or None,
                        "lock_reason": L(option["lock_reason_ko"], option["lock_reason_en"])
                                       if option["when"] else None,
                        "result_steps": result_steps,
                    })
                common.pop("arg", None)
                common["options"] = options
                append_step(common)
            elif d["type"] == "effect":
                append_step({
                    "type": "set_state",
                    "effects": d["effects"],
                    "when": d["when"] or None,
                })
            elif d["type"] == "goto":
                # 대사 중 조건에 따른 자동 분기는 when이 붙은 goto 지문으로 표현한다.
                # 현재 운영 데이터에는 없지만 새 거리 계약에서 정식으로 허용한다.
                append_step({
                    "type": "goto",
                    "scene_id": d["arg"] or None,
                    "when": d["when"] or None,
                })
            else:
                append_step(common)
                if d["effects"]:
                    append_step({
                        "type": "set_state",
                        "effects": d["effects"],
                        "when": d["when"] or None,
                    })
        if scene["on_complete_effects"]:
            append_step({
                "type": "set_state",
                "effects": scene["on_complete_effects"],
                "when": None,
            })
        return out

    scenes = [{"id": d["id"], "steps": output_steps(d)} for d in scene_by_id.values()]
    scenes.sort(key=lambda d: d["id"])
    prod_points = [d for d in points if not d["id"].startswith("p_qa_")]
    qa_points = [d for d in points if d["id"].startswith("p_qa_")]
    prod_scenes = [d for d in scenes if scene_by_id[d["id"]]["day"] != 99]
    qa_scenes = [d for d in scenes if scene_by_id[d["id"]]["day"] == 99]
    return {
        "prod_points": prod_points,
        "qa_points": qa_points,
        "prod_script": {"place": "street", "scenes": prod_scenes},
        "qa_script": {"place": "street", "scenes": qa_scenes},
    }


def validate_street_runtime_contract(runtime):
    errors = []
    point_ids = set()
    spot_ids = {row[0] for row in SPOTS}
    transition_ids = {row[0] for row in TRANSITIONS}
    system_actions = {"elevator_toggle", "home_sofa_interaction"}
    scene_ids = {
        scene["id"]
        for key in ("prod_script", "qa_script")
        for scene in runtime[key]["scenes"]
    }
    scene_advance_modes = {}
    for key in ("prod_points", "qa_points"):
        for point in runtime[key]:
            if point["id"] in point_ids:
                errors.append(f"[필드] point id {point['id']} 중복")
            point_ids.add(point["id"])
            if point.get("spot_id") not in spot_ids:
                errors.append(f"[필드] {point['id']}: spot_id {point.get('spot_id')} 없음")
            action_type = point.get("action_type")
            action_ref = point.get("action_ref")
            if action_type not in ("dialogue", "transition", "system", None):
                errors.append(f"[필드] {point['id']}: action_type {action_type} 불가")
            if action_type == "transition" and action_ref not in transition_ids:
                errors.append(f"[필드] {point['id']}: transition {action_ref} 없음")
            if action_type == "system" and action_ref not in system_actions:
                errors.append(f"[필드] {point['id']}: system action {action_ref} 없음")
            if action_type is None and action_ref:
                errors.append(f"[필드] {point['id']}: action_type 없이 action_ref 사용 불가")
            if point.get("action_type") == "dialogue":
                flows = point.get("dialogue_flows") or []
                if not flows:
                    errors.append(f"[거리] {point['id']}: dialogue인데 dialogue_flows가 비어 있음")
                seen_seq = set()
                for flow in flows:
                    if flow["scene_id"] not in scene_ids:
                        errors.append(f"[거리] {point['id']}: scene_id {flow['scene_id']} 없음")
                    if flow["play_type"] not in ("once", "repeat"):
                        errors.append(f"[거리] {point['id']}: play_type {flow['play_type']} 불가")
                    if flow["flow_seq"] in seen_seq:
                        errors.append(f"[거리] {point['id']}: flow_seq {flow['flow_seq']} 중복")
                    seen_seq.add(flow["flow_seq"])
                    expected_sync = "auto" if point.get("activation_mode") == "proximity" else "player_input"
                    previous_sync = scene_advance_modes.setdefault(flow["scene_id"], expected_sync)
                    if previous_sync != expected_sync:
                        errors.append(
                            f"[거리] {flow['scene_id']}: 서로 다른 진행 방식({previous_sync}/{expected_sync})의 지점이 같은 씬을 참조")
            elif "dialogue_flows" in point:
                errors.append(f"[거리] {point['id']}: dialogue가 아닌데 dialogue_flows가 있음")
    for key in ("prod_script", "qa_script"):
        for scene in runtime[key]["scenes"]:
            if set(scene) != {"id", "steps"}:
                errors.append(f"[거리] {scene['id']}: street 씬에는 id와 steps만 허용")
            seen_seq = set()
            for step in scene["steps"]:
                if step["seq"] in seen_seq:
                    errors.append(f"[거리] {scene['id']}: step seq {step['seq']} 중복")
                seen_seq.add(step["seq"])
                if step["type"] == "set_state":
                    if not step.get("effects"):
                        errors.append(f"[거리] {scene['id']}#{step['seq']}: set_state effects 누락")
                elif step["type"] == "goto":
                    if not step.get("scene_id"):
                        errors.append(f"[거리] {scene['id']}#{step['seq']}: goto scene_id 누락")
                    elif step["scene_id"] not in scene_ids:
                        errors.append(
                            f"[거리] {scene['id']}#{step['seq']}: goto {step['scene_id']} 없음")
                elif "effects" in step:
                    errors.append(f"[거리] {scene['id']}#{step['seq']}: 상태 변경은 set_state만 허용")
                if step["type"] == "say":
                    if step.get("sync") not in ("auto", "player_input"):
                        errors.append(
                            f"[거리] {scene['id']}#{step['seq']}: say.sync는 auto/player_input만 허용")
                    expected_sync = scene_advance_modes.get(scene["id"])
                    if expected_sync and step.get("sync") != expected_sync:
                        errors.append(
                            f"[거리] {scene['id']}#{step['seq']}: 지점 activation_mode에 따른 sync는 {expected_sync}여야 함")
                elif "sync" in step:
                    errors.append(
                        f"[거리] {scene['id']}#{step['seq']}: sync는 say 스텝에서만 허용")
                if step["type"] == "choice":
                    if not step.get("options"):
                        errors.append(f"[거리] {scene['id']}#{step['seq']}: choice options 누락")
                    for option in step.get("options", []):
                        for result in option["result_steps"]:
                            if result["type"] == "goto" and result["scene_id"] not in scene_ids:
                                errors.append(
                                    f"[거리] {scene['id']}#{step['seq']} {option['id']}: "
                                    f"goto {result['scene_id']} 없음")
                            elif result["type"] not in ("set_state", "goto"):
                                errors.append(
                                    f"[거리] {scene['id']}#{step['seq']} {option['id']}: "
                                    f"result type {result['type']} 불가")
    return errors

def emit_json(derived):
    jdir = os.path.join(OUT, "json"); os.makedirs(os.path.join(jdir, "script", "bar"), exist_ok=True)
    os.makedirs(os.path.join(jdir, "script", "qa"), exist_ok=True)
    def dump(name, obj):
        path = os.path.join(jdir, name)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as f:
            json.dump(obj, f, ensure_ascii=False, indent=2)

    master = {"cocktails": [], "shelf_items": [], "characters": [],
              "personalities": [], "barks": [],
              "ui_strings": {k: L(ko, en) for k, ko, en in UI_STRINGS}}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        master["cocktails"].append({
            "id": d["id"], "name": L(d["name_ko"], d["name_en"]), "status": d["status"],
            "price": d["price"], "abv": d["abv"],
            "glass": d["glass"],
            # 신규 코드는 target_*을 사용한다. mix/prep은 기존 로더 호환을 위해 한 버전 유지한다.
            "target_mix_method": d["mix"],
            "target_prep_action": "open" if d["prep"] == "cap" else None,
            "mix": d["mix"], "prep": d["prep"] or None, "garnish": d["garnish"],
            "color": d["color"], "color2": d["color2"] or None,
            "mixing_ice": d["mixing_ice"], "serving_ice": d["serving_ice"],
            "tags": [{"id": TAG_IDS[t], "ko": t, "en": TAG_EN[t], "category": TAG_CATEGORY[t]}
                     for t in d["tags"].split(";")],
            "flavor": L(d["flavor_ko"], d["flavor_en"]),
            "recipe_desc": L(d["recipe_desc_ko"], d["recipe_desc_en"]),
            "unlock_day": d["unlock_day"], "unlock_when": d["unlock_when"] or None,
            "time_limit_sec": d["time_limit_sec"],
            "recipe": recipe_lines(d["id"]),
            **derived[d["id"]],
        })
    # v1.9: 재료·잔·도구·가니시 통합 — kind가 어느 선반 화면에 놓일지를 결정
    for d in shelf_dicts():
        master["shelf_items"].append({
            "id": d["id"], "kind": d["kind"], "name": L(d["name_ko"], d["name_en"]),
            "category": d["category"] or None, "color": d["color"], "sprite": d["sprite"] or None,
            "unlock_day": d["unlock_day"], "unlock_when": d["unlock_when"] or None,
            "shop_price": d["shop_price"], "desc": L(d["desc_ko"], d["desc_en"]),
            "default_action": d["default_action"] or None,
            "prep_action": d["prep_action"] or None,
            "default_target_qty": d["default_target_qty"],
            "default_target_unit": d["default_target_unit"] or None,
            "shelf_group": d["shelf_group"] or None,
            "liquid_alpha": d["liquid_alpha"]})
    for c in CHARACTERS:
        d = dict(zip(CHAR_COLS, c))
        master["characters"].append({"id": d["id"], "name": L(d["name_ko"], d["name_en"]),
            "name_color": d["name_color"], "role": d["role"], "affinity": d["affinity"],
            "alive_flag": d["alive_flag"], "expressions": d["expressions"].split(";"),
            "base_body": BASE_BODY.get(d["id"]),
            "enter_sfx": d["enter_sfx"], "exit_sfx": d["exit_sfx"]})
    # 표정 시스템 — {캐릭터: {표정: {mode, sprite, talk_anim, parts}}}
    master["character_anim"] = {}   # 배포명 = 플머 구현(character_anim.json) 정합. 저작 시트명은 Expressions 유지
    for e in EXPRESSIONS:
        d = dict(zip(EXPR_COLS, e))
        parts = {p[2]: {"clip": p[3], "loop": p[4]}
                 for p in EXPRESSION_PARTS if p[0] == d["character_id"] and p[1] == d["expression"]}
        talk = any(v["loop"] in ("always_on_dialogue", "on_dialogue") and k == "lower_face"
                   for k, v in parts.items())
        master["character_anim"].setdefault(d["character_id"], {})[d["expression"]] = {
            "mode": d["mode"], "sprite": d["sprite_key"] or None,
            "talk_anim": talk, "parts": parts or None,
        }
    master["cutscenes"] = [dict(zip(CUT_COLS, c)) for c in CUTSCENES]
    master["field_anims"] = [dict(zip(FIELD_COLS, f)) for f in FIELD_ANIMS]
    for p in PERSONALITIES:
        d = dict(zip(PERS_COLS, p))
        master["personalities"].append({"id": d["id"], "name": L(d["name_ko"], d["name_en"]),
            "tip_mult": d["tip_mult"], "patience_mult": d["patience_mult"],
            "think_chance": d["think_chance"]})
    for b in BARKS:
        d = dict(zip(BARK_COLS, b))
        master["barks"].append({"voice_id": d["voice_id"] or None, "expression": d["expression"] or None,
            "situation": d["situation"], "text": L(d["text_ko"], d["text_en"]), "weight": d["weight"]})
    master["tastes"] = [dict(zip(TASTE_COLS, t)) for t in TASTES]

    # 도메인별 분할 배포 (v1.8 확정 — 구엔진처럼 파일 단위) : 저작은 통짜 xlsx, 배포는 잘게.
    # 엔진 로딩 목록 = 아래 파일들 + balance/schedule/quests/endings/orders + script/*.json
    # ResourceMap은 에셋 검수 대장(작업 관리표)이라 런타임이 쓰지 않는다 → 시트에만 두고 배포 제외 (v1.9)
    master["dossier"] = [
        {"character_id": r[0], "min_affinity": r[1], "kind": r[2], "when": r[3] or None,
         "text": L(r[4], r[5])} for r in DOSSIER]
    MASTER_FILES = ["cocktails", "shelf_items", "characters", "character_anim", "dossier",
                    "cutscenes", "field_anims", "personalities", "barks", "tastes", "ui_strings"]
    # 스키마 개편으로 폐지된 배포 파일 정리 — 남아 있으면 엔진이 구 데이터를 읽을 수 있다 (v1.9)
    for gone in ("ingredients.json", "items.json", "resource_map.json", "master.json", "schedule.json"):
        gp = os.path.join(jdir, gone)
        if os.path.exists(gp):
            os.remove(gp); print(f"  · 폐지 파일 삭제: json/{gone}")
    for k in MASTER_FILES:
        dump(f"{k}.json", master[k])
    stale = os.path.join(jdir, "master.json")   # 구 통합 master.json 잔존 방지
    if os.path.exists(stale): os.remove(stale)

    # 도메인 단위 분할 원칙: "항상 같이 로드·같이 튜닝·혼자서는 의미 없음"인 것만 묶는다.
    # → 밸런스 계수 4종은 balance.json 하나(전부 튜닝 계수, 합쳐도 2KB), 퀘스트 스테이지는 quests.json 안에.
    dump("balance.json", {
        "config": {k: v for k, v, _ in CONFIG},
        "grade_cuts": [dict(zip(["grade","min_pct"], g)) for g in GRADE_CUTS],
        "settlement_rules": {g: {"sale_rate": s, "tip_rate": t, "refund_rate": r}
                             for g, s, t, r, _ in SETTLEMENT_RULES},
        "score_bands": [dict(zip(SCOREBAND_COLS[:-1], b[:-1])) for b in SCORE_BANDS],
        "affinity_matrix": [dict(zip(["taste_tier","excellent","good","decent","poor","sewage"], a)) for a in AFFINITY_MATRIX],
    })
    dump("days.json", [{**{k: v for k, v in zip(DAY_COLS, d) if k not in ("label_ko", "label_en")}, "label": L(d[1], d[2])} for d in DAYS])
    # v2.0: 구 guest_slots를 시스템(랜덤 웨이브)/서사(단골 슬롯)로 분리 배포 — 런타임은 둘을 seq로 병합
    dump("random_waves.json", [dict(zip(WAVE_COLS, g)) for g in RANDOM_WAVES])
    dump("regular_slots.json", [dict(zip(RSLOT_COLS, g)) for g in REGULAR_SLOTS])
    dump("spots.json", [dict(zip(SPOT_COLS, s)) for s in SPOTS])
    # v2.7.0 공용 필드 런타임 계약 — 집·외부의 배치와 상호작용은 InteractPoints가 소유하고,
    # street 대본은 대사·선택지·상태 변경 스텝만 소유한다. 엑셀 원본은 변경하지 않고
    # 배포 단계에서 정규화하여 기존 저작 시트와 새 런타임 계약을 함께 유지한다.
    _street_runtime = build_street_runtime_contract()
    _transition_rows = [dict(zip(TRANSITION_COLS, t)) for t in TRANSITIONS]
    for d in _transition_rows:
        d["target_spot"] = d["target_spot"] or None
    dump("interact_points.json", _street_runtime["prod_points"])
    if _street_runtime["qa_points"]:
        dump("qa/interact_points_day99.json", _street_runtime["qa_points"])
    dump("transitions.json", _transition_rows)
    # Day 99 거리 QA도 배치·상호작용을 한 파일이 소유한다.
    _stale_qa_entities = os.path.join(jdir, "qa", "field_entities_day99.json")
    if os.path.exists(_stale_qa_entities):
        os.remove(_stale_qa_entities)
        print("  · 폐지 파일 삭제: json/qa/field_entities_day99.json")
    dump("quests.json", {
        "quests": [dict(id=q[0], title=L(q[1], q[2]), kind=q[3], reward_effects=q[4], note=q[5]) for q in QUESTS],
        "stages": [dict(zip(QSTAGE_COLS, s)) for s in QUEST_STAGES],
    })
    dump("endings.json", [dict(zip(END_COLS, e)) for e in ENDINGS])
    dump("order_rules.json", [dict(zip(ORDER_COLS, o)) for o in ORDERS])
    dump("text_tags.json", {t: {"kind": k, "value": v} for t, k, v, _ in TEXT_TAGS})
    dump("bark_situations.json", {s: {"default_expression": e} for s, e, _ in BARK_SITUATIONS})
    # 랜덤 손님 조합형 외형 — 슬롯별 그룹으로 배포(GB_SLOT_ORDER 순). parts=null은 애니 전환용 자리.
    # v3.3 — defaults(성별별 기본 조합: is_default 마킹에서 생성, 선택 슬롯 null) + exclusions(금지 쌍, note 제외).
    #   is_default·status·note는 시트 전용 — json에 배포하지 않는다.
    gb = {GB_JSON_KEY[p]: [] for p in GB_SLOT_ORDER}
    for r in GUEST_BODIES:
        d = dict(zip(GBODY_COLS, r))
        emo, _ = parse_emotions(d["emotions"])   # 표정별 교체 스프라이트(v2.9). 빈 dict = 표정 고정
        gb[GB_JSON_KEY[d["part"]]].append({"id": d["id"], "gender": d["gender"],
                                       "personalities": [x.strip() for x in str(d["personalities"] or "").split(";") if x.strip()],
                                       "mode": d["mode"], "sprite": d["sprite"] or None, "emotions": emo or None,
                                       "parts": None, "weight": d["weight"]})
    _defs = {}
    for r in GUEST_BODIES:
        d = dict(zip(GBODY_COLS, r))
        if d.get("is_default") and d["part"] in GB_REQUIRED:
            _defs.setdefault(d["gender"], {})[d["part"]] = d["id"]
    gb["defaults"] = {g: {**{p: _defs.get(g, {}).get(p) for p in GB_REQUIRED},
                          **{p: None for p in GB_OPTIONAL}} for g in ("m", "f")}
    gb["exclusions"] = [{"a": a, "b": b} for a, b in sorted({tuple(sorted((e[0], e[1]))) for e in GUEST_BODY_EXCLUSIONS})]
    dump("guest_bodies.json", gb)
    for old in ("schedule.json", "config.json", "grade_cuts.json", "tip_rates.json",
                "affinity_matrix.json", "quest_stages.json", "guest_slots.json", "orders.json", "points.json",
                "field_entities.json"):   # 구/과분할 파일 잔존 방지
        p = os.path.join(jdir, old)
        if os.path.exists(p): os.remove(p)

    # v2.6 대본 배포 = 장소별 분리 + bar만 일차별 재분할 (PD 확정).
    #   home/street/cutscene = 통짜(반복물·소량) / bar = bar/dayN.json (12~13일차면 텍스트 본체가 수천 스텝 — 일차 단위 관리).
    #   런타임: 시작 시 home·street·cutscene 로드, 일차 진입 시 bar/dayN 로드 → (day, phase, seq)로 조회.
    PLACE_OF = {"bar_open": "bar", "bar": "bar", "home": "home",
                "commute_in": "street", "commute_out": "street", "street": "street",
                "intro": "cutscene", "dream": "cutscene", "ending": "cutscene"}
    PHASE_RANK = {"intro": 0, "home": 1, "commute_in": 2, "bar_open": 3, "bar": 4,
                  "commute_out": 5, "dream": 6, "street": 7, "ending": 8}
    place_members = {}
    for x in SCENES:
        place_members.setdefault(PLACE_OF.get(dict(zip(SCENE_COLS, x))["phase"], "cutscene"), []).append(x)
    # 바 장면이 없는 일차도 빈 파일을 생성한다. 일차 파일의 부재와 '그날 바 장면 없음'을 구분하기 위함.
    # 테스트 컨벤션(day 99)처럼 Days 밖의 일차도 바 씬이 있으면 파일을 만든다 — 없으면 조용히 유실된다
    _bar_days = {x[0] for x in DAYS} | {x[1] for x in place_members.get("bar", []) if x[1] is not None}
    bundles = [(f"script/bar/day{d}.json", [x for x in place_members.get("bar", []) if x[1] == d], "bar")
               for d in sorted(_bar_days)]
    bundles += [("script/home.json", place_members.get("home", []), "home")]
    # Day 99 거리 시나리오는 프로덕션 street.json에 섞지 않고 QA 번들로 분리한다.
    # InteractPoints·Spots·Characters의 qa_* 행은 day == 99에서만 활성화되므로 일반 플레이에 영향을 주지 않는다.
    _street_members = place_members.get("street", [])
    bundles += [("script/street.json", [x for x in _street_members if x[1] != 99], "street")]
    if any(x[1] == 99 for x in _street_members):
        bundles += [("script/qa/street_day99.json", [x for x in _street_members if x[1] == 99], "street")]
    bundles += [("script/cutscene.json", place_members.get("cutscene", []), "cutscene")]
    def _scene_complete_effect_map(raw):
        if not raw:
            return None
        out = {}
        for token in str(raw).split(";"):
            m = re.fullmatch(r"\s*(flag\.[a-z0-9_]+)\s*=\s*(true|false)\s*", token)
            if m:
                out[m.group(1)] = m.group(2) == "true"
        return out or None

    for fname, members, place in bundles:
        if fname == "script/street.json":
            dump(fname, _street_runtime["prod_script"])
            continue
        if fname == "script/qa/street_day99.json":
            dump(fname, _street_runtime["qa_script"])
            continue
        scenes, used_choices = [], set()
        for s in sorted(members, key=lambda x: (-1 if x[1] is None else x[1], PHASE_RANK.get(x[2], 9), x[3])):   # None = 상시 씬을 맨 앞에
            sd = dict(zip(SCENE_COLS, s))
            steps = []
            for st in sorted([x for x in STEPS if x[0] == s[0]], key=lambda x: x[1]):
                d = dict(zip(STEP_COLS, st))
                steps.append({"seq": d["seq"], "type": d["type"], "actor": d["actor"] or None,
                              "dialogue_id": d["dialogue_id"] or None,
                              "arg": d["arg"] or None,
                              "text": L(d["text_ko"], d["text_en"]) if d["text_ko"] else None,
                              "when": d["when"] or None, "effects": d["effects"] or None,
                              "sync": d["sync"] or "wait"})
                if d["type"] == "choice": used_choices.add(d["arg"])
            scene_payload = {**sd}
            if place == "street":
                scene_payload.pop("trigger", None)
                # 문서 「외부 거리 시스템」 정본: on_complete_effects는 문자열 DSL 그대로 배포한다
                scene_payload["on_complete_effects"] = (str(sd["on_complete_effects"]).strip() or None) if sd["on_complete_effects"] else None
            else:
                scene_payload.pop("start_mode", None)
                scene_payload.pop("on_complete_effects", None)
            scenes.append({**scene_payload, "steps": steps})
        choices = {cid: [{"seq": c[1], "text": L(c[2], c[3]), "when": c[4] or None,
                          "effects": c[5] or None, "goto": c[6] or None,
                          "lock_reason": L(c[8], c[9]) if c[4] else None}
                         for c in CHOICES if c[0] == cid] for cid in sorted(used_choices)}
        dump(fname, {"place": place, "scenes": scenes, "choices": choices})
    for old in [f"script/day_{n}.json" for n in range(0, 14)] + ["script/common.json", "script/bar.json", "expressions.json"] + [f"script/bar_day_{n}.json" for n in range(0, 14)] + [f"script/bar/day_{n}.json" for n in range(0, 14)]:   # 구 분할 잔존 방지
        pth = os.path.join(jdir, old)
        if os.path.exists(pth): os.remove(pth)

# ============================================================
def main():
    os.makedirs(OUT, exist_ok=True)
    derived = derive()
    errors, report = validate(derived)
    lines = ["===== L.U.N.A 데이터 빌드 리포트 (v2) ====="] + report + [""]
    if errors:
        lines.append(f"❌ 오류 {len(errors)}건:"); lines += ["  " + e for e in errors]
    else:
        lines.append("✅ 검증 통과: 참조 무결성 · 레시피 플래그 · 주문 풀 · L10N(ko/en) · 루나 대사 규칙 전부 정상")
    lines.append(f"칵테일 {len(COCKTAILS)} / 선반 {len(SHELF_ITEMS)} / 캐릭터 {len(CHARACTERS)} / 씬 {len(SCENES)} / 스텝 {len(STEPS)} / 선택지 {len(CHOICES)} / 웨이브 {len(RANDOM_WAVES)} / 단골슬롯 {len(REGULAR_SLOTS)} / UI문자열 {len(UI_STRINGS)}")
    print("\n".join(lines))
    with open(os.path.join(OUT, "빌드리포트.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    if errors: sys.exit(1)
    emit_xlsx(derived)
    emit_json(derived)
    print("→ LUNA_System.xlsx / LUNA_Narrative.xlsx / json/ 출력 완료")

if __name__ == "__main__":
    main()
