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
SHELF_COLS = ["id","kind","name_ko","name_en","category","color","sprite",
              "unlock_day","unlock_when","shop_price","desc_ko","desc_en"]
# v1.9: 재료(Ingredients)와 잔·도구·가니시(Items)를 한 테이블로 통합.
#   제조가 "잔 선반 → 도구 선반 → 가니시 선반 → 재료 선반" 4단계 선택으로 바뀌면서
#   넷 다 '선반에서 고르는 것'이 되었기 때문. kind가 어느 선반에 놓일지를 결정한다.
#   category는 재료일 때만 의미(어떤 기믹인지), sprite는 잔·도구·가니시용.
#   unlock_day/unlock_when/shop_price는 전 kind 공통 — 가니시도 입고·해금 대상이 될 수 있다.
SHELF_ITEMS = [
    # ── 재료 (소모품 — 수량·해금·상점가를 가진다) ──
    ("gin"            , "ingredient", "진"            , "Gin"                     , "base"     , "230,235,240", ""              , 1 , ""                       , None, "주니퍼베리 향의 증류주. 칵테일의 기본기."        , "A juniper-scented spirit. The foundation of cocktails."),
    ("beer"           , "ingredient", "맥주"           , "Beer"                    , "wine_beer", "202,162,4"  , ""              , 1 , ""                       , None, "차갑게. 그게 전부이자 진리."               , "Serve it cold. That's the whole truth."),
    ("red_wine"       , "ingredient", "레드와인"         , "Red Wine"                , "wine_beer", "98,15,11"   , ""              , 1 , ""                       , None, "잔에 따르는 순간부터 분위기가 달라진다."         , "The mood changes the moment it hits the glass."),
    ("champagne"      , "ingredient", "샴페인"          , "Champagne"               , "wine_beer", "255,245,225", ""              , 1 , ""                       , None, "축하할 일이 없어도 축하하게 만드는 술."         , "Makes you celebrate even with nothing to celebrate."),
    ("tonic_water"    , "ingredient", "토닉워터"         , "Tonic Water"             , "mixer"    , "245,250,255", ""              , 1 , ""                       , None, "씁쓸한 탄산. 진의 가장 오랜 친구."           , "Bitter fizz. Gin's oldest friend."),
    ("soda_water"     , "ingredient", "소다수"          , "Soda Water"              , "mixer"    , "245,250,255", ""              , 1 , ""                       , None, "아무 맛도 없어서 무엇이든 될 수 있다."         , "Tastes like nothing, so it can become anything."),
    ("lemon"          , "ingredient", "레몬"           , "Lemon"                   , "fruit"    , ""           , ""              , 1 , ""                       , 20  , "스퀴즈용. 새콤함의 표준."                 , "For squeezing. The standard of sour."),
    ("sugar"          , "ingredient", "설탕"           , "Sugar"                   , "powder"   , ""           , ""              , 1 , ""                       , 10  , "파우더용. 1티스푼의 위로."                , "For powder. One teaspoon of comfort."),
    ("tequila"        , "ingredient", "데킬라"          , "Tequila"                 , "base"     , "240,235,220", ""              , 3 , ""                       , None, "아가베의 태양을 병에 담은 것."              , "The agave sun, bottled."),
    ("vodka"          , "ingredient", "보드카"          , "Vodka"                   , "base"     , "240,245,250", ""              , 2 , ""                       , None, "무색무취. 그래서 어디에나 스며든다."           , "Colorless and odorless — that's how it blends in anywhere."),
    ("rum"            , "ingredient", "럼"            , "Rum"                     , "base"     , "180,120,60" , ""              , 3 , ""                       , None, "사탕수수와 항해의 술."                   , "The spirit of sugarcane and long voyages."),
    ("orange_juice"   , "ingredient", "오렌지주스"        , "Orange Juice"            , "juice"    , "253,180,60" , ""              , 3 , ""                       , 25  , "아침의 맛. 새벽의 바에서는 위장약."           , "The taste of morning. At a bar past midnight, it's medicine."),
    ("ginger_ale"     , "ingredient", "진저에일"         , "Ginger Ale"              , "mixer"    , "230,200,120", ""              , 3 , ""                       , 25  , "알싸한 생강 탄산."                     , "Spicy ginger fizz."),
    ("cola"           , "ingredient", "콜라"           , "Cola"                    , "mixer"    , "60,30,20"   , ""              , 3 , ""                       , 25  , "무엇을 섞어도 콜라 맛이 이긴다. 그게 무기다."     , "Mix in anything — cola wins. That's its weapon."),
    ("lime"           , "ingredient", "라임"           , "Lime"                    , "fruit"    , ""           , ""              , 2 , ""                       , 20  , "스퀴즈용. 레몬보다 한 톤 낮은 산미."          , "For squeezing. One tone lower than lemon."),
    ("whiskey"        , "ingredient", "위스키"          , "Whiskey"                 , "base"     , "190,120,40" , ""              , 4 , ""                       , None, "오크통에서 잠들었다 깨어난 시간."             , "Time that slept in an oak barrel and woke up."),
    ("dry_vermouth"   , "ingredient", "드라이 버무스"      , "Dry Vermouth"            , "liqueur"  , "220,220,190", ""              , 1 , ""                       , None, "마티니를 마티니로 만드는 한 방울."            , "The one drop that makes a martini a martini."),
    ("grenadine"      , "ingredient", "그레나딘"         , "Grenadine"               , "syrup"    , "180,30,50"  , ""              , 4 , ""                       , None, "석류빛 붉은 시럽. 노을 담당."              , "Pomegranate-red syrup. In charge of sunsets."),
    ("sour_mix"       , "ingredient", "사워믹스"         , "Sour Mix"                , "mixer"    , "230,220,160", ""              , 4 , ""                       , 25  , "새콤한 필업 베이스."                    , "A tangy fill-up base."),
    ("cointreau"      , "ingredient", "쿠앵트로"         , "Cointreau"               , "liqueur"  , "235,200,140", ""              , 2 , ""                       , None, "오렌지 리큐르의 귀족."                   , "The aristocrat of orange liqueurs."),
    ("cranberry_juice", "ingredient", "크랜베리주스"       , "Cranberry Juice"         , "juice"    , "170,30,60"  , ""              , 2 , ""                       , 25  , "붉고 떫고 세련된 맛."                   , "Red, tart, and sophisticated."),
    ("triple_sec"     , "ingredient", "트리플섹"         , "Triple Sec"              , "liqueur"  , "235,225,200", ""              , 6 , ""                       , None, "오렌지 리큐르의 실용주의자."                , "The pragmatist of orange liqueurs."),
    ("kahlua"         , "ingredient", "칼루아"          , "Kahlua"                  , "liqueur"  , "60,35,25"   , ""              , 2 , ""                       , None, "커피를 술로 번역한 것."                  , "Coffee, translated into liquor."),
    ("milk"           , "ingredient", "우유"           , "Milk"                    , "dairy"    , "245,245,240", ""              , 2 , ""                       , 15  , "고양이와 초보 손님의 선택."                , "The choice of cats and cautious customers."),
    ("honey_syrup"    , "ingredient", "벌꿀 원액"        , "Raw Honey"               , "syrup"    , "235,180,60" , ""              , 99, "flag.q_samho_honey_done", None, "삼호의 창고에서 나온 진짜 꿀. 요즘 세상엔 금값이다." , "Real honey from Samho's warehouse. Worth its weight in gold these days."),
    # ── 잔 (제조 1단계 선반) ──
    ("mug"            , "glass"     , "맥주잔"          , "Beer Mug"                , ""         , ""           , "glass_mug"     , 1 , ""                       , None, "생맥주·뮬 담당의 묵직한 잔."               , "A heavy glass for draft beer and mules."),
    ("wine"           , "glass"     , "와인잔"          , "Wine Glass"              , ""         , ""           , "glass_wine"    , 1 , ""                       , None, "다리가 긴 잔. 향을 가둔다."               , "A long-stemmed glass that holds the aroma."),
    ("flute"          , "glass"     , "플루트잔"         , "Flute Glass"             , ""         , ""           , "glass_flute"   , 1 , ""                       , None, "기포가 오래 살아있는 좁고 긴 잔."            , "Narrow and tall — the bubbles live longer."),
    ("highball"       , "glass"     , "하이볼잔"         , "Highball Glass"          , ""         , ""           , "glass_highball", 1 , ""                       , None, "탄산 롱드링크의 표준."                   , "The standard for fizzy long drinks."),
    ("collins"        , "glass"     , "콜린스잔"         , "Collins Glass"           , ""         , ""           , "glass_collins" , 1 , ""                       , None, "하이볼보다 조금 더 길고 늘씬한 잔."           , "A touch taller and slimmer than a highball."),
    ("rocks"          , "glass"     , "온더락잔"         , "Rocks Glass"             , ""         , ""           , "glass_rocks"   , 1 , ""                       , None, "낮고 두꺼운 잔. 얼음과 독주의 자리."          , "Low and thick. A seat for ice and strong spirits."),
    ("cocktail"       , "glass"     , "칵테일잔"         , "Cocktail Glass"          , ""         , ""           , "glass_cocktail", 1 , ""                       , None, "역삼각형의 그 잔. 격식의 상징."             , "The inverted triangle. A symbol of formality."),
    # ── 도구 (제조 2단계 선반 — 집으면 그 기믹) ──
    ("shaker"         , "tool"      , "셰이커"          , "Shaker"                  , ""         , ""           , "tool_shaker"   , 1 , ""                       , None, "셰이킹 기믹용."                       , "For the shaking gimmick."),
    ("mixing_glass"   , "tool"      , "믹싱 글라스 & 바 스푼", "Mixing Glass & Bar Spoon", ""         , ""           , "tool_mixing"   , 1 , ""                       , None, "스터 기믹용 — 잔에 붓기 전 여기서 젓는다."      , "For the stirring gimmick — stir here before pouring."),
    ("opener"         , "tool"      , "따개"           , "Opener"                  , ""         , ""           , "tool_opener"   , 1 , ""                       , None, "병뚜껑도 코르크도 이걸로 — 병따기·코르크 따기 기믹용.", "Caps and corks alike — for the cap-pop and cork-twist gimmicks."),
    # ── 가니시 (제조 3단계 선반 — v1.9부터 플레이어 선택·채점 대상) ──
    ("lime_wedge"     , "garnish"   , "라임 웨지"        , "Lime Wedge"              , ""         , ""           , "gn_lime"       , 1 , ""                       , None, "라임 조각 장식."                      , "A lime garnish."),
    ("lemon_slice"    , "garnish"   , "레몬 슬라이스"      , "Lemon Slice"             , ""         , ""           , "gn_lemon"      , 1 , ""                       , None, "레몬 슬라이스 장식."                    , "A lemon slice garnish."),
    ("olive"          , "garnish"   , "올리브"          , "Olive"                   , ""         , ""           , "gn_olive"      , 1 , ""                       , None, "마티니의 완성."                       , "What completes a martini."),
    ("cherry"         , "garnish"   , "체리"           , "Cherry"                  , ""         , ""           , "gn_cherry"     , 1 , ""                       , None, "사워 칵테일의 마침표."                   , "The period at the end of a sour."),
    ("orange_slice"   , "garnish"   , "오렌지 슬라이스"     , "Orange Slice"            , ""         , ""           , "gn_orange"     , 1 , ""                       , None, "선라이즈의 태양."                      , "The sun of a sunrise."),
]


# SHELF_ITEMS 접근 헬퍼 — 호출 시점에 전역을 읽는다(build.py가 시트 데이터로 갈아끼워도 동작)
def shelf_rows(kind=None):
    return [r for r in SHELF_ITEMS if kind is None or r[1] == kind]

def shelf_dicts(kind=None):
    return [dict(zip(SHELF_COLS, r)) for r in shelf_rows(kind)]

def shelf_ids(kind=None):
    return {r[0] for r in shelf_rows(kind)}


# ============================================================
# 2. 마스터 — Cocktails + RecipeLines
# unlock_day_override: 재료가 있어도 이 일차 전엔 미해금 (공란=재료 따름)
# unlock_when: 이벤트 조건부 해금 (공란=없음)
# ============================================================
# v1.7: prep = 병 개봉 사전 동작("" 없음 / cap 병뚜껑 / cork 코르크) — 도구 '따개' 하나가 둘 다 담당
#        mix의 bottle_open은 폐기(prep=cap으로 이동). 기믹 실행은 플레이어 선택 주도 — 이 값들은 '채점 정답'이다
# v1.9: unlock_day_override = 완전 수동 지정(공란이면 재료에서 파생). tier_override = 체감 난이도 수동 지정.
CK_COLS = ["id","name_ko","name_en","price","abv","glass","mix","prep","fill","garnish","color","tags",
           "flavor_ko","flavor_en","unlock_day_override","unlock_when","tier_override"]
COCKTAILS = [
    ("gin_tonic",       "진토닉",             "Gin & Tonic",     180, 8.0,  "highball", "build",  "tonic_water", "lime_wedge",   "255,255,255", "상큼한;청량한;클래식", "진과 토닉워터. 가장 단순해서 가장 정직한 칵테일.", "Gin and tonic. The simplest, and therefore the most honest.", None, ""),
    ("gin_fizz",        "진피즈",             "Gin Fizz",        220, 8.0,  "highball", "shake",  "soda_water",  "lemon_slice",  "255,255,255", "상큼한;클래식;청량한", "진과 레몬, 그리고 '피즈' 하는 탄산 소리.", "Gin, lemon, and that 'fizz' of carbonation.", None, ""),
    ("bottle_beer",     "병맥주",             "Bottled Beer",    90,  4.5,  "mug",      "none", "cap",  None,     None,           "202,162,4",   "청량한;가벼운",        "뚜껑 따는 소리가 안주다. 잔은 곁들여서.", "The pop of the cap is the appetizer. Served with a glass.", None, ""),
    ("red_wine",        "레드와인",           "Red Wine",        220, 13.0, "wine",     "none", "cork", None,          None,           "98,15,11",    "묵직한;클래식",        "말이 필요 없는 잔. 코르크는 조심스럽게.", "A glass that needs no words. Mind the cork.", None, ""),
    ("champagne",       "샴페인",             "Champagne",       350, 12.0, "flute",    "none", "cork", None,          None,           "255,245,225", "화사한;달콤한",        "기포가 올라오는 동안은 누구나 주인공.", "While the bubbles rise, everyone's the main character.", None, ""),
    ("screwdriver",     "스크류드라이버",     "Screwdriver",     190, 12.0, "highball", "build",  None,          None,           "236,223,95",  "달콤한;부드러운",      "보드카와 오렌지주스. 이름은 공구, 맛은 과일.", "Vodka and orange juice. Named after a tool, tastes like fruit.", None, ""),
    ("moscow_mule",     "모스코뮬",           "Moscow Mule",     210, 10.0, "mug",      "build",  "ginger_ale",  "lime_wedge",   "215,163,2",   "청량한;알싸한",        "생강의 알싸함이 노새의 뒷발차기 같다고 해서 뮬.", "The ginger kick they say feels like a mule's hind leg.", None, ""),
    ("tequila_sunrise", "데킬라 선라이즈",    "Tequila Sunrise", 200, 12.0, "collins",  "build",  None,          "orange_slice", "253,161,48",  "달콤한;화사한",        "잔 속에서 해가 뜬다. 새벽의 바에서 제일 잘 팔리는 아침.", "A sunrise inside a glass. The best-selling morning at a late-night bar.", None, ""),
    ("long_island",     "롱아일랜드 아이스티","Long Island Iced Tea", 380, 22.0, "collins", "build", "cola",     "lemon_slice",  "214,122,13",  "독한;달콤한",          "홍차는 한 방울도 안 들어간다. 그게 함정이다.", "Not a drop of tea in it. That's the trap.", None, ""),
    ("whiskey_sour",    "위스키 사워",        "Whiskey Sour",    240, 15.0, "rocks",    "shake",  "sour_mix",    "cherry",       "243,208,144", "새콤한;묵직한",        "위스키의 무게에 레몬의 균형.", "The weight of whiskey, balanced by lemon.", None, ""),
    ("dry_martini",     "드라이 마티니",      "Dry Martini",     300, 30.0, "cocktail", "stir",   None,          "olive",        "230,229,201", "씁쓸한;클래식;독한",   "젓지 말고 흔들어서, 라고 말하는 손님을 조심할 것.", "Beware the customer who says 'shaken, not stirred.'", None, ""),
    ("bacardi",         "바카디",             "Bacardi",         230, 20.0, "cocktail", "shake",  None,          None,           "219,96,89",   "새콤한;클래식",        "럼과 그레나딘과 라임. 이름을 건 칵테일.", "Rum, grenadine, lime. A cocktail that bears the name.", None, ""),
    ("cosmopolitan",    "코스모폴리탄",       "Cosmopolitan",    250, 20.0, "cocktail", "shake",  None,          "lemon_slice",  "219,109,92",  "상큼한;부드러운",      "도시적인 붉은 빛. 유행은 지나가도 맛은 남는다.", "Urban red. Trends pass; the taste stays.", None, ""),
    ("margarita",       "마가리타",           "Margarita",       260, 25.0, "cocktail", "shake",  None,          "lime_wedge",   "210,218,109", "새콤한;독한",          "데킬라의 태양과 레몬의 번개.", "Tequila's sun and lemon's lightning.", None, ""),
    ("white_lady",      "화이트레이디",       "White Lady",      270, 25.0, "cocktail", "shake",  None,          None,           "247,241,214", "상큼한;우아한",        "새하얀 드레스처럼 우아하고, 도수는 우아하지 않다.", "Elegant as a white dress. The proof is not elegant.", None, ""),
    ("kahlua_milk",     "깔루아 밀크",        "Kahlua Milk",     150, 7.0,  "rocks",    "build",  None,          None,           "159,162,119", "달콤한;부드러운",      "커피와 우유와 약간의 알코올. 고양이도 탐내는 맛.", "Coffee, milk, a little alcohol. Even cats covet it.", None, ""),
    # 히든 레시피 데모 — 퀘스트 samho_honey 보상(unlock_recipe)으로만 해금. 재료 honey_syrup이 day 99라 파생 해금일도 99 → 메뉴 비노출
    ("bees_knees",      "비즈 니즈",          "Bee's Knees",     260, 20.0, "cocktail", "shake",  None,          "lemon_slice",  "240,200,90",  "달콤한;클래식;비밀",   "금주법 시대, 싸구려 진의 향을 진짜 꿀로 감추던 밀주 칵테일. '최고'라는 뜻의 은어.", "A Prohibition-era bootleg cocktail — real honey to mask cheap gin. Slang for 'the best.'", None, ""),
]

# prep(v1.7) 역호환 — 구형 15필드 행에 prep="" 삽입 (mix 다음)
# 행 길이 보정: prep(7번째) 누락분 삽입 → tier_override(마지막) 누락분은 None으로 채움
COCKTAILS = [r if len(r) >= 16 else tuple(list(r[:7]) + [""] + list(r[7:])) for r in COCKTAILS]
COCKTAILS = [r if len(r) == len(CK_COLS) else tuple(list(r) + [None] * (len(CK_COLS) - len(r))) for r in COCKTAILS]

RECIPES = {
    "gin_tonic":       [("pour","gin",1.5,"oz")],
    "gin_fizz":        [("pour","gin",1.5,"oz"), ("squeeze","lemon",0.5,"oz"), ("powder","sugar",1,"tsp")],
    "bottle_beer":     [("pour","beer",12,"oz")],
    "red_wine":        [("pour","red_wine",8,"oz")],
    "champagne":       [("pour","champagne",8,"oz")],
    "screwdriver":     [("pour","vodka",1.5,"oz"), ("pour","orange_juice",6,"oz")],
    "moscow_mule":     [("pour","vodka",1.5,"oz"), ("squeeze","lime",0.5,"oz")],
    "tequila_sunrise": [("pour","tequila",1,"oz"), ("pour","orange_juice",6,"oz")],
    "long_island":     [("pour","gin",1,"oz"), ("pour","vodka",1,"oz"), ("pour","rum",1,"oz"), ("pour","tequila",1,"oz"), ("squeeze","lemon",0.5,"oz")],
    "whiskey_sour":    [("pour","whiskey",1.5,"oz"), ("squeeze","lemon",0.5,"oz"), ("powder","sugar",1,"tsp")],
    "dry_martini":     [("pour","gin",6,"oz"), ("pour","dry_vermouth",1.5,"oz")],
    "bacardi":         [("pour","rum",4.5,"oz"), ("pour","grenadine",0.75,"oz"), ("squeeze","lime",1.5,"oz")],
    "cosmopolitan":    [("pour","vodka",4,"oz"), ("pour","cointreau",1.5,"oz"), ("pour","cranberry_juice",3,"oz"), ("squeeze","lime",1.5,"oz")],
    "margarita":       [("pour","tequila",3.5,"oz"), ("pour","triple_sec",2,"oz"), ("squeeze","lemon",1.5,"oz")],
    "white_lady":      [("pour","gin",40,"ml"), ("pour","triple_sec",30,"ml"), ("squeeze","lemon",20,"ml")],
    "kahlua_milk":     [("pour","kahlua",1.5,"oz"), ("pour","milk",0.75,"oz")],
    "bees_knees":      [("pour","gin",2,"oz"), ("pour","honey_syrup",0.75,"oz"), ("squeeze","lemon",0.75,"oz")],
}

EXPECTED_TIER = {
    "gin_tonic":1,"gin_fizz":3,"bottle_beer":1,"red_wine":1,"champagne":1,
    "screwdriver":2,"moscow_mule":2,"tequila_sunrise":2,"long_island":5,
    "whiskey_sour":3,"dry_martini":2,"bacardi":3,"cosmopolitan":4,
    "margarita":3,"white_lady":3,"kahlua_milk":2,
    "bees_knees":3,  # ②문서 17종 외 신규 히든 레시피 (3라인+셰이크=기믹4)
}

# ============================================================
# 3. 마스터 — Characters / Personalities / Barks
# ============================================================
CHAR_COLS = ["id","name_ko","name_en","name_color","role","affinity","alive_flag","expressions","enter_sfx","exit_sfx","note"]
CHARACTERS = [
    ("luna",   "루나",     "Luna",       "#5e9fe1", "player",      False, None,    "default",                    None, None, "주인공. 인조인간 바텐더"),
    ("chris",  "크리스",   "Chris",      "#8b5e2f", "master",      True,  None,    "default;success;fail",       "SFX_chris_enter", "SFX_chris_exit", "바 언노운의 마스터. 前 연구원"),
    ("aili",   "아일리",   "Aili",       "#fcfe57", "guest_multi", True,  None,    "default;success;fail;joy",   None, None, "의사. 루나를 치료한 인물"),
    ("port",   "포트",     "Port",       "#3c3cca", "guest_multi", True,  None,    "default;anger;joy;serious;event_surprise",  "SFX_port_enter", "SFX_port_exit", "루나를 데려온 인물. 前 기자"),
    ("tom",    "톰 거너",  "Tom Gunner", "#7a4a2b", "guest_multi", True,  None,    "default;serious",            None, None, "갱 두목. 前 벡터 용병 대대장"),
    ("samho",  "삼호",     "Samho",      "#ea3a3a", "guest_multi", True,  "samho", "default;success;fail;drunk", None, None, "갱단원. day4 생사 분기(구 day3)"),
    ("bubi",   "부비",     "Bubi",       "#fdd48e", "guest_multi", True,  None,    "default",                    "SFX_cat_meow", None, "고양이"),
    ("haru",   "송하루",   "Song Haru",  "#9fd08f", "guest_multi", True,  "haru",  "default;sad",                None, None, "취준생. day8 생사 분기(구 day7)"),
    ("sunha",  "유선하",   "Yu Sunha",   "#c58bd6", "guest_multi", True,  None,    "default;serious",            None, None, "기자"),
    ("hina",   "히나",     "Hina",       "#f2a0b5", "guest_twice", False, None,    "default",                    None, None, "2회 등장(구 day5·10)"),
    ("shiba",  "시바",     "Shiba",      "#d9c58a", "guest_twice", False, None,    "default",                    None, None, "말하는 시바견. 크리스 앞에선 멍멍"),
    ("rios",   "리오스",   "Rios",       "#b03060", "guest_once",  False, None,    "default",                    None, None, "루나와 같은 실험체. 최종일 등장"),
    ("volts",  "볼츠",     "Volts",      "#708090", "guest_once",  False, None,    "default",                    None, None, "1회 등장(구 day8)"),
    ("yuna",   "유나",     "Yuna",       "#a8d8ff", "cutscene",    False, None,    "default",                    None, None, "꿈/과거 회상 전용. 루나의 은인"),
    ("soldier","경비병",   "Guard",      "#888888", "cutscene",    False, None,    "default",                    None, None, "꿈 컷씬 전용"),
    ("vendor", "완",       "Wan",        "#cccccc", "npc_street",  False, None,    "default",                    None, None, "노점상. 출퇴근길 상점"),
    ("board",  "전광판",   "News Board", "#66d9ff", "npc_street",  False, None,    "default",                    None, None, "거리 뉴스 전광판(연출용 화자)"),
    ("radio",  "라디오",   "Radio",      "#9ad0a0", "npc_street",  False, None,    "default",                    None, None, "엘리베이터·집 라디오(연출용 화자, 구엔진 elevator_radio 이식)"),
    ("sign",   "표기",     "Sign",       "#9aa2b5", "npc_street",  False, None,    "default",                    None, None, "전단·간판·자판기 등 사물 텍스트 화자(구엔진 speaker=object 대응)"),
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
    ("vendor", "default", "sprite", "vendor_default", ""),
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
    ("tl_samho_meet",      "timeline", "SmahoMeet",          "삼호 첫 만남"),
    ("tl_samho_arrive",    "timeline", "SmahoArrive",        "삼호 달려옴(생존루트 퇴근길 — day4)"),
    ("tl_samho_dead",      "timeline", "SamhoDead",          "삼호 사망 — day4 분기 연출"),
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
    ("bubi",  "idle",       "Ch/Bubi/cat_animation-Sheet",      "OK",     "거리 배회·발견 이벤트"),
    ("shiba", "idle",       "Ch/Shiba/shi_Bar-Sheet",           "OK",     "거리 NPC 겸용"),
    ("yuna",  "raid_set",   "CutSceneLab/08.yuna_attack 등",    "OK",     "꿈 컷씬 — 습격 시퀀스 프레임에 포함"),
    ("soldier","raid_set",  "CutSceneLab/04.soilder_idle",      "OK",     "꿈 컷씬"),
    ("hound", "raid_set",   "CutSceneLab/03·05·06·07 (hound_*)", "OK",    "꿈 컷씬 — 하운드(캐릭터 시트엔 없음, 컷씬 전용)"),
    ("chris", "idle",       "(미제작)",                          "신규필요", "테라스 대화·day1 퇴근길 동행에 필요 — 대화를 초상만으로 처리하면 불필요(연출 결정 대기)"),
    ("vendor","idle",       "(미제작)",                          "신규필요", "노점상 — 거리 상호작용 NPC"),
    ("haru",  "idle",       "(미제작)",                          "신규필요", "노말엔딩1 퇴근길 연출에 필요"),
    ("rios",  "idle",       "(미제작)",                          "신규필요", "배드엔딩 엘리베이터 연출에 필요"),
]

# ── 랜덤 손님 공용 외형 — 조합형 카탈로그 (바디×의상×눈×헤어, 성별 일치 조합) ──
# 각 항목은 character_anim 항목과 같은 이원 구조: 지금은 mode=sprite(한 장),
# 애니 전환 시 그 행의 mode를 parts_anim으로 바꾸고 파츠 시트를 추가하면 된다(스키마 변경 없음).
# sprite 키는 가칭 — 아트 제작 중, 완성 시 실제 리소스 키로 치환.
# emotions(v2.9) — 표정별 교체 스프라이트 맵 "표정:키; 표정:키" (예: "joy:Guest/eyes_m_1_joy; anger:Guest/eyes_m_1_anger").
#   랜덤 손님의 표정이 정해지면(barks.expression → bark_situations 기본 → default) 각 파트에서 그 표정 키를 찾아
#   있으면 그 스프라이트로 교체, 없으면 기본 sprite 유지. 전부 공란 = 현행 '표정 고정'과 동일 동작.
#   감정 표현은 눈 파트가 주 대상(코·입은 바디에 인쇄) — 감정 눈 아트가 나오면 eyes 행에만 채우면 켜진다.
GBODY_COLS = ["part","id","gender","personalities","mode","sprite","emotions","weight","status","note"]
GUEST_BODIES = [
    ("body",   "body_m",     "m", "", "sprite", "Guest/body_m",     "", 1, "제작중", "남성 바디 — 얼굴·몸통·코·입·패널 라인을 붙여 한 장으로 제작"),
    ("body",   "body_f",     "f", "", "sprite", "Guest/body_f",     "", 1, "제작중", "여성 바디 — 얼굴·몸통·코·입·패널 라인을 붙여 한 장으로 제작"),
    ("eyes",   "eyes_m_1",   "m", "", "sprite", "Guest/eyes_m_1",   "", 1, "제작중", ""),
    ("eyes",   "eyes_m_2",   "m", "", "sprite", "Guest/eyes_m_2",   "", 1, "제작중", ""),
    ("eyes",   "eyes_f_1",   "f", "", "sprite", "Guest/eyes_f_1",   "", 1, "제작중", ""),
    ("eyes",   "eyes_f_2",   "f", "", "sprite", "Guest/eyes_f_2",   "", 1, "제작중", ""),
    ("hair",   "hair_m_1",   "m", "", "sprite", "Guest/hair_m_1",   "", 1, "제작중", ""),
    ("hair",   "hair_m_2",   "m", "", "sprite", "Guest/hair_m_2",   "", 1, "제작중", ""),
    ("hair",   "hair_f_1",   "f", "", "sprite", "Guest/hair_f_1",   "", 1, "제작중", ""),
    ("hair",   "hair_f_2",   "f", "", "sprite", "Guest/hair_f_2",   "", 1, "제작중", ""),
    ("outfit", "outfit_m_1", "m", "", "sprite", "Guest/outfit_m_1", "", 1, "제작중", "민소매"),
    ("outfit", "outfit_m_2", "m", "", "sprite", "Guest/outfit_m_2", "", 1, "제작중", "아우터"),
    ("outfit", "outfit_f_1", "f", "", "sprite", "Guest/outfit_f_1", "", 1, "제작중", "민소매"),
    ("outfit", "outfit_f_2", "f", "", "sprite", "Guest/outfit_f_2", "", 1, "제작중", "아우터"),
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

PERS_COLS = ["id","name_ko","name_en","tip_mult","patience_mult","note"]
PERSONALITIES = [  # 26.07.17 PD 시트('랜덤 (일반) 손님 대사') 기준 5종. 배율은 가안
    ("gentle", "온화형", "Gentle", 1.0, 1.2, "예의 바르고 참을성 많음. 부정 반응도 조심스러움"),
    ("rough",  "거친형", "Rough",  1.1, 0.8, "입이 거침. 빨리 안 오면 폭발, 맛있으면 화끈하게 리액션"),
    ("touchy", "예민형", "Touchy", 1.0, 0.9, "(구)짜증난&예민한 손님. 응대 하나하나에 민감"),
    ("quiet",  "과묵형", "Quiet",  1.1, 1.1, "말수 최소. '...'가 대사의 절반"),
    ("chatty", "수다형", "Chatty", 1.0, 1.0, "혼잣말·너스레 많음. 대사가 김"),
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
    # ===== 루나(바텐더) 응대 라인 — 주문 3박자 플로우: 루나 질문 → 손님 고민 → 주문 =====
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
    (3, "삼호와 아일리", "Samho & Aili", "home", "bgm_street_night", "bgm_bar_main", 0, "입고 4종(데킬라·보드카·럼·OJ). 밤: 습격의 꿈 2"),
]

# v2.0 저작 분리 — 구 GuestSlots를 둘로 쪼갬. 1부 랜덤 손님(시스템 튜닝)과 단골 슬롯(서사)은
# 만지는 사람이 다르다. seq는 두 시트가 '하루 공용 번호'를 나눠 쓴다(스폰 순서 병합 기준) —
# 같은 날 같은 seq가 양쪽에 있으면 빌드 에러.
WAVE_COLS = ["day","seq","tier","personality","delay_sec","max_rounds","branch_choice"]
# 26.07.17 무드 확정 — 사이버펑크 슬럼가의 한적한 바: 하루 최대 4명, 스폰 텀 35~50초로 느슨하게
RANDOM_WAVES = [
    # day2 — T1×3 + T3×1(진피즈 프리뷰)
    (2, 1, 1, "gentle", 0,  1, False),
    (2, 2, 1, "chatty", 35, 1, False),
    (2, 3, 3, "quiet",  50, 1, False),
    (2, 4, 1, "touchy", 40, 1, False),
    # day3 — T2 + T5(롱아일랜드 프리뷰) + T2(다회주문)
    (3, 1, 2, "chatty", 0,  1, False),
    (3, 2, 5, "rough",  45, 1, False),
    (3, 3, 2, "touchy", 50, 2, False),
]

RSLOT_COLS = ["day","seq","character","tier","delay_sec","max_rounds","branch_choice","cameo_scene","must_serve","serve_effects"]
REGULAR_SLOTS = [
    # day3 — 포트 카메오(1부 말미에 T3 한 잔 하러 들른다)
    # must_serve=True: 인내심 타이머 없음 — 응대할 때까지 좌석 유지(2부 연결 손님 보호). False면 일반 손님처럼 이탈
    (3, 4, "port", 3, 40, 1, False, "d3_cameo_port", True, "flag.port_served_d3 = true"),
]

# 위치 프리셋 — 항상 같은 맵이므로 좌표 대신 이름 붙은 지점을 쓴다.
# Unity 씬에 같은 이름의 앵커 오브젝트(SpotAnchor)를 배치하고, 데이터는 이름만 참조.
# 배경 아트가 바뀌어도 앵커만 옮기면 되고, 데이터는 불변.
SPOT_COLS = ["id","area","desc","note"]
SPOTS = [
    ("bar_door",     "street", "바 '언노운' 정문 앞",   "출근 도착점 / 퇴근 출발점"),
    ("street_board", "street", "뉴스 전광판 앞",        "일차별 뉴스 연출"),
    ("street_stall", "street", "완의 노점",             "상점 + 노점 이벤트"),
    ("street_mid",   "street", "거리 중간",             "걷기 연출 기본 목적지 + 자판기"),
    ("street_wall",  "street", "전단이 붙은 벽",        "구엔진 outside_objects 이식(전단·포스터류)"),
    ("home_door",    "street", "집 현관 앞",            "퇴근 도착점 / 출근 출발점"),
    ("alley_in",     "alley",  "뒷골목 입구",           "골목 이벤트 얕은 쪽 + 임상시험 전단"),
    ("alley_deep",   "alley",  "뒷골목 안쪽",           "골목 이벤트 깊은 쪽(상자·고양이·쓰레기통)"),
    ("elevator",     "street", "엘리베이터 앞",         "배드엔딩2 연출 예약"),
]

# selection — 같은 포인트를 다시 조사했을 때의 규칙 (구엔진 outside_objects selection 이식):
#   once        1회 보면 소모
#   repeat      매번 같은 씬
#   sequential  조사할 때마다 group의 다음 씬(flow_seq 순) — 마지막 씬에서 멈춤
#   conditional 매번 group에서 when을 통과하는 첫 씬
# scene_or_shop: 씬 id / "group:<그룹id>" / "shop:<상점id>"
# trigger(v3.0 거리 시스템) — 대사 시작 방식: interact(E키 상호작용, 기본) / auto(재생 범위 진입 시 자동 재생 —
#   E 아이콘 없음·조작 락 없음·타이핑 종료 후 street_auto_next_delay_sec 뒤 다음 대사). auto는 대사 전용이라 shop: 참조 불가.
# actor(v3.1) — 이 지점에 서 있는 캐릭터(characters.id). 빈값 = 사물·전단 등 캐릭터 아님.
#   NPC의 "존재"는 actor가, "발동"은 지점이 담당 — 같은 actor를 phase·when이 다른 지점 여러 개에 배치하면
#   시간대별 위치 이동을 표현할 수 있다(소비 상태는 지점별, 그룹 진행은 scene_or_shop 공유로 이어짐).
POINT_COLS = ["id","spot","kind","actor","phase","trigger","when","scene_or_shop","selection","note"]
POINTS = [
    ("p_news_d2",      "street_board", "object", "", "commute_in",  "interact", "day == 2", "d2_news",         "once",   "전광판 뉴스(세계관 떡밥)"),
    ("p_news_d3",      "street_board", "object", "", "commute_in",  "interact", "day == 3", "d3_news",         "once",   "전광판 뉴스(벡터 정화사업)"),
    ("p_vendor_intro", "street_stall", "npc", "vendor",    "commute_in",  "interact", "day == 2 && !flag.q_lost_box_started", "d2_vendor_intro", "once", "노점상 완 첫 인사 + 퀘스트 시작"),
    ("p_vendor_shop",  "street_stall", "shop", "vendor",   "both",        "interact", "day >= 3", "shop:vendor",     "repeat", "노점 상점(과일·믹서 구매)"),
    ("p_lost_box",     "alley_deep",   "object", "", "commute_out", "interact", "flag.q_lost_box_started && !flag.q_lost_box_done", "d2_box_found", "once", "잃어버린 상자(퀘스트)"),
    ("p_alley_cat",    "alley_in",     "object", "", "commute_in",  "interact", "day == 3 && !flag.d3_cat_seen", "d3_alley_cat", "once", "노란 꼬리 목격 → d3 아일리 선택지 연동"),
    # --- 구엔진 outside_objects.json 이식 4종 (원문 대사 기반) ---
    ("p_shiba",       "alley_in",     "npc", "shiba",    "both",        "interact", "day >= 2", "group:np_shiba",     "conditional", "시바견 NPC — 구엔진 shiba.json 이식 (첫 조우→반복)"),
    ("p_ob_parttime",  "street_wall",  "object", "", "both", "interact", "day >= 2", "group:ob_parttime",  "sequential", "[이식] parttime_posting — 볼 때마다 다음 감상(순차 소비 데모)"),
    ("p_ob_vending",   "street_mid",   "object", "", "both", "interact", "day >= 2", "group:ob_vending",   "conditional", "[이식] vending_machine — 골드에 따라 분기(조건 선택 데모)"),
    ("p_ob_experiment","alley_in",     "object", "", "commute_out", "interact", "day >= 2", "group:ob_experiment", "once", "[이식] experiment_recruit — 코라테크 임상시험(세계관 복선)"),
    ("p_ob_toilet",    "alley_deep",   "object", "", "commute_out", "interact", "day >= 2 && flag.q_lost_box_done", "group:ob_toilet", "once", "[이식] toilet_bin — 유머"),
]

# ============================================================
# 5. 대본 — Scenes / Steps / Choices / Orders
# ============================================================
# trigger: auto / interact / cameo (+v1.2: pass:<spot> / event:<키> / manual)
# skippable: 컷씬 스킵 허용 여부 / group: interact 플로우 그룹(Points.selection과 연동)
# day 0 = 일차 무관 상시 씬(반복 NPC·오브젝트) — phase에 따라 street/home 등으로 배포
SCENE_COLS = ["id","day","phase","seq","trigger","when","title","skippable","group"]
SCENES = [
    ("d1_tutorial",    1, "bar",         1, "auto",     "", "튜토리얼 — 크리스의 진토닉 강습"),
    ("d1_home_talk",   1, "home",        1, "auto",     "", "테라스 — 바텐더의 태도 + 근황"),
    ("d1_intro",       1, "intro",       1, "auto",     "", "인트로 — 검은 화면, 루나가 처음 눈뜨던 밤"),
    ("d1_note",        1, "home",        0, "interact", "!flag.d1_note_read", "크리스의 쪽지"),
    ("d1_port",        1, "bar",         3, "auto",     "", "포트 첫 잔 — 진피즈 (튜토리얼 두 번째 제조)"),
    ("d1_elevator",    1, "commute_out", 2, "auto",     "", "퇴근길 엘리베이터 — 라디오 뉴스 (구엔진 elevator_radio 이식)"),
    ("d2_meet",        2, "commute_in",  2, "auto",     "", "출근길 — 삼호와 시바견"),
    ("d2_home_talk",   2, "home",        1, "auto",     "", "테라스 — 이틀째 밤"),
    ("d2_commute_in",  2, "commute_in",  1, "auto",     "", "첫 단독 출근"),
    ("d2_news",        2, "commute_in",  0, "interact", "", "전광판 — 코라테크/실종 뉴스"),
    ("d2_vendor_intro",2, "commute_in",  0, "interact", "", "노점상 완 — 인사 + 상자 퀘스트"),
    ("d2_bar_open",    2, "bar_open",    1, "auto",     "", "개점 전 — 크리스의 확인, OPEN 간판을 걸기까지"),
    ("d2_port_chris",  2, "bar",         1, "auto",     "", "포트 첫 등장 — 루나 구조의 진실 일부"),
    ("d2_shiba",       2, "bar",         2, "auto",     "", "시바 첫 등장 — 개똥철학 (크리스 부재)"),
    ("d2_chris_return",2, "bar",         3, "auto",     "", "크리스 복귀 — 시바는 멍멍"),
    ("d2_commute_out", 2, "commute_out", 1, "auto",     "", "퇴근길 독백"),
    ("d2_box_found",   2, "commute_out", 0, "interact", "", "상자 발견(퀘스트 완료)"),
    ("d2_dream",       2, "dream",       1, "auto",     "", "꿈 — 습격 1: 유나의 목소리와 총성"),
    ("d3_commute_in",  3, "commute_in",  1, "auto",     "", "출근길 — 어제의 꿈"),
    ("d3_news",        3, "commute_in",  0, "interact", "", "전광판 — 벡터 '정화 사업'"),
    ("d3_alley_cat",   3, "commute_in",  0, "interact", "", "골목의 노란 꼬리"),
    ("d3_bar_open",    3, "bar_open",    1, "auto",     "", "개점 전 — 어제 손님 이야기, OPEN"),
    ("d3_aili_bubi",   3, "bar",         1, "auto",     "", "아일리 첫 대면 + 부비 등장"),
    ("d3_samho",       3, "bar",         2, "auto",     "", "삼호 첫 등장 — 고도수 2연속"),
    ("d3_cameo_port",  3, "bar",         0, "cameo",    "", "1부 카메오 — 포트가 짧게 들름 (서빙 후 재생)"),
    ("d3_commute_out", 3, "commute_out", 1, "auto",     "", "퇴근길 독백"),
    ("d3_samho_death", 3, "commute_out", 2, "auto",     "flag.samho_death_route",   "…골목의 삼호 (사망 목격 — 데모 컷)"),
    ("d3_samho_rescue",3, "commute_out", 2, "auto",     "flag.samho_refused_drink", "도움 요청 — 삼호 구출"),
    ("d3_chris_witness",3,"home",        2, "auto",     "flag.samho_refused_drink", "크리스의 목격 — 데모 컷"),
    ("d3_home_talk",   3, "home",        1, "auto",     "!flag.samho_death_route && !flag.samho_refused_drink", "테라스 — 은인들 (데모에선 분기 씬이 대체)"),
    ("d3_dream",       3, "dream",       1, "auto",     "!flag.samho_death_route && !flag.samho_refused_drink", "꿈 — 습격 2 (데모에선 분기 엔딩이 대체)"),
    # --- 구엔진 outside_objects.json 이식 (day 0 = 상시 공용) ---
    ("np_shiba_1",      0, "street", 1, "interact", "!flag.shiba_met", "[이식] 시바 첫 조우 (구 first_encounter)", False, "np_shiba"),
    ("np_shiba_2",      0, "street", 2, "interact", "", "[이식] 시바 반복 대사 (구 revisit_repeat)", False, "np_shiba"),
    ("np_tv_1",         0, "home",   1, "interact", "!flag.tv_seen1", "홀로그램 TV — 실종 뉴스 (구 radio 이식)", False, "home_tv"),
    ("np_tv_2",         0, "home",   2, "interact", "", "홀로그램 TV — 토크쇼 (구 radio 이식)", False, "home_tv"),
    ("ob_parttime_1",   0, "street", 1, "interact", "", "[이식] 전단 — 알바 공고 (1회차 감상)", False, "ob_parttime"),
    ("ob_parttime_2",   0, "street", 2, "interact", "", "[이식] 전단 — 알바 공고 (2회차 감상)", False, "ob_parttime"),
    ("ob_parttime_3",   0, "street", 3, "interact", "", "[이식] 전단 — 알바 공고 (3회차 감상)", False, "ob_parttime"),
    ("ob_vending_buy",  0, "street", 1, "interact", "money >= 25", "[이식] 자판기 — 구매 성공", False, "ob_vending"),
    ("ob_vending_poor", 0, "street", 2, "interact", "", "[이식] 자판기 — 잔액 부족", False, "ob_vending"),
    ("ob_experiment_1", 0, "street", 1, "interact", "", "[이식] 임상시험 전단 — 코라테크 복선", False, "ob_experiment"),
    ("ob_toilet_1",     0, "street", 1, "interact", "", "[이식] 쓰레기통", False, "ob_toilet"),
]
# 구형 7필드 행 정규화 (skippable=False, group="")
SCENES = [r if len(r) == len(SCENE_COLS) else tuple(list(r) + [False, ""]) for r in SCENES]

# type v1.2 추가: expr(대사 없이 표정 전환) / anim(1회성 동작 클립) / emote(머리 위 이모트)
#                 timeline(Unity Timeline 재생) / gif(스파인→GIF 삽입) — timeline/gif는 Cutscenes 표 참조
# sync: ""(=wait, 완료 후 다음) / no_wait(완료를 기다리지 않고 즉시 다음 — 병렬 연출)
# say의 arg = 표정(Expressions 참조). 본인 대사 출력 중 입 애니메이션은 표정 데이터의 lower_face 규칙이 담당
STEP_COLS = ["scene_id","seq","type","actor","arg","text_ko","text_en","when","effects","sync","note"]
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
    ("d1_note", 1, "say", "luna", "default", "(테이블 위에 쪽지가 놓여 있다.)", "(There's a note on the table.)", "", "", ""),
    ("d1_note", 2, "say", "luna", "default", "「먼저 나간다. 가게로 와라.\n길은 어제 알려준 대로. — 크리스」", "\"Went ahead. Come to the bar.\nThe way I showed you yesterday. — Chris\"", "", "", ""),
    ("d1_note", 3, "say", "luna", "default", "(…출근이라는 걸, 해보자.)", "(...Time to try this thing called 'going to work.')", "", "flag.d1_note_read = true", ""),

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
    ("d1_elevator", 1, "say", "luna",  "idle", "(엘리베이터가 낡은 소리를 내며 내려간다.)", "(The elevator rattles its way down.)", "", "", "강제 컷씬"),
    ("d1_elevator", 2, "say", "radio", "", "…다음 뉴스입니다.", "...In other news.", "", "", "구 radio_d1_out_001"),
    ("d1_elevator", 3, "say", "radio", "", "뉴런 트롤프 주니어가 대통령 8연임에 성공하며,\n신미합중국의 제67대 대통령으로 다시 한번 당선되었습니다.", "Newron Trolph Jr. has won his eighth term,\nre-elected as the 67th President of the New United States.", "", "", ""),
    ("d1_elevator", 4, "say", "radio", "", "트롤프 대통령은 당선 직후 연설에서", "In his victory speech, President Trolph declared:", "", "", ""),
    ("d1_elevator", 5, "say", "radio", "", "\"위대한 국가 재건은 아직 끝나지 않았다.\"\n\"신미합중국은 다시 한 번 세계의 중심에 설 것이다.\"", "\"The great national rebuilding is not over.\"\n\"The New United States will stand at the center of the world once more.\"", "", "", ""),
    ("d1_elevator", 6, "say", "radio", "", "라고 밝혔습니다.", "— he stated.", "", "", ""),
    ("d1_elevator", 7, "say", "luna",  "idle", "(…세계는, 생각보다 넓다.)", "(...The world is wider than I thought.)", "", "", ""),

    # ---------- day2 출근길 — 삼호와 시바견 조우 ----------
    ("d2_meet", 1,  "say", "samho", "idle", "어? 너, 그 언노운의 새 바텐더 아냐?", "Huh? You're that new bartender at Unknown, right?", "", "", ""),
    ("d2_meet", 2,  "say", "luna",  "idle", "맞습니다. 당신은…", "That's right. And you are...", "", "", ""),
    ("d2_meet", 3,  "say", "samho", "idle", "삼호! 이 구역 애니멀 갱의 미래지. 기억해 둬.", "Samho! The future of the Animal Gang in this district. Remember it.", "", "", ""),
    ("d2_meet", 4,  "say", "shiba", "idle", "시끄러워, 시바.", "Too loud, shiba.", "", "", ""),
    ("d2_meet", 5,  "say", "luna",  "idle", "(…개가, 말을 했다.)", "(...The dog. It talked.)", "", "", ""),
    ("d2_meet", 6,  "say", "shiba", "idle", "뭘 봐. 처음 봐, 시바?", "What're you looking at. Never seen one before, shiba?", "", "", ""),
    ("d2_meet", 7,  "say", "samho", "idle", "하하, 얘는 시바. 말버릇은 저래도 나쁜 녀석은 아냐.", "Haha, this is Shiba. Foul mouth, decent guy.", "", "", ""),
    ("d2_meet", 8,  "say", "shiba", "idle", "네가 나쁜 놈이 아닌 거겠지, 시바.", "You mean YOU'RE the decent one, shiba.", "", "", ""),
    ("d2_meet", 9,  "say", "samho", "idle", "아무튼! 조만간 그 가게에 들를 거니까, 맛있는 거 준비해 둬!", "Anyway! I'll drop by that bar of yours soon — have something good ready!", "", "", "d3_samho_visit '내가 온다고 했잖아'의 복선"),
    ("d2_meet", 10, "say", "luna",  "idle", "(…시끄러운 아침이다.)", "(...A loud morning.)", "", "", ""),

    # ---------- day2 테라스 ----------
    ("d2_home_talk", 1, "fx", "", "terrace_night", "", "", "", "", ""),
    ("d2_home_talk", 2, "say", "chris", "default", "…이틀째다. 오늘은 어땠나.", "...Day two. How was it.", "", "", ""),
    ("d2_home_talk", 3, "say", "luna",  "default", "손님이 많았습니다. …사람들은, 전부 다르네요.", "There were many guests. ...People are all different.", "", "", ""),
    ("d2_home_talk", 4, "say", "chris", "default", "그래. 같은 잔을 시켜도 이유는 전부 다르다.", "Right. Even when they order the same glass, the reasons are never the same.", "", "", ""),
    ("d2_home_talk", 5, "say", "chris", "default", "그걸 읽는 게 이 일의 절반이야.", "Reading that is half of this job.", "", "", ""),
    ("d2_home_talk", 6, "say", "luna",  "default", "…나머지 절반은요?", "...And the other half?", "", "", ""),
    ("d2_home_talk", 7, "say", "chris", "default", "기다리는 거다. 손님이 먼저 말할 때까지.", "Waiting. Until the guest speaks first.", "", "", ""),
    ("d2_home_talk", 8, "say", "luna",  "default", "(기다린다… 기록해 둔다.)", "(Waiting... noted.)", "", "", "이후 습격의 꿈 1"),

    # ---------- day3 분기 — 사망 목격 (death route) ----------
    ("d3_samho_death", 1, "say", "luna", "idle", "(…골목이 소란스럽— 아니. 조용하다. 너무.)", "(...The alley is loud— no. It's quiet. Too quiet.)", "", "", ""),
    ("d3_samho_death", 2, "say", "luna", "idle", "(…삼호?)", "(...Samho?)", "", "", ""),
    ("d3_samho_death", 3, "say", "luna", "idle", "(골목 벽에… 기대앉아 있다.\n움직이지 않는다.)", "(He's slumped against the alley wall...\nNot moving.)", "", "", ""),
    ("d3_samho_death", 4, "say", "luna", "idle", "(손에… 노란 꽃이 쥐여 있다.)", "(In his hand... a yellow flower.)", "", "", "d3_samho_pour의 꽃 약속 회수"),
    ("d3_samho_death", 5, "say", "luna", "idle", "삼호. …삼호?", "Samho. ...Samho?", "", "", ""),
    ("d3_samho_death", 6, "say", "luna", "idle", "(………)", "(.........)", "", "", ""),
    ("d3_samho_death", 7, "say", "luna", "idle", "(반응이 없다. 체온이… 내려가 있다.)", "(No response. His body temperature... is falling.)", "", "alive.samho = false", ""),
    ("d3_samho_death", 8, "say", "luna", "idle", "(…내가 따라준, 마지막 잔이 생각났다.)", "(...I thought of the last glass I poured him.)", "", "", "→ 데모 엔딩(사망)"),

    # ---------- day3 분기 — 구출 (refuse route) ----------
    ("d3_samho_rescue", 1, "say", "samho", "idle", "…루나! 루나 맞지?!", "...Luna! Luna, that's you, right?!", "", "", "골목에서 튀어나옴"),
    ("d3_samho_rescue", 2, "say", "luna",  "idle", "삼호? 무슨 일—", "Samho? What's going—", "", "", ""),
    ("d3_samho_rescue", 3, "say", "samho", "idle", "쉿— 조용히. …마가로프 놈들이 우리 구역을 쳤어.", "Shh— quiet. ...The Magarov crew hit our turf.", "", "", ""),
    ("d3_samho_rescue", 4, "say", "samho", "idle", "아지트가 당했어. 동생들은 미리 빼돌렸는데…\n나도 지금 쫓기는 중이야.", "The hideout's gone. I got my siblings out in time...\nbut they're after me now.", "", "", ""),
    ("d3_samho_rescue", 5, "say", "luna",  "idle", "다친 겁니까? 팔이—", "Are you hurt? Your arm—", "", "", ""),
    ("d3_samho_rescue", 6, "say", "samho", "idle", "스친 거야. …저기, 부탁 하나만 하자.\n오늘 하룻밤만. 숨을 곳이 필요해.", "Just a graze. ...Listen, one favor.\nJust for tonight. I need somewhere to hide.", "", "", ""),
    ("d3_samho_rescue", 7, "say", "luna",  "idle", "(…크리스 씨한테 혼날지도 모른다. 하지만—)", "(...Chris might be furious. But—)", "", "", ""),
    ("d3_samho_rescue", 8, "say", "luna",  "idle", "따라오세요.", "Follow me.", "", "", ""),
    ("d3_samho_rescue", 9, "say", "samho", "idle", "…고마워. 진짜로.", "...Thank you. Really.", "", "", "→ 집으로 (크리스 목격)"),

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
    ("d2_commute_in", 1, "say",   "luna",  "idle", "(첫 정식 출근. 크리스는 먼저 나갔다.)", "(My first real shift. Chris left ahead of me.)", "", "", "오후 7시, 이미 밤"),
    ("d2_commute_in", 2, "say",   "luna",  "idle", "(…거리의 냄새는 아직 낯설다.)", "(...The smell of this street is still unfamiliar.)", "", "", "독백 후 자유 이동"),
    ("d2_news", 1, "say", "board", "", "[속보] 코라테크, 3분기 신경보철 출하량 사상 최대 기록", "[BREAKING] CoraTech posts record Q3 shipments of neural prosthetics", "", "", "세계관 뉴스"),
    ("d2_news", 2, "say", "board", "", "[지역] 서울 외곽 3구역, 실종 신고 3개월 연속 증가… 경비업체 \"순찰 강화\"", "[LOCAL] Missing-person reports in Outer Seoul District 3 rise for a third straight month... security firms 'stepping up patrols'", "", "", "중막 복선"),
    ("d2_news", 3, "say", "luna",  "idle", "(…외곽. 남 일 같지 않은 단어다.)", "(...The outskirts. A word that doesn't feel like someone else's problem.)", "", "", ""),
    ("d2_vendor_intro", 1, "say", "vendor", "idle", "오, 새 얼굴. 언노운의 새 알바가 너구나? 크리스한테 얘기 들었다.", "Oh, a new face. You're Unknown's new hire, aren't you? Chris told me about you.", "", "", ""),
    ("d2_vendor_intro", 2, "say", "luna",   "idle", "…안녕하세요. 루나입니다.", "...Hello. I'm Luna.", "", "", ""),
    ("d2_vendor_intro", 3, "say", "vendor", "idle", "난 완. 이 노점 주인이다. 과일이든 탄산이든, 재료가 떨어지면 나한테 와라.", "Name's Wan. I run this stall. Fruit, fizz, whatever — when you run out of stock, come to me.", "", "", "상점 기능 소개"),
    ("d2_vendor_intro", 4, "say", "vendor", "idle", "아 참 — 어제 배송 상자 하나를 뒷골목에서 흘렸다. 퇴근길에 보이면 주워다 주라. 사례는 하마.", "Oh, right — I dropped a delivery box in the back alley yesterday. If you spot it on your way home, bring it over. I'll make it worth your while.", "", "", "사이드퀘스트 발주"),
    ("d2_vendor_intro", 5, "effect", "", "", "", "", "", "flag.q_lost_box_started = true", "퀘스트 시작"),
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
    ("d2_commute_out", 1, "say", "luna", "idle", "(말하는 개, 목소리 큰 단골… 인간의 밤은 소란스럽다.)", "(A talking dog, a loud regular... human nights are noisy.)", "", "", "새벽 2시"),
    ("d2_box_found", 1, "say",    "luna", "idle", "(…골목 구석, 발자국에 밟힌 상자. 노점 마크가 찍혀 있다.)", "(...A box in the corner of the alley, trampled, stamped with the stall's mark.)", "", "", ""),
    ("d2_box_found", 2, "effect", "",     "", "", "", "", "flag.q_lost_box_done = true; quest(lost_box).advance", "퀘스트 완료 → 보상은 Quests 시트"),
    ("d2_box_found", 3, "say",    "luna", "idle", "(내일 완 아저씨한테 가져다주자.)", "(I'll bring it to Wan tomorrow.)", "", "", ""),
    ("d2_dream", 1, "fx",  "",     "glitch_in", "", "", "", "", "노이즈 인 — 카타나 제로식 조각 연출"),
    ("d2_dream", 2, "say", "yuna", "default", "루나. 눈 감아. 무슨 소리가 나도, 뜨면 안 돼.", "Luna. Close your eyes. Whatever you hear — don't open them.", "", "", "구 day1 꿈: 유나의 대사 재생"),
    ("d2_dream", 3, "sfx", "",     "alarm_distant", "", "", "", "", "멀리서 경보음"),
    ("d2_dream", 4, "say", "yuna", "default", "괜찮아. …넌 사람을 돕는 아이야. 그렇지?", "It's okay. ...You're a child who helps people. Aren't you?", "", "", ""),
    ("d2_dream", 5, "sfx", "",     "gunshot", "", "", "", "", "총성 — 여기서 컷"),
    ("d2_dream", 6, "fx",  "",     "hard_cut", "", "", "", "", "강제 암전"),
    ("d2_dream", 7, "say", "luna", "default", "…!!", "...!!", "", "flag.dream_raid_1 = true", "침대에서 깨어남"),
    # ---------- DAY 3 ----------
    ("d3_commute_in", 1, "say", "luna", "idle", "(어제의 꿈. …기록에 없는 소리였다.)", "(Last night's dream. ...A sound that isn't in my records.)", "", "", "꿈 후유증 독백"),
    ("d3_news", 1, "say", "board", "", "[경제] 벡터 그룹, 외곽 재개발 \"정화 사업\" 착수 발표… \"더 안전한 신대한민국\"", "[BUSINESS] Vector Group launches 'Purification Project' for the outskirts... 'A safer New Korea'", "", "", "벡터 첫 언급"),
    ("d3_news", 2, "say", "luna",  "idle", "(정화. …단어가 차갑다.)", "(Purification. ...A cold word.)", "", "", ""),
    ("d3_alley_cat", 1, "say",    "luna", "idle", "(골목 안쪽 — 노란 꼬리가 휙, 사라졌다.)", "(Deep in the alley — a yellow tail flicked out of sight.)", "", "", ""),
    ("d3_alley_cat", 2, "effect", "",     "", "", "", "", "flag.d3_cat_seen = true", "아일리 선택지 연동"),
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
    # --- 사이드퀘스트 데모: '진짜 벌꿀' (수주→선택지→씬 내 제작·서빙으로 목표 달성→보상 자동 발동) ---
    ("d3_samho", 16, "say",    "samho", "default", "아 — 가기 전에 장사 얘기 하나. 요즘 위쪽 동네에서 '진짜 벌꿀'이 금값이야. 마침 내 창고에 원액이 몇 병 있지.", "Oh — one bit of business before I go. Real honey goes for gold uptown these days. And I happen to have a few jars of the raw stuff in my warehouse.", "", "", "퀘스트 밑밥"),
    ("d3_samho", 17, "say",    "samho", "default", "한 병 구해다 주지. 대신 조건이 있다 — 셰이킹 실력 좀 보자. 진피즈 한 잔, 제대로.", "I'll get you a jar. One condition — show me your shake. A gin fizz, done right.", "", "", "제안 — 선택지 직전은 손님 대사(루나 규칙)"),
    ("d3_samho", 18, "choice", "",      "ch_d3_deal", "", "", "", "", "수락/거절 → 이후 스텝은 플래그로 분기"),
    ("d3_samho", 19, "order",  "samho", "exact:gin_fizz", "좋아. 레몬은 아끼지 말고.", "Good. Don't skimp on the lemon.", "flag.q_samho_honey_started", "", "수락 루트"),
    ("d3_samho", 20, "craft",  "",      "order", "", "", "flag.q_samho_honey_started", "", ""),
    ("d3_samho", 21, "serve",  "samho", "", "", "", "flag.q_samho_honey_started", "", "서빙 순간 serve:gin_fizz 목표 자동 달성 → Quests.reward_effects 발동"),
    ("d3_samho", 22, "say",    "samho", "success", "…호오. 손목이 살아 있는데? 거래 성립이다. 벌꿀은 내일 입고시켜 주지.", "...Well now. That wrist is alive. Deal. The honey arrives with tomorrow's stock.", "flag.q_samho_honey_started", "", "보상 재료는 다음날 stock_in부터 (Ingredients.unlock_when)"),
    ("d3_samho", 23, "say",    "samho", "default", "덤으로 레시피 하나 — '비즈 니즈'. 금주법 시대에 밀주 진을 꿀로 감추던 물건이다. 진짜 꿀 없인 못 만들지.", "And a recipe on the house — 'Bee's Knees.' Prohibition-era stuff, honey over bootleg gin. Can't make it without the real thing.", "flag.q_samho_honey_started", "", "unlock_recipe(bees_knees)는 보상에서 이미 발동"),
    ("d3_samho", 24, "say",    "samho", "default", "쳇, 배짱이 없구만. 거래는 타이밍인데 말이야.", "Tch. No guts. And in trade, timing is everything.", "flag.q_samho_honey_declined", "", "거절 루트 — 재제안 여지는 declined 플래그로"),
    ("d3_samho", 25, "say",    "samho", "default", "간다. 계산은 달아놔. …농담이다, 여기.", "I'm off. Put it on my tab. ...Kidding. Here.", "", "", ""),
    ("d3_samho", 26, "exit",   "samho", "", "", "", "", "", ""),
    ("d3_samho", 27, "say",    "aili",  "default", "…쟤 저래 봬도 나쁜 애는 아니야. 나중에 알게 될 거야.", "...He acts tough, but he's not a bad kid. You'll see.", "", "", "day4 분기 감정 밑작업"),
    ("d3_samho", 19, "exit",   "aili",  "", "", "", "", "", "부비를 안고 퇴장"),
    ("d3_samho", 20, "end_part","",     "", "", "", "", "", ""),
    ("d3_cameo_port", 1, "say", "port", "joy", "…역시. 어제 그 맛이 아니었으면 어쩌나 했지.", "...Right. I'd have worried if it wasn't yesterday's taste.", "", "", "1부 서빙 직후 재생되는 짧은 카메오"),
    ("d3_cameo_port", 2, "say", "port", "default", "배우는 속도가 빠르군. 크리스한테 칭찬해두지.", "You learn fast. I'll put in a good word with Chris.", "", "affinity.port += 1", "카메오는 2~3줄로 짧게 — 비중 최소"),
    ("d3_commute_out", 1, "say", "luna", "idle", "(고도수 두 잔을 연달아 마시는 인간의 간은, 어떤 구조일까.)", "(What is the structure of a human liver that takes two of those back to back?)", "", "", ""),
    ("d3_commute_out", 2, "say", "luna", "idle", "(진짜 벌꿀… 내일 입고 목록이 하나 늘었다. '거래'라는 건, 계산보다 나쁘지 않다.)", "(Real honey... one more line on tomorrow's stock list. 'Trade' is less unpleasant than my calculations suggested.)", "quest.samho_honey.stage >= 1", "", "연계 데모 — when DSL의 quest.<id>.stage 참조(0=미시작)"),
    ("d3_home_talk", 1,  "fx",     "",      "terrace_night", "", "", "", "", ""),
    ("d3_home_talk", 2,  "say",    "chris", "default", "아일리는 만났나.", "Did you meet Aili.", "", "", ""),
    ("d3_home_talk", 3,  "say",    "luna",  "default", "네. …포트 씨가 절 데려왔고, 아일리 씨가 고쳤다고 들었어요.", "Yes. ...I heard Port brought me in, and Aili fixed me.", "", "", ""),
    ("d3_home_talk", 4,  "say",    "chris", "default", "그래. 그 둘한테는 빚이 있다. 나도, 너도.", "Right. We owe those two. Both of us.", "", "", "구 day2 테라스 주제"),
    ("d3_home_talk", 5,  "say",    "luna",  "default", "…크리스 씨는, 왜 절 받아준 거예요?", "...Chris. Why did you take me in?", "", "", ""),
    ("d3_home_talk", 6,  "say",    "chris", "default", "……。", "......", "", "", "침묵"),
    ("d3_home_talk", 7,  "say",    "chris", "default", "…그 얘긴 아직이다. 때가 되면 말해주지.", "...Not yet. When it's time, I'll tell you.", "", "", "회피 — day10(구9)까지 이어짐"),
    ("d3_home_talk", 8,  "say",    "chris", "default", "…삼호와 거래를 텄다고? 나쁠 건 없지. 다만 걔 물건엔 늘 이야기가 붙어 있다. 조심해라.", "...You cut a deal with Samho? No harm in that. Just know his goods always come with a story attached. Be careful.", "flag.q_samho_honey_done", "", "연계 데모 — 퀘스트 완료 플래그로 씬 간 반응"),
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
    ("ob_parttime_1", 1, "say", "sign", "", "아르바이트 구함. 연락처: 154*455*587", "PART-TIMER WANTED. Contact: 154*455*587", "", "", "", "원문: parttime_flow_1/5"),
    ("ob_parttime_1", 2, "say", "luna", "idle", "(급여도 근무 시간도 없이, 연락처만 있네.)", "(No pay, no hours listed. Just a number.)", "", "", "", ""),
    ("ob_parttime_2", 1, "say", "luna", "idle", "(Bc25 편의점… 크리스한테 들은 적 있는 것 같은데. 이 구역에서 그나마 오래된 곳이라고 했던가.)", "(Bc25 convenience store... Chris mentioned it, I think. One of the oldest shops in this district?)", "", "", "", "원문: parttime_flow_2"),
    ("ob_parttime_3", 1, "say", "luna", "idle", "(글씨가… 손으로 쓴 건가, 발로 쓴 건가.)", "(Was this written by hand... or by foot?)", "", "", "", "원문: parttime_flow_3"),
    ("ob_vending_buy",  1, "say", "sign", "", "(지이잉―)", "(Vrrrr—)", "", "", "", "자판기 작동음"),
    ("ob_vending_buy",  2, "effect", "", "", "", "", "", "money -= 25; give(soda_water, 1)", "", "구매"),
    ("ob_vending_buy",  3, "say", "luna", "idle", "('초정 탄산수'를 얻었다.)", "(Got a bottle of 'Chojeong Sparkling Water.')", "", "", "", ""),
    ("ob_vending_poor", 1, "say", "sign", "", "(지이잉―)", "(Vrrrr—)", "", "", "", ""),
    ("ob_vending_poor", 2, "say", "luna", "idle", "(…살 돈이 모자란 것 같다.)", "(...I don't have enough for this.)", "", "", "", ""),
    ("ob_experiment_1", 1, "say", "sign", "", "인공 신경망 분석 — 임상시험 참가자 모집. 주관: (주)코라테크", "NEURAL NETWORK ANALYSIS — Clinical trial participants wanted. Sponsor: CoraTech Inc.", "", "", "", "원문: experiment_recruit"),
    ("ob_experiment_1", 2, "say", "luna", "idle", "(…설마.)", "(...It couldn't be.)", "", "flag.seen_coratech_ad = true", "", "세계관 복선 플래그"),
    ("ob_toilet_1", 1, "say", "luna", "idle", "…뭐지, 이건?", "...What is this?", "", "", "", "원문: toilet_bin"),
    ("ob_toilet_1", 2, "say", "luna", "idle", "(방금 전까지 누군가 사용한 흔적이 있는 것 같은데.)", "(Looks like someone just used it.)", "", "", "", ""),
    ("ob_toilet_1", 3, "say", "luna", "idle", "(더는 보고 싶지 않다.)", "(I don't want to look at this any longer.)", "", "", "", ""),
]
# 구형 10필드 행 정규화 (sync="" 를 effects 뒤에 삽입)
STEPS = [r if len(r) == len(STEP_COLS) else tuple(list(r[:9]) + [""] + [r[9]]) for r in STEPS]

CHOICE_COLS = ["choice_id","seq","text_ko","text_en","when","effects","goto","note"]
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
    ("ch_d3_deal",  1, "좋아요. 셰이커는 이미 잡았어요.", "Deal. My hand's already on the shaker.",  "", "flag.q_samho_honey_started = true", "", "퀘스트 수주"),
    ("ch_d3_deal",  2, "오늘은 사양할게요.",             "I'll pass tonight.",                      "", "flag.q_samho_honey_declined = true", "", "거절 — 재제안 연출용 플래그"),
]

# ── 구엔진 실대본(바 2부) 병합 — 가안을 실제 게임 대본으로 대체 (26.07.18) ──
# 원본: StreamingAssets/day0·1·2.json → converted/dayN_bar.py (자세한 것은 day_bar_scripts.py)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from day_bar_scripts import BAR_SCENES, BAR_STEPS, BAR_CHOICES, REPLACED_SCENES, REPLACED_CHOICES
SCENES = [s for s in SCENES if s[0] not in REPLACED_SCENES] + BAR_SCENES
STEPS = [s for s in STEPS if s[0] not in REPLACED_SCENES] + BAR_STEPS
CHOICES = [c for c in CHOICES if c[0] not in REPLACED_CHOICES] + BAR_CHOICES

ORDER_COLS = ["order_id","seq","when","verdict","effects","react","note"]
ORDERS = [
    ("d4_samho_dilemma", 1, "cocktail.abv >= 20",                 "fulfill", "flag.samho_drunk = true",                       "", "[day4 가안] 독한 술 → 취함 → 사망 루트"),
    ("d4_samho_dilemma", 2, "cocktail.abv <= 5 && grade >= good", "care",    "flag.samho_calmed = true; affinity.samho += 2", "", "[day4 가안] 배려 정답"),
    ("d4_samho_dilemma", 3, "cocktail.abv <= 5",                  "partial", "",                                              "", "[day4 가안]"),
    ("d4_samho_dilemma", 4, "",                                   "miss",    "",                                              "", "[day4 가안] default"),
]

QUEST_COLS = ["id","title_ko","title_en","kind","reward_effects","note"]
QUESTS = [
    ("lost_box",    "노점상의 잃어버린 상자", "The Vendor's Lost Box", "side", "money += 50; give(lime, 3)", "day2 발주 → 퇴근길 회수. interact: 목표 데모"),
    # serve: 목표 + 재료/레시피 해금 데모 — 재료는 Ingredients.unlock_when(다음날 입고), 레시피는 unlock_recipe(즉시)
    ("samho_honey", "진짜 벌꿀",              "The Real Honey",        "side", "affinity.samho += 10; unlock_recipe(bees_knees); flag.q_samho_honey_done = true", "day3 삼호 거래 — 진피즈 제공 시 완료. 히든 레시피 '비즈 니즈' 해금"),
]
QSTAGE_COLS = ["quest_id","stage","goal","when","on_complete"]
QUEST_STAGES = [
    ("lost_box",    1, "interact:p_lost_box", "flag.q_lost_box_started",    "flag.q_lost_box_reward = true"),
    ("samho_honey", 1, "serve:gin_fizz",      "flag.q_samho_honey_started", ""),
]

END_COLS = ["priority","id","when","scene_id","note"]
ENDINGS = [
    (1, "bad_1",    "flag.rios_accepted",                                                                           "ed_bad1",    "벡터 이전 수락 → 병기화"),
    (2, "happy_1",  "affinity.aili >= 100 && affinity.tom >= 100 && affinity.port >= 100 && affinity.sunha >= 100", "ed_happy1",  "벡터 고발, 전체 생존"),
    (3, "happy_2",  "affinity.aili >= 100 && affinity.tom >= 100 && affinity.port >= 100",                          "ed_happy2",  "언노운 이사, 전체 생존"),
    (4, "normal_1", "alive.haru && affinity.haru >= 100",                                                           "ed_normal1", "하루 루트, 크리스 사망"),
    (5, "bad_2",    "",                                                                                             "ed_bad2",    "폴백 — 납치"),
]

CONFIG_COLS = ["key","value","type","note"]   # type: 엑셀 왕복에서 3.0→3으로 뭉개지는 것을 막는 명시 타입 (v2.2)
CONFIG = [
    ("gold_start",             300,     "시작 골드"),
    ("reputation_start",       0,       "시작 평판"),
    ("commute_in_time",        "19:00", "출근 시각(연출 표기용). 배경은 밤 고정 단일 리소스"),
    ("commute_out_time",       "02:00", "퇴근 시각(연출 표기용)"),
    ("coaster_base_sec",       22,      "코스터 인내심 기본(①§3.7)"),
    ("coaster_per_tier_sec",   1.5,     "티어당 감소"),
    ("coaster_min_sec",        12,      "하한"),
    ("serve_bonus_sec",        40,      "서빙 인내심 = 제한시간 + 이 값"),
    ("serve_per_tier_sec",     3,       "티어당 감소"),
    ("serve_min_bonus_sec",    10,      "하한 보너스"),
    ("warn_yellow_ratio",      0.5,     "노란 경고 임계"),
    ("warn_red_ratio",         0.8,     "빨간 점멸 임계"),
    ("time_limit_base_sec",    20,      "제조 제한시간 기본 [가안]"),
    ("time_limit_per_gimmick_sec", 8,   "기믹 1개당 추가 초 [가안]"),
    ("spawn_delay_default_sec", 25,     "슬롯 delay 공란 시 기본"),
    ("next_round_delay_sec",   3,       "다회 주문 다음 잔 텀"),
    ("drunk_vomit_chance",     0.4,     "3잔째 제공 시 토함 확률 [TBD]"),
    ("wrong_cocktail_revenue_mult", -1.0, "주문과 다른 술: 술값 배율(-1.0=전액 배상)"),
    ("leave_coaster_rep",      -1,      "코스터 미제공 이탈 평판"),
    ("leave_serve_rep",        -2,      "서빙 지연 이탈 평판"),
    ("autosave_interval_step", 1,       "크래시 복구 스냅숏 갱신 주기(스텝 단위)"),
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
    # ── v3.0 거리 시스템 — 말풍선 상수는 바와 분리해 따로 튜닝한다 ──
    ("street_typing_interval_ms",   50, "거리 말풍선 타이핑 문자당 간격(ms) — 바(typing_interval_ms)와 별도 튜닝"),
    ("street_auto_next_delay_sec",  3,  "auto 재생 대사 — 타이핑 종료 후 다음 대사까지 텀"),
]

GRADE_CUTS = [("excellent",95),("good",80),("decent",60),("poor",35),("sewage",0)]
# v2.3 등급별 매출 배율 (PD 확정 26.07.24 — 고현정안 채택) — revenue_mult=술값 배율(음수면 배상).
#   팁 컬럼은 은퇴: 팁 = 가격 × (revenue_mult − 1.0) × 성격 tip_mult — 1.0 초과분(현재 excellent만)이 팁이다.
#   sewage와 오제조만 배상(-1.0) 유지. poor는 30%라도 받는다.
GRADE_PAYOUT = [("excellent", 1.2), ("good", 1.0), ("decent", 0.7),
                ("poor", 0.3), ("sewage", -1.0)]
AFFINITY_MATRIX = [
    ("love",6,4,1,-1,-2),("good",4,3,1,-1,-2),("ok",2,1,0,-1,-3),("dislike",1,0,-1,-2,-4),("miss",-4,-4,-4,-4,-4),
]

# 취향 — 서빙 호감 판정용 (first-match: 위에서부터 첫 일치, 매치 없으면 ok)
# when에 cocktail.* 외에 flag/day/affinity도 쓸 수 있다 → "그때그때 달라지는" 상황부 취향까지 데이터로 표현
TASTE_COLS = ["character_id","seq","when","tier","note"]
TASTES = [
    ("samho", 1, "flag.samho_calmed && cocktail.abv >= 20", "dislike", "[day4 가안] 배려 루트 이후엔 독주를 밀어냄 — 상황부 취향 데모"),
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
    ("ui_new_game",    "새 게임",            "New Game"),
    ("ui_sales",       "매출",               "Sales"),
    ("ui_tips",        "팁",                 "Tips"),
    ("ui_reputation",  "평판",               "Reputation"),
    ("ui_give_drink",  "제공한다",           "Serve it"),
    ("ui_refuse_drink","돌려보낸다",         "Send them home"),
    ("ui_crash_resume","지난 세션이 비정상 종료되었습니다. 이어서 진행할까요?", "The last session ended unexpectedly. Resume where you left off?"),
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
# 파생 계산
# ============================================================
def derive():
    ing = {r[0]: dict(zip(SHELF_COLS, r)) for r in shelf_rows()}
    cfg = {k: v for k, v, _ in CONFIG}
    derived = {}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        lines = RECIPES[d["id"]]
        gimmick = len(lines)
        if d["mix"] in ("build", "stir", "shake"): gimmick += 1
        if d["prep"] in ("cap", "cork"): gimmick += 1
        # 티어: 공란이면 기믹 수 공식, 값이 있으면 그 값 그대로 (체감 난이도 수동 지정 — v1.9)
        tier = d["tier_override"] or (1 if gimmick <= 2 else min(gimmick - 1, 5))
        used = [l[1] for l in lines] + ([d["fill"]] if d["fill"] else [])
        # 해금일: 공란이면 재료에서 파생, 값이 있으면 그 값 그대로 (완전 수동 지정 — v1.9)
        unlock = d["unlock_day_override"] or max([ing[u]["unlock_day"] for u in used] or [1])
        # 채점 항목: 시간·잔 + 믹스 + prep + 레시피 라인 + 필업 + 가니시(v1.9 플레이어 선택으로 편입)
        scoring = 2 + (1 if d["mix"] != "none" else 0) + (1 if d["prep"] else 0) + len(lines) + (1 if d["fill"] else 0) + 1
        tlimit = cfg["time_limit_base_sec"] + gimmick * cfg["time_limit_per_gimmick_sec"]
        derived[d["id"]] = dict(gimmick_count=gimmick, tier=tier, unlock_day=unlock,
                                scoring_items=scoring, time_limit_sec=tlimit)
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
    check_dup("Cutscenes", CUTSCENES, lambda r: r[0])
    check_dup("Quests", QUESTS, lambda r: r[0])
    check_dup("QuestStages", QUEST_STAGES, lambda r: (r[0], r[1]))
    check_dup("Endings", ENDINGS, lambda r: r[1])
    check_dup("Config", CONFIG, lambda r: r[0])
    check_dup("UIStrings", UI_STRINGS, lambda r: r[0])
    check_dup("TextTags", TEXT_TAGS, lambda r: r[0])
    check_dup("BarkSituations", BARK_SITUATIONS, lambda r: r[0])
    check_dup("GuestBodies", GUEST_BODIES, lambda r: r[1])
    check_dup("Expressions", EXPRESSIONS, lambda r: (r[0], r[1]))
    check_dup("ExpressionParts", EXPRESSION_PARTS, lambda r: (r[0], r[1], r[2]))
    check_dup("FieldAnims", FIELD_ANIMS, lambda r: (r[0], r[1]))
    check_dup("Tastes", TASTES, lambda r: (r[0], r[1]))
    check_dup("Dossier", DOSSIER, lambda r: (r[0], r[2], r[1], r[3]))

    ing_ids = shelf_ids("ingredient")
    glass_ids = shelf_ids("glass")
    garnish_ids = shelf_ids("garnish")
    tool_ids = shelf_ids("tool")
    item_ids = glass_ids | garnish_ids | tool_ids
    # kind 오배치 검사 — 한 테이블이 되면서 "잔 칸에 재료 id" 같은 실수가 가능해졌다 (v1.9)
    for r in shelf_rows():
        if r[1] not in ("ingredient", "glass", "tool", "garnish"):
            errors.append(f"[선반] {r[0]}: kind '{r[1]}' 은(는) 허용되지 않음 (ingredient/glass/tool/garnish)")
    char_ids = {c[0] for c in CHARACTERS}
    scene_ids = {s[0] for s in SCENES}
    choice_ids = {c[0] for c in CHOICES}
    cocktail_ids = {c[0] for c in COCKTAILS}
    pers_ids = {p[0] for p in PERSONALITIES}
    voice_ids = pers_ids | char_ids

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

    for cid, lines in RECIPES.items():
        for a, i, q, u in lines:
            if i not in ing_ids: errors.append(f"[레시피] {cid}: 재료 {i} 없음")
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        if d["mix"] not in ("none", "build", "stir", "shake"):
            errors.append(f"[칵테일] {d['id']}: mix '{d['mix']}' 불가 — bottle_open은 폐기(prep=cap으로)")
        if d["prep"] not in ("", "cap", "cork"):
            errors.append(f"[칵테일] {d['id']}: prep '{d['prep']}' 불가 (공란/cap/cork)")
        # kind까지 대조 — 통합 테이블이라 "잔 칸에 가니시 id"가 문법상 가능해졌다 (v1.9)
        if d["glass"] and d["glass"] not in glass_ids:
            errors.append(f"[칵테일] {d['id']}: 잔 {d['glass']} 없음" if d["glass"] not in shelf_ids()
                          else f"[칵테일] {d['id']}: 잔 칸에 kind=glass가 아닌 {d['glass']} 지정됨")
        if d["fill"] and d["fill"] not in ing_ids:
            errors.append(f"[칵테일] {d['id']}: 필 {d['fill']} 없음" if d["fill"] not in shelf_ids()
                          else f"[칵테일] {d['id']}: 필 칸에 kind=ingredient가 아닌 {d['fill']} 지정됨")
        if d["garnish"] and d["garnish"] not in garnish_ids:
            errors.append(f"[칵테일] {d['id']}: 가니시 {d['garnish']} 없음" if d["garnish"] not in shelf_ids()
                          else f"[칵테일] {d['id']}: 가니시 칸에 kind=garnish가 아닌 {d['garnish']} 지정됨")
        # 티어 회귀 가드 — 수동 지정(tier_override)한 칵테일은 사람이 정한 값이므로 대조 제외 (v1.9)
        if not d["tier_override"] and d["id"] in EXPECTED_TIER and derived[d["id"]]["tier"] != EXPECTED_TIER[d["id"]]:
            errors.append(f"[티어] {d['id']}: 계산 T{derived[d['id']]['tier']} ≠ ②문서 T{EXPECTED_TIER[d['id']]}")

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

    # 스텝 참조 + 루나 대사 규칙(플레이어 행동 직전 루나 say 금지)
    by_scene = {}
    for s in STEPS:
        by_scene.setdefault(s[0], []).append(dict(zip(STEP_COLS, s)))
    for sid, steps in by_scene.items():
        if sid not in scene_ids: errors.append(f"[스텝] 씬 {sid} 없음"); continue
        steps.sort(key=lambda x: x["seq"])
        prev = None
        for st in steps:
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
            if st["sync"] not in ("", "no_wait"): errors.append(f"[스텝] {sid}#{st['seq']}: sync {st['sync']} 불가")
            if st["type"] in PLAYER_ACTION_STEPS and prev and prev["type"] == "say" and prev["actor"] == "luna":
                errors.append(f"[루나규칙] {sid}#{st['seq']}: 플레이어 행동({st['type']}) 직전에 루나 대사 — 흐름 끊김")
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

    for p in POINTS:
        d = dict(zip(POINT_COLS, p))
        if d["spot"] not in spot_ids: errors.append(f"[포인트] {d['id']}: 위치 프리셋 {d['spot']} 없음")
        if d["selection"] not in ("once", "repeat", "sequential", "conditional"):
            errors.append(f"[포인트] {d['id']}: selection {d['selection']} 불가")
        tgt = d["scene_or_shop"]
        if tgt.startswith("group:"):
            if tgt[6:] not in scene_groups: errors.append(f"[포인트] {d['id']}: 씬 그룹 {tgt[6:]} 없음")
        elif not tgt.startswith("shop:") and tgt not in scene_ids:
            errors.append(f"[포인트] {d['id']}: 씬 {tgt} 없음")
        if d["selection"] in ("sequential", "conditional") and not tgt.startswith("group:"):
            errors.append(f"[포인트] {d['id']}: {d['selection']} 선택은 group: 참조가 필요")
        # v3.0 거리 시스템 — 대사 시작 방식 (proximity = 접근 범위 자동 재생. Scenes.trigger의 auto와 층이 달라 이름을 분리)
        if d["trigger"] not in ("interact", "proximity"):
            errors.append(f"[포인트] {d['id']}: trigger '{d['trigger']}' 불가 (interact/proximity)")
        if d["trigger"] == "proximity" and tgt.startswith("shop:"):
            errors.append(f"[포인트] {d['id']}: proximity 재생은 대사 전용 — shop: 참조 불가")
        if d["actor"] and d["actor"] not in char_ids:
            errors.append(f"[포인트] {d['id']}: actor '{d['actor']}' — Characters에 없음")
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
    for s in STEPS:  # move 스텝의 spot: 참조 검사
        if s[2] == "move" and str(s[4]).startswith("spot:") and s[4][5:] not in spot_ids:
            errors.append(f"[스텝] {s[0]}#{s[1]}: 위치 프리셋 {s[4][5:]} 없음")
    for g in RANDOM_WAVES:
        d = dict(zip(WAVE_COLS, g))
        if d["personality"] and d["personality"] not in pers_ids: errors.append(f"[웨이브] day{d['day']}#{d['seq']}: 성격 {d['personality']} 없음")
    # ── 랜덤 손님 외형 카탈로그 (v2.8) — 성별별 파트 풀이 비면 조합 불가 ──
    _gb_pool = {}
    for r in GUEST_BODIES:
        d = dict(zip(GBODY_COLS, r))
        if d["part"] not in ("body", "outfit", "eyes", "hair"):
            errors.append(f"[외형] {d['id']}: part '{d['part']}' 불가 (body/outfit/eyes/hair)")
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
    for g in ("m", "f"):
        for part in ("body", "outfit", "eyes", "hair"):
            for pid in pers_ids:
                if not _gb_pool.get((g, part, pid)):
                    errors.append(f"[외형] 성별 '{g}' × 성격 '{pid}'의 {part} 풀이 0개 — 이 성격 손님은 조합 불가")
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
        # goto 대상 검증 (v2.2) — 오타 나면 런타임에서 조용히 점프 실패하므로 여기서 잡는다
        if ch[6] and ch[6] not in scene_ids:
            errors.append(f"[선택지] {ch[0]}#{ch[1]}: goto 씬 {ch[6]} 없음")
    _ch_sizes = {}  # v3.0 — 선택지 세트는 2~4개 (1개짜리 확인용 금지, 5개 이상 UI 초과)
    for ch in CHOICES:
        _ch_sizes[ch[0]] = _ch_sizes.get(ch[0], 0) + 1
    for cid, n in _ch_sizes.items():
        if not (2 <= n <= 4):
            errors.append(f"[선택지] {cid}: 항목 {n}개 — 세트는 2~4개여야 함")
    for st in STEPS:
        if st[2] == "goto" and st[4] and st[4] not in scene_ids:
            errors.append(f"[스텝] {st[0]}#{st[1]}: goto 씬 {st[4]} 없음")

    # 배선 대기 씬 리포트 (v2.2) — trigger=manual인데 어디서도 참조되지 않는 씬은 게임에 안 나온다.
    # 초안 반영(draft_tools import)이 manual 씬을 만들므로, PD가 배선을 깜빡한 것을 여기서 드러낸다.
    _refs = {c[6] for c in CHOICES if c[6]}
    _refs |= {st[4] for st in STEPS if st[2] == "goto" and st[4]}
    _refs |= {p[5] for p in POINTS}
    _refs |= {dict(zip(RSLOT_COLS, r))["cameo_scene"] for r in REGULAR_SLOTS}
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
        [(f"단골슬롯 day{r[0]}#{r[1]}", r[9]) for r in REGULAR_SLOTS if r[9]])
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
        if derived[d["id"]]["unlock_day"] >= 99 and not d["unlock_when"] and d["id"] not in unlocked_by_effect:
            errors.append(f"[칵테일] {d['id']}: 해금 경로 없음 — unlock_when 또는 어딘가의 unlock_recipe() 필요")

    # 수동 해금일이 재료 입고일보다 빠르면 '메뉴에는 뜨는데 못 만드는' 상태 (v1.9) — 의도적일 수 있어 경고
    _ing_day = {r[0]: dict(zip(SHELF_COLS, r))["unlock_day"] for r in shelf_rows()}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        if not d["unlock_day_override"]:
            continue
        used = [l[1] for l in RECIPES[d["id"]]] + ([d["fill"]] if d["fill"] else [])
        need = max([_ing_day.get(u, 1) for u in used] or [1])
        if d["unlock_day_override"] < need:
            report.append(f"⚠ [해금] {d['id']}: 수동 해금 {d['unlock_day_override']}일차인데 재료는 {need}일차 입고 "
                          f"— 그 사이엔 메뉴에 보이지만 만들 수 없음(대본 지정 제조용이면 정상)")

    # 티어 풀 검증 (v1.9) — 1부 손님은 tier와 같은 티어에서 칵테일을 뽑는다. 비면 주문 불가
    for cols, rows, tag in ((WAVE_COLS, RANDOM_WAVES, "웨이브"), (RSLOT_COLS, REGULAR_SLOTS, "단골슬롯")):
        for sl in rows:
            s = dict(zip(cols, sl))
            pool = [c[0] for c in COCKTAILS
                    if derived[c[0]]["tier"] == s["tier"] and derived[c[0]]["unlock_day"] <= s["day"]]
            if not pool:
                errors.append(f"[{tag}] day{s['day']}#{s['seq']}: T{s['tier']} 칵테일이 그날 0종 — 손님이 주문할 게 없음")

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
    for k, ko, en in UI_STRINGS:
        check_text_tags(ko, f"UI {k}")
        check_text_tags(en, f"UI {k}(en)")

    # ── 2부 종료 트리거 검사 (v2.3, 명세서 §1.1) — end_part가 없거나 마지막 씬이 아니면 뒤 씬이 유실된다 ──
    for day in sorted({s[1] for s in SCENES}):
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

    # ── 대본 주문 해금 검사 (v2.7) — 2부에서 그날 못 만드는 칵테일을 주문하면 진행 불가 ──
    _ck_unlock = {cid: d["unlock_day"] for cid, d in derived.items()}
    _scene_day = {s[0]: s[1] for s in SCENES}
    for st in STEPS:
        if st[2] != "order" or not st[4].startswith("exact:"):
            continue
        cid = st[4][6:]
        day = _scene_day.get(st[0])
        if cid in _ck_unlock and day and _ck_unlock[cid] > day:
            errors.append(f"[주문 해금] {st[0]}#{st[1]}: day{day}에 '{cid}' 주문 — 해금 day{_ck_unlock[cid]}라 "
                          f"플레이어가 재료를 못 가짐 (재료 입고일을 당기거나 다른 칵테일로 교체)")

    # ── when DSL 전수 검사 (v1.5) — 문법 + 참조 무결성 ─────────────────
    WHEN_TOKEN = re.compile(
        r"^(day|money|reputation|grade|phase"
        r"|flag\.\w+|affinity\.\w+|alive\.\w+|quest\.\w+\.stage"
        r"|cocktail\.id|cocktail\.abv|cocktail\.tag\([^)]+\))$")
    CMP = re.compile(r"^(\S+)\s*(==|!=|>=|<=|>|<)\s*(\S+)$")
    NUM = re.compile(r"^-?\d+(\.\d+)?$")
    GRADES = {"excellent", "good", "decent", "poor", "sewage"}
    NUMERIC_LHS = ("day", "money", "reputation", "cocktail.abv")
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
                if lhs == "grade" and rhs not in GRADES:
                    errors.append(f"[when] {src}: 등급 '{rhs}' 불가")
                elif lhs == "cocktail.id" and rhs not in cocktail_ids:
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
        [(f"포인트 {p[0]}", dict(zip(POINT_COLS, p))["when"]) for p in POINTS] +
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
    for fl, src in sorted(referenced_flags.items()):
        if fl not in set_flags:
            flag_notes.append(f"⚠ [플래그] '{fl}' — 참조({src})되지만 어디서도 세워지지 않음 (오타 or 미작성 일차)")
    for fl, src in sorted(set_flags.items()):
        if fl not in referenced_flags:
            flag_notes.append(f"ℹ [플래그] '{fl}' — 세워지지만({src}) 아직 아무 조건도 참조하지 않음")
    report.extend(flag_notes)

    # ── 호감도 상한 시뮬레이션 (v1.5) — 최선 플레이 가정 상한치 ──
    scene_day = {s[0]: s[1] for s in SCENES}
    max_day = max((s[1] for s in SCENES), default=0)
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

    report.append("=== 일차별 입고표 (파생·검증용, 새 넘버링 — 입고 화면은 그날 바 오픈 직전) ===")
    for d in range(1, 7):
        new_ing = [s2["name_ko"] for s2 in shelf_dicts()
                   if s2["unlock_day"] == d and s2["kind"] in ("ingredient", "garnish")]   # 소모품만(잔·도구는 상시 비치)
        new_ck = [f"{c[1]}(T{derived[c[0]]['tier']})" for c in COCKTAILS if derived[c[0]]["unlock_day"] == d]
        report.append(f"Day {d}: 입고[{', '.join(new_ing) or '-'}] → 신규 가능[{', '.join(new_ck) or '-'}]")
    cond_ing = [f'{s2["name_ko"]}({s2["unlock_when"]})' for s2 in shelf_dicts() if (s2["unlock_day"] or 0) >= 99]
    cond_ck = [f"{c[1]}(T{derived[c[0]]['tier']})" for c in COCKTAILS if derived[c[0]]["unlock_day"] >= 99]
    if cond_ing: report.append(f"퀘스트/이벤트 해금 재료: {', '.join(cond_ing)}")
    if cond_ck: report.append(f"퀘스트/이벤트 해금 칵테일: {', '.join(cond_ck)}")
    report.append("=== 퀘스트 ===")
    for q in QUESTS:
        goals = " → ".join(s[2] for s in sorted((s for s in QUEST_STAGES if s[0] == q[0]), key=lambda s: s[1]))
        report.append(f"[{q[3]}] {q[0]} '{q[1]}': {goals} ⇒ 보상[{q[4]}]")
    merged_slots = ([(dict(zip(WAVE_COLS, g)), "") for g in RANDOM_WAVES]
                    + [(dict(zip(RSLOT_COLS, g)), f" [카메오: {dict(zip(RSLOT_COLS, g))['character']}]") for g in REGULAR_SLOTS])
    for d, tag in sorted(merged_slots, key=lambda x: (x[0]["day"], x[0]["seq"])):
        pool = [c[0] for c in COCKTAILS if derived[c[0]]["tier"] == d["tier"] and derived[c[0]]["unlock_day"] <= d["day"]]
        if pool:
            report.append(f"슬롯 day{d['day']}#{d['seq']} T{d['tier']} 풀({len(pool)}): {', '.join(pool)}{tag}")
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
    "Cocktails": "master", "RecipeLines": "master", "ShelfItems": "master",
    "Characters": "master", "Expressions": "master", "ExpressionParts": "master", "Cutscenes": "master",
    "ResourceMap": "audit", "FieldAnims": "master", "Personalities": "master", "Barks": "master",
    "GuestBodies": "master", "Days": "schedule", "RandomWaves": "schedule", "RegularSlots": "schedule", "Spots": "schedule", "InteractPoints": "schedule",
    "Scenes": "script", "Steps": "script", "Choices": "script", "OrderRules": "script", "TextTags": "master", "BarkSituations": "master",
    "Quests": "script", "QuestStages": "script", "Endings": "script",
    "Config": "balance", "GradeCuts": "balance", "GradePayout": "balance", "AffinityMatrix": "balance", "Tastes": "balance", "Dossier": "master",
    "UIStrings": "etc",
}

# 헤더 셀 메모(마우스오버 툴팁) — 작업자용 컬럼 설명
COL_DOCS = {
    "Cocktails": {
        "id": "snake_case 고유 id. 다른 시트가 이 값으로 이 칵테일을 가리킨다 — 한 번 정하면 바꾸지 말 것(참조가 전부 깨짐)",
        "name_ko": "메뉴판·주문에 뜨는 이름(한국어). 필수",
        "name_en": "영어 이름. 8월 번역 전까진 비워도 됨(비우면 빌드가 ko로 폴백)",
        "price": "판매가(골드). 정산 매출과 팁 계산의 기준값 — 팁 = 가격 × 등급 팁배율 × 손님 성격 배율",
        "abv": "도수(%). 손님 취향 판정(cocktail.abv)과 삼호 취함 분기(abv>=20)에 쓰인다",
        "glass": "정답 잔 — ShelfItems에서 kind=glass인 id. 비우면 정답이 '병째로'(병맥주). 플레이어가 ① 잔 선반에서 고른다",
        "mix": "섞는 방식 정답 — none(안 섞음)/build(가볍게 젓기)/stir(스터)/shake(셰이크). 플레이어가 ② 도구 선반에서 고른 도구와 대조: shake→셰이커, stir·build→믹싱글라스",
        "prep": "병 개봉 정답 — 공란(열 것 없음)/cap(병뚜껑 3연타)/cork(코르크는 천천히 2바퀴, 급하면 부스러져 감점). 도구 '따개' 하나가 둘 다 담당",
        "fill": "마지막에 잔을 채우는 재료(kind=ingredient, category=mixer). 채점: 정확히 일치해야 1점. 없으면 비움",
        "garnish": "정답 가니시 — ShelfItems에서 kind=garnish인 id. 플레이어가 ③ 가니시 선반에서 고른다. 비우면 정답은 '없음'(플레이어도 '없음'을 골라야 정답)",
        "color": "잔에 채워질 액체 색(R,G,B). 예: 255,255,255",
        "tags": "맛 분류 키워드. 세미콜론(;)으로 구분. 손님 취향 판정의 cocktail.tag(태그명)가 이 값을 본다",
        "flavor_ko": "메뉴판에 뜨는 한 줄 설명(한국어). 손님이 고민할 때 분위기를 만드는 문구",
        "flavor_en": "위 설명의 영어판. 비워도 됨(ko 폴백)",
        "unlock_day_override": "해금일을 손으로 지정. 비우면 재료들의 해금일에서 자동 계산. 재료보다 이른 날을 넣으면 빌드가 경고(메뉴엔 뜨는데 못 만드는 상태 — 대본 지정 제조용이면 정상)",
        "unlock_when": "조건부 해금(when 문법). 예: flag.q_samho_honey_done — 퀘스트를 깨야 열리는 히든 레시피에 사용",
        "tier_override": "난이도(T1~T5)를 손으로 지정. 비우면 기믹 개수로 자동 계산. 기믹 수와 체감 난이도가 다를 때 사람이 보정한다",
    },
    "RecipeLines": {
        "cocktail_id": "이 재료 줄이 속한 칵테일 id (Cocktails 참조)",
        "seq": "표시·채점용 순번. ※ 실행 순서가 아니다 — 실제 기믹은 개봉→따르기→스퀴즈→파우더→믹스→필업 순서로 강제된다",
        "action": "투입 방식 — pour(따르기)/squeeze(스퀴즈)/powder(파우더). 재료의 category가 아니라 이 값이 어떤 기믹이 나올지 결정",
        "ingredient_id": "넣을 재료 — ShelfItems에서 kind=ingredient인 id",
        "qty": "목표량. 채점 공식은 '정답에 가까울수록 고득점'(오차 비율로 감점)",
        "unit": "단위 — oz(온스)/ml/tsp(티스푼). 따르기·스퀴즈는 보통 oz, 파우더는 tsp",
    },
    "ShelfItems": {
        "id": "고유 id. 레시피·칵테일의 잔/가니시 칸이 이 값을 참조한다",
        "kind": "어느 선반 화면에 놓일지 — ingredient(재료)/glass(잔)/tool(도구)/garnish(가니시). 제조는 ①잔 →②도구 →③가니시 →④재료 순서로 진행된다",
        "name_ko": "화면에 표시되는 이름(한국어)",
        "name_en": "영어 이름(비워도 됨 — ko 폴백)",
        "category": "재료(kind=ingredient)일 때만 의미 — 어떤 기믹으로 갈지 결정. base·liqueur·wine_beer·juice·dairy·syrup=따르기 / fruit=스퀴즈 / powder=파우더 / mixer=필업",
        "color": "액체 렌더용 RGB. 스퀴즈·파우더 재료나 잔·도구는 비워도 됨",
        "sprite": "잔·도구·가니시의 스프라이트 리소스 키. 재료는 비움 (※ 아트 파일명 = 이 값)",
        "unlock_day": "며칠차부터 선반에 나오는지. 1=처음부터, N=그날 입고, 99=날짜로는 영원히 안 열림(퀘스트 전용 — unlock_when 필수, 없으면 빌드 에러)",
        "unlock_when": "조건부 입고(when 문법). 조건이 참이 되면 다음 입고 화면에 등장한다. 예: flag.q_samho_honey_done",
        "shop_price": "퇴근길 노점 판매가. 비우면 비매품. ※ 현재는 자동 입고가 기본이고 상점은 히든 재료용 보조 경로",
        "desc_ko": "입고 화면·제조 화면에서 보이는 설명(한국어)",
        "desc_en": "위 설명의 영어판(비워도 됨)",
    },
    "Characters": {
        "id": "고유 id. 대사(Steps.actor)·취향·수첩이 전부 이 값으로 인물을 가리킨다",
        "name_ko": "대사창에 뜨는 이름(한국어)",
        "name_en": "영어 이름(비워도 됨)",
        "name_color": "대사창 이름 글자색(#RRGGBB). 누가 말하는지 색으로 구분된다",
        "role": "player(루나)/master(크리스)/guest_multi(여러 번 오는 단골)/guest_twice/guest_once/npc_street(거리 NPC)/cutscene(컷씬 전용)",
        "affinity": "호감도를 추적할 인물인가(TRUE/FALSE). TRUE인 인물만 단골 수첩에 실리고 엔딩 조건에 들어간다",
        "alive_flag": "생사 분기가 있는 인물만 기입(예: alive.samho). when 문법의 alive.X와 연동",
        "expressions": "이 인물이 쓸 수 있는 표정 목록(Expressions 시트 참조). 세미콜론 구분",
        "base_body": "파츠 애니메이션 캐릭터의 베이스 바디 리소스 키",
        "enter_sfx": "등장 효과음 키(비워도 됨)",
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
    "Personalities": {
        "id": "성격 id. GuestSlots의 personality 칸과 Barks의 voice_id가 이 값을 참조",
        "name_ko": "성격 이름(한국어) — 온화형/거친형 등",
        "name_en": "영어 이름",
        "tip_mult": "팁 배율. 1.0이 기본이고 높을수록 후하다(예: 1.1 = 10% 더 줌)",
        "patience_mult": "인내심 시간 배율. 1.0이 기본이고 **높을수록 오래 기다려준다**(1.2=여유로움, 0.8=성질 급함)",
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
        "status": "제작중/OK — 에셋 검수용, 게임 미사용",
        "note": "기획 메모 (게임 미사용)",
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
        "tier": "이 손님이 주문할 칵테일의 난이도(T1~T5). 그 시점에 해금된 같은 티어 칵테일 중에서 무작위로 뽑는다 — 해당 티어가 0종이면 빌드 에러",
        "personality": "손님 성격(Personalities의 id). 비우면 무작위",
        "delay_sec": "앞 손님으로부터 몇 초 뒤에 들어오는지. 35~50초가 '한적한 슬럼가' 기본값",
        "max_rounds": "이 손님이 몇 잔이나 주문하는지(보통 1)",
        "branch_choice": "3잔째 취함 제공/거절 분기 대상인지(TRUE/FALSE, 명세서 §3.9)",
    },
    "RegularSlots": {
        "day": "몇 일차에 들르는지",
        "seq": "그날 몇 번째로 오는지. RandomWaves와 번호를 나눠 쓴다 — 같은 날 같은 번호가 양쪽에 있으면 빌드 에러",
        "character": "누가 오는지(Characters의 id). 필수 — 이름 없는 랜덤 손님은 RandomWaves 시트에",
        "tier": "1부 카메오로 왔을 때 주문할 칵테일의 난이도(T1~T5)",
        "delay_sec": "앞 손님으로부터 몇 초 뒤에 들어오는지",
        "max_rounds": "몇 잔이나 주문하는지(보통 1)",
        "branch_choice": "분기 선택지가 붙는 슬롯인지(TRUE/FALSE)",
        "cameo_scene": "착석 시 재생할 씬 id (없으면 일반 주문 흐름)",
        "must_serve": "TRUE = 인내심 타이머 없음. 시간이 지나도 이탈하지 않고 응대할 때까지 좌석을 지킨다 — 2부·스토리로 이어지는 손님이 1부에서 유실되는 사고 방지. FALSE = 일반 손님처럼 타임아웃",
        "serve_effects": "이 손님을 서빙(정산)한 순간 실행할 효과 — flag.x = true; affinity.x += 1 등. 스토리·서브퀘스트 연결용",
    },
    "Spots": {
        "id": "위치 id. Points의 spot 칸이 이 값을 참조한다",
        "area": "구역 — street(큰길)/alley(뒷골목)",
        "desc": "어디인지 설명(작업용 — 게임에 안 나오므로 번역 대상 아님)",
        "note": "용도 메모. 좌표 대신 이름으로 관리하는 이유 = 배경이 수정돼도 안 깨지기 때문",
    },
    "InteractPoints": {
        "id": "포인트 id. 퀘스트 목표(interact:이 id)가 참조한다",
        "spot": "어느 위치에 있는지(Spots 참조)",
        "kind": "object(조사하면 연출)/shop(상점 열기)/npc(대화 상대)",
        "phase": "언제 나타나는지 — commute_in(출근길)/commute_out(퇴근길)/both",
        "actor": "이 지점에 서 있는 캐릭터(Characters.id). 빈값 = 사물·전단 등. 같은 actor를 phase·when이 다른 지점 여러 개에 두면 시간대별 위치 이동 표현",
        "trigger": "대사 시작 방식 — interact(E키 상호작용, 기본)/proximity(재생 범위 진입 시 자동 재생 — E 아이콘 없음·조작 락 없음·street_auto_next_delay_sec 간격 자동 진행. 현재 실사용 0 = 예약)",
        "when": "나타날 조건(when 문법). 예: day == 2 → 2일차에만 등장",
        "scene_or_shop": "조사했을 때 재생할 씬 id. 'group:이름'이면 그 그룹의 씬들 중 selection 규칙대로 고른다. 'shop:'이면 상점",
        "selection": "반복 규칙 — once(한 번만)/repeat(계속)/sequential(볼 때마다 다음 씬)/conditional(조건 맞는 첫 씬). 시바견처럼 '첫 대화 후 반복 대사'는 conditional",
        "note": "작업 메모",
    },
    "Scenes": {
        "id": "씬 고유 id. Steps가 이 값으로 자기 소속을 밝힌다",
        "day": "몇 일차 씬인지. **0 = 일차 무관 공용 씬**(거리 오브젝트·NPC 등)",
        "phase": "어느 구간인지 — bar(바 2부)/bar_open(개점 전 대화)/commute_in/commute_out/home/dream/street/intro. **이 값이 비주얼을 자동 결정한다**(bar 계열=고해상도 흉상, 나머지=SD 픽셀)",
        "seq": "같은 phase 안에서의 재생 순서",
        "trigger": "발동 방식 — auto(그 구간 오면 자동)/interact(조사해야)/cameo(1부 카메오)/manual(다른 곳에서 호출할 때만)",
        "when": "이 씬이 재생될 조건(when 문법). 비우면 항상 재생",
        "title": "작업자용 제목(게임에 안 나옴)",
        "skippable": "건너뛰기 허용 여부(TRUE/FALSE). 튜토리얼은 FALSE",
        "group": "그룹 이름. Points의 'group:이름'과 짝을 이뤄 여러 씬을 묶는다(첫 대화/반복 대사 등)",
    },
    "Steps": {
        "scene_id": "이 스텝이 속한 씬(Scenes 참조)",
        "seq": "씬 안에서의 순서. 이 순서대로 한 줄씩 실행된다",
        "type": "무슨 동작인지 — say(대사)/enter·exit(등퇴장)/move(이동)/order(주문)/craft(제조 시작)/serve(서빙)/choice(선택지)/effect(효과만)/timeline(컷씬)/fx·sfx(연출·효과음)/end_part",
        "actor": "누가 하는지(Characters 참조). 연출용 스텝은 비움",
        "arg": "타입별 추가 정보 — say는 표정 이름, enter는 방향(L/M/R), order는 exact:칵테일id, craft는 order 또는 tutorial:칵테일id, choice는 Choices의 choice_id, timeline은 컷씬 id",
        "text_ko": "대사 원문(한국어). 리치텍스트 태그 사용 가능: <order>주문</order> <name>이름</name> <world>세계관용어</world>",
        "text_en": "영어 대사(비워도 됨 — ko 폴백)",
        "when": "이 스텝만의 조건(when 문법). 조건이 거짓이면 이 줄을 건너뛴다",
        "effects": "이 스텝이 일으키는 변화(effects 문법). 예: affinity.chris += 2; flag.x = true",
        "sync": "wait(끝나야 다음 줄) 또는 no_wait(다음 줄과 동시에 진행)",
        "note": "작업 메모(게임에 안 나옴)",
    },
    "Choices": {
        "choice_id": "선택지 묶음 id. Steps의 choice 타입이 arg에 이 값을 적어 호출한다",
        "seq": "선택지가 표시될 순서",
        "text_ko": "선택지 문구(한국어)",
        "text_en": "영어 문구(비워도 됨)",
        "when": "이 선택지가 **보일** 조건(when 문법). 조건부 선택지를 만들 때 사용",
        "effects": "이 선택지를 고르면 일어나는 변화(effects 문법). 호감도가 움직이는 주요 지점",
        "goto": "고른 뒤 점프할 씬 id. 비우면 원래 씬을 이어서 진행",
        "note": "작업 메모",
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
        "when": "이 단계가 활성화될 조건. 보통 수주 플래그(예: flag.q_samho_honey_started)",
        "on_complete": "이 단계를 넘길 때 실행할 효과(effects 문법). 비워도 됨",
    },
    "Endings": {
        "priority": "판정 순서(작을수록 먼저). **위에서부터 확인해 처음 조건이 맞는 엔딩으로 확정**된다",
        "id": "엔딩 id",
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
    "GradePayout": {
        "grade": "등급 이름(GradeCuts와 짝)",
        "revenue_mult": "술값 배율. 1.0=정가 다 받음, **음수면 배상**(-1.0 = 술값만큼 물어줌). sewage와 오제조만 음수",
        "tip_mult": "팁 비율(술값 대비). 여기에 손님 성격의 tip_mult가 한 번 더 곱해진다. 0이면 팁 없음",
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
    "InteractPoints": "거리에서 조사할 수 있는 지점(연출·상점·NPC)",
    "Scenes": "대본의 씬(대사 한 덩어리). phase가 비주얼과 재생 시점을 결정한다",
    "Steps": "씬 안의 한 줄 한 줄. 대사·등장·주문·제조·선택지가 전부 여기 들어간다",
    "Choices": "선택지 묶음. Steps의 choice 타입이 choice_id로 호출한다",
    "OrderRules": "애매한 주문에 '무엇을 만들어 냈는지'로 판정하는 표. 스토리 분기가 여기서 갈린다",
    "Quests": "퀘스트 기본 정보와 보상",
    "QuestStages": "퀘스트의 단계별 목표. 마지막 단계를 끝내면 Quests의 보상이 나온다",
    "Endings": "엔딩 조건. priority 순으로 확인해 처음 맞는 엔딩으로 확정된다",
    "Config": "게임 전역 상수. 시작 골드·시간대·제한시간 공식·페널티 등",
    "BarkSituations": "1부 대사 상황 사전 + 상황별 기본 표정. Barks.situation의 정본 목록",
    "GuestBodies": "랜덤 손님 공용 외형 카탈로그 — 바디(얼굴·코·입 포함)×의상×눈×헤어를 성별 맞춰 조합. 스폰 시 성별 추첨 후, 그 손님 성격을 허용하는 항목만 남겨 파트별 weight 가중 랜덤",
    "TextTags": "텍스트 연출 태그 정의 — <world> 색·<slow> 속도·<big> 크기 등. 대사에 쓴 태그는 반드시 여기 등록",
    "GradeCuts": "제조 점수(%)를 5등급으로 나누는 기준선",
    "GradePayout": "등급별 정산 — 술값을 얼마나 받고 팁이 얼마나 붙는지. 음수면 배상",
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
                    "계산 근거를 바꾸고 싶으면 재료·레시피·override 컬럼을 수정할 것.")
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
        "RandomWaves", "Config", "GradeCuts", "GradePayout"]},
    **{s: "Narrative" for s in [
        "Scenes", "Steps", "Choices", "Barks", "OrderRules", "Quests", "QuestStages",
        "Endings", "Dossier", "RegularSlots", "Characters", "Expressions",
        "ExpressionParts", "Cutscenes", "FieldAnims", "ResourceMap",
        "Days", "Spots", "InteractPoints", "UIStrings", "Tastes", "AffinityMatrix",
        "TextTags", "BarkSituations"]},
}
WORKBOOK_FILES = {"System": "LUNA_System.xlsx", "Narrative": "LUNA_Narrative.xlsx"}


def emit_xlsx(derived):
    # 1) 전 시트 데이터 구성 (name → (headers, rows))
    sheets = {
        "Cocktails": (CK_COLS + ["(파생)tier", "(파생)gimmick", "(파생)unlock_day", "(파생)scoring_items", "(파생)time_limit"],
            [list(c) + [f"T{derived[c[0]]['tier']}", derived[c[0]]["gimmick_count"], derived[c[0]]["unlock_day"],
                        derived[c[0]]["scoring_items"], derived[c[0]]["time_limit_sec"]] for c in COCKTAILS]),
        "RecipeLines": (["cocktail_id", "seq", "action", "ingredient_id", "qty", "unit"],
            [[cid, i + 1, a, ing, q, u] for cid, lines in RECIPES.items() for i, (a, ing, q, u) in enumerate(lines)]),
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
        "GradePayout": (["grade", "revenue_mult"], GRADE_PAYOUT),
        "AffinityMatrix": (["taste_tier", "excellent", "good", "decent", "poor", "sewage"], AFFINITY_MATRIX),
        "UIStrings": (UI_COLS, UI_STRINGS),
        "TextTags": (TEXTTAG_COLS, TEXT_TAGS),
        "BarkSituations": (BARKSIT_COLS, BARK_SITUATIONS),
        "GuestBodies": (GBODY_COLS, GUEST_BODIES),
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
            ("", "난이도·보상을 조절하고 싶다        →  Config + GradeCuts + GradePayout"),
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
            ("", "거리에 조사할 것을 놓고 싶다       →  Spots(위치) + InteractPoints(조사 지점)"),
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

def emit_json(derived):
    jdir = os.path.join(OUT, "json"); os.makedirs(os.path.join(jdir, "script", "bar"), exist_ok=True)
    def dump(name, obj):
        with open(os.path.join(jdir, name), "w", encoding="utf-8") as f:
            json.dump(obj, f, ensure_ascii=False, indent=2)

    master = {"cocktails": [], "shelf_items": [], "characters": [],
              "personalities": [], "barks": [],
              "ui_strings": {k: L(ko, en) for k, ko, en in UI_STRINGS}}
    for c in COCKTAILS:
        d = dict(zip(CK_COLS, c))
        master["cocktails"].append({
            "id": d["id"], "name": L(d["name_ko"], d["name_en"]), "price": d["price"], "abv": d["abv"],
            "glass": d["glass"], "mix": d["mix"], "prep": d["prep"] or None, "fill": d["fill"], "garnish": d["garnish"],
            "color": d["color"], "tags": d["tags"].split(";"),
            "flavor": L(d["flavor_ko"], d["flavor_en"]),
            "unlock_when": d["unlock_when"] or None,
            "recipe": [dict(zip(["action","ingredient","qty","unit"], l)) for l in RECIPES[d["id"]]],
            **derived[d["id"]],
        })
    # v1.9: 재료·잔·도구·가니시 통합 — kind가 어느 선반 화면에 놓일지를 결정
    for d in shelf_dicts():
        master["shelf_items"].append({
            "id": d["id"], "kind": d["kind"], "name": L(d["name_ko"], d["name_en"]),
            "category": d["category"] or None, "color": d["color"], "sprite": d["sprite"] or None,
            "unlock_day": d["unlock_day"], "unlock_when": d["unlock_when"] or None,
            "shop_price": d["shop_price"], "desc": L(d["desc_ko"], d["desc_en"])})
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
            "tip_mult": d["tip_mult"], "patience_mult": d["patience_mult"]})
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
        "grade_payout": {g: {"revenue_mult": rv} for g, rv in GRADE_PAYOUT},
        "affinity_matrix": [dict(zip(["taste_tier","excellent","good","decent","poor","sewage"], a)) for a in AFFINITY_MATRIX],
    })
    dump("days.json", [{**{k: v for k, v in zip(DAY_COLS, d) if k not in ("label_ko", "label_en")}, "label": L(d[1], d[2])} for d in DAYS])
    # v2.0: 구 guest_slots를 시스템(랜덤 웨이브)/서사(단골 슬롯)로 분리 배포 — 런타임은 둘을 seq로 병합
    dump("random_waves.json", [dict(zip(WAVE_COLS, g)) for g in RANDOM_WAVES])
    dump("regular_slots.json", [dict(zip(RSLOT_COLS, g)) for g in REGULAR_SLOTS])
    dump("spots.json", [dict(zip(SPOT_COLS, s)) for s in SPOTS])
    dump("interact_points.json", [dict(zip(POINT_COLS, p)) for p in POINTS])
    dump("quests.json", {
        "quests": [dict(id=q[0], title=L(q[1], q[2]), kind=q[3], reward_effects=q[4], note=q[5]) for q in QUESTS],
        "stages": [dict(zip(QSTAGE_COLS, s)) for s in QUEST_STAGES],
    })
    dump("endings.json", [dict(zip(END_COLS, e)) for e in ENDINGS])
    dump("order_rules.json", [dict(zip(ORDER_COLS, o)) for o in ORDERS])
    dump("text_tags.json", {t: {"kind": k, "value": v} for t, k, v, _ in TEXT_TAGS})
    dump("bark_situations.json", {s: {"default_expression": e} for s, e, _ in BARK_SITUATIONS})
    # 랜덤 손님 조합형 외형 — 파트별 그룹으로 배포. parts=null은 애니 전환용 자리(character_anim과 동일 규칙)
    gb = {"bodies": [], "outfits": [], "eyes": [], "hairs": []}
    _GB_KEY = {"body": "bodies", "outfit": "outfits", "eyes": "eyes", "hair": "hairs"}
    for r in GUEST_BODIES:
        d = dict(zip(GBODY_COLS, r))
        emo, _ = parse_emotions(d["emotions"])   # 표정별 교체 스프라이트(v2.9). 빈 dict = 표정 고정
        gb[_GB_KEY[d["part"]]].append({"id": d["id"], "gender": d["gender"],
                                       "personalities": [x.strip() for x in str(d["personalities"] or "").split(";") if x.strip()],
                                       "mode": d["mode"], "sprite": d["sprite"] or None, "emotions": emo or None,
                                       "parts": None, "weight": d["weight"]})
    dump("guest_bodies.json", gb)
    for old in ("schedule.json", "config.json", "grade_cuts.json", "tip_rates.json",
                "affinity_matrix.json", "quest_stages.json", "guest_slots.json", "orders.json", "points.json"):   # 구/과분할 파일 잔존 방지
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
    bundles = [(f"script/bar/day{d}.json", [x for x in place_members.get("bar", []) if x[1] == d], "bar")
               for d in sorted({x[1] for x in place_members.get("bar", [])})]
    bundles += [(f"script/{pl}.json", place_members.get(pl, []), pl) for pl in ("home", "street", "cutscene")]
    for fname, members, place in bundles:
        scenes, used_choices = [], set()
        for s in sorted(members, key=lambda x: (x[1], PHASE_RANK.get(x[2], 9), x[3])):
            sd = dict(zip(SCENE_COLS, s))
            steps = []
            for st in sorted([x for x in STEPS if x[0] == s[0]], key=lambda x: x[1]):
                d = dict(zip(STEP_COLS, st))
                steps.append({"seq": d["seq"], "type": d["type"], "actor": d["actor"] or None,
                              "arg": d["arg"] or None,
                              "text": L(d["text_ko"], d["text_en"]) if d["text_ko"] else None,
                              "when": d["when"] or None, "effects": d["effects"] or None,
                              "sync": d["sync"] or "wait"})
                if d["type"] == "choice": used_choices.add(d["arg"])
            scenes.append({**sd, "steps": steps})
        choices = {cid: [{"seq": c[1], "text": L(c[2], c[3]), "when": c[4] or None,
                          "effects": c[5] or None, "goto": c[6] or None}
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
        lines.append("✅ 검증 통과: 참조 무결성 · 티어 공식 · 슬롯 풀 · L10N(ko/en) · 루나 대사 규칙 전부 정상")
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
