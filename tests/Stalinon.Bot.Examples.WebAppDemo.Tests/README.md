# Stalinon.Bot.Examples.WebAppDemo.Tests

Тестовый проект для библиотеки `Stalinon.Bot.Examples.WebAppDemo`.

## Назначение
Проверяет маршруты и авторизацию Web App.

Тесты размещены в каталоге `Integration`.

### Интеграционные проверки
В каталоге `Integration` приложение разворачивается в памяти с фейковым `Bot__Token` и заглушкой валидатора.
Проверяется отдача статической страницы и выдача JWT через `POST /webapp/auth`.

## Запуск
```bash
dotnet test
```
