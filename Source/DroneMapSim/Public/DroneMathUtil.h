#pragma once

#include "CoreMinimal.h"

// -----------------------------------------------------------------------------
// [수학 헬퍼]: 공용 수학 유틸리티 함수 모음
// -----------------------------------------------------------------------------
class DroneMathUtil
{
public:
    // 3D 월드 좌표를 카메라 뷰포트 정규화 좌표(0.0 ~ 1.0)로 정확히 투영
    static bool ProjectWorldPointToScreenExact(
        const FVector& WorldPoint,
        const FVector& CamLoc,
        const FRotator& CamRot,
        float CamFOV,
        float RenderWidth,
        float RenderHeight,
        FVector2D& OutNormalizedPos);
};
