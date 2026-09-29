#include "DroneMathUtil.h"

bool DroneMathUtil::ProjectWorldPointToScreenExact(
    const FVector& WorldPoint,
    const FVector& CamLoc,
    const FRotator& CamRot,
    float CamFOV,
    float RenderWidth,
    float RenderHeight,
    FVector2D& OutNormalizedPos)
{
    FTransform CamTransform(CamRot, CamLoc);
    FVector LocalPoint = CamTransform.InverseTransformPosition(WorldPoint);
    if (LocalPoint.X <= 1.0f) return false; // 카메라 뒤쪽은 제외

    float AspectRatio = RenderWidth / RenderHeight;
    float HalfHFOVRads = FMath::DegreesToRadians(CamFOV * 0.5f);
    float TanHalfHFOV = FMath::Tan(HalfHFOVRads);
    float TanHalfVFOV = TanHalfHFOV / AspectRatio;

    float ScreenX = (LocalPoint.Y / (LocalPoint.X * TanHalfHFOV)) * 0.5f + 0.5f;
    float ScreenY = 0.5f - (LocalPoint.Z / (LocalPoint.X * TanHalfVFOV)) * 0.5f;

    OutNormalizedPos = FVector2D(ScreenX, ScreenY);
    return true;
}
