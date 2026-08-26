# Project L.U.N.A 컷씬 독립 프로토타입

이 폴더는 기존 게임 컷씬·데이터·로더에 연결하지 않는 독립 테스트 영역이다.

## 실행

1. `CutscenePrototype_Lab.unity`를 연다.
2. Play를 누르면 `Data/cutscene_test.json`의 `lab_escape_prototype`이 자동 재생된다.
3. 대사는 `Space`, `Enter` 또는 마우스 왼쪽 클릭으로 진행한다.
4. 선택지는 숫자 `1`~`4` 또는 마우스로 선택한다. 조건을 만족하지 못한 선택지는 잠금 사유와 함께 비활성화된다.
5. `S`를 누르면 선택지 표시 중을 제외하고 컷씬을 스킵한다. 필수 이벤트와 `end_state`는 정상 종료와 동일하게 적용된다.
6. `R`을 누르면 플래그·완료 기록·무대 상태를 초기화하고 처음부터 다시 재생한다.
7. `L`을 누르면 한국어와 영어 표시를 전환한다.
8. `F1`을 누르면 현재 상태·시퀀스·플래그·발화 이벤트를 확인하는 디버그 패널을 연다.

## 구성

- `Data/cutscene_test.json`: 프로토타입 전용 컷씬 순서와 한·영 대사
- `Scripts/CutscenePrototypeLoader.cs`: JSON 파싱, 스키마·참조·현지화·선택지·종료 상태 검증
- `Scripts/PrototypeCutsceneRunner.cs`: 상태 머신, 선택지·분기·플래그, 안전 스킵, 중복 재생 차단, 종료 상태 적용
- `Scripts/PrototypeTimelineAsset.cs`: PlayableDirector로 재생되는 독립 연출 세그먼트
- `Scripts/PrototypeCutsceneView.cs`: 테스트 연구소, 캐릭터, 대사 UI, 화면 효과
- `Editor/CutscenePrototypeBuilder.cs`: 테스트 씬과 연출 에셋 생성

프로토타입의 모든 소스·데이터·씬·생성 에셋은 `Assets/99.CutsceneProto/` 안에만 둔다.

## 98번 프로토타입에서 선별 적용한 기능

- `once` / `repeatable` 재생 모드와 세션 완료 기록
- `choice`와 `branch` 스텝, 조건부 선택지, 잠금 사유, 한·영 표시
- `flag.*` 조건과 여러 플래그의 원자적 상태 변경
- `event_key` 중복 실행 차단과 `fire_on_skip` 필수 이벤트
- 정상 종료와 스킵 종료가 함께 사용하는 명시적 `end_state`
- Timeline·actor·command·choice·seq 참조의 재생 전 검증
- F1 런타임 디버그 패널과 전체 무대 상태 초기화

자체 MicroTimeline은 가져오지 않는다. 실제 연출은 계속 Unity `PlayableDirector`와 `PrototypeTimelineAsset`을 통해 재생한다.
