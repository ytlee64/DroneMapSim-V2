"""
========================================================================================
🛰️ DroneMapSim - free_flight_digital_mapper.py
[1단계] 자율 비행 미션 관제 & 항공 정사 데이터 수집 GCS (Flight & Data Acquisition)
========================================================================================
- 기능:
  1) [TELEPORT] 수색 시작점(-50m, -40m) 즉시 배치 (물리 선속도/각속도 0 리셋)
  2) 고도 맞춤 속도 제어 (65m 고도: 18m/s, 500m 고도: 60m/s 자동 가속)
  3) ㄹ자 자율 스윕 순항 및 10Hz 텔레메트리 실시간 추적
  4) 언리얼 씬 캡처와 텔레메트리(좌표, 기체자세, 짐벌피치) 1:1 동기화 레코딩
  5) 실시간 YOLOv8 표적 탐지 및 3D 지리참조 (Georeferencing)
  6) ⭐️ 비행 완료 시 후처리용 표준 데이터셋 매니페스트(survey_manifest_*.json) 저장
========================================================================================
* 정사 모자이크 영상 합성은 비행 종료 후 독립 후처리 스크립트(build_aerial_orthomosaic.py)에서 수행합니다.
========================================================================================
"""

import socket
import json
import time
import math
import os
import glob
import cv2
from datetime import datetime
from ultralytics import YOLO

# ======================================================================================
# ⚙️ 1. GCS 통신 & 디렉터리 환경 설정
# ======================================================================================
UE5_IP = "127.0.0.1"
CMD_PORT = 9000       # Python GCS -> UE5 Commands (TELEPORT, FLY_TO, SET_GIMBAL 등)
TELEM_PORT = 9001     # UE5 -> Python GCS Telemetry Stream (10Hz)

# 언리얼 엔진 고해상도 스크린샷 저장 경로
CAPTURED_FRAMES_DIR = r"d:\ytlee\ureal\DroneMapSim\Saved\Screenshots\WindowsEditor"
# YOLOv8 학습 가중치 파일 경로
YOLO_MODEL_PATH = r"d:\ytlee\ureal\DroneMapSim\Python\Yolo\runs\detect\train\weights\best.pt"
# 수집된 미션 데이터셋 매니페스트 저장 폴더
OUTPUT_DATA_DIR = r"d:\ytlee\ureal\DroneMapSim\Python\TacticalMaps"

# 카메라 광학 파라미터 (UE5 CineCameraComponent 기본 세팅)
CAMERA_FOV_H = 90.0   # 수평 화각 Horizontal FOV (Deg)
IMG_WIDTH = 1920      # 캡처 해상도 Width (px)
IMG_HEIGHT = 1080     # 캡처 해상도 Height (px)

# 탐지 대상 클래스 라벨 매핑 (YOLO 학습 모델 기준)
CLASS_NAMES = {0: "MIL_TRUCK", 1: "COMMAND_VAN", 2: "LIGHT_VEHICLE", 3: "CAMO_NET"}


class FreeFlightDataCollector:
    def __init__(self, mode="survey", cruise_alt_m=65.0, high_speed_mode=False):
        """
        GCS 초기화: 소켓 바인딩, YOLOv8 가속 로딩, 데이터 저장소 준비
        :param mode: 'survey' (65m 정밀 수색) or 'landscape' (500m 광역 조감)
        :param cruise_alt_m: 비행 고도 (미터 단위, 기본 65.0m)
        :param high_speed_mode: True 시 500m 고도에서 60m/s로 가속
        """
        print("\n" + "=" * 75)
        print("🚀 [DroneMapSim] Free Flight GCS: 자율 비행 & 데이터 수집 엔진 초기화")
        print("=" * 75)

        self.cruise_alt_m = cruise_alt_m
        # 고도 500m 체감 속도 보정: 300m 이상일 경우 60m/s, 65m일 경우 18m/s
        self.cruise_speed_ms = 60.0 if (cruise_alt_m >= 300.0 or high_speed_mode) else 18.0
        self.gimbal_pitch = -85.0  # 정사 왜곡 최소화 수직 직하(Nadir) 각도

        os.makedirs(OUTPUT_DATA_DIR, exist_ok=True)
        if not os.path.exists(CAPTURED_FRAMES_DIR):
            os.makedirs(CAPTURED_FRAMES_DIR, exist_ok=True)

        # 1. UDP 소켓 세팅
        self.cmd_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.telem_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.telem_sock.bind(("0.0.0.0", TELEM_PORT))
        self.telem_sock.settimeout(0.3)
        print(f"📡 [NET] UDP Commands -> {UE5_IP}:{CMD_PORT} | Telemetry Listening on 0.0.0.0:{TELEM_PORT}")

        # 2. YOLOv8 객체 탐지 모델 로드 (CUDA 가속)
        print(f"🧠 [AI] YOLOv8 실시간 탐지 모델 로드: {YOLO_MODEL_PATH}")
        if os.path.exists(YOLO_MODEL_PATH):
            self.model = YOLO(YOLO_MODEL_PATH)
            print("   -> YOLOv8 CUDA 가속 모델 준비 완료!")
        else:
            print("   ⚠️ [경고] 로컬 가중치가 없어 기본 yolov8n.pt 모델을 다운로드하여 로드합니다.")
            self.model = YOLO("yolov8n.pt")

        # 3. 데이터셋 컨테이너 (후처리 모자이크용 매니페스트에 저장됨)
        self.survey_records = []     # 캡처 프레임별 정밀 지리참조 메타데이터
        self.confirmed_targets = []  # 실시간 식별된 표적 목록
        self.flight_logs = []        # 10Hz 전체 비행 궤적 히스토리

    # ==================================================================================
    # 📡 2. UDP JSON 제어 명령 송신부 (Port 9000)
    # ==================================================================================
    def send_command(self, cmd_dict):
        payload = json.dumps(cmd_dict).encode("utf-8")
        self.cmd_sock.sendto(payload, (UE5_IP, CMD_PORT))

    def teleport_drone(self, x_cm, y_cm, z_cm, yaw_deg=0.0):
        """[TELEPORT] 목표 좌표로 즉각 순간이동 (물리 선속도/각속도 리셋)"""
        cmd = {
            "cmd": "TELEPORT",
            "x": float(x_cm),
            "y": float(y_cm),
            "z": float(z_cm),
            "yaw": float(yaw_deg),
            "reset_velocity": True
        }
        self.send_command(cmd)

    def set_gimbal(self, pitch_deg=-85.0, yaw_deg=0.0, roll_deg=0.0):
        """[SET_GIMBAL] 카메라 짐벌 자세 제어"""
        self.gimbal_pitch = pitch_deg
        cmd = {
            "cmd": "SET_GIMBAL",
            "pitch": float(pitch_deg),
            "yaw": float(yaw_deg),
            "roll": float(roll_deg)
        }
        self.send_command(cmd)

    def fly_to(self, x_cm, y_cm, z_cm, speed_cm_s=1800.0, yaw_deg=0.0, accept_radius_cm=200.0):
        """[FLY_TO] 웨이포인트 물리 순항 항법 명령"""
        cmd = {
            "cmd": "FLY_TO",
            "x": float(x_cm),
            "y": float(y_cm),
            "z": float(z_cm),
            "yaw": float(yaw_deg),
            "speed": float(speed_cm_s),
            "accept_radius": float(accept_radius_cm)
        }
        self.send_command(cmd)

    def trigger_capture(self, filename=""):
        """[CAPTURE_IMAGE] 언리얼 엔진 프레임 캡처 트리거"""
        cmd = {
            "cmd": "CAPTURE_IMAGE",
            "filename": filename,
            "sync_telem": True
        }
        self.send_command(cmd)

    def hover_stop(self):
        """[HOVER] 제자리 비행 정지"""
        self.send_command({"cmd": "HOVER"})

    # ==================================================================================
    # 🛰️ 3. 10Hz 텔레메트리 수신 및 웨이포인트 감시 (Port 9001)
    # ==================================================================================
    def get_latest_telemetry(self):
        """수신 큐를 비워 가장 최신의 패킷만 즉시 반환"""
        latest_telem = None
        while True:
            try:
                data, _ = self.telem_sock.recvfrom(4096)
                latest_telem = json.loads(data.decode("utf-8"))
            except socket.timeout:
                break
            except Exception:
                break
        return latest_telem

    def parse_telemetry(self, telem):
        if not telem:
            return (0.0, 0.0, self.cruise_alt_m), (0.0, 0.0, 0.0), (self.gimbal_pitch, 0.0), False

        loc = telem.get("loc", [0.0, 0.0, self.cruise_alt_m * 100.0])
        rot = telem.get("rot", [0.0, 0.0, 0.0])
        gimbal = telem.get("gimbal", [self.gimbal_pitch, 0.0])
        reached = telem.get("reached", False)

        pos_m = (float(loc[0]) / 100.0, float(loc[1]) / 100.0, float(loc[2]) / 100.0)
        drone_rot = (float(rot[0]), float(rot[1]), float(rot[2]))
        gimbal_rot = (float(gimbal[0]), float(gimbal[1]))

        return pos_m, drone_rot, gimbal_rot, reached

    def record_synchronized_frame(self, pos_m, rot, gimbal):
        """현재 시각 스크린샷을 트리거하고 동기화 텔레메트리를 기록"""
        self.trigger_capture()
        time.sleep(0.05)

        # 디렉터리에서 가장 최근에 생성된 스크린샷 확인
        existing_imgs = sorted(glob.glob(os.path.join(CAPTURED_FRAMES_DIR, "*.png")), key=os.path.getmtime)
        if existing_imgs:
            latest_img = existing_imgs[-1]
            record = {
                "frame_id": len(self.survey_records) + 1,
                "timestamp": time.time(),
                "img_path": latest_img,
                "loc_cm": [round(pos_m[0] * 100.0, 1), round(pos_m[1] * 100.0, 1), round(pos_m[2] * 100.0, 1)],
                "loc_m": [round(pos_m[0], 2), round(pos_m[1], 2), round(pos_m[2], 2)],
                "drone_rot": [round(rot[0], 2), round(rot[1], 2), round(rot[2], 2)],
                "gimbal_rot": [round(gimbal[0], 2), round(gimbal[1], 2)],
                "total_pitch": round(gimbal[0] + rot[0], 2),
                "total_yaw": round(rot[1], 2)
            }
            # 중복 프레임 등록 방지
            if not self.survey_records or self.survey_records[-1]["img_path"] != latest_img:
                self.survey_records.append(record)
                return latest_img

        return None

    def wait_until_waypoint_reached(self, target_x_m, target_y_m, timeout_sec=14.0, sample_interval=0.6):
        """지정 웨이포인트 도달 시까지 비행하며 정밀 캡처 기록"""
        start_time = time.time()
        last_sample_time = 0

        while (time.time() - start_time) < timeout_sec:
            telem = self.get_latest_telemetry()
            if telem:
                pos_m, rot, gimbal, reached = self.parse_telemetry(telem)
                self.flight_logs.append({
                    "time": time.time(),
                    "pos": pos_m,
                    "rot": rot,
                    "gimbal": gimbal
                })

                # 비행 중 주기적 캡처 레코딩
                # if time.time() - last_sample_time >= sample_interval:
                #     last_sample_time = time.time()
                #     new_frame = self.record_synchronized_frame(pos_m, rot, gimbal)
                #     if new_frame:
                #         self.detect_targets_in_frame(new_frame, pos_m, rot, gimbal[0])

                # 도달 판정
                dist = math.sqrt((pos_m[0] - target_x_m) ** 2 + (pos_m[1] - target_y_m) ** 2)
                if reached or dist <= 2.5:
                    return True, pos_m

            time.sleep(0.05)

        return False, None

    # ==================================================================================
    # 🚀 4. 시작 지점 텔레포트 배치 (지연시간 0.1초)
    # ==================================================================================
    def setup_at_mission_start(self, start_x_m=-50.0, start_y_m=-40.0, yaw_deg=0.0):
        print(f"\n⚡ [TELEPORT] 수색 시작 지점으로 즉각 이동:")
        print(f"   위치: X={start_x_m}m, Y={start_y_m}m, Alt(Z)={self.cruise_alt_m}m, Yaw={yaw_deg}°")
        print(f"   순항 속도: {self.cruise_speed_ms} m/s ({self.cruise_speed_ms*100:.0f} cm/s)")

        self.set_gimbal(pitch_deg=self.gimbal_pitch, yaw_deg=0.0, roll_deg=0.0)
        time.sleep(0.15)

        x_cm = start_x_m * 100.0
        y_cm = start_y_m * 100.0
        z_cm = self.cruise_alt_m * 100.0

        # 패킷 유실 방지 2회 연속 전송
        self.teleport_drone(x_cm, y_cm, z_cm, yaw_deg)
        time.sleep(0.1)
        self.teleport_drone(x_cm, y_cm, z_cm, yaw_deg)

        time.sleep(0.5)
        telem = self.get_latest_telemetry()
        if telem:
            pos_m, _, gimbal, _ = self.parse_telemetry(telem)
            print(f"   -> [안착 확인] X={pos_m[0]:.1f}m, Y={pos_m[1]:.1f}m, Alt={pos_m[2]:.1f}m (짐벌: {gimbal[0]:.1f}°)")

        print("🎯 1초 후 ㄹ자 자율 수색 및 항공 데이터 취득을 시작합니다...\n")
        time.sleep(1.0)

    # ==================================================================================
    # 📐 5. 3D 지표면 삼각측량 & YOLOv8 표적 탐지
    # ==================================================================================
    def pixel_to_world_coords(self, px, py, drone_x, drone_y, drone_z, total_pitch, total_yaw):
        fov_h_rad = math.radians(CAMERA_FOV_H)
        fov_v_rad = 2.0 * math.atan(math.tan(fov_h_rad / 2.0) * (IMG_HEIGHT / float(IMG_WIDTH)))

        u_norm = (px / float(IMG_WIDTH)) - 0.5
        v_norm = (py / float(IMG_HEIGHT)) - 0.5

        delta_yaw = u_norm * fov_h_rad
        delta_pitch = -v_norm * fov_v_rad

        ray_pitch = math.radians(total_pitch) + delta_pitch
        ray_yaw = math.radians(total_yaw) + delta_yaw

        vx = math.cos(ray_pitch) * math.cos(ray_yaw)
        vy = math.cos(ray_pitch) * math.sin(ray_yaw)
        vz = math.sin(ray_pitch)
        if vz >= -0.05:
            vz = -0.05

        t = -drone_z / vz
        return drone_x + t * vx, drone_y + t * vy

    def detect_targets_in_frame(self, image_path, drone_pos, drone_rot, gimbal_pitch):
        if not os.path.exists(image_path):
            return

        frame = cv2.imread(image_path)
        if frame is None:
            return

        results = self.model.predict(frame, conf=0.45, verbose=False)
        total_pitch = gimbal_pitch + drone_rot[0]
        total_yaw = drone_rot[1]

        for r in results:
            for box in r.boxes:
                cls_id = int(box.cls[0].item())
                conf = float(box.conf[0].item())
                xyxy = box.xyxy[0].tolist()
                cx = (xyxy[0] + xyxy[2]) / 2.0
                cy = (xyxy[1] + xyxy[3]) / 2.0

                wx, wy = self.pixel_to_world_coords(
                    cx, cy, 
                    drone_pos[0], drone_pos[1], drone_pos[2],
                    total_pitch, total_yaw
                )

                label_name = CLASS_NAMES.get(cls_id, f"TARGET_{cls_id}")

                # 4m 이내 표적 중복 병합
                is_duplicate = False
                for tgt in self.confirmed_targets:
                    dist = math.hypot(tgt["world_x"] - wx, tgt["world_y"] - wy)
                    if dist < 4.0:
                        is_duplicate = True
                        tgt["hits"] += 1
                        tgt["confidence"] = max(tgt["confidence"], conf)
                        break

                if not is_duplicate:
                    new_target = {
                        "id": f"TGT-{len(self.confirmed_targets)+1:02d}",
                        "type": label_name,
                        "confidence": round(conf, 3),
                        "world_x": round(wx, 2),
                        "world_y": round(wy, 2),
                        "hits": 1,
                        "detected_frame": os.path.basename(image_path),
                        "time": datetime.now().strftime("%H:%M:%S")
                    }
                    self.confirmed_targets.append(new_target)
                    print(f"   🎯 [표적 탐지] {new_target['id']} ({label_name}) | 좌표: ({wx:.1f}m, {wy:.1f}m) | 신뢰도: {conf*100:.1f}%")

    # ==================================================================================
    # 💾 6. 후처리용 항공 미션 데이터셋 매니페스트 저장 (핵심 분리 기능!)
    # ==================================================================================
    def save_mission_manifest(self):
        """
        비행 중 수집된 모든 사진 경로, 텔레메트리, 표적 정보, 비행 궤적을 
        독립 후처리 스크립트(build_aerial_orthomosaic.py)가 읽을 수 있는 표준 JSON 파일로 저장합니다.
        """
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        manifest_path = os.path.join(OUTPUT_DATA_DIR, f"survey_manifest_{timestamp}.json")
        latest_symlink_path = os.path.join(OUTPUT_DATA_DIR, "survey_manifest_latest.json")

        manifest_data = {
            "metadata": {
                "mission_name": "DroneMapSim_Autonomous_Survey",
                "timestamp": timestamp,
                "altitude_m": self.cruise_alt_m,
                "speed_ms": self.cruise_speed_ms,
                "camera_fov_h": CAMERA_FOV_H,
                "img_width": IMG_WIDTH,
                "img_height": IMG_HEIGHT,
                "total_frames": len(self.survey_records),
                "total_targets": len(self.confirmed_targets)
            },
            "frames": self.survey_records,
            "targets": self.confirmed_targets,
            "flight_trail": [
                {"t": round(l["time"], 2), "x": round(l["pos"][0], 2), "y": round(l["pos"][1], 2), "z": round(l["pos"][2], 2)}
                for l in self.flight_logs
            ]
        }

        with open(manifest_path, "w", encoding="utf-8") as f:
            json.dump(manifest_data, f, indent=2, ensure_ascii=False)

        # 가장 최신 파일로 바로 참조할 수 있도록 survey_manifest_latest.json도 갱신
        with open(latest_symlink_path, "w", encoding="utf-8") as f:
            json.dump(manifest_data, f, indent=2, ensure_ascii=False)

        print("\n" + "=" * 75)
        print("💾 [DATASET EXPORT] 항공 정사 데이터 수집 완료!")
        print(f"   📁 매니페스트 파일: {manifest_path}")
        print(f"   📁 최신 심볼릭 링크: {latest_symlink_path}")
        print(f"   📸 수집된 고해상도 프레임: {len(self.survey_records)}장")
        print(f"   🎯 식별된 지상 표적: {len(self.confirmed_targets)}개")
        print("=" * 75)
        print("👉 [안내] 정사 모자이크 지도 합성을 진행하려면 다음 명령을 실행하세요:")
        print("   $ python build_aerial_orthomosaic.py")
        print("=" * 75 + "\n")

    # ==================================================================================
    # 🏁 7. 통합 미션 실행 루틴 (Entry Point)
    # ==================================================================================
    def execute_mission(self):
        # 1. 시작점 즉각 순간이동
        self.setup_at_mission_start(start_x_m=-50.0, start_y_m=-40.0, yaw_deg=0.0)

        input("Press Enter to start the autonomous sweep mission...")

        # 2. 'ㄹ'자 차선 (Lane) 정의 (-40m부터 +40m까지 20m 간격)
        lanes = [-40.0, -20.0, 0.0, 20.0, 40.0]
        speed_cm_s = self.cruise_speed_ms * 100.0

        for idx, y_m in enumerate(lanes):
            target_x_m = 50.0 if (idx % 2 == 0) else -50.0
            target_yaw = 0.0 if (idx % 2 == 0) else 180.0

            print(f"✈️ [LANE {idx+1}/{len(lanes)}] 이동 목표: X={target_x_m}m, Y={y_m}m (Yaw={target_yaw}°)")

            self.fly_to(
                x_cm=target_x_m * 100.0,
                y_cm=y_m * 100.0,
                z_cm=self.cruise_alt_m * 100.0,
                speed_cm_s=speed_cm_s,
                yaw_deg=target_yaw,
                accept_radius_cm=200.0
            )

            # 도달할 때까지 비행하며 텔레메트리 & 캡처 수집
            self.wait_until_waypoint_reached(
                target_x_m=target_x_m,
                target_y_m=y_m,
                timeout_sec=14.0
            )

        # 3. 비행 종료 및 정지
        self.hover_stop()
        print("\n🎉 모든 수색 차선 비행 완료! 기체를 호버링 상태로 전환했습니다.")

        # 4. 후처리용 정사 데이터셋 매니페스트 저장
        self.save_mission_manifest()


if __name__ == "__main__":
    # 고도 65m 정밀 차량 탐지 모드 (고도 500m 조감 시 cruise_alt_m=500.0 설정)
    collector = FreeFlightDataCollector(mode="survey", cruise_alt_m=200.0)
    collector.execute_mission()
