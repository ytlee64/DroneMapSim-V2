# DroneMapSim GCS ↔ UE5 UDP JSON 통신 프로토콜 명세서 (Communication Protocol Specification v2.0)

본 문서는 **지상관제국(Python GCS / C# WPF GCS)**과 **언리얼 엔진 5 드론 시뮬레이터(UE5 ADronePawn / UDroneCommandReceiver)** 간의 양방향 UDP 통신 규격, 제어 명령(Commands), 관찰자 카메라 제어, 실시간 텔레메트리(Telemetry) 스트림 포맷 및 고도별 최적 파라미터 표준을 정의합니다.

---

## 1. 네트워크 통신 아키텍처 개요

DroneMapSim은 실시간성과 경량성을 보장하고 특정 프로그래밍 언어에 종속되지 않도록 **UDP 소켓 기반의 UTF-8 JSON 패킷 방식**을 사용합니다. 수동 조종과 자율 비행 간의 별도 모드 전환 없이, 수신된 명령의 성격에 따라 즉시 심리스(Seamless)하게 반응하도록 설계되었습니다.

```
+-----------------------------------+                       +---------------------------------------+
|       GCS 지상관제국              |                       |         UE5 드론 시뮬레이터           |
| (Python GCS / C# WPF Controller)  |                       |   (ADronePawn + DroneCommandReceiver) |
+-----------------------------------+                       +---------------------------------------+
                  |                                                             |
                  | ----------- [Port 9000] JSON 제어 명령 (비동기) -----------> |
                  |             (TELEPORT, FLY_TO, MANUAL_MOVE,                 |
                  |              SET_OBSERVER_MODE, SET_GIMBAL ...)             |
                  |                                                             |
                  | <---------- [Port 9001] 10Hz 텔레메트리 브로드캐스트 --------|
                  |             (loc, rot, gimbal, observer_mode, reached)      |
```

### 포트 및 전송 주기 규격

| 구분 | 포트 (Port) | 전송 방향 | 패킷 주기 | 인코딩 |
| :--- | :--- | :--- | :--- | :--- |
| **Command Socket** | **`9000`** | GCS ➔ UE5 Drone | 이벤트 발생 시 / 조이스틱 루프 (20~50Hz) | UTF-8 JSON String |
| **Telemetry Socket** | **`9001`** | UE5 Drone ➔ GCS | **10Hz (0.1초 주기 스트림)** | UTF-8 JSON String |

---

## 2. 표준 물리 단위 체계 (Standard Units)

언리얼 엔진 5의 네이티브 좌표계는 **센티미터(`cm`)** 기준이므로, 통신 패킷은 좌표 계산의 직관성과 불필요한 연산 오차를 방지하기 위해 센티미터를 표준으로 채택합니다.

| 물리량 | 표준 단위 | 기호 | 변환식 및 예시 |
| :--- | :--- | :--- | :--- |
| **거리 및 위치 (X, Y, Z)** | 센티미터 | `cm` | $1\,\text{m} = 100\,\text{cm}$<br>• $-50\,\text{m} \rightarrow -5,000\,\text{cm}$<br>• $65\,\text{m} \rightarrow 6,500\,\text{cm}$<br>• $500\,\text{m} \rightarrow 50,000\,\text{cm}$ |
| **비행 속도 (Speed)** | 센티미터/초 | `cm/s` | $1\,\text{m/s} = 100\,\text{cm/s}$<br>• $18\,\text{m/s} \rightarrow 1,800\,\text{cm/s}$<br>• $60\,\text{m/s} \rightarrow 6,000\,\text{cm/s}$ |
| **회전 각도 (Pitch, Yaw, Roll)** | 도 | `deg (°)` | • Pitch: $-90^\circ$ (직하) $\sim +90^\circ$ (상향)<br>• Yaw: $0^\circ$ (동쪽) $\sim 360^\circ$<br>• Roll: $0^\circ$ (수평 유지) |
| **정규화 수동 입력 (Axis)** | 무차원 | `norm` | 조이스틱/키보드 축 입력: $-1.0 \sim +1.0$ |

---

## 3. GCS ➔ UE5 제어 명령 명세 (`Port 9000`)

모든 명령은 JSON 객체 내 최상위 `"cmd"` 필드로 구분되며, 대소문자를 구분하지 않습니다.

### 3.1. `TELEPORT` (시작점 즉시 순간이동 및 물리 안정화)

미션 시작점이나 테스트 구역으로 0초 만에 기체를 워프시킵니다. 기존 자율비행은 즉시 취소되고 제자리에 정지합니다.

#### 패킷 구조
```json
{
  "cmd": "TELEPORT",
  "x": -5000.0,
  "y": -4000.0,
  "z": 6500.0,
  "yaw": 0.0,
  "reset_velocity": true
}
```

#### 파라미터 필드
| 필드명 | 타입 | 단위 | 필수 | 기본값 | 설명 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `cmd` | `string` | - | **Yes** | - | `"TELEPORT"` |
| `x`, `y`, `z` | `float` | cm | **Yes** | - | 월드 3D 목표 좌표 |
| `yaw` | `float` | deg | No | 현재각 | 배치 시 드론 기수 방향 |
| `reset_velocity` | `bool` | - | No | `true` | 물리 선속도/각속도 0 초기화 |

---

### 3.2. `FLY_TO` (좌표 기반 자율 순항 항법)

물리 엔진의 부드러운 위치 보간과 기수 정렬(Heading Alignment)을 거쳐 지정 좌표로 비행합니다.

#### 패킷 구조
```json
{
  "cmd": "FLY_TO",
  "x": 5000.0,
  "y": -4000.0,
  "z": 6500.0,
  "speed": 1800.0,
  "yaw": 0.0
}
```

#### 파라미터 필드
| 필드명 | 타입 | 단위 | 필수 | 기본값 | 설명 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `cmd` | `string` | - | **Yes** | - | `"FLY_TO"` |
| `x`, `y`, `z` | `float` | cm | **Yes** | - | 목표 웨이포인트 3D 월드 좌표 |
| `speed` | `float` | cm/s | No | `1800.0` | 순항 속도 ($18\text{m/s} = 1800$) |
| `yaw` | `float` | deg | No | 자동 | 지정 시 해당 기수 유지, 미지정 시 이동 방향 정렬 |

---

### 3.3. `MANUAL_MOVE` (수동 조이스틱/키보드 비행 조종)

GCS의 키보드(WASD), 조이스틱, 게임패드 신호를 드론 기체 로컬 축으로 직접 전달합니다.
**동작 규칙**: 비행 중 `FLY_TO`가 수행 중이더라도 `MANUAL_MOVE`가 들어오는 순간 자율비행은 즉시 중단(Safety Override)되며 수동 입력에 실시간 반응합니다.

#### 패킷 구조
```json
{
  "cmd": "MANUAL_MOVE",
  "x": 1.0,
  "y": 0.0,
  "z": 0.5,
  "yaw_rate": -0.2
}
```

#### 파라미터 필드
| 필드명 | 타입 | 범위 | 필수 | 설명 |
| :--- | :--- | :--- | :--- | :--- |
| `cmd` | `string` | - | **Yes** | `"MANUAL_MOVE"` |
| `x` | `float` | $-1.0 \sim +1.0$ | **Yes** | 전진(+1.0) / 후진(-1.0) |
| `y` | `float` | $-1.0 \sim +1.0$ | **Yes** | 우측 이동(+1.0) / 좌측 이동(-1.0) |
| `z` | `float` | $-1.0 \sim +1.0$ | **Yes** | 고도 상승(+1.0) / 하강(-1.0) |
| `yaw_rate` | `float` | $-1.0 \sim +1.0$ | No | 시계방향 우회전(+1.0) / 반시계 좌회전(-1.0) |

---

### 3.4. `SET_OBSERVER_MODE` (관찰자 카메라 모드 제어)

시뮬레이터에서 드론을 3인칭으로 관찰하는 카메라의 위치 종속성을 원격 제어합니다.

#### 패킷 구조
```json
{
  "cmd": "SET_OBSERVER_MODE",
  "mode": "STATIONARY"
}
```

#### 파라미터 필드
| 필드명 | 타입 | 필수 | 허용 값 | 설명 |
| :--- | :--- | :--- | :--- | :--- |
| `cmd` | `string` | **Yes** | `"SET_OBSERVER_MODE"` | 관찰자 시점 제어 명령 |
| `mode` | `string` | **Yes** | `"CHASE"`, `"STATIONARY"`, `"TOGGLE"` | • `"CHASE"`: 드론 몸통에 Attach되어 드론을 계속 따라다님 (상대적 위치)<br>• `"STATIONARY"`: **드론과 분리(Detach)되어 현재 월드 위치에 삼각대처럼 고정. 드론이 어디로 날아가든 그 자리에 우두커니 서서 멀어지는 드론을 관찰 (절대적 위치)**<br>• `"TOGGLE"`: 상대 $\leftrightarrow$ 절대 모드 상호 전환 |

---

### 3.5. `SET_GIMBAL` (카메라 짐벌 각도 제어)

카메라 짐벌의 Pitch(상하) 및 Yaw(좌우) 기계식 각도를 직접 제어합니다.

#### 패킷 구조
```json
{
  "cmd": "SET_GIMBAL",
  "pitch": -85.0,
  "yaw": 0.0
}
```

#### 권장 짐벌 피치 설정
- **`-85.0°` (수직 직하, Nadir)**: 정사영상 모자이크(Orthomosaic) 제작에 최적. 지평선 왜곡 및 기하학적 투영 오차 최소화.
- **`-60.0°` (사선 정찰, Oblique)**: 건물 입체 정찰 및 수목 아래 숨은 차량의 측면 실루엣/번호판 탐색.

---

### 3.6. `TOGGLE_GIMBAL` (짐벌 축 회전 순서 전환)

물리 짐벌의 외측-내측 축 결합 우선순위(`YawOuter_PitchInner` $\leftrightarrow$ `PitchOuter_YawInner`)를 변경합니다.
```json
{
  "cmd": "TOGGLE_GIMBAL"
}
```

---

### 3.7. `CAPTURE_IMAGE` (고해상도 캡처 및 AI 라벨링 자동 생성)

드론 카메라 뷰를 PNG 이미지로 저장하고, 화면에 포착된 모든 지상 객체(차량 등)의 YOLO 2D Bounding Box(`TXT`) 및 비행 메타데이터(`Dataset_Log.csv`)를 동기화하여 저장합니다.
```json
{
  "cmd": "CAPTURE_IMAGE"
}
```

---

### 3.8. `HOVER` (제자리 일시 정지)

진행 중인 자율 비행 웨이포인트를 즉시 취소하고 현재 좌표와 고도를 유지합니다.
```json
{
  "cmd": "HOVER"
}
```

---

## 4. UE5 ➔ GCS 실시간 텔레메트리 명세 (`Port 9001`)

언리얼 엔진은 매 **100ms (10Hz)** 마다 드론의 물리 상태 및 시뮬레이터 뷰 상태를 GCS로 브로드캐스트합니다.

### 텔레메트리 패킷 샘플 (JSON)
```json
{
  "seq": 1042,
  "loc": [-5000.0, -4000.0, 6500.0],
  "rot": [0.0, 45.0, 0.0],
  "gimbal": [-85.0, 0.0],
  "speed": 1800.0,
  "reached": true,
  "mode": "AUTO",
  "observer_mode": "STATIONARY",
  "gimbal_mode": "YawOuter_PitchInner"
}
```

### 필드 상세 명세
| 키(Key) | 데이터 타입 | 단위 | 상세 내용 |
| :--- | :--- | :--- | :--- |
| `seq` | `int` | - | 누적 패킷 시퀀스 번호 (패킷 유실 체크용) |
| `loc` | `[float, float, float]` | cm | 드론 현재 월드 좌표 `[X, Y, Z]` |
| `rot` | `[float, float, float]` | deg | 기체 현재 자세 `[Pitch, Yaw, Roll]` |
| `gimbal` | `[float, float]` | deg | 실제 짐벌 각도 `[Pitch, Yaw]` (역투영 계산 핵심) |
| `speed` | `float` | cm/s | 현재 비행 속도 |
| `reached`| `bool` | - | 직전 목표 웨이포인트 도달 여부 |
| `mode` | `string` | - | 비행 상태 (`"AUTO"`, `"MANUAL"`, `"HOVER"`) |
| **`observer_mode`** | `string` | - | 관찰자 카메라 모드 (`"CHASE"` / `"STATIONARY"`) |
| **`gimbal_mode`** | `string` | - | 짐벌 축 순서 (`"YawOuter_PitchInner"` / `"PitchOuter_YawInner"`) |

---

## 5. 고도 500m 체감 속도 문제 분석 및 최적 파라미터 가이드

### 5.1. 왜 고도 500m에서는 기어가는 것처럼 보이는가?

카메라 화각(FOV $90^\circ$) 기준 고도별 지표면 커버리지(Footprint):
- **고도 65m (6,500cm)**: 카메라 1장이 지표면 **가로 약 130m**를 비춥니다. $18\text{m/s}$로 이동하면 매초 화면의 $14\%$가 빠르게 갱신되므로 역동적인 속도감이 느껴집니다.
- **고도 500m (50,000cm)**: 카메라 1장이 지표면 **가로 약 1,000m ($1\text{km}$)**를 비춥니다. 동일한 $18\text{m/s}$로 비행하면 매초 화면의 $1.8\%$밖에 이동하지 않아 눈으로 볼 때 거의 정지해 있거나 기어가는 것처럼 착시가 생깁니다.

### 5.2. 고도별 권장 파라미터 매트릭스

| 수색 목적 | 권장 고도 ($Z$) | 권장 비행 속도 (`speed`) | 웨이포인트 차선 간격 | 짐벌 피치 | 주요 특징 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **광역 랜드스케이프 조감** | **`500m`** ($50,000\text{cm}$) | **`60.0 ~ 80.0 m/s`** ($6000\sim 8000\text{cm/s}$) | $150\text{m} \sim 200\text{m}$ | $-85^\circ$ | 시원한 비행 속도감, 광범위 지형 파악 |
| **정밀 정사 모자이크 & 차량 탐지** | **`65m`** ($6,500\text{cm}$) | **`18.0 ~ 22.0 m/s`** ($1800\sim 2200\text{cm/s}$) | $12\text{m} \sim 15\text{m}$ | $-85^\circ$ (Nadir) | 수목 속 차량 YOLO 고해상도 포착, 격자 정렬 최적 |

---

## 6. 소스 코드 구현 레퍼런스

### 6.1. 언리얼 엔진 5 C++ 명령 처리 구현 (`DroneCommandReceiver.cpp`)

```cpp
void UDroneCommandReceiver::ProcessJsonCommand(const FString& JsonString)
{
    TSharedPtr<FJsonObject> JsonObj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);
    if (!FJsonSerializer::Deserialize(Reader, JsonObj) || !JsonObj.IsValid()) return;

    FString Cmd = JsonObj->GetStringField(TEXT("cmd")).ToUpper();
    ADronePawn* Drone = Cast<ADronePawn>(GetOwner());
    if (!Drone) return;

    // [1] TELEPORT
    if (Cmd == TEXT("TELEPORT"))
    {
        float X = JsonObj->GetNumberField(TEXT("x"));
        float Y = JsonObj->GetNumberField(TEXT("y"));
        float Z = JsonObj->GetNumberField(TEXT("z"));
        float Yaw = JsonObj->HasField(TEXT("yaw")) ? JsonObj->GetNumberField(TEXT("yaw")) : Drone->GetActorRotation().Yaw;

        Drone->bNavTargetReached = true;
        Drone->SetActorLocationAndRotation(FVector(X, Y, Z), FRotator(0.0f, Yaw, 0.0f), false, nullptr, ETeleportType::TeleportPhysics);
        UE_LOG(LogTemp, Display, TEXT("[DroneCmd] Teleport completed -> (%.1f, %.1f, %.1f)"), X, Y, Z);
    }
    // [2] FLY_TO
    else if (Cmd == TEXT("FLY_TO"))
    {
        Drone->NavTargetLocation = FVector(
            JsonObj->GetNumberField(TEXT("x")),
            JsonObj->GetNumberField(TEXT("y")),
            JsonObj->GetNumberField(TEXT("z"))
        );
        Drone->NavFlySpeed = JsonObj->HasField(TEXT("speed")) ? JsonObj->GetNumberField(TEXT("speed")) : 1800.0f;
        Drone->bNavTargetReached = false;
        UE_LOG(LogTemp, Display, TEXT("[DroneCmd] Navigating to (%.1f, %.1f, %.1f)"), 
            Drone->NavTargetLocation.X, Drone->NavTargetLocation.Y, Drone->NavTargetLocation.Z);
    }
    // [3] MANUAL_MOVE (조이스틱/키보드 수동 입력 - FLY_TO 즉시 중단)
    else if (Cmd == TEXT("MANUAL_MOVE"))
    {
        Drone->bNavTargetReached = true; // 수동 개입 시 자율비행 오버라이드 취소

        float MoveX = JsonObj->GetNumberField(TEXT("x"));
        float MoveY = JsonObj->GetNumberField(TEXT("y"));
        float MoveZ = JsonObj->GetNumberField(TEXT("z"));
        float YawRate = JsonObj->HasField(TEXT("yaw_rate")) ? JsonObj->GetNumberField(TEXT("yaw_rate")) : 0.0f;

        Drone->MoveDrone(FVector(MoveX, MoveY, MoveZ));
        if (FMath::Abs(YawRate) > 0.01f)
        {
            Drone->RotateDrone(YawRate);
        }
    }
    // [4] SET_OBSERVER_MODE (관찰자 카메라 모드 제어)
    else if (Cmd == TEXT("SET_OBSERVER_MODE"))
    {
        FString ModeStr = JsonObj->GetStringField(TEXT("mode")).ToUpper();
        if (ModeStr == TEXT("TOGGLE"))
        {
            Drone->ToggleObserverTrackingMode();
        }
        else if (ModeStr == TEXT("STATIONARY"))
        {
            Drone->SetObserverTrackingMode(EObserverPositionMode::AbsoluteStationary);
        }
        else if (ModeStr == TEXT("CHASE"))
        {
            Drone->SetObserverTrackingMode(EObserverPositionMode::RelativeChase);
        }
    }
    // [5] SET_GIMBAL
    else if (Cmd == TEXT("SET_GIMBAL"))
    {
        float Pitch = JsonObj->GetNumberField(TEXT("pitch"));
        float Yaw = JsonObj->HasField(TEXT("yaw")) ? JsonObj->GetNumberField(TEXT("yaw")) : 0.0f;
        Drone->SetGimbalOrientation(Pitch, Yaw);
    }
    // [6] TOGGLE_GIMBAL
    else if (Cmd == TEXT("TOGGLE_GIMBAL"))
    {
        Drone->ToggleGimbalMode();
    }
    // [7] CAPTURE_IMAGE
    else if (Cmd == TEXT("CAPTURE_IMAGE"))
    {
        Drone->ExecuteCapture();
    }
    // [8] HOVER
    else if (Cmd == TEXT("HOVER"))
    {
        Drone->bNavTargetReached = true;
    }
}
```

---

### 6.2. Python GCS 클라이언트 예시 (`drone_gcs_client.py`)

```python
import socket
import json
import time

UE5_IP = "127.0.0.1"
CMD_PORT = 9000
TELEM_PORT = 9001

class DroneGCSClient:
    def __init__(self):
        self.cmd_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.telem_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.telem_sock.bind(("0.0.0.0", TELEM_PORT))
        self.telem_sock.settimeout(0.5)

    def send_cmd(self, cmd_dict):
        payload = json.dumps(cmd_dict).encode('utf-8')
        self.cmd_sock.sendto(payload, (UE5_IP, CMD_PORT))

    def teleport(self, x_m, y_m, alt_m, yaw_deg=0.0):
        """지정 좌표로 순간이동 (m -> cm 자동 변환)"""
        self.send_cmd({
            "cmd": "TELEPORT",
            "x": float(x_m * 100.0),
            "y": float(y_m * 100.0),
            "z": float(alt_m * 100.0),
            "yaw": float(yaw_deg)
        })

    def fly_to(self, x_m, y_m, alt_m, speed_mps=18.0):
        """자율 순항 비행 (m -> cm 자동 변환)"""
        self.send_cmd({
            "cmd": "FLY_TO",
            "x": float(x_m * 100.0),
            "y": float(y_m * 100.0),
            "z": float(alt_m * 100.0),
            "speed": float(speed_mps * 100.0)
        })

    def manual_control(self, forward=0.0, right=0.0, up=0.0, yaw=0.0):
        """수동 벡터 조종 (-1.0 ~ 1.0)"""
        self.send_cmd({
            "cmd": "MANUAL_MOVE",
            "x": float(forward),
            "y": float(right),
            "z": float(up),
            "yaw_rate": float(yaw)
        })

    def set_observer_mode(self, mode="STATIONARY"):
        """관찰자 카메라 모드 제어 ('CHASE', 'STATIONARY', 'TOGGLE')"""
        self.send_cmd({
            "cmd": "SET_OBSERVER_MODE",
            "mode": mode.upper()
        })

    def capture_image(self):
        """촬영 및 라벨링 자동 생성 요청"""
        self.send_cmd({"cmd": "CAPTURE_IMAGE"})
```

---

### 6.3. 향후 C# WPF 전환 시 구현 예시 (참고용)

C# WPF의 `System.Net.Sockets.UdpClient`를 활용하여 동일한 패킷을 전송하는 예시입니다:

```csharp
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class DroneGCSController
{
    private UdpClient cmdClient = new UdpClient();
    private const string Ue5Ip = "127.0.0.1";
    private const int CmdPort = 9000;

    public async Task SendCommandAsync(object packet)
    {
        string json = JsonSerializer.Serialize(packet);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await cmdClient.SendAsync(bytes, bytes.Length, Ue5Ip, CmdPort);
    }

    // 수동 조이스틱 예시
    public async Task SendManualMoveAsync(float forward, float right, float up, float yawRate)
    {
        var packet = new
        {
            cmd = "MANUAL_MOVE",
            x = forward,
            y = right,
            z = up,
            yaw_rate = yawRate
        };
        await SendCommandAsync(packet);
    }

    // 관찰자 시점 전환 예시
    public async Task SetObserverModeAsync(string mode)
    {
        var packet = new { cmd = "SET_OBSERVER_MODE", mode = mode };
        await SendCommandAsync(packet);
    }
}
```
