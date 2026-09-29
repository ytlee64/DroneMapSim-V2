# Unreal Engine C++ Naming Conventions & Prefixes

In Unreal Engine (UE5), class and type prefixes are not merely stylistic choices; they are strictly enforced by the **Unreal Header Tool (UHT)** for code reflection, garbage collection, and compilation.

---

## 1. Standard Class & Type Prefixes

| Prefix | Type Category | Description | Examples |
| :--- | :--- | :--- | :--- |
| **`A`** | **Actor** | Classes inheriting directly or indirectly from `AActor`. Can be placed or spawned into the 3D level. | `ADronePawn`, `AActor`, `ACharacter`, `AGameModeBase` |
| **`U`** | **UObject** | Classes inheriting from `UObject` (excluding Actors). Managed by Unreal GC and UObject reflection. | `UActorComponent`, `UStaticMeshComponent`, `UCommLink`, `UTexture2D` |
| **`F`** | **Frame / Plain C++** | Plain C++ structs, math primitives, and utility classes (not garbage collected). | `FVector`, `FRotator`, `FTransform`, `FString`, `FSocket`, `FHitResult` |
| **`T`** | **Template** | Template classes, standard containers, and smart pointers. | `TArray<T>`, `TMap<Key, Value>`, `TSharedPtr<T>`, `TObjectPtr<T>`, `TWeakObjectPtr<T>` |
| **`I`** | **Interface** | Abstract interface classes defining contracts for multiple classes. | `IInterface`, `ISocketSubsystem`, `IAbilitySystemInterface` |
| **`E`** | **Enum** | Enumeration declarations (`enum` or `enum class`). | `EDroneFlightMode`, `EGimbalMode`, `EEndPlayReason` |

---

## 2. Variable & Member Prefixes

| Prefix | Type | Rule / Purpose | Examples |
| :--- | :--- | :--- | :--- |
| **`b`** | **Boolean** | All boolean member variables must start with a lowercase `b`. | `bIsFlying`, `bWaypointReached`, `bCaptureEveryFrame` |
| *PascalCase* | **Other Variables** | All other variables, member pointers, and functions use PascalCase. | `TargetLocation`, `FlightSpeed`, `CommLink` |

---

## 3. Blueprint & Asset Naming Conventions

For project assets and Blueprints in the Content Browser:

| Prefix | Asset Type | Example |
| :--- | :--- | :--- |
| **`BP_`** | Blueprint Class | `BP_DronePawn`, `BP_ObserverPawn` |
| **`M_`** | Material | `M_ScanGround`, `M_ScanProjection` |
| **`MI_`** | Material Instance | `MI_DroneBody_Carbon` |
| **`RT_`** | Render Target 2D | `RT_DroneCapture` |
| **`SM_`** | Static Mesh | `SM_QuadcopterBody`, `SM_Propeller` |
| **`IMC_`** | Input Mapping Context | `IMC_DroneFlight` |
| **`IA_`** | Input Action | `IA_DroneMove`, `IA_DroneRotate` |
| **`WBP_`** | Widget Blueprint (UI) | `WBP_FlightHUD`, `WBP_GCSStatus` |

---

## 4. Applied Example from Drone Simulator

```cpp
// E: Enum
enum class EDroneFlightMode : uint8 { Manual, Autonomous };

// F: Struct
struct FDroneTelemetryPacket
{
    FVector Location;      // F: Vector struct
    FRotator Rotation;     // F: Rotator struct
    bool bReachedTarget;   // b: Boolean member
};

// U: UObject / ActorComponent (No 3D position by itself)
class UCommLink : public UActorComponent
{
    // ...
};

// A: Actor / Pawn (Exists in 3D world)
class ADronePawn : public APawn
{
    // T: Template pointer, U: Component
    TObjectPtr<UCommLink> CommLink; 
};