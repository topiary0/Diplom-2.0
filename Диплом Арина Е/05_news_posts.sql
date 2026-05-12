USE [DiplomISPO];
GO

IF OBJECT_ID(N'dbo.NewsPosts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NewsPosts
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NewsPosts PRIMARY KEY,
        Body NVARCHAR(4000) NOT NULL,
        ImageUrl NVARCHAR(500) NULL,
        CreatedBy NVARCHAR(180) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NewsPosts_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.NewsPosts')
      AND name = N'IX_NewsPosts_CreatedAt'
)
BEGIN
    CREATE INDEX IX_NewsPosts_CreatedAt ON dbo.NewsPosts (CreatedAt DESC);
END
GO
