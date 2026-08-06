CREATE TABLE [etl].[PipelineExecution] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [PipelineName] NVARCHAR(100) NOT NULL,
    [ScheduleName] NVARCHAR(100) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Running',
    [StartedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt] DATETIME2 NULL,
    [RecordsExtracted] INT NOT NULL DEFAULT 0,
    [RecordsTransformed] INT NOT NULL DEFAULT 0,
    [RecordsLoaded] INT NOT NULL DEFAULT 0,
    [RecordsFailed] INT NOT NULL DEFAULT 0,
    [DurationMs] BIGINT NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [ExecutedBy] NVARCHAR(100) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_etl_PipelineExecution_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_etl_PipelineExecution_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_PipelineExecution_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_PipelineExecution_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_PipelineExecution_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_PipelineExecution_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_PipelineExecution_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_PipelineExecution_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [CK_PipelineExecution_Status] CHECK ([Status] IN ('Running', 'Succeeded', 'Failed', 'Cancelled'))
)
GO

CREATE INDEX [IX_PipelineExecution_PipelineName] ON [etl].[PipelineExecution]([PipelineName])
GO

CREATE INDEX [IX_PipelineExecution_ScheduleName] ON [etl].[PipelineExecution]([ScheduleName])
GO

CREATE INDEX [IX_PipelineExecution_Status] ON [etl].[PipelineExecution]([Status])
GO

CREATE INDEX [IX_PipelineExecution_StartedAt] ON [etl].[PipelineExecution]([StartedAt] DESC)
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Pipeline execution history and metrics. Tracks each pipeline run with status, timing, and record counts.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE',  @level1name=N'PipelineExecution'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique identifier for the pipeline execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the pipeline that was executed', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'PipelineName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the schedule that triggered this execution (null for manual executions)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'ScheduleName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Current status of the pipeline execution (Running, Succeeded, Failed, Cancelled)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'Status';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when the pipeline execution started', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'StartedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when the pipeline execution completed (null if still running)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'CompletedAt';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records extracted during the pipeline execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'RecordsExtracted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records transformed during the pipeline execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'RecordsTransformed';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records loaded during the pipeline execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'RecordsLoaded';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Number of records that failed during the pipeline execution', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'RecordsFailed';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Total duration of the pipeline execution in milliseconds', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'DurationMs';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Error message if the pipeline execution failed', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'ErrorMessage';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that executed the pipeline', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'ExecutedBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates whether this is the current active version (1) or a historical version (0)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag indicating whether this record has been deleted (1) or is active (0)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system (if applicable)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was created', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that created this record', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was created (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when this record was last modified', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User or service that last modified this record', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'User on whose behalf this record was last modified (for impersonation scenarios)', @level0type=N'SCHEMA', @level0name=N'etl', @level1type=N'TABLE', @level1name=N'PipelineExecution', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
