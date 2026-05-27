<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { NRadioGroup, NRadio, NButton, NAlert, useMessage } from 'naive-ui';
import PageHeader from '@/components/PageHeader.vue';
import { employeePortalApi, type SafetyQuizQuestion } from '@/api/employeePortal';
import { toApiError } from '@/api/client';

const router = useRouter();
const msg = useMessage();
const questions = ref<SafetyQuizQuestion[]>([]);
const answers = ref<Record<string, string>>({});
const submitting = ref(false);
const result = ref<string | null>(null);
const loading = ref(true);

onMounted(async () => {
  try {
    questions.value = await employeePortalApi.quiz();
    for (const q of questions.value) answers.value[q.id] = '';
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
});

async function submit() {
  submitting.value = true;
  result.value = null;
  try {
    const res = await employeePortalApi.submitQuiz(answers.value);
    result.value = res.message;
    if (res.passed) {
      msg.success(res.message);
      setTimeout(() => router.push({ name: 'dashboard' }), 800);
    } else {
      msg.warning(res.message);
    }
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <PageHeader title="Опрос по ТБ" subtitle="Ответьте на все вопросы правильно" />

  <section class="portal-card">
    <NAlert type="info" :show-icon="false" style="margin-bottom:16px">
      После прохождения откроется QR-пропуск на объект.
    </NAlert>

    <div v-for="(q, idx) in questions" :key="q.id" style="margin-bottom:20px">
      <p style="margin:0 0 10px;font-weight:600">{{ idx + 1 }}. {{ q.text }}</p>
      <NRadioGroup v-model:value="answers[q.id]">
        <div style="display:flex;flex-direction:column;gap:8px">
          <NRadio v-for="opt in q.options" :key="opt.id" :value="opt.id" style="align-items:flex-start">
            {{ opt.text }}
          </NRadio>
        </div>
      </NRadioGroup>
    </div>

    <NAlert
      v-if="result"
      :type="result.includes('пройден') ? 'success' : 'warning'"
      :show-icon="false"
      style="margin-bottom:16px"
    >
      {{ result }}
    </NAlert>

    <div style="display:flex;flex-direction:column;gap:10px">
      <NButton block @click="router.back()" style="min-height:var(--tap-min)">Назад</NButton>
      <NButton type="primary" block :loading="submitting" style="min-height:var(--tap-min)" @click="submit">
        Отправить ответы
      </NButton>
    </div>
  </section>
</template>
