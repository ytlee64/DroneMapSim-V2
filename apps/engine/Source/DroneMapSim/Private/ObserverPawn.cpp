#include "ObserverPawn.h"
#include "Camera/CameraComponent.h"
#include "Components/SceneComponent.h"
#include "Kismet/KismetMathLibrary.h"
#include "Kismet/GameplayStatics.h"
#include "DronePawn.h"

AObserverPawn::AObserverPawn()
{
    PrimaryActorTick.bCanEverTick = true;
    PrimaryActorTick.TickGroup = TG_PostUpdateWork;

    SceneRootComp = CreateDefaultSubobject<USceneComponent>(TEXT("SceneRootComp"));
    RootComponent = SceneRootComp;

    CameraComp = CreateDefaultSubobject<UCameraComponent>(TEXT("ObserverCamera"));
    CameraComp->SetupAttachment(RootComponent);
    CameraComp->SetFieldOfView(90.0f);
    CameraComp->bUsePawnControlRotation = false; // 카메라가 액터 회전만 따름

    // 옵저버는 순수 관전용, 물리적 충돌이 전혀 없어야 함 (드론/지형과 부딪혀 못 따라가는 현상 방지)
    SetActorEnableCollision(false);
}

void AObserverPawn::BeginPlay()
{
    Super::BeginPlay();

    // 1. 카메라 잔상 및 포커스 번짐 제거
    FPostProcessSettings& PPS = CameraComp->PostProcessSettings;
    PPS.bOverride_MotionBlurAmount = true;
    PPS.MotionBlurAmount = 0.0f;
    PPS.bOverride_DepthOfFieldFocalDistance = true;
    PPS.DepthOfFieldFocalDistance = 0.0f;

    // 2. 대상 드론 탐색 및 캐싱
    if (!TargetDrone)
    {
        TargetDrone = Cast<ADronePawn>(UGameplayStatics::GetActorOfClass(GetWorld(), ADronePawn::StaticClass()));
    }

    // 3. 드론 틱 종속성 등록 후 기본 CHASE 모드로 결합
    if (TargetDrone)
    {
        AddTickPrerequisiteActor(TargetDrone);
        SetTrackingMode(EObserverTrackingMode::Chase);
    }
    else
    {
        UE_LOG(LogTemp, Warning, TEXT("[ObserverPawn] TargetDrone not found in BeginPlay!"));
        SetTrackingMode(EObserverTrackingMode::FreeRoam);
    }
}

void AObserverPawn::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);
}

// -----------------------------------------------------------------------------
// [모드별 전환 처리기]
// -----------------------------------------------------------------------------
void AObserverPawn::EnterChaseMode()
{
    if (!TargetDrone)
    {
        TargetDrone = Cast<ADronePawn>(UGameplayStatics::GetActorOfClass(GetWorld(), ADronePawn::StaticClass()));
        if (!TargetDrone) return;
    }

    // 드론에 물리적 결합 (부모-자식 Attach)
    AttachToActor(TargetDrone, FAttachmentTransformRules::KeepRelativeTransform);

    // 드론 기준 상대 위치 배치 (뒤로 3.5m, 위로 1.5m)
    SetActorRelativeLocation(ChaseOffset);

    // 드론 중심(0, 0, 0)을 정면으로 주시하도록 회전 정렬
    FRotator LookRot = UKismetMathLibrary::FindLookAtRotation(ChaseOffset, FVector::ZeroVector);
    SetActorRelativeRotation(LookRot);

    if (CameraComp)
    {
        CameraComp->SetRelativeRotation(FRotator::ZeroRotator);
    }

    UE_LOG(LogTemp, Log, TEXT("[ObserverPawn] CHASE MODE: Hard-Attached to Drone"));
}

void AObserverPawn::EnterTopDownMode()
{
    // 드론과의 부착 해제 (독립된 Top-Down 관제 카메라로 전환)
    DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);

    FVector TopDownPos = FVector(0.0f, 0.0f, TopDownAltCm);
    SetActorLocation(TopDownPos);

    // 바닥 수직 직하방(-90도) 주시
    FRotator NadirRot(-90.0f, 0.0f, 0.0f);
    SetActorRotation(NadirRot);

    if (CameraComp)
    {
        CameraComp->SetRelativeRotation(FRotator::ZeroRotator);
    }

    UE_LOG(LogTemp, Log, TEXT("[ObserverPawn] TOP DOWN MODE: Centered at (%.1f, %.1f) Alt: %.1fm"),
        TopDownPos.X, TopDownPos.Y, TopDownPos.Z * 0.01f);
}

void AObserverPawn::EnterFreeRoamMode()
{
    DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);

    // ⭐ TopDown(구름 위 5km) 등에서 전환될 때, 드론 주변의 편안한 자유 비행 구도로 자동 재배치!
    if (TargetDrone)
    {
        // 드론 뒤 20m (-2000cm), 위 10m (+1000cm)
        FVector StartPos = TargetDrone->GetActorLocation()
            - TargetDrone->GetActorForwardVector() * 2000.0f
            + FVector(0.0f, 0.0f, 1000.0f);

        SetActorLocation(StartPos);

        // 드론을 내려다보며 시선 고정 및 수평(Roll=0) 정렬
        FRotator LookAtDrone = UKismetMathLibrary::FindLookAtRotation(StartPos, TargetDrone->GetActorLocation());
        LookAtDrone.Roll = 0.0f;
        SetActorRotation(LookAtDrone);

        if (CameraComp)
        {
            CameraComp->SetRelativeRotation(FRotator::ZeroRotator);
        }
    }

    UE_LOG(LogTemp, Log, TEXT("[ObserverPawn] FREE ROAM MODE: Detached & Positioned behind drone"));
}

void AObserverPawn::SetTrackingMode(EObserverTrackingMode NewMode)
{
    TrackingMode = NewMode;

    switch (TrackingMode)
    {
    case EObserverTrackingMode::Chase:
        EnterChaseMode();
        break;

    case EObserverTrackingMode::TopDown:
        EnterTopDownMode();
        break;

    case EObserverTrackingMode::FreeRoam:
        EnterFreeRoamMode();
        break;
    }
}

void AObserverPawn::CycleNextObserverMode()
{
    switch (TrackingMode)
    {
    case EObserverTrackingMode::Chase:
        SetTrackingMode(EObserverTrackingMode::TopDown);
        break;

    case EObserverTrackingMode::TopDown:
        SetTrackingMode(EObserverTrackingMode::FreeRoam);
        break;

    case EObserverTrackingMode::FreeRoam:
        SetTrackingMode(EObserverTrackingMode::Chase);
        break;
    }
}

void AObserverPawn::MoveObserver(const FVector& LocalMove, float YawRate)
{
    if (TrackingMode != EObserverTrackingMode::FreeRoam)
    {
        SetTrackingMode(EObserverTrackingMode::FreeRoam);
    }

    float DeltaTime = GetWorld()->GetDeltaSeconds();
    float MoveSpeed = 1200.0f; // 관찰자 비행 속도 (cm/s)
    float RotSpeed = 90.0f;   // 관찰자 회전 속도 (deg/s)

    // 관찰자가 바라보는 방향 기준 월드 벡터 계산
    FVector WorldMove = GetActorForwardVector() * LocalMove.X +
        GetActorRightVector() * LocalMove.Y +
        FVector::UpVector * LocalMove.Z;

    AddActorWorldOffset(WorldMove * MoveSpeed * DeltaTime, true);

    if (FMath::Abs(YawRate) > 0.01f)
    {
        AddActorLocalRotation(FRotator(0.0f, YawRate * RotSpeed * DeltaTime, 0.0f));
    }
}