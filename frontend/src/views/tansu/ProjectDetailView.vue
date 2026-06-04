<script setup lang="ts">
import { ref, onMounted, h, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  NCard, NSpace, NButton, NDescriptions, NDescriptionsItem, NDataTable, NTag,
  NModal, NForm, NFormItem, NInput, NInputNumber, NSelect, NUpload, NEmpty,
  NTabs, NTabPane, NProgress, useMessage, type DataTableColumns, type UploadFileInfo
} from 'naive-ui';
import { projectsApi, PROJECT_DOCUMENT_TYPES, formatBudget, approvalStatusLabel } from '@/api/projects';
import type { ProjectDetail, ProjectStaffOption } from '@/api/projects';
import { subcontractorsApi, type Subcontractor } from '@/api/subcontractors';
import { toApiError, apiClient } from '@/api/client';
import { useAuthStore } from '@/stores/auth';

const route = useRoute();
const router = useRouter();
const msg = useMessage();
const auth = useAuthStore();

const projectOid = computed(() => String(route.params.projectOid));
const detail = ref<ProjectDetail | null>(null);
const staff = ref<ProjectStaffOption[]>([]);
const loading = ref(false);
const showEdit = ref(false);
const saving = ref(false);
const workforceFilter = ref<'all' | 'approved'>('approved');
const showUpload = ref(false);
const uploadName = ref('');
const uploadType = ref('contract');
const uploadFile = ref<File | null>(null);
const uploading = ref(false);
const allSubcontractors = ref<Subcontractor[]>([]);
const showAddSub = ref(false);
const addSubForm = ref({ subcontractorId: null as string | null, activityType: '' });
const addingSub = ref(false);
const showEditSub = ref(false);
const editSubTarget = ref<ProjectDetail['subcontractors'][0] | null>(null);
const editSubActivityType = ref('');
const savingSub = ref(false);

const canEdit = computed(() =>
  !auth.isReadOnly &&
  (auth.permissions.isGlobalAdmin || auth.permissions.canManageProjects));

const editForm = ref({
  name: '',
  customerName: '',
  customerPhone: '',
  customerEmail: '',
  budgetAmount: null as number | null,
  budgetCurrency: 'KZT',
  responsibleAdminUserId: null as string | null,
  projectManagerUserId: null as string | null
});

const staffOptions = computed(() =>
  staff.value.map((s) => ({
    label: `${s.fullName} (${s.email})`,
    value: s.id
  }))
);

const filteredWorkforce = computed(() => {
  if (!detail.value) return [];
  if (workforceFilter.value === 'approved')
    return detail.value.workforce.filter((w) => w.approvalStatus === 'approved');
  return detail.value.workforce;
});

async function load() {
  loading.value = true;
  try {
    detail.value = await projectsApi.get(projectOid.value);
  } catch (e) {
    msg.error(toApiError(e).detail);
    detail.value = null;
  } finally {
    loading.value = false;
  }
}

async function loadStaff() {
  if (!canEdit.value) return;
  try {
    [staff.value, allSubcontractors.value] = await Promise.all([
      projectsApi.staffOptions(),
      subcontractorsApi.list()
    ]);
  } catch {
    staff.value = [];
    allSubcontractors.value = [];
  }
}

const availableSubOptions = computed(() => {
  const bound = new Set(detail.value?.subcontractors.map((s) => s.id) ?? []);
  return allSubcontractors.value
    .filter((s) => !bound.has(s.id))
    .map((s) => ({ label: `${s.name} (${s.bin})`, value: s.id }));
});

function openAddSub() {
  addSubForm.value = { subcontractorId: null, activityType: '' };
  showAddSub.value = true;
}

async function submitAddSub() {
  if (!addSubForm.value.subcontractorId || !addSubForm.value.activityType.trim()) {
    msg.warning('Выберите субподрядчика и укажите вид деятельности');
    return;
  }
  addingSub.value = true;
  try {
    await projectsApi.bindSubcontractor(
      projectOid.value,
      addSubForm.value.subcontractorId,
      addSubForm.value.activityType.trim()
    );
    showAddSub.value = false;
    await load();
    msg.success('Субподрядчик добавлен');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { addingSub.value = false; }
}

function openEditSub(row: ProjectDetail['subcontractors'][0]) {
  editSubTarget.value = row;
  editSubActivityType.value = row.activityType;
  showEditSub.value = true;
}

async function saveEditSub() {
  if (!editSubTarget.value || !editSubActivityType.value.trim()) return;
  savingSub.value = true;
  try {
    await projectsApi.updateSubcontractorBinding(
      projectOid.value,
      editSubTarget.value.id,
      editSubActivityType.value.trim()
    );
    showEditSub.value = false;
    await load();
    msg.success('Сохранено');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { savingSub.value = false; }
}

function openEdit() {
  if (!detail.value) return;
  editForm.value = {
    name: detail.value.name ?? '',
    customerName: detail.value.customerName ?? '',
    customerPhone: detail.value.customerPhone ?? '',
    customerEmail: detail.value.customerEmail ?? '',
    budgetAmount: detail.value.budgetAmount,
    budgetCurrency: detail.value.budgetCurrency || 'KZT',
    responsibleAdminUserId: detail.value.responsibleAdminUserId,
    projectManagerUserId: detail.value.projectManagerUserId
  };
  showEdit.value = true;
}

async function saveEdit() {
  saving.value = true;
  try {
    detail.value = await projectsApi.update(projectOid.value, {
      name: editForm.value.name || null,
      customerName: editForm.value.customerName || null,
      customerPhone: editForm.value.customerPhone || null,
      customerEmail: editForm.value.customerEmail || null,
      budgetAmount: editForm.value.budgetAmount,
      budgetCurrency: editForm.value.budgetCurrency,
      responsibleAdminUserId: editForm.value.responsibleAdminUserId,
      projectManagerUserId: editForm.value.projectManagerUserId
    });
    showEdit.value = false;
    msg.success('Сохранено');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { saving.value = false; }
}

function onUploadChange(options: { file: UploadFileInfo }) {
  uploadFile.value = options.file.file ?? null;
}

async function submitUpload() {
  if (!uploadFile.value) {
    msg.warning('Выберите файл');
    return;
  }
  if (!uploadName.value.trim()) {
    msg.warning('Укажите название документа');
    return;
  }
  uploading.value = true;
  try {
    await projectsApi.uploadDocument(
      projectOid.value,
      uploadFile.value,
      uploadName.value.trim(),
      uploadType.value
    );
    showUpload.value = false;
    uploadName.value = '';
    uploadFile.value = null;
    await load();
    msg.success('Документ загружен');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { uploading.value = false; }
}

async function openDocument(doc: ProjectDetail['documents'][0]) {
  try {
    const res = await apiClient.get(
      `/api/projects/${projectOid.value}/documents/${doc.id}`,
      { responseType: 'blob' }
    );
    const url = URL.createObjectURL(res.data);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function removeDocument(id: string) {
  try {
    await projectsApi.deleteDocument(projectOid.value, id);
    await load();
    msg.success('Документ удалён');
  } catch (e) { msg.error(toApiError(e).detail); }
}

const subColumns = computed<DataTableColumns<ProjectDetail['subcontractors'][0]>>(() => {
  const cols: DataTableColumns<ProjectDetail['subcontractors'][0]> = [
    { title: 'Наименование', key: 'name', ellipsis: { tooltip: true } },
    { title: 'БИН', key: 'bin', width: 140 },
    { title: 'Вид деятельности', key: 'activityType', width: 200, ellipsis: { tooltip: true } },
    {
      title: '% выполнения', key: 'completionPercent', width: 160,
      render: (r) => h(NProgress, {
        type: 'line',
        percentage: r.completionPercent,
        indicatorPlacement: 'inside',
        processing: r.completionPercent > 0 && r.completionPercent < 100
      })
    },
    {
      title: 'Отчёт', key: 'progressReportedAt', width: 160,
      render: (r) => {
        if (!r.progressReportedAt) return '—';
        const date = new Date(r.progressReportedAt).toLocaleDateString('ru-RU');
        return r.progressReportedByFullName ? `${date} · ${r.progressReportedByFullName}` : date;
      }
    },
    { title: 'Сотрудников', key: 'employeesCount', width: 110 },
    { title: 'В работе', key: 'approvedEmployeesCount', width: 90 }
  ];
  if (canEdit.value) {
    cols.push({
      title: '', key: 'actions', width: 100,
      render: (r) => h(NButton, { size: 'small', onClick: () => openEditSub(r) }, () => 'Изменить')
    });
  }
  return cols;
});

const workforceColumns: DataTableColumns<ProjectDetail['workforce'][0]> = [
  { title: 'ФИО', key: 'fullName', ellipsis: { tooltip: true } },
  { title: 'Должность', key: 'position', width: 160, ellipsis: { tooltip: true } },
  { title: 'Субподрядчик', key: 'subcontractorName', width: 200, ellipsis: { tooltip: true } },
  {
    title: 'Статус', key: 'approvalStatus', width: 140,
    render: (r) => {
      const label = approvalStatusLabel(r.approvalStatus);
      const type = r.approvalStatus === 'approved' ? 'success'
        : r.approvalStatus === 'pending' ? 'warning'
          : r.approvalStatus === 'rejected' ? 'error' : 'default';
      return h(NTag, { size: 'small', type }, () => label);
    }
  }
];

const teamColumns: DataTableColumns<ProjectDetail['team'][0]> = [
  { title: 'ФИО', key: 'fullName', ellipsis: { tooltip: true } },
  { title: 'Email', key: 'email', width: 220, ellipsis: { tooltip: true } },
  { title: 'Должность', key: 'position', width: 180, ellipsis: { tooltip: true } },
  { title: 'Роль на проекте', key: 'roleLabel', width: 200 }
];

const docColumns: DataTableColumns<ProjectDetail['documents'][0]> = [
  { title: 'Название', key: 'name', ellipsis: { tooltip: true } },
  { title: 'Тип', key: 'documentTypeLabel', width: 120 },
  { title: 'Загрузил', key: 'uploadedByFullName', width: 160, ellipsis: { tooltip: true } },
  {
    title: 'Дата', key: 'uploadedAt', width: 160,
    render: (r) => new Date(r.uploadedAt).toLocaleString('ru-RU')
  },
  {
    title: '', key: 'a', width: 160,
    render: (r) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'small', onClick: () => openDocument(r) }, () => 'Открыть'),
      canEdit.value
        ? h(NButton, { size: 'small', type: 'error', onClick: () => removeDocument(r.id) }, () => 'Удалить')
        : null
    ])
  }
];

onMounted(async () => {
  await Promise.all([load(), loadStaff()]);
});
</script>

<template>
  <NSpace vertical :size="16">
    <NSpace align="center" justify="space-between">
      <NSpace align="center">
        <NButton quaternary @click="router.push({ name: 'projects' })">← Проекты</NButton>
        <h2 style="margin:0;font-size:20px">{{ detail?.name ?? 'Проект' }}</h2>
      </NSpace>
      <NButton v-if="canEdit && detail" type="primary" @click="openEdit">Редактировать</NButton>
    </NSpace>

    <NCard v-if="detail" :loading="loading">
      <NDescriptions label-placement="left" :column="2" bordered size="small">
        <NDescriptionsItem label="OID">{{ detail.projectOid }}</NDescriptionsItem>
        <NDescriptionsItem label="Смета">
          {{ formatBudget(detail.budgetAmount, detail.budgetCurrency) }}
        </NDescriptionsItem>
        <NDescriptionsItem label="Заказчик">{{ detail.customerName ?? '—' }}</NDescriptionsItem>
        <NDescriptionsItem label="Телефон">{{ detail.customerPhone ?? '—' }}</NDescriptionsItem>
        <NDescriptionsItem label="Email заказчика">{{ detail.customerEmail ?? '—' }}</NDescriptionsItem>
        <NDescriptionsItem label="Субподрядчиков">{{ detail.subcontractorsCount }}</NDescriptionsItem>
        <NDescriptionsItem label="Ответственный админ">
          <template v-if="detail.responsibleAdminFullName">
            {{ detail.responsibleAdminFullName }}
            <span v-if="detail.responsibleAdminEmail" style="color:var(--brand-text-muted)">
              · {{ detail.responsibleAdminEmail }}
            </span>
          </template>
          <template v-else>—</template>
        </NDescriptionsItem>
        <NDescriptionsItem label="Руководитель проекта">
          <template v-if="detail.projectManagerFullName">
            {{ detail.projectManagerFullName }}
            <span v-if="detail.projectManagerEmail" style="color:var(--brand-text-muted)">
              · {{ detail.projectManagerEmail }}
            </span>
          </template>
          <template v-else>—</template>
        </NDescriptionsItem>
      </NDescriptions>
    </NCard>

    <NCard v-else-if="!loading">
      <NEmpty description="Проект не найден" />
    </NCard>

    <NTabs v-if="detail" type="line" animated>
      <NTabPane name="subs" tab="Субподрядчики">
        <NSpace vertical>
          <NButton v-if="canEdit" type="primary" size="small" @click="openAddSub">
            + Добавить субподрядчика
          </NButton>
          <div class="t-table-wrap">
            <NDataTable
              :columns="subColumns"
              :data="detail.subcontractors"
              :row-key="(r) => r.id"
              :scroll-x="1100"
              size="small"
            />
          </div>
        </NSpace>
      </NTabPane>

      <NTabPane name="workforce" :tab="`Персонал (${filteredWorkforce.length})`">
        <NSpace style="margin-bottom:12px">
          <NButton
            :type="workforceFilter === 'approved' ? 'primary' : 'default'"
            size="small"
            @click="workforceFilter = 'approved'"
          >
            В работе (согласованы)
          </NButton>
          <NButton
            :type="workforceFilter === 'all' ? 'primary' : 'default'"
            size="small"
            @click="workforceFilter = 'all'"
          >
            Все
          </NButton>
        </NSpace>
        <div class="t-table-wrap">
          <NDataTable
            :columns="workforceColumns"
            :data="filteredWorkforce"
            :row-key="(r) => r.employeeId"
            size="small"
          />
        </div>
      </NTabPane>

      <NTabPane name="team" tab="Команда ТАНСУ">
        <div class="t-table-wrap">
          <NDataTable
            :columns="teamColumns"
            :data="detail.team"
            :row-key="(r) => r.userId"
            size="small"
          />
        </div>
      </NTabPane>

      <NTabPane name="docs" tab="Документы">
        <NSpace vertical>
          <NButton v-if="canEdit" type="primary" size="small" @click="showUpload = true">
            + Загрузить документ
          </NButton>
          <div class="t-table-wrap">
            <NDataTable
              :columns="docColumns"
              :data="detail.documents"
              :row-key="(r) => r.id"
              size="small"
            />
          </div>
        </NSpace>
      </NTabPane>
    </NTabs>

    <NModal v-model:show="showEdit" preset="card" title="Редактирование проекта" style="width:560px">
      <NForm @submit.prevent="saveEdit">
        <NFormItem label="Название"><NInput v-model:value="editForm.name" /></NFormItem>
        <NFormItem label="Заказчик"><NInput v-model:value="editForm.customerName" /></NFormItem>
        <NFormItem label="Телефон"><NInput v-model:value="editForm.customerPhone" /></NFormItem>
        <NFormItem label="Email"><NInput v-model:value="editForm.customerEmail" /></NFormItem>
        <NFormItem label="Смета">
          <NInputNumber v-model:value="editForm.budgetAmount" :min="0" style="width:100%" />
        </NFormItem>
        <NFormItem label="Ответственный админ">
          <NSelect
            v-model:value="editForm.responsibleAdminUserId"
            :options="staffOptions"
            filterable
            clearable
          />
        </NFormItem>
        <NFormItem label="Руководитель проекта">
          <NSelect
            v-model:value="editForm.projectManagerUserId"
            :options="staffOptions"
            filterable
            clearable
          />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showEdit = false">Отмена</NButton>
          <NButton type="primary" :loading="saving" @click="saveEdit">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal v-model:show="showUpload" preset="card" title="Документ проекта" style="width:480px">
      <NForm @submit.prevent="submitUpload">
        <NFormItem label="Название"><NInput v-model:value="uploadName" /></NFormItem>
        <NFormItem label="Тип">
          <NSelect v-model:value="uploadType" :options="PROJECT_DOCUMENT_TYPES" />
        </NFormItem>
        <NFormItem label="Файл">
          <NUpload :max="1" @change="onUploadChange">
            <NButton>Выбрать файл</NButton>
          </NUpload>
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showUpload = false">Отмена</NButton>
          <NButton type="primary" :loading="uploading" @click="submitUpload">Загрузить</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal v-model:show="showAddSub" preset="card" title="Добавить субподрядчика" style="width:520px">
      <NForm @submit.prevent="submitAddSub">
        <NFormItem label="Субподрядчик">
          <NSelect
            v-model:value="addSubForm.subcontractorId"
            :options="availableSubOptions"
            filterable
            placeholder="Выберите из справочника"
          />
        </NFormItem>
        <NFormItem label="Вид деятельности">
          <NInput v-model:value="addSubForm.activityType" placeholder="Например: Монтажные работы" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showAddSub = false">Отмена</NButton>
          <NButton type="primary" :loading="addingSub" @click="submitAddSub">Добавить</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal v-model:show="showEditSub" preset="card" title="Вид деятельности" style="width:480px">
      <NForm @submit.prevent="saveEditSub">
        <NFormItem label="Субподрядчик">
          <NInput :value="editSubTarget?.name" disabled />
        </NFormItem>
        <NFormItem label="Вид деятельности">
          <NInput v-model:value="editSubActivityType" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showEditSub = false">Отмена</NButton>
          <NButton type="primary" :loading="savingSub" @click="saveEditSub">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </NSpace>
</template>
