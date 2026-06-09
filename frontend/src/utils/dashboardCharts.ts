export type ChartOption = Record<string, unknown>;

const COLORS = {
  approved: '#14a37f',
  pending: '#f59e0b',
  rejected: '#e11d48',
  draft: '#94a3b8',
  blocked: '#e11d48',
  visits: '#3b82f6',
  low: '#60a5fa',
  medium: '#f59e0b',
  high: '#f97316',
  critical: '#e11d48'
};

export type StatusSlice = { name: string; value: number; color: string };

export function buildStatusDonut(_title: string, slices: StatusSlice[]): ChartOption {
  const data = slices.filter((s) => s.value > 0);
  return {
    color: data.map((s) => s.color),
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { bottom: 0, type: 'scroll' },
    series: [
      {
        type: 'pie',
        radius: ['42%', '68%'],
        center: ['50%', '45%'],
        avoidLabelOverlap: true,
        itemStyle: { borderRadius: 6, borderColor: '#fff', borderWidth: 2 },
        label: { show: data.length <= 6, formatter: '{b}\n{c}' },
        data: data.length
          ? data.map((s) => ({ name: s.name, value: s.value }))
          : [{ name: 'Нет данных', value: 0 }]
      }
    ]
  };
}

export function buildComplianceBar(
  labels: string[],
  approved: number[],
  blocked: number[]
): ChartOption {
  return {
    color: [COLORS.approved, COLORS.blocked],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    legend: { bottom: 0 },
    grid: { left: 8, right: 16, top: 24, bottom: 48, containLabel: true },
    xAxis: { type: 'category', data: labels, axisLabel: { interval: 0, rotate: labels.length > 4 ? 24 : 0, fontSize: 11 } },
    yAxis: { type: 'value', minInterval: 1 },
    series: [
      { name: 'Допущено', type: 'bar', stack: 'total', barMaxWidth: 36, data: approved },
      { name: 'Заблокировано', type: 'bar', stack: 'total', barMaxWidth: 36, data: blocked }
    ]
  };
}

export function buildVisitsLine(labels: string[], counts: number[]): ChartOption {
  return {
    color: [COLORS.visits],
    tooltip: { trigger: 'axis' },
    grid: { left: 8, right: 16, top: 24, bottom: 32, containLabel: true },
    xAxis: { type: 'category', data: labels, boundaryGap: false },
    yAxis: { type: 'value', minInterval: 1 },
    series: [
      {
        name: 'Проходы',
        type: 'line',
        smooth: true,
        areaStyle: { opacity: 0.12 },
        data: counts
      }
    ]
  };
}

export function buildHorizontalBar(title: string, labels: string[], values: number[]): ChartOption {
  const pairs = labels
    .map((label, i) => ({ label, value: values[i] ?? 0 }))
    .filter((p) => p.value > 0)
    .sort((a, b) => b.value - a.value)
    .slice(0, 8);

  return {
    color: ['#ee6c1c'],
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 8, right: 24, top: 16, bottom: 16, containLabel: true },
    xAxis: { type: 'value', minInterval: 1 },
    yAxis: {
      type: 'category',
      data: pairs.map((p) => p.label),
      axisLabel: { width: 120, overflow: 'truncate' }
    },
    series: [{ name: title, type: 'bar', barMaxWidth: 20, data: pairs.map((p) => p.value) }]
  };
}

export function buildSeverityPie(slices: { name: string; value: number; color: string }[]): ChartOption {
  const data = slices.filter((s) => s.value > 0);
  return {
    color: data.map((s) => s.color),
    tooltip: { trigger: 'item' },
    legend: { bottom: 0 },
    series: [
      {
        type: 'pie',
        radius: '62%',
        center: ['50%', '44%'],
        data: data.map((s) => ({ name: s.name, value: s.value })),
        label: { formatter: '{b}: {c}' }
      }
    ]
  };
}

export function countByKey<T>(items: T[], keyFn: (item: T) => string): Record<string, number> {
  const map: Record<string, number> = {};
  for (const item of items) {
    const k = keyFn(item) || '—';
    map[k] = (map[k] ?? 0) + 1;
  }
  return map;
}

export function lastNDaysLabels(days: number): string[] {
  const labels: string[] = [];
  for (let i = days - 1; i >= 0; i--) {
    const d = new Date();
    d.setDate(d.getDate() - i);
    labels.push(d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' }));
  }
  return labels;
}

export function groupVisitsByDay(dates: string[], days: number): number[] {
  const labels = lastNDaysLabels(days);
  const keys = new Set(labels);
  const counts = Object.fromEntries(labels.map((l) => [l, 0]));
  for (const iso of dates) {
    const label = new Date(iso).toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
    if (keys.has(label)) counts[label]++;
  }
  return labels.map((l) => counts[l] ?? 0);
}

export { COLORS };
