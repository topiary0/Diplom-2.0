-- =============================================================
-- Data seed script for current DiplomISPO contents
-- Generated automatically: 2026-05-13 00:41:55
-- =============================================================
USE [DiplomISPO];
GO

-- --------------------------------------------------
-- Table: [dbo].[CourseCategories]
SET IDENTITY_INSERT [dbo].[CourseCategories] ON;
INSERT INTO [dbo].[CourseCategories] ([Id], [Name], [Description]) VALUES (1, N'Программирование', N'Курсы по разработке ПО и алгоритмам');
INSERT INTO [dbo].[CourseCategories] ([Id], [Name], [Description]) VALUES (2, N'Веб-разработка', N'Frontend и backend для веб-приложений');
INSERT INTO [dbo].[CourseCategories] ([Id], [Name], [Description]) VALUES (3, N'Базы данных', N'Проектирование, SQL и администрирование БД');
SET IDENTITY_INSERT [dbo].[CourseCategories] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Teachers]
SET IDENTITY_INSERT [dbo].[Teachers] ON;
INSERT INTO [dbo].[Teachers] ([Id], [FullName], [Email], [Department], [PositionName], [CreatedAt]) VALUES (1, N'Иванова Мария Сергеевна', N'ivanova@ispo.local', N'Отделение ИТ', N'Преподаватель', '2026-04-13T20:33:14.4517886');
INSERT INTO [dbo].[Teachers] ([Id], [FullName], [Email], [Department], [PositionName], [CreatedAt]) VALUES (2, N'Петров Алексей Игоревич', N'petrov@ispo.local', N'Отделение ИТ', N'Старший преподаватель', '2026-04-13T20:33:14.4527860');
INSERT INTO [dbo].[Teachers] ([Id], [FullName], [Email], [Department], [PositionName], [CreatedAt]) VALUES (3, N'Смирнова Ольга Викторовна', N'smirnova@ispo.local', N'Центр ДПО', N'Методист', '2026-04-13T20:33:14.4527860');
INSERT INTO [dbo].[Teachers] ([Id], [FullName], [Email], [Department], [PositionName], [CreatedAt]) VALUES (4, N'Преподаватель ИСПО', N'teacher@ispo.local', N'Общее отделение', N'Преподаватель', '2026-04-16T17:39:29.9746527');
SET IDENTITY_INSERT [dbo].[Teachers] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[StudentGroups]
SET IDENTITY_INSERT [dbo].[StudentGroups] ON;
INSERT INTO [dbo].[StudentGroups] ([Id], [GroupCode], [Name], [Specialization], [StartYear]) VALUES (1, N'ИСПО-24-1', N'Группа ИСПО 24-1', N'Информационные системы и программирование', 2024);
INSERT INTO [dbo].[StudentGroups] ([Id], [GroupCode], [Name], [Specialization], [StartYear]) VALUES (2, N'ИСПО-24-2', N'Группа ИСПО 24-2', N'Информационные системы и программирование', 2024);
INSERT INTO [dbo].[StudentGroups] ([Id], [GroupCode], [Name], [Specialization], [StartYear]) VALUES (3, N'ИСПО-25-1', N'Группа ИСПО 25-1', N'Прикладная информатика', 2025);
INSERT INTO [dbo].[StudentGroups] ([Id], [GroupCode], [Name], [Specialization], [StartYear]) VALUES (4, N'ИСПО', N'2229090/1098', N'Програаммирование', 2026);
SET IDENTITY_INSERT [dbo].[StudentGroups] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Users]
SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (1, N'Системный администратор', N'admin@ispo.local', N'admin123', N'admin', 1, '2026-04-12T21:47:03.8579241');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (2, N'Преподаватель ИСПО', N'teacher@ispo.local', N'teacher123', N'teacher', 1, '2026-04-12T21:47:03.8968656');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (3, N'Студент ИСПО', N'student@ispo.local', N'student123', N'student', 1, '2026-04-12T21:47:03.8978810');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (4, N'Петров Алексей Игоревич', N'petrov@ispo.local', N'teacher123', N'teacher', 1, '2026-04-13T20:33:17.6709869');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (5, N'Орлов Влад Павлович', N'orlov.v@ispo.local', N'student123', N'student', 1, '2026-04-13T20:33:17.6719842');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (7, N'Иванова Мария Сергеевна', N'ivanova@ispo.local', N'teacher123', N'teacher', 1, '2026-04-16T17:08:39.0204647');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (8, N'Смирнова Ольга Викторовна', N'smirnova@ispo.local', N'teacher123', N'teacher', 1, '2026-04-16T17:08:39.0204647');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (11, N'Белова Кристина Юрьевна', N'belova.k@ispo.local', N'student123', N'student', 1, '2026-04-16T17:08:39.0709602');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (12, N'Никитин Максим Олегович', N'nikitin.m@ispo.local', N'student123', N'student', 1, '2026-04-16T17:08:39.0709602');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (13, N'Румянцев Иван Николаевич', N'podawanrew9@gmail.com', N'student123', N'student', 1, '2026-04-16T17:08:39.0709602');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (17, N'пыпыпы', N'4@ispo.local', N'111111', N'student', 1, '2026-04-16T20:58:31.7940726');
INSERT INTO [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt]) VALUES (18, N'Рогов Владислав Максимович', N'rogov@ispo.local', N'123456', N'student', 1, '2026-05-04T18:29:53.4128791');
SET IDENTITY_INSERT [dbo].[Users] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Courses]
SET IDENTITY_INSERT [dbo].[Courses] ON;
INSERT INTO [dbo].[Courses] ([Id], [CourseCode], [Title], [Description], [DurationHours], [CategoryId], [TeacherId], [IsActive], [CreatedAt]) VALUES (1, N'CSHARP-START', N'Основы C#', N'Базовый курс по языку C# и ООП', 72, 1, 1, 1, '2026-04-13T20:33:14.4537841');
INSERT INTO [dbo].[Courses] ([Id], [CourseCode], [Title], [Description], [DurationHours], [CategoryId], [TeacherId], [IsActive], [CreatedAt]) VALUES (2, N'WEB-PRO', N'Веб-приложения на ASP.NET Core', N'Практика разработки MVC-приложений', 96, 2, 2, 1, '2026-04-13T20:33:14.4547810');
INSERT INTO [dbo].[Courses] ([Id], [CourseCode], [Title], [Description], [DurationHours], [CategoryId], [TeacherId], [IsActive], [CreatedAt]) VALUES (3, N'SQL-DBA', N'SQL и проектирование БД', N'Нормализация, индексы, представления, процедуры', 64, 3, 3, 1, '2026-04-13T20:33:14.4547810');
SET IDENTITY_INSERT [dbo].[Courses] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Students]
SET IDENTITY_INSERT [dbo].[Students] ON;
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (1, N'Орлов', N'Влад', N'Павлович', '2006-03-11T00:00:00.0000000', N'orlov.v@ispo.local', N'+7-900-111-22-33', 1, '2026-04-13T20:33:14.4527860');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (2, N'Егорова', N'Анна', N'Романовна', '2006-06-19T00:00:00.0000000', N'egorova.a@ispo.local', N'+7-900-111-44-55', 1, '2026-04-13T20:33:14.4527860');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (3, N'Волков', N'Даниил', N'Андреевич', '2005-12-05T00:00:00.0000000', N'volkov.d@ispo.local', N'+7-900-222-33-44', 2, '2026-04-13T20:33:14.4527860');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (4, N'Белова', N'Кристина', N'Юрьевна', '2007-01-21T00:00:00.0000000', N'belova.k@ispo.local', N'+7-900-333-55-66', 3, '2026-04-13T20:33:14.4537841');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (5, N'Никитин', N'Максим', N'Олегович', '2006-08-30T00:00:00.0000000', N'nikitin.m@ispo.local', N'+7-900-444-66-77', 2, '2026-04-13T20:33:14.4537841');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (6, N'Румянцев', N'Иван', N'Николаевич', '2008-04-14T00:00:00.0000000', N'podawanrew9@gmail.com', N'+79188821525', 2, '2026-04-13T22:27:53.6753341');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (7, N'Студент', N'ИСПО', NULL, '2008-04-16T00:00:00.0000000', N'student@ispo.local', NULL, 1, '2026-04-16T17:33:43.8311181');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (8, N'Рогов', N'Владислав', N'Максимович', '2008-04-16T00:00:00.0000000', N'rogov@ispo.local', N'+79777777777', 4, '2026-04-16T17:33:43.8311181');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (9, N'Панченко', N'Михаил', N'Станиславович', '2008-04-16T00:00:00.0000000', N'panch@ispo.local', NULL, 1, '2026-04-16T18:22:37.9135931');
INSERT INTO [dbo].[Students] ([Id], [LastName], [FirstName], [MiddleName], [BirthDate], [Email], [Phone], [GroupId], [CreatedAt]) VALUES (10, N'пыпыпы', N'Новый', NULL, '2008-04-16T00:00:00.0000000', N'4@ispo.local', NULL, 4, '2026-04-16T20:18:49.0187001');
SET IDENTITY_INSERT [dbo].[Students] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[CourseMaterials]
SET IDENTITY_INSERT [dbo].[CourseMaterials] ON;
INSERT INTO [dbo].[CourseMaterials] ([Id], [CourseId], [Title], [MaterialType], [FileUrl], [SortOrder]) VALUES (1, 1, N'Синтаксис C#', N'lecture', N'https://example.local/csharp/syntax.pdf', 1);
INSERT INTO [dbo].[CourseMaterials] ([Id], [CourseId], [Title], [MaterialType], [FileUrl], [SortOrder]) VALUES (2, 2, N'Архитектура MVC', N'lecture', N'https://example.local/web/mvc.pdf', 1);
INSERT INTO [dbo].[CourseMaterials] ([Id], [CourseId], [Title], [MaterialType], [FileUrl], [SortOrder]) VALUES (3, 3, N'Нормализация БД', N'lecture', N'https://example.local/sql/normalization.pdf', 1);
SET IDENTITY_INSERT [dbo].[CourseMaterials] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Schedules]
SET IDENTITY_INSERT [dbo].[Schedules] ON;
INSERT INTO [dbo].[Schedules] ([Id], [CourseId], [GroupId], [LessonDate], [StartTime], [EndTime], [Room], [LessonTopic], [ConferenceUrl], [LiveStatus], [LiveStartedAt], [LiveEndedAt]) VALUES (1, 1, 1, '2026-04-15T00:00:00.0000000', '10:00:00', '11:30:00', N'А-301', N'Типы данных и операторы', N'https://meet.google.com/abc-defg-hij', N'live', '2026-05-12T16:37:56.3317064', NULL);
INSERT INTO [dbo].[Schedules] ([Id], [CourseId], [GroupId], [LessonDate], [StartTime], [EndTime], [Room], [LessonTopic], [ConferenceUrl], [LiveStatus], [LiveStartedAt], [LiveEndedAt]) VALUES (2, 2, 2, '2026-04-16T00:00:00.0000000', '12:00:00', '13:30:00', N'Б-204', N'Razor и маршрутизация', NULL, N'planned', NULL, NULL);
INSERT INTO [dbo].[Schedules] ([Id], [CourseId], [GroupId], [LessonDate], [StartTime], [EndTime], [Room], [LessonTopic], [ConferenceUrl], [LiveStatus], [LiveStartedAt], [LiveEndedAt]) VALUES (3, 3, 3, '2026-04-17T00:00:00.0000000', '09:30:00', '11:00:00', N'В-105', N'JOIN, GROUP BY, HAVING', N'https://localhost:61879/Teacher/Lessons', N'finished', '2026-04-16T20:22:11.9930988', '2026-04-16T21:01:10.6283840');
INSERT INTO [dbo].[Schedules] ([Id], [CourseId], [GroupId], [LessonDate], [StartTime], [EndTime], [Room], [LessonTopic], [ConferenceUrl], [LiveStatus], [LiveStartedAt], [LiveEndedAt]) VALUES (4, 1, 4, '2026-04-16T00:00:00.0000000', '09:00:00', '10:30:00', N'322', N'Программирование', N'https://www.youtube.com/watch?v=Zv5rFQK7wfc', N'live', '2026-05-12T16:37:54.9846234', NULL);
INSERT INTO [dbo].[Schedules] ([Id], [CourseId], [GroupId], [LessonDate], [StartTime], [EndTime], [Room], [LessonTopic], [ConferenceUrl], [LiveStatus], [LiveStartedAt], [LiveEndedAt]) VALUES (5, 1, 1, '2026-04-27T00:00:00.0000000', '09:00:00', '10:30:00', N'321', N'пав', N'https://www.youtube.com/watch?v=Zv5rFQK7wfc', N'finished', '2026-05-12T16:28:21.1970882', '2026-05-12T16:37:53.4644224');
SET IDENTITY_INSERT [dbo].[Schedules] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Enrollments]
SET IDENTITY_INSERT [dbo].[Enrollments] ON;
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (1, 1, 1, '2026-04-13T20:33:14.5046484', N'active');
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (2, 2, 2, '2026-04-13T20:33:14.5046484', N'active');
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (3, 3, 3, '2026-04-13T20:33:14.5046484', N'active');
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (4, 4, 1, '2026-04-13T20:33:14.5046484', N'active');
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (5, 5, 2, '2026-04-13T20:33:14.5056451', N'active');
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (6, 6, 1, '2026-04-16T20:37:03.7345075', N'active');
INSERT INTO [dbo].[Enrollments] ([Id], [StudentId], [CourseId], [EnrolledAt], [Status]) VALUES (7, 5, 1, '2026-04-16T20:57:39.3172200', N'active');
SET IDENTITY_INSERT [dbo].[Enrollments] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[LessonMaterials]
SET IDENTITY_INSERT [dbo].[LessonMaterials] ON;
INSERT INTO [dbo].[LessonMaterials] ([Id], [ScheduleId], [Title], [MaterialType], [CloudUrl], [SortOrder], [CreatedAt]) VALUES (5, 1, N'Практ задание', N'task', N'https://localhost:61879/Teacher/Lessons', 1, '2026-04-26T22:57:36.1490222');
SET IDENTITY_INSERT [dbo].[LessonMaterials] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[Attendances]
SET IDENTITY_INSERT [dbo].[Attendances] ON;
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (1, 1, 1, 1, '2026-04-26T20:29:01.6970486');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (2, 2, 2, 1, '2026-04-13T20:33:14.5973997');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (3, 3, 3, 0, '2026-04-13T20:33:14.5973997');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (4, 4, 10, 1, '2026-04-17T20:07:51.3042280');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (5, 1, 2, 0, '2026-04-26T20:29:01.6972201');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (6, 1, 7, 1, '2026-04-26T20:29:01.6999242');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (7, 1, 8, 0, '2026-04-26T20:29:01.7000023');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (8, 1, 9, 0, '2026-04-26T20:29:01.7000549');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (9, 5, 1, 1, '2026-05-04T18:23:49.8305840');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (10, 5, 2, 0, '2026-05-04T18:23:49.8306402');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (11, 5, 7, 0, '2026-05-04T18:23:49.8306404');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (12, 5, 8, 1, '2026-05-04T18:23:49.8306407');
INSERT INTO [dbo].[Attendances] ([Id], [ScheduleId], [StudentId], [IsPresent], [MarkedAt]) VALUES (13, 5, 9, 1, '2026-05-04T18:23:49.8306408');
SET IDENTITY_INSERT [dbo].[Attendances] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[LessonSubmissions]
SET IDENTITY_INSERT [dbo].[LessonSubmissions] ON;
INSERT INTO [dbo].[LessonSubmissions] ([Id], [ScheduleId], [StudentId], [CloudFileUrl], [StudentComment], [Status], [Score], [TeacherComment], [RevisionNumber], [SubmittedAt], [ReviewedAt]) VALUES (1, 1, 7, N'https://www.youtube.com/watch?v=Zv5rFQK7wfc', N'укуаы', N'accepted', 5.00, NULL, 3, '2026-04-16T20:42:58.0088874', '2026-04-16T21:01:33.3560193');
INSERT INTO [dbo].[LessonSubmissions] ([Id], [ScheduleId], [StudentId], [CloudFileUrl], [StudentComment], [Status], [Score], [TeacherComment], [RevisionNumber], [SubmittedAt], [ReviewedAt]) VALUES (2, 1, 1, N'https://localhost:61879/Student/Submissions', N'нкенке', N'accepted', 3.00, N'пв', 3, '2026-04-26T22:46:16.1996432', '2026-04-26T22:46:26.0861231');
INSERT INTO [dbo].[LessonSubmissions] ([Id], [ScheduleId], [StudentId], [CloudFileUrl], [StudentComment], [Status], [Score], [TeacherComment], [RevisionNumber], [SubmittedAt], [ReviewedAt]) VALUES (3, 5, 1, N'https://l', N'блббддб', N'accepted', 5.00, N'крос', 2, '2026-05-04T18:23:26.9367834', '2026-05-04T18:24:17.0731349');
SET IDENTITY_INSERT [dbo].[LessonSubmissions] OFF;
GO

-- --------------------------------------------------
-- Table: [dbo].[NewsPosts]
SET IDENTITY_INSERT [dbo].[NewsPosts] ON;
INSERT INTO [dbo].[NewsPosts] ([Id], [Body], [ImageUrl], [CreatedBy], [CreatedAt]) VALUES (36, N'смсчм', NULL, N'Системный администратор', '2026-05-04T17:33:12.0000000');
SET IDENTITY_INSERT [dbo].[NewsPosts] OFF;
GO

