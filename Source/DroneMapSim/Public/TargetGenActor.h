// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "TargetGenActor.generated.h"

class UStaticMesh;
class UHierarchicalInstancedStaticMeshComponent;

// =============================================================================
// 지형 생성 요소 설정 구조체
// =============================================================================
USTRUCT(BlueprintType)
struct FEnvElementConfig
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Config")
	FString ElementName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Config")
	TObjectPtr<UStaticMesh> ElementMesh;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Config", meta = (ClampMin = "0.0"))
	float SpawnWeight;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Config", meta = (ClampMin = "0.0", ClampMax = "90.0"))
	float MaxSlopeAngle;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Config")
	FVector2D ScaleRange;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Environment", meta = (ToolTip = "공중에 뜨면 음수(-), 땅에 파묻히면 양수(+)로 높이 조절"))
	float ZOffset = -5.0f;

	// AI 학습용 표적(차량/목표물) 여부
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	bool bIsTarget;

	// 라벨 ID (예: 0=승용차, 1=트럭 등)
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	int32 TargetClassId;

	FEnvElementConfig()
		: ElementName(TEXT("Element"))
		, ElementMesh(nullptr)
		, SpawnWeight(1.0f)
		, MaxSlopeAngle(45.0f)
		, ScaleRange(FVector2D(0.8f, 1.2f))
		, bIsTarget(false)
		, TargetClassId(0)
	{
	}
};

// =============================================================================
// 데이터셋 정답(Ground Truth) 기록 구조체
// =============================================================================
USTRUCT(BlueprintType)
struct FEnvTargetRecord
{
	GENERATED_BODY()

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	int32 ClassId;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	FString ClassName;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	FVector WorldLocation;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	FRotator WorldRotation;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	FVector WorldExtent;

	FEnvTargetRecord()
		: ClassId(0)
		, ClassName(TEXT(""))
		, WorldLocation(FVector::ZeroVector)
		, WorldRotation(FRotator::ZeroRotator)
		, WorldExtent(FVector::ZeroVector)
	{
	}
};

// =============================================================================
// 타겟 관리 및 생성기 액터 (ATargetGenActor)
// =============================================================================
UCLASS()
class DRONEMAPSIM_API ATargetGenActor : public AActor
{
	GENERATED_BODY()

public:
	ATargetGenActor();

	// -------------------------------------------------------------------------
	// 1. 외부 액터(드론, 관찰자 등)를 위한 좌표/지형 인터페이스
	// -------------------------------------------------------------------------
	UFUNCTION(BlueprintCallable, Category = "Environment")
	FVector GetMapCenter() const { return CachedMapCenter; }

	UFUNCTION(BlueprintCallable, Category = "Environment")
	FVector GetMapExtent() const { return CachedMapExtent; }

	UFUNCTION(BlueprintCallable, Category = "Environment")
	FVector GetMapCenterWithAltitude(float AltitudeCm) const
	{
		return FVector(CachedMapCenter.X, CachedMapCenter.Y, AltitudeCm);
	}

	UFUNCTION(BlueprintCallable, Category = "Environment")
	FVector GetTerrainSpawnLocation(float DesiredAlt = 3000.0f);

	// -------------------------------------------------------------------------
	// 2. 타겟 생성 및 관리 에디터 버튼 (CallInEditor)
	// -------------------------------------------------------------------------
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "EnvGen")
	void GenerateEnvironment();

	UFUNCTION(BlueprintCallable, CallInEditor, Category = "EnvGen")
	void ClearEnvironment();

	UFUNCTION(BlueprintCallable, Category = "EnvGen")
	void SetupHISMComponents();

	// -------------------------------------------------------------------------
	// 3. AI 데이터셋 출력 인터페이스
	// -------------------------------------------------------------------------
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Dataset")
	bool ExportTargetsToJson(const FString& FileName);

	UFUNCTION(BlueprintPure, Category = "Dataset")
	const TArray<FEnvTargetRecord>& GetSpawnedTargets() const { return SpawnedTargets; }

	// -------------------------------------------------------------------------
	// 4. 에디터 설정 프로퍼티
	// -------------------------------------------------------------------------
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "EnvGen|Config")
	TArray<FEnvElementConfig> EnvElements;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "EnvGen|Config", meta = (ClampMin = "50.0"))
	float GridSpacing;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "EnvGen|Config")
	float PositionJitter;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "EnvGen|Config", meta = (ClampMin = "0.0", ClampMax = "1.0"))
	float AlignToSurfaceNormal;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "EnvGen|Config")
	FVector FallbackBoundsExtent;

	// 런타임 인스턴스 및 기록 데이터
	UPROPERTY(VisibleInstanceOnly, BlueprintReadOnly, Category = "EnvGen|Runtime")
	TArray<TObjectPtr<UHierarchicalInstancedStaticMeshComponent>> HISMComponents;

	UPROPERTY(VisibleInstanceOnly, BlueprintReadOnly, Category = "Dataset|Runtime")
	TArray<FEnvTargetRecord> SpawnedTargets;

protected:
	virtual void BeginPlay() override;

private:
	// 지형 바운드 계산 및 캐싱
	void CalculateLandscapeBounds();

	// 월드 전체 Landscape/Proxy 통합 바운딩 박스를 계산하는 공통 헬퍼
	FBox GetTotalLandscapeBounds(TArray<AActor*>& OutLandscapeActors);

	UPROPERTY(VisibleAnywhere, Category = "Environment")
	FVector CachedMapCenter = FVector::ZeroVector;

	UPROPERTY(VisibleAnywhere, Category = "Environment")
	FVector CachedMapExtent = FVector::ZeroVector;
};