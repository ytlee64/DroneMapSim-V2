// DronePawn.cpp
#include "DronePawn.h"
#include "CommLink.h"
#include "TargetGenActor.h"
#include "ObserverPawn.h"
#include "ScanProjection.h"
#include "NavFixedWing.h" // 순수 C++ 고정익 항법 엔진
#include "DroneMathUtil.h"

#include "ImageUtils.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Components/SceneCaptureComponent2D.h"
#include "Engine/TextureRenderTarget2D.h"
#include "Kismet/GameplayStatics.h"
#include "Kismet/KismetSystemLibrary.h"

// -----------------------------------------------------------------------------
// [생성자 및 컴포넌트 셋업]
// -----------------------------------------------------------------------------
ADronePawn::ADronePawn()
{
    PrimaryActorTick.bCanEverTick = true;

    // 1. 루트 충돌체
    CollisionComp = CreateDefaultSubobject<UCapsuleComponent>(TEXT("CollisionComponent"));
    RootComponent = CollisionComp;

    // 2. 드론 시각적 본체
    DroneMeshComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("DroneMeshComponent"));
    DroneMeshComp->SetupAttachment(RootComponent);
    DroneMeshComp->SetCollisionEnabled(ECollisionEnabled::NoCollision);

    // 3. 2축 짐벌 기구학 컴포넌트
    GimbalOuterAxisComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("GimbalOuterAxisComponent"));
    GimbalOuterAxisComp->SetupAttachment(RootComponent);

    GimbalInnerAxisComp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("GimbalInnerAxisComponent"));
    GimbalInnerAxisComp->SetupAttachment(GimbalOuterAxisComp);

    // 4. 촬영용 카메라 (SceneCapture)
    DroneCameraComponent = CreateDefaultSubobject<USceneCaptureComponent2D>(TEXT("DroneCameraComponent"));
    DroneCameraComponent->SetupAttachment(GimbalInnerAxisComp);
    DroneCameraComponent->SetRelativeRotation(FRotator::ZeroRotator);

    CurrentGimbalMode = EGimbalMode::YawOuter_PitchInner;

    // 5. 명령 수신 컴포넌트 및 짐벌 연결
    CommLink = CreateDefaultSubobject<UCommLink>(TEXT("CommLink"));
    CommLink->GimbalComponentYaw = GimbalOuterAxisComp;
    CommLink->GimbalComponentPitch = GimbalInnerAxisComp;

    // 6. 순수 C++ 고정익 항법 엔진 장착!
    NavEngine = MakeUnique<FNavFixedWing>();
}

void ADronePawn::BeginPlay()
{
    Super::BeginPlay();

    // 1. 타겟 관리자에게 30m 고도의 지형 스폰 좌표 질의 후 안착
    ATargetGenActor* TargetGen = Cast<ATargetGenActor>(UGameplayStatics::GetActorOfClass(GetWorld(), ATargetGenActor::StaticClass()));
    if (TargetGen)
    {
        const float SpawnAlt = 3000.0f; // 30m 고도
        FVector SpawnLoc = TargetGen->GetTerrainSpawnLocation(SpawnAlt);
        SetActorLocation(SpawnLoc, false, nullptr, ETeleportType::TeleportPhysics);
    }

    // 2. 드론 카메라에서 본체 가리기
    if (DroneCameraComponent)
    {
        DroneCameraComponent->HiddenActors.Add(this);
    }

    // 3. 월드 내 모든 스캔 사영 카메라 뷰에서 숨김
    TArray<AActor*> FoundProjection;
    UGameplayStatics::GetAllActorsOfClass(GetWorld(), AScanProjection::StaticClass(), FoundProjection);
    for (AActor* ProjectionActor : FoundProjection)
    {
        DroneCameraComponent->HiddenActors.AddUnique(ProjectionActor);
    }

    // 1. [핵심] 톤매핑과 포스트 프로세스가 모두 적용된 최종 색상(LDR)으로 캡처
    DroneCameraComponent->CaptureSource = ESceneCaptureSource::SCS_FinalColorLDR;

    // 2. [핵심] 자동 노출(Eye Adaptation) 활성화
    DroneCameraComponent->PostProcessSettings.bOverride_AutoExposureMethod = true;
    DroneCameraComponent->PostProcessSettings.AutoExposureMethod = EAutoExposureMethod::AEM_Histogram;

    // 3. 노출 보정 (밝기 강제 증가: 1.0 ~ 2.5 사이로 원하는 만큼 조절)
    DroneCameraComponent->PostProcessSettings.bOverride_AutoExposureBias = true;
    DroneCameraComponent->PostProcessSettings.AutoExposureBias = 1.5f; // 숫자가 클수록 화면이 밝아짐

    // 4. 최소/최대 밝기 폭을 넓혀서 그림자 속도 밝게 보이도록 설정
    DroneCameraComponent->PostProcessSettings.bOverride_AutoExposureMinBrightness = true;
    DroneCameraComponent->PostProcessSettings.bOverride_AutoExposureMaxBrightness = true;
    DroneCameraComponent->PostProcessSettings.AutoExposureMinBrightness = 0.5f;
    DroneCameraComponent->PostProcessSettings.AutoExposureMaxBrightness = 3.0f;
}

void ADronePawn::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    Super::EndPlay(EndPlayReason);
    UKismetSystemLibrary::ExecuteConsoleCommand(GetWorld(), TEXT("r.AntiAliasingMethod 1"));
    UKismetSystemLibrary::ExecuteConsoleCommand(GetWorld(), TEXT("r.MotionBlurQuality 0"));
}

// -----------------------------------------------------------------------------
// [비행 물리 보간 Tick: NavEngine에 위임]
// -----------------------------------------------------------------------------
void ADronePawn::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    if (NavEngine)
    {
        FVector NextLoc;
        FRotator NextRot;
        NavEngine->Step(GetActorLocation(), GetActorRotation(), DeltaTime, NextLoc, NextRot);
        SetActorLocation(NextLoc, true);
        SetActorRotation(NextRot);
    }
}

// -----------------------------------------------------------------------------
// [수동 조종 인터페이스: CommLink에서 호출]
// -----------------------------------------------------------------------------
void ADronePawn::ApplyManualControl(float Throttle, float Roll, float Pitch, float Yaw)
{
    if (NavEngine)
    {
        NavEngine->SetManualInput(Throttle, Roll, Pitch, Yaw);
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
    if (!DroneCameraComponent || !DroneRenderTargetAsset) return;

    FString FileTimestamp = FDateTime::Now().ToString(TEXT("%Y%m%d_%H%M%S"));
    FString DirectorySavePath = FPaths::ProjectSavedDir() / TEXT("DroneCaptures");
    IFileManager::Get().MakeDirectory(*DirectorySavePath, true);

    FString ImageBaseName = FString::Printf(TEXT("IMG_%s"), *FileTimestamp);
    UE_LOG(LogTemp, Log, TEXT("[DronePawn] Capture initiated. Saving to: %s"), *ImageBaseName);

    FString FullImageFilePath = DirectorySavePath / (ImageBaseName + TEXT(".png"));
    FString LabelFileName = DirectorySavePath / (ImageBaseName + TEXT(".txt"));
    FString MetadataCSVFileName = DirectorySavePath / TEXT("Dataset_Log.csv");

    // 1. 카메라 렌더타깃 이미지 PNG 저장
    DroneCameraComponent->CaptureScene();

    TUniquePtr<FArchive> FileWriter(IFileManager::Get().CreateFileWriter(*FullImageFilePath));
    if (FileWriter.IsValid())
    {
        FImageUtils::ExportRenderTarget2DAsPNG(DroneRenderTargetAsset, *FileWriter);
        FileWriter->Close();
    }

    // 2. YOLO Bounding Box 계산
    FVector CamLoc = DroneCameraComponent->GetComponentLocation();
    FRotator CamRot = DroneCameraComponent->GetComponentRotation();
    float CamFOV = DroneCameraComponent->FOVAngle;

    int32 RenderWidth = DroneRenderTargetAsset->SizeX > 0 ? DroneRenderTargetAsset->SizeX : 1920;
    int32 RenderHeight = DroneRenderTargetAsset->SizeY > 0 ? DroneRenderTargetAsset->SizeY : 1080;

    FString YoloLabelString = TEXT("");
    int32 VisibleTargetCount = 0;

    ATargetGenActor* TargetGen = Cast<ATargetGenActor>(UGameplayStatics::GetActorOfClass(GetWorld(), ATargetGenActor::StaticClass()));
    if (TargetGen)
    {
        const TArray<FTargetRecord>& TargetList = TargetGen->GetSpawnedTargets();
        for (const FTargetRecord& Target : TargetList)
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
                if (DroneMathUtil::ProjectWorldPointToScreenExact(WorldCorner, CamLoc, CamRot, CamFOV,
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