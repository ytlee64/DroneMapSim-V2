# Copyright Epic Games, Inc. All Rights Reserved.
# Automated YOLO Dataset Splitter and YAML Generator

import os
import glob
import shutil
import random
import yaml

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, ".."))

def prepare_yolo_dataset(
    source_dir=None,
    output_dir=None,
    train_ratio=0.8,
    seed=42
):
    random.seed(seed)

    if source_dir is None:
        source_dir = os.path.join(PROJECT_ROOT,"engine" ,"Saved", "DroneCaptures")
    if output_dir is None:
        output_dir = os.path.join(SCRIPT_DIR, "YoloDataset")

    source_dir = os.path.abspath(source_dir)
    output_dir = os.path.abspath(output_dir)

    if not os.path.exists(source_dir):
        print(f"[Error] Source directory '{source_dir}' does not exist.")
        return

    # 1. Find all PNG images
    image_files = sorted(glob.glob(os.path.join(source_dir, "IMG_*.png")))
    if not image_files:
        image_files = sorted(glob.glob(os.path.join(source_dir, "*.png")))

    total_images = len(image_files)
    if total_images == 0:
        print(f"[Error] No PNG images found in '{source_dir}'.")
        return

    print(f">>> Found {total_images} raw captured images in '{source_dir}'.")

    # 2. Create YOLO target directories
    sub_dirs = [
        os.path.join(output_dir, "images", "train"),
        os.path.join(output_dir, "images", "val"),
        os.path.join(output_dir, "labels", "train"),
        os.path.join(output_dir, "labels", "val"),
    ]

    for d in sub_dirs:
        os.makedirs(d, exist_ok=True)

    # 3. Shuffle and split into Train and Val sets
    random.shuffle(image_files)
    train_count = int(total_images * train_ratio)
    train_files = image_files[:train_count]
    val_files = image_files[train_count:]

    print(f">>> Splitting: Train = {len(train_files)} images, Val = {len(val_files)} images")

    def copy_subset(files, subset_name):
        copied_cnt = 0
        for img_path in files:
            base_name = os.path.basename(img_path)
            stem = os.path.splitext(base_name)[0]
            txt_path = os.path.join(os.path.dirname(img_path), stem + ".txt")

            # Copy Image
            dest_img = os.path.join(output_dir, "images", subset_name, base_name)
            shutil.copy2(img_path, dest_img)

            # Copy or Create Label
            dest_txt = os.path.join(output_dir, "labels", subset_name, stem + ".txt")
            if os.path.exists(txt_path):
                shutil.copy2(txt_path, dest_txt)
            else:
                # Create empty label file for negative samples
                with open(dest_txt, "w") as f:
                    pass

            copied_cnt += 1
        return copied_cnt

    print(">>> Copying train images and labels...")
    copy_subset(train_files, "train")

    print(">>> Copying validation images and labels...")
    copy_subset(val_files, "val")

    # 4. Generate data.yaml configuration
    abs_output_dir = output_dir.replace("\\", "/")
    data_yaml_content = {
        "path": abs_output_dir,
        "train": "images/train",
        "val": "images/val",
        "names": {
            0: "vehicle"
        }
    }

    yaml_path = os.path.join(output_dir, "data.yaml")
    with open(yaml_path, "w", encoding="utf-8") as f:
        yaml.dump(data_yaml_content, f, sort_keys=False)

    print(f">>> SUCCESS: Dataset prepared at: {abs_output_dir}")
    print(f">>> Configuration YAML generated: {yaml_path}")

if __name__ == "__main__":
    prepare_yolo_dataset()