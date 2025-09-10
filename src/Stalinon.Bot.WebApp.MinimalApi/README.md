# Stalinon.Bot.WebApp.MinimalApi

Эндпоинты авторизации и профиля для Telegram Web App.

## Возможности
- проверка `initData` и выдача короткого JWT;
- получение сведений о пользователе по токену.

## Использование
```csharp
app.MapWebAppAuth();
app.MapWebAppMe();
```
Секрет и время жизни настраиваются в `WebAppAuthOptions`.
