CREATE TABLE [etl].[Pipeline] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] VARCHAR(256) NOT NULL,
    [PipelineType] VARCHAR(100) NOT NULL,
    [Description] VARCHAR(500) NULL,
    [IsEnabled] BIT NOT NULL CONSTRAINT [DF_etl_Pipeline_IsEnabled] DEFAULT 1,
    [SourceConnectionName] VARCHAR(256) NOT NULL,
    [SourceDataSet] VARCHAR(256) NULL,
    [DestinationConnectionName] VARCHAR(256) NOT NULL,
    [DestinationDataSet] VARCHAR(256) NULL,
    [BatchSize] INT NOT NULL CONSTRAINT [DF_etl_Pipeline_BatchSize] DEFAULT 1000,
    [ContinueOnError] BIT NOT NULL CONSTRAINT [DF_etl_Pipeline_ContinueOnError] DEFAULT 0,
    [MaxErrors] INT NOT NULL CONSTRAINT [DF_etl_Pipeline_MaxErrors] DEFAULT 100,
    [SecretManagerName] VARCHAR(256) NULL,
    [SecretKeyName] VARCHAR(256) NULL,
    [ResiliencyPolicyName] VARCHAR(256) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_etl_Pipeline_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_etl_Pipeline_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_Pipeline_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_Pipeline_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_Pipeline_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_etl_Pipeline_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_Pipeline_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_etl_Pipeline_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_Pipeline] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Pipeline_Name] UNIQUE ([Name])
)
GO

CREATE INDEX [IX_Pipeline_Name] ON [etl].[Pipeline]([Name])
GO

CREATE INDEX [IX_Pipeline_PipelineType] ON [etl].[Pipeline]([PipelineType])
GO

CREATE INDEX [IX_Pipeline_IsCurrent] ON [etl].[Pipeline]([IsCurrent])
GO

CREATE INDEX [IX_Pipeline_IsDeleted] ON [etl].[Pipeline]([IsDeleted])
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Base ETL pipeline configurations for all pipeline types. Parent table in type-per-hierarchy pattern.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE',  @level1name=N'Pipeline'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique identifier for the pipeline.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'Id'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Unique name identifying the pipeline configuration.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'Name'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Pipeline type discriminator: BatchCopy, Streaming, etc.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'PipelineType'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional description of the pipeline purpose and behavior.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'Description'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether the pipeline is enabled for execution.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'IsEnabled'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Name of the source connection for data extraction.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'SourceConnectionName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional name of the source dataset or query to read from.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'SourceDataSet'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Name of the destination connection for data loading.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'DestinationConnectionName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional name of the destination dataset or table to write to.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'DestinationDataSet'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Number of records to process per batch.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'BatchSize'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates whether to continue processing when errors occur.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'ContinueOnError'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Maximum number of errors allowed before pipeline execution stops.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'MaxErrors'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional secret manager service name for credential resolution.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'SecretManagerName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional secret key name to retrieve credentials.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'SecretKeyName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Optional resiliency policy name for retry and circuit breaker logic.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'ResiliencyPolicyName'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Indicates if this record represents the current active version.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'IsCurrent'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Soft delete flag. When 1, record is logically deleted.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'IsDeleted'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Original creation timestamp from source system (if applicable).',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was created in this database.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'CreateDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that created the record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'CreateBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'UTC timestamp when the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyDate'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Database user or service account that last modified the record.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyBy'
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description', @value=N'Application user on whose behalf the record was last modified.',
    @level0type=N'SCHEMA', @level0name=N'etl',
    @level1type=N'TABLE', @level1name=N'Pipeline',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf'
GO
