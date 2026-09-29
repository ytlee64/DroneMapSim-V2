# Copyright Epic Games, Inc. All Rights Reserved.
# Simple YOLO Dataset Viewer with Bounding Box Visualizer

import os
import glob
import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk, ImageDraw, ImageFont

class YoloDatasetViewer:
    def __init__(self, root):
        self.root = root
        self.root.title("Drone Dataset YOLO Bounding Box Viewer")
        self.root.geometry("1100x800")
        self.root.minsize(800, 600)

        # Class ID to Name and Color Mapping
        self.class_names = {
            0: "Vehicle",
            1: "Truck",
            2: "Drone",
            3: "Target"
        }
        self.class_colors = {
            0: "#00FF00",  # Bright Green
            1: "#00FFFF",  # Cyan
            2: "#FF00FF",  # Magenta
            3: "#FFFF00"   # Yellow
        }

        self.image_files = []
        self.current_index = 0
        self.current_pil_image = None
        self.display_tk_image = None

        # Build UI layout
        self.create_widgets()

        # Try default Saved/DroneCaptures directory if exists
        default_dir = os.path.join(os.getcwd(), "Saved", "DroneCaptures")
        if os.path.exists(default_dir):
            self.load_directory(default_dir)

    def create_widgets(self):
        # Top Control Bar
        control_frame = tk.Frame(self.root, bg="#2E2E2E", pady=8)
        control_frame.pack(side=tk.TOP, fill=tk.X)

        btn_open = tk.Button(control_frame, text="Open Folder", command=self.select_folder,
                             bg="#4A4A4A", fg="white", font=("Arial", 10, "bold"), padx=10)
        btn_open.pack(side=tk.LEFT, padx=10)

        self.btn_prev = tk.Button(control_frame, text="< Prev", command=self.show_prev,
                                  bg="#4A4A4A", fg="white", state=tk.DISABLED, width=8)
        self.btn_prev.pack(side=tk.LEFT, padx=5)

        self.btn_next = tk.Button(control_frame, text="Next >", command=self.show_next,
                                  bg="#4A4A4A", fg="white", state=tk.DISABLED, width=8)
        self.btn_next.pack(side=tk.LEFT, padx=5)

        self.lbl_info = tk.Label(control_frame, text="No folder selected",
                                 bg="#2E2E2E", fg="#CCCCCC", font=("Arial", 10))
        self.lbl_info.pack(side=tk.LEFT, padx=15)

        # Main Canvas Area
        canvas_frame = tk.Frame(self.root, bg="#1E1E1E")
        canvas_frame.pack(side=tk.TOP, fill=tk.BOTH, expand=True)

        self.canvas = tk.Canvas(canvas_frame, bg="#1E1E1E", highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True)
        self.canvas.bind("<Configure>", self.on_window_resize)

        # Bottom Status Bar
        self.status_bar = tk.Label(self.root, text="Ready", bd=1, relief=tk.SUNKEN,
                                   anchor=tk.W, bg="#333333", fg="white", font=("Arial", 9))
        self.status_bar.pack(side=tk.BOTTOM, fill=tk.X)

        # Keyboard bindings
        self.root.bind("<Left>", lambda e: self.show_prev())
        self.root.bind("<Right>", lambda e: self.show_next())

    def select_folder(self):
        folder = filedialog.askdirectory(title="Select DroneCaptures Folder")
        if folder:
            self.load_directory(folder)

    def load_directory(self, folder_path):
        # Search for PNG images
        pattern = os.path.join(folder_path, "IMG_*.png")
        self.image_files = sorted(glob.glob(pattern))

        if not self.image_files:
            # Fallback to any PNG
            self.image_files = sorted(glob.glob(os.path.join(folder_path, "*.png")))

        if not self.image_files:
            messagebox.showinfo("No Images Found", f"No PNG files found in:\n{folder_path}")
            return

        self.current_index = 0
        self.btn_prev.config(state=tk.NORMAL)
        self.btn_next.config(state=tk.NORMAL)
        self.display_current_image()

    def show_prev(self):
        if self.image_files and self.current_index > 0:
            self.current_index -= 1
            self.display_current_image()

    def show_next(self):
        if self.image_files and self.current_index < len(self.image_files) - 1:
            self.current_index += 1
            self.display_current_image()

    def load_yolo_labels(self, img_path):
        txt_path = os.path.splitext(img_path)[0] + ".txt"
        boxes = []
        if not os.path.exists(txt_path):
            return boxes

        try:
            with open(txt_path, "r", encoding="utf-8") as f:
                for line in f:
                    parts = line.strip().split()
                    if len(parts) == 5:
                        class_id = int(parts[0])
                        cx = float(parts[1])
                        cy = float(parts[2])
                        w = float(parts[3])
                        h = float(parts[4])
                        boxes.append((class_id, cx, cy, w, h))
        except Exception as e:
            print(f"[Error] Failed to parse {txt_path}: {e}")

        return boxes

    def draw_bounding_boxes(self, pil_image, boxes):
        draw = ImageDraw.Draw(pil_image)
        img_w, img_h = pil_image.size

        # Try default font
        font = None
        try:
            font = ImageFont.truetype("arial.ttf", 16)
        except:
            font = ImageFont.load_default()

        for class_id, cx, cy, w, h in boxes:
            # Convert YOLO normalized format to pixel coordinates
            box_w = w * img_w
            box_h = h * img_h
            box_x1 = (cx * img_w) - (box_w * 0.5)
            box_y1 = (cy * img_h) - (box_h * 0.5)
            box_x2 = box_x1 + box_w
            box_y2 = box_y1 + box_h

            color = self.class_colors.get(class_id, "#00FF00")
            class_name = self.class_names.get(class_id, f"Class {class_id}")
            label_text = f"{class_name} ({class_id})"

            # Draw outer rectangle
            line_width = max(2, int(img_w / 600))
            draw.rectangle([box_x1, box_y1, box_x2, box_y2], outline=color, width=line_width)

            # Draw label background
            text_pos = (box_x1 + 3, max(0, box_y1 - 20))
            draw.rectangle([text_pos[0] - 2, text_pos[1] - 2, text_pos[0] + 110, text_pos[1] + 18], fill=color)
            draw.text(text_pos, label_text, fill="black", font=font)

        return len(boxes)

    def display_current_image(self):
        if not self.image_files:
            return

        img_path = self.image_files[self.current_index]
        file_name = os.path.basename(img_path)

        # Update Top Header
        self.lbl_info.config(text=f"[{self.current_index + 1}/{len(self.image_files)}]  {file_name}")

        try:
            # Load fresh image
            base_image = Image.open(img_path).convert("RGB")
        except Exception as e:
            self.status_bar.config(text=f"Failed to open image: {e}")
            return

        # Load YOLO boxes and draw on image
        boxes = self.load_yolo_labels(img_path)
        box_count = self.draw_bounding_boxes(base_image, boxes)
        self.current_pil_image = base_image

        # Scale image to fit current canvas size
        self.render_to_canvas()

        # Update bottom status bar
        txt_path = os.path.splitext(img_path)[0] + ".txt"
        has_label = "Found" if os.path.exists(txt_path) else "Missing"
        self.status_bar.config(text=f"Resolution: {base_image.width}x{base_image.height} | Targets: {box_count} | Label File: {has_label} ({os.path.basename(txt_path)})")

    def render_to_canvas(self):
        if self.current_pil_image is None:
            return

        canvas_w = self.canvas.winfo_width()
        canvas_h = self.canvas.winfo_height()

        if canvas_w < 50 or canvas_h < 50:
            return

        img_w, img_h = self.current_pil_image.size

        # Maintain aspect ratio
        ratio = min(canvas_w / img_w, canvas_h / img_h)
        new_w = max(1, int(img_w * ratio))
        new_h = max(1, int(img_h * ratio))

        resized_img = self.current_pil_image.resize((new_w, new_h), Image.Resampling.BILINEAR)
        self.display_tk_image = ImageTk.PhotoImage(resized_img)

        # Center in canvas
        self.canvas.delete("all")
        pos_x = (canvas_w - new_w) // 2
        pos_y = (canvas_h - new_h) // 2
        self.canvas.create_image(pos_x, pos_y, anchor=tk.NW, image=self.display_tk_image)

    def on_window_resize(self, event):
        self.render_to_canvas()

if __name__ == "__main__":
    root = tk.Tk()
    app = YoloDatasetViewer(root)
    root.mainloop()