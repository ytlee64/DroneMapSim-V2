// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;
using System.IO;

public class DroneMapSim : ModuleRules
{
    public DroneMapSim(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicIncludePaths.AddRange(
            new string[] {
                Path.Combine(ModuleDirectory, "Public")
            }
        );

        PrivateIncludePaths.AddRange(
            new string[] {
                Path.Combine(ModuleDirectory, "Private")
            }
        );

        PublicDependencyModuleNames.AddRange(new string[] {
            "Core",
            "CoreUObject",
            "Engine",
            "InputCore",
            "EnhancedInput",
            "ImageWriteQueue",
            "UMG",
            "ProceduralMeshComponent",
            "Landscape",
            "Sockets",       // Added for UDP/TCP Sockets
			"Networking",    // Added for Network Address and Helpers
			"Json",          // Added for JSON Protocol parsing
			"JsonUtilities"  // Added for JSON Struct serialization
        });

        PrivateDependencyModuleNames.AddRange(
            new string[] { }
        );
    }
}