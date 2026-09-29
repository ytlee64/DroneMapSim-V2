#include "ScanPyramid.h"
#include "Components/SceneCaptureComponent2D.h"
#include "Kismet/GameplayStatics.h"

AScanPyramid::AScanPyramid()
{
    PrimaryActorTick.bCanEverTick = true;

    PrimaryActorTick.TickGroup = TG_PostPhysics;
    // 2. Line Separation 
    ProjectionMesh = CreateDefaultSubobject<UProceduralMeshComponent>(TEXT("ProjectionMesh"));
    RootComponent = ProjectionMesh;

    // 3. Line Separation 
    GroundMesh = CreateDefaultSubobject<UProceduralMeshComponent>(TEXT("GroundMesh"));
    GroundMesh->SetupAttachment(RootComponent);

    // 4. Both guide line components are completely no-collision/no-shadow locked to avoid interfering with drone flight physics!
    ProjectionMesh->SetCollisionProfileName(TEXT("NoCollision"));
    ProjectionMesh->SetGenerateOverlapEvents(false);
    ProjectionMesh->SetCastShadow(false);

    GroundMesh->SetCollisionProfileName(TEXT("NoCollision"));
    GroundMesh->SetGenerateOverlapEvents(false);
    GroundMesh->SetCastShadow(false);

    PrimaryActorTick.TickGroup = TG_PostPhysics;
}

void AScanPyramid::BeginPlay()
{
    Super::BeginPlay();

    if (!TargetDronePawn)
    {
        TargetDronePawn = UGameplayStatics::GetPlayerPawn(GetWorld(), 0);
    }
}

void AScanPyramid::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    if (TargetDronePawn)
    {
        AddTickPrerequisiteActor(TargetDronePawn);

        USceneCaptureComponent2D* DroneCaptureCam = TargetDronePawn->FindComponentByClass<USceneCaptureComponent2D>();
        if (DroneCaptureCam)
        {
            // 1. Real-time world absolute position of the camera (top vertex)
            FVector StartLocation = DroneCaptureCam->GetComponentLocation();
            FRotator CamRotation = DroneCaptureCam->GetComponentRotation();

            // To inject all calculated array coordinates as world absolute coordinates, fix the actor itself at the origin!
            SetActorLocation(FVector::ZeroVector);
            SetActorRotation(FRotator::ZeroRotator);

            // [Best Practice] Set the target absolute horizontal altitude Z plane
            float TargetZPlane = 5.0f;

            // If the drone is below the target altitude, clear both mesh sections and return
            if (StartLocation.Z <= TargetZPlane)
            {
                ProjectionMesh->ClearMeshSection(0);
                GroundMesh->ClearMeshSection(0);
                return;
            }

            // 2. Extract the camera's unique three physical axes to reflect roll and pitch components (fully include Roll and Pitch)
            FVector CamForward = CamRotation.Vector();
            FVector CamRight = FRotationMatrix(CamRotation).GetUnitAxis(EAxis::Y);
            FVector CamUp = FRotationMatrix(CamRotation).GetUnitAxis(EAxis::Z);

            // 3. [Screen-based local square vector design - typo correction complete]
            float HFactor = FMath::Tan(FMath::DegreesToRadians(WidthAngle * 0.5f));
            float VFactor = FMath::Tan(FMath::DegreesToRadians(HeightAngle * 0.5f));  

            // Pure direction vectors of the four corners extending directly in front of the camera lens
            FVector Ray_FR = (CamForward + (CamRight * HFactor) + (CamUp * VFactor)).GetSafeNormal(); // Front Right
            FVector Ray_FL = (CamForward - (CamRight * HFactor) + (CamUp * VFactor)).GetSafeNormal(); // Front Left
            FVector Ray_BL = (CamForward - (CamRight * HFactor) - (CamUp * VFactor)).GetSafeNormal(); // Back Left
            FVector Ray_BR = (CamForward + (CamRight * HFactor) - (CamUp * VFactor)).GetSafeNormal(); // Back Right
            // 4. [Absolute altitude Z=5 plane equation intersection calculation]
            float HeightDiff = TargetZPlane - StartLocation.Z;

            // Guardrail for crash prevention (exception handling when looking at the sky)
            if (Ray_FR.Z >= 0.0f) Ray_FR.Z = -0.001f;
            if (Ray_FL.Z >= 0.0f) Ray_FL.Z = -0.001f;
            if (Ray_BL.Z >= 0.0f) Ray_BL.Z = -0.001f;
            if (Ray_BR.Z >= 0.0f) Ray_BR.Z = -0.001f;

            FVector Corner_FR = StartLocation + (Ray_FR * (HeightDiff / Ray_FR.Z));
            FVector Corner_FL = StartLocation + (Ray_FL * (HeightDiff / Ray_FL.Z));
            FVector Corner_BL = StartLocation + (Ray_BL * (HeightDiff / Ray_BL.Z));
            FVector Corner_BR = StartLocation + (Ray_BR * (HeightDiff / Ray_BR.Z));

            // Perfectly lock the Z-axis height of the four corners to the plane like a magnet
            Corner_FR.Z = TargetZPlane;
            Corner_FL.Z = TargetZPlane;
            Corner_BL.Z = TargetZPlane;
            Corner_BR.Z = TargetZPlane;

            // 5. Create an array of absolute coordinates for the shared vertices
            TArray<FVector> Vertices;
            Vertices.Add(StartLocation);      // Index 0: Top vertex of the camera
            Vertices.Add(Corner_FR);          // Index 1: Front Right of the ground
            Vertices.Add(Corner_FL);          // Index 2: Front Left of the ground
            Vertices.Add(Corner_BL);          // Index 3: Back Left of the ground
            Vertices.Add(Corner_BR);          // Index 4: Back Right of the ground

            // =========================================================================
            // ⭐️ [Component Separation 1] ProjectionMesh only: Assemble the 4 side edges of the pillar!
            TArray<int32> ProjectionTriangles;
            ProjectionTriangles.Add(0); ProjectionTriangles.Add(1); ProjectionTriangles.Add(2); // Front face pillar
            ProjectionTriangles.Add(0); ProjectionTriangles.Add(2); ProjectionTriangles.Add(3); // Left face pillar
            ProjectionTriangles.Add(0); ProjectionTriangles.Add(3); ProjectionTriangles.Add(4); // Back face pillar
            ProjectionTriangles.Add(0); ProjectionTriangles.Add(4); ProjectionTriangles.Add(1); // Right face pillar

            ProjectionMesh->CreateMeshSection_LinearColor(
                0, Vertices, ProjectionTriangles, TArray<FVector>(), TArray<FVector2D>(),
                TArray<FLinearColor>(), TArray<FProcMeshTangent>(), false
            );

            // Inject the resurrected Pillar material into the pillar component in real-time!
            if (PillarWireframeMaterial)
            {
                ProjectionMesh->SetMaterial(0, PillarWireframeMaterial);
            }

            // =========================================================================
            // [Component Separation 2] GroundMesh only: Assemble the ground border rectangle + internal X-shaped lines!
            TArray<int32> GroundTriangles;
            // Assemble the horizontal plane frame of the ground (border faces)
            GroundTriangles.Add(1); GroundTriangles.Add(2); GroundTriangles.Add(3);
            GroundTriangles.Add(1); GroundTriangles.Add(3); GroundTriangles.Add(4);
            // Internal X-shaped crossing lines of the ground
            GroundTriangles.Add(0); GroundTriangles.Add(2); GroundTriangles.Add(4); // X line 1
            GroundTriangles.Add(0); GroundTriangles.Add(1); GroundTriangles.Add(3); // X line 2

            GroundMesh->CreateMeshSection_LinearColor(
                0, Vertices, GroundTriangles, TArray<FVector>(), TArray<FVector2D>(),
                TArray<FLinearColor>(), TArray<FProcMeshTangent>(), false
            );

            // Inject the resurrected Ground material into the ground component in real-time!
            if (GroundWireframeMaterial)
            {
                GroundMesh->SetMaterial(0, GroundWireframeMaterial);
            }
        }
    }
}