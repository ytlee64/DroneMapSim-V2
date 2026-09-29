"""
DroneMapSim Integrated GCS Controller (v6.0 - Unified Mission & Dataset Edition)
==============================================================================
[수동 조종 (왼손: 기체 / 오른손: 짐벌)]
  W / S : 전진 / 후진          |  I / K : 짐벌 Pitch (올리기 / 내리기)
  A / D : 좌 / 우 이동          |  J / L : 짐벌 Yaw (좌회전 / 우회전)
  E / Q : 상승 / 하강          |  Space : 즉시 단발 사진 촬영
  Z / C : 선회 (Yaw 회전)      |  H     : 즉시 정지 / 호버링 (Hover)

[자율 비행 모드 (Lawnmower Grid)]
  [1] 키 : 📸 [학습 데이터셋 수집] 1,000장 서베이 비행 & 저장
  [2] 키 : 🔍 [실제 정찰 & 매핑 미션] 실시간 객체 탐지 및 지도 생성 비행
  [O] 키 : ✈️ [고정익 선회] 원형 Loiter 정찰
  [8] 키 : ✈️ [고정익 8자] Figure-8 감시 기동

[시스템]
  P : 실시간 계기판 출력  |  V : 시점 순환  |  X : 종료
"""

import socket
import json
import time
import threading
import sys
import os

# 윈도우 키보드 입력용 모듈
if os.name == 'nt':
    import msvcrt
else:
    import select

UE5_IP = "127.0.0.1"
CMD_PORT = 9000
TELEM_PORT = 9001


class DroneGCS:
    def __init__(self):
        # UDP 소켓
        self.cmd_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.telem_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.telem_sock.bind(("0.0.0.0", TELEM_PORT))
        self.telem_sock.settimeout(0.5)

        self.latest_telemetry = {}
        self.b_running = True

        # 기구학 상태
        self.cur_pitch = -60.0
        self.cur_yaw = 0.0

        # 자율 비행 관리
        self.is_auto_flying = False
        self.auto_thread = None

        # 텔레메트리 수신 스레드
        self.recv_thread = threading.Thread(target=self._telemetry_worker, daemon=True)
        self.recv_thread.start()

    def _telemetry_worker(self):
        while self.b_running:
            try:
                data, _ = self.telem_sock.recvfrom(4096)
                self.latest_telemetry = json.loads(data.decode('utf-8'))
            except (socket.timeout, Exception):
                pass

    def send_cmd(self, payload):
        try:
            msg = json.dumps(payload).encode('utf-8')
            self.cmd_sock.sendto(msg, (UE5_IP, CMD_PORT))
        except Exception as e:
            print(f"\n>>> [송신 에러]: {e}")

    # =========================================================================
    # 기본 비행 제어 함수
    # =========================================================================
    def manual_move(self, x=0.0, y=0.0, z=0.0, yaw=0.0):
        if self.is_auto_flying:
            self.stop_auto_flight()
            print("\n>>> [MANUAL] ⚠️ 수동 개입: 자율 비행 모드가 취소되었습니다.")
        self.send_cmd({"cmd": "MANUAL_MOVE", "x": float(x), "y": float(y), "z": float(z), "yaw_rate": float(yaw)})

    def navigate_to(self, x, y, z, speed=2500.0):
        if self.latest_telemetry:
            self.latest_telemetry["reached"] = False
        self.send_cmd({"cmd": "NAV_TO", "x": float(x), "y": float(y), "z": float(z), "speed": float(speed)})

    def wait_until_reached(self, timeout=10.0):
        start_t = time.time()
        time.sleep(0.1)
        while time.time() - start_t < timeout and self.is_auto_flying:
            if self.latest_telemetry and self.latest_telemetry.get("reached", False):
                return True
            time.sleep(0.04)
        return False

    def hover(self):
        self.stop_auto_flight()
        self.send_cmd({"cmd": "HOVER"})
        print("\n>>> [DRONE] 🛑 제자리 호버링 (Hover) 활성화")

    def set_gimbal(self, pitch=None, yaw=None, d_pitch=0.0, d_yaw=0.0):
        if pitch is not None: self.cur_pitch = pitch
        if yaw is not None: self.cur_yaw = yaw
        self.cur_pitch = max(-90.0, min(20.0, self.cur_pitch + d_pitch))
        self.cur_yaw = (self.cur_yaw + d_yaw) % 360.0
        self.send_cmd({"cmd": "SET_GIMBAL", "pitch": self.cur_pitch, "yaw": self.cur_yaw})

    def capture(self):
        self.send_cmd({"cmd": "CAPTURE"})

    def stop_auto_flight(self):
        self.is_auto_flying = False

    # =========================================================================
    # 🌟 공통 그리드 웨이포인트 생성기 (2.3km 섬 지형 최적화)
    # =========================================================================
    def generate_survey_grid(self, target_count=1000):
        min_x, max_x = -50000.0, 50000.0
        min_y, max_y = -50000.0, 50000.0
        lanes = 32
        points_per_lane = 32

        x_step = (max_x - min_x) / (points_per_lane - 1)
        y_step = (max_y - min_y) / (lanes - 1)
        altitudes = [5000.0, 7500.0, 10000.0]  # 50m, 75m, 100m
        pitches = [-45.0, -75.0, -90.0]

        waypoints = []
        direction = 1

        for lane in range(lanes):
            cur_y = min_y + (lane * y_step)
            alt = altitudes[lane % len(altitudes)]
            pitch = pitches[lane % len(pitches)]
            xs = [min_x + (i * x_step) for i in range(points_per_lane)] if direction == 1 else [max_x - (i * x_step) for i in range(points_per_lane)]

            for cur_x in xs:
                waypoints.append({"x": cur_x, "y": cur_y, "z": alt, "pitch": pitch})
                if len(waypoints) >= target_count:
                    return waypoints
            direction *= -1

        return waypoints

    # =========================================================================
    # 🚀 비행 실행 엔진 (미션 목적에 따라 콜백 처리)
    # =========================================================================
    def start_grid_mission(self, mission_mode="DATASET", target_count=1000):
        if self.is_auto_flying:
            print("\n>>> [AUTO] 이미 다른 자율 비행이 실행 중입니다. [H]로 정지하세요.")
            return

        self.is_auto_flying = True

        def worker():
            wps = self.generate_survey_grid(target_count)
            total = len(wps)

            mode_name = "📸 [AI 학습 데이터셋 수집]" if mission_mode == "DATASET" else "🔍 [실제 차량 탐지 & 지도 매핑 미션]"
            print(f"\n==================================================")
            print(f"{mode_name} 시작! (총 {total}개 지점)")
            print(f" - 중단하려면 언제든 [H] 키를 누르세요.")
            print(f"==================================================")

            start_t = time.time()
            done = 0
            last_pitch = None

            for wp in wps:
                if not self.is_auto_flying: break

                # 짐벌 각도 변경
                if wp["pitch"] != last_pitch:
                    self.set_gimbal(pitch=wp["pitch"], yaw=0.0)
                    last_pitch = wp["pitch"]
                    time.sleep(0.04)

                # 목표 지점으로 90km/h 이동
                self.navigate_to(wp["x"], wp["y"], wp["z"], speed=2500.0)
                self.wait_until_reached(timeout=8.0)

                # 사진 촬영 및 처리
                time.sleep(0.04)
                self.capture()

                # 💡 여기서 모드별 행동 분기!
                if mission_mode == "DATASET":
                    # [모드 1]: 언리얼이 YOLO 파일셋을 자동 저장함
                    pass
                elif mission_mode == "RECON_MAP":
                    # [모드 2]: 실시간 자동차 탐지 및 지도에 마킹하는 로직이 들어가는 자리!
                    pass

                done += 1
                elapsed = time.time() - start_t
                rate = done / elapsed if elapsed > 0 else 0
                pct = (done / total) * 100.0

                sys.stdout.write(
                    f"\r>>> {mode_name} [{done}/{total}] ({pct:.1f}%) | "
                    f"고도: {wp['z']/100:.0f}m | 속도: {rate:.1f}장/s   "
                )
                sys.stdout.flush()

            if self.is_auto_flying:
                self.hover()
                print(f"\n>>> 🏁 미션 완수! 총 {done}개 구역 처리 완료.")

            self.is_auto_flying = False

        self.auto_thread = threading.Thread(target=worker, daemon=True)
        self.auto_thread.start()

    # =========================================================================
    # 고정익 특화 비행 (Orbit, Figure-8)
    # =========================================================================
    def start_orbit(self):
        if self.is_auto_flying: return
        self.is_auto_flying = True
        def worker():
            print("\n>>> [FIXED-WING] ✈️ 고정익 선회 정찰 시작 (중단: [H])")
            while self.is_auto_flying:
                self.send_cmd({"cmd": "MANUAL_MOVE", "x": 1.0, "y": 0.0, "z": 0.0, "yaw_rate": 0.2})
                time.sleep(0.02)
        threading.Thread(target=worker, daemon=True).start()

    def start_figure_eight(self):
        if self.is_auto_flying: return
        self.is_auto_flying = True
        def worker():
            print("\n>>> [FIXED-WING] ✈️ 8자 기동 정찰 시작 (중단: [H])")
            while self.is_auto_flying:
                t0 = time.time()
                while self.is_auto_flying and (time.time() - t0 < 6.0):
                    self.send_cmd({"cmd": "MANUAL_MOVE", "x": 1.0, "y": 0.0, "z": 0.0, "yaw_rate": -0.4})
                    time.sleep(0.02)
                t0 = time.time()
                while self.is_auto_flying and (time.time() - t0 < 6.0):
                    self.send_cmd({"cmd": "MANUAL_MOVE", "x": 1.0, "y": 0.0, "z": 0.0, "yaw_rate": 0.4})
                    time.sleep(0.02)
        threading.Thread(target=worker, daemon=True).start()

    def print_telem(self):
        t = self.latest_telemetry
        if not t:
            print("\n>>> [TELEM] 수신 데이터 없음")
            return
        loc = t.get("loc", [0, 0, 0])
        print(f"\n📊 [TELEM] X={loc[0]:.1f}, Y={loc[1]:.1f}, Alt={loc[2]*0.01:.1f}m | Gimbal={self.cur_pitch:.1f}°")

    def close(self):
        self.b_running = False
        self.stop_auto_flight()
        self.cmd_sock.close()
        self.telem_sock.close()


# =============================================================================
# 키보드 입력 인터페이스 루프
# =============================================================================
def get_key():
    if os.name == 'nt':
        return msvcrt.getch().decode('utf-8', errors='ignore')
    else:
        r, _, _ = select.select([sys.stdin], [], [], 0.1)
        return sys.stdin.read(1) if r else None


def main():
    gcs = UnifiedDroneGCS()
    print("==================================================")
    print("🚁 DroneMapSim Unified GCS Controller (v6.0)")
    print(" [수동 조종]: W/S (전후), A/D (좌우), E/Q (상하), Z/C (선회)")
    print(" [짐벌 각도]: I/K (Pitch 상하), J/L (Yaw 좌우), Space (촬영)")
    print(" ------------------------------------------------")
    print(" [1] : 📸 [학습 데이터 수집] 1,000장 서베이 비행 & 라벨링 저장")
    print(" [2] : 🔍 [실제 미션] 실시간 정찰 탐지 및 지도 매핑 비행")
    print(" [O] : ✈️ 고정익 선회 | [8] : ✈️ 8자 정찰 | [H] : 즉시 호버링(정지)")
    print(" [P] : 텔레메트리 출력 | [V] : 시점 변경 | [X] : 프로그램 종료")
    print("==================================================")

    try:
        while True:
            key = get_key()
            if not key:
                time.sleep(0.01)
                continue

            k = key.lower()

            # 수동 기체 이동
            if k == 'w': gcs.manual_move(x=1.0)
            elif k == 's': gcs.manual_move(x=-1.0)
            elif k == 'a': gcs.manual_move(y=-1.0)
            elif k == 'd': gcs.manual_move(y=1.0)
            elif k == 'e': gcs.manual_move(z=1.0)
            elif k == 'q': gcs.manual_move(z=-1.0)
            elif k == 'z': gcs.manual_move(yaw=-1.0)
            elif k == 'c': gcs.manual_move(yaw=1.0)

            # 짐벌 조작
            elif k == 'i': gcs.set_gimbal(d_pitch=5.0)
            elif k == 'k': gcs.set_gimbal(d_pitch=-5.0)
            elif k == 'j': gcs.set_gimbal(d_yaw=-5.0)
            elif k == 'l': gcs.set_gimbal(d_yaw=5.0)

            # 단축키 & 자율 모드
            elif k == ' ': gcs.capture(); print("\n>>> 📸 사진 촬영 완료!")
            elif k == 'h': gcs.hover()
            elif k == '1': gcs.start_grid_mission(mission_mode="DATASET", target_count=1000)
            elif k == '2': gcs.start_grid_mission(mission_mode="RECON_MAP", target_count=1000)
            elif k == 'o': gcs.start_orbit()
            elif k == '8': gcs.start_figure_eight()
            elif k == 'p': gcs.print_telem()
            elif k == 'v': gcs.send_cmd({"cmd": "SET_OBSERVER_MODE", "mode": "CYCLE"})
            elif k == 'x': break

    except KeyboardInterrupt:
        pass
    finally:
        gcs.close()
        print("\n>>> GCS 프로그램이 안전하게 종료되었습니다.")

if __name__ == "__main__":
    main()