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
struct FTargetConfig
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	FString ElementName;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	TObjectPtr<UStaticMesh> ElementMesh;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset", meta = (ClampMin = "0.0"))
	float SpawnWeight;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset", meta = (ClampMin = "0.0", ClampMax = "90.0"))
	float MaxSlopeAngle;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	FVector2D ScaleRange;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset", meta = (ToolTip = "공중에 뜨면 음수(-), 땅에 파묻히면 양수(+)로 높이 조절"))
	float ZOffset = -5.0f;

	// AI 학습용 표적(차량/목표물) 여부
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	bool bIsTarget;

	// 라벨 ID (예: 0=승용차, 1=트럭 등)
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	int32 TargetClassId;

	FTargetConfig()
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
struct FTargetRecord
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

	FTargetRecord()
		: ClassId(0)
		, ClassName(TEXT(""))
		, WorldLocation(FVector::ZeroVector)
		, WorldRotation(FRotator::ZeroRotator)
		, WorldExtent(FVector::ZeroVector)
	{
	}
};

// =============================================================================
// ⭐️ 비히클 전체 배치 정보 영구 저장용 구조체 (.umap 직렬화 보장)
// =============================================================================
USTRUCT(BlueprintType)
struct FSavedVehicleInstance
{
	GENERATED_BODY()

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	int32 ElementIndex = 0;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	FTransform WorldTransform;
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
	UFUNCTION(BlueprintCallable, Category = "Dataset")
	FVector GetMapCenter() const { return CachedMapCenter; }

	UFUNCTION(BlueprintCallable, Category = "Dataset")
	FVector GetMapExtent() const { return CachedMapExtent; }

	UFUNCTION(BlueprintCallable, Category = "Dataset")
	FVector GetMapCenterWithAltitude(float AltitudeCm) const
	{
		return FVector(CachedMapCenter.X, CachedMapCenter.Y, AltitudeCm);
	}

	UFUNCTION(BlueprintCallable, Category = "Dataset")
	FVector GetTerrainSpawnLocation(float DesiredAlt = 3000.0f);

	// -------------------------------------------------------------------------
	// 2. 타겟 생성 및 관리 에디터 버튼 (CallInEditor)
	// -------------------------------------------------------------------------
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Dataset")
	void GenerateTarget();

	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Dataset")
	void ClearEnvironment();

	UFUNCTION(BlueprintCallable, Category = "Dataset")
	void SetupHISMComponents();

	// ⭐️ 현재 HISM 배치 상태(바다 제거 후)를 기반으로 SavedInstances와 SpawnedTargets를 동기화 및 영구 박제
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Dataset")
	void FinalizeAndSaveState();

	// ⭐️ 저장된 TargetElements + SavedInstances로부터 화면의 HISM 메쉬와 Ground Truth를 즉시 복원
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Dataset")
	void RestoreFromSavedState();

	// -------------------------------------------------------------------------
	// 3. AI 데이터셋 출력 인터페이스
	// -------------------------------------------------------------------------
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Dataset")
	bool ExportTargetsToJson(const FString& FileName);

	UFUNCTION(BlueprintPure, Category = "Dataset")
	const TArray<FTargetRecord>& GetSpawnedTargets() const { return SpawnedTargets; }

	// -------------------------------------------------------------------------
	// 4. 에디터 설정 및 영구 저장 프로퍼티
	// -------------------------------------------------------------------------
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	TArray<FTargetConfig> TargetElements;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset", meta = (ClampMin = "50.0"))
	float GridSpacing;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	float PositionJitter;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset", meta = (ClampMin = "0.0", ClampMax = "1.0"))
	float AlignToSurfaceNormal;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dataset")
	FVector FallbackBoundsExtent;

	// 런타임 HISM 컴포넌트
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset`")
	TArray<TObjectPtr<UHierarchicalInstancedStaticMeshComponent>> HISMComponents;

	// ⭐️ 확정된 모든 차량의 배치 좌표 (.umap에 영구 저장됨)
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	TArray<FSavedVehicleInstance> SavedInstances;

	// ⭐️ AI 추론용 Ground Truth 정답 배열 (.umap에 영구 저장됨)
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset")
	TArray<FTargetRecord> SpawnedTargets;

	// -------------------------------------------------------------------------
	// ⭐️ 4-0. 에디터 디테일 패널용 배치 현황 요약 (읽기 전용)
	// -------------------------------------------------------------------------
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset", meta = (DisplayName = "전체 배치 차량 수 (Total Vehicles)"))
	int32 TotalVehicleCount = 0;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset", meta = (DisplayName = "AI 타겟 표적 수 (Ground Truth Targets)"))
	int32 TotalTargetCount = 0;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset", meta = (DisplayName = "일반 배경 차량 수 (Normal Vehicles)"))
	int32 NormalVehicleCount = 0;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Dataset", meta = (DisplayName = "차량 종류별 배치 수 (Count By Element)"))
	TMap<FString, int32> SpawnCountByElement;


protected:
	virtual void PostLoad() override;
	virtual void OnConstruction(const FTransform& Transform) override;
	virtual void BeginPlay() override;

private:
	void CalculateLandscapeBounds();
	FBox GetTotalLandscapeBounds(TArray<AActor*>& OutLandscapeActors);
	bool NeedsHISMRebuild() const;

	UPROPERTY(VisibleAnywhere, Category = "Dataset")
	FVector CachedMapCenter = FVector::ZeroVector;

	UPROPERTY(VisibleAnywhere, Category = "Dataset")
	FVector CachedMapExtent = FVector::ZeroVector;

	void UpdateSummaryCounts(); // ⭐️ 디테일 패널 카운트 갱신 헬퍼
};