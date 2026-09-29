#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "ObserverPawn.generated.h"

class UCameraComponent;
class USceneComponent;
class ADronePawn;

// =============================================================================
// 관찰자 추적 모드 열거형
// =============================================================================
UENUM(BlueprintType)
enum class EObserverTrackingMode : uint8
{
    Chase      UMETA(DisplayName = "Chase Mode"),       // 1. 드론 3인칭 근접 추적 (Attach)
    TopDown    UMETA(DisplayName = "Top-Down Mode"),    // 2. 맵 정중앙 수직 탑뷰 관제 (Detach)
    FreeRoam   UMETA(DisplayName = "Free Roam Mode")    // 3. 드론 주위에서 시작하는 자유 비행 (Detach)
};

// =============================================================================
// 관찰자 폰 액터 (AObserverPawn)
// =============================================================================
UCLASS()
class DRONEMAPSIM_API AObserverPawn : public APawn
{
    GENERATED_BODY()

public:
    AObserverPawn();

    virtual void Tick(float DeltaTime) override;

    // -------------------------------------------------------------------------
    // 1. 시점 모드 제어 인터페이스
    // -------------------------------------------------------------------------
    UFUNCTION(BlueprintCallable, Category = "Observer|Mode")
    void SetTrackingMode(EObserverTrackingMode NewMode);

    UFUNCTION(BlueprintCallable, Category = "Observer|Mode")
    void CycleNextObserverMode();

    // -------------------------------------------------------------------------
    // 2. FreeRoam 모드일 때 외부 조종 인터페이스
    // -------------------------------------------------------------------------
    UFUNCTION(BlueprintCallable, Category = "Observer|Control")
    void MoveObserver(const FVector& LocalMove, float YawRate);

    // -------------------------------------------------------------------------
    // 3. 인스펙터 설정 및 참조 변수
    // -------------------------------------------------------------------------
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Observer|Status")
    EObserverTrackingMode TrackingMode = EObserverTrackingMode::Chase;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Observer|Target")
    TObjectPtr<ADronePawn> TargetDrone;

    UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category = "Observer|Offset")
    FVector ChaseOffset = FVector(-500.0f, 0.0f, 300.0f); // 드론 뒤 5m, 위 3m

    UPROPERTY(EditDefaultsOnly, BlueprintReadWrite, Category = "Observer|Offset")
    float TopDownAltCm = 30000.0f; // 탑다운 관제 고도 (기본 300m)

protected:
    virtual void BeginPlay() override;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Observer|Components")
    TObjectPtr<USceneComponent> SceneRootComp;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Observer|Components")
    TObjectPtr<UCameraComponent> CameraComp;

private:
    // 모드별 전환 세부 로직 (캡슐화)
    void EnterChaseMode();
    void EnterTopDownMode();
    void EnterFreeRoamMode();
};