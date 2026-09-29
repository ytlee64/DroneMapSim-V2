# gcs_mission_controller.py
# DroneMapSim Manual Flight & Mission Operations Controller

from drone_sdk import DroneSDK
import time
import threading

class MissionGCS:
    def __init__(self):
        self.drone = DroneSDK()
        self.is_auto_flying = False
        self.auto_thread = None

    def stop_auto(self):
        self.is_auto_flying = False

    def handle_manual_move(self, x=0.0, y=0.0, z=0.0, yaw=0.0):
        if self.is_auto_flying:
            self.stop_auto()
            print(">>> [MANUAL] ⚠️ 수동 개입: 자율 비행 모드가 취소되었습니다.")
        self.drone.manual_move(x, y, z, yaw)

    # --- 특화 미션 비행 ---
    def start_orbit(self, cruise_speed=1.0, turn_rate=0.2):
        if self.is_auto_flying: return
        self.is_auto_flying = True
        def worker():
            print("\n>>> [MISSION] ✈️ 고정익 선회 정찰(Orbit) 비행 시작 (중단: [H])")
            while self.is_auto_flying:
                self.drone.manual_move(cruise_speed, 0.0, 0.0, turn_rate)
                time.sleep(0.02)
        threading.Thread(target=worker, daemon=True).start()

    def start_figure_eight(self, half_cycle=7.0):
        if self.is_auto_flying: return
        self.is_auto_flying = True
        def worker():
            print("\n>>> [MISSION] ✈️ 고정익 8자 기동 정찰 시작 (중단: [H])")
            while self.is_auto_flying:
                t0 = time.time()
                while self.is_auto_flying and (time.time() - t0 < half_cycle):
                    self.drone.manual_move(1.0, 0.0, 0.0, -0.4)
                    time.sleep(0.02)
                t0 = time.time()
                while self.is_auto_flying and (time.time() - t0 < half_cycle):
                    self.drone.manual_move(1.0, 0.0, 0.0, 0.4)
                    time.sleep(0.02)
        threading.Thread(target=worker, daemon=True).start()

    def print_status(self):
        t = self.drone.latest_telemetry
        if not t:
            print(">>> [TELEM] 텔레메트리 없음")
            return
        loc = t.get("loc", [0, 0, 0])
        print(f"📊 [TELEM] X={loc[0]:.1f}, Y={loc[1]:.1f}, Alt={loc[2]*0.01:.1f}m | Gimbal Pitch={self.drone.cur_pitch:.1f}°")

    def run(self):
        print("==================================================")
        print("🎮 DroneMapSim Manual & Mission Controller Active")
        print(" - [W/S, A/D, E/Q, Z/C]: 기체 수동 비행")
        print(" - [O]: 고정익 선회 | [8]: 8자 정찰 | [H]: 호버링")
        print(" - [Space]: 캡처 | [V]: 시점 전환 | [P]: 계기판 | [X]: 종료")
        print("==================================================")
        # (여기에 기존의 키 입력 루프 연결)

if __name__ == "__main__":
    app = MissionGCS()
    app.run()