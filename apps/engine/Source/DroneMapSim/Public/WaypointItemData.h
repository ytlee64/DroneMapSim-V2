#pragma once

#include "CoreMinimal.h"
struct FWaypointItemData
{
	int32 Id = 0;

	FVector Location = FVector::ZeroVector;

	float Speed_cm_s = 1500.0f; // 15 m/s
};