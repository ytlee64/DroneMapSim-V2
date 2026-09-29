#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "ProceduralMeshComponent.h" 
#include "ScanPyramid.generated.h"

UCLASS()
class DRONEMAPSIM_API AScanPyramid : public AActor
{
    GENERATED_BODY()

public:
    AScanPyramid();

protected:
    virtual void BeginPlay() override;

public:
    virtual void Tick(float DeltaTime) override;

    
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Target")
    TObjectPtr<APawn> TargetDronePawn;

    // [Core Control Numbers for Program] The size of the pyramid is governed by code numbers.
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Parameters")
    float ScanDistance = 1000.0f; // Pyramid vertical length (cm)

    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Parameters")
    float WidthAngle = 60.0f;     // Pyramid horizontal spread angle

    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Parameters")
    float HeightAngle = 45.0f;    // Pyramid vertical spread angle

protected:
    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Visual", meta = (AllowPrivateAccess = "true"))
    TObjectPtr<UProceduralMeshComponent> ProjectionMesh;

    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Visual", meta = (AllowPrivateAccess = "true"))
    TObjectPtr<UProceduralMeshComponent> GroundMesh;

    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Visual")
    TObjectPtr<UMaterialInterface> PillarWireframeMaterial;

    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Scan | Visual")
    TObjectPtr<UMaterialInterface> GroundWireframeMaterial;
};