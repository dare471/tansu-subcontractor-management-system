import json
import logging
from dataclasses import dataclass, asdict

import cv2
import numpy as np
from deepface import DeepFace

from app.config import DETECTOR, MODEL

logger = logging.getLogger(__name__)

_model_warmed = False

# Hikvision face terminal (DS-K1T604 / import data guidelines)
MIN_WIDTH = 640
MIN_HEIGHT = 480
MAX_WIDTH = 2160
MAX_HEIGHT = 3840
MIN_FILE_BYTES = 40 * 1024
MAX_FILE_BYTES = 200 * 1024
MAX_BACKGROUND_STD = 45.0
MIN_FACE_AREA_RATIO = 0.08
MAX_FACE_AREA_RATIO = 0.75


@dataclass
class PhotoCheck:
    code: str
    passed: bool
    message: str


@dataclass
class ReferencePhotoValidation:
    valid: bool
    width: int
    height: int
    file_size: int
    face_count: int
    checks: list[PhotoCheck]
    message: str

    def to_dict(self) -> dict:
        return {
            "valid": self.valid,
            "width": self.width,
            "height": self.height,
            "fileSize": self.file_size,
            "faceCount": self.face_count,
            "message": self.message,
            "checks": [asdict(c) for c in self.checks],
        }


def warm_up_model() -> None:
    global _model_warmed
    if _model_warmed:
        return
    DeepFace.build_model(MODEL)
    _model_warmed = True


def decode_image_raw(data: bytes) -> np.ndarray | None:
    if not data or len(data) < 100:
        return None
    arr = np.frombuffer(data, dtype=np.uint8)
    image = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if image is None or image.size == 0:
        return None
    return image


def _is_jpeg(data: bytes) -> bool:
    return len(data) >= 3 and data[0:3] == b"\xff\xd8\xff"


def _background_is_neutral(bgr: np.ndarray) -> tuple[bool, str]:
    h, w = bgr.shape[:2]
    margin = max(4, min(h, w) // 20)
    border = np.concatenate(
        [
            bgr[:margin, :].reshape(-1, 3),
            bgr[-margin:, :].reshape(-1, 3),
            bgr[:, :margin].reshape(-1, 3),
            bgr[:, -margin:].reshape(-1, 3),
        ],
        axis=0,
    )
    std = float(np.std(border))
    if std > MAX_BACKGROUND_STD:
        return False, f"Фон не однотонный (вариация {std:.0f}). Используйте нейтральный фон."
    return True, "Фон нейтральный."


def validate_reference_photo(image_bytes: bytes) -> ReferencePhotoValidation:
    warm_up_model()
    checks: list[PhotoCheck] = []
    file_size = len(image_bytes)

    fmt_ok = _is_jpeg(image_bytes)
    checks.append(
        PhotoCheck(
            "format",
            fmt_ok,
            "Формат JPEG/JPG (требование Hikvision)." if fmt_ok else "Допустим только формат JPEG/JPG.",
        )
    )

    size_ok = MIN_FILE_BYTES <= file_size <= MAX_FILE_BYTES
    checks.append(
        PhotoCheck(
            "file_size",
            size_ok,
            f"Размер файла {file_size // 1024} КБ (норма 40–200 КБ)."
            if size_ok
            else f"Размер файла {file_size // 1024} КБ — допустимо 40–200 КБ (Hikvision).",
        )
    )

    bgr = decode_image_raw(image_bytes)
    if bgr is None:
        checks.append(
            PhotoCheck("decode", False, "Не удалось прочитать изображение.")
        )
        return ReferencePhotoValidation(
            valid=False,
            width=0,
            height=0,
            file_size=file_size,
            face_count=0,
            checks=checks,
            message="Не удалось прочитать изображение.",
        )

    height, width = bgr.shape[:2]
    res_ok = width >= MIN_WIDTH and height >= MIN_HEIGHT and width <= MAX_WIDTH and height <= MAX_HEIGHT
    checks.append(
        PhotoCheck(
            "resolution",
            res_ok,
            f"Разрешение {width}×{height} (норма {MIN_WIDTH}×{MIN_HEIGHT} – {MAX_WIDTH}×{MAX_HEIGHT})."
            if res_ok
            else f"Разрешение {width}×{height} вне диапазона {MIN_WIDTH}×{MIN_HEIGHT} – {MAX_WIDTH}×{MAX_HEIGHT}.",
        )
    )

    face_count = 0
    face_ok = False
    face_message = "Лицо не обнаружено."
    try:
        faces = DeepFace.extract_faces(
            img_path=bgr,
            detector_backend=DETECTOR,
            enforce_detection=True,
            align=True,
        )
        face_count = len(faces)
        if face_count == 0:
            face_message = "Лицо не обнаружено. Смотрите прямо в камеру."
        elif face_count > 1:
            face_message = "На фото должно быть только одно лицо."
        else:
            area = faces[0].get("facial_area") or {}
            fw = max(1, area.get("w", 0))
            fh = max(1, area.get("h", 0))
            ratio = (fw * fh) / (width * height)
            if ratio < MIN_FACE_AREA_RATIO:
                face_message = "Лицо слишком мелкое — подойдите ближе к камере."
            elif ratio > MAX_FACE_AREA_RATIO:
                face_message = "Лицо слишком крупное или обрезано."
            else:
                face_ok = True
                face_message = "Лицо чётко видно, без посторонних лиц."
    except ValueError as exc:
        text = str(exc).lower()
        if "face could not be detected" in text or "could not detect" in text:
            face_message = (
                "Лицо не обнаружено или закрыто (очки, головной убор, маска). "
                "Снимите аксессуары и повторите."
            )
        else:
            face_message = f"Ошибка детекции лица: {exc}"
    except Exception as exc:
        logger.exception("Face validation failed")
        face_message = f"Ошибка проверки лица: {exc}"

    checks.append(PhotoCheck("face", face_ok and face_count == 1, face_message))

    bg_ok, bg_msg = _background_is_neutral(bgr)
    checks.append(PhotoCheck("background", bg_ok, bg_msg))

    failed = [c for c in checks if not c.passed]
    valid = len(failed) == 0
    message = "Фото соответствует требованиям." if valid else "; ".join(c.message for c in failed)

    return ReferencePhotoValidation(
        valid=valid,
        width=width,
        height=height,
        file_size=file_size,
        face_count=face_count,
        checks=checks,
        message=message,
    )
