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
    // 1. [실제 항공기 조타면 물리]: 조종 입력값 자체를 부드럽게 스무딩
    //    (GCS 통신이 띄엄띄엄 오더라도 드론 스스로 부드러운 곡선을 그림)
    // -------------------------------------------------------------
    static float FilteredRoll = 0.0f;
    static float FilteredPitch = 0.0f;
    static float FilteredYaw = 0.0f;

    // 조타면이 기계적으로 천천히 움직이는 반응 속도 (공력 반응)
    FilteredRoll = FMath::FInterpTo(FilteredRoll, InputRoll, DeltaTime, 3.0f);
    FilteredPitch = FMath::FInterpTo(FilteredPitch, InputPitch, DeltaTime, 3.0f);
    FilteredYaw = FMath::FInterpTo(FilteredYaw, InputYaw, DeltaTime, 3.0f);

    // -------------------------------------------------------------
    // 2. 스로틀 및 속도 가감속 (관성 반영)
    // -------------------------------------------------------------
    CurrentSpeed = FMath::Clamp(CurrentSpeed + (InputThrottle * 600.0f * DeltaTime), MinSpeed, MaxSpeed);

    // -------------------------------------------------------------
    // 3. 기체 회전각 계산 (실제 고정익 뱅크-턴 비행 역학)
    // -------------------------------------------------------------
    FRotator TargetRot = CurrentRot;

    // [Roll]: 필터링된 조타값에 따라 최대 40도까지 날개를 부드럽게 기울임
    float TargetBankRoll = FilteredRoll * 40.0f;
    TargetRot.Roll = FMath::FInterpTo(CurrentRot.Roll, TargetBankRoll, DeltaTime, 2.5f);

    // [Pitch]: 승강타 반응 (최대 ±30도)
    float TargetPitch = FilteredPitch * 30.0f;
    TargetRot.Pitch = FMath::FInterpTo(CurrentRot.Pitch, TargetPitch, DeltaTime, 3.0f);

    // [Yaw]: ⭐️ 비행 역학의 핵심!
    // 날개가 기울어진 각도(Roll)에 따라 공기역학적으로 원을 그리며 자연 선회 + 러더
    float CoordinatedTurnRate = (TargetRot.Roll / 40.0f) * 45.0f; // 뱅크 비례 선회 각속도
    float RudderRate = FilteredYaw * 35.0f;                       // 러더 편향 각속도
    TargetRot.Yaw += (CoordinatedTurnRate + RudderRate) * DeltaTime;

    // 최종 자세 보간
    OutNextRot = TargetRot;

    // -------------------------------------------------------------
    // 4. 고정익은 기수가 가리키는 벡터 방향으로 끊임없이 전진
    // -------------------------------------------------------------
    FVector ForwardDir = OutNextRot.Vector();
    OutNextLoc = CurrentLoc + (ForwardDir * CurrentSpeed * DeltaTime);

    // -------------------------------------------------------------
    // 5. ⭐️ [자동 중립 감쇠]: GCS에서 추가 패킷이 안 오면 조타면이 스스로 중립(0)으로 복귀!
    // -------------------------------------------------------------
    InputRoll = FMath::FInterpTo(InputRoll, 0.0f, DeltaTime, 2.0f);
    InputPitch = FMath::FInterpTo(InputPitch, 0.0f, DeltaTime, 2.0f);
    InputYaw = FMath::FInterpTo(InputYaw, 0.0f, DeltaTime, 2.0f);
    InputThrottle = FMath::FInterpTo(InputThrottle, 0.0f, DeltaTime, 2.0f);
}