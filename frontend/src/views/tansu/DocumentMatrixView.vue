<script setup lang="ts">
import { ref, watch, onMounted, computed, h } from 'vue';
import {
  NCard, NSpace, NSelect, NButton, NDataTable, NEmpty, NTag, NSpin, useMessage
} from 'naive-ui';
import {
  documentMatrixApi, APPROVER_ROLES, REQUEST_TYPES, approverRoleLabel, requestTypeLabel,
  type DocumentMatrixStep, type DocumentMatrixSummary
} from '@/api/documentRequests';
import { projectsApi, type Project } from '@/api/projects';
import { subcontractorsApi, type Subcontractor } from '@/api/subcontractors';
import { toApiError } from '@/api/client';

const msg = useMessage();
const projects = ref<Project[]>([]);
const subs = ref<Subcontractor[]>([]);
const summaries = ref<DocumentMatrixSummary[]>([]);
const summariesLoading = ref(false);

const projectOid = ref<string | null>(null);
const subcontractorId = ref<string | null>(null);
const requestType = ref<string>('leave');
const steps = ref<DocumentMatrixStep[]>([]);
const newRole = ref<string | null>(null);
const saving = ref(false);
const editorRef = ref<HTMLElement | null>(null);

const projectOptions = computed(() => projects.value.map((p) => ({ label: p.name || p.projectOid, value: p.projectOid })));
const subOptions = computed(() => subs.value.map((s) => ({ label: s.name, value: s.id })));
const typeOptions = REQUEST_TYPES.map((t) => ({ label: t.label, value: t.value }));
const roleOptions = APPROVER_ROLES.map((r) => ({ label: r.label, value: r.value }));

async function loadRefs() {
  const [p, s] = await Promise.all([projectsApi.list(), subcontractorsApi.list()]);
  projects.value = p;
  subs.value = s;
}

async function loadSummaries() {
  summariesLoading.value = true;
  try {
    summaries.value = await documentMatrixApi.list();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    summariesLoading.value = false;
  }
}

async function loadMatrix() {
  if (!projectOid.value || !subcontractorId.value) { steps.value = []; return; }
  try {
    steps.value = await documentMatrixApi.get(projectOid.value, subcontractorId.value, requestType.value);
  } catch (e) { msg.error(toApiError(e).detail); }
}

function openMatrix(summary: DocumentMatrixSummary) {
  projectOid.value = summary.projectOid;
  subcontractorId.value = summary.subcontractorId;
  requestType.value = summary.requestType;
  editorRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function addStep() {
  if (!newRole.value) return;
  const order = steps.value.length === 0 ? 1 : Math.max(...steps.value.map((s) => s.orderNo)) + 1;
  steps.value.push({
    id: 'temp-' + crypto.randomUUID(),
    orderNo: order,
    approverRole: newRole.value
  });
  newRole.value = null;
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
    const payload = steps.value.map((s) => ({ orderNo: s.orderNo, approverRole: s.approverRole }));
    steps.value = await documentMatrixApi.set(
      projectOid.value, subcontractorId.value, requestType.value, payload
    );
    msg.success('Матрица заявок сохранена');
    await loadSummaries();
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { saving.value = false; }
}

watch([projectOid, subcontractorId, requestType], loadMatrix);

onMounted(async () => {
  await Promise.all([loadRefs(), loadSummaries()]);
});

const columns = [
  { title: '№', key: 'orderNo', width: 60 },
  { title: 'Роль согласующего', key: 'approverRole', render: (row: DocumentMatrixStep) => approverRoleLabel(row.approverRole) },
  {
    title: 'Действия', key: 'actions', width: 220,
    render: (_row: DocumentMatrixStep, idx: number) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'small', onClick: () => moveUp(idx) }, () => '↑'),
      h(NButton, { size: 'small', onClick: () => moveDown(idx) }, () => '↓'),
      h(NButton, { size: 'small', type: 'error', onClick: () => removeStep(idx) }, () => 'Удалить')
    ])
  }
];
</script>

<template>
  <NSpace vertical :size="20">
    <NCard title="Настроенные матрицы заявок">
      <NSpin :show="summariesLoading">
        <div v-if="summaries.length" class="t-matrix-grid">
          <div
            v-for="item in summaries"
            :key="item.projectOid + '-' + item.subcontractorId + '-' + item.requestType"
            class="t-matrix-card"
          >
            <div class="t-matrix-card__head">
              <div>
                <div class="t-matrix-card__project">{{ item.projectName || item.projectOid }}</div>
                <div class="t-matrix-card__sub">{{ item.subcontractorName }}</div>
              </div>
              <NTag type="warning" size="small">{{ requestTypeLabel(item.requestType) }}</NTag>
            </div>
            <div class="t-matrix-card__meta">{{ item.steps.length }} {{ item.steps.length === 1 ? 'шаг' : 'шагов' }}</div>
            <ol class="t-matrix-card__steps">
              <li v-for="step in item.steps" :key="step.id">
                <span class="t-matrix-card__step-no">{{ step.orderNo }}.</span>
                {{ approverRoleLabel(step.approverRole) }}
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
      <NCard title="Редактор матрицы заявок">
        <NSpace vertical>
          <NSpace wrap>
            <NSelect v-model:value="projectOid" :options="projectOptions" placeholder="Проект" style="width:280px" filterable />
            <NSelect v-model:value="subcontractorId" :options="subOptions" placeholder="Субподрядчик" style="width:280px" filterable />
            <NSelect v-model:value="requestType" :options="typeOptions" placeholder="Тип заявки" style="width:240px" />
          </NSpace>
          <template v-if="projectOid && subcontractorId">
            <NSpace>
              <NSelect v-model:value="newRole" :options="roleOptions" placeholder="Добавить роль" style="width:280px" />
              <NButton type="primary" :disabled="!newRole" @click="addStep">+ Добавить шаг</NButton>
              <NButton type="success" :loading="saving" :disabled="!steps.length" @click="save">Сохранить</NButton>
            </NSpace>
            <NDataTable :columns="columns" :data="steps" :row-key="(r) => r.id" />
              
          </template>
          <NEmpty v-else description="Выберите проект, субподрядчика и тип" />
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
  gap: 10px;
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

.t-matrix-card__meta {
  font-size: 12px;
  color: var(--brand-text-muted);
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
</style>
