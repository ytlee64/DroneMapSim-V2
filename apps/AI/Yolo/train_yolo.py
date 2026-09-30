# Copyright Epic Games, Inc. All Rights Reserved.
# YOLO Model Training Script on Drone Synthetic Dataset

from ultralytics import YOLO
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
RUNS_DETECT_DIR = os.path.join(SCRIPT_DIR, "runs", "detect")
WEIGHTS_DIR = os.path.join(SCRIPT_DIR, "weights")
BASE_WEIGHTS_PATH = os.path.join(WEIGHTS_DIR, "yolov8n.pt")
LEGACY_ROOT_WEIGHTS_PATH = os.path.abspath(os.path.join(SCRIPT_DIR, "..", "..", "yolov8n.pt"))


def resolve_base_weights_path():
    os.makedirs(WEIGHTS_DIR, exist_ok=True)

    if not os.path.exists(BASE_WEIGHTS_PATH) and os.path.exists(LEGACY_ROOT_WEIGHTS_PATH):
        os.replace(LEGACY_ROOT_WEIGHTS_PATH, BASE_WEIGHTS_PATH)
        print(f">>> Moved legacy weights to: {BASE_WEIGHTS_PATH}")

    return BASE_WEIGHTS_PATH

def main():
    yaml_config = os.path.join(SCRIPT_DIR, "YoloDataset", "data.yaml")
    if not os.path.exists(yaml_config):
        print(f"[Error] Config file '{yaml_config}' not found. Run prepare_dataset.py first!")
        return

    print("==========================================================")
    print(">>> INITIALIZING YOLO TRAINING PIPELINE")
    print("==========================================================")

    # 1. Load Pretrained Lightweight Model (yolov8n.pt or yolo11n.pt)
    # Keep base weights under apps/AI/Yolo/weights to avoid root-level files.
    model = YOLO(resolve_base_weights_path())

    # 2. Start Training
    # imgsz=640: Standard high-speed input resolution (or 1280 for tiny vehicles)
    # epochs=50: Sufficient for 1,000 synthetic drone dataset
    # batch=16: Standard batch size (reduce to 8 if GPU VRAM is small)
    results = model.train(
        data=yaml_config,
        epochs=50,
        imgsz=640,
        batch=16,
        project=RUNS_DETECT_DIR,
        name="drone_vehicle_model",
        device=0,          # Set 0 for GPU, or 'cpu' if no dedicated GPU
        workers=4,
        patience=15,       # Early stopping if no improvement for 15 epochs
        save=True,
        plots=True
    )

    print("\n==========================================================")
    print(">>> TRAINING COMPLETE!")
    best_weight_path = os.path.join(RUNS_DETECT_DIR, "drone_vehicle_model", "weights", "best.pt")
    print(f">>> Best weights saved at: {best_weight_path}")
    print("==========================================================")

    # 3. Validate Best Model
    metrics = model.val()
    print(f">>> Final Validation mAP50: {metrics.box.map50:.4f}")
    print(f">>> Final Validation mAP50-95: {metrics.box.map:.4f}")

if __name__ == "__main__":
    main()