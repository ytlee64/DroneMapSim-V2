# 🛰️ DroneMapSim
### 언리얼 엔진 5 기반 절차적 지형·식생 및 공대지 표적 정찰 시뮬레이터
> **Subtitle (EN)**: *UE5 Procedural Environment & Air-to-Ground Reconnaissance Simulator*

---

## 1. 프로젝트 개요 (Project Overview)

### 1.1. 연구 및 개발 목적 (Objectives)
- **드론 항공 영상(UAV Aerial Imagery) 기반 지상 차량(Ground Vehicle) 식별 AI의 엔드투엔드 학습·검증 파이프라인 구축**
  - **UAV 비행 및 다각도 카메라 제어**: 자유로운 기체 기동 및 GCS(Ground Control Station) 관제 시점 실시간 추적
  - **절차적 산림 환경(Procedural Forest) 조성**: 고도 데이터 기반 랜드스케이프 및 수목 차폐율을 반영한 현실적 자연 배경/차량 배치
  - **합성 데이터셋(Synthetic Dataset) 자동 수집**: 드론 운용 및 스캔 피라미드 시각화를 통한 고해상도 공대지(Air-to-Ground) 정찰 영상 확보
  - **지상 표적 식별 AI 모델 학습**: 가상 시뮬레이션을 통해 대규모로 생성된 합성 데이터를 활용한 딥러닝 객체 검출 모델 훈련
  - **실시간 인-시뮬레이션 검증**: 시뮬레이터와 AI 추론 모듈을 연동하여 다양한 식생 밀도/기상/고도 조건에서의 은폐 차량 실시간 식별 성능 검증

### 1.2. 개발 환경 및 기술 스택 (Technical Stack)
- **Game Engine**: Unreal Engine 5.7
- **Core Languages & IDE**: C++ (Visual Studio 2022), Python 3.x
- **Graphics & Assets**: Hierarchical Instanced Static Mesh (HISM), Stylized Foliage (`EasyBuildingSystem`), Landscape DEM/Satellite Map
- **Architecture Design**: Minimalist Object-Oriented Programming (OOP), Single Source of Truth (SSOT)

### 1.3. 핵심 아키텍처 원칙 (Core Principles)
- **모듈화 및 네임스페이스 일원화**: `DroneMapSim` 단일 루트 기반 C++/Python/Asset 패스 통합
- **시선 분리형 단방향 제어**: 기체 조종권(Possess)과 관찰자 카메라(View Target)의 완전 분리를 통한 제어 무결성 확립
- **멱등성(Idempotency) 보장 파이프라인**: 반복 실행에도 쓰레기 잔여물 없이 항상 최적 상태로 복구되는 에디터 자동화

---

## 2. 세부 시스템 아키텍처 및 구현 (System Architecture & Implementation)
+-----------------------------------------------------------------------------------+
| AeroVision-Sim Architecture |
+-----------------------------------------------------------------------------------+
| [C++ Core Engine] |
| - BP_DronePawn (Flight Control & Continuous Possess) |
| - ObserverPawn (Chase View -35° / Satellite View -90° Centered Tracking) |
| - DroneMapSimGameMode (Safe Init & View Target Binding) |
| - ForestGeneratorActor (HISM Batching, Slope-Aware Multi-Species Distribution) |
+-----------------------------------------+-----------------------------------------+
|
v
+-----------------------------------------+-----------------------------------------+
| [Air-to-Ground Sensor & Visualization] |
| - SceneCaptureComponent2D -> RT_DroneCapture (LDR Final Color RGB) |
| - BP_ScanPyramid (M_ScanProjection, M_ScanGround Frustum Procedural Mesh) |
| - Dynamic Landscape Center Teleportation (Raycast Altitude Auto-Correction) |
+-----------------------------------------+-----------------------------------------+
|
v
+-----------------------------------------+-----------------------------------------+
| [Python Automation Layer] |
| - config.py (SSOT Parameter Architecture) |
| - drone_setup.py (1-Click Scene Reset, Idempotent Actor Clearance & Auto Setup) |
| - Procedural Foliage Generation & Fail-Fast Validation |
+-----------------------------------------------------------------------------------+

---

### 2.1. 드론 비행 제어 및 관제 시스템 (UAV Flight & GCS Control)

#### 1) 시선 분리형 단방향 조종 아키텍처
- **조종권(Possess) 영구 고정 (`BP_DronePawn`)**:
  - 사용자 입력(W, A, S, D, Space, C)을 드론 조종석에 독점 고정하여 조종권 전환으로 인한 입력 유실 원천 차단
- **관찰자 시점 실시간 락온 추적 (`ObserverPawn`)**:
  - 드론 기수(Yaw/Heading)를 추적하여 드론이 항상 **화면 정중앙**에 배치되도록 카메라 위치 및 정조준(Look-At) 회전 동기화
  - **Chase View**: 기체 후방 4.5m, 상방 2.2m에서 -35° 각도로 추적하는 정밀 비행 모드
  - **Satellite View**: 기체 직상방 15.0m에서 -90° 수직 하방을 관제하는 탑다운 위성 뷰
- **초기화 지연 방어 게임모드 (`DroneMapSimGameMode`)**:
  - `DefaultPawnClass = nullptr` 설정을 통한 불필요한 더미 스폰 억제 및 안전 지연(Timer) 기반 뷰타겟 체결

#### 2) 지형 적응형 자동 안착 시스템 (`DronePawn.cpp`)
- **다형성 랜드스케이프 통합 바운드 자동 연산**:
  - 단일 `Landscape` 액터 및 분할된 `LandscapeStreamingProxy` 조각 전체의 바운딩 박스를 병합하여 지형의 정확한 정중앙 좌표 산출
- **수직 레이캐스트 기반 안전 고도 스폰**:
  - 랜드스케이프 최상단에서 하방 레이캐스트(`LineTraceSingleByChannel`)를 수행하여 지표면 기준 30m 안전 고도로 자동 텔레포트

#### 3) 공대지 씬 캡처 및 3D 스캔 시각화 (Air-to-Ground Sensing)
- **실시간 씬 캡처 (SceneCapture2D)**:
  - 기체 하방 캡처 센서와 렌더 타겟(`RT_DroneCapture`)을 직렬 연결하고 `Final Color (LDR) in RGB` 포맷으로 실시간 프레임 스트리밍
- **절사두 사각뿔(Frustum) 프로시저럴 메쉬 (`BP_ScanPyramid`)**:
  - 반투명 사영 머터리얼(`M_ScanProjection`)과 지표면 스캔 머터리얼(`M_ScanGround`)을 오버라이드하여 카메라 화각 및 지상 스캔 영역을 3차원으로 시각화

---

### 2.2. 절차적 숲/식생 자동 생성 시스템 (Procedural Forest Generation)

#### 1) 고성능 C++ 식생 엔진 (`ForestGeneratorActor`)
- **HISM(Hierarchical Instanced Static Mesh) 기반 렌더링 최적화**:
  - 수천 개의 식생을 단일 드로우콜로 병합하여 실시간 60+ FPS 렌더링 보장
- **모빌리티 계층 불일치 방어**:
  - `RootComponent` 및 모든 `HISM` 컴포넌트의 모빌리티를 `Static`으로 통일하여 에디터 계층 경고 및 크래시 제거
- **거리 컬링 해제**:
  - `bDisableDistanceCulling = true` 설정을 적용하여 고고도 비행 시 원거리 식생이 원형으로 잘려나가는 현상 방지
- **지형 경사도(Slope-Aware) 적응형 스폰**:
  - 지표면 법선 벡터(Normal Vector)를 추출하여 수종별 한계 경사도(40°~70°) 이내의 안전 지형에만 자연스럽게 배치

#### 2) 다중 식생 10종 생태계 비율 설계
- **교목류 4종 (60%)**: 소나무(`SM_Tree_001`), 슬림 침엽수(`SM_Tree_002`), 피라미드 침엽수(`SM_Tree_003`), 활엽수(`SM_Tree_004`)
- **관목/뿌리 2종 (15%)**: 고사리(`SM_Fern_001`), 덤불 뿌리(`SM_Roots_001`)
- **초본류 3종 (17%)**: 기본 잔디(`SM_Grass_001`), 키 큰 풀(`SM_Grass_002`), 밀집 들풀(`SM_Grass_003`)
- **야생화 2종 (8%)**: 보라꽃(`SM_Flowers_001`), 주황꽃(`SM_Flowers_002`)

---

### 2.3. 파이썬 기반 원클릭 셋업 자동화 파이프라인 (`drone_setup.py`)

#### 1) 멱등성(Idempotent) 환경 초기화
- 새로운 레벨 또는 기존 레벨에서 스크립트 실행 시, 기존에 잔류하던 드론/관제 액터(`BP_DronePawn`, `BP_ObserverPawn`, `BP_ScanPyramid`)를 전수 검사하여 깨끗이 소거(Clean-up)한 뒤 재배치

#### 2) 에디터 서브시스템 기반 원클릭 인프라 구성
- **스마트 에셋 로더**: UE 5.7 `AssetRegistry` API를 준수하여 프로젝트 내 폴더 위치에 구애받지 않고 에셋 이름으로 100% 탐색 및 로드
- **자동 프로퍼티 바인딩**:
  - `BP_DronePawn` 스폰 ➔ `AutoPossess: Player 0` 할당 ➔ `RT_DroneCapture` 및 `SCS_FinalColorLDR` 씬 캡처 연결
  - `BP_ScanPyramid` 스폰 ➔ 반투명 스캔 머터리얼 자동 오버라이드
  - `BP_ObserverPawn` 관제탑 스폰
  - 최신 `UnrealEditorSubsystem`을 통한 레벨의 `DefaultGameMode`를 `BP_DroneMapSimGameMode`로 자동 치환

---

## 3. 향후 연구 및 확장 과제 (Future Work)

1. **지상 표적(Vehicle) 객체 탐지 AI(YOLOv8/v10/RT-DETR) 실시간 연동**:
   - `RT_DroneCapture` 프레임을 무지연 소켓(UDP/Shared Memory)으로 추출하여 파이썬 AI 추론 모듈과 실시간 통신
2. **산림 차폐율(Occlusion) 조건에 따른 탐지 성능 정량 평가**:
   - 식생 밀도(0% ~ 100%) 및 드론 고도/각도 변화에 따른 차량 식별 mAP/Recall 분석 논문 실험 수행
3. **WGS84 GPS 좌표계 변환 및 텔레메트리 비동기 로깅**:
   - 3D 언리얼 월드 좌표를 실제 위/경도(WGS84) 및 비행 고도 데이터로 실시간 변환하는 지상관제소 로그 파이프라인 확장