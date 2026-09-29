#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "Sockets.h"
#include "Networking.h"
#include "WaypointItemData.h"
#include "DroneCommandReceiver.generated.h"


class UStaticMeshComponent;
class ADronePawn;
class AObserverPawn;

UCLASS(ClassGroup = (Custom), meta = (BlueprintSpawnableComponent))
class DRONEMAPSIM_API UDroneCommandReceiver : public UActorComponent
{
	GENERATED_BODY()

public:
	UDroneCommandReceiver();

protected:
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

public:
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
	int32 ListenPort = 9000;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
	int32 TelemetryPort = 9001;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
	FString TelemetryIP = TEXT("127.0.0.1");

	// 비행 및 짐벌 조작 속도 설정
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone Controls")
	float MoveSpeed = 100.0f; // 이동 속도 (m/s, 기본 10m/s)

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone Controls")
	float RotateSpeed = 60.0f; // 기체 회전 속도 (도/초)

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone Controls")
	float GimbalSpeed = 45.0f; // 짐벌 회전 속도 (도/초)

	// 1회 캐싱할 참조 (에디터 디테일 패널에서 수동 지정 가능, 비워둘 시 자동 탐색)
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone References")
	TObjectPtr<ADronePawn> CachedDronePawn = nullptr;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Drone References")
	TObjectPtr<AObserverPawn> CachedObserverActor = nullptr;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gimbal")
	TObjectPtr<UStaticMeshComponent> GimbalComponentPitch = nullptr;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Gimbal")
	TObjectPtr<UStaticMeshComponent> GimbalComponentYaw = nullptr;

private:
	FSocket* ListenSocket = nullptr;
	FSocket* TelemetrySocket = nullptr;
	TSharedPtr<FInternetAddr> TelemetryEndpoint;

	float TelemetryTimer = 0.0f;
	uint32 SequenceNumber = 0;

	// BeginPlay에서 딱 1번만 실행되는 초기화 함수
	void InitializeReferences();
	void FindGimbalComponents(AActor* DroneActor);

	void StartListening();
	void StopListening();
	void ProcessInputKeyboard();
	void ProcessIncomingData();
	void ProcessJsonCommand(const FString& JsonString);
	void SendTelemetry();
};