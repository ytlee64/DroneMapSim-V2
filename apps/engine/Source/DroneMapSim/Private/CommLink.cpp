#pragma once

#include "CoreMinimal.h"
#include "NavBase.h"

/**
 * 고정익(Fixed-Wing) 순수 C++ 물리 기반 항법 엔진
 * - 최소 실속 방지 속도 제어 (일정한 뒷바람 이하 절대 없음)
 * - 웨이포인트 지원 예정
 * - Roll 값을 뱅킹 각 및 선회/러더제어에 사용
 */
class DRONEMAPSIM_API FNavFixedWing : public INavBase
{
public:
    FNavFixedWing();
    virtual ~FNavFixedWing() override = default;

    // 1. CommLink로 부터 조종 입력값 수신 (-1.0f ~ +1.0f)
    virtual void SetManualInput(float Throttle, float Roll, float Pitch, float Yaw) override;

    // 2. 매 틱마다 조종 물리 연산 (현재 위치/자세 -> 다음 위치/자세 산출)
    virtual void Step(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
        FVector& OutNextLoc, FRotator& OutNextRot) override;

    // -------------------------------------------------------------
    // 3. 자율주행(AUTO) 모드: 1km 라운드 사각형 궤적
    // -------------------------------------------------------------
    virtual void SetAutoNavEnabled(bool bEnabled) override;
    virtual bool IsAutoNavEnabled() const override { return bAutoNavEnabled; }

private:
    // 순항 속도 파라미터 (단위: cm/s)
    float MinSpeed = 1000.0f;     // 10 m/s (고정익 최소 실속 방지 속도)
    float MaxSpeed = 3000.0f;     // 30 m/s
    float CurrentSpeed = 1500.0f; // 기본 순항 속도 15 m/s

    // 수동 조종 입력 값들 (-1.0f ~ 1.0f)
    float InputThrottle = 0.0f;
    float InputRoll = 0.0f;
    float InputPitch = 0.0f;
    float InputYaw = 0.0f;

    // -------------------------------------------------------------
    // 자율주행(AUTO) 모드 상태
    // -------------------------------------------------------------
    bool bAutoNavEnabled = false;

    // 자율주행 진입 시점의 위치를 사각 궤적의 중심 기준으로 사용
    bool bAutoNavPatternInitialized = false;
    FVector AutoNavPatternCenter = FVector::ZeroVector;

    // TODO: 1km 라운드 사각형 궤적 파라미터 및 코너 상태값은 다음 단계에서 추가
};