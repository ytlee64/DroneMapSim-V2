#include "CommLink.h"
#include "Engine/World.h"
#include "Engine/Engine.h"
#include "GameFramework/Actor.h"
#include "GameFramework/Pawn.h"
#include "GameFramework/PlayerController.h"
#include "Components/StaticMeshComponent.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "Common/UdpSocketBuilder.h"
#include "Kismet/GameplayStatics.h"
#include "DronePawn.h"
#include "ObserverPawn.h"

UCommLink::UCommLink()
{
	PrimaryComponentTick.bCanEverTick = true;
}

// -------------------------------------------------------------
// [핵심] BeginPlay에서 딱 1번만 드론, 옵저버, 짐벌을 찾아 캐싱
// -------------------------------------------------------------
void UCommLink::InitializeReferences()
{
	UWorld* World = GetWorld();
	if (!World) return;

	// 1. 드론 폰(ADronePawn) 확정 캐싱
	CachedDronePawn = Cast<ADronePawn>(GetOwner());
	if (!CachedDronePawn)
	{
		CachedDronePawn = Cast<ADronePawn>(UGameplayStatics::GetActorOfClass(World, ADronePawn::StaticClass()));
	}
	FindGimbalComponents(CachedDronePawn);

	// 2. 옵저버 폰(AObserverPawn) 확정 캐싱
	CachedObserverActor = Cast<AObserverPawn>(UGameplayStatics::GetActorOfClass(World, AObserverPawn::StaticClass()));

	// 3. 짐벌 초기 각도 설정: Pitch -90도(수직 하방), Yaw 0도
	if (GimbalComponentPitch)
	{
		GimbalComponentPitch->SetRelativeRotation(FRotator(-90.0f, 0.0f, 0.0f));
	}
	if (GimbalComponentYaw)
	{
		GimbalComponentYaw->SetRelativeRotation(FRotator(0.0f, 0.0f, 0.0f));
	}

	UE_LOG(LogTemp, Log, TEXT("[드론] %s | [옵저버] %s 캐싱 완료"),
		CachedDronePawn ? *CachedDronePawn->GetName() : TEXT("None"),
		CachedObserverActor ? *CachedObserverActor->GetName() : TEXT("None"));
}

void UCommLink::FindGimbalComponents(AActor* DroneActor)
{
	if (!DroneActor) return;

	TArray<UStaticMeshComponent*> MeshComps;
	DroneActor->GetComponents<UStaticMeshComponent>(MeshComps);

	for (UStaticMeshComponent* Comp : MeshComps)
	{
		if (Comp->ComponentHasTag(TEXT("GimbalPitch")) || Comp->GetName().Contains(TEXT("Pitch")))
		{
			GimbalComponentPitch = Comp;
		}
		else if (Comp->ComponentHasTag(TEXT("GimbalYaw")) || Comp->GetName().Contains(TEXT("Yaw")))
		{
			GimbalComponentYaw = Comp;
		}
	}
}

void UCommLink::BeginPlay()
{
	Super::BeginPlay();

	InitializeReferences();

	// 텔레메트리 송신 주소 세팅 (127.0.0.1:9001)
	FIPv4Address TargetIP;
	FIPv4Address::Parse(TelemetryIP, TargetIP);
	TelemetryEndpoint = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
	TelemetryEndpoint->SetIp(TargetIP.Value);
	TelemetryEndpoint->SetPort(TelemetryPort);

	StartListening();
}

void UCommLink::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	StopListening();
	Super::EndPlay(EndPlayReason);
}

void UCommLink::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// 1. 키보드 입력 처리
	ProcessInputKeyboard();

	// 2. UDP 명령 패킷 처리
	ProcessIncomingData();

	// 3. 10Hz 텔레메트리 주기 전송
	TelemetryTimer += DeltaTime;
	if (TelemetryTimer >= 0.1f)
	{
		TelemetryTimer = 0.0f;
		SendTelemetry();
		CachedDronePawn->ExecuteCapture();
	}
}

void UCommLink::StartListening()
{
	FIPv4Address Addr;
	FIPv4Address::Parse(TEXT("0.0.0.0"), Addr);
	FIPv4Endpoint Endpoint(Addr, ListenPort);

	ListenSocket = FUdpSocketBuilder(TEXT("DroneCommandListenSocket"))
		.AsNonBlocking()
		.AsReusable()
		.BoundToEndpoint(Endpoint)
		.WithReceiveBufferSize(64 * 1024);

	TelemetrySocket = FUdpSocketBuilder(TEXT("DroneTelemetrySocket"))
		.AsNonBlocking()
		.AsReusable()
		.WithSendBufferSize(64 * 1024);

	if (ListenSocket)
	{
		UE_LOG(LogTemp, Log, TEXT("[UCommLink] UDP 명령 수신 대기 (포트: %d)"), ListenPort);
	}
}

void UCommLink::StopListening()
{
	if (ListenSocket)
	{
		ListenSocket->Close();
		ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(ListenSocket);
		ListenSocket = nullptr;
	}

	if (TelemetrySocket)
	{
		TelemetrySocket->Close();
		ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(TelemetrySocket);
		TelemetrySocket = nullptr;
	}
}

// -------------------------------------------------------------
// 1. 키보드 입력 -> JSON 명령으로 변환 후 ProcessJsonCommand 호출
// -------------------------------------------------------------
void UCommLink::ProcessInputKeyboard()
{
	APlayerController* PC = GetWorld()->GetFirstPlayerController();
	if (!PC) return;

	// 1. 단발성 토글/실행 키 (Space: 캡처, O: 옵저버 모드)
	if (PC->WasInputKeyJustPressed(EKeys::SpaceBar))
	{
		ProcessJsonCommand(TEXT("{\"id\":\"CAPTURE\"}"));
	}

	if (PC->WasInputKeyJustPressed(EKeys::O))
	{
		ProcessJsonCommand(TEXT("{\"id\":\"OBSERVER\"}"));
	}

	// 2. 비행 제어 키 (W/S: 스로틀 가감속, A/D: 좌우 뱅크 롤, E/Q: 기수 피치, C/Z: 러더 요)
	float Forward = 0.0f;
	float Right = 0.0f;
	float Pitch = 0.0f;
	float Yaw = 0.0f;

	if (PC->IsInputKeyDown(EKeys::W)) Forward += 1.0f;
	if (PC->IsInputKeyDown(EKeys::S)) Forward -= 1.0f;
	if (PC->IsInputKeyDown(EKeys::D)) Right += 1.0f;
	if (PC->IsInputKeyDown(EKeys::A)) Right -= 1.0f;
	if (PC->IsInputKeyDown(EKeys::E)) Pitch += 1.0f;
	if (PC->IsInputKeyDown(EKeys::Q)) Pitch -= 1.0f;
	if (PC->IsInputKeyDown(EKeys::C)) Yaw += 1.0f;
	if (PC->IsInputKeyDown(EKeys::Z)) Yaw -= 1.0f;

	// 비행 키가 눌렸거나, 뗐을 때(중립 복귀) 둘 다 JSON으로 생성하여 전달!
	static float LastFwd = 0.0f, LastRight = 0.0f, LastPitch = 0.0f, LastYaw = 0.0f;
	if (Forward != 0.0f || Right != 0.0f || Pitch != 0.0f || Yaw != 0.0f ||
		LastFwd != 0.0f || LastRight != 0.0f || LastPitch != 0.0f || LastYaw != 0.0f)
	{
		FString FlightJson = FString::Printf(
			TEXT("{\"id\":\"MOVE\",\"forward\":%.2f,\"right\":%.2f,\"pitch\":%.2f,\"yaw\":%.2f}"),
			Forward, Right, Pitch, Yaw
		);
		ProcessJsonCommand(FlightJson);

		LastFwd = Forward;
		LastRight = Right;
		LastPitch = Pitch;
		LastYaw = Yaw;
	}

	// 3. 짐벌 제어 키 (방향키: 상/하/좌/우)
	int GimbalUp = 0;
	int GimbalRight = 0;

	if (PC->WasInputKeyJustPressed(EKeys::Up))    GimbalUp += 5;
	if (PC->WasInputKeyJustPressed(EKeys::Down))  GimbalUp -= 5;
	if (PC->WasInputKeyJustPressed(EKeys::Right)) GimbalRight += 5;
	if (PC->WasInputKeyJustPressed(EKeys::Left))  GimbalRight -= 5;
	
	if (GimbalUp != 0 || GimbalRight != 0)
	{
		FString GimbalJson = FString::Printf(
			TEXT("{\"id\":\"GIMBAL\",\"up\":%d,\"right\":%d}"),
			GimbalUp, GimbalRight
		);
		ProcessJsonCommand(GimbalJson);
	}

	if (PC->WasInputKeyJustPressed(EKeys::One)) {
		if (CachedDronePawn)
		{
			int enable = CachedDronePawn->GetNavEngine()->IsAutoNav();

			enable = (enable + 1) % 2;
			FString Json = FString::Printf(
				TEXT("{\"id\":\"AUTONAV\",\"enable\":%d}"), enable);

			ProcessJsonCommand(Json);
		}
	}
}

// -------------------------------------------------------------
// 2. UDP 수신 패킷 처리
// -------------------------------------------------------------
void UCommLink::ProcessIncomingData()
{
	if (!ListenSocket) return;

	uint32 PendingDataSize = 0;
	while (ListenSocket->HasPendingData(PendingDataSize))
	{
		TArray<uint8> ReceivedData;
		ReceivedData.SetNumUninitialized(FMath::Min(PendingDataSize, 65535u));

		int32 BytesRead = 0;
		TSharedRef<FInternetAddr> Sender = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();

		if (ListenSocket->RecvFrom(ReceivedData.GetData(), ReceivedData.Num(), BytesRead, *Sender))
		{
			if (BytesRead > 0)
			{
				FString JsonString = FString(UTF8_TO_TCHAR(reinterpret_cast<const char*>(ReceivedData.GetData())));
				JsonString.LeftInline(BytesRead);
				ProcessJsonCommand(JsonString);
			}
		}
	}
}

// -------------------------------------------------------------
// 3. [단일 명령 처리 센터] 모든 명령(UDP + 키보드) 집행
// -------------------------------------------------------------
void UCommLink::ProcessJsonCommand(const FString& JsonString)
{
	TSharedPtr<FJsonObject> JsonObj;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);

	if (!FJsonSerializer::Deserialize(Reader, JsonObj) || !JsonObj.IsValid()) return;

	FString PacketId;
	if (!JsonObj->TryGetStringField(TEXT("id"), PacketId)) return;

	// =========================================================
	// 1. 수동 조종 (MOVE) ➔ NavEngine으로 조종값 전달
	// =========================================================
	if (PacketId == TEXT("MOVE"))
	{
		// 옵저버 자유 시점 모드인 경우: 옵저버 이동
		if (CachedObserverActor && CachedObserverActor->TrackingMode == EObserverTrackingMode::FreeRoam)
		{
			float Forward_cm = JsonObj->GetNumberField(TEXT("forward")) * 100.0f;
			float Right_cm = JsonObj->GetNumberField(TEXT("right")) * 100.0f;
			float Up_cm = JsonObj->HasField(TEXT("up")) ? JsonObj->GetNumberField(TEXT("up")) * 100.0f : 0.0f;
			float Yaw_deg = JsonObj->GetNumberField(TEXT("yaw"));

			FVector MoveOffset = (CachedObserverActor->GetActorForwardVector() * Forward_cm) +
				(CachedObserverActor->GetActorRightVector() * Right_cm) +
				(FVector::UpVector * Up_cm);

			if (!MoveOffset.IsNearlyZero())
			{
				CachedObserverActor->SetActorLocation(CachedObserverActor->GetActorLocation() + MoveOffset, true);
			}
			if (!FMath::IsNearlyZero(Yaw_deg))
			{
				CachedObserverActor->AddActorLocalRotation(FRotator(0.0f, Yaw_deg, 0.0f));
			}
			return;
		}

		// 드론 고정익 비행 조종인 경우: NavEngine에 정규화된 조종값 입력!
		if (CachedDronePawn)
		{
			float Throttle = JsonObj->GetNumberField(TEXT("forward")); // -1.0 ~ 1.0
			float Roll = JsonObj->GetNumberField(TEXT("right"));   // -1.0 ~ 1.0 (좌우 뱅킹)
			float Pitch = JsonObj->HasField(TEXT("pitch")) ? JsonObj->GetNumberField(TEXT("pitch")) : 0.0f; // 승강타
			float Yaw = JsonObj->GetNumberField(TEXT("yaw"));     // -1.0 ~ 1.0 (러더 조향)

			CachedDronePawn->ApplyManualControl(Throttle, Roll, Pitch, Yaw);
		}
	}
	else if (PacketId == TEXT("GIMBAL"))
	{
		int Up = JsonObj->GetNumberField(TEXT("up"));
		int Right = JsonObj->GetNumberField(TEXT("right"));

		if (Up != 0 && GimbalComponentPitch)
		{
			float CurrentPitch = GimbalComponentPitch->GetRelativeRotation().Pitch;
			float NewPitch = FMath::Clamp(CurrentPitch + Up, -90.0f, 0.0f);
			GimbalComponentPitch->SetRelativeRotation(FRotator(NewPitch, 0.0f, 0.0f));
		}

		if (Right != 0 && GimbalComponentYaw)
		{
			float CurrentYaw = GimbalComponentYaw->GetRelativeRotation().Yaw;
			float NewYaw = FMath::Clamp(CurrentYaw + Right, -30.0f, 30.0f);
			GimbalComponentYaw->SetRelativeRotation(FRotator(0.0f, NewYaw, 0.0f));
		}
	}
	else if (PacketId == TEXT("TELEPORT"))
	{
		if (!CachedDronePawn) return;

		float LocX_cm = JsonObj->GetNumberField(TEXT("loc_x")) * 100.0f;
		float LocY_cm = JsonObj->GetNumberField(TEXT("loc_y")) * 100.0f;
		float LocZ_cm = JsonObj->GetNumberField(TEXT("loc_z")) * 100.0f;

		CachedDronePawn->SetActorLocationAndRotation(
			FVector(LocX_cm, LocY_cm, LocZ_cm),
			FRotator(0.0f, 0.0f, 0.0f),
			false, nullptr, ETeleportType::TeleportPhysics
		);
	}
	else if (PacketId == TEXT("OBSERVER"))
	{
		if (CachedObserverActor)
		{
			CachedObserverActor->CycleNextObserverMode();
		}
	}
	else if (PacketId == TEXT("CAPTURE"))
	{
		if (CachedDronePawn)
		{
			CachedDronePawn->ExecuteCapture();
		}
	}
	else if (PacketId == TEXT("AUTONAV"))
	{
		if (CachedDronePawn)
		{
			int enable = JsonObj->GetNumberField(TEXT("enable"));
			CachedDronePawn->GetNavEngine()->SetAutoNav((bool)enable);
		}
	}
}

void UCommLink::SendTelemetry()
{
	if (!CachedDronePawn) return;

	FVector Loc = CachedDronePawn->GetActorLocation();
	FRotator Rot = CachedDronePawn->GetActorRotation();

	float GPitch = GimbalComponentPitch ? GimbalComponentPitch->GetRelativeRotation().Pitch : -90.0f;
	float GYaw = GimbalComponentYaw ? GimbalComponentYaw->GetRelativeRotation().Yaw : 0.0f;

	float PosX_m = Loc.X / 100.0f;
	float PosY_m = Loc.Y / 100.0f;
	float PosZ_m = Loc.Z / 100.0f;

	// [HUD 계기판 디스플레이]
	if (GEngine)
	{
		const float DisplayDuration = 0.25f;

		FString MsgHeader = FString::Printf(TEXT("[텔레메트리 10Hz] Seq: #%u ➔ GCS (%s:%d)"),
			SequenceNumber, *TelemetryIP, TelemetryPort);
		FString MsgPos = FString::Printf(TEXT("드론 위치: X = %8.1f m | Y = %8.1f m | 고도(Z) = %6.1f m"),
			PosX_m, PosY_m, PosZ_m);
		FString MsgRot = FString::Printf(TEXT("기체 자세: Pitch = %5.1f° | Yaw = %5.1f° | Roll = %5.1f° (뱅크)"),
			Rot.Pitch, Rot.Yaw, Rot.Roll);
		FString MsgGimbal = FString::Printf(TEXT("짐벌 상태: Pitch = %5.1f° | Yaw = %5.1f°속도 %5.0f [%s]"),
			GPitch, GYaw, 
			CachedDronePawn->GetNavEngine()->GetCurrentSpeed(),
			CachedDronePawn->GetNavEngine()->IsAutoNav() ? TEXT("AUTO") : TEXT("MANUAL"));

		GEngine->AddOnScreenDebugMessage(1, DisplayDuration, FColor::Cyan, MsgHeader);
		GEngine->AddOnScreenDebugMessage(2, DisplayDuration, FColor::White, MsgPos);
		GEngine->AddOnScreenDebugMessage(3, DisplayDuration, FColor::Yellow, MsgRot);
		GEngine->AddOnScreenDebugMessage(4, DisplayDuration, FColor::Green, MsgGimbal);
	}

	// JSON 조립 및 UDP 송신
	TSharedPtr<FJsonObject> Root = MakeShareable(new FJsonObject());
	Root->SetStringField(TEXT("id"), TEXT("TELEMETRY"));
	Root->SetNumberField(TEXT("seq"), ++SequenceNumber);

	Root->SetNumberField(TEXT("loc_x"), PosX_m);
	Root->SetNumberField(TEXT("loc_y"), PosY_m);
	Root->SetNumberField(TEXT("loc_z"), PosZ_m);

	Root->SetNumberField(TEXT("rot_pitch"), Rot.Pitch);
	Root->SetNumberField(TEXT("rot_yaw"), Rot.Yaw);
	Root->SetNumberField(TEXT("rot_roll"), Rot.Roll);

	Root->SetNumberField(TEXT("gimbal_pitch"), GPitch);
	Root->SetNumberField(TEXT("gimbal_yaw"), GYaw);

	Root->SetNumberField(TEXT("speed"), CachedDronePawn->GetNavEngine()->GetCurrentSpeed());
	Root->SetStringField(TEXT("mode"), 
		CachedDronePawn->GetNavEngine()->IsAutoNav()? TEXT("AUTO"):TEXT("MANUAL"));

	Root->SetStringField(TEXT("last_capture"),
		CachedDronePawn->LastCapturedFile);

	FString JsonOut;
	TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&JsonOut);
	FJsonSerializer::Serialize(Root.ToSharedRef(), Writer);

	if (TelemetrySocket && TelemetryEndpoint.IsValid())
	{
		FTCHARToUTF8 Utf8String(*JsonOut);
		int32 BytesSent = 0;
		TelemetrySocket->SendTo(reinterpret_cast<const uint8*>(Utf8String.Get()), Utf8String.Length(), BytesSent, *TelemetryEndpoint);
	}
}