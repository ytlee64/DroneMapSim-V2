# DroneMapSim Development Log (DEVLOG)

> 드론 시뮬레이터 및 전술 관제 시스템(GCS)의 개발 내역과 의사결정 기록입니다.

---

## 📅 [2026-10-01] GCS UI/MVVM 리팩토링, AI 폴더 정리 및 3색 바운딩 박스 도입

### [Git] AI 디렉터리 평탄화 및 캐시 정리

- 경로 단순화를 위해 `apps/AI/Yolo/*`를 상위 `apps/AI/*`로 이력 보존 이동:

  ```powershell
  Get-ChildItem "apps\AI\Yolo" | ForEach-Object { git mv $_.FullName "apps\AI" }
  ```

- `.gitignore` 수정 사항이 기존 추적 파일에 반영되지 않던 문제를 `git rm -r --cached .` ➔ `git add .`로 해결.

### [GCS] 밀리터리 다크 테마 구축 (`Style.xaml`)

- 기존 회색(`#5E5E5E`) 배경을 딥 슬레이트 네이비(`#0B0F19`, `#131B2E`) 및 시안(`#0284C7`) 테마로 교체.
- 전역 `<Style TargetType="Grid">` 배경 강제 지정 안티패턴 제거 및 `GroupBox` 헤더 잘림 현상(`Panel.ZIndex="10"` 부여) 해결.

### [GCS] WPF MVVM 분리 및 UserControl 컴포넌트화 (`View` 접미사 제거)

- 거대 뷰모델을 **`MainWindowVM.cs`**(통신/조종 총괄)와 **`TacticalMapVM.cs`**(2km 캔버스 좌표 변환/궤적)로 분리.
- 불필요한 `View` 접미사를 빼고 `FlightControlPanel.xaml`, `TacticalMap.xaml`로 독립시켜 `MainWindow.xaml`을 60줄로 슬림화.

### [AI] 정찰 영상 IoU 3색 바운딩 박스 오버레이

- 정답(Ground Truth)과 AI 추론(Detection) 박스가 겹쳐 가려지는 문제를 해결하기 위해, 화면 2분할(공간 낭비/시선 분산) 대신 단일 화면 **IoU 3색 판정** 채택:
  - 🟢 **Cyan**: 적중(`IoU ≥ 0.4`) / 🔴 **Red**: 오탐(FP) / 🟡 **Yellow 점선**: 미탐(FN)

### [Docs] 투-트랙(Two-Track) 문서화 정책 수립

- `DRONE_MAP_SIM_ARCHITECTURE.md`(현재 최종 스펙 SSOT)와 `DEVLOG.md`(히스토리)로 역할 분리.

---

## 📅 [2026-10-08] TargetGenActor 영구 보존, 전술 지도 GT 가시화 및 텔레메트리 동기화 ImageService 구축

### [UE5] `TargetGenActor` 영구 직렬화 및 자동 복원 (`TargetGenActor.h/.cpp`, `target_setup.py`)

- **원인**: 동적 생성한 HISM 컴포넌트가 레벨(`.umap`)에 직렬화되지 않고, `target_setup.py`에 `generate_target()`(단수형) 호출과 `TargetElements` 프로퍼티 주입이 누락되어 에디터 재실행 시 초기화가 풀림.
- **해결**:
  - `EnvElements`를 `TargetElements`로 일원화하고 HISM에 `EComponentCreationMethod::Instance` + `AddInstanceComponent` 적용.
  - 바다(`WaterBody`) 차량 제거 후 `FinalizeAndSaveState()`로 `SavedInstances`(배치 좌표)와 `SpawnedTargets`(Ground Truth)를 `.umap` 및 `GroundTruth_Targets.json`에 영구 저장.
  - `PostLoad()`, `OnConstruction()`, `BeginPlay()`에서 `RestoreFromSavedState()`를 호출해 재계산(LineTrace) 없이 즉시 복원.
  - 에디터 Details 패널 상단에 타겟 개수 현황판(`TotalVehicleCount`, `TotalTargetCount`, `NormalVehicleCount`, `SpawnCountByElement`) 추가.
- **참고**: Live Coding 패치는 임시 파일이므로, 에디터 종료 후 VS에서 정식 `Development Editor` (`Win64`) 빌드를 해두어야 재실행 시 모듈 재빌드 팝업이 뜨지 않음.

### [GCS/AI] 전술 지도(`TacticalMap`) Ground Truth 가시화 및 YOLO 학습 부진 원인 규명

- **구현**: `GroundTruth_Targets.json`의 `cm` 좌표를 `m` 단위로 변환(`/ 100.0`) 후 드론과 동일한 `ToScreenPoint()`로 투영하여 지상 타겟 마커 표시 (상단 GT 개수 배지, On/Off 체크박스, 새로고침 버튼 추가).
- **인사이트**: 지도에 84개 지상 타겟(초록 점)과 드론 `ㄹ`자 수색 궤적(하늘색 선)을 겹쳐 본 결과, **비행 경로 내부에 타겟이 5~6개뿐**임을 발견(수집 이미지의 95%가 빈 배경). `config.py`의 타겟 스폰 가중치(`weight`) 및 밀도 상향 필요성 확인.

### [GCS] 조종 패널(`FlightControlPanel`) 키 매핑 동기화

- `MainWindowVM.HandleKeyDown` 실제 키(`1`: 모드 토글, `I`: 드론/짐벌 초기화, `Q/W/E/A/S/D`: 비행, 방향키 `↑/↓/←/→`: 짐벌, `C/Space`: 캡처, `V/O`: 뷰 전환)와 UI 버튼/문구를 1:1 일치시키고, `Focusable="False"`로 방향키/스페이스바 포커스 가로채기 방지.

### [통신/UE5] 텔레메트리 동기화 `ImageService` 전환 및 캡처 파일명 밀리초(`ms`) 확장

- **원인**:
  1. `FileSystemWatcher`는 0바이트 파일 생성 시점에 다중 발화되어 언리얼의 파일 쓰기와 `System.IO.IOException` 충돌을 일으킴.
  2. `DronePawn.cpp`가 초 단위 파일명(`IMG_YYYYMMDD_HHMMSS`)을 사용하여 1초에 4~5장 촬영 시 같은 파일에 덮어쓰기되면서 `IOException`과 타겟 데이터 유실 발생.
- **해결**:
  1. `DronePawn.cpp` 파일명에 `Now.GetMillisecond()`(`%03d`)를 추가하여 `IMG_YYYYMMDD_HHMMSS_fff` 고유 파일명 보장.
  2. `git mv ImageWatcherService.cs ImageService.cs` 변경 후 `FileSystemWatcher`를 제거하고, 파일 저장 완료 후 텔레메트리(`packet.LastCapture`) 수신 시점에만 로드하도록 전환하여 예외 원천 제거.

---

## 🚀 향후 개발 과제 (Next Steps)

- [ ] `config.py` 타겟 스폰 가중치(`weight`) 및 밀도 조정을 통한 양질의 YOLO 학습 데이터셋 재수집
- [ ] 다중 웨이포인트 기반 자동 비행 오케스트레이터 (`UMissionManager`) 연동
- [ ] 전술 지도(`TacticalMap`) 위 마우스 클릭을 통한 실시간 웨이포인트 추가 및 전송 기능
- [ ] 캡처된 항공 정찰 이미지의 실시간 정사 모자이크(Orthomosaic) 스티칭 프로토타입

