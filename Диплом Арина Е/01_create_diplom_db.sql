/*
  Проект: Разработка веб-приложения для управления дополнительным образованием ИСПО
  СУБД: Microsoft SQL Server
*/

-- 1) Создание базы данных с нуля
IF DB_ID(N'DiplomISPO') IS NULL
BEGIN
    CREATE DATABASE DiplomISPO;
END;
GO

USE DiplomISPO;
GO

-- 2) Пересоздание схемы dbo (безопасный запуск скрипта повторно)
IF OBJECT_ID('dbo.vw_StudentProgress', 'V') IS NOT NULL DROP VIEW dbo.vw_StudentProgress;
IF OBJECT_ID('dbo.vw_CourseEnrollment', 'V') IS NOT NULL DROP VIEW dbo.vw_CourseEnrollment;
IF OBJECT_ID('dbo.vw_ScheduleByGroup', 'V') IS NOT NULL DROP VIEW dbo.vw_ScheduleByGroup;
GO

IF OBJECT_ID('dbo.Attendances', 'U') IS NOT NULL DROP TABLE dbo.Attendances;
IF OBJECT_ID('dbo.Schedules', 'U') IS NOT NULL DROP TABLE dbo.Schedules;
IF OBJECT_ID('dbo.Enrollments', 'U') IS NOT NULL DROP TABLE dbo.Enrollments;
IF OBJECT_ID('dbo.CourseMaterials', 'U') IS NOT NULL DROP TABLE dbo.CourseMaterials;
IF OBJECT_ID('dbo.Courses', 'U') IS NOT NULL DROP TABLE dbo.Courses;
IF OBJECT_ID('dbo.Teachers', 'U') IS NOT NULL DROP TABLE dbo.Teachers;
IF OBJECT_ID('dbo.Students', 'U') IS NOT NULL DROP TABLE dbo.Students;
IF OBJECT_ID('dbo.StudentGroups', 'U') IS NOT NULL DROP TABLE dbo.StudentGroups;
IF OBJECT_ID('dbo.CourseCategories', 'U') IS NOT NULL DROP TABLE dbo.CourseCategories;
GO

-- 3) Справочники
CREATE TABLE dbo.CourseCategories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL
);
GO

CREATE TABLE dbo.StudentGroups (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    GroupCode NVARCHAR(30) NOT NULL UNIQUE,
    Name NVARCHAR(120) NOT NULL,
    Specialization NVARCHAR(150) NOT NULL,
    StartYear INT NOT NULL CHECK (StartYear BETWEEN 2000 AND 2100)
);
GO

-- 4) Основные сущности
CREATE TABLE dbo.Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LastName NVARCHAR(80) NOT NULL,
    FirstName NVARCHAR(80) NOT NULL,
    MiddleName NVARCHAR(80) NULL,
    BirthDate DATE NOT NULL,
    Email NVARCHAR(120) NOT NULL UNIQUE,
    Phone NVARCHAR(30) NULL,
    GroupId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Students_StudentGroups FOREIGN KEY (GroupId)
        REFERENCES dbo.StudentGroups(Id)
);
GO

CREATE TABLE dbo.Teachers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(180) NOT NULL,
    Email NVARCHAR(120) NOT NULL UNIQUE,
    Department NVARCHAR(150) NOT NULL,
    PositionName NVARCHAR(120) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE dbo.Courses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CourseCode NVARCHAR(40) NOT NULL UNIQUE,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DurationHours INT NOT NULL CHECK (DurationHours > 0),
    CategoryId INT NOT NULL,
    TeacherId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Courses_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.CourseCategories(Id),
    CONSTRAINT FK_Courses_Teachers FOREIGN KEY (TeacherId)
        REFERENCES dbo.Teachers(Id)
);
GO

CREATE TABLE dbo.CourseMaterials (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CourseId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    MaterialType NVARCHAR(40) NOT NULL,
    FileUrl NVARCHAR(500) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_CourseMaterials_Courses FOREIGN KEY (CourseId)
        REFERENCES dbo.Courses(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Enrollments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    CourseId INT NOT NULL,
    EnrolledAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status NVARCHAR(30) NOT NULL DEFAULT N'active',
    CONSTRAINT UQ_Enrollments_StudentCourse UNIQUE (StudentId, CourseId),
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId)
        REFERENCES dbo.Students(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Enrollments_Courses FOREIGN KEY (CourseId)
        REFERENCES dbo.Courses(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Schedules (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CourseId INT NOT NULL,
    GroupId INT NOT NULL,
    LessonDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Room NVARCHAR(50) NULL,
    LessonTopic NVARCHAR(250) NULL,
    CONSTRAINT CK_Schedules_Time CHECK (EndTime > StartTime),
    CONSTRAINT FK_Schedules_Courses FOREIGN KEY (CourseId)
        REFERENCES dbo.Courses(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Schedules_Groups FOREIGN KEY (GroupId)
        REFERENCES dbo.StudentGroups(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Attendances (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NOT NULL,
    StudentId INT NOT NULL,
    IsPresent BIT NOT NULL,
    MarkedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Attendance_Unique UNIQUE (ScheduleId, StudentId),
    CONSTRAINT FK_Attendances_Schedules FOREIGN KEY (ScheduleId)
        REFERENCES dbo.Schedules(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Attendances_Students FOREIGN KEY (StudentId)
        REFERENCES dbo.Students(Id) ON DELETE CASCADE
);
GO

-- 5) Индексы
CREATE INDEX IX_Students_GroupId ON dbo.Students(GroupId);
CREATE INDEX IX_Courses_TeacherId ON dbo.Courses(TeacherId);
CREATE INDEX IX_Courses_CategoryId ON dbo.Courses(CategoryId);
CREATE INDEX IX_Enrollments_StudentId ON dbo.Enrollments(StudentId);
CREATE INDEX IX_Enrollments_CourseId ON dbo.Enrollments(CourseId);
CREATE INDEX IX_Schedules_LessonDate ON dbo.Schedules(LessonDate);
GO

-- 6) Представления (Views)
CREATE VIEW dbo.vw_CourseEnrollment
AS
SELECT
    c.Id AS CourseId,
    c.CourseCode,
    c.Title AS CourseTitle,
    t.FullName AS Teacher,
    COUNT(e.Id) AS EnrolledStudents
FROM dbo.Courses c
JOIN dbo.Teachers t ON t.Id = c.TeacherId
LEFT JOIN dbo.Enrollments e ON e.CourseId = c.Id
GROUP BY c.Id, c.CourseCode, c.Title, t.FullName;
GO

CREATE VIEW dbo.vw_StudentProgress
AS
SELECT
    s.Id AS StudentId,
    CONCAT(s.LastName, N' ', s.FirstName, COALESCE(N' ' + s.MiddleName, N'')) AS StudentName,
    c.Title AS CourseTitle,
    CAST(NULL AS DECIMAL(4,2)) AS AvgGrade,
    SUM(CASE WHEN a.IsPresent = 1 THEN 1 ELSE 0 END) AS PresentCount,
    SUM(CASE WHEN a.IsPresent = 0 THEN 1 ELSE 0 END) AS AbsentCount
FROM dbo.Students s
JOIN dbo.Enrollments e ON e.StudentId = s.Id
JOIN dbo.Courses c ON c.Id = e.CourseId
LEFT JOIN dbo.Attendances a ON a.StudentId = s.Id
GROUP BY s.Id, s.LastName, s.FirstName, s.MiddleName, c.Title;
GO

CREATE VIEW dbo.vw_ScheduleByGroup
AS
SELECT
    g.GroupCode,
    g.Name AS GroupName,
    c.Title AS CourseTitle,
    sc.LessonDate,
    sc.StartTime,
    sc.EndTime,
    sc.Room,
    t.FullName AS Teacher
FROM dbo.Schedules sc
JOIN dbo.StudentGroups g ON g.Id = sc.GroupId
JOIN dbo.Courses c ON c.Id = sc.CourseId
JOIN dbo.Teachers t ON t.Id = c.TeacherId;
GO

PRINT N'База данных DiplomISPO и представления успешно созданы.';
