# 🛰️ Drone Camera Simulator & Autonomous Digital Tactical Mapping System

---

## 1. 프로젝트 개요 및 아키텍처

### 1.1. 프로젝트 목적 및 핵심 개발 방향
- **드론 항공 카메라 기반 지상 객체(Vehicle) 식별 AI 학습, 자율 수색 비행 및 실시간 디지털 전술 지도(Digital Tactical Map) 매핑 파이프라인 구축**
  - **드론 정밀 비행 및 기체 다형성 아키텍처**: 언리얼 엔진 5.7 C++ 기반의 고정익(Fixed-Wing) 비행 물리, 뱅킹 턴(Bank-Turn) 역학, 2축 짐벌 제어, 관찰자(Observer) 자유/추적 시점 전환
  - **양방향 네트워크 관제 시스템**: 언리얼 시뮬레이터(C++)와 지상 관제 시스템(C# GCS / Python) 간의 비동기 논블로킹 UDP JSON 통신 프로토콜 구축
  - **대규모 합성 데이터셋 자동화 수집**: 고해상도 렌더타깃 이미지, 3D 월드 좌표 투영 기반 정밀 2D YOLO 라벨(.txt) 및 비행/짐벌 메타데이터 CSV 자동 생성
  - **AI 전이 학습 & 추론**: NVIDIA RTX 5070 GPU 가속 기반 YOLOv8 학습 및 실시간 객체 탐지
  - **실시간 지리참조(Georeferencing) 및 전술 매핑**: 핀홀 카메라 역투영 및 공간 클러스터링을 통한 실시간 전술 지도 자동 렌더링 및 항공 정사 모자이크(Orthomosaic) 합성

### 1.2. 개발 환경
- **Engine**: Unreal Engine 5.7
- **Language / IDE**: C++ (Visual Studio 2022), C# (.NET 8 / WPF GCS), Python 3.10+
- **Deep Learning / AI**: PyTorch (CUDA 12.x 가속), Ultralytics YOLOv8, OpenCV
- **Hardware Acceleration**: NVIDIA GeForce RTX 5070 (12GB VRAM)

### 1.3. 핵심 설계 원칙 (Architecture Principles)
- **순수 C++ 항법 엔진 분리 (Engine-Agnostic Navigation Engine)**:
  - 언리얼 엔진 종속성(`UCLASS`, `UActorComponent`)을 완전히 배제한 **순수 C++ 클래스(`NavBase`, `NavFixedWing`)** 로 항법/물리 엔진 구축
  - 드론 폰(`ADronePawn`)은 비행 물리 계산을 직접 하지 않고, 매 틱마다 `NavEngine->Step()`을 호출하여 위치/각도만 반영받는 극단적 경량화 달성
- **단일 창구 입력 체계 (Single Entry-Point Command Flow)**:
  - 언리얼 내부 키보드 입력이든 C# GCS 네트워크 패킷이든 **100% 동일한 JSON 규격으로 변환되어 `UCommLink::ProcessJsonCommand()` 단일 진입점**만을 통과
- **실제 항공기 조타면 역학 (Aero Surface Inertia & Auto-Decay)**:
  - 통신 주기나 패킷 수신 빈도와 무관하게, 드론 내부 필터(`FInterpTo`)와 조타면 자동 중립 복귀(Auto Neutral Decay)를 통해 자연스러운 뱅크 턴(Bank Turn) 비행 구현
- **절대 해수면 고도(MSL) 표준화**:
  - 언리얼 월드 절대 원점 기준의 **MSL(Mean Sea Level, Absolute Z)** 을 텔레메트리 및 항법의 단일 표준으로 채택

---

## 2. 핵심 구현 현황

### 2.1. 순수 C++ 고정익 항법 엔진 (`NavBase.h`, `NavFixedWing.h / .cpp`)
- **계층형 네이밍 규칙**: `NavBase`, `NavFixedWing` (향후 `NavMulticopter`, `NavVTOL` 확장 구조)
- **고정익 실속 방지 및 속도 제어**: 최소 속도(10 m/s = 1000 cm/s) ~ 최대 속도(30 m/s = 3000 cm/s), 스로틀 가감속
- **공기역학적 연동 선회 (Coordinated Bank-Turn)**:
  - Roll(에일러론) 입력 시 기체 날개를 최대 40도까지 기울이고, 뱅크각에 비례하여 기수(Yaw)가 자연스럽게 호를 그리며 선회하도록 수식 통합:
    $$\text{TurnRate} = \left(\frac{\text{Roll}}{40^\circ}\right) \times 45^\circ/\text{s} + \text{RudderRate}$$
- **조타 입력 자체 스무딩 및 자동 중립 감쇠**:
  - 외부 조작이 중단되면 기체 스스로 조타면을 중립(0.0)으로 서서히 복원하여 수평 순항 상태 유지

### 2.2. 언리얼 비행체 및 통신 컴포넌트 (`ADronePawn`, `UCommLink`)
- **`ADronePawn` 경량화**: `TUniquePtr<INavBase> NavEngine`과 `ApplyManualControl()` 단일 브릿지만 유지
- **`UCommLink` 입력 일원화**: 모든 키보드 입력(WASD, EQ, CZ, 방향키, Space, O)을 JSON 명령(`MOVE`, `GIMBAL`, `CAPTURE`, `OBSERVER`)으로 직렬화하여 처리
- **MSL 절대 고도 텔레메트리 10Hz 송신**: 센티미터 좌표를 미터 단위 절대 MSL 고도로 변환하여 브로드캐스트

### 2.3. C# WPF GCS 연동 (`MainWindowVM.cs`, `DroneCommands.cs`)
- **표준 비행 조종 규격(`MoveCommand`)**: `MoveCommand(forward, right, pitch, yaw)`를 `float` 정규화 단위(-1.0f ~ +1.0f)로 통합
- **스틱 상태 기반 제어**: 키 누름/뗌 이벤트에 따른 조타 및 중립 복귀 처리
- **텔레메트리 연동**: Roll/Yaw 자세 및 MSL 고도 실시간 모니터링 반영

---

## 3. 통신 프로토콜 규격 (Port: 9000 CMD / 9001 TLM)

### 3.1. 제어 명령 패킷 (GCS / Keyboard ➔ Unreal C++, Port: 9000)
- **`MOVE` (수동 비행 조타)**:
  ```json
  {"id":"MOVE", "forward": 1.0, "right": -1.0, "pitch": 0.0, "yaw": 0.0}
  ```
  *(forward: 스로틀 가감속, right: Roll 뱅킹 좌우, pitch: 기수 상하, yaw: 러더 좌우)*
- **`GIMBAL` (2축 짐벌 각도 제어)**:
  ```json
  {"id":"GIMBAL", "up": 5, "right": 0}
  ```
- **`CAPTURE` (즉시 렌더타깃 이미지 & YOLO 라벨 저장)**:
  ```json
  {"id":"CAPTURE"}
  ```
- **`OBSERVER` (관찰자 시점 Chase / TopDown / Free 순환)**:
  ```json
  {"id":"OBSERVER"}
  ```

### 3.2. 텔레메트리 패킷 (Unreal C++ ➔ GCS, Port: 9001)
```json
{
  "id": "TELEMETRY",
  "seq": 1520,
  "loc_x": 12.5, "loc_y": -4.2, "loc_z": 100.0,
  "rot_pitch": 2.1, "rot_yaw": 85.4, "rot_roll": -24.8,
  "gimbal_pitch": -90.0, "gimbal_yaw": 0.0,
  "speed": 15.0,
  "mode": "MANUAL"
}
```
*(loc_z는 순수 MSL 절대 고도 미터 단위)*

---

## 4. 새로운 세션에서 진행할 핵심 개발 과제 (Next Steps)

1. **순수 C++ 고정익 자율 항법 복구 및 통합 (`NavFixedWing`)**:
   - `INavBase`에 `StartMission(const TArray<FWaypointItemData>& Waypoints)` API 표준 추가
   - 3m 반경 내 도달 시 즉시 다음 웨이포인트로 전환하는 논스톱(Non-stop) 선회 항법 연동
   - 마지막 웨이포인트 완주 후 반경 40m 원형 선회(Loiter Mode, Roll 25도 고정 뱅크) 모드 구현
2. **자동 촬영(Periodic Capture) 및 YOLO 데이터셋 파이프라인 연계**:
   - 자율 비행 중 설정된 시간 간격(예: 2.5초)으로 자동 `ExecuteCapture()` 트리거
   - 생성된 이미지와 바운딩 박스 라벨의 실시간 폴더 감시(`ImageWatcherService`) 및 GCS 수신
3. **C# WPF GCS 전술 지도(Tactical Map) 위 웨이포인트 가시화**:
   - GCS 화면의 전술 지도 캔버스 위에 드론의 실시간 위치/헤딩 아이콘 렌더링
   - 직사각형 수색 격자(Search Grid) 웨이포인트 전송 UI 및 비행 경로 궤적 시각화
