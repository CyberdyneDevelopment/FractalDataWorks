CREATE TABLE [cfg].[DataSetSource]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataSetSource_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [DataSetId] UNIQUEIDENTIFIER NOT NULL,
    [SourceName] NVARCHAR(200) NOT NULL,
    [DataStoreName] NVARCHAR(200) NULL,
    [ConnectionName] NVARCHAR(200) NOT NULL,
    [ConnectionType] NVARCHAR(100) NOT NULL DEFAULT 'MsSql',
    [Priority] INT NOT NULL DEFAULT 100,
    [EstimatedCost] INT NOT NULL DEFAULT 50,
    [SupportsPredicatePushdown] BIT NOT NULL DEFAULT 1,
    [SqlSchemaName] NVARCHAR(128) NULL,
    [SqlTableName] NVARCHAR(128) NULL,
    [HttpEndpoint] NVARCHAR(500) NULL,
    [HttpMethod] NVARCHAR(10) NULL,
    [FilePath] NVARCHAR(500) NULL,
    [FileFormat] NVARCHAR(50) NULL,
    [MapperTypeName] NVARCHAR(100) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetSource_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetSource_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetSource_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetSource_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetSource_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetSource_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetSource_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetSource_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSetSource] PRIMARY KEY ([RowId])
    -- No FK constraint: DataSetId references DataSet.Id which isn't unique (versioning)
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataSetSource_Id_Current] ON [cfg].[DataSetSource]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per DataSet + SourceName combination
CREATE UNIQUE INDEX [UX_DataSetSource_DataSetId_SourceName_Current] ON [cfg].[DataSetSource]([DataSetId], [SourceName]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataSetSource_Id] ON [cfg].[DataSetSource]([Id]);
GO

CREATE INDEX [IX_DataSetSource_DataSetId] ON [cfg].[DataSetSource]([DataSetId]);
GO

CREATE INDEX [IX_DataSetSource_ConnectionName] ON [cfg].[DataSetSource]([ConnectionName]);
GO

CREATE INDEX [IX_DataSetSource_IsCurrent] ON [cfg].[DataSetSource]([IsCurrent]);
GO

CREATE INDEX [IX_DataSetSource_IsDeleted] ON [cfg].[DataSetSource]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Physical source mappings for datasets with row-level versioning. Supports multiple sources per dataset with priority and cost.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'DataSetSource';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the source mapping (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataSet.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'DataSetId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique name for this source within the dataset.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'SourceName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional reference to a named DataStore.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'DataStoreName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Connection name to use for this source.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'ConnectionName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Connection type discriminator (MsSql, Http, File).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'ConnectionType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Source selection priority (lower = higher priority).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'Priority';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Estimated relative cost for query optimization.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'EstimatedCost';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this source supports pushing filters to the backend.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'SupportsPredicatePushdown';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server schema name for SQL sources.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'SqlSchemaName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server table or view name for SQL sources.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'SqlTableName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'HTTP endpoint URL for REST sources.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'HttpEndpoint';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'HTTP method (GET, POST) for REST sources.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'HttpMethod';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'File path for file-based sources.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'FilePath';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'File format (CSV, JSON, Parquet) for file sources.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'FileFormat';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Custom mapper type name for complex transformations.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'MapperTypeName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetSource', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
