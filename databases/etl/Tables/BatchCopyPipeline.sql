CREATE TABLE [etl].[BatchCopyPipeline] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [PipelineId] UNIQUEIDENTIFIER NOT NULL,
    [MaxParallelism] INT NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_MaxParallelism] DEFAULT 1,
    [LoadMode] VARCHAR(50) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_LoadMode] DEFAULT 'Append',
    [TruncateBeforeLoad] BIT NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_TruncateBeforeLoad] DEFAULT 0,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_BatchCopyPipeline_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_BatchCopyPipeline] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BatchCopyPipeline_Pipeline] FOREIGN KEY ([PipelineId])
        REFERENCES [etl].[Pipeline]([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_BatchCopyPipeline_PipelineId] ON [etl].[BatchCopyPipeline]([PipelineId])
GO

CREATE INDEX [IX_BatchCopyPipeline_IsCurrent] ON [etl].[BatchCopyPipeline]([IsCurrent])
GO

CREATE INDEX [IX_BatchCopyPipeline_IsDeleted] ON [etl].[BatchCopyPipeline]([IsDeleted])
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Batch copy pipeline configurations. Child table containing BatchCopy-specific properties; common properties in parent etl.Pipeline.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE',  @level1name=N'BatchCopyPipeline'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique identifier for the batch copy pipeline configuration.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'Id'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Foreign key to parent etl.Pipeline record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'PipelineId'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Maximum number of parallel batch operations allowed during execution.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'MaxParallelism'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Load mode strategy: Append, Upsert, Replace, Merge, etc.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'LoadMode'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether to truncate the destination table before loading data.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'TruncateBeforeLoad'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates if this record represents the current active version.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'IsCurrent'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Soft delete flag. When 1, record is logically deleted.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'IsDeleted'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Original creation timestamp from source system (if applicable).',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was created in this database.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'CreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that created the record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'CreateBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'BatchCopyPipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf'
GO
