import cv2
import mediapipe as mp
import socket
import time

# UDP setup
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# MediaPipe setup
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(max_num_faces=1, refine_landmarks=True)

cap = cv2.VideoCapture(0)

# OPTIMIZATION: Track previous state to only send when it changes
previous_state = None
last_send_time = 0
MIN_SEND_INTERVAL = 0.1  # Minimum 100ms between sends (even if state changes rapidly)


def is_looking_away(landmarks, yaw_threshold=0.08, pitch_threshold=0.50):
    # --- Yaw detection (looking left/right) ---
    nose_x = landmarks[1].x
    left_ear_x = landmarks[234].x
    right_ear_x = landmarks[454].x

    face_center_x = (left_ear_x + right_ear_x) / 2
    yaw = nose_x - face_center_x

    # --- Pitch detection (looking down) ---
    forehead_y = landmarks[10].y   # Top of forehead
    chin_y = landmarks[152].y      # Bottom of chin
    nose_tip_y = landmarks[1].y    # Nose tip

    # Calculate where nose is relative to face height
    # When looking straight: ~0.52-0.53
    # When looking down: ~0.75-0.83
    face_height = chin_y - forehead_y
    nose_relative = (nose_tip_y - forehead_y) / face_height if face_height > 0 else 0.5

    # Looking away if turning sideways OR looking down
    looking_sideways = abs(yaw) > yaw_threshold
    looking_down = nose_relative > pitch_threshold

    return looking_sideways or looking_down


while True:
    success, img = cap.read()
    if not success:
        break

    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    results = face_mesh.process(img_rgb)

    looking_away = True
    if results.multi_face_landmarks:
        for face_landmarks in results.multi_face_landmarks:
            looking_away = is_looking_away(face_landmarks.landmark)

    # OPTIMIZATION: Only send UDP message when state actually changes
    current_time = time.time()
    if looking_away != previous_state and (current_time - last_send_time) >= MIN_SEND_INTERVAL:
        message = "LOOKING_AWAY" if looking_away else "LOOKING"
        sock.sendto(message.encode(), (UDP_IP, UDP_PORT))
        previous_state = looking_away
        last_send_time = current_time

        # Optional: Print state changes for debugging
        # print(f"State changed: {message}")

    # Update display
    message_display = "LOOKING_AWAY" if looking_away else "LOOKING"
    color = (0, 0, 255) if looking_away else (255, 0, 0)  # Red if away, Green if looking
    cv2.putText(img, message_display, (30, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, color, 2)
    cv2.imshow("Webcam", img)

    if cv2.waitKey(1) & 0xFF == 27:  # ESC to quit
        break

cap.release()
cv2.destroyAllWindows()
sock.close()