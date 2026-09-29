"""
================================================================================
 파일명: 01_create_mountain_material.py
 설명: 00번에서 생성된 'T_Mauntain' 텍스처를 불러와
       LandscapeLayerCoords(614.4) + Y축 반전(1-V) 머티리얼(M_Mountain)만 생성합니다.
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


def load_satellite_texture():
    # ==============================================================================
    # 2. 00번에서 만든 텍스처 에셋 불러오기
    # ==============================================================================
    texture_asset_path = f"{config.PACKAGE_PATH}/{config.TEXTURE_NAME}"
    satellite_texture = unreal.EditorAssetLibrary.load_asset(texture_asset_path)

    if not satellite_texture:
        print(f">>> [오류] '{texture_asset_path}' 텍스처를 찾을 수 없습니다. 00번 스크립트를 먼저 실행하세요!")
        raise FileNotFoundError("Texture not found")

    log_step(1, f"텍스처 로드 성공: {texture_asset_path}")
    return satellite_texture


def build_material_graph(my_material, satellite_texture):
    # 1) Texture Sample 노드 생성
    tex_sample = unreal.MaterialEditingLibrary.create_material_expression(
        my_material, unreal.MaterialExpressionTextureSample, -200, 0
    )
    tex_sample.set_editor_property("texture", satellite_texture)

    # 2) LandscapeLayerCoords 노드 생성 (Mapping Scale: 614.4)
    layer_coords = unreal.MaterialEditingLibrary.create_material_expression(
        my_material, unreal.MaterialExpressionLandscapeLayerCoords, -800, 0
    )
    layer_coords.set_editor_property("mapping_scale", config.MAPPING_SCALE)

    # 3) R 채널 분리 (X / U 좌표)
    mask_x = unreal.MaterialEditingLibrary.create_material_expression(
        my_material, unreal.MaterialExpressionComponentMask, -600, -100
    )
    mask_x.set_editor_property("r", True)
    mask_x.set_editor_property("g", False)
    mask_x.set_editor_property("b", False)
    mask_x.set_editor_property("a", False)
    unreal.MaterialEditingLibrary.connect_material_expressions(layer_coords, "", mask_x, "")

    # 4) G 채널 분리 (Y / V 좌표)
    mask_y = unreal.MaterialEditingLibrary.create_material_expression(
        my_material, unreal.MaterialExpressionComponentMask, -600, 100
    )
    mask_y.set_editor_property("r", False)
    mask_y.set_editor_property("g", True)
    mask_y.set_editor_property("b", False)
    mask_y.set_editor_property("a", False)
    unreal.MaterialEditingLibrary.connect_material_expressions(layer_coords, "", mask_y, "")

    # 5) OneMinus 노드 (Y축 반전: 1 - V)
    one_minus = unreal.MaterialEditingLibrary.create_material_expression(
        my_material, unreal.MaterialExpressionOneMinus, -450, 100
    )
    unreal.MaterialEditingLibrary.connect_material_expressions(mask_y, "", one_minus, "")

    # 6) AppendVector (X와 반전된 Y 결합)
    append_uv = unreal.MaterialEditingLibrary.create_material_expression(
        my_material, unreal.MaterialExpressionAppendVector, -350, 0
    )
    unreal.MaterialEditingLibrary.connect_material_expressions(mask_x, "", append_uv, "A")
    unreal.MaterialEditingLibrary.connect_material_expressions(one_minus, "", append_uv, "B")

    # 7) Texture Sample의 UV 입력핀에 연결
    unreal.MaterialEditingLibrary.connect_material_expressions(append_uv, "", tex_sample, "UVs")

    # 8) Base Color에 최종 연결
    unreal.MaterialEditingLibrary.connect_material_property(
        tex_sample, "RGB", unreal.MaterialProperty.MP_BASE_COLOR
    )


def create_mountain_material(satellite_texture):
    # ==============================================================================
    # 3. 순수 머티리얼 에셋 생성 및 노드 그래프 조립
    # ==============================================================================
    log_step(2, f"머티리얼 '{config.MATERIAL_NAME}' 생성 및 노드 연결 중...")

    asset_tools = unreal.AssetToolsHelpers.get_asset_tools()
    material_factory = unreal.MaterialFactoryNew()
    material_asset_path = f"{config.PACKAGE_PATH}/{config.MATERIAL_NAME}"

    # 기존 머티리얼이 있으면 삭제 후 재생성
    if unreal.EditorAssetLibrary.does_asset_exist(material_asset_path):
        unreal.EditorAssetLibrary.delete_asset(material_asset_path)

    # 새 머티리얼 에셋 생성
    my_material = asset_tools.create_asset(
        config.MATERIAL_NAME,
        config.PACKAGE_PATH,
        unreal.Material,
        material_factory
    )

    if not my_material:
        raise RuntimeError(f"Material creation failed: {material_asset_path}")

    build_material_graph(my_material, satellite_texture)
    unreal.MaterialEditingLibrary.recompile_material(my_material)
    unreal.EditorAssetLibrary.save_loaded_asset(my_material)
    print(f">>> [성공] 머티리얼 에셋 '{material_asset_path}' 생성이 완벽하게 끝났습니다! 🎉")


def main():
    satellite_texture = load_satellite_texture()
    create_mountain_material(satellite_texture)

if __name__ == "__main__":
    main()