#pragma once

#include "Math/Vector.h"
#include "Math/Rotator.h"
#include "Containers/Array.h"
#include "WaypointItemData.h"



class INavBase
{
public:
	INavBase();
    virtual ~INavBase() = default;

    virtual void SetManualInput(float Throttle, float Roll, float Pitch, float Yaw) = 0;

    // 현재 위치/회전 + 시간 -> 다음 위치/회전 계산
    virtual void Step(const FVector& CurrentLoc, const FRotator& CurrentRot, float DeltaTime,
        FVector& OutNextLoc, FRotator& OutNextRot) = 0;

    virtual float GetCurrentSpeed() = 0;

    // -------------------------------------------------------------
    // 자율주행(AUTO) 모드 온/오프 트리거
    // - 상태(bAutoNavEnabled)는 모든 항법 엔진에 공통이므로 여기서 관리
    // - 파생 클래스는 필요 시 override 후 Super(INavBase::SetAutoNavEnabled)를 호출해
    //   추가 초기화(궤적 리셋 등)를 수행할 수 있음
    // -------------------------------------------------------------
    TArray<FWaypointItemData> Waypoints;

    void SetAutoNav(bool bEnabled) { bAutoNav = bEnabled; }
    bool IsAutoNav() const { return bAutoNav; }

protected:
    bool bAutoNav = false;
    int32 CurrentWaypointIndex = 0;
};