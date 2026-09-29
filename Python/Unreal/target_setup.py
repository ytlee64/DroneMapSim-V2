"""
================================================================================
 파일명: env_setup.py
 설명: config.py의 전역 설정을 C++ EnvGenActor에 주입하고,
       세슘(Cesium) 지형 및 랜드스케이프 환경에 맞춰 식생/건물/지상 표적을
       절차적으로 자동 생성(GenerateEnvironment)합니다.
================================================================================
"""
import unreal
import os
import sys
import importlib

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

import config as config
config = importlib.reload(config)
from log_utils import make_loggers

log_info, _, log_error = make_loggers(
    "EnvSetup",
    error_label="치명적 오류",
    error_prefix="❌",
    raise_on_error=True,
)

TOTAL_STEPS = 5


def log_step(step, msg):
    log_info(f"[{step}/{TOTAL_STEPS}] {msg}")


def get_editor_context():
    actor_subsystem = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    asset_lib = unreal.EditorAssetLibrary
    asset_reg = unreal.AssetRegistryHelpers.get_asset_registry()
    return actor_subsystem, asset_lib, asset_reg


def resolve_spawn_location_and_bounds(actor_subsystem):
    """
    월드 내 지형(Landscape 또는 Cesium)을 감지하여 
    스폰 기준 위치(Location)와 생성 영역 크기(Bounds)를 동적으로 결정합니다.
    """
    all_actors = actor_subsystem.get_all_level_actors()
    landscape = None
    cesium_tileset = None
    cesium_georef = None
    
    for actor in all_actors:
        class_name = actor.get_class().get_name()
        if "Landscape" in class_name:
            landscape = actor
        elif "Cesium3DTileset" in class_name:
            cesium_tileset = actor
        elif "CesiumGeoreference" in class_name:
            cesium_georef = actor

    # 1. 일반 언리얼 랜드스케이프인 경우
    if landscape:
        origin, box_extent = landscape.get_actor_bounds(False)
        spawn_location = unreal.Vector(origin.x, origin.y, origin.z + box_extent.z + 500.0)
        area_extent = box_extent
        log_info(f"✅ [EnvSetup] 랜드스케이프 감지: 중심 {spawn_location}, 크기 {area_extent}")
        return all_actors, spawn_location, area_extent, False

    # 2. 세슘(Cesium) 지형 환경인 경우
    elif cesium_tileset or cesium_georef:
        # 세슘 지형에서는 원점 (0, 0) 기준 상공 50m(5000cm) 위치를 생성기 중심으로 잡음
        spawn_location = unreal.Vector(0.0, 0.0, 5000.0)
        
        # 반경 1km (가로 100,000cm x 세로 100,000cm)의 비행 훈련 구역 영역 설정
        # 빽빽한 산림 구역을 형성할 기본 크기
        default_half_width = getattr(config, "CESIUM_ENV_RADIUS", 60000.0) # 기본 반경 600m
        area_extent = unreal.Vector(default_half_width, default_half_width, 10000.0)
        
        log_info(f"🌍 [EnvSetup] Cesium 지구 환경 감지!")
        log_info(f"   - 중심 위치: {spawn_location}")
        log_info(f"   - 생성 반경: {default_half_width / 100.0}m (총 가로세로 {default_half_width * 2 / 100.0}m 영역)")
        return all_actors, spawn_location, area_extent, True

    # 3. 폴백 (지형 없음)
    else:
        spawn_location = unreal.Vector(0.0, 0.0, 3000.0)
        area_extent = unreal.Vector(50000.0, 50000.0, 5000.0)
        log_info(f"⚠️ [EnvSetup] 지형 액터 없음. 기본 원점 스폰: {spawn_location}")
        return all_actors, spawn_location, area_extent, False


def get_or_spawn_env_actor(actor_subsystem, asset_lib, all_actors, spawn_location):
    env_actor = next((a for a in all_actors if "EnvGenActor" in a.get_name()), None)

    env_class = (
        getattr(unreal, "EnvGenActor", None) or
        asset_lib.load_blueprint_class("/Script/DroneMapSim.EnvGenActor")
    )
    if not env_class:
        log_error("C++ 클래스 'EnvGenActor'를 찾을 수 없습니다. C++ 컴파일 상태를 확인하세요.")

    if not env_actor:
        log_info("  + 월드에 EnvGenActor를 새로 스폰합니다...")
        env_actor = actor_subsystem.spawn_actor_from_class(env_class, spawn_location)
    else:
        # 기존 액터가 있다면 최신 스폰 위치로 갱신
        env_actor.set_actor_location(spawn_location, False, False)

    if not env_actor:
        log_error("EnvGenActor 생성에 실패했습니다.")

    log_step(3, f"EnvGenActor 준비 완료: '{env_actor.get_name()}'")
    return env_actor


def apply_global_params(env_actor, area_extent, is_cesium=False):
    params = getattr(config, "ENV_GEN_PARAMS", {})
    
    # 세슘 환경일 때는 나무를 더욱 빽빽하게 심기 위해 간격을 살짝 좁힘
    default_spacing = 600.0 if is_cesium else 1200.0
    grid_spacing = float(params.get("GridSpacing", default_spacing))
    position_jitter = float(params.get("PositionJitter", grid_spacing * 0.4))
    align_normal = float(params.get("AlignToSurfaceNormal", 0.0))

    env_actor.set_editor_property("GridSpacing", grid_spacing)
    env_actor.set_editor_property("PositionJitter", position_jitter)
    env_actor.set_editor_property("AlignToSurfaceNormal", align_normal)
    
    # C++ 액터에 AreaExtent 프로퍼티가 있으면 주입 (없으면 안전하게 패스)
    try:
        env_actor.set_editor_property("AreaExtent", area_extent)
        log_info(f"  + 생성 영역 Extent 주입: X={area_extent.x}, Y={area_extent.y}")
    except Exception:
        # C++에 AreaExtent 변수가 없는 경우 예외 무시하고 정상 진행
        pass

    log_info(f"  + 파라미터 주입: GridSpacing={grid_spacing}cm (식생 밀도), Jitter={position_jitter}cm, NormalAlign={align_normal}")


def find_static_mesh(mesh_name, asset_reg, asset_lib):
    assets = asset_reg.get_assets_by_path("/Game", recursive=True)
    for a in assets:
        if str(a.asset_name) == mesh_name:
            pkg_name = str(a.package_name)
            obj_path = f"{pkg_name}.{mesh_name}"
            mesh = unreal.load_object(None, obj_path) or asset_lib.load_asset(obj_path)
            if mesh and isinstance(mesh, unreal.StaticMesh):
                return mesh
    return None


def build_env_elements(asset_reg, asset_lib, is_cesium=False):
    element_configs = getattr(config, "ENV_ELEMENT_CONFIGS", [])
    if not element_configs:
        log_error("config.py에 'ENV_ELEMENT_CONFIGS'가 비어있습니다.")

    new_elements_array = unreal.Array(unreal.EnvElementConfig)
    failed_meshes = []

    log_step(4, "환경 요소(식생/건물/차량) 메쉬 에셋 검증 및 필터링 중...")
    for item in element_configs:
        mesh_name = item["mesh_name"]
        mesh_asset = find_static_mesh(mesh_name, asset_reg, asset_lib)

        if not mesh_asset:
            failed_meshes.append(mesh_name)
            continue

        cfg = unreal.EnvElementConfig()
        cfg.set_editor_property("ElementName", str(item.get("name", mesh_name)))
        cfg.set_editor_property("ElementMesh", mesh_asset)
        
        # 가중치: 산림 빽빽하게 심기
        weight = float(item.get("weight", 1.0))
        cfg.set_editor_property("SpawnWeight", weight)
        
        # 도로 평지 제외 / 경사도 한계 설정
        # 숲이나 산림(Tree)은 경사도 35도~50도 비탈에도 잘 심어지도록 설정
        max_slope = float(item.get("max_slope", 45.0))
        cfg.set_editor_property("MaxSlopeAngle", max_slope)

        cfg.set_editor_property("bIsTarget", bool(item.get("b_is_target", False)))
        cfg.set_editor_property("TargetClassId", int(item.get("target_class_id", 0)))

        scale_min = float(item.get("scale_min", 0.8))
        scale_max = float(item.get("scale_max", 1.2))
        cfg.set_editor_property("ScaleRange", unreal.Vector2D(scale_min, scale_max))

        new_elements_array.append(cfg)
        log_info(f"  + [정상 등록] {item['name']} -> '{mesh_asset.get_name()}' (가중치: {weight}, 한계경사: {max_slope}°)")

    if failed_meshes:
        log_error(f"다음 {len(failed_meshes)}개의 메쉬를 프로젝트 내에서 찾지 못했습니다:\n" + "\n".join(f"  - {m}" for m in failed_meshes))

    return new_elements_array


def generate_environment(env_actor):
    log_info("========================================================")
    log_step(5, "절차적 환경 자동 생성(GenerateEnvironment) 실행 중...")
    log_info("========================================================")
    
    # C++ GenerateEnvironment() 호출
    env_actor.generate_environment()
    log_info("🎉 절차적 산림/환경 생성이 성공적으로 완료되었습니다!")


def main():
    log_info("========================================================")
    log_step(1, "EnvGenActor 환경 자동 생성 파이프라인 시작...")
    log_info("========================================================")

    actor_subsystem, asset_lib, asset_reg = get_editor_context()
    
    # [개선 1] 지형 타입(세슘/랜드스케이프)에 따라 스폰 위치와 영역 반경(Extent) 자동 획득
    all_actors, spawn_location, area_extent, is_cesium = resolve_spawn_location_and_bounds(actor_subsystem)
    
    # [개선 2] EnvGenActor 위치 동기화
    env_actor = get_or_spawn_env_actor(actor_subsystem, asset_lib, all_actors, spawn_location)
    
    # [개선 3] 세슘 지형에 맞춘 밀도 및 영역 파라미터 주입
    apply_global_params(env_actor, area_extent, is_cesium)
    
    # [개선 4] 메쉬 에셋 및 식생 조건 바인딩
    new_elements_array = build_env_elements(asset_reg, asset_lib, is_cesium)
    env_actor.set_editor_property("EnvElements", new_elements_array)
    log_info(f"✅ 총 {len(new_elements_array)}개 환경 요소 주입 완료!")
    
    # [개선 5] 환경 생성 실행
    generate_environment(env_actor)


if __name__ == "__main__":
    main()