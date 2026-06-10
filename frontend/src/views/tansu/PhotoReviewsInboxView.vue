<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import {
  NCard, NSpace, NButton, NDataTable, NInput, NAlert,
  useMessage, type DataTableColumns
} from 'naive-ui';
import { photoReviewsApi, type PendingPhotoReview } from '@/api/photoReviews';
import EmployeePhotoAvatar from '@/components/EmployeePhotoAvatar.vue';
import { toApiError } from '@/api/client';
import AppDrawer from '@/components/AppDrawer.vue';

const msg = useMessage();
const items = ref<PendingPhotoReview[]>([]);
const loading = ref(false);

const decisionItem = ref<PendingPhotoReview | null>(null);
const decisionMode = ref<'approve' | 'reject'>('approve');
const comment = ref('');
const submitting = ref(false);

async function load() {
  loading.value = true;
  try { items.value = await photoReviewsApi.pending(); }
  catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

function openApprove(item: PendingPhotoReview) {
  decisionItem.value = item;
  decisionMode.value = 'approve';
  comment.value = '';
}

function openReject(item: PendingPhotoReview) {
  decisionItem.value = item;
  decisionMode.value = 'reject';
  comment.value = '';
}

async function confirm() {
  if (!decisionItem.value) return;
  if (decisionMode.value === 'reject' && comment.value.trim().length < 3) {
    msg.warning('Укажите причину отклонения (не короче 3 символов).');
    return;
  }
  submitting.value = true;
  try {
    if (decisionMode.value === 'approve') {
      await photoReviewsApi.approve(decisionItem.value.employeeId, comment.value.trim() || undefined);
      msg.success('Фото одобрено');
    } else {
      await photoReviewsApi.reject(decisionItem.value.employeeId, comment.value.trim());
      msg.success('Фото отклонено, субподрядчик получит причину');
    }
    decisionItem.value = null;
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { submitting.value = false; }
}

const TABLE_SCROLL_X = 1200;

const columns: DataTableColumns<PendingPhotoReview> = [
  {
    title: 'Фото', key: 'photo', width: 72, align: 'center',
    render: (row) => h(EmployeePhotoAvatar, {
      employeeId: row.employeeId,
      fullName: row.fullName,
      photoPath: row.photoPath,
      size: 40
    })
  },
  {
    title: 'Сотрудник', key: 'fullName', width: 200,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Должность', key: 'position', width: 160,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Субподрядчик', key: 'subcontractorName', width: 180,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Проект', key: 'projectName', width: 200,
    ellipsis: { tooltip: true },
    render: (r) => r.projectName ?? '—'
  },
  {
    title: 'Загрузил', key: 'uploadedByFullName', width: 180,
    ellipsis: { tooltip: true },
    render: (r) => r.uploadedByFullName ?? r.uploadedByEmail ?? '—'
  },
  {
    title: 'Загружено', key: 'uploadedAt', width: 160,
    render: (r) => new Date(r.uploadedAt).toLocaleString('ru-RU')
  },
  {
    title: 'Действия', key: 'a', width: 240,
    render: (row) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'small', type: 'primary', onClick: () => openApprove(row) }, () => 'Одобрить'),
      h(NButton, { size: 'small', type: 'error', onClick: () => openReject(row) }, () => 'Отклонить')
    ])
  }
];

onMounted(load);
</script>

<template>
  <NCard title="Проверка фото сотрудников">
    <NSpace vertical>
      <NAlert type="info" :show-icon="false">
        JPEG, одно лицо, нейтральный фон. Допустимый размер файла задаётся в настройках системы.
        После одобрения субподрядчик сможет отправить сотрудника на согласование.
      </NAlert>
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(r) => r.employeeId"
          :scroll-x="TABLE_SCROLL_X"
          size="small"
        />
      </div>
    </NSpace>

    <AppDrawer
      :show="!!decisionItem"
      :title="decisionMode === 'approve' ? 'Одобрить фото' : 'Отклонить фото'"
      width="narrow"
      @update:show="(v) => { if (!v) decisionItem = null }"
    >
      <p v-if="decisionItem" style="margin:0 0 8px">
        {{ decisionItem.fullName }} · {{ decisionItem.subcontractorName }}
      </p>
      <NInput
        v-model:value="comment"
        type="textarea"
        :placeholder="decisionMode === 'approve'
          ? 'Комментарий (необязательно)'
          : 'Причина отклонения (обязательно)'"
        :rows="4"
      />
      <NSpace justify="end" style="margin-top:16px">
        <NButton @click="decisionItem = null">Отмена</NButton>
        <NButton
          :type="decisionMode === 'approve' ? 'primary' : 'error'"
          :loading="submitting"
          @click="confirm"
        >
          {{ decisionMode === 'approve' ? 'Одобрить' : 'Отклонить' }}
        </NButton>
      </NSpace>
    </AppDrawer>
  </NCard>
</template>
