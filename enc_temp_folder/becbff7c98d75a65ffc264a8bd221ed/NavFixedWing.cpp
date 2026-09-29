#include "NavFixedWing.h"

FNavFixedWing::FNavFixedWing()
{
    CurrentSpeed = 1500.0f; // 15 m/s로 시작
    InputThrottle = 0.0f;
    InputRoll = 0.0f;
    InputPitch = 0.0f;
    InputYaw = 0.0f;
}

void FNavFixedWing::SetManualInput(float Throttle, float Roll, float Pitch, float Yaw)
{
    InputThrottle = Throttle;
    InputRoll = Roll;
    InputPitch = Pitch;
    InputYaw = Yaw;
}

void FNavFixedWing::Step(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
    FVector& OutNextLoc, FRotator& OutNextRot)
{
    // 1. 스로틀 입력으로 비행 속도 가감속 (키를 떼어도 MinSpeed 아래로는 떨어지지 않음)
    CurrentSpeed = FMath::Clamp(CurrentSpeed + (InputThrottle * 800.0f * DeltaTime), MinSpeed, MaxSpeed);

    // 2. 조향 각도 계산 (비행기식 뱅크 턴 연동)
    FRotator TargetRot = CurrentRot;

    // Pitch: 엘리베이터 (기수 올림/내림, 최대 ±35도)
    TargetRot.Pitch = FMath::Clamp(TargetRot.Pitch + (InputPitch * 45.0f * DeltaTime), -35.0f, 35.0f);

    // Roll: 날개 기울기 (최대 40도 뱅크각으로 부드럽게 기울어짐)
    float TargetBankRoll = InputRoll * 40.0f;
    TargetRot.Roll = FMath::FInterpTo(CurrentRot.Roll, TargetBankRoll, DeltaTime, 4.0f);

    // Yaw: 날개가 기울어진 뱅크각에 비례하여 비행기가 자연스럽게 선회 + 러더(Yaw) 입력
    float TurnRateFromRoll = (TargetRot.Roll / 40.0f) * 50.0f;
    TargetRot.Yaw += (TurnRateFromRoll + InputYaw * 40.0f) * DeltaTime;

    // 자세 회전값 부드럽게 보간
    OutNextRot = FMath::RInterpTo(CurrentRot, TargetRot, DeltaTime, 5.0f);

    // 3. 고정익은 기수가 바라보는 전방(Forward)으로 끊임없이 전진 비행!
    FVector ForwardDir = OutNextRot.Vector();
    OutNextLoc = CurrentLoc + (ForwardDir * CurrentSpeed * DeltaTime);
}