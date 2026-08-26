# 98.Claude — 컷씬 시스템 프로토타입

노션 「컷씬 시스템」 문서의 구현 기준을 검증하는 **완전 독립 샌드박스**다.
이 폴더 밖의 어떤 코드·에셋·데이터도 참조하지 않는다 (엔진 코드·`Document/` 파이프라인·아트·폰트 전부 미사용).
지우고 싶으면 `Assets/98.Claude` 폴더만 삭제하면 된다.

## 실행

1. `Scenes/C98_CutsceneLab.unity` 열기 → Play.
   (씬 파일이 열리지 않으면 메뉴 `Tools → 98.Claude → 컷씬 프로토타입 씬 재생성`)
2. 대기 화면에서 **Enter** → 컷씬 `cs_lab_raid_01`(연구소 습격 — 루나의 꿈) 재생.

| 키 | 동작 |
|---|---|
| Enter | 컷씬 재생 |
| Space / 클릭 / E | 대사 진행 (타이핑 중 입력 = 문장 즉시 전체 표시) |
| 1~3 | 선택지 선택 (마우스 클릭도 가능) |
| S | 스킵 — 선택지 표시 중에는 차단 |
| L | 한국어 ↔ English |
| R | 진행 초기화 (완료 기록·플래그) |
| F1 | 디버그 패널 (상태·스텝·타임라인 클록·플래그·배속) |

## 컷씬 직접 만들기 (Timeline 시각 편집)

코드·JSON을 건드리지 않고 Unity Timeline 창에서 드래그로 컷씬을 만드는 흐름:

1. **`Tools → 98.Claude → 1. 무대를 씬에 배치`** — 연구소 무대(배우·앵커·조명)가 씬에 생긴다. 씬을 저장하면 유지된다.
2. **`Tools → 98.Claude → 2. 새 컷씬 타임라인 만들기`** — 샘플 클립이 채워진 타임라인이 만들어지고 Timeline 창이 열린다.
3. Timeline 창에서 **클립을 드래그해 시점·길이를 조절**하고, **스크럽 바를 긁으면 무대가 그 시점 상태로 미리보기**된다.
   - 클립을 선택하면 인스펙터에서 대상 id(`luna`, `cam_door`, `lab_door`…)를 바꿀 수 있다 — 앵커 id는 씬 뷰 기즈모에 표시된다.
   - 우클릭 → Add C98 Event Marker: 대사(`sc_*` 씬 id)·SFX·페이드·흔들림. 마커는 미리보기가 없고 런타임에만 실행된다.
4. **Play → T키** → 방금 만든 타임라인을 골라 바로 재생(레터박스·스킵·상태 머신 전부 적용).
5. 미리보기로 무대가 어질러지면 **`3. 무대 상태 초기화`**.

트랙 4종: 배우 이동(파랑) / 카메라(주황) / 오브젝트 슬라이드(초록) / 조명 프리셋(빨강) + 이벤트 마커.
파일명(`tl_*`)이 곧 `timeline_key`다 — 컷씬 JSON의 timeline 스텝과 같은 키면 **에셋이 JSON보다 우선** 재생된다.
스크럽을 앞뒤로 긁을 때 정확한 미리보기를 원하면 이동 클립의 `fromAnchor`를 명시할 것.

## 폴더 구성

```
98.Claude/
├─ Scenes/C98_CutsceneLab.unity      씬 — C98_Bootstrap 하나뿐 (무대·카메라·UI 전부 코드 생성)
├─ Resources/Claude98/cutscene_lab.json   컷씬 데이터 (아래 구조)
└─ Scripts/
   ├─ C98_Bootstrap.cs               진입점 — 무대 구축·데이터 로드·키 입력
   ├─ C98_Data.cs                    데이터 모델 + 로더 + 검증기 (실패 시 재생 거부)
   ├─ C98_Manager.cs                 CutsceneManager(상태 머신·시퀀스 실행기) + MicroTimeline 실행기
   ├─ C98_Stage.cs                   바인딩 레지스트리 + 배우/오브젝트/앵커 + 무대 구성
   ├─ C98_DialogueUI.cs              말풍선(월드 추적)·타이핑·선택지·bark
   ├─ C98_Fx.cs                      카메라 연출·페이드/플래시·조명 프리셋·사운드 스텁
   ├─ C98_DebugPanel.cs              F1 디버그 패널
   └─ Editor/C98_SceneTools.cs       씬 재생성 메뉴
```

## 문서 요구사항 → 구현 매핑

| 문서 항목 | 구현 위치 | 비고 |
|---|---|---|
| JSON 시퀀스 + Timeline 혼합 구조 | `cutscene_lab.json → sequence[]` + `timelines[]` | timeline 스텝 = MicroTimeline (아래 참고) |
| 상태 머신 9종 (Idle~Recovering) | `C98_Manager.cs → C98_State` | 전환은 매니저만 수행 |
| CutsceneContext (런타임 상태, 저장 안 함) | `C98_Context` | 종료 시 폐기 |
| 바인딩 레지스트리 (문자열 검색 금지, ID 등록) | `C98_BindingRegistry` | 중복 ID = 오류, 필수 바인딩 누락 = 재생 중단 |
| Step 6종 timeline/dialogue/choice/command/wait/branch | `C98_Manager.cs → PlayCo` | scene_transition은 단일 씬이라 제외 |
| wait_mode = PauseTimeline | `tl_lab_entry`의 dialogue 이벤트 | 대사 완료 후 같은 클록에서 재개 |
| event_key 중복 실행 차단 | `C98_Context.fired_event_keys` | 공란이면 키 자동 생성 |
| fire_on_skip 필수 이벤트 | 경보등 프리셋·문 열림 | 스킵해도 실행됨 |
| 스킵 시 명시적 end_state 적용 | `ApplyEndState` — 정상 종료와 동일 코드 | 어느 지점에서 스킵해도 같은 최종 상태 |
| 선택 이전 스킵 차단 | `RequestSkip` — ShowingChoice면 토스트 | |
| play_mode=once 중복 재생 차단 | `Play` 진입 조건 | R로 초기화 가능 |
| 컷씬 종료 이벤트 1회 보장 | `completionFired` | |
| 카메라 이동·줌·흔들림 + 시작 상태 복원 | `C98_CameraDirector` (temporary_state) | 실전은 Cinemachine Track |
| 조명 프리셋 (하드코딩 금지) | `C98_ScreenFx.SetLightPreset` — light_default/light_lab_alarm/light_blackout | |
| 말풍선 월드 앵커 추적·화면 밖 보정·앵커 부재 시 screen 폴백+경고 | `C98_DialogueUI` | |
| 대사 채널 main/bark/narration | 〃 | bark는 타임라인을 멈추지 않음, 동시 2개 제한 |
| 타이핑 40ms·입력 시 전체 표시·▼ 대기 아이콘·auto 진행 | 〃 | |
| 데이터 검증 실패 = 재생 거부 | `C98_Loader.Validate` → DATA_ERROR 패널 | 참조 무결성·L10N·선택지 규칙·분기 대상 |
| 디버그 패널 (상태·스텝·클록·배속·스킵) | `C98_DebugPanel` | |
| 시네마틱 진입·종료 연출 | `C98_ScreenFx` 레터박스 + `C98_CameraDirector.ZoomByFactor` | 시작 시 위아래 각 10% 검은 바가 차오르며 카메라 7% 줌인, 종료·스킵 시 걷히며 복귀. 말풍선·내레이션은 바 안쪽으로 보정 |

## 프로젝트 컨벤션 반영 (기획 파이프라인과의 접점)

- id 전부 소문자 스네이크 (`cs_lab_raid_01`, `tl_lab_entry`, `luna`, `hound`)
- 대사·선택지 텍스트 전부 **ko/en 쌍** — en 누락은 검증 오류
- 선택지 잠금 = 숨기지 않고 **회색 + lock_reason** (`when` ↔ `lock_reason_ko/en` 상호 필수 검증)
- 분기 조건 = when 문자열 (`flag.x` / `!flag.x`) — 실전은 when DSL 18계열 재사용 전제
- 대사는 별도 line_id 링크드리스트가 아니라 **씬(steps) 단위** — 기존 Scenes/Steps 모델과 같은 형태
- 좌표는 JSON에 넣지 않고 **씬의 앵커만 참조** (`a_*`, `cam_*`)

## 의도적 단순화 (실전 교체 지점)

| 프로토타입 | 실전 |
|---|---|
| MicroTimeline (코드 실행기) | **Unity Timeline + PlayableDirector** — 에디터 없이 Timeline 에셋을 만들 수 없어 같은 계약(t 배치·wait_mode·fire_on_skip·event_key)을 코드로 재현했다 |
| 사각형/원 런타임 스프라이트 | 실제 캐릭터·배경 아트 |
| OS 폰트 동적 로드 | `06.Fonts`의 프로젝트 폰트 |
| 절차 생성 효과음·BGM 스텁 | AudioManager + 발주 음원 |
| 카메라 직접 제어 | Cinemachine |
| 플래그 인메모리 저장 | 세이브 시스템 연동 |
| Resources 로드 | Addressables |

## 검증 상태

- 스크립트 8종: Unity 6000.3.14 동봉 DLL 대상 외부 컴파일 **오류 0·경고 0**
- `cutscene_lab.json`: 검증기 로직 전수 통과 (참조·L10N·선택지 규칙·분기 경로 hide/run 양쪽 시뮬레이션)
- 에디터 내 실행은 미확인 — 첫 실행 시 콘솔에 `[C98]` 로그로 상태 전환이 찍힌다
