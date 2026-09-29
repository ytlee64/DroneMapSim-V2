"""
================================================================================
 파일명: 02_apply_landscape_material.py
 설명: 월드의 랜드스케이프를 찾아 120% 스케일(614.4m)로 보정하고,
       01번에서 생성한 'M_Mountain' 머티리얼을 지형에 장착합니다.
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

TOTAL_STEPS = 2


def log_step(step, msg):
    print(f">>> [{step}/{TOTAL_STEPS}] {msg}")
 
def load_mountain_material():
    # ==============================================================================
    # 1. 불러올 머티리얼 경로
    # ==============================================================================
    material_asset_path = f"{config.PACKAGE_PATH}/{config.MATERIAL_NAME}"
    mountain_material = unreal.EditorAssetLibrary.load_asset(material_asset_path)

    if not mountain_material:
        print(f">>> [오류] '{material_asset_path}' 머티리얼이 없습니다. 01번 스크립트를 먼저 실행하세요!")
        raise FileNotFoundError("Material not found")

    log_step(1, f"머티리얼 로드 성공: {material_asset_path}")
    return mountain_material


def apply_material_to_landscape(mountain_material):
    # ==============================================================================
    # 2. 월드에서 랜드스케이프 검색 및 머티리얼 장착
    # ==============================================================================
    log_step(2, "월드의 랜드스케이프를 검색하는 중...")
    all_actors = unreal.EditorLevelLibrary.get_all_level_actors()
    target_landscape = None

    for actor in all_actors:
        if isinstance(actor, unreal.Landscape) or isinstance(actor, unreal.LandscapeProxy):
            target_landscape = actor
            break

    if target_landscape:
        # 1:1 현실 스케일: 가로세로 120% (614.4m), 높이 100%
        target_landscape.set_actor_location(unreal.Vector(0.0, 0.0, 0.0), False, False)
        target_landscape.set_actor_scale3d(unreal.Vector(120.0, 120.0, 100.0))

        # 랜드스케이프 머티리얼 슬롯에 장착
        target_landscape.set_editor_property("landscape_material", mountain_material)

        print(f">>> [성공] 랜드스케이프 '{target_landscape.get_name()}'에 '{config.MATERIAL_NAME}' 머티리얼을 장착했습니다! 🎉")
        print(f">>> 위치: (0,0,0) | 스케일: (120, 120, 100)")
        return

    print(">>> [오류] 월드에 랜드스케이프가 없습니다. 지형(Shift+2)을 먼저 생성해 주세요.")
    raise RuntimeError("Landscape not found")


def main():
    mountain_material = load_mountain_material()
    apply_material_to_landscape(mountain_material)


if __name__ == "__main__":
    main()