/*
  ISPO v2: онлайн-уроки, материалы уроков, сдача работ студентами.
  Выполнять после:
  1) 01_create_diplom_db.sql
  2) 02_seed_diplom_db.sql
  3) 03_users_and_roles.sql
*/

IF DB_ID(N'DiplomISPO') IS NULL
    THROW 50001, N'База данных DiplomISPO не найдена.', 1;
GO

USE DiplomISPO;
GO

-- 1) Расширение расписания полями онлайн-урока
IF COL_LENGTH('dbo.Schedules', 'ConferenceUrl') IS NULL
    ALTER TABLE dbo.Schedules ADD ConferenceUrl NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.Schedules', 'LiveStatus') IS NULL
    ALTER TABLE dbo.Schedules ADD LiveStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Schedules_LiveStatus DEFAULT N'planned';
GO

IF COL_LENGTH('dbo.Schedules', 'LiveStartedAt') IS NULL
    ALTER TABLE dbo.Schedules ADD LiveStartedAt DATETIME2 NULL;
GO

IF COL_LENGTH('dbo.Schedules', 'LiveEndedAt') IS NULL
    ALTER TABLE dbo.Schedules ADD LiveEndedAt DATETIME2 NULL;
GO

IF OBJECT_ID('dbo.CK_Schedules_LiveStatus', 'C') IS NULL
    ALTER TABLE dbo.Schedules
    ADD CONSTRAINT CK_Schedules_LiveStatus CHECK (LiveStatus IN (N'planned', N'live', N'finished'));
GO

IF OBJECT_ID('dbo.CK_Schedules_ConferenceUrl_Https', 'C') IS NULL
    ALTER TABLE dbo.Schedules
    ADD CONSTRAINT CK_Schedules_ConferenceUrl_Https
    CHECK (ConferenceUrl IS NULL OR ConferenceUrl LIKE N'https://%');
GO

-- 2) Материалы конкретного урока
IF OBJECT_ID('dbo.LessonMaterials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonMaterials (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ScheduleId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        MaterialType NVARCHAR(40) NOT NULL,
        CloudUrl NVARCHAR(1000) NOT NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_LessonMaterials_Schedules FOREIGN KEY (ScheduleId)
            REFERENCES dbo.Schedules(Id) ON DELETE CASCADE,
        CONSTRAINT CK_LessonMaterials_MaterialType CHECK (MaterialType IN (N'task', N'methodic', N'reference'))
    );
END;
GO

IF OBJECT_ID('dbo.IX_LessonMaterials_ScheduleId', 'IX') IS NULL
    CREATE INDEX IX_LessonMaterials_ScheduleId ON dbo.LessonMaterials(ScheduleId);
GO

IF OBJECT_ID('dbo.CK_LessonMaterials_CloudUrl_Https', 'C') IS NULL
    ALTER TABLE dbo.LessonMaterials
    ADD CONSTRAINT CK_LessonMaterials_CloudUrl_Https
    CHECK (CloudUrl LIKE N'https://%');
GO

-- 3) Сдачи работ по урокам
IF OBJECT_ID('dbo.LessonSubmissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonSubmissions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ScheduleId INT NOT NULL,
        StudentId INT NOT NULL,
        CloudFileUrl NVARCHAR(1000) NOT NULL,
        StudentComment NVARCHAR(2000) NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'new',
        Score DECIMAL(4,2) NULL,
        TeacherComment NVARCHAR(2000) NULL,
        RevisionNumber INT NOT NULL DEFAULT 1,
        SubmittedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ReviewedAt DATETIME2 NULL,
        CONSTRAINT FK_LessonSubmissions_Schedules FOREIGN KEY (ScheduleId)
            REFERENCES dbo.Schedules(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LessonSubmissions_Students FOREIGN KEY (StudentId)
            REFERENCES dbo.Students(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_LessonSubmissions_ScheduleStudent UNIQUE (ScheduleId, StudentId),
        CONSTRAINT CK_LessonSubmissions_Status CHECK (Status IN (N'new', N'in_review', N'revision', N'accepted')),
        CONSTRAINT CK_LessonSubmissions_Score CHECK (Score IS NULL OR (Score BETWEEN 0 AND 5)),
        CONSTRAINT CK_LessonSubmissions_Revision CHECK (RevisionNumber >= 1)
    );
END;
GO

IF OBJECT_ID('dbo.IX_LessonSubmissions_StudentId', 'IX') IS NULL
    CREATE INDEX IX_LessonSubmissions_StudentId ON dbo.LessonSubmissions(StudentId);
GO

IF OBJECT_ID('dbo.IX_LessonSubmissions_Status', 'IX') IS NULL
    CREATE INDEX IX_LessonSubmissions_Status ON dbo.LessonSubmissions(Status);
GO

IF OBJECT_ID('dbo.CK_LessonSubmissions_CloudFileUrl_Https', 'C') IS NULL
    ALTER TABLE dbo.LessonSubmissions
    ADD CONSTRAINT CK_LessonSubmissions_CloudFileUrl_Https
    CHECK (CloudFileUrl LIKE N'https://%');
GO

-- 4) Небольшой seed для демонстрации
DECLARE @sampleScheduleId INT = (SELECT TOP 1 Id FROM dbo.Schedules ORDER BY LessonDate, StartTime);
IF @sampleScheduleId IS NOT NULL
BEGIN
    UPDATE dbo.Schedules
    SET ConferenceUrl = COALESCE(ConferenceUrl, N'https://meet.google.com/abc-defg-hij'),
        LiveStatus = COALESCE(NULLIF(LiveStatus, N''), N'planned')
    WHERE Id = @sampleScheduleId;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.LessonMaterials
        WHERE ScheduleId = @sampleScheduleId AND Title = N'Практическое задание №1'
    )
        INSERT INTO dbo.LessonMaterials (ScheduleId, Title, MaterialType, CloudUrl, SortOrder)
        VALUES (@sampleScheduleId, N'Практическое задание №1', N'task', N'https://drive.google.com/file/d/task-demo', 1);

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.LessonMaterials
        WHERE ScheduleId = @sampleScheduleId AND Title = N'Методические указания'
    )
        INSERT INTO dbo.LessonMaterials (ScheduleId, Title, MaterialType, CloudUrl, SortOrder)
        VALUES (@sampleScheduleId, N'Методические указания', N'methodic', N'https://drive.google.com/file/d/methodic-demo', 2);
END;
GO

SELECT TOP 20 Id, LessonDate, StartTime, EndTime, ConferenceUrl, LiveStatus, LiveStartedAt, LiveEndedAt
FROM dbo.Schedules
ORDER BY LessonDate, StartTime;

SELECT TOP 20 *
FROM dbo.LessonMaterials
ORDER BY CreatedAt DESC;

SELECT TOP 20 *
FROM dbo.LessonSubmissions
ORDER BY SubmittedAt DESC;
GO

PRINT N'Миграция ISPO v2 успешно применена.';
