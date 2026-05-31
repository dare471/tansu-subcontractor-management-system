import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse

from app.config import DETECTOR, MODEL, PORT
from app.verifier import VerifyResult, detect_face, verify_faces, warm_up_model
from app.reference_photo import validate_reference_photo

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(_: FastAPI):
    try:
        warm_up_model()
    except Exception:
        logger.exception("Не удалось прогреть модель при старте — загрузка при первом запросе.")
    yield


app = FastAPI(title="Tansu Face Verify", version="1.0.0", lifespan=lifespan)


@app.get("/health")
def health():
    return {"status": "ok", "model": MODEL, "detector": DETECTOR}


@app.post("/api/validate-reference-photo")
async def validate_reference_photo_endpoint(photo: UploadFile = File(...)):
    image_bytes = await photo.read()
    result = validate_reference_photo(image_bytes)
    return JSONResponse(result.to_dict())


@app.post("/api/detect-face")
async def detect_face_endpoint(photo: UploadFile = File(...)):
    image_bytes = await photo.read()
    result: VerifyResult = detect_face(image_bytes)
    return JSONResponse(
        {
            "hasFace": result.matched,
            "message": result.message,
            "model": MODEL,
            "detector": DETECTOR,
        }
    )


@app.post("/api/verify")
async def verify(
    referencePhoto: UploadFile = File(...),
    livePhoto: UploadFile = File(...),
):
    reference_bytes = await referencePhoto.read()
    live_bytes = await livePhoto.read()
    result: VerifyResult = verify_faces(reference_bytes, live_bytes)
    return JSONResponse(
        {
            "matched": result.matched,
            "confidence": round(result.confidence, 4),
            "message": result.message,
            "distance": result.distance,
            "threshold": result.threshold,
            "model": MODEL,
            "detector": DETECTOR,
        }
    )


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("app.main:app", host="0.0.0.0", port=PORT, reload=False)
