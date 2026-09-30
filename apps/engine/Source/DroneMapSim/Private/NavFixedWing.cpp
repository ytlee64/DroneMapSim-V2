#include "NavFixedWing.h"

FNavFixedWing::FNavFixedWing()
{
    CurrentSpeed = 1500.0f; // 15 m/s
    InputThrottle = 0.0f;
    InputRoll = 0.0f;
    InputPitch = 0.0f;
    InputYaw = 0.0f;
}

void FNavFixedWing::SetManualInput(float Throttle, float Roll, float Pitch, float Yaw)
{
    // 조종 입력이 들어오면 타겟 조타값 갱신
    InputThrottle = Throttle;
    InputRoll = Roll;
    InputPitch = Pitch;
    InputYaw = Yaw;
}

void FNavFixedWing::Step(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
    FVector& OutNextLoc, FRotator& OutNextRot)
{
    // -------------------------------------------------------------
    // 0. 자율 비행 중일 때: 웨이포인트 목표 조타값 계산
    // -------------------------------------------------------------
    if (bAutoNav && Waypoints.Num() > 0)
    {
        float TargetAutoRoll = 0.0f;
        float TargetAutoPitch = 0.0f;
        float TargetAutoThrottle = 0.0f;

        // 목표 웨이포인트를 향하기 위한 목표 조타량 산출
        UpdateAutonomousGuidance(CurrentLoc, CurrentRot, DeltaTime,
            TargetAutoRoll, TargetAutoPitch, TargetAutoThrottle);

        // ⭐️ 핵심: 강제 대입(=)이 아니라, 현재 Input값을 자율 목표치로 서서히 끌어당김!
        // (조종사가 스틱을 쳐서 InputRoll이 튀어도, 스틱을 놓으면 스스로 자율 목표값으로 복귀)
        InputRoll = FMath::FInterpTo(InputRoll, TargetAutoRoll, DeltaTime, 2.0f);
        InputPitch = FMath::FInterpTo(InputPitch, TargetAutoPitch, DeltaTime, 2.0f);
        InputThrottle = FMath::FInterpTo(InputThrottle, TargetAutoThrottle, DeltaTime, 1.5f);
        InputYaw = FMath::FInterpTo(InputYaw, 0.0f, DeltaTime, 2.0f);
    }
    else
    {
        // 수동 모드일 때는 중립(0)으로 자동 감쇠
        InputRoll = FMath::FInterpTo(InputRoll, 0.0f, DeltaTime, 2.0f);
        InputPitch = FMath::FInterpTo(InputPitch, 0.0f, DeltaTime, 2.0f);
        InputYaw = FMath::FInterpTo(InputYaw, 0.0f, DeltaTime, 2.0f);
        InputThrottle = FMath::FInterpTo(InputThrottle, 0.0f, DeltaTime, 2.0f);
    }

    // -------------------------------------------------------------
    // 1. 조타면 물리 반응 (기존 작성하신 코드 그대로 유지)
    // -------------------------------------------------------------
    static float FilteredRoll = 0.0f;
    static float FilteredPitch = 0.0f;
    static float FilteredYaw = 0.0f;

    FilteredRoll = FMath::FInterpTo(FilteredRoll, InputRoll, DeltaTime, 3.0f);
    FilteredPitch = FMath::FInterpTo(FilteredPitch, InputPitch, DeltaTime, 3.0f);
    FilteredYaw = FMath::FInterpTo(FilteredYaw, InputYaw, DeltaTime, 3.0f);

    // -------------------------------------------------------------
    // 2. 속도 및 가감속 (기존 코드 그대로)
    // -------------------------------------------------------------
    CurrentSpeed = FMath::Clamp(CurrentSpeed + (InputThrottle * 600.0f * DeltaTime), MinSpeed, MaxSpeed);

    // -------------------------------------------------------------
    // 3. 기체 회전각 (기존 코드 그대로)
    // -------------------------------------------------------------
    FRotator TargetRot = CurrentRot;

    float TargetBankRoll = FilteredRoll * 40.0f;
    TargetRot.Roll = FMath::FInterpTo(CurrentRot.Roll, TargetBankRoll, DeltaTime, 2.5f);

    float TargetPitch = FilteredPitch * 30.0f;
    TargetRot.Pitch = FMath::FInterpTo(CurrentRot.Pitch, TargetPitch, DeltaTime, 3.0f);

    float CoordinatedTurnRate = (TargetRot.Roll / 40.0f) * 45.0f;
    float RudderRate = FilteredYaw * 35.0f;
    TargetRot.Yaw += (CoordinatedTurnRate + RudderRate) * DeltaTime;

    OutNextRot = TargetRot;

    // -------------------------------------------------------------
    // 4. 위치 전진 (기존 코드 그대로)
    // -------------------------------------------------------------
    FVector ForwardDir = OutNextRot.Vector();
    OutNextLoc = CurrentLoc + (ForwardDir * CurrentSpeed * DeltaTime);
}


void FNavFixedWing::UpdateAutonomousGuidance(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
    float& OutRoll, float& OutPitch, float& OutThrottle)
{
    if (!Waypoints.IsValidIndex(CurrentWaypointIndex))
    {
        CurrentWaypointIndex = 0;
    }

    const FWaypointItemData& TargetWp = Waypoints[CurrentWaypointIndex];

    FVector ToTarget = TargetWp.Location - CurrentLoc;
    float Dist2D = FVector::Dist2D(CurrentLoc, TargetWp.Location);
    float AltDiff = TargetWp.Location.Z - CurrentLoc.Z;

    // 30m 반경 안에 들어오면 다음 웨이포인트로 무한 순환 (% 연산자)
    const float AcceptanceRadius_cm = 3000.0f;
    if (Dist2D <= AcceptanceRadius_cm)
    {
        CurrentWaypointIndex = (CurrentWaypointIndex + 1) % Waypoints.Num();
        return;
    }

    // 헤딩 오차 기반 Roll 계산
    FRotator DesiredRot = ToTarget.Rotation();
    float YawError = FMath::FindDeltaAngleDegrees(CurrentRot.Yaw, DesiredRot.Yaw);
    OutRoll = FMath::Clamp(YawError / 35.0f, -1.0f, 1.0f);

    // 고도 오차 기반 Pitch 계산 (50m 유지)
    OutPitch = FMath::Clamp(AltDiff / 1200.0f, -0.6f, 0.6f);

    // 속도 유지 Throttle 계산 (15 m/s 유지)
    OutThrottle = (CurrentSpeed < TargetWp.Speed_cm_s) ? 0.3f : -0.3f;
}