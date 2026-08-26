# Project L.U.N.A 컷씬 제작 도구 사용 가이드

이 도구는 Unity Timeline을 중심으로 컷씬을 직접 조립하기 위한 독립 프로토타입이다. 캐릭터·카메라 애니메이션은 Timeline 클립으로 배치하고, 대사·화면 효과·스프라이트 교체는 전용 마커로 같은 시간축에 배치한다.

모든 테스트 파일은 `Assets/99.CutsceneProto/` 안에 있으며 기존 게임 컷씬 시스템이나 데이터에는 연결하지 않는다.

## 가장 빠른 시작 방법

1. `CutsceneAuthoring_Lab.unity`를 연다.
2. Unity 상단 메뉴에서 `Window > Project L.U.N.A > Cutscene Authoring Studio`를 연다.
3. `1. 씬·배우`에서 Hierarchy의 배우를 선택하고 `선택 배우 빠른 설정`을 누른다.
4. `2. 이동·마커`에서 도착 앵커를 만들고 Move Track에 이동 클립을 배치한다.
5. 같은 단계에서 제자리 픽셀 AnimationClip과 `＋ 대사`, `＋ 효과`, `＋ 스프라이트` 마커를 배치한다.
6. `3. 미리보기`에서 전체·구간·프레임 단위 재생을 확인한다.
7. 컷씬 마지막 상태를 무대에 만든 뒤 `4. 완료·검사`에서 종료 상태를 캡처한다.
8. `현재 씬 저장 + Timeline 베이크 + 전체 검사`를 누른다.
9. 오류가 없으면 정상 재생과 스킵을 각각 검증한다.

예제 씬은 연구소 침입 상황을 짧게 구성해 둔 제작 샘플이다. 기존 클립을 옮기거나 길이를 바꾸면서 Timeline과 대사가 함께 어떻게 진행되는지 바로 확인할 수 있다.

## 제작 화면의 역할

### Timeline 창

실제 컷씬의 시간과 연출 순서를 조립하는 공간이다.

- Move Track: 앵커 사이로 배우 위치를 옮기는 컷씬 전용 클립
- Animation Track: 배우의 제자리 픽셀 동작과 카메라 이동 연출
- Audio Track: BGM과 환경음, 긴 오디오 클립
- Marker Track: 대사, 즉시 효과, 스프라이트 교체
- 재생 헤드: 새 마커가 삽입될 정확한 시간

애니메이션을 수정해도 대사 코드를 다시 작성할 필요가 없다. Timeline에서 클립과 마커의 위치만 옮기면 된다.

### Cutscene Authoring Studio

Timeline 제작 중 반복되는 설정을 한곳에서 처리한다.

- 예제 제작 씬 생성 또는 열기
- 빈 Timeline·Definition 생성
- 현재 재생 헤드에 전용 마커 추가
- Scene 오브젝트에 고정 Binding ID 등록
- 배우별 Move·Animation·Visibility Track 세트 생성 및 자동 바인딩
- 앵커 생성과 이동 클립 자동 연결
- 전체·구간 반복·배속·프레임 단위 편집 미리보기
- Scene View의 앵커·이동 경로·카메라 16:9·레터박스 가이드
- TMP 말풍선 Prefab·Style 자동 생성·복구와 배우별 고정 SpeechAnchor 배치
- 현재 배우 상태를 Definition 종료 상태로 캡처
- Timeline 마커를 런타임 데이터로 베이크
- 누락된 번역, ID·앵커 중복, 잘못된 대상 참조, 이동 클립 겹침, 위험한 스킵 설정 검사

창 상단은 `1. 씬·배우` → `2. 이동·마커` → `3. 미리보기` → `4. 완료·검사`의 네 단계로 나뉜다. 현재 배우 세트·바인딩·앵커·마커 수와 다음 권장 작업을 함께 표시하므로 긴 도구 목록을 위아래로 찾을 필요가 없다.

`Hierarchy 선택을 대상 오브젝트에 자동 반영`을 켜면 Scene에서 고른 GameObject가 Studio의 작업 대상으로 자동 지정된다. 아직 ID가 없으면 오브젝트 이름으로 Binding ID를 제안한다.

### 배우 트랙 세트와 앵커 이동

`배우 트랙 세트 생성`은 선택한 배우에 다음 세 트랙을 한 번에 만든다.

- Move: 시작 앵커에서 도착 앵커까지 배우의 위치를 이동
- Animation: 걷기·달리기·웅크리기·피격처럼 제자리에서 재생되는 픽셀 AnimationClip
- Visibility: 컷씬 중 등장·퇴장 구간

위치는 Move Track만, 픽셀 동작은 제자리 AnimationClip만 담당한다. 두 트랙이 동시에 Transform 위치를 쓰면 값이 충돌하므로 Validator는 배우 AnimationClip에 위치 커브가 있을 때 경고한다.

`선택 배우 빠른 설정`은 Binding ID 등록, Move·Animation·Visibility 트랙 연결, 현재 위치의 `{binding_id}_start` 앵커 준비를 한 번에 처리한다. 완료 후 다음 도착 앵커 이름을 `{binding_id}_end`로 제안하고 `2. 이동·마커` 단계로 이동한다.

빠른 설정은 배우 자식에 `SpeechAnchor` 오브젝트와 `LunaCutsceneSpeechAnchor`를 함께 추가한다. 최초 위치는 현재 스프라이트 상단으로 캡지만, 실행 중에는 스프라이트 크기를 매 프레임 다시 계산하지 않고 고정된 자식 Transform을 사용한다. 표정·자세 스프라이트가 바뀌어도 말풍선 기준점이 흔들리지 않는 구조다.

Studio의 `말풍선 앵커 위치 다시 캡기`는 현재 스프라이트 상단으로 앵커를 재배치한다. 자동 배치 후 배우별로 더 높게 또는 왼쪽·오른쪽으로 조정하려면 Hierarchy의 `SpeechAnchor` 자식을 직접 옮긴다.

### TMP 말풍선 Prefab과 Style

말풍선 UI는 Scene에 고정 패널로 복제하지 않고 `Authoring/UI/CutsceneSpeechBubble.prefab`을 대사 출력 시 생성한다. 텍스트는 모두 TextMeshProUGUI를 사용하며, 기본 폰트는 `NeoDunggeunmo SDF` 에셋이다.

`Authoring/UI/CutsceneSpeechBubbleStyle.asset`에서 아래 표시 값을 조정한다.

- 화자·본문·입력 안내의 TMP 폰트, 글자 크기, 색, 행간
- 배경과 말풍선 꼬리 색
- 최소·최대 가로/세로 크기
- 좌우·상하 여백과 화자·본문·입력 안내 사이 간격
- 화자 기준 화면 오프셋, 말풍선 꼬리 크기, 안전 영역 여백

대사가 시작되면 타이핑 전에 **해당 언어의 전체 대사**를 TMP로 먼저 측정한다. 측정한 가로 크기를 최소·최대 범위로 제한한 뒤 줄바꿈 높이를 계산하고, 말풍선 크기를 한 번만 확정한다. 타이핑 중에는 `maxVisibleCharacters`만 변경하므로 글자가 나올 때마다 말풍선이 늘거나 줄지 않는다.

말풍선은 화자를 향하는 꼬리를 표시하고, 화면 가장자리에서는 Safe Area 안쪽으로 위치를 보정한다. 위치 보정 후에도 꼬리는 실제 화자 앵커를 가리킨다. 카메라가 교체되면 매 프레임 현재 활성 카메라를 다시 확인해 새 화면 좌표로 즉시 갱신한다.

Prefab 참조가 끊겼거나 구형 `UI.Text` 패널이 남았으면 Studio 1단계의 `UI 프리팡·스타일 생성/복구`를 누른다. 이 기능은 프리팡과 Style을 복구하고 현재 컷씬 Scene을 TMP 구조로 교체한다.

앵커는 `luna_start`, `luna_console`, `intruder_entry`처럼 장면 안에서 재사용할 위치 ID다. Scene View에서 앵커를 옮기면 해당 앵커를 참조하는 이동 클립의 경로도 함께 변한다.

1. 첫 이동에는 시작과 도착 앵커를 모두 선택한다.
2. 두 번째 이후는 `이전 이동에서 자동으로 잇기`를 켜 직전 클립의 도착 앵커를 시작점으로 쓴다.
3. `이동 속도`와 앵커 간 거리로 클립 길이를 자동 계산한다.
4. `Facing Mode = Auto`는 이동 방향에 따라 `SpriteRenderer.flipX`를 적용한다.

시작·도착 앵커는 Studio의 ID 목록에서 고를 수 있다. `시작 ↔ 도착 바꾸기`와 `도착 앵커 보기`로 방향 수정과 Scene 포커스를 빠르게 처리한다. 같은 앵커 ID를 다시 만들려고 하면 생성 전에 차단한다.

### 편집 미리보기

- 현재 시간: Timeline을 즉시 평가해 무대 상태를 보여 준다.
- 1프레임 이동: Timeline 프레임 레이트 기준으로 앞뒤으로 이동한다.
- 구간 반복·배속: 특정 장면만 반복하거나 0.1배부터 4배까지 확인한다.
- 정지·원상복구: 미리보기 전의 배우 Transform으로 돌린다.
- Scene 가이드: 앵커, 선택 이동 경로, 카메라 16:9 프레임과 상하단 10% 레터박스 선을 보여 준다.

편집 미리보기는 시각 트랙을 빠르게 다듬는 기능이다. 대사 정지·입력 재개, 런타임 플래그, 스킵 결과는 Play 모드 자동 검증으로 확인한다.

### Cutscene Definition

Timeline 외부에서 컷씬 하나의 공통 규칙을 설정하는 에셋이다.

- `cutsceneId`: 저장과 중복 재생 판정에 사용하는 고정 ID
- `titleKo`, `titleEn`: 제작자 확인용 한·영 제목
- `playMode`: 한 번만 재생하거나 반복 재생
- `autoPlay`: Scene 시작과 동시에 자동 재생할지 여부
- `skippable`: 스킵 허용 여부
- `startFromBlack`: 검은 화면에서 시작할지 여부
- `requiredWhen`: 컷씬 재생 조건
- `endBindings`: 정상 종료와 스킵 종료 후 반드시 남아야 할 무대 상태
- `completionFlag`: 정상 종료와 스킵 종료가 공통으로 기록하는 완료 플래그

`bakedEvents`와 `bakedTimelineDuration`은 Studio가 자동 생성한다. 직접 편집하지 않는다.

## 전용 마커 사용법

### 대사 마커

Timeline의 해당 지점에서 대사창을 열고 필요하면 Timeline을 일시정지한다.

- `dialogueId`: 읽음 기록과 중복 검증에 사용하는 고정 ID
- `speakerId`: 화자 데이터와 연결할 영문 ID
- `speakerKo`, `speakerEn`: 한·영 화자 이름
- `textKo`, `textEn`: 한·영 대사
- `pauseTimeline`: 대사 중 Timeline을 멈출지 여부
- `advanceMode`: 플레이어 입력 또는 자동 진행
- `autoDelay`: 자동 진행 대사가 모두 출력된 뒤 기다릴 시간

일반적인 컷씬 대사는 `pauseTimeline = true`, `advanceMode = PlayerInput`을 사용한다. 플레이어가 첫 입력을 하면 타이핑 중인 문장을 즉시 완성하고, 다음 입력에서 대사창을 닫은 뒤 정확히 멈춘 시간부터 Timeline을 재개한다.

### 효과 마커

코드를 새로 작성하지 않고 컷씬에서 자주 쓰는 효과와 상태 변경을 호출한다.

- FadeIn / FadeOut
- Flash
- CameraShake
- SetActive
- SetSpriteColor
- PlaySfx
- SetFlag

`targetId`가 필요한 효과는 Scene의 Binding Registry에 등록된 ID를 사용한다. `eventKey`는 같은 효과가 한 번의 재생 중 중복 실행되는 것을 막는 ID다.

`fireOnSkip`은 스킵해도 반드시 적용돼야 하는 상태 변경에만 사용한다. 예를 들어 컷씬 완료 플래그, 문이 열린 최종 상태, 등장한 캐릭터의 최종 위치처럼 이후 플레이에 영향을 주는 결과가 대상이다. 화면 흔들림이나 순간 플래시에는 사용하지 않는다.

### 스프라이트 교체 마커

지정한 Binding ID의 `SpriteRenderer`에 새 스프라이트를 적용한다. 실제 SpriteRenderer가 등록 루트의 자식에 있어도 자동으로 찾는다. 정지 이미지 교체, 표정 교체, 모니터 화면 전환 등에 사용한다.

- `targetId`: 교체할 Scene 오브젝트의 Binding ID
- `sprite`: 적용할 Sprite
- `flipX`: 좌우 반전 여부
- `fireOnSkip`: 스킵 후에도 이 모습이 최종 상태로 남아야 할 때만 사용
- `eventKey`: 중복 실행 방지 ID

연속된 프레임 애니메이션은 이 마커를 반복 배치하지 말고 AnimationClip을 만들어 Animation Track에 배치한다.

## 오브젝트와 트랙 연결

Timeline 트랙 바인딩과 컷씬 Binding ID는 역할이 다르다.

- Timeline 트랙 바인딩: AnimationClip이나 AudioClip을 어떤 Animator·AudioSource가 재생할지 연결
- Binding ID: 효과 마커, 스프라이트 마커, 종료 상태가 어떤 GameObject를 찾아야 하는지 연결

Studio에서 대상 오브젝트를 지정하고 `선택 오브젝트를 Binding Registry에 등록`하면 고정 ID 컴포넌트가 붙는다. 같은 컷씬 Scene 안에서는 ID가 중복되면 안 된다.

이미 다른 오브젝트가 쓰는 Binding ID는 등록 단계에서 바로 차단한다. 대사·효과·스프라이트 마커를 같은 시간에 여러 개 만들더라도 `dialogueId`와 `eventKey`에는 자동으로 고유한 접미사가 붙는다. 현재 배우가 등록되어 있으면 새 대사와 스프라이트 마커의 화자·대상 ID도 그 배우를 기본값으로 사용한다.

효과·스프라이트 마커와 Definition 종료 상태의 `targetId`는 현재 Scene에 등록된 Binding ID 드롭다운을 사용한다. 삭제되었거나 미등록된 기존 ID는 `⚠`로 표시된다.

그다음 대상 오브젝트를 유지한 채 `Animation Track 생성 + 연결`을 누르면 Animator와 연결된 새 트랙이 만들어진다. 실제 픽셀 애니메이션 클립을 해당 트랙으로 드래그해 배치한다.

### 종료 상태 캡처

컷씬을 정상 재생했을 때와 중간에 스킵했을 때의 무대 결과는 같아야 한다. 배우를 원하는 상태로 배치한 뒤 `선택 배우의 현재 상태를 종료 상태로 캡처`를 누르면 활성 상태, 로컬 위치, 현재 스프라이트를 Definition에 저장한다. 같은 Binding ID가 이미 있으면 중복 추가하지 않고 기존 값을 갱신한다. 여러 배우를 마지막 위치에 둔 뒤 `등록된 모든 배우의 종료 상태 캡처`를 사용하면 한 번에 저장할 수 있다.

## 기존 예제 자산을 실제 리소스로 바꾸는 방법

1. 예제 Scene을 복제하거나 `빈 에셋 만들기`로 새 Timeline과 Definition을 만든다.
2. 임시 도형 오브젝트 대신 실제 연구소 배경과 캐릭터 SpriteRenderer를 Scene에 둔다.
3. 오브젝트마다 변하지 않는 Binding ID를 등록한다.
4. 실제 픽셀 AnimationClip을 Animation Track에 배치한다.
5. 대사와 효과 마커를 재생 시간에 맞춰 옮긴다.
6. Definition의 종료 상태를 실제 컷씬 결과에 맞게 설정한다.
7. `현재 씬 저장 + Timeline 베이크 + 전체 검사` 후 정상 재생과 스킵 재생을 각각 확인한다.

컷씬을 수정하더라도 다른 데이터에서 참조할 `cutsceneId`, 대사의 `dialogueId`, 오브젝트의 Binding ID는 함부로 바꾸지 않는다.

## 테스트 조작

- `Space`, `Enter`, 마우스 왼쪽 클릭: 대사 완성·다음 대사
- `S`: 컷씬 안전 스킵
- `R`: 테스트 상태 초기화 후 처음부터 다시 재생
- `L`: 한국어·영어 표시 전환

## 완료 전 검사 목록

- [ ] Timeline과 Definition이 같은 컷씬을 가리킨다.
- [ ] 모든 대사에 고유한 `dialogueId`가 있다.
- [ ] 모든 캐릭터 대사의 `speakerId`가 Binding Registry의 배우 ID와 연결된다.
- [ ] 모든 화자 배우에 자식 `SpeechAnchor`가 있고 위치가 머리 위에 맞다.
- [ ] 배우가 이동하는 동안 말풍선과 꼬리가 화자를 따라가고 화면 밖으로 잘리지 않는다.
- [ ] 타이핑 시작 전 전체 대사 기준으로 크기가 확정되고, 타이핑 중 가로·세로 크기가 변하지 않는다.
- [ ] 카메라를 전환해도 말풍선이 새 카메라 화면 좌표를 따라간다.
- [ ] 모든 대사에 한국어와 영어가 들어 있다.
- [ ] 모든 효과·스프라이트의 `targetId`가 Binding Registry에 존재한다.
- [ ] 같은 Binding ID가 중복되지 않는다.
- [ ] 앵커 ID가 비어 있거나 중복되지 않는다.
- [ ] 모든 이동 클립에 시작·도착 앵커가 연결되어 있다.
- [ ] 같은 Move Track의 이동 클립끼리 겹치지 않는다.
- [ ] 배우의 픽셀 AnimationClip이 위치 Transform을 쓰지 않는다.
- [ ] 스킵 후 남아야 하는 결과만 `fireOnSkip`으로 지정했다.
- [ ] Definition의 `endBindings`가 정상 종료와 스킵 종료에서 같은 최종 상태를 만든다.
- [ ] `현재 씬 저장 + Timeline 베이크 + 전체 검사`가 오류 없이 끝난다.
- [ ] 정상 재생에서 대사 정지·재개와 애니메이션 타이밍이 맞는다.
- [ ] 스킵 재생에서도 필수 플래그와 최종 무대 상태가 적용된다.
- [ ] 한국어와 영어를 각각 한 번 이상 재생했다.

## 현재 프로토타입 범위

현재 버전은 제작 흐름을 검증하기 위한 독립 도구다. 실제 게임의 공용 저장, 대사 데이터, Addressables, 기존 캐릭터 프리팹에는 아직 연결하지 않았다. 제작 방식이 확정되면 이 폴더에서 검증한 Director·마커·베이크·검사 구조를 실제 컷씬 시스템으로 이전한다.
