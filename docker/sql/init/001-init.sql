SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'PlanWriterDb') IS NULL
BEGIN
    CREATE DATABASE [PlanWriterDb];
END
GO

USE [PlanWriterDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        FirstName NVARCHAR(200) NOT NULL,
        LastName NVARCHAR(200) NOT NULL,
        DateOfBirth DATETIME2 NULL,
        Email NVARCHAR(320) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Bio NVARCHAR(MAX) NULL,
        AvatarUrl NVARCHAR(MAX) NULL,
        IsProfilePublic BIT NOT NULL CONSTRAINT DF_Users_IsProfilePublic DEFAULT (0),
        Slug NVARCHAR(200) NULL,
        DisplayName NVARCHAR(200) NULL,
        IsAdmin BIT NOT NULL CONSTRAINT DF_Users_IsAdmin DEFAULT (0),
        MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT (0),
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Users_Email'
      AND object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Users_Slug'
      AND object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    CREATE UNIQUE INDEX UX_Users_Slug ON dbo.Users (Slug)
    WHERE Slug IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(200) NOT NULL,
        [Type] INT NOT NULL,
        StartsAtUtc DATETIME2 NOT NULL,
        EndsAtUtc DATETIME2 NOT NULL,
        DefaultTargetWords INT NULL,
        ValidationWindowStartsAtUtc DATETIME2 NULL,
        ValidationWindowEndsAtUtc DATETIME2 NULL,
        AllowedValidationSources NVARCHAR(100) NOT NULL
            CONSTRAINT DF_Events_AllowedValidationSources DEFAULT (N'current,paste,manual'),
        IsActive BIT NOT NULL CONSTRAINT DF_Events_IsActive DEFAULT (1),
        CONSTRAINT PK_Events PRIMARY KEY (Id)
    );
END
GO

IF COL_LENGTH(N'dbo.Events', N'ValidationWindowStartsAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Events
        ADD ValidationWindowStartsAtUtc DATETIME2 NULL;
END
GO

IF COL_LENGTH(N'dbo.Events', N'ValidationWindowEndsAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Events
        ADD ValidationWindowEndsAtUtc DATETIME2 NULL;
END
GO

IF COL_LENGTH(N'dbo.Events', N'AllowedValidationSources') IS NULL
BEGIN
    ALTER TABLE dbo.Events
        ADD AllowedValidationSources NVARCHAR(100) NULL;

    EXEC(N'
        UPDATE dbo.Events
        SET AllowedValidationSources = N''current,paste,manual''
        WHERE AllowedValidationSources IS NULL;
    ');

    EXEC(N'
        ALTER TABLE dbo.Events
            ALTER COLUMN AllowedValidationSources NVARCHAR(100) NOT NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.EventWordWars', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventWordWars
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        EventId UNIQUEIDENTIFIER NOT NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        DurationInMinutes INT NOT NULL,
        StartAtUtc DATETIME2 NOT NULL,
        EndAtUtc DATETIME2 NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_EventWordWars_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        FinishedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_EventWordWars PRIMARY KEY (Id),
        CONSTRAINT FK_EventWordWars_Events_EventId FOREIGN KEY (EventId)
            REFERENCES dbo.Events (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_EventWordWars_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId)
            REFERENCES dbo.Users (Id)
            ON DELETE NO ACTION,
        CONSTRAINT CK_EventWordWars_Status
            CHECK (Status IN (N'Waiting', N'Running', N'Finished'))
    );
END
GO

IF OBJECT_ID(N'dbo.EventWordWars', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.EventWordWars', N'StartAtUtc') IS NULL
       AND COL_LENGTH(N'dbo.EventWordWars', N'StartsAtUtc') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.EventWordWars
            ADD StartAtUtc DATETIME2 NULL;

        UPDATE dbo.EventWordWars
        SET StartAtUtc = StartsAtUtc
        WHERE StartAtUtc IS NULL;

        ALTER TABLE dbo.EventWordWars
            ALTER COLUMN StartAtUtc DATETIME2 NOT NULL;
    END

    IF COL_LENGTH(N'dbo.EventWordWars', N'EndAtUtc') IS NULL
       AND COL_LENGTH(N'dbo.EventWordWars', N'EndsAtUtc') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.EventWordWars
            ADD EndAtUtc DATETIME2 NULL;

        UPDATE dbo.EventWordWars
        SET EndAtUtc = EndsAtUtc
        WHERE EndAtUtc IS NULL;

        ALTER TABLE dbo.EventWordWars
            ALTER COLUMN EndAtUtc DATETIME2 NOT NULL;
    END
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_EventWordWars_EventId_Status_CreatedAtUtc'
      AND object_id = OBJECT_ID(N'dbo.EventWordWars')
)
BEGIN
    CREATE INDEX IX_EventWordWars_EventId_Status_CreatedAtUtc
        ON dbo.EventWordWars (EventId, Status, CreatedAtUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.EventWordWarParticipants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventWordWarParticipants
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        WordWarId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        JoinedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_EventWordWarParticipants_JoinedAtUtc DEFAULT (SYSUTCDATETIME()),
        WordsInRound INT NOT NULL
            CONSTRAINT DF_EventWordWarParticipants_WordsInRound DEFAULT (0),
        LastCheckpointAtUtc DATETIME2 NULL,
        FinalRank INT NULL,
        CONSTRAINT PK_EventWordWarParticipants PRIMARY KEY (Id),
        CONSTRAINT FK_EventWordWarParticipants_EventWordWars_WordWarId FOREIGN KEY (WordWarId)
            REFERENCES dbo.EventWordWars (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_EventWordWarParticipants_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id)
            ON DELETE NO ACTION,
        CONSTRAINT FK_EventWordWarParticipants_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE NO ACTION,
        CONSTRAINT CK_EventWordWarParticipants_WordsInRound_NonNegative
            CHECK (WordsInRound >= 0)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_EventWordWarParticipants_WordWarId_UserId'
      AND object_id = OBJECT_ID(N'dbo.EventWordWarParticipants')
)
BEGIN
    CREATE UNIQUE INDEX UX_EventWordWarParticipants_WordWarId_UserId
        ON dbo.EventWordWarParticipants (WordWarId, UserId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_EventWordWarParticipants_WordWarId_WordsInRound'
      AND object_id = OBJECT_ID(N'dbo.EventWordWarParticipants')
)
BEGIN
    CREATE INDEX IX_EventWordWarParticipants_WordWarId_WordsInRound
        ON dbo.EventWordWarParticipants (WordWarId, WordsInRound DESC, LastCheckpointAtUtc ASC, JoinedAtUtc ASC);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Events')
      AND c.name = N'AllowedValidationSources'
)
BEGIN
    ALTER TABLE dbo.Events
        ADD CONSTRAINT DF_Events_AllowedValidationSources
            DEFAULT (N'current,paste,manual') FOR AllowedValidationSources;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Events_Slug'
      AND object_id = OBJECT_ID(N'dbo.Events')
)
BEGIN
    CREATE UNIQUE INDEX UX_Events_Slug ON dbo.Events (Slug);
END
GO

IF OBJECT_ID(N'dbo.Projects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Projects
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(300) NULL,
        Description NVARCHAR(MAX) NULL,
        Genre NVARCHAR(120) NULL,
        WordCountGoal INT NULL,
        GoalAmount INT NOT NULL CONSTRAINT DF_Projects_GoalAmount DEFAULT (0),
        GoalUnit INT NOT NULL CONSTRAINT DF_Projects_GoalUnit DEFAULT (0),
        StartDate DATETIME2 NOT NULL,
        Deadline DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL,
        CurrentWordCount INT NOT NULL CONSTRAINT DF_Projects_CurrentWordCount DEFAULT (0),
        CoverBytes VARBINARY(MAX) NULL,
        CoverMime NVARCHAR(100) NULL,
        CoverSize INT NULL,
        CoverUpdatedAt DATETIME2 NULL,
        IsPublic BIT NOT NULL CONSTRAINT DF_Projects_IsPublic DEFAULT (0),
        ValidatedWords INT NULL,
        ValidatedAtUtc DATETIME2 NULL,
        ValidationPassed BIT NULL,
        CONSTRAINT PK_Projects PRIMARY KEY (Id),
        CONSTRAINT FK_Projects_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Projects_UserId'
      AND object_id = OBJECT_ID(N'dbo.Projects')
)
BEGIN
    CREATE INDEX IX_Projects_UserId ON dbo.Projects (UserId);
END
GO

IF OBJECT_ID(N'dbo.EventValidationAudits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventValidationAudits
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        EventId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Source NVARCHAR(50) NOT NULL,
        SubmittedWords INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        ValidatedAtUtc DATETIME2 NULL,
        Reason NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_EventValidationAudits_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_EventValidationAudits PRIMARY KEY (Id),
        CONSTRAINT FK_EventValidationAudits_Events_EventId FOREIGN KEY (EventId)
            REFERENCES dbo.Events (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_EventValidationAudits_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_EventValidationAudits_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id)
            ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_EventValidationAudits_Event_Project_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.EventValidationAudits')
)
BEGIN
    CREATE INDEX IX_EventValidationAudits_Event_Project_CreatedAt
        ON dbo.EventValidationAudits (EventId, ProjectId, CreatedAtUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.ProjectProgresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectProgresses
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        TotalWordsWritten INT NOT NULL,
        RemainingWords INT NOT NULL,
        RemainingPercentage FLOAT NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        [Date] DATETIME2 NOT NULL,
        TimeSpentInMinutes INT NOT NULL,
        WordsWritten INT NOT NULL,
        Notes NVARCHAR(MAX) NULL,
        Minutes INT NOT NULL CONSTRAINT DF_ProjectProgresses_Minutes DEFAULT (0),
        Pages INT NOT NULL CONSTRAINT DF_ProjectProgresses_Pages DEFAULT (0),
        CONSTRAINT PK_ProjectProgresses PRIMARY KEY (Id),
        CONSTRAINT FK_ProjectProgresses_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ProjectProgresses_ProjectId_Date'
      AND object_id = OBJECT_ID(N'dbo.ProjectProgresses')
)
BEGIN
    CREATE INDEX IX_ProjectProgresses_ProjectId_Date
        ON dbo.ProjectProgresses (ProjectId, [Date]);
END
GO

IF OBJECT_ID(N'dbo.ProjectEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectEvents
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        EventId UNIQUEIDENTIFIER NOT NULL,
        TargetWords INT NULL,
        Won BIT NOT NULL CONSTRAINT DF_ProjectEvents_Won DEFAULT (0),
        ValidatedAtUtc DATETIME2 NULL,
        FinalWordCount INT NULL,
        ValidatedWords INT NULL,
        ValidationSource NVARCHAR(50) NULL,
        CONSTRAINT PK_ProjectEvents PRIMARY KEY (Id),
        CONSTRAINT FK_ProjectEvents_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_ProjectEvents_Events_EventId FOREIGN KEY (EventId)
            REFERENCES dbo.Events (Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_ProjectEvents_Project_Event'
      AND object_id = OBJECT_ID(N'dbo.ProjectEvents')
)
BEGIN
    CREATE UNIQUE INDEX UX_ProjectEvents_Project_Event
        ON dbo.ProjectEvents (ProjectId, EventId);
END
GO

IF OBJECT_ID(N'dbo.Milestones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Milestones
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        TargetAmount INT NOT NULL,
        DueDate DATETIME2 NULL,
        AutoGenerated BIT NOT NULL CONSTRAINT DF_Milestones_AutoGenerated DEFAULT (0),
        Completed BIT NOT NULL CONSTRAINT DF_Milestones_Completed DEFAULT (0),
        CompletedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL,
        AchievedAtUtc DATETIME2 NULL,
        Notes NVARCHAR(MAX) NULL,
        [Order] INT NOT NULL CONSTRAINT DF_Milestones_Order DEFAULT (0),
        CONSTRAINT PK_Milestones PRIMARY KEY (Id),
        CONSTRAINT FK_Milestones_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Milestones_Project_Order'
      AND object_id = OBJECT_ID(N'dbo.Milestones')
)
BEGIN
    CREATE INDEX IX_Milestones_Project_Order
        ON dbo.Milestones (ProjectId, [Order]);
END
GO

IF OBJECT_ID(N'dbo.DailyWordLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DailyWordLogs
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        [Date] DATE NOT NULL,
        WordsWritten INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DailyWordLogs_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_DailyWordLogs PRIMARY KEY (Id),
        CONSTRAINT FK_DailyWordLogs_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_DailyWordLogs_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id)
            ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_DailyWordLogs_Project_User_Date'
      AND object_id = OBJECT_ID(N'dbo.DailyWordLogs')
)
BEGIN
    CREATE UNIQUE INDEX UX_DailyWordLogs_Project_User_Date
        ON dbo.DailyWordLogs (ProjectId, UserId, [Date]);
END
GO

IF OBJECT_ID(N'dbo.UserFollows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserFollows
    (
        FollowerId UNIQUEIDENTIFIER NOT NULL,
        FolloweeId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_UserFollows_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserFollows PRIMARY KEY (FollowerId, FolloweeId),
        CONSTRAINT FK_UserFollows_Users_FollowerId FOREIGN KEY (FollowerId)
            REFERENCES dbo.Users (Id)
            ON DELETE NO ACTION,
        CONSTRAINT FK_UserFollows_Users_FolloweeId FOREIGN KEY (FolloweeId)
            REFERENCES dbo.Users (Id)
            ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID(N'dbo.Badges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Badges
    (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        Icon NVARCHAR(200) NOT NULL,
        AwardedAt DATETIME2 NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        EventId UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_Badges PRIMARY KEY (Id),
        CONSTRAINT FK_Badges_Projects_ProjectId FOREIGN KEY (ProjectId)
            REFERENCES dbo.Projects (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_Badges_Events_EventId FOREIGN KEY (EventId)
            REFERENCES dbo.Events (Id)
            ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Badges_ProjectId'
      AND object_id = OBJECT_ID(N'dbo.Badges')
)
BEGIN
    CREATE INDEX IX_Badges_ProjectId ON dbo.Badges (ProjectId);
END
GO
