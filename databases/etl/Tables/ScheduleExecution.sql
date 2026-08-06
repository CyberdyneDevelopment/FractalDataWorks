CREATE TABLE [etl].[ScheduleExecution] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ScheduleName] NVARCHAR(100) NOT NULL,
    [PipelineName] NVARCHAR(100) NOT NULL,
    [PipelineExecutionId] UNIQUEIDENTIFIER NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Running',
    [ScheduledTime] DATETIME2 NOT NULL,
    [StartedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt] DATETIME2 NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_ScheduleExecution_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [FK_ScheduleExecution_PipelineExecution] FOREIGN KEY ([PipelineExecutionId])
        REFERENCES [etl].[PipelineExecution]([Id]),
    CONSTRAINT [CK_ScheduleExecution_Status] CHECK ([Status] IN ('Running', 'Succeeded', 'Failed', 'Skipped'))
)
GO

CREATE INDEX [IX_ScheduleExecution_ScheduleName] ON [etl].[ScheduleExecution]([ScheduleName])
GO

CREATE INDEX [IX_ScheduleExecution_Status] ON [etl].[ScheduleExecution]([Status])
GO

CREATE INDEX [IX_ScheduleExecution_ScheduledTime] ON [etl].[ScheduleExecution]([ScheduledTime] DESC)
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Schedule execution history. Links scheduled triggers to pipeline executions with timing and status information.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE',  @level1name=N'ScheduleExecution'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier for the schedule execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the schedule that triggered this execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ScheduleName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the pipeline that was scheduled to execute', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'PipelineName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Foreign key to the associated pipeline execution (null if execution was skipped)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'PipelineExecutionId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Current status of the schedule execution (Running, Succeeded, Failed, Skipped)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'Status';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Time when this execution was scheduled to occur', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ScheduledTime';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this schedule execution actually started', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'StartedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this schedule execution completed (null if still running)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CompletedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Error message if the schedule execution failed', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ErrorMessage';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates whether this is the current active version (1) or a historical version (0)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag indicating whether this record has been deleted (1) or is active (0)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system (if applicable)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was created', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that created this record', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was created (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was last modified', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that last modified this record', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was last modified (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'ScheduleExecution', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
