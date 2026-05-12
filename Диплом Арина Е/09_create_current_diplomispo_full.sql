-- =============================================================
-- Текущая структурная выгрузка БД DiplomISPO (schema only)
-- Сервер: DESKTOP-K3TACJO\SQLEXPRESS01
-- База: DiplomISPO
-- Дата: 2026-05-04 23:14:46
-- Сгенерировано автоматически (SMO Transfer)
-- =============================================================

IF DB_ID(N'DiplomISPO') IS NULL
BEGIN
    CREATE DATABASE [DiplomISPO];
END
GO
USE [DiplomISPO];
GO

/****** Object:  Table [dbo].[Teachers]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Teachers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](180) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Email] [nvarchar](120) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Department] [nvarchar](150) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[PositionName] [nvarchar](120) COLLATE Cyrillic_General_CI_AS NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Courses]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Courses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CourseCode] [nvarchar](40) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Title] [nvarchar](200) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE Cyrillic_General_CI_AS NULL,
	[DurationHours] [int] NOT NULL,
	[CategoryId] [int] NOT NULL,
	[TeacherId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[CourseCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Enrollments]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Enrollments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StudentId] [int] NOT NULL,
	[CourseId] [int] NOT NULL,
	[EnrolledAt] [datetime2](7) NOT NULL,
	[Status] [nvarchar](30) COLLATE Cyrillic_General_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Enrollments_StudentCourse] UNIQUE NONCLUSTERED 
(
	[StudentId] ASC,
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  View [dbo].[vw_CourseEnrollment]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

-- 6) Представления (Views)
GO

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

/****** Object:  Table [dbo].[StudentGroups]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[StudentGroups](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GroupCode] [nvarchar](30) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Name] [nvarchar](120) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Specialization] [nvarchar](150) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[StartYear] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[GroupCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Schedules]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Schedules](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CourseId] [int] NOT NULL,
	[GroupId] [int] NOT NULL,
	[LessonDate] [date] NOT NULL,
	[StartTime] [time](7) NOT NULL,
	[EndTime] [time](7) NOT NULL,
	[Room] [nvarchar](50) COLLATE Cyrillic_General_CI_AS NULL,
	[LessonTopic] [nvarchar](250) COLLATE Cyrillic_General_CI_AS NULL,
	[ConferenceUrl] [nvarchar](1000) COLLATE Cyrillic_General_CI_AS NULL,
	[LiveStatus] [nvarchar](20) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[LiveStartedAt] [datetime2](7) NULL,
	[LiveEndedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  View [dbo].[vw_ScheduleByGroup]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

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

/****** Object:  Table [dbo].[Students]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Students](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LastName] [nvarchar](80) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[FirstName] [nvarchar](80) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[MiddleName] [nvarchar](80) COLLATE Cyrillic_General_CI_AS NULL,
	[BirthDate] [date] NOT NULL,
	[Email] [nvarchar](120) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Phone] [nvarchar](30) COLLATE Cyrillic_General_CI_AS NULL,
	[GroupId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Attendances]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Attendances](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ScheduleId] [int] NOT NULL,
	[StudentId] [int] NOT NULL,
	[IsPresent] [bit] NOT NULL,
	[MarkedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Attendance_Unique] UNIQUE NONCLUSTERED 
(
	[ScheduleId] ASC,
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[LessonSubmissions]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[LessonSubmissions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ScheduleId] [int] NOT NULL,
	[StudentId] [int] NOT NULL,
	[CloudFileUrl] [nvarchar](1000) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[StudentComment] [nvarchar](2000) COLLATE Cyrillic_General_CI_AS NULL,
	[Status] [nvarchar](30) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Score] [decimal](4, 2) NULL,
	[TeacherComment] [nvarchar](2000) COLLATE Cyrillic_General_CI_AS NULL,
	[RevisionNumber] [int] NOT NULL,
	[SubmittedAt] [datetime2](7) NOT NULL,
	[ReviewedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_LessonSubmissions_ScheduleStudent] UNIQUE NONCLUSTERED 
(
	[ScheduleId] ASC,
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  View [dbo].[vw_StudentProgress]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

GO

            CREATE VIEW dbo.vw_StudentProgress
            AS
            SELECT
                s.Id AS StudentId,
                CONCAT(s.LastName, N' ', s.FirstName, COALESCE(N' ' + s.MiddleName, N'')) AS StudentName,
                c.Title AS CourseTitle,
                (
                    SELECT AVG(CAST(ls.Score AS DECIMAL(4,2)))
                    FROM dbo.LessonSubmissions ls
                    JOIN dbo.Schedules sc ON sc.Id = ls.ScheduleId
                    WHERE ls.StudentId = s.Id
                      AND sc.CourseId = c.Id
                      AND ls.Status = N'accepted'
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
        
GO

/****** Object:  Table [dbo].[CourseCategories]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[CourseCategories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](120) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Description] [nvarchar](500) COLLATE Cyrillic_General_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[CourseMaterials]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[CourseMaterials](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CourseId] [int] NOT NULL,
	[Title] [nvarchar](200) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[MaterialType] [nvarchar](40) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[FileUrl] [nvarchar](500) COLLATE Cyrillic_General_CI_AS NULL,
	[SortOrder] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[LessonMaterials]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[LessonMaterials](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ScheduleId] [int] NOT NULL,
	[Title] [nvarchar](200) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[MaterialType] [nvarchar](40) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[CloudUrl] [nvarchar](1000) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[SortOrder] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[NewsPosts]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[NewsPosts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Body] [nvarchar](4000) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[ImageUrl] [nvarchar](500) COLLATE Cyrillic_General_CI_AS NULL,
	[CreatedBy] [nvarchar](180) COLLATE Cyrillic_General_CI_AS NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_NewsPosts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[Users]    Script Date: 04.05.2026 23:14:46 ******/
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](150) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Email] [nvarchar](120) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[PasswordHash] [nvarchar](255) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[Role] [nvarchar](30) COLLATE Cyrillic_General_CI_AS NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Index [IX_Courses_CategoryId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_Courses_CategoryId] ON [dbo].[Courses]
(
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_Courses_TeacherId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_Courses_TeacherId] ON [dbo].[Courses]
(
	[TeacherId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_Enrollments_CourseId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_Enrollments_CourseId] ON [dbo].[Enrollments]
(
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_Enrollments_StudentId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_Enrollments_StudentId] ON [dbo].[Enrollments]
(
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_LessonMaterials_ScheduleId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_LessonMaterials_ScheduleId] ON [dbo].[LessonMaterials]
(
	[ScheduleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON

GO

/****** Object:  Index [IX_LessonSubmissions_Status]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_LessonSubmissions_Status] ON [dbo].[LessonSubmissions]
(
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_LessonSubmissions_StudentId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_LessonSubmissions_StudentId] ON [dbo].[LessonSubmissions]
(
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_NewsPosts_CreatedAt]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_NewsPosts_CreatedAt] ON [dbo].[NewsPosts]
(
	[CreatedAt] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_Schedules_LessonDate]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_Schedules_LessonDate] ON [dbo].[Schedules]
(
	[LessonDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

/****** Object:  Index [IX_Students_GroupId]    Script Date: 04.05.2026 23:14:46 ******/
CREATE NONCLUSTERED INDEX [IX_Students_GroupId] ON [dbo].[Students]
(
	[GroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Attendances] ADD  DEFAULT (sysutcdatetime()) FOR [MarkedAt]
ALTER TABLE [dbo].[CourseMaterials] ADD  DEFAULT ((0)) FOR [SortOrder]
ALTER TABLE [dbo].[Courses] ADD  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [dbo].[Courses] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[Enrollments] ADD  DEFAULT (sysutcdatetime()) FOR [EnrolledAt]
ALTER TABLE [dbo].[Enrollments] ADD  DEFAULT (N'active') FOR [Status]
ALTER TABLE [dbo].[LessonMaterials] ADD  DEFAULT ((0)) FOR [SortOrder]
ALTER TABLE [dbo].[LessonMaterials] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[LessonSubmissions] ADD  DEFAULT (N'new') FOR [Status]
ALTER TABLE [dbo].[LessonSubmissions] ADD  DEFAULT ((1)) FOR [RevisionNumber]
ALTER TABLE [dbo].[LessonSubmissions] ADD  DEFAULT (sysutcdatetime()) FOR [SubmittedAt]
ALTER TABLE [dbo].[NewsPosts] ADD  CONSTRAINT [DF_NewsPosts_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[Schedules] ADD  CONSTRAINT [DF_Schedules_LiveStatus]  DEFAULT (N'planned') FOR [LiveStatus]
ALTER TABLE [dbo].[Students] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[Teachers] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [dbo].[Users] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
ALTER TABLE [dbo].[Attendances]  WITH CHECK ADD  CONSTRAINT [FK_Attendances_Schedules] FOREIGN KEY([ScheduleId])
REFERENCES [dbo].[Schedules] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Attendances] CHECK CONSTRAINT [FK_Attendances_Schedules]
ALTER TABLE [dbo].[Attendances]  WITH CHECK ADD  CONSTRAINT [FK_Attendances_Students] FOREIGN KEY([StudentId])
REFERENCES [dbo].[Students] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Attendances] CHECK CONSTRAINT [FK_Attendances_Students]
ALTER TABLE [dbo].[CourseMaterials]  WITH CHECK ADD  CONSTRAINT [FK_CourseMaterials_Courses] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[CourseMaterials] CHECK CONSTRAINT [FK_CourseMaterials_Courses]
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD  CONSTRAINT [FK_Courses_Categories] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[CourseCategories] ([Id])
ALTER TABLE [dbo].[Courses] CHECK CONSTRAINT [FK_Courses_Categories]
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD  CONSTRAINT [FK_Courses_Teachers] FOREIGN KEY([TeacherId])
REFERENCES [dbo].[Teachers] ([Id])
ALTER TABLE [dbo].[Courses] CHECK CONSTRAINT [FK_Courses_Teachers]
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_Courses] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_Courses]
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_Students] FOREIGN KEY([StudentId])
REFERENCES [dbo].[Students] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_Students]
ALTER TABLE [dbo].[LessonMaterials]  WITH CHECK ADD  CONSTRAINT [FK_LessonMaterials_Schedules] FOREIGN KEY([ScheduleId])
REFERENCES [dbo].[Schedules] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[LessonMaterials] CHECK CONSTRAINT [FK_LessonMaterials_Schedules]
ALTER TABLE [dbo].[LessonSubmissions]  WITH CHECK ADD  CONSTRAINT [FK_LessonSubmissions_Schedules] FOREIGN KEY([ScheduleId])
REFERENCES [dbo].[Schedules] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[LessonSubmissions] CHECK CONSTRAINT [FK_LessonSubmissions_Schedules]
ALTER TABLE [dbo].[LessonSubmissions]  WITH CHECK ADD  CONSTRAINT [FK_LessonSubmissions_Students] FOREIGN KEY([StudentId])
REFERENCES [dbo].[Students] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[LessonSubmissions] CHECK CONSTRAINT [FK_LessonSubmissions_Students]
ALTER TABLE [dbo].[Schedules]  WITH CHECK ADD  CONSTRAINT [FK_Schedules_Courses] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Schedules] CHECK CONSTRAINT [FK_Schedules_Courses]
ALTER TABLE [dbo].[Schedules]  WITH CHECK ADD  CONSTRAINT [FK_Schedules_Groups] FOREIGN KEY([GroupId])
REFERENCES [dbo].[StudentGroups] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Schedules] CHECK CONSTRAINT [FK_Schedules_Groups]
ALTER TABLE [dbo].[Students]  WITH CHECK ADD  CONSTRAINT [FK_Students_StudentGroups] FOREIGN KEY([GroupId])
REFERENCES [dbo].[StudentGroups] ([Id])
ALTER TABLE [dbo].[Students] CHECK CONSTRAINT [FK_Students_StudentGroups]
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD CHECK  (([DurationHours]>(0)))
ALTER TABLE [dbo].[LessonMaterials]  WITH CHECK ADD  CONSTRAINT [CK_LessonMaterials_CloudUrl_Https] CHECK  (([CloudUrl] like N'https://%'))
ALTER TABLE [dbo].[LessonMaterials] CHECK CONSTRAINT [CK_LessonMaterials_CloudUrl_Https]
ALTER TABLE [dbo].[LessonMaterials]  WITH CHECK ADD  CONSTRAINT [CK_LessonMaterials_MaterialType] CHECK  (([MaterialType]=N'reference' OR [MaterialType]=N'methodic' OR [MaterialType]=N'task'))
ALTER TABLE [dbo].[LessonMaterials] CHECK CONSTRAINT [CK_LessonMaterials_MaterialType]
ALTER TABLE [dbo].[LessonSubmissions]  WITH CHECK ADD  CONSTRAINT [CK_LessonSubmissions_CloudFileUrl_Https] CHECK  (([CloudFileUrl] like N'https://%'))
ALTER TABLE [dbo].[LessonSubmissions] CHECK CONSTRAINT [CK_LessonSubmissions_CloudFileUrl_Https]
ALTER TABLE [dbo].[LessonSubmissions]  WITH CHECK ADD  CONSTRAINT [CK_LessonSubmissions_Revision] CHECK  (([RevisionNumber]>=(1)))
ALTER TABLE [dbo].[LessonSubmissions] CHECK CONSTRAINT [CK_LessonSubmissions_Revision]
ALTER TABLE [dbo].[LessonSubmissions]  WITH CHECK ADD  CONSTRAINT [CK_LessonSubmissions_Score_5] CHECK  (([Score] IS NULL OR [Score]>=(0) AND [Score]<=(5)))
ALTER TABLE [dbo].[LessonSubmissions] CHECK CONSTRAINT [CK_LessonSubmissions_Score_5]
ALTER TABLE [dbo].[LessonSubmissions]  WITH CHECK ADD  CONSTRAINT [CK_LessonSubmissions_Status] CHECK  (([Status]=N'accepted' OR [Status]=N'revision' OR [Status]=N'in_review' OR [Status]=N'new'))
ALTER TABLE [dbo].[LessonSubmissions] CHECK CONSTRAINT [CK_LessonSubmissions_Status]
ALTER TABLE [dbo].[Schedules]  WITH CHECK ADD  CONSTRAINT [CK_Schedules_ConferenceUrl_Https] CHECK  (([ConferenceUrl] IS NULL OR [ConferenceUrl] like N'https://%'))
ALTER TABLE [dbo].[Schedules] CHECK CONSTRAINT [CK_Schedules_ConferenceUrl_Https]
ALTER TABLE [dbo].[Schedules]  WITH CHECK ADD  CONSTRAINT [CK_Schedules_LiveStatus] CHECK  (([LiveStatus]=N'finished' OR [LiveStatus]=N'live' OR [LiveStatus]=N'planned'))
ALTER TABLE [dbo].[Schedules] CHECK CONSTRAINT [CK_Schedules_LiveStatus]
ALTER TABLE [dbo].[Schedules]  WITH CHECK ADD  CONSTRAINT [CK_Schedules_Time] CHECK  (([EndTime]>[StartTime]))
ALTER TABLE [dbo].[Schedules] CHECK CONSTRAINT [CK_Schedules_Time]
ALTER TABLE [dbo].[StudentGroups]  WITH CHECK ADD CHECK  (([StartYear]>=(2000) AND [StartYear]<=(2100)))
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [CK_Users_Role] CHECK  (([Role]=N'student' OR [Role]=N'teacher' OR [Role]=N'admin'))
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [CK_Users_Role]

GO
