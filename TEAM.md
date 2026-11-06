# OUTPOST 09 — 3인 개발 분담

Unity 버전: 6000.6.7f1. 실행: Assets/Scenes/SampleScene.unity를 열고 Play.

## 1번: 게임 진행 / 통합
- 담당: Assets/Outpost09/Scripts/Gameplay/Outpost09.Progression.cs
- 담당: Assets/Outpost09.cs (시작 처리, 공용 상태), ProjectSettings, Packages, 메인 씬
- 기능: 웨이브, 스테이지, 크레딧, 체력, 승패, 재시작
- 다음 작업: 일시정지와 재시작 정리, 설정 데이터 분리

## 2번: 전투 / 적
- 담당: Assets/Outpost09/Scripts/Combat/ 아래 전체
- 기능: 초소 이동, 입력, 사격, 재장전, 수류탄, 적 생성과 AI, 산성 투사체
- 다음 작업: 장애물을 피하는 경로, 투사체 충돌 검증, 전투 밸런스

## 3번: 그래픽 / UI
- 담당: Assets/Outpost09/Scripts/Presentation/ 아래 전체
- 기능: 맵, 조명, 모델, 발사 효과, HUD, 메뉴, 상점 화면
- 모델/텍스처/음원 추가 위치: Assets/Outpost09/Art/
- 다음 작업: 실제 모델과 텍스처, 피격/사망 효과, UI 개선

## 파일 연결 방식
이번 분리는 동작을 유지하면서 파일 충돌을 줄이는 partial class 방식이다.
Outpost09.*.cs 파일들은 컴파일 시 하나의 Outpost09 컴포넌트가 된다.
따라서 이 파일들을 각각 GameObject에 붙이지 않는다. 시작 시 자동 생성된다.
Alien.cs와 AcidShot.cs는 별도 컴포넌트다.
독립 시스템과 이벤트 기반 구조로 완전히 분리된 상태는 아니므로 공용 변수/메서드 이름 변경은 1번 담당자와 먼저 합의한다.

## 작업과 통합
1. 각자 gameplay/이름, combat/이름, presentation/이름 브랜치에서 작업한다.
2. 다른 담당자 파일을 바꿔야 하면 먼저 범위를 합의한다.
3. Unity에서 컴파일 오류가 없는지 확인하고 담당 기능을 Play로 확인한다.
4. 변경 내용을 검토받은 후 1번 담당자가 공용 브랜치에 합친다.
5. 합친 뒤 시작 → 초소 이동 → 사격 → 웨이브 완료 → 구매 → 다음 웨이브를 확인한다.
6. .meta 파일은 함께 공유한다. Library, Temp, Logs, obj는 공유하지 않는다.

## 검증 범위
분리 작업은 기존 메서드를 담당 파일로 이동한 구조 변경이다.
Unity 컴파일 및 실제 플레이 완료를 의미하지 않는다. 기존 경로 이동/일시정지 등의 미완성 기능도 별도 작업이 필요하다.
