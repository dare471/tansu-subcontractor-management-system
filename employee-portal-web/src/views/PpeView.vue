<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NTag, NSkeleton, useMessage } from 'naive-ui';
import PageHeader from '@/components/PageHeader.vue';
import {
  employeePortalApi,
  formatDateTime,
  ppeItemIcon,
  ppeItemLabel,
  type EmployeePpeSummary,
  type PpeIssuance
} from '@/api/employeePortal';
import { toApiError } from '@/api/client';

const msg = useMessage();
const data = ref<EmployeePpeSummary | null>(null);
const loading = ref(true);

const activeItems = ref<{ type: string; item: PpeIssuance | null }[]>([]);

async function load() {
  loading.value = true;
  try {
    data.value = await employeePortalApi.ppe();
    activeItems.value = [
      { type: 'helmet', item: data.value.activeHelmet },
      { type: 'uniform', item: data.value.activeUniform }
    ];
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <PageHeader title="СИЗ" subtitle="Каска, униформа и история выдачи" />

  <template v-if="loading">
    <div class="portal-card"><NSkeleton height="140px" :sharp="false" /></div>
  </template>

  <template v-else-if="data">
    <div class="ppe-grid" style="margin-bottom:16px">
      <article
        v-for="{ type, item } in activeItems"
        :key="type"
        class="ppe-item"
        :class="item ? 'ppe-item--ok' : 'ppe-item--missing'"
      >
        <div class="ppe-item__icon">{{ ppeItemIcon(type) }}</div>
        <div class="ppe-item__body">
          <p class="ppe-item__name">{{ ppeItemLabel(type) }}</p>
          <template v-if="item">
            <p class="ppe-item__meta">Выдано {{ formatDateTime(item.issuedAt) }}</p>
            <p v-if="item.size" class="ppe-item__meta">Размер: {{ item.size }}</p>
            <p v-if="item.inventoryNumber" class="ppe-item__meta">№ {{ item.inventoryNumber }}</p>
            <NTag type="success" size="small" round style="margin-top:6px">На руках</NTag>
          </template>
          <template v-else>
            <p class="ppe-item__meta">Не выдано — обратитесь на пункт выдачи СИЗ</p>
            <NTag type="warning" size="small" round style="margin-top:6px">Ожидает</NTag>
          </template>
        </div>
      </article>
    </div>

    <section v-if="data.history.length" class="portal-card">
      <h2 class="portal-card__title">История выдачи</h2>
      <ul style="list-style:none;margin:0;padding:0">
        <li
          v-for="row in data.history"
          :key="row.id"
          style="padding:12px 0;border-bottom:1px solid var(--color-border)"
        >
          <div style="display:flex;justify-content:space-between;gap:8px;flex-wrap:wrap">
            <span style="font-weight:500">{{ ppeItemIcon(row.itemType) }} {{ ppeItemLabel(row.itemType) }}</span>
            <NTag :type="row.isActive ? 'success' : 'default'" size="small">
              {{ row.isActive ? 'Активно' : 'Возвращено' }}
            </NTag>
          </div>
          <p style="margin:4px 0 0;font-size:0.8125rem;color:var(--color-text-muted)">
            {{ formatDateTime(row.issuedAt) }} · {{ row.issuedByFullName }}
            <template v-if="row.size"> · р. {{ row.size }}</template>
          </p>
          <p v-if="row.returnedAt" style="margin:2px 0 0;font-size:0.8125rem;color:var(--color-text-muted)">
            Возврат: {{ formatDateTime(row.returnedAt) }}
          </p>
        </li>
      </ul>
    </section>
  </template>
</template>
