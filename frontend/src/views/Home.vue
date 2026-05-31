<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import { useRouter } from 'vue-router';
import { NIcon, NSpace, NTag, NEmpty, NSpin } from 'naive-ui';
import {
  PeopleOutline, IdCardOutline, MailUnreadOutline, BusinessOutline,
  CheckmarkDoneOutline, AlertCircleOutline, AddCircleOutline,
  DocumentTextOutline, GitNetworkOutline, PersonCircleOutline
} from '@vicons/ionicons5';
import { useAuthStore } from '@/stores/auth';
import { subcontractorsApi } from '@/api/subcontractors';
import { employeesApi } from '@/api/employees';
import { projectsApi } from '@/api/projects';
import { approvalsApi, type InboxItem } from '@/api/approvals';
import { usersApi } from '@/api/users';

const auth = useAuthStore();
const router = useRouter();
const loading = ref(true);

const subsCount = ref(0);
const projectsCount = ref(0);
const usersCount = ref(0);
const employeesTotal = ref(0);
const employeesApproved = ref(0);
const employeesPending = ref(0);
const employeesRejected = ref(0);
const inbox = ref<InboxItem[]>([]);

const greeting = computed(() => {
  const h = new Date().getHours();
  if (h < 6) return 'Доброй ночи';
  if (h < 12) return 'Доброе утро';
  if (h < 18) return 'Добрый день';
  return 'Добрый вечер';
});

const firstName = computed(() => {
  const n = auth.user?.fullName ?? '';
  return n.split(/\s+/)[1] ?? n.split(/\s+/)[0] ?? '';
});

async function loadTansu() {
  const [subs, projects, users] = await Promise.all([
    subcontractorsApi.list().catch(() => []),
    projectsApi.list().catch(() => []),
    usersApi.list().catch(() => [])
  ]);
  subsCount.value = subs.length;
  projectsCount.value = projects.length;
  usersCount.value = users.length;
}

async function loadSubcontractor() {
  const employees = await employeesApi.list().catch(() => []);
  employeesTotal.value = employees.length;
  employeesApproved.value = employees.filter((e) => e.currentStatus === 'approved').length;
  employeesPending.value = employees.filter((e) => e.currentStatus === 'pending').length;
  employeesRejected.value = employees.filter((e) => e.currentStatus === 'rejected').length;
}

async function loadInbox() {
  inbox.value = await approvalsApi.inbox().catch(() => []);
}

onMounted(async () => {
  loading.value = true;
  try {
    if (auth.isTansu) {
      await loadTansu();
      await loadInbox();
    }
    if (auth.isSubcontractor) await loadSubcontractor();
  } finally { loading.value = false; }
});

const tansuQuickActions = [
  { label: 'Новый субподрядчик', icon: PeopleOutline, to: 'subcontractors' },
  { label: 'Новый пользователь', icon: PersonCircleOutline, to: 'users' },
  { label: 'Матрица согласования', icon: GitNetworkOutline, to: 'matrix' },
  { label: 'Зарегистрировать проект', icon: BusinessOutline, to: 'projects' }
];

const subQuickActions = [
  { label: 'Добавить сотрудника', icon: AddCircleOutline, to: 'employees' },
  { label: 'Пакеты согласования', icon: IdCardOutline, to: 'employee-batches' },
  { label: 'Отчётность по проектам', icon: BusinessOutline, to: 'project-progress' },
  { label: 'Новая заявка', icon: DocumentTextOutline, to: 'document-requests' }
];

const quickActions = computed(() => auth.isTansu ? tansuQuickActions : subQuickActions);

function statusTag(status: string | null) {
  if (!status) return h(NTag, {}, () => 'Черновик');
  const map: Record<string, { type: any; label: string }> = {
    approved: { type: 'success', label: 'Согласован' },
    rejected: { type: 'error', label: 'Отклонён' },
    pending: { type: 'warning', label: 'На согласовании' },
    skipped: { type: 'default', label: 'Пропущен' }
  };
  const m = map[status] ?? { type: 'default' as const, label: status };
  return h(NTag, { type: m.type }, () => m.label);
}
</script>

<template>
  <NSpin :show="loading">
    <NSpace vertical :size="20">
      <div class="t-greeting">
        <div class="t-greeting__icon">
          <NIcon :component="PersonCircleOutline" size="28" />
        </div>
        <div style="flex:1">
          <div class="t-greeting__title">{{ greeting }}, {{ firstName || auth.user?.fullName }}!</div>
        </div>
        <NTag :bordered="false" type="warning" size="large">
          {{ auth.isTansu ? 'Сотрудник ТАНСУ' : 'Субподрядчик' }}
        </NTag>
      </div>

      <div style="display:grid;grid-template-columns:repeat(auto-fit, minmax(220px, 1fr));gap:16px">
        <template v-if="auth.isTansu">
          <div class="t-stat t-stat--orange">
            <div class="t-stat__icon"><NIcon :component="PeopleOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">Субподрядчиков</div>
              <div class="t-stat__value">{{ subsCount }}</div>
            </div>
          </div>
          <div class="t-stat t-stat--blue">
            <div class="t-stat__icon"><NIcon :component="BusinessOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">Проектов</div>
              <div class="t-stat__value">{{ projectsCount }}</div>
            </div>
          </div>
          <div class="t-stat t-stat--purple">
            <div class="t-stat__icon"><NIcon :component="PersonCircleOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">Пользователей</div>
              <div class="t-stat__value">{{ usersCount }}</div>
            </div>
          </div>
        </template>

        <template v-if="auth.isSubcontractor">
          <div class="t-stat t-stat--orange">
            <div class="t-stat__icon"><NIcon :component="IdCardOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">Сотрудников</div>
              <div class="t-stat__value">{{ employeesTotal }}</div>
            </div>
          </div>
          <div class="t-stat t-stat--green">
            <div class="t-stat__icon"><NIcon :component="CheckmarkDoneOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">Согласовано</div>
              <div class="t-stat__value">{{ employeesApproved }}</div>
            </div>
          </div>
          <div class="t-stat t-stat--blue">
            <div class="t-stat__icon"><NIcon :component="MailUnreadOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">На согласовании</div>
              <div class="t-stat__value">{{ employeesPending }}</div>
            </div>
          </div>
          <div class="t-stat t-stat--red">
            <div class="t-stat__icon"><NIcon :component="AlertCircleOutline" size="24" /></div>
            <div>
              <div class="t-stat__title">Отклонено</div>
              <div class="t-stat__value">{{ employeesRejected }}</div>
            </div>
          </div>
        </template>

        <div v-if="auth.isTansu" class="t-stat t-stat--slate">
          <div class="t-stat__icon"><NIcon :component="MailUnreadOutline" size="24" /></div>
          <div>
            <div class="t-stat__title">Ждут вашего решения</div>
            <div class="t-stat__value">{{ inbox.length }}</div>
          </div>
        </div>
      </div>

      <div
        :style="{
          display: 'grid',
          gridTemplateColumns: auth.isTansu ? '1fr 360px' : '1fr',
          gap: '20px',
          alignItems: 'flex-start'
        }"
      >
        <div v-if="auth.isTansu" class="t-card">
          <h3 class="t-section-title">Входящие согласования</h3>
          <NEmpty v-if="!inbox.length" description="Нет записей, ожидающих решения" />
          <div v-else style="display:flex;flex-direction:column;gap:10px">
            <div
              v-for="item in inbox.slice(0, 5)"
              :key="item.sheetId"
              style="display:flex;align-items:center;gap:14px;padding:12px;border:1px solid var(--brand-border);border-radius:10px;cursor:pointer"
              @click="router.push({ name: 'approvals-inbox' })"
            >
              <div style="width:40px;height:40px;border-radius:10px;background:var(--brand-orange-soft);display:flex;align-items:center;justify-content:center;color:var(--brand-orange);font-weight:700">
                {{ item.employeeFullName.split(' ').slice(0, 2).map((s) => s[0]).join('') }}
              </div>
              <div style="flex:1;min-width:0">
                <div style="font-weight:600">{{ item.employeeFullName }}</div>
                <div style="font-size:12px;color:var(--brand-text-muted)">
                  {{ item.subcontractorName }} · {{ item.projectName || item.projectOid }}
                </div>
              </div>
              <NTag type="warning" :bordered="false">Шаг {{ item.orderNo }}</NTag>
            </div>
          </div>
        </div>

        <div class="t-card">
          <h3 class="t-section-title">Быстрые действия</h3>
          <div class="t-quick-grid">
            <div
              v-for="qa in quickActions"
              :key="qa.label"
              class="t-quick"
              @click="router.push({ name: qa.to })"
            >
              <div class="t-quick__icon">
                <NIcon :component="qa.icon" size="20" />
              </div>
              <span>{{ qa.label }}</span>
            </div>
          </div>
        </div>
      </div>
    </NSpace>
  </NSpin>
</template>
