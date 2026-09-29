"""
================================================================================
 파일명: 00_import_texture.py
 설명: 외부 위성 PNG 파일을 언리얼 엔진 Texture2D 에셋으로 임포트하고,
       C++ 산림 생성기에서 CPU 픽셀을 고속으로 읽을 수 있도록 설정을 보정합니다.
       외부 위성 PNG 파일을 'T_Mauntain'이라는 이름의 Texture2D 에셋으로 임포트합니다.
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


def import_texture_asset():
    # ==============================================================================
    # 2. 텍스처 임포트 (이름 지정)
    # ==============================================================================
    log_step(1, f"텍스처 임포트 시작: {config.SATELLITE_IMAGE_FILE_PATH} -> {config.TEXTURE_NAME}")

    import_task = unreal.AssetImportTask()
    import_task.set_editor_property("filename", config.SATELLITE_IMAGE_FILE_PATH)
    import_task.set_editor_property("destination_path", config.PACKAGE_PATH)
    import_task.set_editor_property("destination_name", config.TEXTURE_NAME) # 🌟 핵심: 에셋 이름 강제 지정!
    import_task.set_editor_property("save", True)
    import_task.set_editor_property("replace_existing", True)
    import_task.set_editor_property("automated", True)

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([import_task])


def apply_no_mipmaps():
    # ==============================================================================
    # 3. 설정 보정 (NoMipmaps)
    # ==============================================================================
    asset_full_path = f"{config.PACKAGE_PATH}/{config.TEXTURE_NAME}"
    loaded_texture = unreal.EditorAssetLibrary.load_asset(asset_full_path)

    if loaded_texture:
        # C++ CPU 픽셀 읽기를 위한 NoMipmaps 설정
        loaded_texture.set_editor_property("mip_gen_settings", unreal.TextureMipGenSettings.TMGS_NO_MIPMAPS)
        unreal.EditorAssetLibrary.save_loaded_asset(loaded_texture)

        log_step(2, f"[성공] '{config.TEXTURE_NAME}' 텍스처 에셋이 성공적으로 생성되었습니다!")
        print(f">>> 에셋 전체 경로: {asset_full_path}")
        return

    print(f">>> [오류] 텍스처 생성에 실패했습니다. 경로를 확인하세요: {config.SATELLITE_IMAGE_FILE_PATH}")
    raise FileNotFoundError("Texture import failed")


def main():
    import_texture_asset()
    apply_no_mipmaps()

if __name__ == "__main__":
    main()