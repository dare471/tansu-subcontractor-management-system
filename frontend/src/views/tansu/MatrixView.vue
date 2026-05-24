<script setup lang="ts">
import { ref, watch, onMounted, computed, h } from 'vue';
import {
  NCard, NSpace, NSelect, NButton, NDataTable, useMessage, NEmpty, NTag, NSpin
} from 'naive-ui';
import { matrixApi, type MatrixStep, type MatrixSummary } from '@/api/matrix';
import { projectsApi, type Project } from '@/api/projects';
import { subcontractorsApi, type Subcontractor } from '@/api/subcontractors';
import { usersApi, type User } from '@/api/users';
import { toApiError } from '@/api/client';

const msg = useMessage();
const projects = ref<Project[]>([]);
const subs = ref<Subcontractor[]>([]);
const users = ref<User[]>([]);
const summaries = ref<MatrixSummary[]>([]);
const summariesLoading = ref(false);

const projectOid = ref<string | null>(null);
const subcontractorId = ref<string | null>(null);
const steps = ref<MatrixStep[]>([]);
const newUserId = ref<string | null>(null);
const saving = ref(false);
const editorRef = ref<HTMLElement | null>(null);

const projectOptions = computed(() => projects.value.map((p) => ({ label: p.name || p.projectOid, value: p.projectOid })));
const subOptions = computed(() => subs.value.map((s) => ({ label: s.name, value: s.id })));
const tansuUserOptions = computed(() => users.value.filter((u) => u.userType === 'TANSU' && u.isActive)
  .map((u) => ({ label: `${u.fullName} (${u.email})`, value: u.id })));

async function loadRefs() {
  const [p, s, u] = await Promise.all([
    projectsApi.list(), subcontractorsApi.list(), usersApi.list({ userType: 'TANSU' })
  ]);
  projects.value = p;
  subs.value = s;
  users.value = u;
}

async function loadSummaries() {
  summariesLoading.value = true;
  try {
    summaries.value = await matrixApi.list();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    summariesLoading.value = false;
  }
}

async function loadMatrix() {
  if (!projectOid.value || !subcontractorId.value) { steps.value = []; return; }
  try {
    steps.value = await matrixApi.get(projectOid.value, subcontractorId.value);
  } catch (e) { msg.error(toApiError(e).detail); }
}

function openMatrix(summary: MatrixSummary) {
  projectOid.value = summary.projectOid;
  subcontractorId.value = summary.subcontractorId;
  editorRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function addStep() {
  if (!newUserId.value) return;
  const user = users.value.find((u) => u.id === newUserId.value);
  if (!user) return;
  const order = steps.value.length === 0 ? 1 : Math.max(...steps.value.map((s) => s.orderNo)) + 1;
  steps.value.push({
    id: 'temp-' + crypto.randomUUID(), orderNo: order, userId: user.id,
    userFullName: user.fullName, userEmail: user.email
  });
  newUserId.value = null;
}

function moveUp(idx: number) {
  if (idx === 0) return;
  const arr = steps.value;
  [arr[idx - 1], arr[idx]] = [arr[idx], arr[idx - 1]];
  for (let i = 0; i < arr.length; i++) arr[i].orderNo = i + 1;
}

function moveDown(idx: number) {
  if (idx === steps.value.length - 1) return;
  const arr = steps.value;
  [arr[idx + 1], arr[idx]] = [arr[idx], arr[idx + 1]];
  for (let i = 0; i < arr.length; i++) arr[i].orderNo = i + 1;
}

function removeStep(idx: number) {
  steps.value.splice(idx, 1);
  for (let i = 0; i < steps.value.length; i++) steps.value[i].orderNo = i + 1;
}

async function save() {
  if (!projectOid.value || !subcontractorId.value) return;
  saving.value = true;
  try {
    const payload = steps.value.map((s) => ({ orderNo: s.orderNo, userId: s.userId }));
    steps.value = await matrixApi.set(projectOid.value, subcontractorId.value, payload);
    msg.success('Матрица сохранена');
    await loadSummaries();
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { saving.value = false; }
}

watch([projectOid, subcontractorId], loadMatrix);

onMounted(async () => {
  await Promise.all([loadRefs(), loadSummaries()]);
});

const columns = [
  { title: '№', key: 'orderNo', width: 60 },
  { title: 'ФИО', key: 'userFullName' },
  { title: 'Email', key: 'userEmail' },
  {
    title: 'Действия', key: 'actions', width: 220,
    render: (_row: MatrixStep, idx: number) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'small', onClick: () => moveUp(idx) }, () => '↑'),
      h(NButton, { size: 'small', onClick: () => moveDown(idx) }, () => '↓'),
      h(NButton, { size: 'small', type: 'error', onClick: () => removeStep(idx) }, () => 'Удалить')
    ])
  }
];
</script>

<template>
  <NSpace vertical :size="20">
    <NCard title="Настроенные матрицы">
      <NSpin :show="summariesLoading">
        <div v-if="summaries.length" class="t-matrix-grid">
          <div
            v-for="item in summaries"
            :key="item.projectOid + '-' + item.subcontractorId"
            class="t-matrix-card"
          >
            <div class="t-matrix-card__head">
              <div>
                <div class="t-matrix-card__project">{{ item.projectName || item.projectOid }}</div>
                <div class="t-matrix-card__sub">{{ item.subcontractorName }}</div>
              </div>
              <NTag type="info" size="small">{{ item.steps.length }} {{ item.steps.length === 1 ? 'шаг' : 'шагов' }}</NTag>
            </div>
            <ol class="t-matrix-card__steps">
              <li v-for="step in item.steps" :key="step.id">
                <span class="t-matrix-card__step-no">{{ step.orderNo }}.</span>
                {{ step.userFullName }}
                <span class="t-matrix-card__email">{{ step.userEmail }}</span>
              </li>
            </ol>
            <NButton size="small" type="primary" ghost block @click="openMatrix(item)">
              Редактировать
            </NButton>
          </div>
        </div>
        <NEmpty v-else description="Нет матриц" />
      </NSpin>
    </NCard>

    <div ref="editorRef">
    <NCard title="Редактор матрицы">
      <NSpace vertical>
        <NSpace>
          <NSelect v-model:value="projectOid" :options="projectOptions" placeholder="Проект" style="width:300px" filterable />
          <NSelect v-model:value="subcontractorId" :options="subOptions" placeholder="Субподрядчик" style="width:300px" filterable />
        </NSpace>
        <template v-if="projectOid && subcontractorId">
          <NSpace>
            <NSelect v-model:value="newUserId" :options="tansuUserOptions" placeholder="Добавить согласующего (ТАНСУ)" style="width:380px" filterable />
            <NButton type="primary" :disabled="!newUserId" @click="addStep">+ Добавить шаг</NButton>
            <NButton type="success" :loading="saving" :disabled="!steps.length" @click="save">Сохранить матрицу</NButton>
          </NSpace>
          <NDataTable :columns="columns" :data="steps" :row-key="(r) => r.id" />
        </template>
        <NEmpty v-else description="Выберите проект и субподрядчика" />
      </NSpace>
    </NCard>
    </div>
  </NSpace>
</template>

<style scoped>
.t-matrix-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}

.t-matrix-card {
  background: var(--brand-bg);
  border: 1px solid var(--brand-border);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.t-matrix-card:hover {
  border-color: var(--brand-orange);
  box-shadow: 0 4px 12px rgba(238, 108, 28, 0.08);
}

.t-matrix-card__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
}

.t-matrix-card__project {
  font-weight: 700;
  font-size: 15px;
  line-height: 1.3;
}

.t-matrix-card__sub {
  color: var(--brand-text-muted);
  font-size: 13px;
  margin-top: 4px;
}

.t-matrix-card__steps {
  margin: 0;
  padding-left: 0;
  list-style: none;
  flex: 1;
}

.t-matrix-card__steps li {
  font-size: 13px;
  padding: 6px 0;
  border-bottom: 1px dashed var(--brand-border);
}

.t-matrix-card__steps li:last-child {
  border-bottom: none;
}

.t-matrix-card__step-no {
  color: var(--brand-orange);
  font-weight: 700;
  margin-right: 4px;
}

.t-matrix-card__email {
  display: block;
  color: var(--brand-text-muted);
  font-size: 12px;
  margin-top: 2px;
}
</style>
