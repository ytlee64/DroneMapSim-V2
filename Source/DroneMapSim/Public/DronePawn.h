#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "WaypointItemData.h"
#include "DronePawn.generated.h"


class UCapsuleComponent;
class USphereComponent;
class UArrowComponent;
class UStaticMeshComponent;
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
    // 1. 고정익 자율 항법 제어 API
    // -------------------------------------------------------------------------
    UFUNCTION(BlueprintCallable, Category = "Drone|Navigation")
    void SetFixedWingWaypoints(
        const TArray<FWaypointItemData>& Waypoints,
        float LoiterRadius_cm,
        bool bPeriodicCapture,
        float CaptureIntervalSec
    );

    UFUNCTION(BlueprintCallable, Category = "Drone|Navigation")
    void AdvanceToNextWaypoint();

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
    // 4. 비행 및 짐벌 상태 변수
    // -------------------------------------------------------------------------
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone|Gimbal")
    EGimbalMode CurrentGimbalMode = EGimbalMode::YawOuter_PitchInner;

    // 고정익 항법 목표 지점 및 순항 속도 (cm/s)
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    FVector NavTargetLocation = FVector::ZeroVector;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    float NavFlySpeed = 1500.0f; // 기본 15 m/s

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

protected:
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

    // -------------------------------------------------------------------------
    // 6. 고정익 자율 비행 내부 상태
    // -------------------------------------------------------------------------
    // 웨이포인트 목록 및 현재 인덱스
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    TArray<FWaypointItemData> FixedWingWaypoints;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    int32 CurrentWaypointIndex = 0;

    // 비행 활성 및 선회(Loiter 40m) 상태
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    bool bFixedWingFlightActive = false;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    bool bLoiteringMode = false;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Drone|Autopilot")
    FVector LoiterCenterLocation = FVector::ZeroVector;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone|Autopilot")
    float LoiterRadius = 4000.0f; // 기본 40m (4000cm)

    float LoiterCurrentAngleRad = 0.0f;

    // 주기적 촬영 타이머 핸들
    FTimerHandle PeriodicCaptureTimerHandle;
};