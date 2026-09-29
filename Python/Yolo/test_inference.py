# Copyright Epic Games, Inc. All Rights Reserved.
# Smart Test Inference Script for Drone Dataset

from ultralytics import YOLO
import glob
import os
import sys

def get_best_weights_path():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.abspath(os.path.join(script_dir, "..", ".."))

    # 우선 1순위: 방금 생성된 -4 가중치 확인
    specific_path = os.path.join(script_dir, "runs", "detect", "drone_vehicle_model-4", "weights", "best.pt")
    if os.path.exists(specific_path):
        return specific_path

    # 2순위: 기본 고정 경로 확인
    default_path = os.path.join(script_dir, "runs", "detect", "drone_vehicle_model", "weights", "best.pt")
    if os.path.exists(default_path):
        return default_path

    # 3순위: runs/detect 안의 모든 best.pt 중 가장 최근 파일 자동 탐색
    search_pattern = os.path.join(script_dir, "runs", "detect", "**", "best.pt")
    all_weights = glob.glob(search_pattern, recursive=True)

    if not all_weights:
        # 프로젝트 루트까지 확장 검색
        all_weights = glob.glob(os.path.join(project_root, "runs", "detect", "**", "best.pt"), recursive=True)

    if not all_weights:
        return None

    # 가장 최근에 수정된 파일 선택
    all_weights.sort(key=os.path.getmtime, reverse=True)
    return all_weights[0]

def main():
    best_pt = get_best_weights_path()

    if not best_pt or not os.path.exists(best_pt):
        print("[Error] Could not find any trained 'best.pt' weights!")
        return

    print("==========================================================")
    print(f">>> [Auto-Detected] Loading Weights: {best_pt}")
    print("==========================================================")

    model = YOLO(best_pt)

    script_dir = os.path.dirname(os.path.abspath(__file__))
    val_images_dir = os.path.join(script_dir, "YoloDataset", "images", "val")

    output_dir = os.path.join(script_dir, "runs", "inference")
    
    # 검증셋 이미지들에 대해 추론 실행
    results = model.predict(
        source=val_images_dir,
        save=True,
        conf=0.35,            # 신뢰도 35% 이상만 박스 표시
        project=output_dir,
        name="val_evaluation",
        exist_ok=True
    )

    print("\n==========================================================")
    print(">>> INFERENCE COMPLETE!")
    print(f">>> Results saved in: {os.path.join(output_dir, 'val_evaluation')}")
    print("==========================================================")

if __name__ == "__main__":
    main()