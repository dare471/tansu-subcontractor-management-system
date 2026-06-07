# Tansu

Система учёта субподрядчиков, согласования персонала и контроля допуска на объект для ТАНСУ.

## Стек

PostgreSQL 16, .NET 10, Vue 3, RabbitMQ, Naive UI, DeepFace (Python).

## Структура

| Каталог | Назначение |
| --- | --- |
| `backend/` | API, worker, verify-api, доменная логика |
| `frontend/` | Веб-приложение ТАНСУ и субподрядчиков |
| `employee-portal-web/` | Личный кабинет сотрудника на объекте |
| `verify-web/` | Проверка QR и Face ID на проходной |
| `face-verify/` | Сервис сравнения лиц (DeepFace) |

## Запуск

```bash
cp .env.example .env
docker compose up --build
```

При первом старте в среде `Development` загружаются начальные данные: проекты, субподрядчики, сотрудники, матрицы согласования, фото, пропуска.

| Сервис | Адрес |
| --- | --- |
| Веб (ТАНСУ / субподрядчик) | http://localhost:5173 |
| API | http://localhost:${API_PORT:-8080} |
| Личный кабинет сотрудника | http://localhost:5175 |
| Verify Web (QR + Face ID) | https://localhost:5174 |
| Verify API | http://localhost:8091 |
| Face Verify | http://localhost:8092 |
| MailHog | http://localhost:8025 |
| RabbitMQ Management | http://localhost:15672 |

Каталог `frontend/` смонтирован в контейнер `web` — изменения UI подхватываются без пересборки.

## Типы пользователей

| Тип | Кто | Связь | Где вход |
| --- | --- | --- | --- |
| `TANSU` | Сотрудники ТАНСУ | Роль + при необходимости проекты и субподрядчики (область видимости) | http://localhost:5173 |
| `Subcontractor` | Администратор организации (HR, руководитель) | Одна организация (`subcontractor_id`, 1:1) | http://localhost:5173 |
| `Employee` | Рабочий на объекте | Запись сотрудника (`employee_id`, 1:1) | http://localhost:5175 |

Учётные записи `Employee` создаются автоматически после полного согласования сотрудника. Email вида `{employeeId}@portal.tansu.local`, пароль — одноразовый.

## Роли ТАНСУ

Права задаются в `TansuRoleMatrix` (бэкенд) и приходят в `/api/auth/me` → `permissions`. Меню и маршруты веб-приложения фильтруются по этим флагам.

| Роль | Меню (разделы) | Регистрация СП | Матрица | Согласование | Блокировка | Журнал | Пользователи | Проекты | Только чтение |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `global_admin` | Все | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ редакт. | — |
| `oid_manager` | Главная, СП, Проекты, Сотрудники СП, Матрица | ✓ | ✓ | — | — | — | — | ✓ редакт. | — |
| `oid_director` | Главная, СП, Проекты, Сотрудники СП, Согласование, Заявки | — | — | ✓ | ✓ | — | — | просмотр | — |
| `sb_project` / `safety_project` | Главная, СП, Проекты, Сотрудники СП, Проверка фото | — | — | — | ✓ | — | — | просмотр | — |
| `sb_chief` / `safety_chief` | + Журнал посещений | — | — | — | ✓ | ✓ | — | просмотр | — |
| `project_manager` | Главная, СП, Проекты, Сотрудники СП, Журнал | — | — | — | — | ✓ | — | просмотр | **да** |

**Область видимости** задаётся ролью и может быть дополнительно сужена явными привязками к проектам и субподрядчикам в карточке пользователя (`oid_manager` — только свои СП; `oid_director` — СП подчинённых; роли на проекте — только назначенные проекты и работающие на них СП; начальники СБ/БиОТ — все активные СП).

## Начальные данные

**Проекты:** ЖК «Keremet» (Астана), БЦ «Abay Tower» (Алматы).

**Субподрядчики:**

| Организация | БИН | Админ |
| --- | --- | --- |
| ТОО «MontazhKomplekt Astana» | 080540012345 | hr@montazh-astana.kz |
| ТОО «Qazaq EnergoStroy» | 060701098765 | energo@qazaq-energo.kz |

У Montazh — набор сотрудников в разных статусах согласования (черновики, на согласовании, согласованные). У согласованных есть фото, QR-пропуск и личный кабинет. В очереди проверки фото — несколько ожидающих записей; в журнале посещений — проходы на КПП Keremet и Abay Tower.

## Учётные записи (локальная среда)

| Роль | Email | Пароль |
| --- | --- | --- |
| Глобальный администратор ТАНСУ | admin@tansu.local | см. dev-login ниже |
| MontazhKomplekt Astana | hr@montazh-astana.kz | Montazh2024! |
| Qazaq EnergoStroy | energo@qazaq-energo.kz | Montazh2024! |

Согласующие ТАНСУ: `approver1@tansu.local`, `approver2@tansu.local`, `approver3@tansu.local`, `accounting@tansu.local`.

Локальный вход сотрудников ТАНСУ (JWT):

```bash
curl -s -X POST "http://localhost:${API_PORT:-8080}/api/auth/dev-login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tansu.local"}'
```

Production — Microsoft Entra ID (`ENTRA_TENANT_ID`, `ENTRA_AUDIENCE`). Справочник сотрудников из ЗУП: `ZUP_CLIENT_ID`, `ZUP_CLIENT_SECRET`, `ZUP_TENANT_ID` (или тот же tenant), `ZUP_AUDIENCE` / `ENTRA_AUDIENCE` для scope `/.default` — см. `.env.example`.

## Согласование персонала

1. Субподрядчик создаёт **пакет** (черновик) и добавляет сотрудников.
2. Отправляет пакет на согласование — сотрудники проходят цепочку по матрице проекта.
3. После полного согласования: выдаётся QR-пропуск, создаётся личный кабинет (`Employee`).

Разделы в веб-приложении: **Пакеты**, **Сотрудники**, **Inbox согласования** (ТАНСУ).

## Личный кабинет сотрудника

1. Вход: http://localhost:5175 — ИИН + одноразовый пароль.
2. В dev пароль пишется в `data/employee-portal-credentials.log` (в контейнере API: `/app/data/employee-portal-credentials.log`).
3. Смена пароля при первом входе, опрос по ТБ, затем QR-пропуск.

## QR-пропуск и Face ID

| Компонент | Назначение |
| --- | --- |
| `Tansu.Api` | Пропуска, QR, эталонное фото |
| `Tansu.Verify.Api` | Проверка QR, вызов face-verify |
| `face-verify` | Python + DeepFace (Facenet) |
| `verify-web` | UI охраны: QR → селфи → допуск |

Проверка на проходной: https://localhost:5174

Первый запуск Docker скачивает модель DeepFace (~150 МБ). Azure не требуется.

**Камера с телефона:** нужен HTTPS. Откройте `https://<LAN-IP>:5174`, примите сертификат. В `.env` укажите `AccessPass__VerifyWebBaseUrl=https://<LAN-IP>:5174`.

Настройки: `FACE_VERIFY_MODEL`, `FACE_VERIFY_DETECTOR`, `EMPLOYEE_PHOTO_REQUIRE_MANUAL`.

## Тесты

```bash
cd backend && dotnet test
cd frontend && npm test
```

Все HTTP-эндпоинты и методы `frontend/src/api/*.ts` покрыты smoke/контракт-тестами. При новой фиче обновляйте каталоги — подробности в [docs/TESTING.md](docs/TESTING.md).

## Разработка без Docker

```bash
# PostgreSQL и RabbitMQ должны быть доступны
cd backend && dotnet run --project src/Tansu.Api
cd frontend && npm install && npm run dev
cd employee-portal-web && npm install && npm run dev
cd verify-web && npm install && npm run dev
cd face-verify && pip install -r requirements.txt && uvicorn app.main:app --port 8092
cd backend && dotnet run --project src/Tansu.Verify.Api --urls http://localhost:8091
```

Конфигурация — `.env.example`.
