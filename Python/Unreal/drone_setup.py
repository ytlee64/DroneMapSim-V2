"""
================================================================================
 File: drone_setup.py (UE 5.7 Standard)
 Description: Idempotent drone spawn, gimbal auto-link, and scene capture setup.
================================================================================
"""
import os
import sys
import unreal

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

from log_utils import make_loggers

log_info, log_warn, log_error = make_loggers("DroneSetup")

TOTAL_STEPS = 5


def log_step(step, msg):
    log_info(f"[{step}/{TOTAL_STEPS}] {msg}")


TARGET_NAMES = {
    "Drone": "BP_DronePawn",
    "CommandReceiver": "DroneCommandReceiver",           # C++ or BP Component
    "ScanProjection": "BP_ScanProjection",
    "Observer": "BP_ObserverPawn",
    "GameMode": "BP_DroneMapSimGameMode",
    "MaterialScanGround": "M_ScanGround",
    "MaterialScanProjection": "M_ScanProjection",
    "RenderTarget": "RT_DroneCapture",
    "MinimapUI": "WBP_MinimapUI"
}

CLEAR_KEYWORDS = [
    "BP_DronePawn",
    "BP_ScanProjection", 
    "BP_ObserverPawn",
    "DroneCommandReceiver",
]


def get_subsystems():
    editor_actor_subsystem = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    unreal_editor_subsystem = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem)
    asset_lib = unreal.EditorAssetLibrary
    asset_reg = unreal.AssetRegistryHelpers.get_asset_registry()
    return editor_actor_subsystem, unreal_editor_subsystem, asset_lib, asset_reg


def clear_existing_actors(editor_actor_subsystem):
    all_actors = editor_actor_subsystem.get_all_level_actors()
    destroyed_count = 0

    for actor in all_actors:
        if not actor:
            continue

        actor_name = actor.get_name()
        actor_label = actor.get_actor_label()
        actor_class_name = actor.get_class().get_name()

        should_destroy = any(
            kw.lower() in actor_name.lower() or
            kw.lower() in actor_label.lower() or
            kw.lower() in actor_class_name.lower()
            for kw in CLEAR_KEYWORDS
        )

        if should_destroy:
            log_info(f"  - Cleaning existing actor: '{actor_label}' ({actor_class_name})")
            editor_actor_subsystem.destroy_actor(actor)
            destroyed_count += 1

    if destroyed_count > 0:
        log_info(f"Total {destroyed_count} existing drone/control actors cleared.")
    else:
        log_info("Level is clean. No leftover actors found.")


def load_bp_class_by_name(target_name, asset_reg, asset_lib):
    assets = asset_reg.get_assets_by_path("/Game", recursive=True)
    for a in assets:
        if str(a.asset_name) == target_name:
            pkg_name = str(a.package_name)
            bp_class_path = f"{pkg_name}.{target_name}_C"
            cls = unreal.load_class(None, bp_class_path) or asset_lib.load_blueprint_class(bp_class_path)
            if cls:
                return cls
    cpp_class_name = target_name.replace("BP_", "")
    return getattr(unreal, cpp_class_name, None)


def load_object_by_name(target_name, asset_reg, asset_lib):
    assets = asset_reg.get_assets_by_path("/Game", recursive=True)
    for a in assets:
        if str(a.asset_name) == target_name:
            pkg_name = str(a.package_name)
            obj_path = f"{pkg_name}.{target_name}"
            return unreal.load_object(None, obj_path) or asset_lib.load_asset(obj_path)
    return None


def determine_spawn_center(editor_actor_subsystem):
    all_actors_refreshed = editor_actor_subsystem.get_all_level_actors()
    target_landscape = next((a for a in all_actors_refreshed if isinstance(a, (unreal.Landscape, unreal.LandscapeProxy))), None)
    spawn_center = unreal.Vector(0, 0, 3000)

    if target_landscape:
        origin, extent = target_landscape.get_actor_bounds(False)
        spawn_center = unreal.Vector(origin.x, origin.y, origin.z + 3000)
        log_step(2, f"Landscape detected -> Spawn center: (X={spawn_center.x:.1f}, Y={spawn_center.y:.1f}, Z={spawn_center.z:.1f})")

    return spawn_center

def spawn_drone(editor_actor_subsystem, spawn_center, asset_reg, asset_lib):
    drone_class = load_bp_class_by_name(TARGET_NAMES["Drone"], asset_reg, asset_lib)
    if not drone_class:
        log_error(f"Failed to find drone class: '{TARGET_NAMES['Drone']}'")
        return None

    drone_actor = editor_actor_subsystem.spawn_actor_from_class(drone_class, spawn_center)
    if not drone_actor:
        log_error("Failed to spawn drone actor instance.")
        return None

    try:
        drone_actor.set_editor_property("AutoPossessPlayer", unreal.AutoReceiveInput.PLAYER0)
    except Exception:
        pass
    log_step(3, "BP_DronePawn spawned successfully (Auto Possess: Player 0)")

    # 1. Connect Scene Capture to Render Target
    scene_captures = drone_actor.get_components_by_class(unreal.SceneCaptureComponent2D)
    rt_asset = load_object_by_name(TARGET_NAMES["RenderTarget"], asset_reg, asset_lib)

    for cap in scene_captures:
        if rt_asset:
            cap.set_editor_property("TextureTarget", rt_asset)
            log_info(f"  + SceneCapture RenderTarget connected -> '{rt_asset.get_name()}'")

        cap.set_editor_property("CaptureSource", unreal.SceneCaptureSource.SCS_FINAL_COLOR_LDR)
        cap.set_editor_property("bCaptureEveryFrame", False)
        log_info("  + Capture source configured -> FinalColor (LDR)")

    # 2. Verify Native C++ DroneCommandReceiver Component
    receiver_class_name = TARGET_NAMES["CommandReceiver"]
    receiver = next((c for c in drone_actor.get_components_by_class(unreal.ActorComponent)
                     if receiver_class_name.lower() in c.get_class().get_name().lower()), None)

    if receiver:
        log_info(f"  + Native C++ '{receiver_class_name}' component verified: {receiver.get_name()}")

        # # Check & Link GimbalComponent
        # current_gimbal = receiver.get_editor_property("GimbalComponent")
        # if not current_gimbal:
        #     scene_comps = drone_actor.get_components_by_class(unreal.SceneComponent)
        #     target_gimbal = next((c for c in scene_comps if "gimbalinner" in c.get_name().lower() or "gimbal" in c.get_name().lower()), None)
        #     if target_gimbal:
        #         receiver.set_editor_property("GimbalComponent", target_gimbal)
        #         log_info(f"  + GimbalComponent auto-linked -> '{target_gimbal.get_name()}'")
    else:
        log_warn(f"  ! Native '{receiver_class_name}' not found on Drone. Ensure it is initialized in C++ constructor.")

    return drone_actor


def spawn_scan_and_observer(editor_actor_subsystem, spawn_center, asset_reg, asset_lib):
    scan_class = load_bp_class_by_name(TARGET_NAMES["ScanProjection"], asset_reg, asset_lib)
    if scan_class:
        scan_actor = editor_actor_subsystem.spawn_actor_from_class(scan_class, spawn_center)
        if scan_actor:
            log_step(4, "BP_ScanProjection spawned successfully")

            m_ground = load_object_by_name(TARGET_NAMES["MaterialScanGround"], asset_reg, asset_lib)
            m_proj = load_object_by_name(TARGET_NAMES["MaterialScanProjection"], asset_reg, asset_lib)

            mesh_comps = scan_actor.get_components_by_class(unreal.MeshComponent)
            for mesh in mesh_comps:
                mesh_name = mesh.get_name()
                if m_ground and "Ground" in mesh_name:
                    mesh.set_material(0, m_ground)
                    log_info(f"  + Ground mesh material applied -> '{m_ground.get_name()}'")
                elif m_proj:
                    mesh.set_material(0, m_proj)
                    log_info(f"  + Projection mesh material applied -> '{m_proj.get_name()}'")
    else:
        log_warn(f"Class '{TARGET_NAMES['ScanProjection']}' not found, skipping.")

    observer_class = load_bp_class_by_name(TARGET_NAMES["Observer"], asset_reg, asset_lib)
    if observer_class:
        observer_actor = editor_actor_subsystem.spawn_actor_from_class(observer_class, spawn_center + unreal.Vector(-450, 0, 220))
        if observer_actor:
            log_step(5, "BP_ObserverPawn spawned successfully")
    else:
        log_warn(f"Class '{TARGET_NAMES['Observer']}' not found, skipping.")

def configure_game_mode(unreal_editor_subsystem, asset_reg, asset_lib):
    gamemode_class = load_bp_class_by_name(TARGET_NAMES["GameMode"], asset_reg, asset_lib)
    if gamemode_class:
        world = unreal_editor_subsystem.get_editor_world()
        if world:
            world_settings = world.get_world_settings()
            if world_settings:
                # 1. 월드 세팅에 게임모드 적용
                world_settings.set_editor_property("DefaultGameMode", gamemode_class)
                
                # 2. 언리얼 5 정석: WorldSettings와 World 패키지를 Dirty(저장 대상)로 마킹
                try:
                    world_settings.modify()
                    world_pkg = world.get_outer()
                    if world_pkg:
                        world_pkg.mark_package_dirty()
                except Exception:
                    pass

                log_info(f"World DefaultGameMode set -> '{gamemode_class.get_name()}'")
            else:
                log_error("Failed to retrieve WorldSettings.")
        else:
            log_error("Failed to retrieve Editor World.")
    else:
        log_error(f"Failed to find GameMode class: '{TARGET_NAMES['GameMode']}'")

def main():
    log_info("========================================================")
    log_step(1, "Starting drone control infrastructure setup...")
    log_info("========================================================")

    editor_actor_subsystem, unreal_editor_subsystem, asset_lib, asset_reg = get_subsystems()
    clear_existing_actors(editor_actor_subsystem)
    spawn_center = determine_spawn_center(editor_actor_subsystem)
    spawn_drone(editor_actor_subsystem, spawn_center, asset_reg, asset_lib)
    spawn_scan_and_observer(editor_actor_subsystem, spawn_center, asset_reg, asset_lib)
    configure_game_mode(unreal_editor_subsystem, asset_reg, asset_lib)

    
    log_info("========================================================")
    log_info("Drone control infrastructure setup completed successfully!")
    log_info("========================================================")


if __name__ == "__main__":
    main()