# ISPO.WebApp

ASP.NET Core MVC приложение для дипломного проекта с ролевой моделью:

- Администратор
- Преподаватель
- Ученик

## Основные возможности

- Авторизация по ролям через таблицу `Users`.
- Отдельные кабинеты для каждой роли.
- Администратор: дашборд и управление пользователями.
- Преподаватель: дашборд, управление студентами и курсами.
- Ученик: личный кабинет, мои курсы, оценки, посещаемость, ближайшие занятия.
- Единый адаптивный дизайн для desktop и mobile.

## Подготовка базы данных

Выполните последовательно SQL-скрипты из корня репозитория:

1. `01_create_diplom_db.sql`
2. `02_seed_diplom_db.sql`
3. `03_users_and_roles.sql`

## Настройка подключения

Проверьте строку подключения в `appsettings.json`:

```json
"ConnectionStrings": {
  "DiplomISPO": "Server=localhost;Database=DiplomISPO;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

## Запуск

```bash
dotnet restore ../ISPO.WebApp.sln
dotnet run --project ./ISPO.WebApp.csproj
```

## Демо-логины

- `admin@ispo.local / admin123`
- `petrov@ispo.local / teacher123`
- `orlov.v@ispo.local / student123`
