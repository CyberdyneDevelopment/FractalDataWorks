CREATE TABLE [sched].[Scheduling] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [ServiceOptionType] NVARCHAR(100) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [MaxConcurrency] INT NOT NULL CONSTRAINT [DF_sched_Scheduling_MaxConcurrency] DEFAULT 10,
    [DefaultTimeoutSeconds] INT NOT NULL CONSTRAINT [DF_sched_Scheduling_DefaultTimeoutSeconds] DEFAULT 3600,
    [PersistJobHistory] BIT NOT NULL CONSTRAINT [DF_sched_Scheduling_PersistJobHistory] DEFAULT 0,
    [PersistenceConnectionString] NVARCHAR(500) NULL,
    [EnableClustering] BIT NOT NULL CONSTRAINT [DF_sched_Scheduling_EnableClustering] DEFAULT 0,
    [ClusterInstanceId] NVARCHAR(100) NULL,
    [MisfireThresholdSeconds] INT NOT NULL CONSTRAINT [DF_sched_Scheduling_MisfireThresholdSeconds] DEFAULT 60,
    [EnableDetailedLogging] BIT NOT NULL CONSTRAINT [DF_sched_Scheduling_EnableDetailedLogging] DEFAULT 0,
    [SecretManagerName] NVARCHAR(100) NULL,
    [SecretKeyName] NVARCHAR(100) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_sched_Scheduling_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_sched_Scheduling_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_Scheduling_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Scheduling_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Scheduling_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_Scheduling_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Scheduling_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_Scheduling_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_Scheduling] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Scheduling_Name] UNIQUE ([Name])
)
GO

CREATE INDEX [IX_Scheduling_Name] ON [sched].[Scheduling]([Name])
GO

CREATE INDEX [IX_Scheduling_IsCurrent] ON [sched].[Scheduling]([IsCurrent])
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Main scheduling service configuration. Defines scheduler behavior, clustering, and job persistence settings.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE',  @level1name=N'Scheduling'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique identifier for the scheduling service configuration.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'Id'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique name identifying the scheduling service configuration.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'Name'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional service type name for specific scheduler implementations.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'ServiceOptionType'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional description of the scheduling service configuration.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'Description'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Maximum number of jobs allowed to execute concurrently.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'MaxConcurrency'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Default timeout in seconds for job execution before cancellation.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'DefaultTimeoutSeconds'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether to persist job execution history to a database.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'PersistJobHistory'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional connection string for job history persistence database.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'PersistenceConnectionString'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether to enable clustering for distributed scheduler execution.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'EnableClustering'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional unique identifier for this cluster instance when clustering is enabled.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'ClusterInstanceId'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Threshold in seconds after which a missed trigger is considered a misfire.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'MisfireThresholdSeconds'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether to enable detailed diagnostic logging for job execution.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'EnableDetailedLogging'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional secret manager service name for credential resolution.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'SecretManagerName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional secret key name to retrieve credentials from secret manager.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'SecretKeyName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates if this record represents the current active version.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'IsCurrent'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Soft delete flag. When 1, record is logically deleted.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'IsDeleted'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Original creation timestamp from source system (if applicable).',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was created in this database.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'CreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that created the record.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'CreateBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'ModifyDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'ModifyBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE', @level1name=N'Scheduling',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf'
GO
