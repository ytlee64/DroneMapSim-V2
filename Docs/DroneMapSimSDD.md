# Drone Camera Simulator & Autonomous Digital Tactical Mapping System
> **버전**: v1.4.0 (최신 빌드 및 아키텍처 업데이트 반영)  
> **최종 수정일**: 2026-10-01  
> **적용 시스템**: Unreal Engine 5.7, C# .NET 8 WPF GCS, PyTorch/YOLOv8 AI Pipeline

---

## 1. 프로젝트 개요 및 아키텍처

### 1.1. 프로젝트 목적 및 핵심 개발 방향
- **드론 항공 카메라 기반 지상 객체 식별, 자율 수색 비행, 실시간 전술 지도(Tactical Map) 생성 파이프라인 구축**
  - **비행 제어**: UE 5.7 C++ 기반 고정익 비행 물리, 뱅킹 연동 선회, 2축 짐벌(Pitch/Yaw), 관찰자 시점(Chase/TopDown/Free) 전환
  - **통신 시스템**: UE5 시뮬레이터와 GCS 간 UDP JSON 기반 비동기 양방향 제어/고속 텔레메트리 (CMD: 9000, TLM: 9001)
  - **데이터 수집**: UE 5.7 64비트 버퍼 기반 고품질 렌더타깃 이미지, YOLO 정답 라벨(`.txt`), 비행 메타데이터(`.csv`) 자동 추출
  - **AI 연동**: GPU 가속 YOLOv8 모델 학습 및 실시간 ONNX 런타임 추론, IoU 기반 정답/추론 혼합 오버레이
  - **전술 매핑**: 2.3km 격자 좌표 변환, 실시간 비행 궤적(Trajectory), 기수 벡터(Heading), 웨이포인트 가이드 가시화

### 1.2. 개발 환경
- **Engine**: Unreal Engine 5.7 (Large World & 64-bit Allocator `TArray64` 규격 대응)
- **Language / IDE**: C++ (Visual Studio 2022), C# (.NET 8 / CommunityToolkit.Mvvm WPF GCS), Python 3.10+
- **Deep Learning / AI**: PyTorch (CUDA 12.x 가속), Ultralytics YOLOv8 (ONNX Runtime 가속), OpenCV
- **Hardware Acceleration**: NVIDIA GeForce RTX 5070 (12GB VRAM)

### 1.3. 핵심 설계 원칙 (Architecture Principles)
- **순수 C++ 항법 엔진 분리 (Engine-Agnostic Navigation Engine)**:
  - 언리얼 엔진 종속성을 배제한 순수 C++ 클래스(`NavBase`, `NavFixedWing`)로 항법과 물리 역학을 분리한다.
  - `ADronePawn`은 매 틱 `NavEngine->Step()`의 결과값만 트랜스폼에 반영하는 최소 책임 브릿지 역할을 수행한다.
- **WPF GCS 모듈형 컴포지션 및 관심사 분리 (MVVM & UserControl Decomposition)**:
  - 단일 거대 뷰모델(God ViewModel)을 지양하고, **`MainWindowVM`**(총괄/통신/조종)과 **`TacticalMapVM`**(2.3km 캔버스 좌표 수학/궤적)으로 책임을 분리한다.
  - UI는 `FlightControlPanel.xaml` 및 `TacticalMap.xaml` 독립형 UserControl로 컴포넌트화한다.
- **단일 창구 입력 체계 (Single Entry-Point Command Flow)**:
  - 모든 사용자 입력(키보드/UI 버튼)은 정규화된 JSON 규격으로 변환되어 `UCommLink::ProcessJsonCommand()`로 집약된다.
- **실제 항공기 조타면 역학 (Aero Surface Inertia & Auto-Decay)**:
  - 내부 필터와 키를 뗐을 때 자동 중립 복귀(Auto-Decay) 처리를 통해 안정적인 수평 순항과 조타 안정성을 보장한다.
- **절대 해수면 고도(MSL) 표준화**:
  - 텔레메트리와 항법의 기준 고도는 MSL(Mean Sea Level) 미터 단위로 일원화한다.

---

### 1.4. 시스템 컨텍스트 및 아키텍처 다이어그램
```mermaid
flowchart TB
  subgraph GCS[Ground Control Station (C# .NET 8 / WPF)]
    MW[MainWindow.xaml / MainWindowVM]
    FCP[FlightControlPanel.xaml]
    TM[TacticalMap.xaml / TacticalMapVM]
    IW[ImageWatcherService / ONNX Detector]
    
    MW --- FCP
    MW --- TM
    MW --- IW
  end

  subgraph UE5[Unreal Engine 5.7 Runtime]
    CL[UCommLink (UDP 9000/9001)]
    DP[ADronePawn]
    NV[NavBase / NavFixedWing]
    OB[AObserverPawn]
    GM[DroneMapSimGameMode]
    SC[SceneCapture2D / RT_DroneCapture]
  end

  subgraph AI[AI & Dataset Pipeline (apps/AI)]
    ONNX[apps/AI/weights/best.onnx]
    TRN[apps/AI/train_yolo.py]
    DS_STORE[Saved/DroneCaptures/*.png, *.txt, *.csv]
  end

  subgraph PY_AUTOMATION[Editor Automation Tools]
    DS[drone_setup.py]
    TS[target_setup.py]
    CFG[config.py]
  end

  %% 통신 및 제어 흐름
  FCP -->|CMD JSON : Port 9000| CL
  CL -->|ProcessJsonCommand| DP
  DP --> NV
  DP --> OB
  DP -->|10Hz Telemetry : Port 9001| MW
  MW -->|Coord & Yaw| TM

  %% 캡처 및 AI 흐름
  DP --> SC --> DS_STORE
  DS_STORE -->|FileSystemWatcher| IW
  IW -->|ONNX Inference| ONNX
  DS_STORE --> TRN --> ONNX

  %% 에디터 설정 흐름
  DS --> DP
  DS --> OB
  DS --> GM
  TS --> CFG
  TS --> DS_STORE
```

---

## 2. 핵심 구현 및 모듈별 상세 현황

### 2.1. Unreal Engine 5.7 C++ 비행체 및 항법 코어
- **64비트 이미지 압축 API 마이그레이션 (`DronePawn.cpp`)**:
  - 기존 Deprecated된 `CompressImageArray`를 **`FImageUtils::PNGCompressImageArray`**로 전환.
  - UE 5.7 최신 컨테이너 규격에 따라 32비트 버퍼에서 64비트 할당자 **`TArray64<uint8>`**로 교체하여 빌드 에러(C2664)를 해결하고 대용량 렌더타깃 압축 안정성 확보.
- **순수 C++ 항법 엔진 (`NavFixedWing`)**:
  - 속도 제어: 최소 10 m/s ~ 최대 30 m/s 순항 스로틀.
  - 공기역학 연동 선회: 롤(Roll) 각도(최대 40°)에 비례하여 요(Yaw)를 자동 보정:
    $$\text{TurnRate} = \left(\frac{\text{Roll}}{40^\circ}\right) \times 45^\circ/\text{s} + \text{RudderRate}$$
- **`UCommLink` 통합 통신 컴포넌트**:
  - `MOVE`, `GIMBAL`, `CAPTURE`, `OBSERVER`, `AUTONAV` JSON 프로토콜을 단일 진입점에서 안전하게 파싱 및 디스패치.

### 2.2. C# WPF GCS 관제소 아키텍처 리팩토링
- **MVVM 컴포지션 분리 (Single Responsibility)**:
  - **`MainWindowVM`**: UDP 명령 송신, 텔레메트리 수신, 이미지 캡처 이벤트 감시, 4개 비행 모드(`MANUAL`, `AUTO NAV`, `LOITER 40M`, `RTH`) 상태 머신 총괄.
  - **`TacticalMapVM`**: 2.3km 실사용 영역의 스크린 좌표 투영, 월드 미터 궤적 리스트 관리, 드론 마커(14px) 및 기수 벡터(22px) 실시간 렌더링.
- **컴포넌트 기반 UI (`UserControl`)**:
  - `MainWindow.xaml`: 상단 원격계측 헤더, 2분할 레이아웃, 하단 상태바만 보유 (60줄로 슬림화).
  - `FlightControlPanel.xaml`: 사이버펑크 택티컬 D-패드(WASD 비행자세, IJKL 짐벌조종, NADIR 단축키, 긴급 CAPTURE).
  - `TacticalMap.xaml`: 리사이즈 이벤트(`SizeChanged`)를 자체 캡슐화한 전술 지도 캔버스.
- **택티컬 딥 슬레이트 디자인 시스템 (`Style.xaml`)**:
  - `ClrBackground` (`#0B0F19`), `ClrSurface` (`#131B2E`), `ClrAccent` (`#0284C7`), `ClrDanger` (`#E11D48`), `ClrWarning` (`#F59E0B`), `ClrSuccess` (`#10B981`) 정립.
  - `GroupBox` 템플릿의 Z-Index(`Panel.ZIndex="10"`) 및 여백 보정을 통해 글씨 잘림 현상 원천 차단.

### 2.3. AI 파이프라인 및 표적 시각화 기법
- **리포지토리 구조 일원화**:
  - 기존 `apps/AI/Yolo/` 디렉터리를 `apps/AI/`로 Git 히스토리 추적성(`git mv`)을 보존한 채 평탄화 이동 완료.
  - 가중치 경로: `apps/AI/weights/best.onnx`, 학습 스크립트: `apps/AI/train_yolo.py`.
- **IoU 기반 혼합 오버레이 시각화 (Confusion Matrix Color-Coding)**:
  - 화면 분할 방식의 공간 낭비 문제를 방지하고, 단일 FPV 뷰에서 정답과 추론을 동시 판정:
    - 🟢 **Cyan(청록색) [True Positive]**: 정답 라벨과 AI 추론 박스가 $\text{IoU} \ge 0.4$ 일치할 때 표시.
    - 🔴 **Red(빨간색) [False Positive]**: 오탐 (실제 표적이 없으나 AI가 잘못 탐지).
    - 🟡 **Yellow 점선(노란색) [False Negative]**: 미탐 (실제 표적이 존재하나 AI가 놓침).

---

## 3. 통신 프로토콜 규격 (Port: 9000 CMD / 9001 TLM)

### 3.1. 제어 명령 패킷 (GCS ➔ UE C++, Port: 9000)
- **`MOVE` (수동 비행 조타)**:
  ```json
  {"id":"MOVE", "forward": 1.0, "right": -1.0, "pitch": 0.0, "yaw": 0.0}
  ```
  *(forward: 스로틀 가감속, right: Roll 뱅킹, pitch: 기수 승강, yaw: 방향타 러더)*
- **`GIMBAL` (2축 짐벌 제어)**:
  ```json
  {"id":"GIMBAL", "up": 5.0, "right": 0.0}
  ```
- **`CAPTURE` (즉시 렌더타깃 이미지 & YOLO 라벨 저장)**:
  ```json
  {"id":"CAPTURE"}
  ```
- **`OBSERVER` (관찰자 카메라 모드 순환)**:
  ```json
  {"id":"OBSERVER"}
  ```
- **`AUTONAV` (자동 항법 모드 토글)**:
  ```json
  {"id":"AUTONAV", "enable": 1}
  ```

### 3.2. 텔레메트리 패킷 (UE C++ ➔ GCS, Port: 9001)
```json
{
  "id": "TELEMETRY",
  "seq": 1520,
  "loc_x": 12.5, "loc_y": -4.2, "loc_z": 100.0,
  "rot_pitch": 2.1, "rot_yaw": 85.4, "rot_roll": -24.8,
  "gimbal_pitch": -45.0, "gimbal_yaw": 0.0,
  "speed": 15.0,
  "mode": "MANUAL"
}
```
*(loc_z는 해수면 기준 MSL 고도 미터 단위)*

---

## 4. 모듈 책임 분리 상세 (Module Responsibility Matrix)

| 모듈 | 책임 및 역할 | 입력 | 출력 | 의존성 |
| :--- | :--- | :--- | :--- | :--- |
| `NavFixedWing` | 6자유도 공기역학, 속도/자세/웨이포인트 계산 | 조타 입력, DeltaTime, 목표 위치 | 3D 위치, 속도, 회전 각도 | 순수 C++ (UE 비종속) |
| `ADronePawn` | 액터 트랜스폼 반영, 캡처 컴포넌트 제어 | `NavFixedWing` 스텝 결과, GCS 명령 | Transform, 렌더타깃 이미지 버퍼 | Unreal Engine 5.7 |
| `UCommLink` | UDP 소켓 관리, JSON 역직렬화 및 디스패치 | Port 9000 수신 바이트 스트림 | C++ 명령 이벤트 호출, 텔레메트리 송신 | UE Sockets / Networking |
| `MainWindowVM` | GCS 총괄 비즈니스 로직, 통신/키보드 바인딩 | 텔레메트리 패킷, 키 입력, 모드 버튼 | 상태 텍스트, 하위 VM 조율, UDP 패킷 | CommunityToolkit.Mvvm |
| `TacticalMapVM` | 2.3km 월드 $\leftrightarrow$ 캔버스 2D 투영 변환, 궤적 보관 | 드론 3D 위치, 기수 각도, 캔버스 크기 | `TrajectoryGeometry`, 마커 오프셋 | WPF PresentationCore |
| `ImageWatcherService` | 캡처 디렉터리 모니터링 및 AI 추론 파이프라인 | 저장소 파일 이벤트 (`.png`, `.txt`) | 바운딩 박스 오버레이 비트맵, 카운터 | FileSystemWatcher, ONNX |
| `drone_setup.py` | 비행 환경, 관찰자, 게임모드 인프라 셋업 | 언리얼 에디터 월드 컨텍스트 | 스폰된 드론/관측 폰, 렌더타깃 바인딩 | Unreal Python API |
| `target_setup.py` | 지형 분석 기반 표적 차량 및 배경 절차적 생성 | `config.py`, 랜드스케이프 지형 정보 | `TargetGenActor`, 배치된 스태틱메시 | Unreal Python API |

---

## 5. 실행 및 운영 가이드

### 5.1. 에디터 인프라 및 표적 초기화
1. Unreal Editor 실행 $\rightarrow$ Output Log (Python 콘솔) 확인.
2. 비행 인프라 스폰:
   ```python
   import drone_setup
   drone_setup.setup_drone_infrastructure()
   ```
3. 표적 차량 절차적 배치:
   ```python
   import target_setup
   target_setup.setup_targets()
   ```

### 5.2. GCS 실행 및 자율 비행 관제
1. Visual Studio에서 `DroneMapGCS.sln` 열고 실행 (`net8.0-windows`).
2. UE5 레벨에서 **Play (PIE)** 또는 **Simulate** 시작.
3. GCS 상단에서 `Count`, `X/Y/Alt`, `Speed`, `Roll/Yaw`가 정상 수신되는지 확인.
4. 조종 패널 또는 키보드(`W/A/S/D`, `Q/E`, `Z/C`, `Space`, `V`)로 실시간 수동 비행 및 짐벌 조작.
5. `CAPTURE [C]` 클릭 시 `Saved/DroneCaptures`에 PNG/TXT가 생성되고 FPV 화면에 IoU 3색 바운딩 박스가 오버레이되는지 확인.

---

## 6. 향후 개발 과제 (Next Roadmaps)

1. **상위 미션 오케스트레이터 (`UMissionManager`)**:
   - 다중 웨이포인트(`TArray<FWaypointItemData>`) 기반 순차 비행 및 자동 주기적 캡처 연동.
   - 반경 3m 도달 판정 및 마지막 포인트 이후 40m 반경 Loiter 선회 비행 진입.
2. **전술 지도(TacticalMap) 웨이포인트 인터랙션**:
   - GCS 지도 위를 마우스로 클릭하여 실시간 웨이포인트 경로 지정 및 전송 기능.
3. **정사 모자이크(Orthomosaic) 실시간 맵 스티칭**:
   - 캡처된 항공 사진들의 카메라 자세와 고도를 기반으로 2D 전술 맵 위에 실시간 위성 모자이크 맵 합성.
