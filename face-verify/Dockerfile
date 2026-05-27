FROM python:3.11-slim-bookworm

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        libgl1 \
        libglib2.0-0 \
        libsm6 \
        libxext6 \
        libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --upgrade pip setuptools wheel \
    && pip install --no-cache-dir -r requirements.txt

# Предзагрузка модели (Facenet ~150 МБ при первом build).
ENV FACE_VERIFY_MODEL=Facenet
ENV FACE_VERIFY_DETECTOR=opencv
ENV DEEPFACE_HOME=/models
RUN mkdir -p /models \
    && python -c "from deepface import DeepFace; DeepFace.build_model('Facenet')"

COPY app ./app

ENV PORT=8092
EXPOSE 8092

HEALTHCHECK --interval=15s --timeout=5s --start-period=120s --retries=10 \
    CMD curl -fsS http://127.0.0.1:8092/health || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8092"]
