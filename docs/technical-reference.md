# Техническая документация Stalinon.Bot

Подробный справочник по архитектуре, сборкам и ключевым классам проекта.

## Обзор архитектуры

Stalinon.Bot — модульный фреймворк для разработки ботов и Telegram Web App. Основа проекта — конвейер обработки "обновлений" (Update), поступающих от внешних источников. Каждое обновление проходит цепочку middleware, после чего передаётся обработчику или сцене. Состояние пользователя хранится во внешнем хранилище, а отправка сообщений выполняется через абстракцию `ITransportClient`.

Главные компоненты:

1. **Источники обновлений** (`IUpdateSource`) получают события из Telegram и других систем.
2. **Очередь** (`UpdateQueue`) буферизует входящие обновления и ограничивает их число.
3. **Пайплайн** (`IUpdatePipeline`) строит цепочку middleware и обработчиков.
4. **Сцены** (`IScene`, `ISceneNavigator`) позволяют реализовывать пошаговые сценарии диалога.
5. **Транспорт** (`ITransportClient`) отправляет ответы пользователям.
6. **Хранилище состояния** (`IStateStore`) сохраняет прогресс сцен и другие данные пользователя.
7. **Дополнительные подсистемы** — наблюдаемость, логирование, администрирование, планировщик фоновых задач и аутбокс для надёжной доставки сообщений.

### Поток обработки обновления

1. `TelegramWebhookSource` или `TelegramPollingSource` получает `Update` от Telegram.
2. Обновление помещается в `UpdateQueue`, где может быть отсечено `DedupMiddleware`, если ключ уже встречался.
3. `PipelineBuilder` выстраивает цепочку middleware. Цепочка включает логирование, трассировку, лимиты, сбор метрик и маршрутизацию.
4. `RouterMiddleware` подбирает обработчик по атрибутам `CommandAttribute` и `UpdateFilterAttribute`.
5. Обработчик или сцена взаимодействует с `IStateStore` и `ITransportClient` для чтения состояния и отправки сообщений.
6. `OutboxTransportClient` гарантирует сохранность исходящих сообщений до фактической отправки.
7. `StatsCollector` и `BotMetricsEventSource` фиксируют результаты обработки для наблюдаемости.

## Сборки и ключевые компоненты

### Stalinon.Bot.Abstractions

Библиотека контрактов и базовых типов. Выделена отдельно, чтобы остальные проекты зависели только от интерфейсов и не создавали циклов зависимостей.

**Основные интерфейсы:**

- `IUpdateSource` — источник обновлений (вебхук, поллинг и т.д.).
- `IUpdatePipeline` и `IUpdateMiddleware` — инфраструктура конвейера.
- `IUpdateHandler` — конечные обработчики, реагирующие на подходящие обновления.
- `IScene`, `ISceneNavigator`, `SceneState` — API для шаговых сценариев.
- `IStateStore` и `IDistributedLock` — хранение состояния и примитивы синхронизации.
- `ITransportClient` — единая точка отправки сообщений.
- `IBotUi` и `IWebAppQueryResponder` — работа с Telegram Web App.

**Атрибуты и вспомогательные типы:**

- `CommandAttribute` — помечает метод‑обработчик команды.
- `TextMatchAttribute` — привязка регулярного выражения к текстовому сообщению.
- `UpdateFilterAttribute` — фильтрация по типу обновления.
- `UpdateContext` — модель, описывающая приходящее обновление и окружение.
- `UpdateItems` — набор ключей для хранения временных данных в `UpdateContext`.

### Stalinon.Bot.Core

Содержит реализацию большинства механизмов фреймворка: пайплайн, сцены, middleware, сбор метрик и вспомогательные утилиты. Проект выделен отдельно, чтобы ядро не зависело от конкретного транспорта или хранилища.

#### Pipeline

- `PipelineBuilder` — потокобезопасно собирает последовательность middleware. При вызове `Build` каждая компонента оборачивает следующую, а каждый update обрабатывается в собственном `IServiceScope`.

#### Middleware

- `LoggingMiddleware` — пишет входящие/исходящие сообщения и параметры обработки.
- `ExceptionHandlingMiddleware` — ловит исключения, логирует их и отправляет пользователю стандартный ответ.
- `RouterMiddleware` — подбирает подходящий `IUpdateHandler` по атрибутам и запускает его.
- `CommandParsingMiddleware` — разбирает текст команды в массив аргументов, поддерживая кавычки.
- `RateLimitMiddleware` — ограничивает частоту сообщений от одного пользователя.
- `DedupMiddleware` — отсеивает дубли на основе `GuidMessageKeyProvider` и `TtlCache`.
- `MetricsMiddleware` — собирает базовые метрики длительности и количества обработанных обновлений.

#### Scenes

- `SceneNavigator` — хранит текущее состояние пользователя в `IStateStore`, переключает шаги и выход из сцен.
- `PromptScene` — простейшая сцена вопрос–ответ с валидацией строки.
- `WizardSceneBuilder` — построитель многошаговых мастеров, упрощающий объявление последовательности сцен.
- `Validators` — набор стандартных валидаторов (например, для чисел и диапазонов).

#### Queue и транспорт

- `UpdateQueue` — блокирующая очередь с ограниченной ёмкостью; при переполнении новые обновления отбрасываются.
- `GuidMessageKeyProvider` — выдаёт уникальный ключ для сообщения по его содержимому.
- `OutboxTransportClient` — обёртка над `ITransportClient`, сохраняющая исходящие сообщения в аутбокс перед отправкой.

#### Метрики и статистика

- `BotMetricsEventSource` — источник EventCounters для базовых метрик.
- `StatsCollector`, `Measurement`, `CustomStats` — детализированный сбор данных о длительности и количестве вызовов обработчиков.
- `WebAppStatsCollector` — отдельная статистика по Web App.

#### Утилиты

- `TtlCache` — словарь с временем жизни ключа; используется для дедупликации и других временных данных.
- `JsonUtils` — компактные функции сериализации/десериализации JSON.
- `HandlerData` — структурированная информация об обработчике для статистики.

#### Конфигурация

- `DeduplicationOptions`, `RateLimitOptions`, `QueueOptions` — наборы настроек для соответствующих механизмов.

### Stalinon.Bot.Hosting

Слой интеграции с `Microsoft.Extensions.Hosting`. Содержит расширения для регистрации всех компонентов и фоновые службы. Благодаря отдельной сборке логика хостинга отделена от ядра и может заменяться.

**Ключевые типы:**

- `ServiceCollectionExtensions` — единая точка регистрации ядра, транспорта, хранилища и доп. компонентов.
- `BotHostedService` — фоновый сервис, запускающий `IUpdateSource`, читает из `UpdateQueue` и передаёт обновления в пайплайн.
- `EndpointRouteBuilderExtensions` — готовые эндпоинты для проверки готовности сервиса.
- Опции: `BotOptions`, `TransportOptions`, `OutboxOptions`, `WebAppOptions`, `WebAppAuthOptions`, `WebhookOptions`, `WebAppCspOptions`, `ObservabilityOptions`, `ObservabilityExportOptions`, `StopOptions`.

### Stalinon.Bot.Telegram

Интеграция с Telegram Bot API. Сборка выделена для изоляции зависимостей от `Telegram.Bot` и специфичных типов.

**Компоненты транспорта и источников:**

- `TelegramTransportClient` — отправка сообщений, действий, клавиатур и web app ответов.
- `TelegramWebhookSource` — приём обновлений через вебхук; `EndpointRouteBuilderExtensions` подключает маршрут `/bot`.
- `TelegramPollingSource` — альтернатива вебхуку: периодический опрос Telegram.
- `TelegramUpdateMapper` — преобразует `Update` из Telegram в `UpdateContext` с заполнением чата, пользователя и текста.

**Дополнительные сервисы:**

- `WebhookService` — установка и удаление вебхука.
- `ChatMenuService` — управление меню чата и кнопками.
- `TelegramWebAppQueryResponder` и `WebAppInitDataValidator` — поддержка Telegram Web App.
- `TelegramBotUi` — генерация ссылок для запуска Web App из Telegram.
- `ServiceCollectionExtensions` — регистрация транспорта и сервисов.

### Stalinon.Bot.Admin.MinimalApi

Минимальный HTTP API для администрирования. Отделён от основного кода, чтобы продакшн-сборка бота не требовала ASP.NET по умолчанию.

- `StorageProbe`, `QueueProbe`, `TransportProbe` — проверки доступности зависимостей.
- `EndpointRouteBuilderExtensions` — монтирование эндпоинтов `/admin/*`.
- `BroadcastRequest` — модель запроса на массовую рассылку.
- `AdminOptions` и `ServiceCollectionExtensions` — настройка и регистрация API.

### Stalinon.Bot.Logging

Кастомное логирование.

- `BotConsoleFormatter` — вывод логов в компактном формате, скрывая токены и длинные сообщения.
- `LoggingBuilderExtensions` — подключение форматтера, чтение уровня логирования из `LOG_LEVEL`.
- `BotLoggerOptions` — параметры форматтера (максимальная длина строки и др.).

### Stalinon.Bot.Observability

Метрики и трассировка. Сборка отделена, чтобы основной проект не зависел от OpenTelemetry при желании его не использовать.

- `Telemetry` — `ActivitySource` для формирования трейсов.
- `TracingTransportClient` и `TracingStateStore` — декораторы, создающие активности вокруг операций транспорта и хранилища.
- `ServiceCollectionExtensions` — регистрация OpenTelemetry и экспортёров.

### Stalinon.Bot.Outbox

Реализация паттерна «аутбокс», обеспечивающего надёжную доставку сообщений. Позволяет сохранить исходящее сообщение в стойкое хранилище, а затем отправить его в транспорт, что защищает от потери данных при сбое.

- `FileOutbox` — простое файловое хранилище сообщений.
- `RedisOutbox` — распределённый аутбокс на Redis, подходящий для нескольких инстансов бота.

### Stalinon.Bot.Scheduler

Простой планировщик фоновых задач.

- `JobScheduler` — регистрация и периодический запуск задач, используемых, например, для очистки аутбокса.

### Stalinon.Bot.Storage.EFCore

Хранилище состояния на базе Entity Framework Core.

- `StateContext` — `DbContext` с таблицей состояний.
- `StateEntry` — сущность, содержащая идентификатор пользователя, данные сцены и время последнего обновления.
- `EfCoreStateStore` — реализация `IStateStore` поверх EF Core.
- `StateContextModelSnapshot` и `Initial` — миграции для базы данных.

### Stalinon.Bot.Storage.File

Локальное файловое хранилище.

- `FileStateStore` — хранит JSON‑файлы в указанной директории.
- `FileStoreOptions` — путь к каталогу и дополнительные параметры.

### Stalinon.Bot.Storage.Redis

Хранилище состояния и утилиты на Redis.

- `RedisStateStore` — сохранение состояния в Redis, поддержка TTL.
- `RedisOptions` — параметры подключения.
- `RedisDistributedLock` — распределённый лок на основе Redlock.
- `RedisSortedSet` — обёртка над отсортированными множествами для задач планировщика.

### Stalinon.Bot.TestKit

Набор вспомогательных компонентов для модульных и интеграционных тестов.

- `SceneTestExtensions` — упрощают запуск сцен в тестах.
- `InMemoryStateStore` и `FakeDistributedLock` — реализация хранилища и блокировок в памяти.
- `FakeTransportClient` — сохраняет отправленные сообщения в память для проверки.
- `FakeJobScheduler` — тестовый планировщик.
- `JsonUpdateSource` — читает обновления из JSON‑файла, воспроизводя реальные события Telegram.

### Stalinon.Bot.WebApp.MinimalApi

Инфраструктура для обслуживания Telegram Web App в стиле Minimal API.

- `EndpointRouteBuilderExtensions` — регистрирует эндпоинты веб‑приложения.
- `ApplicationBuilderExtensions` — подключает статические файлы, авторизацию и Content Security Policy.

## Использование и расширение

1. Зарегистрируйте нужные сервисы через `ServiceCollectionExtensions` из `Stalinon.Bot.Hosting`.
2. Подключите `TelegramWebhookSource` или `TelegramPollingSource` в зависимости от способа получения обновлений.
3. Добавьте собственные middleware и обработчики, реализуя `IUpdateMiddleware` и `IUpdateHandler`.
4. Для сложных диалогов используйте `WizardSceneBuilder` и `SceneNavigator`.
5. Включите `Stalinon.Bot.Logging` и `Stalinon.Bot.Observability`, чтобы получать логи и метрики.

## Нахождение сборок в решении

Каждая сборка вынесена отдельно по следующим причинам:

- **Abstractions** — минимальный набор контрактов; позволяет подключать новые модули без зависимости от конкретных реализаций.
- **Core** — ядро с реализациями, не привязанными к окружению.
- **Hosting** — интеграция с `IHost`, может быть заменена на другой контейнер.
- **Transport и Storage** — в отдельных проектах, чтобы можно было выбирать конкретную технологию (Telegram, Redis, файловая система, EF Core).
- **Observability, Logging, Admin API** — опциональные подсистемы, подключаемые по мере необходимости.
- **Outbox и Scheduler** — дополнительные механизмы, которые используются не во всех инсталляциях.
- **TestKit** — инструменты разработки, не требующие включения в итоговую сборку.

## Заключение

Документ описывает все сборки и ключевые классы Stalinon.Bot. Он помогает быстро понять принципы работы проекта, чтобы легко расширять его, заменять реализации и внедрять новые сценарии.
