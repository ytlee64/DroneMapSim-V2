#pragma once

#include "CoreMinimal.h"
#include "NavBase.h"

/**
 * 고정익(Fixed-Wing) 순수 C++ 수동 비행 항법 엔진
 * - 최소 실속 방지 속도 유지 (엔진이 꺼지지 않고 계속 전진)
 * - 스로틀 가감속
 * - Roll 날개 뱅킹 턴 및 러더/엘리베이터 연동
 */
class DRONEMAPSIM_API FNavFixedWing : public INavBase
{
public:
    FNavFixedWing();
    virtual ~FNavFixedWing() override = default;

    // 1. CommLink를 통한 수동 조종값 설정 (-1.0f ~ +1.0f)
    virtual void SetManualInput(float Throttle, float Roll, float Pitch, float Yaw) override;

    // 2. 매 틱마다 비행 물리 연산 (현재 위치/자세 -> 다음 위치/자세 계산)
    virtual void Step(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
        FVector& OutNextLoc, FRotator& OutNextRot) override;
	virtual float GetCurrentSpeed() { return CurrentSpeed; }

private:
    // 비행 속도 파라미터 (단위: cm/s)
    float MinSpeed = 1000.0f;     // 10 m/s (고정익 최소 실속 방지 속도)
    float MaxSpeed = 3000.0f;     // 30 m/s
    float CurrentSpeed = 1500.0f; // 기본 순항 속도 15 m/s

    // 수동 조종 입력 버퍼 (-1.0f ~ +1.0f)
    float InputThrottle = 0.0f;
    float InputRoll = 0.0f;
    float InputPitch = 0.0f;
    float InputYaw = 0.0f;

    // -------------------------------------------------------------
    // 자율주행 진입 시점의 위치를 사각 궤적의 중심 기준으로 사용
    // -------------------------------------------------------------
    bool bAutoNavPatternInitialized = false;
    FVector AutoNavPatternCenter = FVector::ZeroVector;


    void UpdateAutonomousGuidance(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
        float& OutRoll, float& OutPitch, float& OutThrottle);
    
};