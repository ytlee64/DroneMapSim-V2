// DronePawn.cpp
#include "DronePawn.h"
#include "CommLink.h"
#include "TargetGenActor.h"
#include "ObserverPawn.h"
#include "ScanProjection.h"

#include "ImageUtils.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Components/CapsuleComponent.h"
#include "Components/SphereComponent.h"
#include "Components/ArrowComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/SceneCaptureComponent2D.h"
#include "Engine/TextureRenderTarget2D.h"
#include "Kismet/GameplayStatics.h"
#include "Kismet/KismetSystemLibrary.h"

// -----------------------------------------------------------------------------
// [수학 헬퍼]: 3D 월드 좌표를 카메라 뷰포트 정규화 좌표(0.0 ~ 1.0)로 정확히 투영
// -----------------------------------------------------------------------------
static bool ProjectWorldPointToScreenExact(
    const FVector& WorldPoint,
    const FVector& CamLoc,
    const FRotator& CamRot,
    float CamFOV,
    float RenderWidth,
    float RenderHeight,
    FVector2D& OutNormalizedPos)
{
    FTransform CamTransform(CamRot, CamLoc);
    FVector LocalPoint = CamTransform.InverseTransformPosition(WorldPoint);
    if (LocalPoint.X <= 1.0f) return false; // 카메라 뒤쪽은 제외

    float AspectRatio = RenderWidth / RenderHeight;
    float HalfHFOVRads = FMath::DegreesToRadians(CamFOV * 0.5f);
    float TanHalfHFOV = FMath::Tan(HalfHFOVRads);
    float TanHalfVFOV = TanHalfHFOV / AspectRatio;

    float ScreenX = (LocalPoint.Y / (LocalPoint.X * TanHalfHFOV)) * 0.5f + 0.5f;
    float ScreenY = 0.5f - (LocalPoint.Z / (LocalPoint.X * TanHalfVFOV)) * 0.5f;

    OutNormalizedPos = FVector2D(ScreenX, ScreenY);
    return true;
}

// -----------------------------------------------------------------------------
// [생성자 및 컴포넌트 셋업]
// -----------------------------------------------------------------------------
ADronePawn::ADronePawn()
{
    PrimaryActorTick.bCanEverTick = true;

    // 1. 루트 충돌체
    CollisionComp = CreateDefaultSubobject<UCapsuleComponent>(TEXT("CollisionComponent"));
    RootComponent = CollisionComp;

    // 2. 드론 시각적 본체 및 방향 표시
    DroneMeshComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("DroneMeshComponent"));
    DroneMeshComp->SetupAttachment(RootComponent);
    // 드론 메쉬 자체 충돌은 끄고, 루트인 CollisionComp가 충돌을 전담하도록 설정
    DroneMeshComp->SetCollisionEnabled(ECollisionEnabled::NoCollision);

    //DroneMeshComp = CreateDefaultSubobject<USphereComponent>(TEXT("DroneMeshComponent"));
    //DroneMeshComp->SetupAttachment(RootComponent);
    //DroneMeshComp->SetSphereRadius(50.0f);
    //DroneMeshComp->SetHiddenInGame(false);

    //DirectionConeComp = CreateDefaultSubobject<UArrowComponent>(TEXT("DirectionConeComponent"));
    //DirectionConeComp->SetupAttachment(DroneMeshComp);
    //DirectionConeComp->ArrowSize = 2.0f;
    //DirectionConeComp->SetHiddenInGame(false);

    // 3. 2축 짐벌 기구학 컴포넌트
    GimbalOuterAxisComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("GimbalOuterAxisComponent"));
    GimbalOuterAxisComp->SetupAttachment(RootComponent);

    GimbalInnerAxisComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("GimbalInnerAxisComponent"));
    GimbalInnerAxisComp->SetupAttachment(GimbalOuterAxisComp);

    // 4. 촬영용 카메라 (SceneCapture)
    DroneCameraComp = CreateDefaultSubobject<USceneCaptureComponent2D>(TEXT("DroneCameraComponent"));
    DroneCameraComp->SetupAttachment(GimbalInnerAxisComp);
    DroneCameraComp->SetRelativeRotation(FRotator::ZeroRotator);

    CurrentGimbalMode = EGimbalMode::YawOuter_PitchInner;

    // 5. 고정익 순항 비행 기본값 (15 m/s = 1500 cm/s)
    NavFlySpeed = 1500.0f;

    // 6. 명령 수신 컴포넌트 및 짐벌 연결
    CommLink = CreateDefaultSubobject<UCommLink>(TEXT("CommLink"));
    CommLink->GimbalComponentYaw = GimbalOuterAxisComp;
    CommLink->GimbalComponentPitch = GimbalInnerAxisComp;
}

void ADronePawn::BeginPlay()
{
    Super::BeginPlay();

    // 1. 타겟 관리자(TargetGenActor)에게 30m 고도의 지형 스폰 좌표를 질의하여 안착
    ATargetGenActor* TargetGen = Cast<ATargetGenActor>(UGameplayStatics::GetActorOfClass(GetWorld(), ATargetGenActor::StaticClass()));
    if (TargetGen)
    {
        const float SpawnAlt = 3000.0f; // 30m 고도
        FVector SpawnLoc = TargetGen->GetTerrainSpawnLocation(SpawnAlt);
        SetActorLocation(SpawnLoc, false, nullptr, ETeleportType::TeleportPhysics);
    }

    // 2. 드론 카메라에서 기체 본체 가리기
    if (DroneCameraComp)
    {
        DroneCameraComp->HiddenActors.Add(this);
    }

    // 3. 월드 내 모든 스캔 피라미드 카메라 뷰에서 숨김
    TArray<AActor*> FoundPyramids;
    UGameplayStatics::GetAllActorsOfClass(GetWorld(), AScanProjection::StaticClass(), FoundPyramids);
    for (AActor* PyramidActor : FoundPyramids)
    {
        DroneCameraComp->HiddenActors.AddUnique(PyramidActor);
    }
}

void ADronePawn::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    Super::EndPlay(EndPlayReason);
    UKismetSystemLibrary::ExecuteConsoleCommand(GetWorld(), TEXT("r.AntiAliasingMethod 1"));
    UKismetSystemLibrary::ExecuteConsoleCommand(GetWorld(), TEXT("r.MotionBlurQuality 0"));
}

// -----------------------------------------------------------------------------
// [비행 물리 보간 Tick: 고정익 전용 단일화]
// -----------------------------------------------------------------------------
void ADronePawn::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    // 고정익 자율 비행이 활성화되어 있지 않으면 리턴
    if (!bFixedWingFlightActive) return;

    FVector CurrentLoc = GetActorLocation();

    // [A. 미션 완료 후 Loiter 40m 선회 비행 모드]
    if (bLoiteringMode)
    {
        // 선회 각속도 = 선속도 / 반지름
        float AngularSpeedRad = (NavFlySpeed / LoiterRadius);
        LoiterCurrentAngleRad += AngularSpeedRad * DeltaTime;

        FVector TargetOrbitLoc = LoiterCenterLocation + FVector(
            FMath::Cos(LoiterCurrentAngleRad) * LoiterRadius,
            FMath::Sin(LoiterCurrentAngleRad) * LoiterRadius,
            0.0f // 고도 유지
        );

        FVector FlightDir = (TargetOrbitLoc - CurrentLoc);
        FlightDir.Normalize();

        SetActorLocation(CurrentLoc + FlightDir * NavFlySpeed * DeltaTime, true);

        // 선회 비행 시 접선 방향 회전 및 날개 뱅킹(Roll 25도)
        FRotator TargetRot = FlightDir.Rotation();
        TargetRot.Roll = 25.0f;
        SetActorRotation(FMath::RInterpTo(GetActorRotation(), TargetRot, DeltaTime, 4.0f));
    }
    // [B. 직사각형 순차 웨이포인트 비행 모드]
    else
    {
        FVector Direction = (NavTargetLocation - CurrentLoc);
        float Distance = Direction.Size();

        // 3m 이내 도달 시 정지하지 않고 즉시 다음 웨이포인트로 전환
        if (Distance <= 300.0f)
        {
            AdvanceToNextWaypoint();
        }
        else
        {
            Direction.Normalize();
            SetActorLocation(CurrentLoc + Direction * NavFlySpeed * DeltaTime, true);

            // 비행 방향 회전 및 뱅킹 각도 연동
            FRotator TargetRot = Direction.Rotation();
            TargetRot.Pitch = FMath::Clamp(TargetRot.Pitch, -20.0f, 20.0f);

            float DeltaYaw = FMath::FindDeltaAngleDegrees(GetActorRotation().Yaw, TargetRot.Yaw);
            TargetRot.Roll = FMath::Clamp(DeltaYaw * 0.8f, -30.0f, 30.0f);

            SetActorRotation(FMath::RInterpTo(GetActorRotation(), TargetRot, DeltaTime, 3.5f));
        }
    }
}

// -----------------------------------------------------------------------------
// [고정익 웨이포인트 수신 및 비행 개시]
// -----------------------------------------------------------------------------
void ADronePawn::SetFixedWingWaypoints(
    const TArray<FWaypointItemData>& Waypoints,
    float LoiterRadius_cm,
    bool bPeriodicCapture,
    float CaptureIntervalSec
)
{
    // 기존 타이머 해제
    GetWorldTimerManager().ClearTimer(PeriodicCaptureTimerHandle);

    if (Waypoints.Num() == 0)
    {
        bFixedWingFlightActive = false;
        bLoiteringMode = false;
        UE_LOG(LogTemp, Warning, TEXT("[DronePawn] Received empty waypoints list!"));
        return;
    }

    FixedWingWaypoints = Waypoints;
    CurrentWaypointIndex = 0;
    LoiterRadius = (LoiterRadius_cm > 500.0f) ? LoiterRadius_cm : 4000.0f; // 40m 안전값
    bLoiteringMode = false;
    bFixedWingFlightActive = true;

    // 첫 번째 웨이포인트 설정
    const FWaypointItemData& FirstWp = FixedWingWaypoints[0];
    NavTargetLocation = FirstWp.Location;
    NavFlySpeed = (FirstWp.Speed_cm_s > 500.0f) ? FirstWp.Speed_cm_s : 1500.0f; // 기본 15 m/s

    UE_LOG(LogTemp, Log, TEXT("[DronePawn] Fixed-Wing Mission Started: %d points, Loiter Radius: %.1f cm"),
        FixedWingWaypoints.Num(), LoiterRadius);

    // 주기적 촬영 타이머 등록
    if (bPeriodicCapture && CaptureIntervalSec > 0.1f)
    {
        GetWorldTimerManager().SetTimer(
            PeriodicCaptureTimerHandle,
            this,
            &ADronePawn::ExecuteCapture,
            CaptureIntervalSec,
            true
        );

        UE_LOG(LogTemp, Log, TEXT("[DronePawn] Periodic capture timer started (Interval: %.2f sec)"), CaptureIntervalSec);
    }
}

void ADronePawn::AdvanceToNextWaypoint()
{
    CurrentWaypointIndex++;

    // 사각형 4개 웨이포인트를 모두 완주한 경우 -> 자율비행 종료 및 매뉴얼 모드 복귀!
    if (CurrentWaypointIndex >= FixedWingWaypoints.Num())
    {
        // 1. 자율 비행 상태 비활성화
        bFixedWingFlightActive = false;
        bLoiteringMode = false;

        // 2. 미션 완료되었으므로 주기적 사진 촬영 타이머 정지
        GetWorldTimerManager().ClearTimer(PeriodicCaptureTimerHandle);

        // 3. 기체 자세 수평 안정화 (Roll 뱅킹 각도 0도로 복원)
        FRotator LevelRot = GetActorRotation();
        LevelRot.Roll = 0.0f;
        SetActorRotation(LevelRot);

        UE_LOG(LogTemp, Log, TEXT("[DronePawn] Rectangle Mission Complete! Restored to MANUAL CONTROL mode."));
        return;
    }

    // 다음 웨이포인트로 계속 진행
    const FWaypointItemData& NextWp = FixedWingWaypoints[CurrentWaypointIndex];
    NavTargetLocation = NextWp.Location;
    NavFlySpeed = (NextWp.Speed_cm_s > 500.0f) ? NextWp.Speed_cm_s : 1500.0f;

    UE_LOG(LogTemp, Log, TEXT("[DronePawn] Heading to WP #%d: (%.1f, %.1f, %.1f)"),
        CurrentWaypointIndex + 1, NavTargetLocation.X, NavTargetLocation.Y, NavTargetLocation.Z);
}

// -----------------------------------------------------------------------------
// [짐벌 조종 인터페이스]
// -----------------------------------------------------------------------------
void ADronePawn::SetGimbalOrientation(float Pitch, float Yaw)
{
    if (CurrentGimbalMode == EGimbalMode::YawOuter_PitchInner)
    {
        GimbalOuterAxisComp->SetRelativeRotation(FRotator(0.0f, Yaw, 0.0f));
        GimbalInnerAxisComp->SetRelativeRotation(FRotator(Pitch, 0.0f, 0.0f));
    }
    else
    {
        GimbalOuterAxisComp->SetRelativeRotation(FRotator(Pitch, 0.0f, 0.0f));
        GimbalInnerAxisComp->SetRelativeRotation(FRotator(0.0f, Yaw, 0.0f));
    }
}

void ADronePawn::ToggleGimbalMode()
{
    if (GimbalOuterAxisComp && GimbalInnerAxisComp)
    {
        GimbalOuterAxisComp->SetRelativeRotation(FRotator::ZeroRotator);
        GimbalInnerAxisComp->SetRelativeRotation(FRotator::ZeroRotator);
    }

    CurrentGimbalMode = (CurrentGimbalMode == EGimbalMode::YawOuter_PitchInner)
        ? EGimbalMode::PitchOuter_YawInner
        : EGimbalMode::YawOuter_PitchInner;

    if (CurrentGimbalMode == EGimbalMode::YawOuter_PitchInner)
    {
        CommLink->GimbalComponentYaw = GimbalOuterAxisComp;
        CommLink->GimbalComponentPitch = GimbalInnerAxisComp;
    }
    else
    {
        CommLink->GimbalComponentYaw = GimbalInnerAxisComp;
        CommLink->GimbalComponentPitch = GimbalOuterAxisComp;
    }
}

// -----------------------------------------------------------------------------
// [영상 캡처 및 AI 데이터셋(YOLO + CSV) 자동 생성]
// -----------------------------------------------------------------------------
void ADronePawn::ExecuteCapture()
{
    if (!DroneCameraComp || !DroneRenderTargetAsset) return;

    FString FileTimestamp = FDateTime::Now().ToString(TEXT("%Y%m%d_%H%M%S"));
    FString DirectorySavePath = FPaths::ProjectSavedDir() / TEXT("DroneCaptures");
    IFileManager::Get().MakeDirectory(*DirectorySavePath, true);

    FString ImageBaseName = FString::Printf(TEXT("IMG_%s"), *FileTimestamp);
    UE_LOG(LogTemp, Log, TEXT("[DronePawn] Capture initiated. Saving to: %s"), *ImageBaseName);

    FString FullImageFilePath = DirectorySavePath / (ImageBaseName + TEXT(".png"));
    FString LabelFileName = DirectorySavePath / (ImageBaseName + TEXT(".txt"));
    FString MetadataCSVFileName = DirectorySavePath / TEXT("Dataset_Log.csv");

    // 1. 카메라 렌더타깃 이미지 PNG 저장
    DroneCameraComp->CaptureScene();

    TUniquePtr<FArchive> FileWriter(IFileManager::Get().CreateFileWriter(*FullImageFilePath));
    if (FileWriter.IsValid())
    {
        FImageUtils::ExportRenderTarget2DAsPNG(DroneRenderTargetAsset, *FileWriter);
        FileWriter->Close();
    }

    // 2. YOLO Bounding Box 계산
    FVector CamLoc = DroneCameraComp->GetComponentLocation();
    FRotator CamRot = DroneCameraComp->GetComponentRotation();
    float CamFOV = DroneCameraComp->FOVAngle;

    int32 RenderWidth = DroneRenderTargetAsset->SizeX > 0 ? DroneRenderTargetAsset->SizeX : 1920;
    int32 RenderHeight = DroneRenderTargetAsset->SizeY > 0 ? DroneRenderTargetAsset->SizeY : 1080;

    FString YoloLabelString = TEXT("");
    int32 VisibleTargetCount = 0;

    ATargetGenActor* TargetGen = Cast<ATargetGenActor>(UGameplayStatics::GetActorOfClass(GetWorld(), ATargetGenActor::StaticClass()));
    if (TargetGen)
    {
        const TArray<FEnvTargetRecord>& TargetList = TargetGen->GetSpawnedTargets();
        for (const FEnvTargetRecord& Target : TargetList)
        {
            FTransform TargetTransform(Target.WorldRotation, Target.WorldLocation);
            FVector Extent = Target.WorldExtent;
            FVector CenterOffset(0.0f, 0.0f, Extent.Z);

            FVector LocalCorners[8] = {
                CenterOffset + FVector(Extent.X,  Extent.Y,  Extent.Z),
                CenterOffset + FVector(Extent.X,  Extent.Y, -Extent.Z),
                CenterOffset + FVector(Extent.X, -Extent.Y,  Extent.Z),
                CenterOffset + FVector(Extent.X, -Extent.Y, -Extent.Z),
                CenterOffset + FVector(-Extent.X,  Extent.Y,  Extent.Z),
                CenterOffset + FVector(-Extent.X,  Extent.Y, -Extent.Z),
                CenterOffset + FVector(-Extent.X, -Extent.Y,  Extent.Z),
                CenterOffset + FVector(-Extent.X, -Extent.Y, -Extent.Z)
            };

            float MinX = 1.0f, MaxX = 0.0f;
            float MinY = 1.0f, MaxY = 0.0f;
            int32 ValidProjectedCorners = 0;

            for (int32 c = 0; c < 8; ++c)
            {
                FVector WorldCorner = TargetTransform.TransformPosition(LocalCorners[c]);
                FVector2D NormPos;
                if (ProjectWorldPointToScreenExact(WorldCorner, CamLoc, CamRot, CamFOV,
                    static_cast<float>(RenderWidth), static_cast<float>(RenderHeight), NormPos))
                {
                    MinX = FMath::Min(MinX, NormPos.X);
                    MaxX = FMath::Max(MaxX, NormPos.X);
                    MinY = FMath::Min(MinY, NormPos.Y);
                    MaxY = FMath::Max(MaxY, NormPos.Y);
                    ValidProjectedCorners++;
                }
            }

            if (ValidProjectedCorners < 4) continue;
            if (MaxX <= 0.0f || MinX >= 1.0f || MaxY <= 0.0f || MinY >= 1.0f) continue;

            float ClampedMinX = FMath::Clamp(MinX, 0.0f, 1.0f);
            float ClampedMaxX = FMath::Clamp(MaxX, 0.0f, 1.0f);
            float ClampedMinY = FMath::Clamp(MinY, 0.0f, 1.0f);
            float ClampedMaxY = FMath::Clamp(MaxY, 0.0f, 1.0f);

            float BBoxWidth = ClampedMaxX - ClampedMinX;
            float BBoxHeight = ClampedMaxY - ClampedMinY;
            if (BBoxWidth <= 0.005f || BBoxHeight <= 0.005f) continue;

            float CenterX = ClampedMinX + (BBoxWidth * 0.5f);
            float CenterY = ClampedMinY + (BBoxHeight * 0.5f);

            YoloLabelString += FString::Printf(TEXT("%d %.6f %.6f %.6f %.6f\n"),
                Target.ClassId, CenterX, CenterY, BBoxWidth, BBoxHeight);
            VisibleTargetCount++;
        }

        FFileHelper::SaveStringToFile(YoloLabelString, *LabelFileName, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
    }

    // 3. 비행 및 짐벌 원격계측 메타데이터 CSV 파일 추가
    FVector GlobalLoc = GetActorLocation();
    FRotator GlobalRot = GetActorRotation();
    float MechOuterYaw = GimbalOuterAxisComp ? GimbalOuterAxisComp->GetRelativeRotation().Yaw : 0.0f;
    float MechInnerPitch = GimbalInnerAxisComp ? GimbalInnerAxisComp->GetRelativeRotation().Pitch : 0.0f;
    FRotator AbsGimbalRot = GimbalInnerAxisComp ? GimbalInnerAxisComp->GetComponentRotation() : FRotator::ZeroRotator;

    FString CompleteCSVRowString = TEXT("");
    if (!FPaths::FileExists(MetadataCSVFileName))
    {
        CompleteCSVRowString += TEXT("Timestamp,ImageFile,VisibleTargets,Loc_X,Loc_Y,Loc_Z,Drone_Pitch,Drone_Yaw,Drone_Roll,Mech_Outer_Yaw,Mechanical_Inner_Pitch,Abs_Gimbal_Pitch,Abs_Gimbal_Yaw,Abs_Gimbal_Roll\n");
    }

    CompleteCSVRowString += FString::Printf(TEXT("%s,%s,%d,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f\n"),
        *FileTimestamp, *(ImageBaseName + TEXT(".png")), VisibleTargetCount,
        GlobalLoc.X, GlobalLoc.Y, GlobalLoc.Z,
        GlobalRot.Pitch, GlobalRot.Yaw, GlobalRot.Roll,
        MechOuterYaw, MechInnerPitch,
        AbsGimbalRot.Pitch, AbsGimbalRot.Yaw, AbsGimbalRot.Roll);

    FFileHelper::SaveStringToFile(CompleteCSVRowString, *MetadataCSVFileName, FFileHelper::EEncodingOptions::ForceUTF8, &IFileManager::Get(), FILEWRITE_Append);
}