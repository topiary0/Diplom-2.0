/*
  Заполнение тестовыми данными БД DiplomISPO
  Выполнять после 01_create_diplom_db.sql
*/

IF DB_ID(N'DiplomISPO') IS NULL
    THROW 50001, N'База данных DiplomISPO не найдена. Сначала выполните 01_create_diplom_db.sql', 1;
GO

USE DiplomISPO;
GO

BEGIN TRY
    BEGIN TRAN;

    /* 1) Справочники */
    IF NOT EXISTS (SELECT 1 FROM dbo.CourseCategories WHERE Name = N'Программирование')
        INSERT INTO dbo.CourseCategories (Name, Description)
        VALUES (N'Программирование', N'Курсы по разработке ПО и алгоритмам');

    IF NOT EXISTS (SELECT 1 FROM dbo.CourseCategories WHERE Name = N'Веб-разработка')
        INSERT INTO dbo.CourseCategories (Name, Description)
        VALUES (N'Веб-разработка', N'Frontend и backend для веб-приложений');

    IF NOT EXISTS (SELECT 1 FROM dbo.CourseCategories WHERE Name = N'Базы данных')
        INSERT INTO dbo.CourseCategories (Name, Description)
        VALUES (N'Базы данных', N'Проектирование, SQL и администрирование БД');

    IF NOT EXISTS (SELECT 1 FROM dbo.StudentGroups WHERE GroupCode = N'ИСПО-24-1')
        INSERT INTO dbo.StudentGroups (GroupCode, Name, Specialization, StartYear)
        VALUES (N'ИСПО-24-1', N'Группа ИСПО 24-1', N'Информационные системы и программирование', 2024);

    IF NOT EXISTS (SELECT 1 FROM dbo.StudentGroups WHERE GroupCode = N'ИСПО-24-2')
        INSERT INTO dbo.StudentGroups (GroupCode, Name, Specialization, StartYear)
        VALUES (N'ИСПО-24-2', N'Группа ИСПО 24-2', N'Информационные системы и программирование', 2024);

    IF NOT EXISTS (SELECT 1 FROM dbo.StudentGroups WHERE GroupCode = N'ИСПО-25-1')
        INSERT INTO dbo.StudentGroups (GroupCode, Name, Specialization, StartYear)
        VALUES (N'ИСПО-25-1', N'Группа ИСПО 25-1', N'Прикладная информатика', 2025);

    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE Email = N'ivanova@ispo.local')
        INSERT INTO dbo.Teachers (FullName, Email, Department, PositionName)
        VALUES (N'Иванова Мария Сергеевна', N'ivanova@ispo.local', N'Отделение ИТ', N'Преподаватель');

    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE Email = N'petrov@ispo.local')
        INSERT INTO dbo.Teachers (FullName, Email, Department, PositionName)
        VALUES (N'Петров Алексей Игоревич', N'petrov@ispo.local', N'Отделение ИТ', N'Старший преподаватель');

    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE Email = N'smirnova@ispo.local')
        INSERT INTO dbo.Teachers (FullName, Email, Department, PositionName)
        VALUES (N'Смирнова Ольга Викторовна', N'smirnova@ispo.local', N'Центр ДПО', N'Методист');

    /* 2) Студенты */
    DECLARE @group1 INT = (SELECT Id FROM dbo.StudentGroups WHERE GroupCode = N'ИСПО-24-1');
    DECLARE @group2 INT = (SELECT Id FROM dbo.StudentGroups WHERE GroupCode = N'ИСПО-24-2');
    DECLARE @group3 INT = (SELECT Id FROM dbo.StudentGroups WHERE GroupCode = N'ИСПО-25-1');

    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE Email = N'orlov.v@ispo.local')
        INSERT INTO dbo.Students (LastName, FirstName, MiddleName, BirthDate, Email, Phone, GroupId)
        VALUES (N'Орлов', N'Владимир', N'Павлович', '2006-03-11', N'orlov.v@ispo.local', N'+7-900-111-22-33', @group1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE Email = N'egorova.a@ispo.local')
        INSERT INTO dbo.Students (LastName, FirstName, MiddleName, BirthDate, Email, Phone, GroupId)
        VALUES (N'Егорова', N'Анна', N'Романовна', '2006-06-19', N'egorova.a@ispo.local', N'+7-900-111-44-55', @group1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE Email = N'volkov.d@ispo.local')
        INSERT INTO dbo.Students (LastName, FirstName, MiddleName, BirthDate, Email, Phone, GroupId)
        VALUES (N'Волков', N'Даниил', N'Андреевич', '2005-12-05', N'volkov.d@ispo.local', N'+7-900-222-33-44', @group2);

    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE Email = N'belova.k@ispo.local')
        INSERT INTO dbo.Students (LastName, FirstName, MiddleName, BirthDate, Email, Phone, GroupId)
        VALUES (N'Белова', N'Кристина', N'Юрьевна', '2007-01-21', N'belova.k@ispo.local', N'+7-900-333-55-66', @group3);

    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE Email = N'nikitin.m@ispo.local')
        INSERT INTO dbo.Students (LastName, FirstName, MiddleName, BirthDate, Email, Phone, GroupId)
        VALUES (N'Никитин', N'Максим', N'Олегович', '2006-08-30', N'nikitin.m@ispo.local', N'+7-900-444-66-77', @group2);

    /* 3) Курсы */
    DECLARE @catProg INT = (SELECT Id FROM dbo.CourseCategories WHERE Name = N'Программирование');
    DECLARE @catWeb INT = (SELECT Id FROM dbo.CourseCategories WHERE Name = N'Веб-разработка');
    DECLARE @catDb INT = (SELECT Id FROM dbo.CourseCategories WHERE Name = N'Базы данных');

    DECLARE @teacherIvanova INT = (SELECT Id FROM dbo.Teachers WHERE Email = N'ivanova@ispo.local');
    DECLARE @teacherPetrov INT = (SELECT Id FROM dbo.Teachers WHERE Email = N'petrov@ispo.local');
    DECLARE @teacherSmirnova INT = (SELECT Id FROM dbo.Teachers WHERE Email = N'smirnova@ispo.local');

    IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE CourseCode = N'CSHARP-START')
        INSERT INTO dbo.Courses (CourseCode, Title, Description, DurationHours, CategoryId, TeacherId, IsActive)
        VALUES (N'CSHARP-START', N'Основы C#', N'Базовый курс по языку C# и ООП', 72, @catProg, @teacherIvanova, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE CourseCode = N'WEB-PRO')
        INSERT INTO dbo.Courses (CourseCode, Title, Description, DurationHours, CategoryId, TeacherId, IsActive)
        VALUES (N'WEB-PRO', N'Веб-приложения на ASP.NET Core', N'Практика разработки MVC-приложений', 96, @catWeb, @teacherPetrov, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE CourseCode = N'SQL-DBA')
        INSERT INTO dbo.Courses (CourseCode, Title, Description, DurationHours, CategoryId, TeacherId, IsActive)
        VALUES (N'SQL-DBA', N'SQL и проектирование БД', N'Нормализация, индексы, представления, процедуры', 64, @catDb, @teacherSmirnova, 1);

    /* 4) Материалы курсов */
    DECLARE @courseCSharp INT = (SELECT Id FROM dbo.Courses WHERE CourseCode = N'CSHARP-START');
    DECLARE @courseWeb INT = (SELECT Id FROM dbo.Courses WHERE CourseCode = N'WEB-PRO');
    DECLARE @courseSql INT = (SELECT Id FROM dbo.Courses WHERE CourseCode = N'SQL-DBA');

    IF NOT EXISTS (SELECT 1 FROM dbo.CourseMaterials WHERE CourseId = @courseCSharp AND Title = N'Синтаксис C#')
        INSERT INTO dbo.CourseMaterials (CourseId, Title, MaterialType, FileUrl, SortOrder)
        VALUES (@courseCSharp, N'Синтаксис C#', N'lecture', N'https://example.local/csharp/syntax.pdf', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.CourseMaterials WHERE CourseId = @courseWeb AND Title = N'Архитектура MVC')
        INSERT INTO dbo.CourseMaterials (CourseId, Title, MaterialType, FileUrl, SortOrder)
        VALUES (@courseWeb, N'Архитектура MVC', N'lecture', N'https://example.local/web/mvc.pdf', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.CourseMaterials WHERE CourseId = @courseSql AND Title = N'Нормализация БД')
        INSERT INTO dbo.CourseMaterials (CourseId, Title, MaterialType, FileUrl, SortOrder)
        VALUES (@courseSql, N'Нормализация БД', N'lecture', N'https://example.local/sql/normalization.pdf', 1);

    /* 5) Зачисления */
    DECLARE @studentOrlov INT = (SELECT Id FROM dbo.Students WHERE Email = N'orlov.v@ispo.local');
    DECLARE @studentEgorova INT = (SELECT Id FROM dbo.Students WHERE Email = N'egorova.a@ispo.local');
    DECLARE @studentVolkov INT = (SELECT Id FROM dbo.Students WHERE Email = N'volkov.d@ispo.local');
    DECLARE @studentBelova INT = (SELECT Id FROM dbo.Students WHERE Email = N'belova.k@ispo.local');
    DECLARE @studentNikitin INT = (SELECT Id FROM dbo.Students WHERE Email = N'nikitin.m@ispo.local');

    IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @studentOrlov AND CourseId = @courseCSharp)
        INSERT INTO dbo.Enrollments (StudentId, CourseId, Status) VALUES (@studentOrlov, @courseCSharp, N'active');

    IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @studentEgorova AND CourseId = @courseWeb)
        INSERT INTO dbo.Enrollments (StudentId, CourseId, Status) VALUES (@studentEgorova, @courseWeb, N'active');

    IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @studentVolkov AND CourseId = @courseSql)
        INSERT INTO dbo.Enrollments (StudentId, CourseId, Status) VALUES (@studentVolkov, @courseSql, N'active');

    IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @studentBelova AND CourseId = @courseCSharp)
        INSERT INTO dbo.Enrollments (StudentId, CourseId, Status) VALUES (@studentBelova, @courseCSharp, N'active');

    IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @studentNikitin AND CourseId = @courseWeb)
        INSERT INTO dbo.Enrollments (StudentId, CourseId, Status) VALUES (@studentNikitin, @courseWeb, N'active');

    /* 6) Расписание */
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Schedules
        WHERE CourseId = @courseCSharp AND GroupId = @group1 AND LessonDate = '2026-04-15' AND StartTime = '10:00'
    )
        INSERT INTO dbo.Schedules (CourseId, GroupId, LessonDate, StartTime, EndTime, Room, LessonTopic)
        VALUES (@courseCSharp, @group1, '2026-04-15', '10:00', '11:30', N'А-301', N'Типы данных и операторы');

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Schedules
        WHERE CourseId = @courseWeb AND GroupId = @group2 AND LessonDate = '2026-04-16' AND StartTime = '12:00'
    )
        INSERT INTO dbo.Schedules (CourseId, GroupId, LessonDate, StartTime, EndTime, Room, LessonTopic)
        VALUES (@courseWeb, @group2, '2026-04-16', '12:00', '13:30', N'Б-204', N'Razor и маршрутизация');

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Schedules
        WHERE CourseId = @courseSql AND GroupId = @group3 AND LessonDate = '2026-04-17' AND StartTime = '09:30'
    )
        INSERT INTO dbo.Schedules (CourseId, GroupId, LessonDate, StartTime, EndTime, Room, LessonTopic)
        VALUES (@courseSql, @group3, '2026-04-17', '09:30', '11:00', N'В-105', N'JOIN, GROUP BY, HAVING');

    /* 7) Посещаемость */
    DECLARE @schedule1 INT = (
        SELECT Id FROM dbo.Schedules WHERE CourseId = @courseCSharp AND GroupId = @group1 AND LessonDate = '2026-04-15' AND StartTime = '10:00'
    );
    DECLARE @schedule2 INT = (
        SELECT Id FROM dbo.Schedules WHERE CourseId = @courseWeb AND GroupId = @group2 AND LessonDate = '2026-04-16' AND StartTime = '12:00'
    );
    DECLARE @schedule3 INT = (
        SELECT Id FROM dbo.Schedules WHERE CourseId = @courseSql AND GroupId = @group3 AND LessonDate = '2026-04-17' AND StartTime = '09:30'
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.Attendances WHERE ScheduleId = @schedule1 AND StudentId = @studentOrlov)
        INSERT INTO dbo.Attendances (ScheduleId, StudentId, IsPresent)
        VALUES (@schedule1, @studentOrlov, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Attendances WHERE ScheduleId = @schedule2 AND StudentId = @studentEgorova)
        INSERT INTO dbo.Attendances (ScheduleId, StudentId, IsPresent)
        VALUES (@schedule2, @studentEgorova, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Attendances WHERE ScheduleId = @schedule3 AND StudentId = @studentVolkov)
        INSERT INTO dbo.Attendances (ScheduleId, StudentId, IsPresent)
        VALUES (@schedule3, @studentVolkov, 0);

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
GO

/* 9) Быстрые проверки */
SELECT 'Students' AS EntityName, COUNT(*) AS TotalCount FROM dbo.Students
UNION ALL SELECT 'Teachers', COUNT(*) FROM dbo.Teachers
UNION ALL SELECT 'Courses', COUNT(*) FROM dbo.Courses
UNION ALL SELECT 'Enrollments', COUNT(*) FROM dbo.Enrollments
UNION ALL SELECT 'Schedules', COUNT(*) FROM dbo.Schedules
UNION ALL SELECT 'Attendances', COUNT(*) FROM dbo.Attendances;
GO

SELECT TOP 20 * FROM dbo.vw_CourseEnrollment ORDER BY EnrolledStudents DESC;
SELECT TOP 20 * FROM dbo.vw_StudentProgress ORDER BY StudentName;
SELECT TOP 20 * FROM dbo.vw_ScheduleByGroup ORDER BY LessonDate, StartTime;
GO

PRINT N'Заполнение БД DiplomISPO завершено успешно.';
