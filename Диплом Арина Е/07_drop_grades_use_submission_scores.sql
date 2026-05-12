/*
  Миграция: удаление старой таблицы Grades и переход на оценки из LessonSubmissions (шкала 0-5).
  Скрипт идемпотентный, можно запускать повторно.
*/

IF DB_ID(N'DiplomISPO') IS NULL
    THROW 50001, N'База данных DiplomISPO не найдена.', 1;
GO

USE DiplomISPO;
GO

BEGIN TRY
    BEGIN TRAN;

    -- 1) Приведение старых значений Score к шкале 0-5 (если раньше использовалась шкала 0-100)
    IF OBJECT_ID(N'dbo.LessonSubmissions', N'U') IS NOT NULL
    BEGIN
        UPDATE dbo.LessonSubmissions
        SET Score = CASE
            WHEN Score IS NULL THEN NULL
            WHEN Score > 5 AND Score <= 100 THEN ROUND(Score / 20.0, 2)
            WHEN Score > 100 THEN 5
            WHEN Score < 0 THEN 0
            ELSE Score
        END;

        DECLARE @dropScoreConstraintsSql NVARCHAR(MAX) = N'';
        SELECT @dropScoreConstraintsSql = @dropScoreConstraintsSql +
            N'ALTER TABLE dbo.LessonSubmissions DROP CONSTRAINT [' + cc.name + N'];' + CHAR(10)
        FROM sys.check_constraints cc
        WHERE cc.parent_object_id = OBJECT_ID(N'dbo.LessonSubmissions')
          AND cc.name LIKE N'CK_LessonSubmissions_Score%';

        IF LEN(@dropScoreConstraintsSql) > 0
            EXEC sp_executesql @dropScoreConstraintsSql;

        IF OBJECT_ID(N'dbo.CK_LessonSubmissions_Score_5', N'C') IS NULL
            ALTER TABLE dbo.LessonSubmissions
            ADD CONSTRAINT CK_LessonSubmissions_Score_5 CHECK (Score IS NULL OR (Score BETWEEN 0 AND 5));
    END;

    -- 2) Удаление старой таблицы оценок
    IF OBJECT_ID(N'dbo.Grades', N'U') IS NOT NULL
        DROP TABLE dbo.Grades;

    -- 3) Пересоздание представления прогресса без зависимости от Grades
    IF OBJECT_ID(N'dbo.vw_StudentProgress', N'V') IS NOT NULL
        DROP VIEW dbo.vw_StudentProgress;

    IF OBJECT_ID(N'dbo.LessonSubmissions', N'U') IS NOT NULL
    BEGIN
        EXEC(N'
            CREATE VIEW dbo.vw_StudentProgress
            AS
            SELECT
                s.Id AS StudentId,
                CONCAT(s.LastName, N'' '', s.FirstName, COALESCE(N'' '' + s.MiddleName, N'''')) AS StudentName,
                c.Title AS CourseTitle,
                (
                    SELECT AVG(CAST(ls.Score AS DECIMAL(4,2)))
                    FROM dbo.LessonSubmissions ls
                    JOIN dbo.Schedules sc ON sc.Id = ls.ScheduleId
                    WHERE ls.StudentId = s.Id
                      AND sc.CourseId = c.Id
                      AND ls.Status = N''accepted''
                      AND ls.Score IS NOT NULL
                ) AS AvgGrade,
                (
                    SELECT SUM(CASE WHEN a.IsPresent = 1 THEN 1 ELSE 0 END)
                    FROM dbo.Attendances a
                    WHERE a.StudentId = s.Id
                ) AS PresentCount,
                (
                    SELECT SUM(CASE WHEN a.IsPresent = 0 THEN 1 ELSE 0 END)
                    FROM dbo.Attendances a
                    WHERE a.StudentId = s.Id
                ) AS AbsentCount
            FROM dbo.Students s
            JOIN dbo.Enrollments e ON e.StudentId = s.Id
            JOIN dbo.Courses c ON c.Id = e.CourseId;
        ');
    END
    ELSE
    BEGIN
        EXEC(N'
            CREATE VIEW dbo.vw_StudentProgress
            AS
            SELECT
                s.Id AS StudentId,
                CONCAT(s.LastName, N'' '', s.FirstName, COALESCE(N'' '' + s.MiddleName, N'''')) AS StudentName,
                c.Title AS CourseTitle,
                CAST(NULL AS DECIMAL(4,2)) AS AvgGrade,
                (
                    SELECT SUM(CASE WHEN a.IsPresent = 1 THEN 1 ELSE 0 END)
                    FROM dbo.Attendances a
                    WHERE a.StudentId = s.Id
                ) AS PresentCount,
                (
                    SELECT SUM(CASE WHEN a.IsPresent = 0 THEN 1 ELSE 0 END)
                    FROM dbo.Attendances a
                    WHERE a.StudentId = s.Id
                ) AS AbsentCount
            FROM dbo.Students s
            JOIN dbo.Enrollments e ON e.StudentId = s.Id
            JOIN dbo.Courses c ON c.Id = e.CourseId;
        ');
    END;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;
GO

PRINT N'Миграция завершена: Grades удалена, оценки работают по шкале 0-5.';
GO
