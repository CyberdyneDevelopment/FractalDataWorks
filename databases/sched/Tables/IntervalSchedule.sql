CREATE TABLE [sched].[IntervalSchedule] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [IntervalSeconds] INT NOT NULL DEFAULT 3600,
    [RunOnStartup] BIT NOT NULL DEFAULT 0,
    [DelayAfterCompletion] BIT NOT NULL DEFAULT 0,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_IntervalSchedule_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [FK_IntervalSchedule_Schedule] FOREIGN KEY ([Id])
        REFERENCES [sched].[Schedule]([Id]) ON DELETE CASCADE
)
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Interval-based schedule configuration. Child table extending sched.Schedule with interval-specific properties.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE',  @level1name=N'IntervalSchedule'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier and foreign key to sched.Schedule', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Interval in seconds between scheduled executions (default 3600 = 1 hour)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'IntervalSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether the schedule should trigger immediately on startup (1) or wait for first interval (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'RunOnStartup';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether to delay the next interval after execution completes (1) or schedule from start time (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'DelayAfterCompletion';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates whether this is the current active version (1) or a historical version (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag indicating whether this record has been deleted (1) or is active (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system (if applicable)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was created', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that created this record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was created (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was last modified', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that last modified this record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was last modified (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'IntervalSchedule', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
