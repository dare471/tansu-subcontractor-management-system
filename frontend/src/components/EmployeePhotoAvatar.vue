<script setup lang="ts">
import { ref, watch } from 'vue';
import { NAvatar } from 'naive-ui';
import { employeesApi } from '@/api/employees';

const props = withDefaults(defineProps<{
  employeeId: string;
  fullName: string;
  photoPath?: string | null;
  size?: number;
}>(), {
  photoPath: null,
  size: 40
});

const src = ref<string | null>(null);

async function load() {
  if (!props.photoPath) {
    src.value = null;
    return;
  }

  src.value = await employeesApi.fetchPhotoObjectUrl(props.employeeId, props.photoPath);
}

watch(() => [props.employeeId, props.photoPath] as const, load, { immediate: true });
</script>

<template>
  <NAvatar
    v-if="src"
    :round="false"
    :src="src"
    :size="size"
  />
  <NAvatar v-else :round="false" :size="size">
    {{ fullName?.[0] ?? '?' }}
  </NAvatar>
</template>
