#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "AirframeType.h"
#include "NavBase.h"
#include "DronePawn.generated.h"

class UCapsuleComponent;
class UStaticMeshComponent;
class UArrowComponent;
class USceneCaptureComponent2D;
class UTextureRenderTarget2D;
class UCommLink;

// =============================================================================
// 2축 짐벌 기구학 구조 모드 열거형
// =============================================================================
UENUM(BlueprintType)
enum class EGimbalMode : uint8
{
    YawOuter_PitchInner    UMETA(DisplayName = "Yaw (Outer) / Pitch (Inner)"),
    PitchOuter_YawInner    UMETA(DisplayName = "Pitch (Outer) / Yaw (Inner)")
};

// =============================================================================
// 고정익 드론 비행체 폰 (ADronePawn)
// =============================================================================
UCLASS()
class DRONEMAPSIM_API ADronePawn : public APawn
{
    GENERATED_BODY()

public:
    ADronePawn();

    virtual void Tick(float DeltaTime) override;

    // -------------------------------------------------------------------------
    // 1. 수동 조종 API (CommLink 및 GCS 패킷 수신 시 호출)
    // -------------------------------------------------------------------------
    UFUNCTION(BlueprintCallable, Category = "Drone|Flight")
    void ApplyManualControl(float Throttle, float Roll, float Pitch, float Yaw);

    // -------------------------------------------------------------------------
    // 2. 짐벌 제어 API
    // -------------------------------------------------------------------------
    UFUNCTION(BlueprintCallable, Category = "Drone|Gimbal")
    void SetGimbalOrientation(float Pitch, float Yaw);

    UFUNCTION(BlueprintCallable, Category = "Drone|Gimbal")
    void ToggleGimbalMode();

    // -------------------------------------------------------------------------
    // 3. 센서 촬영 및 AI 라벨링 데이터셋 자동 생성
    // -------------------------------------------------------------------------
    UFUNCTION(BlueprintCallable, Category = "Drone|Sensor")
    void ExecuteCapture();

    // -------------------------------------------------------------------------
    // 4. 기체 타입 및 항법 엔진 접근자
    // -------------------------------------------------------------------------
    EAirframeType GetAirframeType() const { return AirframeType; }
    void SetAirframeType(EAirframeType NewType) { AirframeType = NewType; }

    INavBase* GetNavEngine() const { return NavEngine.Get(); }

    // -------------------------------------------------------------------------
    // 5. 컴포넌트 구조
    // -------------------------------------------------------------------------
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<UCapsuleComponent> CollisionComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<UStaticMeshComponent> DroneMeshComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<UArrowComponent> DirectionConeComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<UStaticMeshComponent> GimbalOuterAxisComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<UStaticMeshComponent> GimbalInnerAxisComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<USceneCaptureComponent2D> DroneCameraComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Components")
    TObjectPtr<UCommLink> CommLink;

    // 촬영 렌더타깃 에셋
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone|Camera")
    TObjectPtr<UTextureRenderTarget2D> DroneRenderTargetAsset;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone|Gimbal")
    EGimbalMode CurrentGimbalMode = EGimbalMode::YawOuter_PitchInner;

protected:
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

    // 기체 타입 (기본: 고정익)
    EAirframeType AirframeType = EAirframeType::FixedWing;

    // 순수 C++ 항법 엔진 (다형성 인터페이스)
    TUniquePtr<INavBase> NavEngine;
};