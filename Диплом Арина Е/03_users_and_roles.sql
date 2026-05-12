/*
  Создание таблицы пользователей и стартовых аккаунтов.
  Выполнять после:
  1) 01_create_diplom_db.sql
  2) 02_seed_diplom_db.sql
*/

IF DB_ID(N'DiplomISPO') IS NULL
    THROW 50001, N'База данных DiplomISPO не найдена.', 1;
GO

USE DiplomISPO;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(120) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Role NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'admin', N'teacher', N'student'))
    );
END;
GO

-- Учебные пароли:
-- admin123 / teacher123 / student123
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@ispo.local')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES (N'Системный администратор', N'admin@ispo.local', N'admin123', N'admin');

-- Общий демонстрационный преподавательский аккаунт
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'teacher@ispo.local')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES (N'Преподаватель ИСПО', N'teacher@ispo.local', N'teacher123', N'teacher');

-- Привязанный к таблице Teachers аккаунт для корректной персональной аналитики
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'petrov@ispo.local')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES (N'Петров Алексей Игоревич', N'petrov@ispo.local', N'teacher123', N'teacher');

-- Общий демонстрационный студенческий аккаунт
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'student@ispo.local')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES (N'Студент ИСПО', N'student@ispo.local', N'student123', N'student');

-- Привязанный к таблице Students аккаунт для личного кабинета
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'orlov.v@ispo.local')
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES (N'Орлов Владимир Павлович', N'orlov.v@ispo.local', N'student123', N'student');
GO

SELECT Id, FullName, Email, Role, IsActive, CreatedAt
FROM dbo.Users
ORDER BY Id;
GO
