CREATE TABLE [sched].[ScheduleExecution] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ScheduleId] UNIQUEIDENTIFIER NOT NULL,
    [ScheduleName] NVARCHAR(100) NOT NULL,
    [PipelineName] NVARCHAR(256) NOT NULL,
    [StartTime] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [EndTime] DATETIMEOFFSET NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Running',
    [ResultMessage] NVARCHAR(MAX) NULL,
    [RecordsProcessed] INT NULL,
    [DurationMs] BIGINT NULL,
    [TriggerType] NVARCHAR(50) NOT NULL DEFAULT 'Scheduled',
    [TriggeredBy] NVARCHAR(100) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_sched_ScheduleExecution_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [FK_ScheduleExecution_Schedule] FOREIGN KEY ([ScheduleId])
        REFERENCES [sched].[Schedule]([Id])
)
GO

CREATE INDEX [IX_ScheduleExecution_ScheduleId] ON [sched].[ScheduleExecution]([ScheduleId])
GO

CREATE INDEX [IX_ScheduleExecution_StartTime] ON [sched].[ScheduleExecution]([StartTime] DESC)
GO

CREATE INDEX [IX_ScheduleExecution_Status] ON [sched].[ScheduleExecution]([Status])
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Schedule execution history and metrics. Tracks each scheduled job run with status, timing, and results.',
    @level0type=N'SCHEMA', @level0name=N'sched',
    @level1type=N'TABLE',  @level1name=N'ScheduleExecution'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier for the schedule execution', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Foreign key to the schedule that triggered this execution', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ScheduleId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the schedule (denormalized for easier querying)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ScheduleName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the pipeline that was executed by this schedule', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'PipelineName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this execution started', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'StartTime';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this execution completed (null if still running)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'EndTime';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Current status of the execution (e.g., Running, Succeeded, Failed)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'Status';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Result message from the execution (error details if failed, summary if succeeded)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ResultMessage';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records processed during the execution', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'RecordsProcessed';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Total duration of the execution in milliseconds', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'DurationMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Type of trigger that initiated this execution (e.g., Scheduled, Manual, API)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'TriggerType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that triggered this execution (for manual or API triggers)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'TriggeredBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates whether this is the current active version (1) or a historical version (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag indicating whether this record has been deleted (1) or is active (0)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system (if applicable)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was created', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that created this record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was created (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was last modified', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that last modified this record', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was last modified (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'sched', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
