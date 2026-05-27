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
| **Verify Web** (QR + Face ID) | https://localhost:5174 (камера с телефона — https://LAN-IP:5174) |
| **Личный кабинет сотрудника** | http://localhost:5175 |
| **Verify API** | http://localhost:8091 (в dev проксируется через verify-web) |
| **Face Verify** (Python + DeepFace) | http://localhost:8092 |
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

## Личный кабинет сотрудника

После **полного согласования** создаётся доступ в личный кабинет:

1. **Вход:** http://localhost:5175/login — ИИН + одноразовый пароль
2. **Dev:** пароль пишется в `data/employee-portal-credentials.log` (внутри контейнера API: `/app/data/...`)
3. При первом входе — смена пароля
4. В кабинете: объект, описание работ, **опрос по ТБ**
5. После опроса открывается **QR-пропуск** → проверка на verify-web (Face ID)

## QR-пропуск и Face ID

После **полного согласования** сотрудника автоматически выдаётся QR-пропуск. QR содержит ссылку на отдельное приложение проверки.

| Компонент | Назначение |
| --- | --- |
| `Tansu.Api` | хранит пропуска, генерирует QR, отдаёт эталонное фото |
| `Tansu.Verify.Api` | проверяет QR, вызывает Python-сервис сравнения лиц |
| `face-verify` | Python + DeepFace (Facenet): реальное сравнение эталона и селфи |
| `verify-web` | UI охраны: скан QR → Face ID → доступ, запись «на объекте» |

Пропуск виден в разделе **История согласования** сотрудника. Для Face ID нужно загрузить фото сотрудника.

Проверка на проходной: https://localhost:5174

**Face ID:** `face-verify` (Python, [DeepFace](https://github.com/serengil/deepface), модель `Facenet`) сравнивает эталонное фото из Tansu с селфи с камеры. Azure не нужен. Первый запуск Docker скачивает модель (~150 МБ).

После успешного Face ID в истории сотрудника (История согласования → блок **«На объекте»**) появляется запись о проходе.

**Камера с телефона/планшета:** браузер требует HTTPS. Откройте `https://<LAN-IP>:5174` (например `https://192.168.1.128:5174`), примите самоподписанный сертификат. В `.env` задайте `AccessPass__VerifyWebBaseUrl=https://<LAN-IP>:5174`, чтобы QR в Tansu вёл на правильный адрес.

```bash
cd verify-web && npm install && npm run dev   # порт 5174
cd face-verify && pip install -r requirements.txt && uvicorn app.main:app --port 8092
cd backend && dotnet run --project src/Tansu.Verify.Api --urls http://localhost:8091
```

Настройки Face ID в `.env`: `FACE_VERIFY_MODEL`, `FACE_VERIFY_DETECTOR` (`opencv`, `retinaface`, …).

## Разработка без Docker

```bash
cd backend && dotnet run --project src/Tansu.Api
cd frontend && npm install && npm run dev
cd employee-portal-web && npm install && npm run dev   # порт 5175
```

Конфигурация — `.env.example`.
