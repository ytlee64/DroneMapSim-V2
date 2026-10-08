// Copyright Epic Games, Inc. All Rights Reserved.

#include "TargetGenActor.h"
#include "Engine/World.h"
#include "Engine/StaticMesh.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Landscape.h"
#include "LandscapeProxy.h"
#include "EngineUtils.h"
#include "Kismet/KismetMathLibrary.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Kismet/GameplayStatics.h"

ATargetGenActor::ATargetGenActor()
{
	PrimaryActorTick.bCanEverTick = false;

	USceneComponent* SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRoot"));
	RootComponent = SceneRoot;
	RootComponent->SetMobility(EComponentMobility::Static);

	GridSpacing = 1200.0f;
	PositionJitter = 400.0f;
	AlignToSurfaceNormal = 0.0f;
	FallbackBoundsExtent = FVector(50000.0f, 50000.0f, 10000.0f);
}

void ATargetGenActor::PostLoad()
{
	Super::PostLoad();

	// ⭐️ 언리얼 에디터를 열었을 때 저장된 메쉬와 배치 정보가 있다면 즉시 복원
	if (TargetElements.Num() > 0 && SavedInstances.Num() > 0 && NeedsHISMRebuild())
	{
		RestoreFromSavedState();
	}
}

void ATargetGenActor::OnConstruction(const FTransform& Transform)
{
	Super::OnConstruction(Transform);

	// ⭐️ 에디터 뷰포트 갱신 시 HISM이 비어 있으면 저장된 배치 정보로 즉시 복원
	if (TargetElements.Num() > 0 && SavedInstances.Num() > 0 && NeedsHISMRebuild())
	{
		RestoreFromSavedState();
	}
}

void ATargetGenActor::BeginPlay()
{
	Super::BeginPlay();

	CalculateLandscapeBounds();

	// ⭐️ Play(PIE) 시작 시 메쉬 또는 Ground Truth가 비어있다면 저장된 상태에서 즉시 복원 보장
	if (TargetElements.Num() > 0 && SavedInstances.Num() > 0)
	{
		if (NeedsHISMRebuild() || SpawnedTargets.Num() == 0)
		{
			RestoreFromSavedState();
		}
	}
}

bool ATargetGenActor::NeedsHISMRebuild() const
{
	if (HISMComponents.Num() == 0)
	{
		return true;
	}

	int32 TotalHISMInstances = 0;
	for (const auto& HISM : HISMComponents)
	{
		if (IsValid(HISM))
		{
			TotalHISMInstances += HISM->GetInstanceCount();
		}
	}

	return TotalHISMInstances == 0;
}

FBox ATargetGenActor::GetTotalLandscapeBounds(TArray<AActor*>& OutLandscapeActors)
{
	OutLandscapeActors.Empty();
	FBox TotalBox(ForceInit);

	if (!GetWorld()) return TotalBox;

	UGameplayStatics::GetAllActorsOfClass(GetWorld(), ALandscapeProxy::StaticClass(), OutLandscapeActors);
	if (OutLandscapeActors.Num() == 0)
	{
		UGameplayStatics::GetAllActorsOfClass(GetWorld(), ALandscape::StaticClass(), OutLandscapeActors);
	}

	for (AActor* Piece : OutLandscapeActors)
	{
		if (Piece)
		{
			FVector Origin, Extent;
			Piece->GetActorBounds(false, Origin, Extent);
			if (!Extent.IsNearlyZero())
			{
				TotalBox += FBox(Origin - Extent, Origin + Extent);
			}
		}
	}

	return TotalBox;
}

void ATargetGenActor::CalculateLandscapeBounds()
{
	TArray<AActor*> LandscapePieces;
	FBox TotalBox = GetTotalLandscapeBounds(LandscapePieces);

	if (TotalBox.IsValid && LandscapePieces.Num() > 0)
	{
		CachedMapCenter = TotalBox.GetCenter();
		CachedMapExtent = TotalBox.GetExtent();

		UE_LOG(LogTemp, Warning, TEXT("[TargetGenActor] Landscape Analyzed!"));
		UE_LOG(LogTemp, Warning, TEXT(" -> Center: X=%.1f, Y=%.1f, Z=%.1f"), CachedMapCenter.X, CachedMapCenter.Y, CachedMapCenter.Z);
		UE_LOG(LogTemp, Warning, TEXT(" -> Size: %.1f km x %.1f km"), (CachedMapExtent.X * 2.0f) / 100000.0f, (CachedMapExtent.Y * 2.0f) / 100000.0f);
	}
	else
	{
		UE_LOG(LogTemp, Error, TEXT("[TargetGenActor] No Landscape found in world! Fallback to (0,0,0)"));
		CachedMapCenter = FVector::ZeroVector;
		CachedMapExtent = FallbackBoundsExtent;
	}
}

FVector ATargetGenActor::GetTerrainSpawnLocation(float DesiredAlt)
{
	TArray<AActor*> LandscapePieces;
	FBox TotalBox = GetTotalLandscapeBounds(LandscapePieces);

	if (TotalBox.IsValid && LandscapePieces.Num() > 0)
	{
		FVector Center = TotalBox.GetCenter();
		FVector TraceStart = FVector(Center.X, Center.Y, TotalBox.Max.Z + 10000.0f);
		FVector TraceEnd = FVector(Center.X, Center.Y, TotalBox.Min.Z - 10000.0f);

		FHitResult HitResult;
		FCollisionQueryParams TraceParams(FName(TEXT("TerrainTrace")), true);

		if (GetWorld()->LineTraceSingleByChannel(HitResult, TraceStart, TraceEnd, ECC_Visibility, TraceParams))
		{
			return HitResult.ImpactPoint + FVector(0.0f, 0.0f, DesiredAlt);
		}

		return FVector(Center.X, Center.Y, TotalBox.Max.Z + DesiredAlt);
	}

	return FVector(0.0f, 0.0f, DesiredAlt);
}

void ATargetGenActor::ClearEnvironment()
{
#if WITH_EDITOR
	Modify();
#endif

	TArray<UHierarchicalInstancedStaticMeshComponent*> ExistingHISMs;
	GetComponents<UHierarchicalInstancedStaticMeshComponent>(ExistingHISMs);
	for (UHierarchicalInstancedStaticMeshComponent* Comp : ExistingHISMs)
	{
		if (Comp)
		{
			Comp->ClearInstances();
			RemoveInstanceComponent(Comp);
			Comp->DestroyComponent();
		}
	}
	HISMComponents.Empty();
	SavedInstances.Empty();
	SpawnedTargets.Empty();
	UpdateSummaryCounts(); // ⭐️ 0으로 초기화 반영
	UE_LOG(LogTemp, Log, TEXT("[TargetGen] Cleared all existing instances and target records."));
}

void ATargetGenActor::SetupHISMComponents()
{
	// HISM 컴포넌트만 정리
	TArray<UHierarchicalInstancedStaticMeshComponent*> ExistingHISMs;
	GetComponents<UHierarchicalInstancedStaticMeshComponent>(ExistingHISMs);
	for (UHierarchicalInstancedStaticMeshComponent* Comp : ExistingHISMs)
	{
		if (Comp)
		{
			Comp->ClearInstances();
			RemoveInstanceComponent(Comp);
			Comp->DestroyComponent();
		}
	}
	HISMComponents.Empty();

	if (!RootComponent)
	{
		USceneComponent* SceneRoot = NewObject<USceneComponent>(this, TEXT("SceneRoot"));
		RootComponent = SceneRoot;
		RootComponent->RegisterComponent();
	}
	RootComponent->SetMobility(EComponentMobility::Static);

	for (int32 i = 0; i < TargetElements.Num(); ++i)
	{
		UStaticMesh* Mesh = TargetElements[i].ElementMesh;
		if (Mesh)
		{
			FString CleanName = TargetElements[i].ElementName.IsEmpty() ? Mesh->GetName() : TargetElements[i].ElementName;
			FName CompName = MakeUniqueObjectName(this, UHierarchicalInstancedStaticMeshComponent::StaticClass(), *FString::Printf(TEXT("HISM_%d_%s"), i, *CleanName));

			UHierarchicalInstancedStaticMeshComponent* HISM = NewObject<UHierarchicalInstancedStaticMeshComponent>(
				this,
				UHierarchicalInstancedStaticMeshComponent::StaticClass(),
				CompName,
				RF_Transactional
			);

			if (HISM)
			{
				// ⭐️ 핵심: Instance 컴포넌트로 명시해야 레벨(.umap)에 직렬화됨
				HISM->CreationMethod = EComponentCreationMethod::Instance;
				AddInstanceComponent(HISM);

				HISM->SetMobility(EComponentMobility::Static);
				HISM->SetStaticMesh(Mesh);
				HISM->AttachToComponent(RootComponent, FAttachmentTransformRules::KeepWorldTransform);
				if (GetWorld())
				{
					HISM->RegisterComponentWithWorld(GetWorld());
				}

				HISM->bCastDynamicShadow = true;
				HISM->bAffectDynamicIndirectLighting = true;
				HISM->InstanceStartCullDistance = 0;
				HISM->InstanceEndCullDistance = 0;

				HISMComponents.Add(HISM);
			}
		}
		else
		{
			HISMComponents.Add(nullptr);
		}
	}
}

void ATargetGenActor::GenerateTarget()
{
	UE_LOG(LogTemp, Log, TEXT("=================================================="));
	UE_LOG(LogTemp, Log, TEXT("[TargetGen] STARTING GENERATION"));
	UE_LOG(LogTemp, Log, TEXT("=================================================="));

	UWorld* World = GetWorld();
	if (!World)
	{
		UE_LOG(LogTemp, Error, TEXT("[TargetGen] Invalid World pointer"));
		return;
	}

#if WITH_EDITOR
	Modify();
#endif

	if (TargetElements.Num() == 0)
	{
		UE_LOG(LogTemp, Error, TEXT("[TargetGen] TargetElements list is empty"));
		return;
	}

	CalculateLandscapeBounds();

	TArray<AActor*> AllLandscapeActors;
	FBox TotalLandscapeBounds = GetTotalLandscapeBounds(AllLandscapeActors);

	FVector MinBound;
	FVector MaxBound;

	if (TotalLandscapeBounds.IsValid && AllLandscapeActors.Num() > 0)
	{
		MinBound = TotalLandscapeBounds.Min;
		MaxBound = TotalLandscapeBounds.Max;
		UE_LOG(LogTemp, Log, TEXT("[TargetGen] Detected %d Landscape Actor proxies"), AllLandscapeActors.Num());
	}
	else
	{
		FVector ActorLoc = GetActorLocation();
		MinBound = ActorLoc - FallbackBoundsExtent;
		MaxBound = ActorLoc + FallbackBoundsExtent;
		UE_LOG(LogTemp, Warning, TEXT("[TargetGen] No Landscape found. Using fallback bounds around Actor"));
	}

	SetupHISMComponents();
	SavedInstances.Empty();
	SpawnedTargets.Empty();

	int32 TotalGridPoints = 0;
	int32 FailRaycastMiss = 0;
	int32 FailNotLandscape = 0;
	int32 FailSlopeAngle = 0;
	int32 FailNullComponent = 0;
	int32 FailSpawnProbability = 0;
	int32 TotalSpawned = 0;

	FCollisionQueryParams TraceParams(FName(TEXT("EnvTrace")), true, this);
	TraceParams.AddIgnoredActor(this);
	TraceParams.bReturnPhysicalMaterial = false;

	for (float CurrentX = MinBound.X; CurrentX <= MaxBound.X; CurrentX += GridSpacing)
	{
		for (float CurrentY = MinBound.Y; CurrentY <= MaxBound.Y; CurrentY += GridSpacing)
		{
			TotalGridPoints++;

			float SampleX = CurrentX + FMath::FRandRange(-PositionJitter, PositionJitter);
			float SampleY = CurrentY + FMath::FRandRange(-PositionJitter, PositionJitter);

			FVector TraceStart(SampleX, SampleY, MaxBound.Z + 50000.0f);
			FVector TraceEnd(SampleX, SampleY, MinBound.Z - 50000.0f);
			FHitResult HitResult;

			bool bHit = World->LineTraceSingleByChannel(HitResult, TraceStart, TraceEnd, ECC_Visibility, TraceParams);
			if (!bHit)
			{
				bHit = World->LineTraceSingleByChannel(HitResult, TraceStart, TraceEnd, ECC_WorldStatic, TraceParams);
			}

			if (!bHit)
			{
				FailRaycastMiss++;
				continue;
			}

			AActor* HitActor = HitResult.GetActor();
			if (!HitActor)
			{
				FailRaycastMiss++;
				continue;
			}

			bool bIsLandscape = HitActor->IsA<ALandscapeProxy>() || HitActor->IsA<ALandscape>();
			if (AllLandscapeActors.Num() > 0 && !bIsLandscape)
			{
				FailNotLandscape++;
				continue;
			}

			FVector SurfaceNormal = HitResult.ImpactNormal;
			float SlopeAngle = FMath::RadiansToDegrees(FMath::Acos(FMath::Clamp(FVector::DotProduct(SurfaceNormal, FVector::UpVector), -1.0f, 1.0f)));

			TArray<int32> ValidIndices;
			float TotalWeight = 0.0f;

			for (int32 i = 0; i < TargetElements.Num(); ++i)
			{
				if (HISMComponents.IsValidIndex(i) && HISMComponents[i] != nullptr && TargetElements[i].ElementMesh != nullptr)
				{
					ValidIndices.Add(i);
					TotalWeight += TargetElements[i].SpawnWeight;
				}
			}

			if (ValidIndices.Num() == 0 || TotalWeight <= 0.0f)
			{
				FailNullComponent++;
				continue;
			}

			float RandomRoll = FMath::FRand();

			if (RandomRoll > TotalWeight)
			{
				FailSpawnProbability++;
				continue;
			}

			int32 SelectedIndex = ValidIndices[0];
			float CurrentWeightSum = 0.0f;

			for (int32 ValidIdx : ValidIndices)
			{
				CurrentWeightSum += TargetElements[ValidIdx].SpawnWeight;
				if (RandomRoll <= CurrentWeightSum)
				{
					SelectedIndex = ValidIdx;
					break;
				}
			}

			const FTargetConfig& SelectedConfig = TargetElements[SelectedIndex];
			if (SlopeAngle > SelectedConfig.MaxSlopeAngle)
			{
				FailSlopeAngle++;
				continue;
			}

			UHierarchicalInstancedStaticMeshComponent* TargetHISM = HISMComponents[SelectedIndex];
			if (!TargetHISM)
			{
				FailNullComponent++;
				continue;
			}

			FVector Location = HitResult.ImpactPoint;
			FRotator BaseRot = FRotator(0.0f, FMath::FRandRange(0.0f, 360.0f), 0.0f);

			if (AlignToSurfaceNormal > 0.0f)
			{
				FRotator AlignRot = UKismetMathLibrary::MakeRotFromZ(SurfaceNormal);
				BaseRot = FMath::Lerp(BaseRot, AlignRot, AlignToSurfaceNormal);
			}

			float UniformScale = FMath::FRandRange(SelectedConfig.ScaleRange.X, SelectedConfig.ScaleRange.Y);
			FVector Scale(UniformScale);

			FBoxSphereBounds MeshBounds = SelectedConfig.ElementMesh->GetBounds();
			float AutoBottomOffset = (MeshBounds.Origin.Z - MeshBounds.BoxExtent.Z) * Scale.Z;
			Location.Z -= AutoBottomOffset;
			Location.Z += (SelectedConfig.ZOffset * Scale.Z);

			FTransform InstanceTransform(BaseRot, Location, Scale);
			TargetHISM->AddInstance(InstanceTransform, true);
			TotalSpawned++;
		}
	}

	// ⭐️ 생성 직후 HISM 상태를 SavedInstances 및 SpawnedTargets에 기록하고 JSON 출력
	FinalizeAndSaveState();

	UE_LOG(LogTemp, Log, TEXT("=================================================="));
	UE_LOG(LogTemp, Log, TEXT("[TargetGen] DIAGNOSTIC REPORT"));
	UE_LOG(LogTemp, Log, TEXT("  - Total Grid Points Checked : %d"), TotalGridPoints);
	UE_LOG(LogTemp, Log, TEXT("  - Skipped (Probability Pass): %d"), FailSpawnProbability);
	UE_LOG(LogTemp, Log, TEXT("  - Total Spawned Instances   : %d"), TotalSpawned);
	UE_LOG(LogTemp, Log, TEXT("  - Recorded Target Objects   : %d"), SpawnedTargets.Num());
	UE_LOG(LogTemp, Log, TEXT("=================================================="));
}

void ATargetGenActor::FinalizeAndSaveState()
{
#if WITH_EDITOR
	Modify();
#endif

	SavedInstances.Empty();
	SpawnedTargets.Empty();

	const int32 Count = FMath::Min(TargetElements.Num(), HISMComponents.Num());
	for (int32 ElemIdx = 0; ElemIdx < Count; ++ElemIdx)
	{
		const FTargetConfig& Config = TargetElements[ElemIdx];
		UHierarchicalInstancedStaticMeshComponent* HISM = HISMComponents[ElemIdx];

		if (!IsValid(HISM) || !IsValid(Config.ElementMesh))
		{
			continue;
		}

		const FBoxSphereBounds MeshBounds = Config.ElementMesh->GetBounds();
		const int32 InstCount = HISM->GetInstanceCount();

		for (int32 InstIdx = 0; InstIdx < InstCount; ++InstIdx)
		{
			FTransform WorldTr;
			if (HISM->GetInstanceTransform(InstIdx, WorldTr, /*bWorldSpace=*/true))
			{
				// 1. 전체 비히클 배치 정보 영구 기록
				FSavedVehicleInstance SavedInst;
				SavedInst.ElementIndex = ElemIdx;
				SavedInst.WorldTransform = WorldTr;
				SavedInstances.Add(SavedInst);

				// 2. AI 표적(bIsTarget)인 경우 Ground Truth(SpawnedTargets)에 기록
				if (Config.bIsTarget)
				{
					FTargetRecord Record;
					Record.ClassId = Config.TargetClassId;
					Record.ClassName = Config.ElementName.IsEmpty() ? Config.ElementMesh->GetName() : Config.ElementName;
					Record.WorldLocation = WorldTr.GetLocation();
					Record.WorldRotation = WorldTr.GetRotation().Rotator();
					Record.WorldExtent = MeshBounds.BoxExtent * WorldTr.GetScale3D();

					SpawnedTargets.Add(Record);
				}
			}
		}
	}

	if (SpawnedTargets.Num() > 0)
	{
		ExportTargetsToJson(TEXT("GroundTruth_Targets.json"));
	}

	UpdateSummaryCounts(); // ⭐️ 생성 및 바다 제거 후 최종 개수 반영

#if WITH_EDITOR
	MarkPackageDirty();
#endif

	UE_LOG(LogTemp, Log, TEXT("💾 [TargetGenActor] Finalized & Saved: %d total vehicles, %d Ground Truth targets."), SavedInstances.Num(), SpawnedTargets.Num());
}

void ATargetGenActor::RestoreFromSavedState()
{
	if (TargetElements.Num() == 0 || SavedInstances.Num() == 0)
	{
		return;
	}

	SetupHISMComponents();
	SpawnedTargets.Empty();

	for (const FSavedVehicleInstance& SavedInst : SavedInstances)
	{
		if (!TargetElements.IsValidIndex(SavedInst.ElementIndex) || !HISMComponents.IsValidIndex(SavedInst.ElementIndex))
		{
			continue;
		}

		const FTargetConfig& Config = TargetElements[SavedInst.ElementIndex];
		UHierarchicalInstancedStaticMeshComponent* HISM = HISMComponents[SavedInst.ElementIndex];

		if (IsValid(HISM) && IsValid(Config.ElementMesh))
		{
			HISM->AddInstance(SavedInst.WorldTransform, /*bWorldSpace=*/true);

			if (Config.bIsTarget)
			{
				const FBoxSphereBounds MeshBounds = Config.ElementMesh->GetBounds();
				FTargetRecord Record;
				Record.ClassId = Config.TargetClassId;
				Record.ClassName = Config.ElementName.IsEmpty() ? Config.ElementMesh->GetName() : Config.ElementName;
				Record.WorldLocation = SavedInst.WorldTransform.GetLocation();
				Record.WorldRotation = SavedInst.WorldTransform.GetRotation().Rotator();
				Record.WorldExtent = MeshBounds.BoxExtent * SavedInst.WorldTransform.GetScale3D();

				SpawnedTargets.Add(Record);
			}
		}
	}

	UpdateSummaryCounts(); // ⭐️ 에디터 재시작/복원 시 개수 반영
	UE_LOG(LogTemp, Log, TEXT("✅ [TargetGenActor] Restored %d vehicles and %d Ground Truth targets from saved state!"), SavedInstances.Num(), SpawnedTargets.Num());
}

bool ATargetGenActor::ExportTargetsToJson(const FString& FileName)
{
	FString SaveDirectory = FPaths::ProjectSavedDir() / TEXT("Datasets");
	IFileManager::Get().MakeDirectory(*SaveDirectory, true);

	FString FullPath = SaveDirectory / (FileName.IsEmpty() ? TEXT("GroundTruth_Targets.json") : FileName);

	FString JsonContent = TEXT("[\n");
	for (int32 i = 0; i < SpawnedTargets.Num(); ++i)
	{
		const FTargetRecord& Target = SpawnedTargets[i];
		JsonContent += FString::Printf(
			TEXT("  {\n    \"class_id\": %d,\n    \"class_name\": \"%s\",\n    \"location\": [%.2f, %.2f, %.2f],\n    \"rotation\": [%.2f, %.2f, %.2f],\n    \"extent\": [%.2f, %.2f, %.2f]\n  }%s\n"),
			Target.ClassId,
			*Target.ClassName,
			Target.WorldLocation.X, Target.WorldLocation.Y, Target.WorldLocation.Z,
			Target.WorldRotation.Pitch, Target.WorldRotation.Yaw, Target.WorldRotation.Roll,
			Target.WorldExtent.X, Target.WorldExtent.Y, Target.WorldExtent.Z,
			(i == SpawnedTargets.Num() - 1) ? TEXT("") : TEXT(",")
		);
	}
	JsonContent += TEXT("]\n");

	bool bSuccess = FFileHelper::SaveStringToFile(JsonContent, *FullPath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
	if (bSuccess)
	{
		UE_LOG(LogTemp, Log, TEXT("[TargetGen] Successfully exported %d targets to: %s"), SpawnedTargets.Num(), *FullPath);
	}
	else
	{
		UE_LOG(LogTemp, Error, TEXT("[TargetGen] Failed to save JSON file to: %s"), *FullPath);
	}

	return bSuccess;
}

void ATargetGenActor::UpdateSummaryCounts()
{
	TotalVehicleCount = SavedInstances.Num();
	TotalTargetCount = SpawnedTargets.Num();
	NormalVehicleCount = FMath::Max(0, TotalVehicleCount - TotalTargetCount);

	SpawnCountByElement.Empty();

	for (int32 i = 0; i < TargetElements.Num(); ++i)
	{
		const FTargetConfig& Config = TargetElements[i];
		FString Name = Config.ElementName.IsEmpty()
			? (Config.ElementMesh ? Config.ElementMesh->GetName() : FString::Printf(TEXT("Element_%d"), i))
			: Config.ElementName;

		FString Key = FString::Printf(TEXT("%s %s"), Config.bIsTarget ? TEXT("[🎯타겟]") : TEXT("[⚪일반]"), *Name);

		int32 Count = 0;
		if (HISMComponents.IsValidIndex(i) && IsValid(HISMComponents[i]))
		{
			Count = HISMComponents[i]->GetInstanceCount();
		}
		SpawnCountByElement.Add(Key, Count);
	}
}