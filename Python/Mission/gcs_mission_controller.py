import time
import drone_sdk as drone_sdk 

if __name__ == "__main__":
    gcs = drone_sdk.DroneGCS()
    #gcs.show_menu()

    try:
        import msvcrt
        while True:
            if msvcrt.kbhit():
                ch = msvcrt.getch()

                # 특수 키 (방향키 4개)
                if ch in (b'\x00', b'\xe0'):
                    arrow = msvcrt.getch()
                    if arrow == b'H': gcs.set_gimbal(d_pitch=+5.0)
                    elif arrow == b'P': gcs.set_gimbal(d_pitch=-5.0)
                    elif arrow == b'K': gcs.set_gimbal(d_yaw=-5.0)
                    elif arrow == b'M': gcs.set_gimbal(d_yaw=+5.0)
                    continue

                key = ch.decode('utf-8', errors='ignore').lower()

                # 자율 비행
                if key == 'o':
                    gcs.start_fixed_wing_orbit(cruise_speed=1.0, turn_rate=0.15, auto_capture_interval=3.0)
                elif key == '8':
                    gcs.start_figure_eight(half_cycle_time=7.0)
                elif key == 'm':
                    gcs.start_box_patrol(leg_duration=4.0)

                # 수동 비행 (WASD + EQ + ZC)
                elif key == 'w': gcs.manual_move(x=1.0)
                elif key == 's': gcs.manual_move(x=-1.0)
                elif key == 'a': gcs.manual_move(y=-1.0)
                elif key == 'd': gcs.manual_move(y=1.0)
                elif key == 'e': gcs.manual_move(z=1.0)
                elif key == 'q': gcs.manual_move(z=-1.0)
                elif key == 'z': gcs.manual_move(yaw=-1.0)
                elif key == 'c': gcs.manual_move(yaw=1.0)

                # 기능 단축키
                elif key == ' ': gcs.capture()
                elif key == 'v': gcs.set_observer_mode("CYCLE")
                elif key == 'g': gcs.toggle_gimbal_mode()
                elif key == 'h': gcs.hover()
                elif key == 'p': gcs.print_telemetry()
                elif key == 'x':
                    print(">>> GCS를 종료합니다.")
                    gcs.close()
                    break

            time.sleep(0.02)
    except ImportError:
        print("Non-Windows platform.")