#pragma once

#include "CoreMinimal.h"
#include "WaypointItemData.generated.h"

USTRUCT(BlueprintType)
struct FWaypointItemData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int32 Id = 0;

	// 언리얼 월드 좌표 (cm 단위)
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FVector Location = FVector::ZeroVector;

	// 이동 속도 (cm/s 단위)
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	float Speed_cm_s = 1500.0f; // 15 m/s
};