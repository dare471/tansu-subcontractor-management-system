<script setup lang="ts">
import { approvalStatusLabel, approvalStatusType } from '@/api/employeePortal';
import { NTag } from 'naive-ui';

defineProps<{ status: string | null | undefined }>();
</script>

<template>
  <NTag :type="approvalStatusType(status)" size="medium" round>
    {{ approvalStatusLabel(status) }}
  </NTag>
</template>
