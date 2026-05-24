# Tansu

Система учёта субподрядчиков и согласования персонала для ТАНСУ.

## Стек

PostgreSQL 16, .NET 10, Vue 3, RabbitMQ, Naive UI.

## Запуск

```bash
cp .env.example .env
docker compose up --build
```

| Сервис | Адрес |
| --- | --- |
| Веб | http://localhost:5173 |
| API | порт `API_PORT` в `.env` (по умолчанию 8080) |
| MailHog | http://localhost:8025 |

## Учётные записи (локальная среда)

| Роль | Email | Пароль |
| --- | --- | --- |
| ТОО «MontazhKomplekt Astana» | `hr@montazh-astana.kz` | `Montazh2024!` |
| ТОО «Qazaq EnergoStroy» | `energo@qazaq-energo.kz` | `Montazh2024!` |
| ТАНСУ | `admin@tansu.local` | вход по email (см. ниже) |

Согласующие: `approver1@tansu.local` … `approver3@tansu.local`, `accounting@tansu.local`.

Локальный вход сотрудников ТАНСУ:

```bash
curl -s -X POST http://localhost:${API_PORT:-8080}/api/auth/dev-login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tansu.local"}'
```

Production — Microsoft Entra ID (`ENTRA_TENANT_ID`, `ENTRA_AUDIENCE`).

## Пакеты согласования

Субподрядчик создаёт **черновик пакета** → добавляет сотрудников → отправляет на согласование одним действием.

## Разработка без Docker

```bash
cd backend && dotnet run --project src/Tansu.Api
cd frontend && npm install && npm run dev
```

Конфигурация — `.env.example`.
