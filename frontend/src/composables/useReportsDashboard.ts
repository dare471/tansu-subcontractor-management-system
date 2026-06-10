import { ref, type Ref } from 'vue';
import { employeesApi, type Employee } from '@/api/employees';
import { reportsApi } from '@/api/reports';
import { siteVisitJournalApi } from '@/api/siteVisitJournal';
import { incidentsApi } from '@/api/incidents';
import { documentRequestsApi } from '@/api/documentRequests';
import type { InboxItem } from '@/api/approvals';
import {
  buildComplianceBar,
  buildHorizontalBar,
  buildSeverityPie,
  buildStatusDonut,
  buildVisitsLine,
  COLORS,
  countByKey,
  groupVisitsByDay,
  lastNDaysLabels,
  type ChartOption,
  type StatusSlice
} from '@/utils/dashboardCharts';

export type ReportsDashboardCharts = {
  personnelStatus: Ref<ChartOption | null>;
  compliance: Ref<ChartOption | null>;
  siteVisits: Ref<ChartOption | null>;
  incidents: Ref<ChartOption | null>;
  pendingBySub: Ref<ChartOption | null>;
  byProject: Ref<ChartOption | null>;
  documentRequests: Ref<ChartOption | null>;
};

function hasChartData(slices: StatusSlice[]): boolean {
  return slices.some((s) => s.value > 0);
}

function employeeStatusSlices(employees: Employee[]): StatusSlice[] {
  let approved = 0;
  let pending = 0;
  let rejected = 0;
  let draft = 0;
  for (const e of employees) {
    const s = e.currentStatus;
    if (s === 'approved') approved++;
    else if (s === 'pending') pending++;
    else if (s === 'rejected') rejected++;
    else draft++;
  }
  return [
    { name: 'Согласован', value: approved, color: COLORS.approved },
    { name: 'На согласовании', value: pending, color: COLORS.pending },
    { name: 'Отклонён', value: rejected, color: COLORS.rejected },
    { name: 'Черновик', value: draft, color: COLORS.draft }
  ];
}

export function useReportsDashboard(): ReportsDashboardCharts {
  const personnelStatus = ref<ChartOption | null>(null);
  const compliance = ref<ChartOption | null>(null);
  const siteVisits = ref<ChartOption | null>(null);
  const incidents = ref<ChartOption | null>(null);
  const pendingBySub = ref<ChartOption | null>(null);
  const byProject = ref<ChartOption | null>(null);
  const documentRequests = ref<ChartOption | null>(null);

  return {
    personnelStatus,
    compliance,
    siteVisits,
    incidents,
    pendingBySub,
    byProject,
    documentRequests
  };
}

export async function loadTansuReportsCharts(
  charts: ReportsDashboardCharts,
  flags: {
    canViewEmployees: boolean;
    canViewReports: boolean;
    canViewVisitJournal: boolean;
    canViewIncidents: boolean;
    canApproveEmployees: boolean;
  },
  inbox: InboxItem[]
) {
  const tasks: Promise<void>[] = [];

  if (flags.canViewEmployees) {
    tasks.push(
      employeesApi.list().then((employees) => {
        const slices = employeeStatusSlices(employees);
        if (hasChartData(slices)) {
          charts.personnelStatus.value = buildStatusDonut('Персонал', slices);
        }
      }).catch(() => {})
    );
  }

  if (flags.canViewReports) {
    tasks.push(
      reportsApi.compliance().then((rows) => {
        if (!rows.length) return;
        const labels = rows.map((r) => r.subcontractorName);
        charts.compliance.value = buildComplianceBar(
          labels,
          rows.map((r) => r.approvedEmployees),
          rows.map((r) => r.blockedEmployees)
        );
      }).catch(() => {})
    );
  }

  if (flags.canViewVisitJournal) {
    const from = new Date();
    from.setDate(from.getDate() - 13);
    tasks.push(
      siteVisitJournalApi.list({ page: 1, pageSize: 500, from: from.toISOString() }).then((page) => {
        const dates = page.items.map((v) => v.checkedInAt);
        if (dates.length > 0) {
          charts.siteVisits.value = buildVisitsLine(lastNDaysLabels(14), groupVisitsByDay(dates, 14));
        }
      }).catch(() => {})
    );
  }

  if (flags.canViewIncidents) {
    tasks.push(
      incidentsApi.list().then((items) => {
        const open = items.filter((i) => i.status !== 'resolved');
        if (!open.length) return;
        const bySev = countByKey(open, (i) => i.severity);
        const labels: Record<string, { name: string; color: string }> = {
          low: { name: 'Низкая', color: COLORS.low },
          medium: { name: 'Средняя', color: COLORS.medium },
          high: { name: 'Высокая', color: COLORS.high },
          critical: { name: 'Критическая', color: COLORS.critical }
        };
        charts.incidents.value = buildSeverityPie(
          Object.entries(bySev).map(([k, v]) => ({
            name: labels[k]?.name ?? k,
            value: v,
            color: labels[k]?.color ?? COLORS.medium
          }))
        );
      }).catch(() => {})
    );
  }

  if (flags.canApproveEmployees && inbox.length) {
    const bySub = countByKey(inbox, (i) => i.subcontractorName);
    charts.pendingBySub.value = buildHorizontalBar(
      'Ожидают решения',
      Object.keys(bySub),
      Object.values(bySub)
    );
  }

  await Promise.all(tasks);
}

export async function loadSubcontractorReportsCharts(
  charts: ReportsDashboardCharts,
  employees: Employee[],
  canViewVisitJournal: boolean
) {
  const slices = employeeStatusSlices(employees);
  if (hasChartData(slices)) {
    charts.personnelStatus.value = buildStatusDonut('Сотрудники', slices);
  }

  if (employees.length) {
    const byProject = countByKey(employees, (e) => e.projectName ?? e.projectOid);
    const labels = Object.keys(byProject);
    charts.byProject.value = buildHorizontalBar('По объектам', labels, labels.map((l) => byProject[l] ?? 0));
  }

  if (canViewVisitJournal) {
    const from = new Date();
    from.setDate(from.getDate() - 13);
    try {
      const page = await siteVisitJournalApi.list({ page: 1, pageSize: 500, from: from.toISOString() });
      const dates = page.items.map((v) => v.checkedInAt);
      if (dates.length > 0) {
        charts.siteVisits.value = buildVisitsLine(lastNDaysLabels(14), groupVisitsByDay(dates, 14));
      }
    } catch {
      /* ignore */
    }
  }
}

export async function loadSubcontractorDocumentChart(charts: ReportsDashboardCharts) {
  try {
    const requests = await documentRequestsApi.list();
    const byStatus = countByKey(requests, (r) => {
      const s = r.currentStatus;
      if (s === 'approved') return 'Согласована';
      if (s === 'pending') return 'На согласовании';
      if (s === 'rejected') return 'Отклонена';
      return 'Черновик';
    });
    if (!hasChartData([
      { name: '', value: byStatus['Согласована'] ?? 0, color: '' },
      { name: '', value: byStatus['На согласовании'] ?? 0, color: '' },
      { name: '', value: byStatus['Отклонена'] ?? 0, color: '' },
      { name: '', value: byStatus['Черновик'] ?? 0, color: '' }
    ])) return;

    charts.documentRequests.value = buildStatusDonut('Заявки', [
      { name: 'Согласована', value: byStatus['Согласована'] ?? 0, color: COLORS.approved },
      { name: 'На согласовании', value: byStatus['На согласовании'] ?? 0, color: COLORS.pending },
      { name: 'Отклонена', value: byStatus['Отклонена'] ?? 0, color: COLORS.rejected },
      { name: 'Черновик', value: byStatus['Черновик'] ?? 0, color: COLORS.draft }
    ]);
  } catch {
    /* ignore */
  }
}
