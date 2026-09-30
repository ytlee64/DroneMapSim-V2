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

void ATargetGenActor::BeginPlay()
{
	Super::BeginPlay();

	CalculateLandscapeBounds();
}

// -----------------------------------------------------------------------------
// [���� ����]: ������ ��� Landscape/Proxy�� �����Ͽ� ���� BoundingBox�� ���
// -----------------------------------------------------------------------------
FBox ATargetGenActor::GetTotalLandscapeBounds(TArray<AActor*>& OutLandscapeActors)
{
	OutLandscapeActors.Empty();
	FBox TotalBox(ForceInit);

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

// -----------------------------------------------------------------------------
// [���� ��ǥ ��ȯ]: ���彺������ �߽��� ������ ����Ʈ���̽��Ͽ� ���� ���� ����
// -----------------------------------------------------------------------------
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

	// ���彺�������� ���� ��� �⺻��
	return FVector(0.0f, 0.0f, DesiredAlt);
}

void ATargetGenActor::ClearEnvironment()
{
	Modify();

	TArray<UHierarchicalInstancedStaticMeshComponent*> ExistingHISMs;
	GetComponents<UHierarchicalInstancedStaticMeshComponent>(ExistingHISMs);
	for (UHierarchicalInstancedStaticMeshComponent* Comp : ExistingHISMs)
	{
		if (Comp)
		{
			Comp->ClearInstances();
			Comp->DestroyComponent();
		}
	}
	HISMComponents.Empty();
	SpawnedTargets.Empty();
	UE_LOG(LogTemp, Log, TEXT("[EnvGen] Cleared all existing instances and target records."));
}

void ATargetGenActor::SetupHISMComponents()
{
	ClearEnvironment();

	if (!RootComponent)
	{
		USceneComponent* SceneRoot = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRoot"));
		RootComponent = SceneRoot;
	}
	RootComponent->SetMobility(EComponentMobility::Static);

	for (int32 i = 0; i < EnvElements.Num(); ++i)
	{
		UStaticMesh* Mesh = EnvElements[i].ElementMesh;
		if (Mesh)
		{
			FString CleanName = EnvElements[i].ElementName.IsEmpty() ? Mesh->GetName() : EnvElements[i].ElementName;
			FName CompName = *FString::Printf(TEXT("HISM_%d_%s"), i, *CleanName);

			UHierarchicalInstancedStaticMeshComponent* HISM = NewObject<UHierarchicalInstancedStaticMeshComponent>(
				this,
				UHierarchicalInstancedStaticMeshComponent::StaticClass(),
				CompName,
				RF_Transactional
			);

			if (HISM)
			{
				HISM->SetMobility(EComponentMobility::Static);
				HISM->SetStaticMesh(Mesh);
				HISM->AttachToComponent(RootComponent, FAttachmentTransformRules::KeepWorldTransform);
				HISM->RegisterComponentWithWorld(GetWorld());

				HISM->bCastDynamicShadow = true;
				HISM->bAffectDynamicIndirectLighting = true;
				HISM->InstanceStartCullDistance = 0;
				HISM->InstanceEndCullDistance = 0;

				AddInstanceComponent(HISM);
				HISMComponents.Add(HISM);
			}
		}
	}
}

void ATargetGenActor::GenerateEnvironment()
{
	UE_LOG(LogTemp, Log, TEXT("=================================================="));
	UE_LOG(LogTemp, Log, TEXT("[EnvGen] STARTING GENERATION"));
	UE_LOG(LogTemp, Log, TEXT("=================================================="));

	UWorld* World = GetWorld();
	if (!World)
	{
		UE_LOG(LogTemp, Error, TEXT("[EnvGen] Invalid World pointer"));
		return;
	}

	if (EnvElements.Num() == 0)
	{
		UE_LOG(LogTemp, Error, TEXT("[EnvGen] EnvElements list is empty"));
		return;
	}

	// 1. ���� �Լ��� ����Ͽ� ���� �ٿ�� �ڽ� ȹ��
	TArray<AActor*> AllLandscapeActors;
	FBox TotalLandscapeBounds = GetTotalLandscapeBounds(AllLandscapeActors);

	FVector MinBound;
	FVector MaxBound;

	if (TotalLandscapeBounds.IsValid && AllLandscapeActors.Num() > 0)
	{
		MinBound = TotalLandscapeBounds.Min;
		MaxBound = TotalLandscapeBounds.Max;
		UE_LOG(LogTemp, Log, TEXT("[EnvGen] Detected %d Landscape Actor proxies"), AllLandscapeActors.Num());
	}
	else
	{
		FVector ActorLoc = GetActorLocation();
		MinBound = ActorLoc - FallbackBoundsExtent;
		MaxBound = ActorLoc + FallbackBoundsExtent;
		UE_LOG(LogTemp, Warning, TEXT("[EnvGen] No Landscape found. Using fallback bounds around Actor"));
	}

	SetupHISMComponents();

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

			for (int32 i = 0; i < EnvElements.Num(); ++i)
			{
				if (HISMComponents.IsValidIndex(i) && HISMComponents[i] != nullptr && EnvElements[i].ElementMesh != nullptr)
				{
					ValidIndices.Add(i);
					TotalWeight += EnvElements[i].SpawnWeight;
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
				CurrentWeightSum += EnvElements[ValidIdx].SpawnWeight;
				if (RandomRoll <= CurrentWeightSum)
				{
					SelectedIndex = ValidIdx;
					break;
				}
			}

			const FEnvElementConfig& SelectedConfig = EnvElements[SelectedIndex];
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

			if (SelectedConfig.bIsTarget)
			{
				FEnvTargetRecord Record;
				Record.ClassId = SelectedConfig.TargetClassId;
				Record.ClassName = SelectedConfig.ElementName.IsEmpty() ? SelectedConfig.ElementMesh->GetName() : SelectedConfig.ElementName;
				Record.WorldLocation = Location;
				Record.WorldRotation = BaseRot;
				Record.WorldExtent = MeshBounds.BoxExtent * Scale;

				SpawnedTargets.Add(Record);
			}
		}
	}

	UE_LOG(LogTemp, Log, TEXT("=================================================="));
	UE_LOG(LogTemp, Log, TEXT("[EnvGen] DIAGNOSTIC REPORT"));
	UE_LOG(LogTemp, Log, TEXT("  - Total Grid Points Checked : %d"), TotalGridPoints);
	UE_LOG(LogTemp, Log, TEXT("  - Skipped (Probability Pass): %d"), FailSpawnProbability);
	UE_LOG(LogTemp, Log, TEXT("  - Total Spawned Instances   : %d"), TotalSpawned);
	UE_LOG(LogTemp, Log, TEXT("  - Recorded Target Objects   : %d"), SpawnedTargets.Num());
	UE_LOG(LogTemp, Log, TEXT("=================================================="));

	if (SpawnedTargets.Num() > 0)
	{
		ExportTargetsToJson(TEXT("GroundTruth_Targets.json"));
	}
}

bool ATargetGenActor::ExportTargetsToJson(const FString& FileName)
{
	FString SaveDirectory = FPaths::ProjectSavedDir() / TEXT("Datasets");
	IFileManager::Get().MakeDirectory(*SaveDirectory, true);

	FString FullPath = SaveDirectory / (FileName.IsEmpty() ? TEXT("GroundTruth_Targets.json") : FileName);

	FString JsonContent = TEXT("[\n");
	for (int32 i = 0; i < SpawnedTargets.Num(); ++i)
	{
		const FEnvTargetRecord& Target = SpawnedTargets[i];
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
		UE_LOG(LogTemp, Log, TEXT("[EnvGen] Successfully exported %d targets to: %s"), SpawnedTargets.Num(), *FullPath);
	}
	else
	{
		UE_LOG(LogTemp, Error, TEXT("[EnvGen] Failed to save JSON file to: %s"), *FullPath);
	}

	return bSuccess;
}