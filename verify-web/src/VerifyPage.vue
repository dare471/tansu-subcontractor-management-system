<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, nextTick } from 'vue';
import {
  NCard, NSpace, NButton, NTag, NText, NAlert, NInput, NSteps, NStep, useMessage
} from 'naive-ui';
import { Html5Qrcode } from 'html5-qrcode';
import { verifyApi, type PassLookup, type VerifyFaceResult } from './api';
import { appBrand } from './config/branding';

const msg = useMessage();
const cardTitle = `${appBrand.brandName} — проверка пропуска`;
const token = ref('');
const pass = ref<PassLookup | null>(null);
const verifyResult = ref<VerifyFaceResult | null>(null);
const scanning = ref(false);
const verifying = ref(false);
const faceCameraOn = ref(false);
const inlineError = ref('');
const scannerId = 'qr-reader';
let scanner: Html5Qrcode | null = null;
const videoRef = ref<HTMLVideoElement | null>(null);
let mediaStream: MediaStream | null = null;

const currentStep = computed(() => {
  if (verifyResult.value?.matched) return 3;
  if (pass.value?.isActive) return 2;
  return 1;
});

const statusTag = computed(() => {
  if (verifyResult.value?.matched) return { type: 'success' as const, label: 'Доступ разрешён' };
  if (verifyResult.value && !verifyResult.value.matched) return { type: 'error' as const, label: 'Доступ запрещён' };
  if (pass.value?.isActive) return { type: 'warning' as const, label: 'QR принят — шаг 2: Face ID' };
  return { type: 'default' as const, label: 'Шаг 1: отсканируйте QR пропуска' };
});

const needsHttpsForCamera = computed(() => {
  const host = window.location.hostname;
  return !window.isSecureContext && host !== 'localhost' && host !== '127.0.0.1';
});

const pageUrlHint = computed(() => {
  const { hostname, port } = window.location;
  return `https://${hostname}:${port || '5174'}`;
});

function cameraErrorMessage(err: unknown, context: 'qr' | 'face'): string {
  if (needsHttpsForCamera.value) {
    return `Камера заблокирована: откройте страницу по HTTPS (${window.location.protocol}//${window.location.host}) и примите сертификат браузера.`;
  }
  if (err instanceof DOMException) {
    if (err.name === 'NotAllowedError') {
      return 'Доступ к камере запрещён. Разрешите камеру в настройках браузера для этого сайта.';
    }
    if (err.name === 'NotFoundError') {
      return 'Камера не найдена на устройстве.';
    }
    if (err.name === 'NotReadableError') {
      return 'Камера занята другим приложением. Закройте его и повторите.';
    }
  }
  return context === 'qr'
    ? 'Не удалось открыть камеру. Разрешите доступ в браузере или вставьте токен вручную.'
    : 'Не удалось открыть камеру для Face ID. Разрешите доступ к камере.';
}

function extractToken(raw: string) {
  const trimmed = raw.trim();
  if (!trimmed) return '';
  try {
    const url = new URL(trimmed);
    return url.searchParams.get('token') ?? trimmed;
  } catch {
    return trimmed;
  }
}

async function lookupToken(raw?: string) {
  inlineError.value = '';
  const value = extractToken(raw ?? token.value);
  if (!value) {
    inlineError.value = 'Укажите токен или отсканируйте QR-код пропуска (не лицо).';
    msg.warning(inlineError.value);
    return;
  }
  token.value = value;
  verifyResult.value = null;
  pass.value = null;
  try {
    pass.value = await verifyApi.scan(value);
    if (!pass.value.isActive) {
      inlineError.value = 'Пропуск отозван.';
      msg.error(inlineError.value);
    } else {
      msg.success(`Пропуск: ${pass.value.fullName}`);
    }
  } catch (e) {
    pass.value = null;
    inlineError.value = e instanceof Error ? e.message : 'Пропуск не найден.';
    msg.error(inlineError.value);
  }
}

async function pickCameraId(): Promise<string | { facingMode: string }> {
  try {
    const cameras = await Html5Qrcode.getCameras();
    if (!cameras.length) return { facingMode: 'user' };
    const rear = cameras.find((c) => /back|rear|environment/i.test(c.label));
    return rear?.id ?? cameras[0].id;
  } catch {
    return { facingMode: 'user' };
  }
}

async function startScanner() {
  if (scanning.value) return;
  inlineError.value = '';
  scanning.value = true;
  await nextTick();

  scanner = new Html5Qrcode(scannerId);
  try {
    const cameraId = await pickCameraId();
    await scanner.start(
      cameraId,
      { fps: 8, qrbox: { width: 240, height: 240 } },
      async (decoded) => {
        await stopScanner();
        await lookupToken(decoded);
      },
      () => {}
    );
    msg.info('Наведите камеру на QR-код пропуска с экрана «История согласования».');
  } catch (e) {
    scanning.value = false;
    inlineError.value = cameraErrorMessage(e, 'qr');
    msg.error(inlineError.value);
  }
}

async function stopScanner() {
  if (scanner) {
    try {
      await scanner.stop();
      scanner.clear();
    } catch { /* ignore */ }
    scanner = null;
  }
  scanning.value = false;
}

async function startFaceCamera() {
  inlineError.value = '';
  stopFaceCamera();
  try {
    mediaStream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: 'user' },
      audio: false
    });
    faceCameraOn.value = true;
    await nextTick();
    if (videoRef.value) {
      videoRef.value.srcObject = mediaStream;
      await videoRef.value.play();
    }
    msg.info('Камера включена. Нажмите «Проверить лицо».');
  } catch (e) {
    faceCameraOn.value = false;
    inlineError.value = cameraErrorMessage(e, 'face');
    msg.error(inlineError.value);
  }
}

function stopFaceCamera() {
  mediaStream?.getTracks().forEach((t) => t.stop());
  mediaStream = null;
  faceCameraOn.value = false;
}

async function waitForVideoFrame(video: HTMLVideoElement, attempts = 30): Promise<boolean> {
  for (let i = 0; i < attempts; i++) {
    if (video.videoWidth > 0 && video.videoHeight > 0) return true;
    await new Promise((r) => setTimeout(r, 100));
  }
  return false;
}

async function verifyFace() {
  inlineError.value = '';
  if (!token.value || !pass.value) {
    inlineError.value = 'Сначала выполните шаг 1 — проверьте QR пропуска.';
    msg.warning(inlineError.value);
    return;
  }
  if (!faceCameraOn.value || !videoRef.value) {
    inlineError.value = 'Нажмите «Камера Face ID» и дождитесь изображения с камеры.';
    msg.warning(inlineError.value);
    return;
  }
  if (!pass.value.hasReferencePhoto) {
    inlineError.value = `У сотрудника нет эталонного фото в ${appBrand.brandName} — Face ID недоступен.`;
    msg.error(inlineError.value);
    return;
  }

  verifying.value = true;
  try {
    const ready = await waitForVideoFrame(videoRef.value);
    if (!ready) throw new Error('Камера ещё не готова — подождите секунду и повторите.');

    const canvas = document.createElement('canvas');
    canvas.width = videoRef.value.videoWidth;
    canvas.height = videoRef.value.videoHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) throw new Error('canvas');
    ctx.drawImage(videoRef.value, 0, 0, canvas.width, canvas.height);
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.92));
    if (!blob) throw new Error('Не удалось сделать снимок.');

    verifyResult.value = await verifyApi.verifyFace(token.value, blob);
    if (verifyResult.value.matched) {
      const recorded = verifyResult.value.siteVisitRecorded ? ' Проход на объект записан.' : '';
      msg.success(`Личность подтверждена.${recorded}`);
    } else msg.error(verifyResult.value.message);
  } catch (e) {
    inlineError.value = e instanceof Error ? e.message : 'Ошибка проверки Face ID.';
    msg.error(inlineError.value);
  } finally {
    verifying.value = false;
  }
}

function reset() {
  token.value = '';
  pass.value = null;
  verifyResult.value = null;
  inlineError.value = '';
  stopScanner();
  stopFaceCamera();
}

onMounted(() => {
  const params = new URLSearchParams(window.location.search);
  const t = params.get('token');
  if (t) lookupToken(t);
});

onUnmounted(() => {
  stopScanner();
  stopFaceCamera();
});
</script>

<template>
  <div class="page">
    <NCard :title="cardTitle" style="max-width:760px;margin:0 auto">
      <NSpace vertical :size="16">
        <NTag :type="statusTag.type" round>{{ statusTag.label }}</NTag>

        <NSteps :current="currentStep" size="small">
          <NStep title="QR пропуска" :description="`Скан или ссылка из ${appBrand.brandName}`" />
          <NStep title="Face ID" description="Селфи и сравнение с фото" />
          <NStep title="Результат" description="Доступ разрешён / запрещён" />
        </NSteps>

        <NAlert v-if="inlineError" type="error" :show-icon="false">{{ inlineError }}</NAlert>

        <NAlert v-if="needsHttpsForCamera" type="warning" :show-icon="false">
          Камера с телефона/планшета работает только по <b>HTTPS</b>.
          Откройте <b>{{ pageUrlHint }}</b>
          и подтвердите самоподписанный сертификат (это dev-среда).
        </NAlert>

        <NAlert type="info" :show-icon="false">
          «Сканировать QR» ищет <b>QR-код</b> с экрана «История согласования» сотрудника.
          Лицо проверяется отдельно на шаге 2 после успешного QR.
        </NAlert>

        <NSpace align="center" wrap>
          <NInput v-model:value="token" placeholder="Токен или ссылка из QR" style="min-width:280px;flex:1" />
          <NButton type="primary" @click="lookupToken()">Проверить QR</NButton>
        </NSpace>

        <NSpace wrap>
          <NButton v-if="!scanning" @click="startScanner">Сканировать QR</NButton>
          <NButton v-else @click="stopScanner">Остановить сканер</NButton>
          <NButton @click="reset">Сброс</NButton>
        </NSpace>

        <div v-show="scanning" :id="scannerId" class="qr-box" />

        <NCard v-if="pass" size="small" title="Данные пропуска">
          <NSpace vertical :size="6">
            <NText><b>{{ pass.fullName }}</b> — {{ pass.position }}</NText>
            <NText depth="3">{{ pass.subcontractorName }} · {{ pass.projectName ?? '—' }}</NText>
            <NText depth="3">Выдан: {{ new Date(pass.issuedAt).toLocaleString('ru-RU') }}</NText>
            <NAlert v-if="!pass.hasReferencePhoto" type="warning" :show-icon="false">
              Нет эталонного фото — загрузите фото в {{ appBrand.brandName }} (Сотрудники → Фото).
            </NAlert>
          </NSpace>
        </NCard>

        <NCard size="small" title="Face ID" :class="{ 'step-disabled': !pass?.isActive }">
          <NSpace vertical :size="12">
            <NText v-if="!pass?.isActive" depth="3">
              Сначала успешно проверьте QR пропуска (шаг 1).
            </NText>
            <video
              v-show="faceCameraOn"
              ref="videoRef"
              class="face-video"
              playsinline
              muted
            />
            <div v-if="!faceCameraOn" class="face-placeholder">Камера выключена</div>
            <NSpace wrap>
              <NButton :disabled="!pass?.isActive" @click="startFaceCamera">Камера Face ID</NButton>
              <NButton
                type="primary"
                :loading="verifying"
                :disabled="!pass?.isActive || !pass?.hasReferencePhoto || !faceCameraOn"
                @click="verifyFace"
              >
                Проверить лицо
              </NButton>
            </NSpace>
            <NAlert v-if="verifyResult" :type="verifyResult.matched ? 'success' : 'error'" :show-icon="false">
              {{ verifyResult.message }}
              <template v-if="verifyResult.confidence"> ({{ Math.round(verifyResult.confidence * 100) }}%)</template>
              <template v-if="verifyResult.siteVisitRecorded">
                <br>Запись о проходе на объект сохранена в {{ appBrand.brandName }}.
              </template>
            </NAlert>
          </NSpace>
        </NCard>
      </NSpace>
    </NCard>
  </div>
</template>

<style scoped>
.page {
  min-height: 100vh;
  padding: 24px 16px;
  background: #f5f7fb;
}
.qr-box {
  width: 100%;
  max-width: 420px;
  min-height: 280px;
  border-radius: 12px;
  overflow: hidden;
  background: #111;
}
.face-video,
.face-placeholder {
  width: 100%;
  max-width: 420px;
  border-radius: 12px;
  background: #111;
  aspect-ratio: 4 / 3;
  object-fit: cover;
}
.face-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #888;
  font-size: 14px;
}
.step-disabled {
  opacity: 0.85;
}
</style>
