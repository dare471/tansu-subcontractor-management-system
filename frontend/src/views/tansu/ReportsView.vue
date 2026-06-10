<script setup lang="ts">
import { ref, computed, onMounted, defineAsyncComponent } from 'vue';
import { NCard, NGrid, NGi, NButton, NSpace, NDataTable, NSpin, NEmpty, useMessage } from 'naive-ui';
import { reportsApi, type SubcontractorCompliance } from '@/api/reports';
import { employeesApi } from '@/api/employees';
import { approvalsApi } from '@/api/approvals';
import { toApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';
import {
  useReportsDashboard,
  loadTansuReportsCharts,
  loadSubcontractorReportsCharts,
  loadSubcontractorDocumentChart
} from '@/composables/useReportsDashboard';

const DashboardChart = defineAsyncComponent(
  () => import('@/components/dashboard/DashboardChart.vue')
);

const auth = useAuthStore();
const msg = useMessage();
const loading = ref(true);
const compliance = ref<SubcontractorCompliance[]>([]);

const {
  personnelStatus,
  compliance: complianceChart,
  siteVisits,
  incidents,
  pendingBySub,
  byProject,
  documentRequests
} = useReportsDashboard();

const hasCharts = computed(() =>
  !!(personnelStatus.value || complianceChart.value || siteVisits.value ||
    incidents.value || pendingBySub.value || byProject.value || documentRequests.value)
);

const reports = [
  { key: 'approved', label: 'Допущенный персонал', exportFn: (f: 'csv' | 'pdf') => reportsApi.exportApprovedPersonnel(f) },
  { key: 'visits', label: 'Журнал посещений', exportFn: (f: 'csv' | 'pdf') => reportsApi.exportSiteVisits(f) },
  { key: 'blocks', label: 'Блокировки', exportFn: (f: 'csv' | 'pdf') => reportsApi.exportBlocks(f) },
  { key: 'requests', label: 'Заявки', exportFn: (f: 'csv' | 'pdf') => reportsApi.exportDocumentRequests(f) },
  { key: 'expiring', label: 'Истекающие документы', exportFn: (f: 'csv' | 'pdf') => reportsApi.exportExpiringDocuments(f, 14) }
];

const columns = [
  { title: 'Субподрядчик', key: 'subcontractorName' },
  { title: 'Всего', key: 'totalEmployees', width: 80 },
  { title: 'Допущено', key: 'approvedEmployees', width: 100 },
  { title: 'Заблок.', key: 'blockedEmployees', width: 90 },
  { title: 'Квиз', key: 'quizCompleted', width: 80 },
  { title: 'Истекает док.', key: 'expiringDocuments', width: 110 }
];

async function loadDashboard() {
  if (auth.isTansu) {
    const inbox = auth.canApproveEmployees
      ? await approvalsApi.inbox().catch(() => [])
      : [];
    await loadTansuReportsCharts({
      personnelStatus,
      compliance: complianceChart,
      siteVisits,
      incidents,
      pendingBySub,
      byProject,
      documentRequests
    }, {
      canViewEmployees: auth.canViewEmployees,
      canViewReports: auth.permissions.canViewReports,
      canViewVisitJournal: auth.canViewVisitJournal,
      canViewIncidents: auth.canBlockEmployee,
      canApproveEmployees: auth.canApproveEmployees
    }, inbox);
  }

  if (auth.isSubcontractor) {
    const employees = await employeesApi.list().catch(() => []);
    await loadSubcontractorReportsCharts({
      personnelStatus,
      compliance: complianceChart,
      siteVisits,
      incidents,
      pendingBySub,
      byProject,
      documentRequests
    }, employees, auth.canViewVisitJournal);
    await loadSubcontractorDocumentChart({
      personnelStatus,
      compliance: complianceChart,
      siteVisits,
      incidents,
      pendingBySub,
      byProject,
      documentRequests
    });
  }
}

async function loadCompliance() {
  if (!auth.isTansu || !auth.permissions.canViewReports) return;
  try {
    compliance.value = await reportsApi.compliance();
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function exportReport(fn: (f: 'csv' | 'pdf') => Promise<void>, format: 'csv' | 'pdf') {
  try {
    await fn(format);
    msg.success('Файл скачан');
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

onMounted(async () => {
  loading.value = true;
  try {
    await Promise.all([loadDashboard(), loadCompliance()]);
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <NSpin :show="loading">
    <NSpace vertical :size="20">
      <div>
        <h2 class="t-section-title" style="margin:0 0 4px">Отчёты и аналитика</h2>
        <p style="margin:0;color:var(--brand-text-muted);font-size:13px">
          Сводка, экспорт и таблицы
        </p>
      </div>

      <div v-if="!loading && hasCharts" class="t-dashboard-grid">
        <div v-if="personnelStatus" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Статус персонала</h3>
          <p class="t-dashboard-card__subtitle">Распределение по этапам согласования</p>
          <DashboardChart :option="personnelStatus" />
        </div>

        <div v-if="auth.isTansu && complianceChart" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Допуск и блокировки</h3>
          <p class="t-dashboard-card__subtitle">По субподрядчикам</p>
          <DashboardChart :option="complianceChart" />
        </div>

        <div v-if="siteVisits" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Проходы на объект</h3>
          <p class="t-dashboard-card__subtitle">За последние 14 дней</p>
          <DashboardChart :option="siteVisits" />
        </div>

        <div v-if="incidents" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Открытые инциденты</h3>
          <p class="t-dashboard-card__subtitle">По уровню серьёзности</p>
          <DashboardChart :option="incidents" />
        </div>

        <div v-if="pendingBySub" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Очередь согласований</h3>
          <p class="t-dashboard-card__subtitle">Ожидают вашего решения</p>
          <DashboardChart :option="pendingBySub" />
        </div>

        <div v-if="byProject" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Сотрудники по объектам</h3>
          <p class="t-dashboard-card__subtitle">Текущее распределение</p>
          <DashboardChart :option="byProject" />
        </div>

        <div v-if="documentRequests" class="t-dashboard-card">
          <h3 class="t-dashboard-card__title">Заявки</h3>
          <p class="t-dashboard-card__subtitle">Статусы документных заявок</p>
          <DashboardChart :option="documentRequests" />
        </div>
      </div>

      <NEmpty
        v-if="!loading && !hasCharts"
        description="Нет данных для отображения."
      />

      <div v-if="!loading">
        <h3 class="t-section-title" style="margin-bottom:12px">Экспорт отчётов</h3>
        <NGrid :cols="2" :x-gap="16" :y-gap="16">
          <NGi v-for="r in reports" :key="r.key">
            <NCard :title="r.label" size="small">
              <NSpace>
                <NButton @click="exportReport(r.exportFn, 'csv')">CSV</NButton>
                <NButton @click="exportReport(r.exportFn, 'pdf')">PDF</NButton>
              </NSpace>
            </NCard>
          </NGi>
        </NGrid>
      </div>

      <NCard v-if="!loading && auth.isTansu && auth.permissions.canViewReports" title="Сводка по субподрядчикам">
        <NDataTable :columns="columns" :data="compliance" :bordered="false" />
      </NCard>
    </NSpace>
  </NSpin>
</template>
