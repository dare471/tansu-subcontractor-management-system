import os
from dataclasses import dataclass

MODEL = os.getenv("FACE_VERIFY_MODEL", "Facenet")
DETECTOR = os.getenv("FACE_VERIFY_DETECTOR", "opencv")
PORT = int(os.getenv("PORT", "8092"))
