# ==============================================================================
# 1. 원본 소스 파일 경로
# ==============================================================================
SATELLITE_IMAGE_FILE_PATH = "D:\\ytlee\\pywork\\MapData\\satellite_data\\Mauntain.png"
SOURCE_HEIGHTMAP_PNG = "D:\\ytlee\\pywork\\MapData\\dem_data\\Mauntain.png"

# ==============================================================================
# 2. 언리얼 에셋 패키지 경로 및 이름
# ==============================================================================
PACKAGE_PATH = "/Game/LandscapeData"
TEXTURE_NAME = "T_Mauntain"
MATERIAL_NAME = "M_Mauntain"
MAPPING_SCALE = 61440.0

# ==============================================================================
# 3. 관차용 드론 Actor 및 환경 생성기 액터 이름
# ==============================================================================
TARGET_NAMES = {
    "Drone": "BP_DronePawn",
    "ScanPyramid": "BP_ScanPyramid",
    "Observer": "BP_ObserverPawn",
    "GameMode": "BP_DroneMapSimGameMode",
    "TargetGenerator": "TargetGenActor",  
    "ScanGround": "M_ScanGround",
    "ScanProjection": "M_ScanProjection",
    "RenderTarget": "RT_DroneCapture"
}


# ==============================================================================
# 4. C++ 식생 생성기 기본 파라미터
# ==============================================================================
ENV_GEN_PARAMS = {
    "GridSpacing": 1200.0,          # 격자 기본 간격 (cm)
    "PositionJitter": 450.0,        # 위치 무작위 지터 (cm)
    "AlignToSurfaceNormal": 0.0,    # 지형 법선 정렬 (0.0 = 수직 기립, 1.0 = 지형 밀착)
    "EnableWaterExclusion": True,   # WaterBody 영역 위 스폰 제거
    "WaterXYMarginCm": 200.0,       # 워터 경계 바깥 여유(양수=더 넓게 제외)
}


# ==============================================================================
# 5. 다중 식생(수종) 리스트 설정
# ==============================================================================

TARGET_MESH_PATH = "/Game/LevelPrototyping/Meshes"


TARGET_ELEMENT_CONFIGS = [
    {
        "name": "세단",
        "mesh_name": "SM_Dummy_Vehicle_Sedan_01",
        "mesh_path": f"{TARGET_MESH_PATH}/SM_Dummy_Vehicle_Sedan_01",
        "weight": 0.01,         # 희소 배치
        "max_slope": 15.0,     # 완만한 평지/도로에만 배치
        "scale_min": 1.0,
        "scale_max": 1.0,
        "b_is_target": True,
        "target_class_id": 0  # 0: Vehicle / Car
    },
    {
        "name": "밴",
        "mesh_name": "SM_Dummy_Vehicle_Van_01",
        "mesh_path": f"{TARGET_MESH_PATH}/SM_Dummy_Vehicle_Van_01",
        "weight": 0.01,         # 희소 배치
        "max_slope": 15.0,     # 완만한 평지/도로에만 배치
        "scale_min": 1.0,
        "scale_max": 1.0,
        "b_is_target": False,
        "target_class_id": -1
    } 
]
