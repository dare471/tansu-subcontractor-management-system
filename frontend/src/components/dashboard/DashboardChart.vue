<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, nextTick } from 'vue';
import * as echarts from 'echarts/core';
import { BarChart, LineChart, PieChart } from 'echarts/charts';
import {
  GridComponent,
  LegendComponent,
  TitleComponent,
  TooltipComponent
} from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';

echarts.use([
  BarChart,
  LineChart,
  PieChart,
  GridComponent,
  LegendComponent,
  TitleComponent,
  TooltipComponent,
  CanvasRenderer
]);

const props = defineProps<{
  option: Record<string, unknown> | null;
  height?: number;
}>();

const el = ref<HTMLElement | null>(null);
let chart: echarts.ECharts | null = null;
let resizeObserver: ResizeObserver | null = null;

async function render() {
  if (!el.value || !props.option) return;
  await nextTick();
  if (!chart) chart = echarts.init(el.value);
  chart.setOption(props.option, { notMerge: true });
  chart.resize();
}

function onResize() {
  chart?.resize();
}

onMounted(async () => {
  await nextTick();
  render();
  window.addEventListener('resize', onResize);
  if (el.value && typeof ResizeObserver !== 'undefined') {
    resizeObserver = new ResizeObserver(() => chart?.resize());
    resizeObserver.observe(el.value);
  }
  setTimeout(() => chart?.resize(), 150);
});

onUnmounted(() => {
  window.removeEventListener('resize', onResize);
  resizeObserver?.disconnect();
  resizeObserver = null;
  chart?.dispose();
  chart = null;
});

watch(() => props.option, render, { deep: true });
</script>

<template>
  <div
    ref="el"
    class="t-dashboard-chart"
    :style="{ height: `${height ?? 280}px` }"
  />
</template>
