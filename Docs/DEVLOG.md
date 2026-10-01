# DroneMapSim Development Log (DEVLOG)
> 이 문서는 드론 시뮬레이터 및 전술 관제 시스템(GCS)의 개발 일지, 핵심 아키텍처 의사결정(ADR), 트러블슈팅 내역을 일자별로 기록하는 엔지니어링 블랙박스입니다.

---

## 📅 [2026-10-01] GCS 관제소 UI/MVVM 전면 리팩토링 & AI 파이프라인 정립

### 1. 📌 요약 (Executive Summary)
- `apps/AI/Yolo/` 디렉터리를 `apps/AI/`로 Git 히스토리 보존 이동 및 `.gitignore` 캐시 정리
- WPF GCS 관제소 밀리터리 택티컬 다크 테마(`Style.xaml`) 구축 및 GroupBox 텍스트 잘림 버그 완벽 수정
- 거대 뷰모델(God ViewModel) 해체: **`MainWindowVM`** + **`TacticalMapVM`** 분리 및 2대 UserControl화
- AI 정찰 화면의 정답(Ground Truth) vs 추론(Detection) 겹침 가림 문제 해결을 위한 **IoU 3색 Confusion Matrix 시각화** 기법 설계
- 최신 시스템 종합 아키텍처 문서(`DRONE_MAP_SIM_ARCHITECTURE.md`, v1.4.0) 작성

---

### 2. 🛠️ 작업 상세 내역 (Work Details)

#### A. 형상 관리 및 리포지토리 정리 (Git)
- **AI 디렉터리 평탄화**:
  - `apps/AI/Yolo/*` 파일들을 상위 `apps/AI/*`로 이동하여 프로젝트 경로 depth 축소.
- **PowerShell 와일드카드 버그 해결**:
  - PowerShell에서 `git mv apps/AI/Yolo/* apps/AI/*` 실행 시 발생하던 `fatal: bad source` 에러를 아래 파이프라인으로 해결:
    ```powershell
    Get-ChildItem "apps\AI\Yolo" | ForEach-Object { git mv $_.FullName "apps\AI" }
    ```
  - Git 추적성(Similarity 100%, Commit History) 완벽 보존.
- **`.gitignore` 캐시 강제 새로고침**:
  - 이미 인덱싱되어 `.gitignore` 수정이 반영되지 않던 문제를 Git 캐시 초기화로 해결:
    ```powershell
    git rm -r --cached .
    git add .
    git commit -m "chore: Apply updated .gitignore rules"
    ```

---

#### B. GCS 관제소 UI/UX & 디자인 시스템 (`Style.xaml`)
- **밀리터리 딥 슬레이트 테마 도입**:
  - 기존 칙칙한 회색(`#5E5E5E`)을 최신 항공 관제소 스타일의 딥 슬레이트 네이비(`#0B0F19`, `#131B2E`)로 교체.
  - 전술 액센트 시안(`#0284C7`), 앰버(`#F59E0B`), 에메랄드 그린(`#10B981`), 레드(`#E11D48`) 색상 체계 구축.
- **치명적 WPF 스타일 버그 제거**:
  - 전역 `<Style TargetType="Grid">`에 배경색이 강제 지정되어 모든 내부 패널의 투명도가 깨지던 안티패턴 제거.
- **GroupBox 헤더 텍스트 하단 잘림 해결**:
  - `ContentPresenter`가 헤더를 덮어쓰던 문제를 `ControlTemplate` 내 `Panel.ZIndex="10"` 부여 및 Top 여백(`Margin="0,10,0,0"`) 보정으로 해결.
- **전술 비행/짐벌 조종 패널 신설 (`FlightControlPanel.xaml`)**:
  - 4버튼 비행 모드 바: `MANUAL`, `AUTO NAV`, `LOITER 40M`, `RTH`.
  - 3x3 WASD 비행 자세 D-패드: 비행 롤(Roll) 각도와 실시간 연동되는 중심 `0°` 표시.
  - 3x3 IJKL 2축 짐벌 D-패드: 중앙 원클릭 `NADIR` 복귀 및 실시간 Pitch/Yaw 수치 표시 (폭 54로 늘려 `NAI` 잘림 수정).
  - 하단 액션 버튼: 긴급 `CAPTURE [C]` (레드 `DangerButton`) & `VIEW MODE [V]` (시안).

---

#### C. WPF MVVM 아키텍처 모듈화 (단일 책임 원칙)
- **뷰모델 2분할 (Composition)**:
  - **`MainWindowVM.cs`**: UDP 소켓 송수신(Port 9000/9001), 텔레메트리 파싱, FPV 영상 감시, 비행 모드 상태 머신 전담.
  - **`TacticalMapVM.cs`**: 2.3km 실사용 영역 캔버스 스케일링, 월드 미터 궤적 버퍼(3,000pt), 기수선(22px) 및 마커(14px) 수학 계산 전담.
- **`UserControl` 컴포넌트화 (`View` 접미사 제거)**:
  - 파일 및 클래스 이름을 직관적으로 단순화:
    - `FlightControlPanel.xaml` $\leftrightarrow$ `FlightControlPanel.xaml.cs`
    - `TacticalMap.xaml` $\leftrightarrow$ `TacticalMap.xaml.cs` (캔버스 `SizeChanged` 이벤트 자체 캡슐화)
  - `MainWindow.xaml`: 300줄이 넘던 XAML이 단 60줄의 마스터 레이아웃으로 정리됨.

---

#### D. AI 비전 파이프라인 (바운딩 박스 오버레이)
- **문제점**:
  - 정답 라벨(Ground Truth, 초록색) 위에 AI 추론 박스(Detection, 빨간색)가 그대로 겹쳐 그려지며 정답이 가려지는 문제 발생.
- **의사결정**:
  - 화면 2개 분할 방식(공간 낭비, 시인성 저하)을 배제하고, 단일 FPV 뷰에서 **IoU Confusion Matrix 3색 판정** 도입:
    - 🟢 **Cyan(청록색) [True Positive]**: AI 추론과 정답의 $\text{IoU} \ge 0.4$ 일치 시 표시 (정상 탐지).
    - 🔴 **Red(빨간색) [False Positive]**: 오탐 (표적이 없는데 잘못 탐지).
    - 🟡 **Yellow 점선(노란색) [False Negative]**: 미탐 (실제 표적인데 AI가 놓침).

---

### 3. 💡 핵심 아키텍처 결정 기록 (ADR)
1. **WPF 네이밍 컨벤션**:
   - 화면 파일명에 중복되는 `View` 접미사를 제거(`TacticalMap`, `FlightControlPanel`)하고, 뷰모델은 `VM` 접미사(`TacticalMapVM`, `MainWindowVM`)로 1:1 대응하여 가독성을 극대화함.
2. **문서 관리 투-트랙(Two-Track) 정책**:
   - `DRONE_MAP_SIM_ARCHITECTURE.md`: 항상 최신 동작 상태만 유지하는 종합 설계도.
   - `DEVLOG.md`: 시도한 내역과 트러블슈팅을 누적하는 히스토리 블랙박스.

---

### 4. 🚀 다음 개발 과제 (Next Steps)
- [ ] 다중 웨이포인트 기반 자동 비행 오케스트레이터 (`UMissionManager`) 연동
- [ ] 전술 지도(`TacticalMap`) 위 마우스 클릭을 통한 실시간 웨이포인트 추가 및 전송 기능
- [ ] 캡처된 항공 정찰 이미지의 실시간 정사 모자이크(Orthomosaic) 스티칭 프로토타입
