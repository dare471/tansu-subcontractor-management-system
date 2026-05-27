import logging
import os
from dataclasses import dataclass

import cv2
import numpy as np
from deepface import DeepFace

from app.config import DETECTOR, MODEL

logger = logging.getLogger(__name__)

_model_warmed = False
MAX_IMAGE_SIDE = int(os.getenv("FACE_VERIFY_MAX_SIDE", "1024"))


def warm_up_model() -> None:
    """Загружает модель при старте, чтобы первый запрос не ждал скачивания."""
    global _model_warmed
    if _model_warmed:
        return
    logger.info("Загрузка модели DeepFace: model=%s detector=%s", MODEL, DETECTOR)
    DeepFace.build_model(MODEL)
    _model_warmed = True
    logger.info("Модель DeepFace готова.")


def decode_image(data: bytes) -> np.ndarray | None:
    if not data or len(data) < 100:
        return None
    arr = np.frombuffer(data, dtype=np.uint8)
    image = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if image is None or image.size == 0:
        return None
    return resize_for_verify(image)


def resize_for_verify(bgr: np.ndarray) -> np.ndarray:
    height, width = bgr.shape[:2]
    max_side = max(height, width)
    if max_side <= MAX_IMAGE_SIDE:
        return bgr
    scale = MAX_IMAGE_SIDE / max_side
    new_size = (int(width * scale), int(height * scale))
    return cv2.resize(bgr, new_size, interpolation=cv2.INTER_AREA)


@dataclass
class VerifyResult:
    matched: bool
    confidence: float
    message: str
    distance: float | None = None
    threshold: float | None = None


def detect_face(image_bytes: bytes) -> VerifyResult:
    warm_up_model()

    bgr = decode_image(image_bytes)
    if bgr is None:
        return VerifyResult(
            matched=False,
            confidence=0.0,
            message="Не удалось прочитать изображение (ожидается JPEG/PNG).",
        )

    try:
        faces = DeepFace.extract_faces(
            img_path=bgr,
            detector_backend=DETECTOR,
            enforce_detection=True,
            align=True,
        )
    except ValueError as exc:
        text = str(exc).lower()
        if "face could not be detected" in text or "could not detect" in text:
            return VerifyResult(
                matched=False,
                confidence=0.0,
                message="Лицо не обнаружено. Смотрите прямо в камеру при хорошем освещении.",
            )
        return VerifyResult(matched=False, confidence=0.0, message=f"Ошибка детекции лица: {exc}")
    except Exception as exc:
        logger.exception("DeepFace detect failed")
        return VerifyResult(matched=False, confidence=0.0, message=f"Ошибка детекции лица: {exc}")

    if not faces:
        return VerifyResult(
            matched=False,
            confidence=0.0,
            message="Лицо не обнаружено. Смотрите прямо в камеру при хорошем освещении.",
        )

    return VerifyResult(
        matched=True,
        confidence=1.0,
        message="Лицо обнаружено.",
    )


def verify_faces(reference_bytes: bytes, live_bytes: bytes) -> VerifyResult:
    warm_up_model()

    ref_bgr = decode_image(reference_bytes)
    live_bgr = decode_image(live_bytes)
    if ref_bgr is None or live_bgr is None:
        return VerifyResult(
            matched=False,
            confidence=0.0,
            message="Не удалось прочитать изображение (ожидается JPEG/PNG).",
        )

    try:
        result = DeepFace.verify(
            img1_path=ref_bgr,
            img2_path=live_bgr,
            model_name=MODEL,
            detector_backend=DETECTOR,
            enforce_detection=True,
            align=True,
        )
    except ValueError as exc:
        text = str(exc).lower()
        if "face could not be detected" in text or "could not detect" in text:
            return VerifyResult(
                matched=False,
                confidence=0.0,
                message="Лицо не обнаружено на одном из снимков. Смотрите прямо в камеру при хорошем освещении.",
            )
        return VerifyResult(matched=False, confidence=0.0, message=f"Ошибка детекции лица: {exc}")
    except Exception as exc:
        logger.exception("DeepFace verify failed")
        return VerifyResult(matched=False, confidence=0.0, message=f"Ошибка сравнения лиц: {exc}")

    distance = float(result["distance"])
    threshold = float(result["threshold"])
    matched = bool(result["verified"])
    confidence = _confidence_from_distance(distance, threshold)

    if matched:
        message = f"Лицо подтверждено ({confidence * 100:.0f}%)."
    else:
        message = f"Лицо не совпадает с эталоном ({confidence * 100:.0f}%)."

    return VerifyResult(
        matched=matched,
        confidence=confidence,
        message=message,
        distance=distance,
        threshold=threshold,
    )


def _confidence_from_distance(distance: float, threshold: float) -> float:
    if threshold <= 0:
        return 0.0
    # Чем меньше distance относительно порога, тем выше уверенность.
    raw = 1.0 - (distance / threshold)
    return max(0.0, min(1.0, raw))
