# Copyright Epic Games, Inc. All Rights Reserved.
# Smart Test Inference Script for Drone Dataset

from ultralytics import YOLO
import os
import sys


def summarize_detection_stats(results):
    total_images = len(results)
    if total_images == 0:
        print(">>> [Stats] No inference results were produced.")
        return

    detected_images = 0
    no_detection_images = 0
    boxes_per_detected = []

    for result in results:
        box_count = len(result.boxes) if result.boxes is not None else 0
        if box_count > 0:
            detected_images += 1
            boxes_per_detected.append(box_count)
        else:
            no_detection_images += 1

    detection_rate = (detected_images / total_images) * 100.0 if total_images else 0.0
    avg_boxes = (sum(boxes_per_detected) / len(boxes_per_detected)) if boxes_per_detected else 0.0

    print("\n==========================================================")
    print(">>> [Detection Summary]")
    print(f">>> Total images: {total_images}")
    print(f">>> Detected images: {detected_images}")
    print(f">>> No detection images: {no_detection_images}")
    print(f">>> Detection rate: {detection_rate:.1f}%")
    print(f">>> Average boxes per detected image: {avg_boxes:.2f}")
    print("==========================================================")

def get_best_weights_path():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    weights_dir = os.path.join(script_dir, "weights")

    candidate_paths = [
        os.path.join(weights_dir, "best.onnx"),
        os.path.join(weights_dir, "best.pt"),
    ]

    for candidate in candidate_paths:
        if os.path.exists(candidate):
            return candidate

    return None

def main():
    best_pt = get_best_weights_path()

    if not best_pt or not os.path.exists(best_pt):
        print("[Error] Could not find any trained model (best.onnx / best.pt) in the YOLO weights folder!")
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

    result_dir = os.path.join(output_dir, 'val_evaluation')

    print("\n==========================================================")
    print(">>> INFERENCE COMPLETE!")
    print(f">>> Results saved in: {result_dir}")
    print("==========================================================")

    summarize_detection_stats(results)

if __name__ == "__main__":
    main()