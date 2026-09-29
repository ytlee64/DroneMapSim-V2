# 🛰️ Drone Camera Simulator & Autonomous Digital Tactical Mapping System

---

## 1. 프로젝트 개요 및 최신 환경 아키텍처

### 1.1. 프로젝트 목적 및 핵심 개발 방향
- **드론 항공 카메라 기반 지상 객체(Vehicle) 식별 AI 학습, 자율 수색 비행 및 실시간 디지털 전술 지도(Digital Tactical Map) 맵핑 파이프라인 구축**
  - **드론 정밀 비행 및 다축 관제**: 언리얼 엔진 5.7 C++ 기반의 오차 없는 미터(m) 단위 정밀 비행, 2축 짐벌 제어, 관찰자(Observer) 자유/추적 시점 전환
  - **양방향 네트워크 관제 시스템**: 언리얼 시뮬레이터(C++)와 지상 관제 시스템(C# GCS / Python) 간의 비동기 논블로킹 UDP JSON 통신 프로토콜 구축
  - **대규모 합성 데이터셋 자동화 수집**: 다중 고도/각도 격자 비행을 통한 고해상도 렌더타깃 이미지 및 2D 투영 바운딩 박스 라벨 자동 생성
  - **AI 전이 학습 & 추론**: NVIDIA RTX 5070 GPU 가속 기반 YOLOv8 학습 및 실시간 객체 탐지
  - **실시간 지리참조(Georeferencing) 및 전술 매핑**: 핀홀 카메라 역투영 및 공간 클러스터링을 통한 실시간 전술 지도 자동 렌더링 및 항공 정사 모자이크(Orthomosaic) 합성

### 1.2. 개발 환경
- **Engine**: Unreal Engine 5.7
- **Language / IDE**: C++ (Visual Studio 2022), C# (.NET 8 / WPF GCS), Python 3.10+
- **Deep Learning / AI**: PyTorch (CUDA 12.x 가속), Ultralytics YOLOv8, OpenCV
- **Hardware Acceleration**: NVIDIA GeForce RTX 5070 (12GB VRAM)
- **Repository Management**: Git (대용량 빌드 캐시/바이너리 제외 최적화 완료, `Content/` 에셋 동기화 관리)

### 1.3. 핵심 설계 원칙 및 책임 분리 (Architecture Principles)
- **책임의 분리 (Separation of Concerns)**:
  - **GCS (C# WPF)**: "임무 계획자(Mission Planner)" — 비행 임무 생성, 웨이포인트 목록(`WAYPOINT_LIST`) 전달, 수신 텔레메트리 시각화
  - **드론 (Unreal Engine C++)**: "물리적 실행자(Flight Controller)" — 경로 추종, 최대 속도/가속도 클램핑, 안전 고도 유지 및 충돌 방어, 도달 반경(Acceptance Radius) 판정
  - **맵에 액터 배치 (Unreal Engine Python Script)**: `(drone_setup.py, env_setup.py)` — 드론 관련 액터 배치, 맵에 타겟/식생 절차적 배치
- **무결성 및 안정성**: 단일 진실 공급원(SSOT), 프레임 드랍 없는 논블로킹 UDP 통신, 객체 지향적 단일 조종 대상(`TargetActor`) 제어 구조 확립

---

## 2. 세부 개발 내용 및 최근 업데이트 내역

### 2.1. 언리얼 C++ 드론 비행 제어 및 입력 처리 (`UDroneCommandReceiver`)

#### 1) 단발성 키보드 입력 체계 개선 (`ProcessInputKeyboard`)
- 기존 `IsInputKeyDown()`의 연속 호출로 인한 제어 폭주 문제를 해결하기 위해, `WasInputKeyJustPressed()` 기반으로 전면 리팩터링
- 키를 1회 누를 때마다 사전에 정의된 정밀 변위(이동 3m, 짐벌 5도, 회전 3도 등)만 정확히 1회 명령(`MOVE`, `GIMBAL`)으로 인코딩하여 송신

#### 2) 정밀 오차 0.00mm 미터(m) 단위 직접 이동 (`MOVE` 패킷)
- `Normalize()` 및 임의의 `MoveSpeed` 계수를 전면 제거
- JSON으로 전달된 `forward`, `right`, `up` (미터 단위)에 언리얼 좌표 변환 계수 `100.0f` (1m = 100cm)만 곱해 기체 로컬 벡터에 직접 가산
- `Yaw`는 거리 단위가 아닌 각도(Degree)이므로 스케일링 없이 순수 회전 각도로 적용
- **FreeRoam 모드 버그 수정 및 코드 단일화**:
  - 기존 옵저버 FreeRoam 모드 시 드론이 옵저버 위치로 순간이동하던 버그를 수정
  - 삼항 연산자를 통해 조종 대상 액터(`TargetActor`) 포인터를 단일화하여 40줄의 중복 코드를 15줄의 견고한 로직으로 압축:
    ```cpp
    AActor* TargetActor = (CachedObserverActor && CachedObserverActor->TrackingMode == EObserverTrackingMode::FreeRoam)
        ? Cast<AActor>(CachedObserverActor)
        : Cast<AActor>(CachedDronePawn);
    ```

#### 3) 관찰자 시점 및 캡처 시스템
- `OBSERVER` 명령 수신 시 `AObserverPawn::SetTrackingMode`와 즉시 연동하여 Chase / Satellite / FreeRoam 시점 실시간 순환 전환
- `CAPTURE` 명령 수신 시 드론 본체(`ADronePawn::ExecuteCapture`)가 파일명(`DRONE_..._0001.png`)과 디스크 저장을 주도적으로 처리하도록 정석화

---

### 2.2. UDP 통신 프로토콜 규격 및 C# GCS DTO 동기화

#### 1) 명령 프로토콜 (GCS ➔ Unreal, Port: 9000)
- **`MOVE` (정밀 이동/회전)**:
  `{"id":"MOVE", "forward": 5.0, "right": 0.0, "up": 0.0, "yaw": 15.0}`
- **`GIMBAL` (짐벌 제어)**:
  `{"id":"GIMBAL", "up": 5.0, "right": 0.0}`
- **`CAPTURE` (사진 촬영)**:
  `{"id":"CAPTURE"}`
- **`OBSERVER` (시점 전환 서클 체인지)**:
  `{"id":"OBSERVER"}`
- **`WAYPOINT_LIST` (향후 작업 예정)**:
  `{"id":"WAYPOINT_LIST", "points":[{"x":10, "y":20, "z":30}, ...]}`

#### 2) 텔레메트리 프로토콜 (Unreal ➔ GCS, Port: 9001)
- 언리얼 엔진에서 10Hz 주기로 브로드캐스트하는 JSON 구조:
  ```json
  {
    "id": "TELEMETRY",
    "seq": 1042,
    "loc_x": 12.5, "loc_y": -4.2, "loc_z": 30.0,
    "rot_pitch": 0.0, "rot_yaw": 90.0, "rot_roll": 0.0,
    "gimbal_pitch": -60.0, "gimbal_yaw": 0.0,
    "speed": 0.0,
    "mode": "MANUAL",
    "reached": false
  }
  ```

#### 3) C# GCS 완벽 매핑 DTO (`DroneTelemetryPacket.cs`)
- 언리얼의 개별 필드(`loc_x`, `rot_pitch` 등)와 1:1 매핑되면서, 기존 C# UI 바인딩과의 호환성을 위해 `[JsonIgnore]` 배열 헬퍼(`Loc`, `Rot`, `Gimbal`)를 내장:
  ```csharp
  public class DroneTelemetryPacket
  {
      [JsonPropertyName("id")] public string Id { get; set; } = "TELEMETRY";
      [JsonPropertyName("seq")] public long Seq { get; set; }
      [JsonPropertyName("loc_x")] public double LocX { get; set; }
      [JsonPropertyName("loc_y")] public double LocY { get; set; }
      [JsonPropertyName("loc_z")] public double LocZ { get; set; }
      [JsonPropertyName("rot_pitch")] public double RotPitch { get; set; }
      [JsonPropertyName("rot_yaw")] public double RotYaw { get; set; }
      [JsonPropertyName("rot_roll")] public double RotRoll { get; set; }
      [JsonPropertyName("gimbal_pitch")] public double GimbalPitch { get; set; }
      [JsonPropertyName("gimbal_yaw")] public double GimbalYaw { get; set; }
      [JsonPropertyName("speed")] public double Speed { get; set; }
      [JsonPropertyName("mode")] public string Mode { get; set; } = "MANUAL";
      [JsonPropertyName("reached")] public bool Reached { get; set; }

      [JsonIgnore] public double[] Loc => new double[] { LocX, LocY, LocZ };
      [JsonIgnore] public double[] Rot => new double[] { RotPitch, RotYaw, RotRoll };
      [JsonIgnore] public double[] Gimbal => new double[] { GimbalPitch, GimbalYaw };
  }
  ```

---

### 2.3. 저장소(Git) 및 디렉터리 최적화 내역
- **37GB 대용량 원인 규명 및 정리**:
  - `Saved/` (19.77GB 크래시/덤프/캡처), `Python/` (10.33GB venv/가중치), `.vs/` (3.58GB), `Intermediate/` (2.58GB) 식별 완료
  - `.gitignore` 최적화: 불필요한 임시/빌드 폴더는 영구 제외하고, 프로젝트 핵심 소스코드 및 필수 맵 에셋(`Content/`, 약 170MB)만 안정적으로 커밋/푸시되도록 설정 완료

---

## 3. 새로운 세션에서 진행할 핵심 개발 과제 (Next Steps)
1. **자율 웨이포인트(Waypoint) 비행 엔진 구현 (Unreal C++)**:
   - GCS로부터 수신된 웨이포인트 큐(Queue) 순차 비행 로직 구현
   - 물리적 한계 방어: 최대 비행 속도 클램핑, 급격한 각속도 제한, 지형 충돌 방지 최소 안전 고도 보정
   - 도달 판정(Acceptance Radius, 예: 1.5m) 감지 시 정지 ➔ 사진 촬영 트리거 ➔ 다음 웨이포인트 가속 전환 및 `reached: true` 텔레메트리 피드백
2. **C# WPF GCS 경로 계획 UI 연동**:
   - 2D 지도 상에서 마우스 클릭으로 웨이포인트 지정 및 고도 설정 인터페이스 구현
   - 전송 버튼 클릭 시 `WAYPOINT_LIST` JSON 생성 및 UDP 9000번 포트 송신

