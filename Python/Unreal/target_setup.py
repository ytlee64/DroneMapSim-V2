"""
================================================================================
 파일명: target_setup.py
 설명: config.py의 TARGET_ELEMENT_CONFIGS 설정을 읽어,
       언리얼 랜드스케이프 지형에 표적(b_is_target=True) 및 일반 차량(False)을
       절차적으로 자동 배치(TargetGenActor)합니다.
================================================================================
"""
import os
import sys
import importlib
import unreal

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

import config as config
config = importlib.reload(config)
from log_utils import make_loggers

log_info, _, log_error = make_loggers(
    "TargetSetup",
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
    월드 내 랜드스케이프 지형을 감지하여 스폰 위치와 영역 크기를 결정합니다.
    """
    all_actors = actor_subsystem.get_all_level_actors()
    landscape = next((a for a in all_actors if isinstance(a, (unreal.Landscape, unreal.LandscapeProxy))), None)

    if landscape:
        origin, box_extent = landscape.get_actor_bounds(False)
        spawn_location = unreal.Vector(origin.x, origin.y, origin.z + box_extent.z + 500.0)
        area_extent = box_extent
        log_info(f"✅ [TargetSetup] 랜드스케이프 감지: 중심 {spawn_location}, 영역 {area_extent}")
        return all_actors, spawn_location, area_extent

    # 랜드스케이프가 없는 경우 기본값
    spawn_location = unreal.Vector(0.0, 0.0, 3000.0)
    area_extent = unreal.Vector(50000.0, 50000.0, 5000.0)
    log_info(f"⚠️ [TargetSetup] 랜드스케이프 없음. 기본 원점 스폰: {spawn_location}")
    return all_actors, spawn_location, area_extent


def get_or_spawn_target_actor(actor_subsystem, asset_lib, all_actors, spawn_location):
    """
    월드에서 TargetGenActor를 찾아 재사용하거나 새로 스폰합니다.
    """
    target_class = (
        getattr(unreal, "TargetGenActor", None) or
        asset_lib.load_blueprint_class("/Script/DroneMapSim.TargetGenActor") 
    )

    if not target_class:
        log_error("C++ 클래스 'TargetGenActor'를 찾을 수 없습니다. C++ 컴파일 상태를 확인하세요.")

    target_actor = next(
        (a for a in all_actors if "TargetGenActor" in a.get_name() ),
        None
    )

    if not target_actor:
        log_info("  + 월드에 TargetGenActor를 새로 스폰합니다...")
        target_actor = actor_subsystem.spawn_actor_from_class(target_class, spawn_location)
    else:
        target_actor.set_actor_location(spawn_location, False, False)

    if not target_actor:
        log_error("TargetGenActor 액터 생성에 실패했습니다.")

    log_step(3, f"TargetGenActor 액터 준비 완료: '{target_actor.get_name()}'")
    return target_actor


def apply_target_params(target_actor, area_extent):
    """
    비히클 배치용 그리드 간격 및 지면 정렬 설정
    """
    params = getattr(config, "TARGET_GEN_PARAMS", getattr(config, "ENV_GEN_PARAMS", {}))

    # 기본 간격 35m (3500cm)
    grid_spacing = float(params.get("GridSpacing", 3500.0))
    position_jitter = float(params.get("PositionJitter", grid_spacing * 0.4))
    align_normal = float(params.get("AlignToSurfaceNormal", 1.0))  # 차량은 땅 경사에 맞춰 착지

    target_actor.set_editor_property("GridSpacing", grid_spacing)
    target_actor.set_editor_property("PositionJitter", position_jitter)
    target_actor.set_editor_property("AlignToSurfaceNormal", align_normal)

    try:
        target_actor.set_editor_property("AreaExtent", area_extent)
    except Exception:
        pass

    log_info(f"  + 배치 파라미터: 간격={grid_spacing/100.0:.1f}m, 지터={position_jitter/100.0:.1f}m, 경사면정렬={align_normal}")


def load_static_mesh(item, asset_reg, asset_lib):
    """
    1) mesh_path로 우선 초고속 직접 로드
    2) 없으면 asset_reg로 mesh_name 검색 폴백
    """
    mesh_path = item.get("mesh_path")
    if mesh_path:
        mesh = unreal.load_object(None, mesh_path) or asset_lib.load_asset(mesh_path)
        if mesh and isinstance(mesh, unreal.StaticMesh):
            return mesh

    mesh_name = item.get("mesh_name")
    if mesh_name:
        assets = asset_reg.get_assets_by_path("/Game", recursive=True)
        for a in assets:
            if str(a.asset_name) == mesh_name:
                pkg_name = str(a.package_name)
                obj_path = f"{pkg_name}.{mesh_name}"
                mesh = unreal.load_object(None, obj_path) or asset_lib.load_asset(obj_path)
                if mesh and isinstance(mesh, unreal.StaticMesh):
                    return mesh
    return None


def build_target_elements(asset_reg, asset_lib):
    """
    config.py의 TARGET_ELEMENT_CONFIGS를 읽어 C++ 구조체 배열로 변환합니다.
    """
    element_configs = getattr(config, "TARGET_ELEMENT_CONFIGS", [])
    if not element_configs:
        log_error("config.py에 'TARGET_ELEMENT_CONFIGS'가 비어있습니다.")

    new_elements_array = unreal.Array(unreal.EnvElementConfig)
    failed_meshes = []

    log_step(4, "차량(표적/비표적) 메쉬 검증 및 로드 중...")

    for item in element_configs:
        mesh_name = item["mesh_name"]
        mesh_asset = load_static_mesh(item, asset_reg, asset_lib)

        if not mesh_asset:
            failed_meshes.append(mesh_name)
            continue

        cfg = unreal.EnvElementConfig()
        cfg.set_editor_property("ElementName", str(item.get("name", mesh_name)))
        cfg.set_editor_property("ElementMesh", mesh_asset)
        cfg.set_editor_property("SpawnWeight", float(item.get("weight", 0.01)))
        cfg.set_editor_property("MaxSlopeAngle", float(item.get("max_slope", 15.0)))

        # ★ 선생님의 설정 그대로 1:1 주입:
        # b_is_target: True/False
        # target_class_id: 지정되어 있으면 그 값, 없으면 기본값(-1)
        b_is_target = bool(item.get("b_is_target", False))
        cfg.set_editor_property("bIsTarget", b_is_target)

        target_class_id = int(item.get("target_class_id", 0 if b_is_target else -1))
        cfg.set_editor_property("TargetClassId", target_class_id)

        scale_min = float(item.get("scale_min", 1.0))
        scale_max = float(item.get("scale_max", 1.0))
        cfg.set_editor_property("ScaleRange", unreal.Vector2D(scale_min, scale_max))

        new_elements_array.append(cfg)

        status_tag = "🎯 [타깃 표적]" if b_is_target else "⚪ [일반 차량]"
        log_info(f"  + {status_tag} {item.get('name', mesh_name)} -> '{mesh_name}' (Target={b_is_target}, ClassID={target_class_id})")

    if failed_meshes:
        log_error(f"다음 {len(failed_meshes)}개의 메쉬를 로드하지 못했습니다:\n" + "\n".join(f"  - {m}" for m in failed_meshes))

    return new_elements_array


def generate_targets(target_actor):
    log_info("========================================================")
    log_step(5, "절차적 차량/표적 자동 생성 실행 중...")
    log_info("========================================================")

    # C++ 함수 호출
    if hasattr(target_actor, "generate_targets"):
        target_actor.generate_targets()
    elif hasattr(target_actor, "generate_environment"):
        target_actor.generate_environment()

    log_info("🎉 지형 위 차량/표적 배치가 성공적으로 완료되었습니다!")


def main():
    log_info("========================================================")
    log_step(1, "TargetGenActor 차량 표적 배치 파이프라인 시작...")
    log_info("========================================================")

    actor_subsystem, asset_lib, asset_reg = get_editor_context()

    all_actors, spawn_location, area_extent = resolve_spawn_location_and_bounds(actor_subsystem)
    target_actor = get_or_spawn_target_actor(actor_subsystem, asset_lib, all_actors, spawn_location)
    apply_target_params(target_actor, area_extent)

    elements_array = build_target_elements(asset_reg, asset_lib)

    # C++ 프로퍼티 매핑 (TargetElements 또는 EnvElements)
    try:
        target_actor.set_editor_property("TargetElements", elements_array)
    except Exception:
        target_actor.set_editor_property("EnvElements", elements_array)

    generate_targets(target_actor)


if __name__ == "__main__":
    main()