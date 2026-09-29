#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "DroneMapSimGameMode.generated.h"

class UUserWidget;
class ADronePawn;
class AObserverPawn;

// =============================================================================
// 드론 시뮬레이터 게임모드 (ADroneMapSimGameMode)
// =============================================================================
UCLASS()
class DRONEMAPSIM_API ADroneMapSimGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    ADroneMapSimGameMode();

protected:
    virtual void BeginPlay() override;

    // -------------------------------------------------------------------------
    // UI 및 미니맵 위젯 참조 변수
    // -------------------------------------------------------------------------
    // 에디터(블루프린트)에서 WBP_Minimap 위젯 클래스를 지정하는 슬롯
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "GameMode|UI")
    TSubclassOf<UUserWidget> MinimapWidgetClass;

    // 런타임에 생성된 실제 미니맵 위젯 인스턴스 (UE5 가비지 컬렉션 안전 포인터)
    UPROPERTY(Transient)
    TObjectPtr<UUserWidget> LiveMinimapWidgetInstance;
};