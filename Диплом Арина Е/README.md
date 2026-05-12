# Diplom ISPO

Дипломный проект: веб-приложение для управления дополнительным образованием ИСПО на базе ASP.NET Core и SQL Server.

## Содержимое репозитория

- `01_create_diplom_db.sql` - создание структуры базы `DiplomISPO`.
- `02_seed_diplom_db.sql` - заполнение базы тестовыми данными.
- `03_users_and_roles.sql` - создание таблицы пользователей и стартовых аккаунтов.
- `ISPO.WebApp.sln` и `ISPO.WebApp/` - веб-приложение ASP.NET Core MVC.

## Порядок запуска

1. Выполните в SQL Server скрипт `01_create_diplom_db.sql`.
2. Выполните скрипт `02_seed_diplom_db.sql`.
3. Выполните скрипт `03_users_and_roles.sql`.
4. Откройте решение `ISPO.WebApp.sln`.
5. Запустите проект `ISPO.WebApp`.

## Демо-аккаунты

- `admin@ispo.local / admin123`
- `petrov@ispo.local / teacher123`
- `orlov.v@ispo.local / student123`
