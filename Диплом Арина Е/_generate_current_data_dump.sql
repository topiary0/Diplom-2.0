SET NOCOUNT ON;

SELECT '-- =============================================================';
SELECT '-- Скрипт заполнения текущими данными БД DiplomISPO';
SELECT '-- Сгенерировано автоматически: ' + CONVERT(varchar(19), GETDATE(), 120);
SELECT '-- =============================================================';
SELECT 'USE [DiplomISPO];';
SELECT 'GO';
SELECT '';

DECLARE @OrderedTables TABLE
(
    SortOrder int NOT NULL,
    TableName sysname NOT NULL
);

INSERT INTO @OrderedTables (SortOrder, TableName)
VALUES
    (10, N'CourseCategories'),
    (20, N'Teachers'),
    (30, N'StudentGroups'),
    (40, N'Users'),
    (50, N'Courses'),
    (60, N'Students'),
    (70, N'CourseMaterials'),
    (80, N'Schedules'),
    (90, N'Enrollments'),
    (100, N'LessonMaterials'),
    (110, N'Attendances'),
    (120, N'LessonSubmissions'),
    (130, N'NewsPosts');

DECLARE @TableName sysname;
DECLARE @ColumnList nvarchar(max);
DECLARE @ValuesExpr nvarchar(max);
DECLARE @OrderByExpr nvarchar(max);
DECLARE @Sql nvarchar(max);
DECLARE @HasIdentity bit;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT TableName
FROM @OrderedTables
ORDER BY SortOrder;

OPEN cur;
FETCH NEXT FROM cur INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @ColumnList = NULL, @ValuesExpr = NULL, @OrderByExpr = NULL, @Sql = NULL, @HasIdentity = 0;

    SELECT @HasIdentity = CASE WHEN EXISTS (
        SELECT 1
        FROM sys.identity_columns ic
        WHERE ic.object_id = OBJECT_ID(N'[dbo].[' + @TableName + N']')
    ) THEN 1 ELSE 0 END;

    SELECT
        @ColumnList = STRING_AGG(QUOTENAME(c.name), ', ') WITHIN GROUP (ORDER BY c.column_id),
        @ValuesExpr = STRING_AGG(
            CASE
                WHEN ty.name IN (N'nvarchar', N'nchar', N'ntext') THEN
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE N'''''' + REPLACE(CONVERT(nvarchar(max), ' + QUOTENAME(c.name) + N'), '''''''', '''''''''''') + '''''''' END'
                WHEN ty.name IN (N'varchar', N'char', N'text') THEN
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE '''''''' + REPLACE(CONVERT(varchar(max), ' + QUOTENAME(c.name) + N'), '''''''', '''''''''''') + '''''''' END'
                WHEN ty.name IN (N'datetime', N'smalldatetime', N'datetime2', N'date', N'time', N'datetimeoffset') THEN
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE '''''''' + CONVERT(varchar(48), ' + QUOTENAME(c.name) + N', 126) + '''''''' END'
                WHEN ty.name = N'bit' THEN
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE CASE WHEN ' + QUOTENAME(c.name) + N' = 1 THEN ''1'' ELSE ''0'' END END'
                WHEN ty.name IN (N'uniqueidentifier') THEN
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE '''''''' + CONVERT(varchar(36), ' + QUOTENAME(c.name) + N') + '''''''' END'
                WHEN ty.name IN (N'binary', N'varbinary', N'image', N'rowversion', N'timestamp') THEN
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE CONVERT(varchar(max), ' + QUOTENAME(c.name) + N', 1) END'
                ELSE
                    N'CASE WHEN ' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE CONVERT(varchar(max), ' + QUOTENAME(c.name) + N') END'
            END,
            N' + '','' + '
        ) WITHIN GROUP (ORDER BY c.column_id)
    FROM sys.columns c
    JOIN sys.types ty ON c.user_type_id = ty.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[' + @TableName + N']')
      AND c.is_computed = 0;

    SELECT @OrderByExpr = STRING_AGG(QUOTENAME(c.name), ', ') WITHIN GROUP (ORDER BY ic.key_ordinal)
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'[dbo].[' + @TableName + N']')
      AND i.is_primary_key = 1;

    IF @OrderByExpr IS NULL
        SET @OrderByExpr = @ColumnList;

    SELECT '-- --------------------------------------------------';
    SELECT '-- Table: [dbo].[' + @TableName + ']';

    IF @HasIdentity = 1
        SELECT 'SET IDENTITY_INSERT [dbo].[' + @TableName + '] ON;';

    SET @Sql = N'SELECT ''INSERT INTO [dbo].[' + @TableName + N'] (' + @ColumnList + N') VALUES ('' + ' + @ValuesExpr + N' + '');'' FROM [dbo].[' + @TableName + N'] ORDER BY ' + @OrderByExpr + N';';
    EXEC sp_executesql @Sql;

    IF @HasIdentity = 1
        SELECT 'SET IDENTITY_INSERT [dbo].[' + @TableName + '] OFF;';

    SELECT 'GO';
    SELECT '';

    FETCH NEXT FROM cur INTO @TableName;
END

CLOSE cur;
DEALLOCATE cur;
