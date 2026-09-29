# Drone Communication Protocol & AI Code Generation Specification
> **UE5 시뮬레이터 ↔ C# WPF GCS ↔ Python AI 통신 프로토콜 및 AI 자동 코드 생성 표준 규격서 (ICD Specification)**

---

## 1. 개요 (Overview)
본 문서는 언리얼 엔진 5(UE5 C++), 지상 관제 시스템(C# WPF GCS), 비전 AI/매핑 스크립트(Python) 간의 **JSON UDP 패킷 데이터 통신 인터페이스(ICD)**를 정의하고, **AI 어시스턴트에게 양방향 소스코드 생성을 의뢰하기 위한 표준 메타데이터 스키마**를 기술합니다.

---

## 2. 표준 ICD 메타데이터 스키마 (Standard ICD Schema)

인터페이스 통제 문서(ICD) 작성 및 AI 코드 생성 의뢰 시 아래의 JSON 포맷을 표준으로 사용합니다:# Drone Communication Protocol & AI Code Generation Specification
> **UE5 시뮬레이터 ↔ C# WPF GCS ↔ Python AI 통신 프로토콜 및 AI 자동 코드 생성 표준 규격서 (ICD Specification)**

---

## 1. 개요 (Overview)
본 문서는 언리얼 엔진 5(UE5 C++), 지상 관제 시스템(C# WPF GCS), 비전 AI/매핑 스크립트(Python) 간의 **JSON UDP 패킷 데이터 통신 인터페이스(ICD)**를 정의하고, **AI 어시스턴트에게 양방향 소스코드 생성을 의뢰하기 위한 표준 메타데이터 스키마**를 기술합니다.

---

## 2. 표준 ICD 메타데이터 스키마 (Standard ICD Schema)

인터페이스 통제 문서(ICD) 작성 및 AI 코드 생성 의뢰 시 아래의 JSON 포맷을 표준으로 사용합니다:

```json
{
  "No": "순번 (0부터 시작하는 정수)",
  "Name": "변수 식별자 (스네이크_표기법 권장, 예: loc_x, rot_pitch)",
  "KorName": "한글 주석 및 화면 표기 레이블 (예: 드론위치X, 기체헤딩Yaw)",
  "Type": "C언어 표준 타입 (uint32_t, int32_t, float, double, string, 정적배열 float[20] 등)",
  "Range": "유효 허용 범위 (예: -200000.0 ~ 200000.0, 0 ~ 2^32-1)",
  "LSB": "최소 분해능 또는 해상도 (예: 1, 0.1, 0.01 / 부동소수점은 1 권장)",
  "Default": "기본값 (예: 0, 0.0, -90.0, \"MANUAL\")",
  "Unit": "물리 단위 (예: cm, deg, cm/s, -)",
  "Format": "[선택사항] 표시 서식 (예: 0x%02X, 0x%04X, %.1f, %.2f, %d, %s / 미지정 시 AI 자동 규칙 적용)",
  "Desc": "필드 상세 기능 설명 및 주의사항"
}
```

---

## 3. AI 자동 코드 생성 및 기본 포맷팅 규칙 (Auto-Formatting Rules)

`Format` 필드가 비어 있거나 생략된 경우, AI 코드 제너레이터는 **`Type` 및 `Unit`의 맥락에 따라 아래 기본 규칙을 자동으로 적용**합니다.

| 데이터 타입 (`Type`) | 기본 표시 서식 (Display Format) | 코드 적용 예시 |
| :--- | :--- | :--- |
| **`float`, `double`** | **소수점 1~2자리 고정 (`F1` / `F2`)** | `LocX_Display => $"{LocX:F1} cm";`<br>`Rot_Display => $"{RotPitch:F2}°";` |
| **`uint32_t`, `int32_t`** | **일반 10진수 (`D`)** | `Seq_Display => $"{Seq:D}";` |
| **`Id` 또는 상태 비트마스크** | **16진수 표기 (`0x%02X`, `0x%04X`, `X`)** | `Id_Display => $"0x{Id:X2}";` (예: `0x11`) |
| **`string`** | **그대로 원본 문자열 (`%s`)** | `Mode_Display => Mode;` |
| **`cm` 단위 필드** | **GCS 표출용 미터(/100.0) 환산 프로퍼티 자동 생성** | `X_meter => LocX / 100.0;` |
| **`cm/s` 단위 필드** | **GCS 표출용 km/h 환산 프로퍼티 자동 생성** | `Speed_kmh => (Speed / 100.0) * 3.6;` |

---

## 4. 텔레메트리 패킷 표준 규격 (Telemetry ICD: 10Hz UDP 9001)

언리얼 드론 시뮬레이터가 GCS로 브로드캐스팅하는 실시간 비행 상태 데이터 규격입니다.

```json
[
  {
    "No": 0,
    "Name": "Id",
    "KorName": "패킷아이디",
    "Type": "uint32_t",
    "Range": "0 ~ 2^32",
    "LSB": 1,
    "Default": 17,
    "Unit": "-",
    "Format": "0x%02X",
    "Desc": "패킷 식별 ID (0x11 = 17: Telemetry Packet)"
  },
  {
    "No": 1,
    "Name": "seq",
    "KorName": "패킷순번",
    "Type": "uint32_t",
    "Range": "0 ~ 2^32-1",
    "LSB": 1,
    "Default": 0,
    "Unit": "-",
    "Format": "%d",
    "Desc": "통신 누락/손실 확인용 단조 증가 시퀀스 번호"
  },
  {
    "No": 2,
    "Name": "loc_x",
    "KorName": "드론위치X",
    "Type": "float",
    "Range": "-200000.0 ~ 200000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm",
    "Format": "%.1f",
    "Desc": "언리얼 월드 X 좌표 (동/서 방향, 100cm=1m)"
  },
  {
    "No": 3,
    "Name": "loc_y",
    "KorName": "드론위치Y",
    "Type": "float",
    "Range": "-200000.0 ~ 200000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm",
    "Format": "%.1f",
    "Desc": "언리얼 월드 Y 좌표 (남/북 방향, 100cm=1m)"
  },
  {
    "No": 4,
    "Name": "loc_z",
    "KorName": "드론고도Z",
    "Type": "float",
    "Range": "0.0 ~ 50000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm",
    "Format": "%.1f",
    "Desc": "언리얼 Z 절대고도 (ASL 기준)"
  },
  {
    "No": 5,
    "Name": "rot_pitch",
    "KorName": "기체자세Pitch",
    "Type": "float",
    "Range": "-90.0 ~ 90.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.2f",
    "Desc": "기체 기수 상하 기울기 각도"
  },
  {
    "No": 6,
    "Name": "rot_yaw",
    "KorName": "기체헤딩Yaw",
    "Type": "float",
    "Range": "-180.0 ~ 180.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.2f",
    "Desc": "기체 진행 방위각 (북쪽 0° 기준)"
  },
  {
    "No": 7,
    "Name": "rot_roll",
    "KorName": "기체자세Roll",
    "Type": "float",
    "Range": "-180.0 ~ 180.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.2f",
    "Desc": "기체 좌우 롤링 각도"
  },
  {
    "No": 8,
    "Name": "gimbal_pitch",
    "KorName": "짐벌각도Pitch",
    "Type": "float",
    "Range": "-90.0 ~ 20.0",
    "LSB": 1,
    "Default": -90.0,
    "Unit": "deg",
    "Format": "%.1f",
    "Desc": "카메라 상하 각도 (-90°: 수직직하 Nadir, -60°: 사선 Oblique)"
  },
  {
    "No": 9,
    "Name": "gimbal_yaw",
    "KorName": "짐벌각도Yaw",
    "Type": "float",
    "Range": "-180.0 ~ 180.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.1f",
    "Desc": "카메라 좌우 팬 각도 (기체 기준 상대각)"
  },
  {
    "No": 10,
    "Name": "speed",
    "KorName": "대지속도",
    "Type": "float",
    "Range": "0.0 ~ 5000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm/s",
    "Format": "%.1f",
    "Desc": "드론 비행 속도 (1800cm/s = 18m/s = 64.8km/h)"
  },
  {
    "No": 11,
    "Name": "reached",
    "KorName": "도달여부",
    "Type": "uint32_t",
    "Range": "0 / 1",
    "LSB": 1,
    "Default": 1,
    "Unit": "-",
    "Format": "%d",
    "Desc": "자율비행 목표점 도달 완료 플래그 (0: 비행중, 1: 도달)"
  },
  {
    "No": 12,
    "Name": "mode",
    "KorName": "비행모드",
    "Type": "string",
    "Range": "-",
    "LSB": "-",
    "Default": "MANUAL",
    "Unit": "-",
    "Format": "%s",
    "Desc": "드론 제어 모드 (AUTO / HOVER / MANUAL / ORBIT)"
  },
  {
    "No": 13,
    "Name": "observer_mode",
    "KorName": "관찰자모드",
    "Type": "string",
    "Range": "-",
    "LSB": "-",
    "Default": "CHASE",
    "Unit": "-",
    "Format": "%s",
    "Desc": "시점 모드 (CHASE / STATIONARY / TOP_DOWN / FREE_ROAM)"
  },
  {
    "No": 14,
    "Name": "gimbal_mode",
    "KorName": "짐벌결합모드",
    "Type": "string",
    "Range": "-",
    "LSB": "-",
    "Default": "YawOuter",
    "Unit": "-",
    "Format": "%s",
    "Desc": "짐벌 물리 축 결합 우선순위 모드 (YawOuter / PitchOuter)"
  }
]
```

---

## 5. 자동 생성 타겟 소스코드 예시

### A. C# WPF GCS 수신 모델 (`DroneTelemetryPacket.cs`)
```csharp
using System;
using System.Text.Json.Serialization;

namespace DroneMapGCS
{
    public class DroneTelemetryPacket
    {
        [JsonPropertyName("Id")]
        public uint Id { get; set; } = 0x11;

        [JsonPropertyName("seq")]
        public uint Seq { get; set; }

        [JsonPropertyName("loc_x")]
        public float LocX { get; set; }

        [JsonPropertyName("loc_y")]
        public float LocY { get; set; }

        [JsonPropertyName("loc_z")]
        public float LocZ { get; set; }

        [JsonPropertyName("rot_pitch")]
        public float RotPitch { get; set; }

        [JsonPropertyName("rot_yaw")]
        public float RotYaw { get; set; }

        [JsonPropertyName("rot_roll")]
        public float RotRoll { get; set; }

        [JsonPropertyName("gimbal_pitch")]
        public float GimbalPitch { get; set; } = -90.0f;

        [JsonPropertyName("gimbal_yaw")]
        public float GimbalYaw { get; set; }

        [JsonPropertyName("speed")]
        public float Speed { get; set; }

        [JsonPropertyName("reached")]
        public uint Reached { get; set; } = 1;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "MANUAL";

        [JsonPropertyName("observer_mode")]
        public string ObserverMode { get; set; } = "CHASE";

        [JsonPropertyName("gimbal_mode")]
        public string GimbalMode { get; set; } = "YawOuter";

        // ==========================================
        // GCS 관제 화면 표출용 단위 변환 & 포맷팅 프로퍼티
        // ==========================================
        [JsonIgnore]
        public string Id_Display => $"0x{Id:X2}";

        [JsonIgnore]
        public double X_meter => LocX / 100.0;

        [JsonIgnore]
        public double Y_meter => LocY / 100.0;

        [JsonIgnore]
        public double Alt_meter => LocZ / 100.0;

        [JsonIgnore]
        public string LocDisplay => $"X:{X_meter:F1}m Y:{Y_meter:F1}m Z:{Alt_meter:F1}m";

        [JsonIgnore]
        public double Speed_Kmh => (Speed / 100.0) * 3.6;

        [JsonIgnore]
        public string SpeedDisplay => $"{Speed_Kmh:F1} km/h";

        [JsonIgnore]
        public string AttitudeDisplay => $"P:{RotPitch:F1}° Y:{RotYaw:F1}° R:{RotRoll:F1}°";

        [JsonIgnore]
        public bool IsReached => Reached != 0;
    }
}
```

### B. Unreal Engine 5.7 C++ 헤더 (`DroneTelemetryTypes.h`)
```cpp
#pragma once

#include "CoreMinimal.h"
#include "DroneTelemetryTypes.generated.h"

USTRUCT(BlueprintType)
struct FDroneTelemetryPacket
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    uint32 Id = 0x11;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    uint32 seq = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float loc_x = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float loc_y = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float loc_z = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float rot_pitch = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float rot_yaw = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float rot_roll = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float gimbal_pitch = -90.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float gimbal_yaw = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float speed = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    uint32 reached = 1;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    FString mode = TEXT("MANUAL");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    FString observer_mode = TEXT("CHASE");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    FString gimbal_mode = TEXT("YawOuter");

    // FVector / FRotator 편의용 헬퍼 함수
    FVector GetWorldLocation() const { return FVector(loc_x, loc_y, loc_z); }
    FRotator GetDroneRotation() const { return FRotator(rot_pitch, rot_yaw, rot_roll); }
};
```

---

## 6. AI 의뢰 시 사용 프롬프트 템플릿
```text
[ICD 정의서]
<정의한 JSON 배열 붙여넣기>

[요청사항]
위 ICD 정의서를 바탕으로 다음 소스코드를 생성해줘:
1. Unreal Engine 5 C++ USTRUCT 구조체 및 FJsonObject 직렬화 함수
2. C# WPF 수신용 클래스 (JsonPropertyName, 미터/kmh 환산 프로퍼티, UI 바인딩용 문자열 포맷팅 포함)
3. float는 소수점 1자리(F1), Id는 0x11 형태의 Hex로 자동 포맷팅 적용
```


```json
{
  "No": "순번 (0부터 시작하는 정수)",
  "Name": "변수 식별자 (스네이크_표기법 권장, 예: loc_x, rot_pitch)",
  "KorName": "한글 주석 및 화면 표기 레이블 (예: 드론위치X, 기체헤딩Yaw)",
  "Type": "C언어 표준 타입 (uint32_t, int32_t, float, double, string, 정적배열 float[20] 등)",
  "Range": "유효 허용 범위 (예: -200000.0 ~ 200000.0, 0 ~ 2^32-1)",
  "LSB": "최소 분해능 또는 해상도 (예: 1, 0.1, 0.01 / 부동소수점은 1 권장)",
  "Default": "기본값 (예: 0, 0.0, -90.0, \"MANUAL\")",
  "Unit": "물리 단위 (예: cm, deg, cm/s, -)",
  "Format": "[선택사항] 표시 서식 (예: 0x%02X, 0x%04X, %.1f, %.2f, %d, %s / 미지정 시 AI 자동 규칙 적용)",
  "Desc": "필드 상세 기능 설명 및 주의사항"
}
```

---

## 3. AI 자동 코드 생성 및 기본 포맷팅 규칙 (Auto-Formatting Rules)

`Format` 필드가 비어 있거나 생략된 경우, AI 코드 제너레이터는 **`Type` 및 `Unit`의 맥락에 따라 아래 기본 규칙을 자동으로 적용**합니다.

| 데이터 타입 (`Type`) | 기본 표시 서식 (Display Format) | 코드 적용 예시 |
| :--- | :--- | :--- |
| **`float`, `double`** | **소수점 1~2자리 고정 (`F1` / `F2`)** | `LocX_Display => $"{LocX:F1} cm";`<br>`Rot_Display => $"{RotPitch:F2}°";` |
| **`uint32_t`, `int32_t`** | **일반 10진수 (`D`)** | `Seq_Display => $"{Seq:D}";` |
| **`Id` 또는 상태 비트마스크** | **16진수 표기 (`0x%02X`, `0x%04X`, `X`)** | `Id_Display => $"0x{Id:X2}";` (예: `0x11`) |
| **`string`** | **그대로 원본 문자열 (`%s`)** | `Mode_Display => Mode;` |
| **`cm` 단위 필드** | **GCS 표출용 미터(/100.0) 환산 프로퍼티 자동 생성** | `X_meter => LocX / 100.0;` |
| **`cm/s` 단위 필드** | **GCS 표출용 km/h 환산 프로퍼티 자동 생성** | `Speed_kmh => (Speed / 100.0) * 3.6;` |

---

## 4. 텔레메트리 패킷 표준 규격 (Telemetry ICD: 10Hz UDP 9001)

언리얼 드론 시뮬레이터가 GCS로 브로드캐스팅하는 실시간 비행 상태 데이터 규격입니다.

```json
[
  {
    "No": 0,
    "Name": "Id",
    "KorName": "패킷아이디",
    "Type": "uint32_t",
    "Range": "0 ~ 2^32",
    "LSB": 1,
    "Default": 17,
    "Unit": "-",
    "Format": "0x%02X",
    "Desc": "패킷 식별 ID (0x11 = 17: Telemetry Packet)"
  },
  {
    "No": 1,
    "Name": "seq",
    "KorName": "패킷순번",
    "Type": "uint32_t",
    "Range": "0 ~ 2^32-1",
    "LSB": 1,
    "Default": 0,
    "Unit": "-",
    "Format": "%d",
    "Desc": "통신 누락/손실 확인용 단조 증가 시퀀스 번호"
  },
  {
    "No": 2,
    "Name": "loc_x",
    "KorName": "드론위치X",
    "Type": "float",
    "Range": "-200000.0 ~ 200000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm",
    "Format": "%.1f",
    "Desc": "언리얼 월드 X 좌표 (동/서 방향, 100cm=1m)"
  },
  {
    "No": 3,
    "Name": "loc_y",
    "KorName": "드론위치Y",
    "Type": "float",
    "Range": "-200000.0 ~ 200000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm",
    "Format": "%.1f",
    "Desc": "언리얼 월드 Y 좌표 (남/북 방향, 100cm=1m)"
  },
  {
    "No": 4,
    "Name": "loc_z",
    "KorName": "드론고도Z",
    "Type": "float",
    "Range": "0.0 ~ 50000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm",
    "Format": "%.1f",
    "Desc": "언리얼 Z 절대고도 (ASL 기준)"
  },
  {
    "No": 5,
    "Name": "rot_pitch",
    "KorName": "기체자세Pitch",
    "Type": "float",
    "Range": "-90.0 ~ 90.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.2f",
    "Desc": "기체 기수 상하 기울기 각도"
  },
  {
    "No": 6,
    "Name": "rot_yaw",
    "KorName": "기체헤딩Yaw",
    "Type": "float",
    "Range": "-180.0 ~ 180.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.2f",
    "Desc": "기체 진행 방위각 (북쪽 0° 기준)"
  },
  {
    "No": 7,
    "Name": "rot_roll",
    "KorName": "기체자세Roll",
    "Type": "float",
    "Range": "-180.0 ~ 180.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.2f",
    "Desc": "기체 좌우 롤링 각도"
  },
  {
    "No": 8,
    "Name": "gimbal_pitch",
    "KorName": "짐벌각도Pitch",
    "Type": "float",
    "Range": "-90.0 ~ 20.0",
    "LSB": 1,
    "Default": -90.0,
    "Unit": "deg",
    "Format": "%.1f",
    "Desc": "카메라 상하 각도 (-90°: 수직직하 Nadir, -60°: 사선 Oblique)"
  },
  {
    "No": 9,
    "Name": "gimbal_yaw",
    "KorName": "짐벌각도Yaw",
    "Type": "float",
    "Range": "-180.0 ~ 180.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "deg",
    "Format": "%.1f",
    "Desc": "카메라 좌우 팬 각도 (기체 기준 상대각)"
  },
  {
    "No": 10,
    "Name": "speed",
    "KorName": "대지속도",
    "Type": "float",
    "Range": "0.0 ~ 5000.0",
    "LSB": 1,
    "Default": 0.0,
    "Unit": "cm/s",
    "Format": "%.1f",
    "Desc": "드론 비행 속도 (1800cm/s = 18m/s = 64.8km/h)"
  },
  {
    "No": 11,
    "Name": "reached",
    "KorName": "도달여부",
    "Type": "uint32_t",
    "Range": "0 / 1",
    "LSB": 1,
    "Default": 1,
    "Unit": "-",
    "Format": "%d",
    "Desc": "자율비행 목표점 도달 완료 플래그 (0: 비행중, 1: 도달)"
  },
  {
    "No": 12,
    "Name": "mode",
    "KorName": "비행모드",
    "Type": "string",
    "Range": "-",
    "LSB": "-",
    "Default": "MANUAL",
    "Unit": "-",
    "Format": "%s",
    "Desc": "드론 제어 모드 (AUTO / HOVER / MANUAL / ORBIT)"
  },
  {
    "No": 13,
    "Name": "observer_mode",
    "KorName": "관찰자모드",
    "Type": "string",
    "Range": "-",
    "LSB": "-",
    "Default": "CHASE",
    "Unit": "-",
    "Format": "%s",
    "Desc": "시점 모드 (CHASE / STATIONARY / TOP_DOWN / FREE_ROAM)"
  },
  {
    "No": 14,
    "Name": "gimbal_mode",
    "KorName": "짐벌결합모드",
    "Type": "string",
    "Range": "-",
    "LSB": "-",
    "Default": "YawOuter",
    "Unit": "-",
    "Format": "%s",
    "Desc": "짐벌 물리 축 결합 우선순위 모드 (YawOuter / PitchOuter)"
  }
]
```

---

## 5. 자동 생성 타겟 소스코드 예시

### A. C# WPF GCS 수신 모델 (`DroneTelemetryPacket.cs`)
```csharp
using System;
using System.Text.Json.Serialization;

namespace DroneMapGCS
{
    public class DroneTelemetryPacket
    {
        [JsonPropertyName("Id")]
        public uint Id { get; set; } = 0x11;

        [JsonPropertyName("seq")]
        public uint Seq { get; set; }

        [JsonPropertyName("loc_x")]
        public float LocX { get; set; }

        [JsonPropertyName("loc_y")]
        public float LocY { get; set; }

        [JsonPropertyName("loc_z")]
        public float LocZ { get; set; }

        [JsonPropertyName("rot_pitch")]
        public float RotPitch { get; set; }

        [JsonPropertyName("rot_yaw")]
        public float RotYaw { get; set; }

        [JsonPropertyName("rot_roll")]
        public float RotRoll { get; set; }

        [JsonPropertyName("gimbal_pitch")]
        public float GimbalPitch { get; set; } = -90.0f;

        [JsonPropertyName("gimbal_yaw")]
        public float GimbalYaw { get; set; }

        [JsonPropertyName("speed")]
        public float Speed { get; set; }

        [JsonPropertyName("reached")]
        public uint Reached { get; set; } = 1;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "MANUAL";

        [JsonPropertyName("observer_mode")]
        public string ObserverMode { get; set; } = "CHASE";

        [JsonPropertyName("gimbal_mode")]
        public string GimbalMode { get; set; } = "YawOuter";

        // ==========================================
        // GCS 관제 화면 표출용 단위 변환 & 포맷팅 프로퍼티
        // ==========================================
        [JsonIgnore]
        public string Id_Display => $"0x{Id:X2}";

        [JsonIgnore]
        public double X_meter => LocX / 100.0;

        [JsonIgnore]
        public double Y_meter => LocY / 100.0;

        [JsonIgnore]
        public double Alt_meter => LocZ / 100.0;

        [JsonIgnore]
        public string LocDisplay => $"X:{X_meter:F1}m Y:{Y_meter:F1}m Z:{Alt_meter:F1}m";

        [JsonIgnore]
        public double Speed_Kmh => (Speed / 100.0) * 3.6;

        [JsonIgnore]
        public string SpeedDisplay => $"{Speed_Kmh:F1} km/h";

        [JsonIgnore]
        public string AttitudeDisplay => $"P:{RotPitch:F1}° Y:{RotYaw:F1}° R:{RotRoll:F1}°";

        [JsonIgnore]
        public bool IsReached => Reached != 0;
    }
}
```

### B. Unreal Engine 5.7 C++ 헤더 (`DroneTelemetryTypes.h`)
```cpp
#pragma once

#include "CoreMinimal.h"
#include "DroneTelemetryTypes.generated.h"

USTRUCT(BlueprintType)
struct FDroneTelemetryPacket
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    uint32 Id = 0x11;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    uint32 seq = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float loc_x = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float loc_y = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float loc_z = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float rot_pitch = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float rot_yaw = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float rot_roll = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float gimbal_pitch = -90.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float gimbal_yaw = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    float speed = 0.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    uint32 reached = 1;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    FString mode = TEXT("MANUAL");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    FString observer_mode = TEXT("CHASE");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Telemetry")
    FString gimbal_mode = TEXT("YawOuter");

    // FVector / FRotator 편의용 헬퍼 함수
    FVector GetWorldLocation() const { return FVector(loc_x, loc_y, loc_z); }
    FRotator GetDroneRotation() const { return FRotator(rot_pitch, rot_yaw, rot_roll); }
};
```

---

## 6. AI 의뢰 시 사용 프롬프트 템플릿
```text
[ICD 정의서]
<정의한 JSON 배열 붙여넣기>

[요청사항]
위 ICD 정의서를 바탕으로 다음 소스코드를 생성해줘:
1. Unreal Engine 5 C++ USTRUCT 구조체 및 FJsonObject 직렬화 함수
2. C# WPF 수신용 클래스 (JsonPropertyName, 미터/kmh 환산 프로퍼티, UI 바인딩용 문자열 포맷팅 포함)
3. float는 소수점 1자리(F1), Id는 0x11 형태의 Hex로 자동 포맷팅 적용
```
