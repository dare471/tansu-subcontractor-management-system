<script setup lang="ts">
import { computed } from 'vue';
import { NDrawer, NDrawerContent } from 'naive-ui';

export type AppDrawerWidth = 'narrow' | 'medium' | 'wide' | 'xl' | 'full';

const props = withDefaults(
  defineProps<{
    show: boolean;
    title?: string;
    width?: AppDrawerWidth | number;
    closable?: boolean;
    nativeScrollbar?: boolean;
  }>(),
  {
    title: '',
    width: 'medium',
    closable: true,
    nativeScrollbar: false
  }
);

const emit = defineEmits<{
  'update:show': [value: boolean];
  afterLeave: [];
}>();

const drawerWidth = computed(() => {
  if (typeof props.width === 'number') return props.width;
  const presets: Record<AppDrawerWidth, number | string> = {
    narrow: 528,
    medium: 686,
    wide: 898,
    xl: 1109,
    full: 'min(1452px, calc(100vw - 48px))'
  };
  return presets[props.width];
});

function onUpdateShow(value: boolean) {
  emit('update:show', value);
}
</script>

<template>
  <NDrawer
    :show="show"
    :width="drawerWidth"
    placement="right"
    :auto-focus="false"
    :trap-focus="false"
    :block-scroll="true"
    class="app-drawer"
    @update:show="onUpdateShow"
    @after-leave="emit('afterLeave')"
  >
    <NDrawerContent
      :title="title"
      :closable="closable"
      :native-scrollbar="nativeScrollbar"
      class="app-drawer-content"
    >
      <div class="app-drawer-body">
        <slot />
      </div>
      <template v-if="$slots.footer" #footer>
        <slot name="footer" />
      </template>
    </NDrawerContent>
  </NDrawer>
</template>
