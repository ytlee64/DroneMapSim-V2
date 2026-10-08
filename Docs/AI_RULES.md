# AI_RULES.md (프로젝트 규칙 및 개발자 프로필)

## 1. 개발자 프로필 및 지식 베이스 (Developer Profile)
- **숙련 보유 기술**: Embedded C, C++, C# (.NET / WPF MVVM), 비동기 통신 및 시스템 타이밍/동기화 설계
- **확장 중인 기술**: Unreal Engine 5.7 고유 아키텍처(UHT 리플렉션, 라이프사이클, 에디터 파이썬 API), Python YOLOv8 학습 파이프라인
- **답변 가이드라인**:
  1. C/C++/C# 기초 문법 설명은 생략하고, **근본 원인(Root Cause)과 아키텍처/타이밍 관점**에서 직관적으로 설명할 것.
  2. 언리얼 엔진의 특수한 동작(직렬화, CDO, 가비지 컬렉션, `UPROPERTY` 등)은 C++/임베디드 관점(메모리/생명주기)과 비교하여 명확히 짚어줄 것.
  3. 내가 명시적으로 지시하기 전에는 임의로 프리뷰나 전체 빌드를 실행하지 말 것.
  4. 문서 및 답변에 그림 문자를 넣지 말 것. 

---

## 2. 프로젝트 기술 스택 요약 (Tech Stack)
- **Simulator**: Unreal Engine 5.7 (C++ & Editor Python Scripting)
- **GCS (Ground Control Station)**: C# .NET 8 WPF (`CommunityToolkit.Mvvm`)
- **AI / Vision**: Python 3.10+, PyTorch (CUDA), Ultralytics YOLOv8 (`best.onnx`)
- **Communication**: UDP JSON (Port `9000`: GCS ➔ UE5 Command / Port `9001`: UE5 ➔ GCS 10Hz Telemetry)

---

## 3. 절대 준수 규칙 (Strict Rules & DO NOT)
- ❌ **폐기된 네이밍 사용 금지**:
  - `EnvElements`, `EnvGen` 사용 금지 ➔ 반드시 **`TargetElements`**, **`TargetGen`** 사용
  - `ImageWatcherService` 사용 금지 ➔ 반드시 **`ImageService`** 사용
- ❌ **파일 감시(`FileSystemWatcher`) 사용 금지**:
  - 캡처 이미지 로드는 반드시 텔레메트리 수신 이벤트(`ImageService.SyncCaptureFromTelemetry(packet.LastCapture)`)에 동기화할 것.
- ❌ **초 단위 캡처 파일명 금지**:
  - 1초 내 다중 촬영 덮어쓰기 방지를 위해 반드시 밀리초 포함 (`IMG_YYYYMMDD_HHMMSS_fff`).
- ❌ **WPF 파일명 `View` 접미사 금지**:
  - `TacticalMap.xaml`, `FlightControlPanel.xaml` 처럼 간결한 컴포넌트명 유지 (뷰모델은 `TacticalMapVM.cs`).
- ⭕ **전술 지도(2D) 좌표 투영 공식**:
  - 언리얼 월드 좌표: `X = 북쪽(화면 -Y)`, `Y = 동쪽(화면 +X)`
  - `GroundTruth_Targets.json`의 `location`은 `cm` 단위이므로 반드시 `/ 100.0`하여 `m` 단위로 변환 후 `ToScreenPoint()`에 전달할 것.