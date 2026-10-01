# Copyright Epic Games, Inc. All Rights Reserved.
# YOLO Model Training & ONNX Export Script on Drone Synthetic Dataset

from ultralytics import YOLO
import os
import shutil

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
    model = YOLO(resolve_base_weights_path())

    # Clear previous training results if they exist
    if os.path.exists(RUNS_DETECT_DIR):
        print(f">>> Removing previous results from: {RUNS_DETECT_DIR}")
        shutil.rmtree(RUNS_DETECT_DIR)

    # 2. Start Training
    results = model.train(
        data=yaml_config,
        epochs=50,
        imgsz=640,
        batch=16,
        project=RUNS_DETECT_DIR,
        name="drone_vehicle_model",
        exist_ok=True,
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

    # ==========================================================
    # ⭐️ 4. Export Best Model to ONNX Format
    # ==========================================================
    print("\n==========================================================")
    print(">>> EXPORTING BEST MODEL TO ONNX...")
    print("==========================================================")
    
    if os.path.exists(best_weight_path):
        # 최적의 가중치 모델 로드
        best_model = YOLO(best_weight_path)
        
        # ONNX 변환 (opset=12는 OpenCV, TensorRT, C++ NNE와 가장 호환성이 뛰어남)
        exported_onnx_path = best_model.export(
            format="onnx",
            imgsz=640,
            opset=12,          # 표준 ONNX 연산자 세트
            dynamic=False,      # 고정 해상도 (C++ 추론 시 속도 최적화)
            simplify=True       # 불필요한 노드 제거 및 최적화
        )
        
        # 관리하기 편하게 weights 폴더로 복사본 저장
        final_deploy_onnx = os.path.join(WEIGHTS_DIR, "best.onnx")
        shutil.copy2(exported_onnx_path, final_deploy_onnx)

        print("==========================================================")
        print(f">>> ONNX EXPORT SUCCESSFUL!")
        print(f">>> 1. Run Artifact : {exported_onnx_path}")
        print(f">>> 2. Deploy Copy  : {final_deploy_onnx}")
        print("==========================================================")
    else:
        print(f"[Error] Best weight file not found at: {best_weight_path}")

if __name__ == "__main__":
    main()