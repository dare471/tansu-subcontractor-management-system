# Тестирование

## Обязательное правило для новых фич

При добавлении API-метода или HTTP-эндпоинта **обязательно** добавьте тест:

| Слой | Что обновить |
|------|----------------|
| Backend | Запись в `backend/tests/Tansu.IntegrationTests/ApiEndpointCatalog.cs` |
| Frontend (main app) | Запись в `frontend/src/api/apiRouteRegistry.ts` |

Meta-тесты упадут, если каталог и фактические методы разойдутся.

## Backend

### Запуск

```bash
cd backend
dotnet test
```

### Структура

- **Unit** (`Tansu.UnitTests`) — чистая логика без HTTP.
- **Integration** (`Tansu.IntegrationTests`) — Testcontainers PostgreSQL, демо-seed, реальные HTTP-запросы.

### Покрытие всех эндпоинтов

`ApiEndpointCatalog.cs` — единый реестр всех маршрутов (сейчас **103** эндпоинта).

- `ApiEndpointSmokeTests` — по одному smoke-тесту на каждый маршрут (ответ не 5xx).
- `EndpointCoverageTests` — проверяет уникальность id и полноту каталога.

При добавлении эндпоинта в `Tansu.Api/Endpoints/`:

1. Добавьте строку в `ApiEndpointCatalog.All`.
2. Обновите `ExpectedCount`.
3. При необходимости добавьте плейсхолдеры в `ApiTestContext.ResolvePath`.
4. Запустите `dotnet test`.

Для сложной бизнес-логики добавляйте отдельные сценарные тесты (как `UserCreateTests`, `ApprovalFlowTests`).

## Frontend (main app)

### Запуск

```bash
cd frontend
npm test          # один прогон
npm run test:watch
```

### Покрытие API-клиента

`apiRouteRegistry.ts` — реестр всех HTTP-вызовов и URL-хелперов в `src/api/*.ts` (**87** маршрутов).

`apiRoutes.test.ts` проверяет, что каждый метод вызывает `apiClient` с ожидаемым HTTP-методом и путём.

При добавлении функции в `src/api/*.ts`:

1. Добавьте запись в `apiRouteRegistry`.
2. Обновите `EXPECTED_API_ROUTE_COUNT`.
3. Запустите `npm test`.

## CI

GitHub Actions (`.github/workflows/test.yml`) запускает:

- `dotnet test` (unit + integration)
- `npm test` в `frontend/`

## Другие приложения

`employee-portal-web` и `verify-web` — отдельные SPA. При появлении там собственных API-модулей заведите аналогичный реестр и vitest по образцу `frontend/`.
