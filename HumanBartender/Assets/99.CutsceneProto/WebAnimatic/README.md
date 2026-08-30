# WebAnimatic — 웹 애니매틱의 유니티 재생기

`Cutscene/web/`(웹 애니매틱)에서 확정한 컷씬 5개(S#1·S#2·S#3+낙하·S#5·S#99)를
유니티에서 그대로 재생한다. 웹 엔진과 동일하게 **화면 상태 = 시간 t의 순수 함수**로
평가하므로 아무 시점으로 Seek해도 결과가 같고, 웹에서 조정한 타이밍이 그대로 이관된다.

## 가장 빠른 실행

1. `WebAnimatic.unity`를 열고 Play.
2. 조작: `Space` 재생/정지 · `← →` 1초 · `R` 처음 · `1~5` 씬 전환.

Timeline에서 확인하려면 `WebAnimaticTimeline.unity`를 열고 Hierarchy의
`WebAnimaticTimeline`을 선택한 뒤 `Window > Sequencing > Timeline`을 연다. PlayableDirector에
S1·S2·S3·S5·S99 Timeline 중 하나를 지정하면 해당 씬을 재생 헤드로 스크럽할 수 있다.

## Timeline Bridge 사용법

1. `Window > Project L.U.N.A > Web Animatic > Build All`을 실행한다.
2. 자동 생성된 `WebAnimaticTimeline.unity`를 연다.
3. Hierarchy의 `WebAnimaticTimeline`을 선택하고 Timeline 창을 연다.
4. `WEB ANIMATIC · SOURCE` 클립을 스크럽해 카메라·배우·대사·이펙트를 같이 확인한다.
5. 클립을 이동하면 전체 시작 시점이, 길이나 Speed Multiplier를 바꾸면 재생 구간과 속도가 바뀐다.
6. 다른 씬은 PlayableDirector의 `Playable`을 `Timelines/` 안의 원하는 에셋으로 바꾼다.
7. 생성 결과는 `Window > Project L.U.N.A > Web Animatic > Validate Timeline Bridge`로 검사한다.

`Build All`은 웹 JSON을 정본으로 사용해 소스 Timeline을 다시 만든다. 따라서 수동 트랙을
보존해야 하면 생성된 Timeline을 복제하거나, 소스 트랙을 두고 추가 트랙에서만 작업한다.
Timeline Bridge는 웹 연출을 빠르고 정확하게 이관하는 경로이고, 배우별 Animation·Move 트랙을
직접 조립하는 작업은 `Cutscene Authoring Studio`를 사용한다.

## 파이프라인

```
웹 애니매틱 (Cutscene/web)
  └─ 내보내기(브라우저): 씬 JSON(트랙·대사·이펙트) + 무대 배경 PNG(상태 변형별) + 시트 프레임 메타
        → Data/{scenes,stages,meta}
유니티
  ├─ Editor > Window/Project L.U.N.A/Web Animatic/Build All
  │    · 무대 PNG 임포트 설정 + 라이브러리 에셋 + 재생 씬 생성
  │    · S1·S2·S3·S5·S99 Timeline + Timeline 미리보기 Scene 생성
  │    · 시트 텍스처는 엔진 기존 것(Resources/Cutscenes/Lab, 01.Sprites/Ch/luna)을 참조
  │    · 프레임 절단은 유니티 슬라이스가 아니라 웹과 동일한 rect/앵커 메타(Sprite.Create)
  └─ Window/Project L.U.N.A/Web Animatic/Capture Keyframes
       · 플레이 모드 없이 Seek→렌더로 키프레임 PNG를 뽑는다 (Captures/)
```

웹 쪽 타이밍·대사가 바뀌면: 웹에서 다시 내보내고 → Data/ 갱신 → Build All.

## 구성

- `Scripts/WebAnimaticData.cs` — 씬 JSON 파싱 + 웹과 동일한 트랙 평가(evalNum/ease/시드 랜덤/typedCount)
- `Scripts/WebAnimaticLibrary.cs` — 시트·무대·씬 에셋 묶음, 앵커 피벗 스프라이트 캐시
- `Scripts/WebAnimaticPlayer.cs` — 재생기: 카메라(흔들림·클램프), 배우(시트 애니·flip·rot·틴트·bob),
  무대 변형 교체(door/chute/alarm), 월드 이펙트(dust·debris·muzzle·tracer·spark·link),
  오버레이(페이드·플래시·레터박스·TV 노이즈·슬로모 그레이드·컷 라벨·사운드 큐 칩),
  말풍선(크기 확정·타이핑·[T:] 정지·[c:] 색·[noise] 스크램블·[censor] 모자이크·[shake] 글자 떨림)
- `Scripts/WebAnimaticTimelineClip.cs`, `Scripts/WebAnimaticTimelineTrack.cs` — Timeline 시간을 웹 장면 평가와 연결
- `Editor/WebAnimaticImporter.cs`, `Editor/WebAnimaticTimelineImporter.cs`, `Editor/WebAnimaticCapture.cs`

## 현재 폴백·제약

- 말풍선 라디오·방송 점선 테두리는 현재 실선으로 표시한다.
- 타이핑 블립과 큐 음은 타이밍 확인용 합성 사운드다. 실제 BGM·SFX는 후속 프로덕션 리소스로 교체한다.
- `slowmo`는 웹 연출의 탈색·틴트를 재현하는 간략화된 오버레이다.
- 전각 노이즈 특수문자 일부가 폰트에 없으면 자동 폴백(▓▒#@$%&?!)

## 주의

- `lab_effect-Sheet`만 엔진 사본이 2048로 다운스케일돼 있어 웹 원본을 `Data/sheets/`에 동봉해 우선 사용
- 무대의 경광등 맥동·모니터 플리커 등은 배경 PNG에 고정 상태로 구워져 있다(웹은 실시간)
