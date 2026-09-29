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

	// 1. 드론 폰(ADronePawn) 100% 확정 캐싱
	CachedDronePawn = Cast<ADronePawn>(GetOwner());
	if (!CachedDronePawn)
	{
		CachedDronePawn = Cast<ADronePawn>(UGameplayStatics::GetActorOfClass(World, ADronePawn::StaticClass()));
	}
	FindGimbalComponents(CachedDronePawn);

	// 2. 옵저버 폰(AObserverPawn) 100% 확정 캐싱
	CachedObserverActor = Cast<AObserverPawn>(UGameplayStatics::GetActorOfClass(World, AObserverPawn::StaticClass()));

	UE_LOG(LogTemp, Log, TEXT("[드론] %s |[옵저버] %s 캐싱 완료"),
		*CachedDronePawn->GetName(), *CachedObserverActor->GetName())
}

// 짐벌 컴포넌트 1회 탐색 바인딩
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

	// 드론 및 옵저버 참조 1회 캐싱
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

	// 1. 키보드 입력 연속 처리 (W/A/S/D, E/Q, Z/C, 화살표)
	ProcessInputKeyboard();

	// 2. UDP 명령 패킷 처리
	ProcessIncomingData();

	// 3. 10Hz 텔레메트리 주기 전송
	TelemetryTimer += DeltaTime;
	if (TelemetryTimer >= 0.1f)
	{
		TelemetryTimer = 0.0f;
		SendTelemetry();
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
	if (!PC || !CachedDronePawn) return;

	if (PC->WasInputKeyJustPressed(EKeys::SpaceBar))
	{
		ProcessJsonCommand(TEXT("{\"id\":\"CAPTURE\"}"));
	}

	if (PC->WasInputKeyJustPressed(EKeys::O))
	{
		ProcessJsonCommand(TEXT("{\"id\":\"OBSERVER\"}"));
	}

	// [1] 비행 이동 벡터 및 회전 계산
	int Forward = 0;
	int Right = 0;
	int Up = 0;
	int Yaw = 0;

	if (PC->WasInputKeyJustPressed(EKeys::W)) Forward += 3;
	if (PC->WasInputKeyJustPressed(EKeys::S)) Forward -= 3;
	if (PC->WasInputKeyJustPressed(EKeys::D)) Right += 3;
	if (PC->WasInputKeyJustPressed(EKeys::A)) Right -= 3;
	if (PC->WasInputKeyJustPressed(EKeys::E)) Up += 3; // 언리얼 상승
	if (PC->WasInputKeyJustPressed(EKeys::Q)) Up -= 3; // 언리얼 하강
	if (PC->WasInputKeyJustPressed(EKeys::C)) Yaw += 3; // 우회전
	if (PC->WasInputKeyJustPressed(EKeys::Z)) Yaw -= 3; // 좌회전

	// 비행 키 입력이 하나라도 들어왔다면 MANUAL_CONTROL JSON 명령 생성
	if (Forward != 0 || Right != 0 || Up != 0 || Yaw != 0)
	{
		FString FlightJson = FString::Printf(
			TEXT("{\"id\":\"MOVE\",\"forward\":%d,\"right\":%d,\"up\":%d,\"yaw\":%d}"),
			Forward, Right, Up, Yaw
		);
		ProcessJsonCommand(FlightJson);
	}

	int GimbalUp = 0;
	int GimbalRight = 0;

	if (PC->WasInputKeyJustPressed(EKeys::Up))    GimbalUp = 5;
	if (PC->WasInputKeyJustPressed(EKeys::Down))  GimbalUp = -5;
	if (PC->WasInputKeyJustPressed(EKeys::Right)) GimbalRight = 5;
	if (PC->WasInputKeyJustPressed(EKeys::Left))  GimbalRight = -5;

	// 짐벌 키 입력이 들어왔다면 GIMBAL_DELTA JSON 명령 생성
	if (GimbalUp != 0 || GimbalRight != 0)
	{
		FString GimbalJson = FString::Printf(
			TEXT("{\"id\":\"GIMBAL\",\"up\":%d,\"right\":%d}"),
			GimbalUp, GimbalRight
		);
		ProcessJsonCommand(GimbalJson);
	}
}

// -------------------------------------------------------------
// 2. UDP 수신 패킷 -> 원문 그대로 ProcessJsonCommand 호출
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

				// 키보드와 마찬가지로 모든 처리는 ProcessJsonCommand 단일 진입점으로 통과!
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
	UE_LOG(LogTemp, Log, TEXT("[UCommLink] Received JSON Command: %s"), *JsonString);

	TSharedPtr<FJsonObject> JsonObj;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);

	if (!FJsonSerializer::Deserialize(Reader, JsonObj) || !JsonObj.IsValid()) return;

	FString PacketId;
	if (!JsonObj->TryGetStringField(TEXT("id"), PacketId)) return;

	// =========================================================
	// 1. 키보드/GCS 수동 조종 (MOVE)
	// =========================================================
	if (PacketId == TEXT("MOVE"))
	{
		// 1. 단위 변환 (미터 -> 센티미터, Yaw는 각도 그대로)
		float Forward_cm = JsonObj->GetNumberField(TEXT("forward")) * 100.0f;
		float Right_cm = JsonObj->GetNumberField(TEXT("right")) * 100.0f;
		float Up_cm = JsonObj->GetNumberField(TEXT("up")) * 100.0f;
		float Yaw_deg = JsonObj->GetNumberField(TEXT("yaw"));

		// 2. 현재 조종 대상 액터 결정 (FreeRoam 모드면 옵저버, 그 외에는 드론)
		AActor* TargetActor = (CachedObserverActor && CachedObserverActor->TrackingMode == EObserverTrackingMode::FreeRoam)
			? Cast<AActor>(CachedObserverActor)
			: Cast<AActor>(CachedDronePawn);

		if (!TargetActor) return;

		// 3. 대상 액터 로컬 기준 이동 적용 (정확히 보낸 미터만큼 이동)
		FVector MoveOffset = (TargetActor->GetActorForwardVector() * Forward_cm) +
			(TargetActor->GetActorRightVector() * Right_cm) +
			(FVector::UpVector * Up_cm);

		if (!MoveOffset.IsNearlyZero())
		{
			FVector NewLoc = TargetActor->GetActorLocation() + MoveOffset;
			TargetActor->SetActorLocation(NewLoc, true); // 벽/지형 충돌 감지
		}

		// 4. 대상 액터 Yaw 제자리 회전 적용
		if (!FMath::IsNearlyZero(Yaw_deg))
		{
			TargetActor->AddActorLocalRotation(FRotator(0.0f, Yaw_deg, 0.0f));
		}
	}
	else if (PacketId == TEXT("GIMBAL"))
	{
		int Up = JsonObj->GetNumberField(TEXT("up"));
		int Right = JsonObj->GetNumberField(TEXT("right"));

		if (Up!=0)
		{
			float CurrentPitch = GimbalComponentPitch->GetRelativeRotation().Pitch;
			float NewPitch = FMath::Clamp(CurrentPitch + Up, -90.0f, 0.0f);
			GimbalComponentPitch->SetRelativeRotation(FRotator(NewPitch, 0.0f, 0.0f));
		}

		if (Right!=0)
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
		CachedObserverActor->CycleNextObserverMode();
	}
	else if (PacketId == TEXT("CAPTURE"))
	{
		if (!CachedDronePawn) return;

		CachedDronePawn->ExecuteCapture();
	}
	else if (PacketId == TEXT("WAYPOINT_LIST"))
	{
		if (!CachedDronePawn) return;

		// 1) 고정익 기본 파라미터 파싱
		float LoiterRadius_cm = 40.0f * 100.0f; // 기본 40m -> 4000cm
		if (JsonObj->HasField(TEXT("default_loiter_radius_m")))
		{
			LoiterRadius_cm = JsonObj->GetNumberField(TEXT("default_loiter_radius_m")) * 100.0f;
		}

		bool bPeriodicCapture = false;
		float CaptureIntervalSec = 2.5f;
		if (JsonObj->HasField(TEXT("periodic_capture")))
		{
			bPeriodicCapture = JsonObj->GetBoolField(TEXT("periodic_capture"));
		}
		if (JsonObj->HasField(TEXT("capture_interval_sec")))
		{
			CaptureIntervalSec = JsonObj->GetNumberField(TEXT("capture_interval_sec"));
		}

		// 2) points JSON 배열 파싱
		const TArray<TSharedPtr<FJsonValue>>* PointsArray = nullptr;
		if (JsonObj->TryGetArrayField(TEXT("points"), PointsArray) && PointsArray)
		{
			TArray<FWaypointItemData> ParsedWaypoints;
			ParsedWaypoints.Reserve(PointsArray->Num());

			for (const TSharedPtr<FJsonValue>& PointVal : *PointsArray)
			{
				TSharedPtr<FJsonObject> PtObj = PointVal->AsObject();
				if (!PtObj.IsValid()) continue;

				FWaypointItemData Wp;
				Wp.Id = PtObj->GetIntegerField(TEXT("id"));

				// 미터(m) -> 센티미터(cm) 단위 변환
				float X_cm = PtObj->GetNumberField(TEXT("x")) * 100.0f;
				float Y_cm = PtObj->GetNumberField(TEXT("y")) * 100.0f;
				float Z_cm = PtObj->GetNumberField(TEXT("z")) * 100.0f;
				Wp.Location = FVector(X_cm, Y_cm, Z_cm);

				// 속도 (m/s -> cm/s 변환)
				float Speed_ms = PtObj->HasField(TEXT("speed")) ? PtObj->GetNumberField(TEXT("speed")) : 15.0f;
				Wp.Speed_cm_s = Speed_ms * 100.0f;

				ParsedWaypoints.Add(Wp);
			}

			// 3) 파싱된 웨이포인트 목록 및 고정익 비행 설정을 드론 폰에 전달
			CachedDronePawn->SetFixedWingWaypoints(
				ParsedWaypoints,
				LoiterRadius_cm,
				bPeriodicCapture,
				CaptureIntervalSec
			);

			UE_LOG(LogTemp, Log, TEXT("[UCommLink] Loaded %d Waypoints (LoiterRadius: %.1f cm, PeriodicCapture: %s)"),
				ParsedWaypoints.Num(),
				LoiterRadius_cm,
				bPeriodicCapture ? TEXT("True") : TEXT("False"));
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

	// [깜빡임 없는 고정 HUD 계기판 (0.25초 수명으로 부드럽게 갱신)]
	if (GEngine)
	{
		const float DisplayDuration = 0.25f;

		FString MsgHeader = FString::Printf(TEXT("[텔레메트리 10Hz] Seq: #%u ➔ GCS (%s:%d)"),
			SequenceNumber, *TelemetryIP, TelemetryPort);
		FString MsgPos = FString::Printf(TEXT("드론 위치: X = %8.1f m | Y = %8.1f m | 고도(Z) = %6.1f m"),
			PosX_m, PosY_m, PosZ_m);
		FString MsgRot = FString::Printf(TEXT("기체 자세: Pitch = %5.1f° | Yaw = %5.1f° | Roll = %5.1f°"),
			Rot.Pitch, Rot.Yaw, Rot.Roll);
		FString MsgGimbal = FString::Printf(TEXT("짐벌 상태: Pitch = %5.1f° (하방) | Yaw = %5.1f°"),
			GPitch, GYaw);

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

	Root->SetNumberField(TEXT("speed"), 0.0f);
	Root->SetStringField(TEXT("mode"), TEXT("MANUAL"));

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