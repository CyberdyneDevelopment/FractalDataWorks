CREATE TABLE [sched].[CronSchedule] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [CronExpression] NVARCHAR(100) NOT NULL DEFAULT '0 * * * *',
    [IncludeSeconds] BIT NOT NULL DEFAULT 0,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_sched_CronSchedule_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_sched_CronSchedule_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_CronSchedule_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_CronSchedule_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_CronSchedule_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_CronSchedule_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_CronSchedule_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_CronSchedule_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [FK_CronSchedule_Schedule] FOREIGN KEY ([Id])
        REFERENCES [sched].[Schedule]([Id]) ON DELETE CASCADE
)
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Cron-based schedule configuration. Child table extending sched.Schedule with cron expression properties.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE',  @level1name=N'CronSchedule'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier and foreign key to sched.Schedule', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Cron expression defining when the schedule should trigger (e.g., 0 * * * * for every hour)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'CronExpression';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether the cron expression includes seconds precision (1) or only minutes precision (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'IncludeSeconds';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates whether this is the current active version (1) or a historical version (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag indicating whether this record has been deleted (1) or is active (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system (if applicable)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was created', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that created this record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was created (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was last modified', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that last modified this record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was last modified (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'CronSchedule', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
