#pragma once

#include "Math/Vector.h"
#include "Math/Rotator.h"
#include "Containers/Array.h"

class INavBase
{
public:
    virtual ~INavBase() = default;

    virtual void SetManualInput(float Throttle, float Roll, float Pitch, float Yaw) = 0;

    // 현재 위치/회전 + 시간 -> 다음 위치/회전 계산
    virtual void Step(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
        FVector& OutNextLoc, FRotator& OutNextRot) = 0;
 
};