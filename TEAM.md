# OUTPOST 09 · 3인 개발 분담

Unity 6000.6.7f1에서 `Assets/Scenes/SampleScene.unity`를 열고 Play를 누른다. 메인 화면에서 START GAME을 누르면 첫 웨이브가 시작된다.

## 담당 파일과 완료 기준

| 담당 | 전담 파일 | 이번 단계 완료 기준 |
|---|---|---|
| 1번 · 게임 진행과 UI | `Assets/Outpost09/Scripts/Gameplay/` 전체, 메인 씬, `ProjectSettings/`, `Packages/` | 메인 화면, HUD, 상점, 일시정지, 결과 화면을 연결한다. 웨이브별 적 수·등장 간격·보상·가격을 조정하고 통합본에서 한 판을 끝까지 확인한다. |
| 2번 · 전투와 적 | `Assets/Outpost09/Scripts/Combat/` 전체, 적·무기·투사체 프리팹과 전투 효과 | 기존 모델을 사용해 총기·수류탄·초소 전환을 확인한다. 최신 맵에서 적 4종의 이동·공격과 투사체 충돌을 검증한다. |
| 3번 · 맵과 환경 | `Assets/Outpost09/Scripts/Map/`, `Assets/Outpost09/Art/`, `Assets/Outpost09/Resources/`, 협곡 도구·미리보기 씬과 생성 에셋 | 초소 3곳에서 주요 접근로가 보이게 하고, 스폰 5곳을 굴과 지형에 연결한다. 이 범위에서 협곡의 지형·조명·재질을 다듬는다. |

### 1번 · 화면과 게임 규칙

- `Gameplay/UI/MainMenuUI.cs`에서 시작 화면을 수정한다. `Gameplay/UI/OutpostGameUI.cs`에서 HUD·상점·일시정지·결과 화면을 수정한다.
- `Gameplay/Outpost09.Progression.cs`에서 웨이브·스테이지, 크레딧, 상점 규칙과 승패를 수정한다. `Gameplay/Outpost09.cs`는 씬 시작 시 맵·전투·UI를 연결한다.

### 2번 · 입력, 전투 규칙, 전투 외형

- `Combat/OutpostCombat.cs`에서 키 설정, 사격·장탄수·초소 전환과 적 생성 처리를 수정한다. HUD와 메인 화면에 표시할 키·무기 이름도 여기에서 제공한다.
- `Combat/Alien.cs`, `Combat/AcidShot.cs`, `Combat/Grenade.cs`에서 적과 투사체 동작을 수정한다.
- `Combat/OutpostCombat.Visuals.cs`와 `Combat/OutpostCombat.Creatures.cs`에서 무기와 적의 외형을 수정한다. 새 전투 에셋은 필요할 때 `Assets/Outpost09/CombatArt/`에 추가한다.

### 3번 · 공간과 시야

- `Map/OutpostMapLayout.cs`에서 초소의 위치와 조준점, 스폰 위치, 경로를 수정한다.
- `Map/OutpostCanyonWorld*.cs`와 `Map/OutpostTerrainShape.cs`에서 협곡·교량·굴·초소·게이트·조명을 수정한다.
- `Tools → OUTPOST 09 → Open Canyon Preview`로 확인한다. 미리보기와 생성 에셋 경로는 [맵 문서](Assets/Outpost09/MAP_DESIGN.md)에 있다.

## 담당 영역이 만나는 곳

| 변경 | 파일을 수정하는 담당자 |
|---|---|
| 적이 어디서 나오고 어느 길로 가는가 | 3번이 스폰과 경로 좌표를 수정한다. |
| 적이 몇 마리씩 언제 나오고, 처치 시 얼마를 받는가 | 1번이 웨이브와 보상 데이터를 수정한다. |
| 적이 경로를 어떻게 따라가고 피해를 주는가 | 2번이 AI와 충돌을 수정한다. |
| 초소 바닥에 시야가 가리는가, F1 입력에 이동하는가 | 3번이 카메라 좌표를, 2번이 전환과 조준 입력을 수정한다. |
| 수류탄 가격과 구매 버튼, 투척과 피해 | 1번이 가격과 화면을, 2번이 인벤토리와 투척·폭발을 수정한다. |

Combat과 Map은 별도 Unity 어셈블리다. 둘은 `Contracts/OutpostBattlefield.cs`로 초소·게이트·경로를 주고받는다. 경로 좌표는 지면 기준이고 초소 번호는 왼쪽·중앙·오른쪽 순서다. Gameplay는 전투에 적 생성과 보급을 요청하고, 전투가 보내는 일시정지·피해·처치 이벤트를 처리한다. UI는 게임 진행과 전투에서 제공하는 상태를 읽는다.

## 병렬 작업과 통합

1. 역할 분리 변경을 한 PR로 검토한 뒤, 합쳐진 커밋에서 `gameplay/작업명`, `combat/작업명`, `map/작업명`을 각각 만든다. 기존 브랜치의 진행 중 변경은 담당 파일의 새 경로에 반영한다.
2. 메인 씬과 `Contracts/` 변경은 1번이 통합 PR에서 맡는다. 공유 API를 바꿔야 할 때는 영향받는 담당자와 합의해 호출부도 같은 PR에서 갱신한다.
3. 각자 담당 기능을 Play에서 확인한다. 2번과 3번이 별도 검증 씬을 만들면 각자의 폴더에 둔다.
4. 1번은 통합 후 메인 화면 → 첫 웨이브 → 초소 전환·사격 → 상점 → 다음 웨이브 → 재시작을 확인한다.
5. Unity 에셋을 옮기거나 추가할 때는 `.meta`도 함께 커밋한다. `Library`, `Temp`, `Logs`, `obj`는 Git에서 제외한다.
