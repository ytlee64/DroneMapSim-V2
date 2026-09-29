// Copyright Epic Games, Inc. All Rights Reserved.

#include "DroneMapSimGameMode.h"
#include "DronePawn.h"
#include "ObserverPawn.h"
#include "Blueprint/UserWidget.h"
#include "Kismet/GameplayStatics.h"

ADroneMapSimGameMode::ADroneMapSimGameMode()
{
    // 수동 배치된 월드 인스턴스들을 직접 빙의/주시하므로 자동 스폰은 비활성화
    DefaultPawnClass = nullptr;
}

void ADroneMapSimGameMode::BeginPlay()
{
    Super::BeginPlay();

    APlayerController* PC = UGameplayStatics::GetPlayerController(GetWorld(), 0);
    if (!PC)
    {
        UE_LOG(LogTemp, Error, TEXT("[GameMode] PlayerController(0) not found!"));
        return;
    }

    // -------------------------------------------------------------------------
    // 1. 월드에 배치된 실제 드론 및 관찰자 폰 인스턴스 탐색
    // -------------------------------------------------------------------------
    ADronePawn* FoundDrone = Cast<ADronePawn>(UGameplayStatics::GetActorOfClass(GetWorld(), ADronePawn::StaticClass()));
    AObserverPawn* FoundObserver = Cast<AObserverPawn>(UGameplayStatics::GetActorOfClass(GetWorld(), AObserverPawn::StaticClass()));

    // -------------------------------------------------------------------------
    // 2. [조종권 빙의]: 키보드 입력 영혼은 월드의 드론에 연결!
    // -------------------------------------------------------------------------
    if (FoundDrone)
    {
        PC->Possess(FoundDrone);
        UE_LOG(LogTemp, Display, TEXT("[GameMode] SUCCESS: Player possessed the World DronePawn."));
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("[GameMode] CRITICAL: ADronePawn instance is MISSING in this level!"));
    }

    // -------------------------------------------------------------------------
    // 3. [시점 고정]: 화면 렌더링 시선은 하늘의 관찰자 카메라로 전환!
    // -------------------------------------------------------------------------
    if (FoundObserver)
    {
        PC->SetViewTarget(FoundObserver);
        UE_LOG(LogTemp, Display, TEXT("[GameMode] SUCCESS: Initial ViewTarget locked to ObserverPawn."));
    }
    else
    {
        UE_LOG(LogTemp, Warning, TEXT("[GameMode] AObserverPawn instance is MISSING in this level!"));
    }

    // -------------------------------------------------------------------------
    // 4. 미니맵 HUD 위젯 생성 및 뷰포트 등록
    // -------------------------------------------------------------------------
    if (MinimapWidgetClass)
    {
        LiveMinimapWidgetInstance = CreateWidget<UUserWidget>(PC, MinimapWidgetClass);
        if (LiveMinimapWidgetInstance)
        {
            LiveMinimapWidgetInstance->AddToViewport();
            UE_LOG(LogTemp, Log, TEXT("[GameMode] Minimap UI successfully added to viewport."));
        }
    }
}