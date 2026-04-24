from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN

BG      = RGBColor(0x0F, 0x17, 0x2A)
CARD    = RGBColor(0x16, 0x22, 0x40)
CARD2   = RGBColor(0x10, 0x1C, 0x34)
BLUE    = RGBColor(0x1A, 0x6F, 0xC4)
ORANGE  = RGBColor(0xE8, 0x52, 0x30)
GREEN   = RGBColor(0x3D, 0xC4, 0x6E)
YELLOW  = RGBColor(0xFF, 0xD0, 0x4A)
WHITE   = RGBColor(0xFF, 0xFF, 0xFF)
GRAY    = RGBColor(0x9A, 0xA8, 0xBF)
CODEBG  = RGBColor(0x08, 0x0E, 0x1A)
CGREEN  = RGBColor(0x6B, 0xFF, 0x72)
CBLUE   = RGBColor(0x79, 0xC0, 0xFF)
CGRAY   = RGBColor(0x6E, 0x76, 0x87)
CORANGE = RGBColor(0xFF, 0xA6, 0x57)

prs = Presentation()
prs.slide_width  = Inches(13.33)
prs.slide_height = Inches(7.5)
blank = prs.slide_layouts[6]

def slide():
    s = prs.slides.add_slide(blank)
    f = s.background.fill; f.solid(); f.fore_color.rgb = BG
    return s

def box(s, x, y, w, h, c):
    sh = s.shapes.add_shape(1, Inches(x), Inches(y), Inches(w), Inches(h))
    sh.line.fill.background(); sh.fill.solid(); sh.fill.fore_color.rgb = c
    return sh

def t(s, text, x, y, w, h, sz=13, bold=False, color=WHITE,
      align=PP_ALIGN.LEFT, italic=False):
    tb = s.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = tb.text_frame; tf.word_wrap = True
    p = tf.paragraphs[0]; p.alignment = align
    r = p.add_run(); r.text = text
    r.font.size = Pt(sz); r.font.bold = bold
    r.font.italic = italic; r.font.color.rgb = color
    return tb

def ml(s, items, x, y, w, h, dsz=13, dc=WHITE):
    tb = s.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = tb.text_frame; tf.word_wrap = True
    first = True
    for item in items:
        p = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False
        if isinstance(item, str):
            p.alignment = PP_ALIGN.LEFT
            r = p.add_run(); r.text = item
            r.font.size = Pt(dsz); r.font.color.rgb = dc
        else:
            p.alignment = item.get('a', PP_ALIGN.LEFT)
            r = p.add_run(); r.text = item.get('t','')
            r.font.size  = Pt(item.get('s', dsz))
            r.font.bold  = item.get('b', False)
            r.font.italic= item.get('i', False)
            r.font.color.rgb = item.get('c', dc)
            if item.get('m'): r.font.name = 'Consolas'

def code(s, lines, x, y, w, h, sz=10.5):
    box(s, x, y, w, h, CODEBG)
    box(s, x, y, 0.06, h, ORANGE)
    tb = s.shapes.add_textbox(Inches(x+0.12), Inches(y+0.1),
                               Inches(w-0.22), Inches(h-0.18))
    tf = tb.text_frame; tf.word_wrap = False
    first = True
    for item in lines:
        p = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False
        r = p.add_run()
        r.text, clr = item if isinstance(item, tuple) else (item, WHITE)
        r.font.size = Pt(sz); r.font.name = 'Consolas'
        r.font.color.rgb = clr

def hdr(s, badge, title, sub=None):
    box(s, 0, 0, 13.33, 0.1, ORANGE)
    if badge:
        box(s, 0.35, 0.17, 1.9, 0.46, ORANGE)
        t(s, badge, 0.35, 0.17, 1.9, 0.46, sz=11, bold=True,
          color=WHITE, align=PP_ALIGN.CENTER)
    ox = 2.4 if badge else 0.35
    t(s, title, ox, 0.16, 10.6, 0.56, sz=26, bold=True, color=WHITE)
    box(s, 0.35, 0.8, 12.63, 0.04, BLUE)
    if sub:
        t(s, sub, 0.35, 0.86, 12.63, 0.36, sz=12, color=GRAY)


# ═══════════════════════════════════════════════════════════
# 1. 표지
# ═══════════════════════════════════════════════════════════
s = slide()
box(s, 8.5, 0, 4.83, 7.5, RGBColor(0x0B,0x13,0x22))
box(s, 9.5, 0, 3.83, 7.5, RGBColor(0x08,0x10,0x1C))
box(s, 0, 0, 13.33, 0.1, ORANGE)
box(s, 0, 7.4, 13.33, 0.1, ORANGE)

t(s, '게임운영체제  |  중간 발표', 0.8, 1.0, 8, 0.48, sz=15, color=YELLOW)
t(s, 'HumanBartender', 0.8, 1.52, 8, 0.9, sz=44, bold=True, color=WHITE)
t(s, '게임 프로젝트와 운영체제 개념의 연결', 0.8, 2.5, 8, 0.5, sz=17, color=GRAY)
box(s, 0.8, 3.15, 5.2, 0.05, ORANGE)

ml(s, [
    {'t':'· 장르   칵테일 바텐더 시뮬레이션 (Unity 6)', 's':13,'c':WHITE},
    {'t':'· 발표자  최용근',                             's':13,'c':WHITE},
    {'t':'· 과목   게임운영체제  |  2026',              's':13,'c':GRAY},
], 0.8, 3.35, 7, 1.4)

weeks = ['2주차\n시스템 콜','3주차\n비동기 스레드',
         '4주차\n세마포어','5주차\nEDF 스케줄링',
         '6주차\n수요 페이징','7주차\n메모리 풀']
wc = [BLUE, GREEN, RGBColor(0x9B,0x59,0xB6), ORANGE,
      RGBColor(0x16,0xA0,0x85), YELLOW]
for i,(w,c) in enumerate(zip(weeks,wc)):
    bx = 9.6 + (i%2)*1.9
    by = 1.3 + (i//2)*1.65
    box(s, bx, by, 1.7, 1.45, RGBColor(0x0F,0x20,0x38))
    box(s, bx, by, 1.7, 0.05, c)
    t(s, w, bx, by+0.12, 1.7, 1.2, sz=10, bold=True, color=c,
      align=PP_ALIGN.CENTER)


# ═══════════════════════════════════════════════════════════
# 2. 프로젝트 소개
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, None, '프로젝트 소개', 'HumanBartender — Unity 6 칵테일 바텐더 시뮬레이션')

ml(s, [
    {'t':'게임 개요', 's':14, 'b':True, 'c':YELLOW},
    {'t':'', 's':5},
    {'t':'손님 주문에 맞는 칵테일을 제조하는 2D 시뮬레이션', 's':12,'c':WHITE},
    {'t':'', 's':5},
    {'t':'핵심 미니게임', 's':14, 'b':True, 'c':YELLOW},
    {'t':'', 's':5},
    {'t':'· Shaking   지그재그 제스처로 쉐이커 조작', 's':12,'c':WHITE},
    {'t':'· Stirring   원형 드래그로 교반 조작',    's':12,'c':WHITE},
    {'t':'', 's':5},
    {'t':'기술 스택', 's':14, 'b':True, 'c':YELLOW},
    {'t':'', 's':5},
    {'t':'Unity 6  /  UniTask  /  VContainer', 's':12,'c':WHITE},
    {'t':'Addressables  /  DoTween  /  Spine',  's':12,'c':WHITE},
], 0.4, 1.05, 5.3, 5.8)

cards = [
    ('대화 시스템',  ['DialogueTriggerManager','Command 패턴'], BLUE),
    ('미니게임',    ['ShakingManager','SturManager'],           ORANGE),
    ('메모리 관리', ['ObjectPool / PoolManager','ResourceLoader'], GREEN),
    ('데이터 로드', ['JsonManager','DataLoadManager'],          YELLOW),
]
for i,(tt,bb,cc) in enumerate(cards):
    bx = 5.9 + (i%2)*3.6
    by = 1.15 + (i//2)*2.35
    box(s, bx, by, 3.3, 2.15, CARD)
    box(s, bx, by, 3.3, 0.05, cc)
    t(s, tt, bx+0.15, by+0.12, 3.0, 0.4, sz=12, bold=True, color=cc)
    for j,b in enumerate(bb):
        t(s, b, bx+0.15, by+0.6+j*0.38, 3.0, 0.35, sz=11, color=GRAY)


# ═══════════════════════════════════════════════════════════
# 3. 전체 연결 구조
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, None, '2~7주차 OS 개념 — 프로젝트 구현 매핑',
    '수업에서 배운 개념이 실제 코드에 구현된 위치')

rows = [
    (BLUE,   '2주차','시스템 콜',     'JsonManager.cs',
     'File.ReadAllText() — OS 파일 I/O 시스템 콜 직접 호출'),
    (GREEN,  '3주차','비동기 스레드', 'ResourceLoader.cs',
     'UniTask async/await — 백그라운드 스레드 에셋 로드'),
    (RGBColor(0x9B,0x59,0xB6),'4주차','세마포어',
     'ShakingManager.cs',
     'UniTaskCompletionSource — Wait/Signal 패턴 직접 구현'),
    (ORANGE, '5주차','EDF 스케줄링',  'CircularGestureDetector.cs',
     'roundTimeout — 데드라인 초과 시 작업 취소(ResetRound)'),
    (RGBColor(0x16,0xA0,0x85),'6주차','수요 페이징',
     'ResourceLoader.cs',
     'Addressables Load/Release — 필요 시 로드, 사용 후 해제'),
    (YELLOW, '7주차','메모리 풀',     'ObjectPool.cs + PoolManager.cs',
     'Queue 사전 할당 — 런타임 GC 없이 UI 오브젝트 재사용'),
]

box(s, 0.3, 1.1, 1.15, 0.38, BLUE)
box(s, 1.5, 1.1, 2.1,  0.38, BLUE)
box(s, 3.65,1.1, 3.3,  0.38, BLUE)
box(s, 7.0, 1.1, 6.0,  0.38, BLUE)
for lbl,xp,wp in [('주차',0.3,1.15),('OS 개념',1.5,2.1),
                   ('구현 파일',3.65,3.3),('핵심 내용',7.0,6.0)]:
    t(s, lbl, xp+0.08, 1.15, wp-0.16, 0.28, sz=11, bold=True,
      color=WHITE, align=PP_ALIGN.CENTER)

for i,(clr,wk,concept,file,desc) in enumerate(rows):
    ry = 1.54 + i*0.9
    bg = CARD if i%2==0 else CARD2
    box(s, 0.3,  ry, 12.73, 0.84, bg)
    box(s, 0.3,  ry, 0.06,  0.84, clr)
    t(s, wk,     0.42, ry+0.2, 1.05, 0.44, sz=12, bold=True,
      color=clr, align=PP_ALIGN.CENTER)
    t(s, concept,1.55, ry+0.2, 2.0,  0.44, sz=12, color=WHITE)
    t(s, file,   3.7,  ry+0.2, 3.2,  0.44, sz=10, color=CGREEN,
      italic=True)
    t(s, desc,   7.05, ry+0.2, 5.9,  0.44, sz=10, color=GRAY)


# ═══════════════════════════════════════════════════════════
# 4. 2주차 — 시스템 콜 (JsonManager)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '2주차', '시스템 콜(System Call) — JsonManager.cs',
    'User Mode 게임 코드가 OS 커널에 파일 읽기를 요청하는 지점')

ml(s, [
    {'t':'왜 게임 개발자가 시스템 콜을 알아야 하는가?', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'게임 코드(User Mode)는 파일을 직접 읽을 수 없다.', 's':12,'c':WHITE},
    {'t':'반드시 OS에 시스템 콜을 요청해야 한다.', 's':12,'c':WHITE},
    {'t':'이 과정에서 User Mode → Kernel Mode 전환이 발생한다.', 's':12,'c':WHITE},
], 0.35, 1.05, 6.0, 2.0)

for i,(lbl,clr) in enumerate([
    ('게임 코드\n(User Mode)',   BLUE),
    ('System Call\nFile.ReadAllText()', ORANGE),
    ('OS 커널\n(Kernel Mode)', GREEN),
    ('디스크\nJSON 파일',        RGBColor(0x7F,0x5A,0x2A)),
]):
    bx = 0.35 + i*3.1
    box(s, bx, 3.15, 2.8, 0.9, clr)
    t(s, lbl, bx, 3.2, 2.8, 0.82, sz=11, bold=True,
      color=WHITE, align=PP_ALIGN.CENTER)
    if i < 3:
        t(s, '→', bx+2.85, 3.42, 0.35, 0.44,
          sz=20, bold=True, color=YELLOW, align=PP_ALIGN.CENTER)

t(s, '※ DataLoadManager.cs에서 게임 시작 시 7개 JSON 파일을 순차 로드',
  0.35, 4.15, 12.63, 0.38, sz=11, color=GRAY)

code(s, [
    ('// JsonManager.cs', CGRAY),
    ('public static T LoadGameData_StreamingAssets(string fileName)', WHITE),
    ('{', WHITE),
    ('    string filePath = Path.Combine(', WHITE),
    ('        Application.streamingAssetsPath, fileName);', WHITE),
    ('', WHITE),
    ('    if (File.Exists(filePath))          // ← OS에 파일 존재 여부 질의 (시스템 콜)', CGREEN),
    ('    {', WHITE),
    ('        string json = File.ReadAllText(filePath); // ← OS 파일 읽기 시스템 콜', CGREEN),
    ('        return JsonConvert.DeserializeObject<T>(json);', WHITE),
    ('    }', WHITE),
    ('}', WHITE),
], 0.35, 4.6, 12.63, 2.65)


# ═══════════════════════════════════════════════════════════
# 5. 3주차 — 비동기 스레드 (ResourceLoader)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '3주차', '비동기 스레드 — ResourceLoader.cs',
    '메인 스레드를 블로킹하지 않는 백그라운드 에셋 로드')

ml(s, [
    {'t':'문제: 메인 스레드 블로킹', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'에셋 로드를 메인 스레드에서 동기로 처리하면', 's':12,'c':WHITE},
    {'t':'로드가 끝날 때까지 게임이 완전히 멈춘다.', 's':12,'c':ORANGE},
    {'t':'', 's':5},
    {'t':'해결: 백그라운드 스레드 위임', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'UniTask async/await로 I/O를 백그라운드에서 처리하고', 's':12,'c':WHITE},
    {'t':'메인 스레드는 렌더링·로직을 계속 실행한다.', 's':12,'c':CGREEN},
], 0.35, 1.05, 5.6, 4.2)

box(s, 0.35, 5.3, 5.6, 0.38, CARD)
t(s, 'CancellationToken = 스레드 안전 종료', 0.5, 5.35, 5.3, 0.28,
  sz=12, bold=True, color=YELLOW)
t(s, '씬 전환 시 진행 중인 로드 작업을 안전하게 취소할 수 있다.',
  0.5, 5.73, 5.3, 0.32, sz=11, color=WHITE)

# 스레드 분리 그림
box(s, 6.2, 1.05, 6.8, 0.38, BLUE)
t(s, '메인 스레드 (계속 실행)', 6.2, 1.05, 6.8, 0.38,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
for i,lbl in enumerate(['Update()','Render()','Input()','UI()']):
    box(s, 6.2+i*1.68, 1.48, 1.58, 0.5, RGBColor(0x0F,0x3A,0x78))
    t(s, lbl, 6.2+i*1.68, 1.55, 1.58, 0.36,
      sz=10, color=WHITE, align=PP_ALIGN.CENTER)

box(s, 6.2, 2.2, 6.8, 0.38, RGBColor(0x3A,0x18,0x18))
t(s, '백그라운드 스레드 (UniTask)', 6.2, 2.2, 6.8, 0.38,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
for i,lbl in enumerate(['에셋 로드','씬 전환']):
    box(s, 6.2+i*3.4, 2.63, 3.2, 0.5, RGBColor(0x5A,0x18,0x18))
    t(s, lbl, 6.2+i*3.4, 2.7, 3.2, 0.36,
      sz=10, color=WHITE, align=PP_ALIGN.CENTER)

code(s, [
    ('// ResourceLoader.cs', CGRAY),
    ('public async static UniTask<AsyncOperationHandle<T>?> TryLoadAsync<T>(', WHITE),
    ('    string address, CancellationToken token)', WHITE),
    ('{', WHITE),
    ('    var handle = Addressables.LoadAssetAsync<T>(address);', WHITE),
    ('    await handle.ToUniTask(cancellationToken: token); // 백그라운드에서 로드', CGREEN),
    ('    // 메인 스레드는 계속 실행 — 완료되면 여기로 복귀', CGRAY),
    ('    if (handle.Status == AsyncOperationStatus.Succeeded)', WHITE),
    ('        return handle;', WHITE),
    ('}', WHITE),
], 6.2, 3.35, 6.8, 2.9)


# ═══════════════════════════════════════════════════════════
# 6. 4주차 — 세마포어 (ShakingManager)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '4주차', '세마포어(Semaphore) — ShakingManager.cs',
    'UniTaskCompletionSource = Wait / Signal 동기화 패턴')

ml(s, [
    {'t':'세마포어 핵심 동작', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'P(Wait)  : 신호가 올 때까지 블로킹', 's':12,'c':WHITE},
    {'t':'V(Signal): 신호를 주어 대기 해제',   's':12,'c':WHITE},
    {'t':'', 's':7},
    {'t':'칵테일 제작 흐름', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'1. StartCraftCommand가 미니게임 완료를 await로 대기', 's':12,'c':WHITE},
    {'t':'2. 유저가 완료 버튼 누름', 's':12,'c':WHITE},
    {'t':'3. ShakingManager.CompleteMade()가 신호 발송', 's':12,'c':WHITE},
    {'t':'4. await 해제 → 칵테일 결과 처리 진행', 's':12,'c':CGREEN},
], 0.35, 1.05, 5.8, 4.5)

box(s, 0.35, 5.7, 5.8, 1.55, CARD)
t(s, '세마포어 vs UniTaskCompletionSource',
  0.5, 5.75, 5.5, 0.38, sz=12, bold=True, color=YELLOW)
pairs = [
    ('sem_init(sem, 0)',      'tcs = new UniTaskCompletionSource()'),
    ('sem_wait(sem)  — 대기', 'await tcs.Task'),
    ('sem_post(sem)  — 신호', 'tcs.TrySetResult()'),
]
for i,(a,b) in enumerate(pairs):
    t(s, a, 0.5,  5.78+0.35*(i+1), 2.6, 0.32, sz=10, color=CGREEN,
      italic=True)
    t(s, b, 3.15, 5.78+0.35*(i+1), 2.8, 0.32, sz=10, color=WHITE)

code(s, [
    ('// ShakingManager.cs', CGRAY),
    ('private UniTaskCompletionSource tcs;', WHITE),
    ('', WHITE),
    ('// ① 초기화 — sem_init(tcs, 0)', CGRAY),
    ('public void InitGame(UniTaskCompletionSource tcs)', WHITE),
    ('{   this.tcs = tcs; }', CGREEN),
    ('', WHITE),
    ('// ② 신호 발송 — V(Signal) : 유저가 완료 버튼 눌렀을 때', CGRAY),
    ('public void CompleteMade()', WHITE),
    ('{', WHITE),
    ('    craftStation.craftingResult.isResult = true;', WHITE),
    ('    tcs.TrySetResult();  // 대기 중인 StartCraftCommand를 깨움', CGREEN),
    ('}', WHITE),
], 6.3, 1.05, 6.7, 3.6)

code(s, [
    ('// StartCraftCommand.cs', CGRAY),
    ('public async UniTask<string> ExecuteAsync(CancellationToken ct)', WHITE),
    ('{', WHITE),
    ('    // ③ 대기 — P(Wait) : 미니게임 완료 신호 올 때까지 블로킹', CGRAY),
    ('    string nextId = await craftMgr.StartCraftAsync(craft_event_id);', CGREEN),
    ('    return nextId;', WHITE),
    ('}', WHITE),
], 6.3, 4.75, 6.7, 2.5)


# ═══════════════════════════════════════════════════════════
# 7. 5주차 — EDF 스케줄링 (CircularGestureDetector)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '5주차', 'EDF 스케줄링 — CircularGestureDetector.cs',
    '데드라인 초과 시 작업을 취소하는 실시간 스케줄링 구현')

ml(s, [
    {'t':'EDF(Earliest Deadline First)란?', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'데드라인이 가장 가까운 태스크를 우선 처리하고', 's':12,'c':WHITE},
    {'t':'데드라인을 초과한 태스크는 취소·재스케줄한다.', 's':12,'c':WHITE},
    {'t':'', 's':7},
    {'t':'원형 제스처 교반(Stirring) 게임', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'6개 섹터를 시계방향으로 모두 통과해야 1회 교반 완료', 's':12,'c':WHITE},
    {'t':'3초(roundTimeout) 내에 완료 못하면 처음부터 다시', 's':12,'c':ORANGE},
    {'t':'→ roundTimeout = 데드라인', 's':12,'b':True,'c':CGREEN},
], 0.35, 1.05, 5.8, 5.2)

# 6섹터 다이어그램
t(s, '6섹터 상태 머신', 6.5, 1.05, 6.5, 0.38,
  sz=13, bold=True, color=YELLOW, align=PP_ALIGN.CENTER)
positions = [(9.65,1.55),(10.85,2.25),(10.85,3.45),
             (9.65,4.15),(8.45,3.45),(8.45,2.25)]
for i,(sx,sy) in enumerate(positions):
    clr = ORANGE if i==0 else BLUE
    box(s, sx-0.4, sy-0.32, 0.82, 0.66, clr)
    t(s, f'섹터\n{i}', sx-0.4, sy-0.32, 0.82, 0.66,
      sz=9, color=WHITE, align=PP_ALIGN.CENTER)
t(s, '→ 시계방향\n완주 = 완료', 9.25, 2.75, 1.4, 0.7,
  sz=9, color=GRAY, align=PP_ALIGN.CENTER)

code(s, [
    ('// CircularGestureDetector.cs', CGRAY),
    ('[SerializeField] private float roundTimeout = 3f; // 데드라인', CGREEN),
    ('', WHITE),
    ('void Update()', WHITE),
    ('{', WHITE),
    ('    // 매 프레임 데드라인 초과 여부 확인', CGRAY),
    ('    if (sectorsVisitedCount > 0 &&', WHITE),
    ('        Time.time - roundStartTime > roundTimeout)', CORANGE),
    ('    {', WHITE),
    ('        ResetRound(); // 데드라인 초과 → 태스크 취소 후 재스케줄', CGREEN),
    ('    }', WHITE),
    ('', WHITE),
    ('    // 반시계 방향 전환 감지 → 즉시 페널티 후 재시작', CGRAY),
    ('    if (direction != lastDirection)', WHITE),
    ('    {   onPenaltyEvent.Raise(...);  ResetRound(); }', CORANGE),
    ('}', WHITE),
], 6.5, 1.55, 6.5, 5.7)


# ═══════════════════════════════════════════════════════════
# 8. 6주차 — 수요 페이징 (ResourceLoader)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '6주차', '수요 페이징(Demand Paging) — ResourceLoader.cs',
    '필요한 순간에만 로드하고 사용 후 해제하는 Addressables 에셋 관리')

ml(s, [
    {'t':'수요 페이징이란?', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'모든 데이터를 처음부터 메모리에 올리지 않고', 's':12,'c':WHITE},
    {'t':'실제로 필요한 순간에만 로드(Page In)하고', 's':12,'c':WHITE},
    {'t':'사용이 끝나면 메모리에서 해제(Page Out)한다.', 's':12,'c':WHITE},
], 0.35, 1.05, 5.8, 2.3)

tbl = [
    ('가상 메모리 (OS)',       'ResourceLoader (프로젝트)'),
    ('논리 주소로 접근 시도',  'string address(key)로 로드 요청'),
    ('Page Table 조회',        'ExistsInAddressables() 확인'),
    ('Page Fault → 디스크 로드','LoadAssetAsync<T>() 실행'),
    ('물리 메모리에 Page 배치', 'AsyncOperationHandle 반환'),
    ('Page 교체 (해제)',        'Addressables.Release(handle)'),
]
box(s, 0.35, 3.45, 5.6, 0.38, BLUE)
box(s, 5.98, 3.45, 6.0, 0.38, ORANGE)
t(s, tbl[0][0], 0.5, 3.5, 5.3, 0.28,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
t(s, tbl[0][1], 6.1, 3.5, 5.7, 0.28,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
for i,(a,b) in enumerate(tbl[1:]):
    ry = 3.88 + i*0.48
    bg = CARD if i%2==0 else CARD2
    box(s, 0.35, ry, 5.6, 0.45, bg)
    box(s, 5.98, ry, 6.0, 0.45, bg)
    t(s, a, 0.5, ry+0.08, 5.3, 0.3, sz=11, color=GRAY)
    t(s, b, 6.1, ry+0.08, 5.7, 0.3, sz=11, color=CGREEN)

code(s, [
    ('// ResourceLoader.cs — 수요 페이징 흐름', CGRAY),
    ('bool exists = await ExistsInAddressables(address, token);', CGREEN),
    ('// Page Fault 감지: 없으면 로드 불필요', CGRAY),
    ('', WHITE),
    ('var handle = Addressables.LoadAssetAsync<T>(address); // Page In', CGREEN),
    ('await handle.ToUniTask(cancellationToken: token);', WHITE),
    ('', WHITE),
    ('// 사용 후 해제 — Page Out (메모리 반환)', CGRAY),
    ('Addressables.Release(handle);', CGREEN),
], 0.35, 6.65, 12.63, 0.62)


# ═══════════════════════════════════════════════════════════
# 9. 7주차 — 메모리 풀 (ObjectPool)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '7주차', '메모리 풀(Memory Pool) — ObjectPool.cs',
    '수업 예제 4(C# ObjectPool)와 동일한 구조를 프로젝트에 직접 구현')

# 수업 예제 vs 프로젝트 비교
box(s, 0.35, 1.05, 5.85, 0.38, RGBColor(0x1A,0x30,0x60))
t(s, '수업 예제 (C# ObjectPool)', 0.35, 1.05, 5.85, 0.38,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
box(s, 6.45, 1.05, 6.55, 0.38, RGBColor(0x1A,0x45,0x25))
t(s, '프로젝트 구현 (ObjectPool.cs)', 6.45, 1.05, 6.55, 0.38,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)

code(s, [
    ('class ObjectPool<T> where T : new()', WHITE),
    ('{', WHITE),
    ('    // Stack 기반 풀', CGRAY),
    ('    private Stack<T> _pool = new Stack<T>();', WHITE),
    ('', WHITE),
    ('    public T Get()', WHITE),
    ('    {', WHITE),
    ('        return _pool.Count > 0', WHITE),
    ('            ? _pool.Pop() : new T();', CGREEN),
    ('    }', WHITE),
    ('', WHITE),
    ('    public void Return(T item)', WHITE),
    ('    {   _pool.Push(item); }', CGREEN),
    ('}', WHITE),
], 0.35, 1.48, 5.85, 3.85)

code(s, [
    ('public class ObjectPool', WHITE),
    ('{', WHITE),
    ('    // Queue 기반 풀 (선입선출)', CGRAY),
    ('    private Queue<GameObject> pool', WHITE),
    ('        = new Queue<GameObject>();', WHITE),
    ('', WHITE),
    ('    public GameObject Get()', WHITE),
    ('    {', WHITE),
    ('        var obj = pool.Dequeue();', WHITE),
    ('        obj.SetActive(true);', CGREEN),
    ('        return obj;', WHITE),
    ('    }', WHITE),
    ('    public void Return(GameObject obj)', WHITE),
    ('    {   obj.SetActive(false);', CGREEN),
    ('        pool.Enqueue(obj); }', CGREEN),
    ('}', WHITE),
], 6.45, 1.48, 6.55, 4.2)

box(s, 0.35, 5.45, 12.63, 0.35, CARD)
t(s, '공통 핵심: 사전 할당 → Get/Return으로 재사용 → new/Destroy 없음 → GC Allocation 0회',
  0.5, 5.5, 12.35, 0.28, sz=11, color=YELLOW)

ml(s, [
    {'t':'실제 사용 위치 (PoolManager.cs)', 's':12,'b':True,'c':YELLOW},
    {'t':'', 's':4},
    {'t':'· 대사창 UI 오브젝트 → 대화마다 반복 생성/삭제 대신 풀에서 재사용', 's':11,'c':WHITE},
    {'t':'· 선택지 버튼 UI → 선택지 개수만큼 꺼내고 대화 종료 시 반납', 's':11,'c':WHITE},
    {'t':'· initialSize개 미리 생성 → 게임 중 GC 발동 없음', 's':11,'c':CGREEN},
], 0.35, 5.9, 12.63, 1.4)


# ═══════════════════════════════════════════════════════════
# 10. 7주차 ② — 데이터 지역성 AoS vs SoA (LiquidCell)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '7주차 ②', '데이터 지역성 — LiquidCell.cs (struct)',
    '수업 예제 2(AoS vs SoA)와 연결 — struct로 구현한 설계 의도')

ml(s, [
    {'t':'수업 예제 2: AoS vs SoA', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'AoS(Array of Structures): 객체마다 모든 필드를 가짐', 's':12,'c':WHITE},
    {'t':'SoA(Structure of Arrays): 같은 필드끼리 배열로 분리', 's':12,'c':WHITE},
    {'t':'', 's':5},
    {'t':'SoA가 x만 연속 접근할 때 캐시 효율이 더 높다.', 's':12,'c':CGREEN},
    {'t':'', 's':7},
    {'t':'프로젝트에서의 선택', 's':13,'b':True,'c':YELLOW},
    {'t':'', 's':5},
    {'t':'LiquidCell을 struct(값 타입)로 정의하여', 's':12,'c':WHITE},
    {'t':'LiquidCell[,] 2D 배열 내에 연속 저장 → AoS 방식', 's':12,'c':WHITE},
    {'t':'힙 할당 없음, GC 없음, 포인터 추적 없음', 's':12,'c':CGREEN},
], 0.35, 1.05, 5.8, 5.5)

# AoS vs SoA 시각화
t(s, '수업 예제 AoS', 6.4, 1.05, 3.1, 0.36,
  sz=11, bold=True, color=ORANGE, align=PP_ALIGN.CENTER)
t(s, '프로젝트 LiquidCell (AoS)', 9.7, 1.05, 3.3, 0.36,
  sz=11, bold=True, color=CGREEN, align=PP_ALIGN.CENTER)

aos_fields = ['x','y','z','velocity']
for i in range(3):
    box(s, 6.4+i*0.82, 1.48, 0.75, 1.6, RGBColor(0x1A,0x30,0x50))
    for j,f in enumerate(aos_fields):
        box(s, 6.42+i*0.82, 1.5+j*0.38, 0.71, 0.35,
            RGBColor(0x0F,0x3A,0x78) if j%2==0 else RGBColor(0x1A,0x4A,0x8A))
        t(s, f, 6.42+i*0.82, 1.53+j*0.38, 0.71, 0.28,
          sz=8, color=WHITE, align=PP_ALIGN.CENTER)
t(s, 'Particle[0]  Particle[1]  Particle[2]',
  6.4, 3.12, 2.5, 0.32, sz=8, color=GRAY)

lc_fields = ['liquidType','color','density','mixRatio']
lc_colors = [BLUE, GREEN, ORANGE, RGBColor(0x9B,0x59,0xB6)]
for i in range(3):
    box(s, 9.7+i*0.82, 1.48, 0.75, 1.6, RGBColor(0x1A,0x35,0x1A))
    for j,(f,c) in enumerate(zip(lc_fields, lc_colors)):
        box(s, 9.72+i*0.82, 1.5+j*0.38, 0.71, 0.35, c)
        t(s, f[:6], 9.72+i*0.82, 1.53+j*0.38, 0.71, 0.28,
          sz=7, color=WHITE, align=PP_ALIGN.CENTER)
t(s, 'Cell[0,0]    Cell[1,0]    Cell[2,0]',
  9.7, 3.12, 2.5, 0.32, sz=8, color=GRAY)

code(s, [
    ('// LiquidCell.cs — class 대신 struct (AoS 방식)', CGRAY),
    ('public struct LiquidCell  // 힙 할당 없음, GC 없음', CGREEN),
    ('{', WHITE),
    ('    public LiquidType liquidType; // 4 bytes', WHITE),
    ('    public Color32    color;      // 4 bytes', WHITE),
    ('    public float      density;    // 4 bytes', WHITE),
    ('    public float      mixRatio;   // 4 bytes', WHITE),
    ('    public float      velocityX, velocityY;', WHITE),
    ('}', WHITE),
    ('// LiquidGrid.cs', CGRAY),
    ('public LiquidCell[,] Cells;  // 연속 2D 배열', CGREEN),
    ('ref LiquidCell c = ref Cells[x, y]; // 복사 없이 직접 접근', CGREEN),
], 0.35, 3.6, 12.63, 3.65)

box(s, 0.35, 7.28, 12.63, 0.17, CARD)
t(s, '  ※ LiquidCell/LiquidGrid는 현재 게임 미사용 — 데이터 지역성을 고려한 설계 의도로 발표',
  0.5, 7.3, 12.35, 0.14, sz=9, color=GRAY)


# ═══════════════════════════════════════════════════════════
# 11. 7주차 ③ — 캐시 미스 패턴 발견 (LiquidRenderer)
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, '7주차 ③', '캐시 일관성 — LiquidRenderer.cs 루프 순서',
    '실제 코드에서 발견한 캐시 비친화적 접근 패턴과 개선 방향')

t(s, 'C# 2D 배열은 행 우선(Row-Major) 저장',
  0.35, 1.05, 12.63, 0.4, sz=14, bold=True, color=YELLOW)
t(s, 'array[x, y] 에서 x가 행(row) — x가 바뀔 때 메모리 주소가 Width만큼 점프 → 캐시 미스',
  0.35, 1.48, 12.63, 0.36, sz=12, color=WHITE)

box(s, 0.35, 1.9, 6.05, 0.36, ORANGE)
t(s, '현재 코드 — 캐시 비친화적', 0.35, 1.9, 6.05, 0.36,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
code(s, [
    ('// LiquidRenderer.cs (현재)', CGRAY),
    ('for (int x = 0; x < gridWidth; x++)',  CORANGE),
    ('    for (int y = 0; y < gridHeight; y++)', CORANGE),
    ('    {', WHITE),
    ('        int idx = y * gridWidth + x;', WHITE),
    ('        buf[idx] = cells[x, y].color;', CORANGE),
    ('        // x 고정, y 변화', CGRAY),
    ('        // → 메모리 주소가 gridWidth씩 점프', CGRAY),
    ('        // → 캐시 미스 반복!', CORANGE),
    ('    }', WHITE),
], 0.35, 2.3, 6.05, 2.8)

box(s, 6.6, 1.9, 6.38, 0.36, GREEN)
t(s, '개선 코드 — 캐시 친화적', 6.6, 1.9, 6.38, 0.36,
  sz=11, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
code(s, [
    ('// 루프 순서만 변경', CGRAY),
    ('for (int y = 0; y < gridHeight; y++)',  CGREEN),
    ('    for (int x = 0; x < gridWidth; x++)', CGREEN),
    ('    {', WHITE),
    ('        int idx = y * gridWidth + x;', WHITE),
    ('        buf[idx] = cells[x, y].color;', CGREEN),
    ('        // y 고정, x 변화', CGRAY),
    ('        // → 메모리 주소가 1씩 순차 증가', CGRAY),
    ('        // → 캐시 히트!', CGREEN),
    ('    }', WHITE),
], 6.6, 2.3, 6.38, 2.8)

# 메모리 배치 시각화
t(s, '메모리 주소 배치 (Width=4 예시)', 0.35, 5.22, 8, 0.36,
  sz=11, bold=True, color=YELLOW)
addrs = [('[0,0]','0'),('[0,1]','4'),('[0,2]','8'),('[0,3]','12'),
         ('[1,0]','1'),('[1,1]','5'),('[1,2]','9'),('[1,3]','13')]
for i,(label,addr) in enumerate(addrs):
    clr = BLUE if i < 4 else RGBColor(0x1A,0x4A,0x1A)
    box(s, 0.35+i*1.55, 5.62, 1.43, 0.72, clr)
    t(s, label, 0.35+i*1.55, 5.66, 1.43, 0.32,
      sz=9, color=WHITE, align=PP_ALIGN.CENTER)
    t(s, f'addr {addr}', 0.35+i*1.55, 5.98, 1.43, 0.28,
      sz=8, color=GRAY, align=PP_ALIGN.CENTER)

t(s, '현재(x고정): [0,0]→[0,1]→[0,2] 주소 0→4→8 (4칸 점프) → 캐시 미스',
  0.35, 6.42, 12.63, 0.36, sz=11, color=CORANGE)
t(s, '개선(y고정): [0,0]→[1,0]→[2,0] 주소 0→1→2 (연속) → 캐시 히트',
  0.35, 6.82, 12.63, 0.36, sz=11, color=CGREEN)


# ═══════════════════════════════════════════════════════════
# 12. 종합 정리
# ═══════════════════════════════════════════════════════════
s = slide()
hdr(s, None, '종합 정리 — 주차별 구현 요약', '')

summary = [
    (BLUE,  '2주차','시스템 콜',
     'JsonManager.cs',
     'File.ReadAllText() → OS 파일 I/O 시스템 콜\nDataLoadManager에서 게임 시작 시 7개 JSON 로드'),
    (GREEN, '3주차','비동기 스레드',
     'ResourceLoader.cs',
     'UniTask async/await → 백그라운드 에셋 로드\nCancellationToken으로 스레드 안전 종료'),
    (RGBColor(0x9B,0x59,0xB6),'4주차','세마포어',
     'ShakingManager.cs',
     'UniTaskCompletionSource → Wait/Signal 패턴\n미니게임 완료 신호를 세마포어처럼 주고받음'),
    (ORANGE,'5주차','EDF 스케줄링',
     'CircularGestureDetector.cs',
     'roundTimeout = 데드라인 → 초과 시 ResetRound()\n매 프레임 Update()에서 데드라인 체크'),
    (RGBColor(0x16,0xA0,0x85),'6주차','수요 페이징',
     'ResourceLoader.cs',
     'Addressables Load/Release → Page In / Page Out\n필요 시 로드, 씬 전환 시 일괄 해제'),
    (YELLOW,'7주차 ①','메모리 풀',
     'ObjectPool.cs + PoolManager.cs',
     'Queue 사전 할당 → Get/Return 재사용\n수업 예제 4와 동일한 구조, GC 0회'),
    (YELLOW,'7주차 ②','데이터 지역성',
     'LiquidCell.cs',
     'struct(값 타입) + 연속 2D 배열 → AoS 방식\n수업 예제 2(AoS vs SoA) 설계 의도 반영'),
    (YELLOW,'7주차 ③','캐시 일관성',
     'LiquidRenderer.cs',
     '루프 순서(x↔y) 변경 → 캐시 미스 개선\n코드에서 직접 발견한 캐시 비친화적 패턴'),
]

for i,(clr,wk,concept,file,desc) in enumerate(summary):
    ry = 1.05 + i*0.8
    bg = CARD if i%2==0 else CARD2
    box(s, 0.3, ry, 12.73, 0.74, bg)
    box(s, 0.3, ry, 0.07, 0.74, clr)
    t(s, wk,     0.45, ry+0.05, 1.2,  0.34, sz=11, bold=True,
      color=clr, align=PP_ALIGN.CENTER)
    t(s, concept,1.7,  ry+0.15, 1.9,  0.34, sz=11, bold=True, color=WHITE)
    t(s, file,   3.7,  ry+0.15, 3.4,  0.34, sz=9,  color=CGREEN, italic=True)
    for j,line in enumerate(desc.split('\n')):
        t(s, line, 7.2, ry+0.04+j*0.33, 5.75, 0.3, sz=9.5, color=GRAY)


# ═══════════════════════════════════════════════════════════
# 13. 결론
# ═══════════════════════════════════════════════════════════
s = slide()
box(s, 8.2, 0, 5.13, 7.5, RGBColor(0x0B,0x13,0x22))
box(s, 0, 0, 13.33, 0.1, ORANGE)
box(s, 0, 7.4, 13.33, 0.1, ORANGE)

t(s, '결론', 0.8, 1.1, 7, 0.85, sz=42, bold=True, color=WHITE)
box(s, 0.8, 2.05, 5.8, 0.05, ORANGE)

t(s, '"수업에서 배운 OS 개념들이\n이미 코드 안에 구현되어 있었다"',
  0.8, 2.2, 7.2, 1.1, sz=17, bold=True, color=YELLOW)

points = [
    (BLUE,  '시스템 콜',    'JsonManager — File.ReadAllText()'),
    (GREEN, '비동기 스레드','ResourceLoader — UniTask async/await'),
    (RGBColor(0x9B,0x59,0xB6),'세마포어','ShakingManager — TrySetResult()'),
    (ORANGE,'EDF 스케줄링', 'CircularGestureDetector — roundTimeout'),
    (RGBColor(0x16,0xA0,0x85),'수요 페이징','ResourceLoader — Load/Release'),
    (YELLOW,'메모리 풀',      'ObjectPool — Queue 사전 할당'),
    (YELLOW,'데이터 지역성',  'LiquidCell — struct AoS 설계'),
    (YELLOW,'캐시 일관성',    'LiquidRenderer — 루프 순서 개선'),
]
for i,(clr,concept,code_ref) in enumerate(points):
    ry = 3.5 + i*0.62
    box(s, 0.8, ry, 0.06, 0.52, clr)
    t(s, concept,  1.0, ry+0.06, 2.1, 0.4, sz=12, bold=True, color=clr)
    t(s, code_ref, 3.2, ry+0.06, 4.4, 0.4, sz=12, color=WHITE)

t(s, '감사합니다', 9.0, 3.3, 4.0, 0.75,
  sz=26, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
box(s, 9.3, 4.15, 3.4, 0.05, ORANGE)
t(s, '최용근', 9.0, 4.3, 4.0, 0.42,
  sz=13, color=GRAY, align=PP_ALIGN.CENTER)


out = r'D:\Unity Project\Unity 6000.0.47f1\HumanBartender\게임운영체제_중간.pptx'
prs.save(out)
print(f'완료: {out}  /  슬라이드 수: {len(prs.slides)}')
