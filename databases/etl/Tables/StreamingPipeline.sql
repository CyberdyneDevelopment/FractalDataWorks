CREATE TABLE [etl].[StreamingPipeline] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [PipelineId] UNIQUEIDENTIFIER NOT NULL,
    [BufferSize] INT NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_BufferSize] DEFAULT 1000,
    [FlushIntervalMs] INT NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_FlushIntervalMs] DEFAULT 5000,
    [UseWindowing] BIT NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_UseWindowing] DEFAULT 0,
    [WindowDurationSeconds] INT NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_WindowDurationSeconds] DEFAULT 60,
    [MaxRecordsPerSecond] INT NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_StreamingPipeline_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_StreamingPipeline] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StreamingPipeline_Pipeline] FOREIGN KEY ([PipelineId])
        REFERENCES [etl].[Pipeline]([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_StreamingPipeline_PipelineId] ON [etl].[StreamingPipeline]([PipelineId])
GO

CREATE INDEX [IX_StreamingPipeline_IsCurrent] ON [etl].[StreamingPipeline]([IsCurrent])
GO

CREATE INDEX [IX_StreamingPipeline_IsDeleted] ON [etl].[StreamingPipeline]([IsDeleted])
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Streaming pipeline configurations. Child table containing Streaming-specific properties; common properties in parent etl.Pipeline.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE',  @level1name=N'StreamingPipeline'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique identifier for the streaming pipeline configuration.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'Id'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Foreign key to parent etl.Pipeline record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'PipelineId'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Number of records to hold in memory buffer before flushing to destination.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'BufferSize'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Maximum time interval in milliseconds before flushing buffered records.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'FlushIntervalMs'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether to use time-based windowing for stream processing.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'UseWindowing'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Duration of each processing window in seconds when windowing is enabled.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'WindowDurationSeconds'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional maximum number of records to process per second for rate limiting.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'MaxRecordsPerSecond'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates if this record represents the current active version.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'IsCurrent'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Soft delete flag. When 1, record is logically deleted.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'IsDeleted'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Original creation timestamp from source system (if applicable).',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was created in this database.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'CreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that created the record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'CreateBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'StreamingPipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf'
GO
