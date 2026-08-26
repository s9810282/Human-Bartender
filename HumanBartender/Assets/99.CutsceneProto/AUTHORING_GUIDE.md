# Project L.U.N.A 컷씬 제작 도구 사용 가이드

이 도구는 Unity Timeline을 중심으로 컷씬을 직접 조립하기 위한 독립 프로토타입이다. 캐릭터·카메라 애니메이션은 Timeline 클립으로 배치하고, 대사·화면 효과·스프라이트 교체는 전용 마커로 같은 시간축에 배치한다.

모든 테스트 파일은 `Assets/99.CutsceneProto/` 안에 있으며 기존 게임 컷씬 시스템이나 데이터에는 연결하지 않는다.

## 가장 빠른 시작 방법

1. `CutsceneAuthoring_Lab.unity`를 연다.
2. Unity 상단 메뉴에서 `Window > Project L.U.N.A > Cutscene Authoring Studio`를 연다.
3. Studio에서 `이 Director를 Timeline 창에서 열기`를 누른다.
4. Timeline 재생 헤드를 원하는 시간으로 옮긴다.
5. Studio의 `＋ 대사`, `＋ 효과`, `＋ 스프라이트` 버튼으로 이벤트를 추가한다.
6. 생성된 마커를 선택하고 Inspector에서 내용을 편집한다.
7. `Timeline 베이크 + 현재 컷씬 전체 검사`를 누른다.
8. 오류가 없으면 Scene을 저장하고 Play한다.

예제 씬은 연구소 침입 상황을 짧게 구성해 둔 제작 샘플이다. 기존 클립을 옮기거나 길이를 바꾸면서 Timeline과 대사가 함께 어떻게 진행되는지 바로 확인할 수 있다.

## 제작 화면의 역할

### Timeline 창

실제 컷씬의 시간과 연출 순서를 조립하는 공간이다.

- Animation Track: 캐릭터 이동, 자세, 카메라 이동처럼 AnimationClip으로 만드는 연출
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
- Animation·Audio Track 생성 및 오브젝트 연결
- Timeline 마커를 런타임 데이터로 베이크
- 누락된 번역, ID 중복, 잘못된 대상 참조, 위험한 스킵 설정 검사

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

지정한 Binding ID의 `SpriteRenderer` 또는 `Image`에 새 스프라이트를 적용한다. 정지 이미지 교체, 표정 교체, 모니터 화면 전환 등에 사용한다.

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

그다음 대상 오브젝트를 유지한 채 `Animation Track 생성 + 연결`을 누르면 Animator와 연결된 새 트랙이 만들어진다. 실제 픽셀 애니메이션 클립을 해당 트랙으로 드래그해 배치한다.

## 기존 예제 자산을 실제 리소스로 바꾸는 방법

1. 예제 Scene을 복제하거나 `빈 에셋 만들기`로 새 Timeline과 Definition을 만든다.
2. 임시 도형 오브젝트 대신 실제 연구소 배경과 캐릭터 SpriteRenderer를 Scene에 둔다.
3. 오브젝트마다 변하지 않는 Binding ID를 등록한다.
4. 실제 픽셀 AnimationClip을 Animation Track에 배치한다.
5. 대사와 효과 마커를 재생 시간에 맞춰 옮긴다.
6. Definition의 종료 상태를 실제 컷씬 결과에 맞게 설정한다.
7. 베이크·검사 후 정상 재생과 스킵 재생을 각각 확인한다.

컷씬을 수정하더라도 다른 데이터에서 참조할 `cutsceneId`, 대사의 `dialogueId`, 오브젝트의 Binding ID는 함부로 바꾸지 않는다.

## 테스트 조작

- `Space`, `Enter`, 마우스 왼쪽 클릭: 대사 완성·다음 대사
- `S`: 컷씬 안전 스킵
- `R`: 테스트 상태 초기화 후 처음부터 다시 재생
- `L`: 한국어·영어 표시 전환

## 완료 전 검사 목록

- [ ] Timeline과 Definition이 같은 컷씬을 가리킨다.
- [ ] 모든 대사에 고유한 `dialogueId`가 있다.
- [ ] 모든 대사에 한국어와 영어가 들어 있다.
- [ ] 모든 효과·스프라이트의 `targetId`가 Binding Registry에 존재한다.
- [ ] 같은 Binding ID가 중복되지 않는다.
- [ ] 스킵 후 남아야 하는 결과만 `fireOnSkip`으로 지정했다.
- [ ] Definition의 `endBindings`가 정상 종료와 스킵 종료에서 같은 최종 상태를 만든다.
- [ ] `Timeline 베이크 + 현재 컷씬 전체 검사`가 오류 없이 끝난다.
- [ ] 정상 재생에서 대사 정지·재개와 애니메이션 타이밍이 맞는다.
- [ ] 스킵 재생에서도 필수 플래그와 최종 무대 상태가 적용된다.
- [ ] 한국어와 영어를 각각 한 번 이상 재생했다.

## 현재 프로토타입 범위

현재 버전은 제작 흐름을 검증하기 위한 독립 도구다. 실제 게임의 공용 저장, 대사 데이터, Addressables, 기존 캐릭터 프리팹에는 아직 연결하지 않았다. 제작 방식이 확정되면 이 폴더에서 검증한 Director·마커·베이크·검사 구조를 실제 컷씬 시스템으로 이전한다.
