CREATE TABLE [cfg].[DataContainer]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataContainer_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [DataPathId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [ContainerType] NVARCHAR(50) NOT NULL DEFAULT 'Table',
    [Format] NVARCHAR(50) NOT NULL DEFAULT 'Tabular',
    [SchemaHash] NVARCHAR(64) NULL,
    [SurrogateKeyName] NVARCHAR(256) NULL,
    [SurrogateKeyColumns] NVARCHAR(MAX) NULL,
    [NaturalKeyName] NVARCHAR(256) NULL,
    [NaturalKeyColumns] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataContainer_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataContainer_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataContainer_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainer_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainer_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataContainer_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainer_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainer_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataContainer] PRIMARY KEY ([RowId])
    -- No FK constraint: DataPathId references DataPath.Id which isn't unique (versioning)
    -- Referential integrity enforced by application + IsCurrent=1 filter
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataContainer_Id_Current] ON [cfg].[DataContainer]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per DataPathId+Name combination
CREATE UNIQUE INDEX [UX_DataContainer_DataPathId_Name_Current] ON [cfg].[DataContainer]([DataPathId], [Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataContainer_Id] ON [cfg].[DataContainer]([Id]);
GO

CREATE INDEX [IX_DataContainer_DataPathId] ON [cfg].[DataContainer]([DataPathId]);
GO

CREATE INDEX [IX_DataContainer_IsCurrent] ON [cfg].[DataContainer]([IsCurrent]);
GO

CREATE INDEX [IX_DataContainer_IsDeleted] ON [cfg].[DataContainer]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Container definitions with row-level versioning. Represents data structures (tables, views, endpoints, files). Physical storage format for data.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'DataContainer';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the data container (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataPath.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'DataPathId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the container (e.g., table name, view name, file name).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Type of container (e.g., Table, View, Endpoint, File).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'ContainerType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Format of data stored in the container (e.g., Tabular, JSON, XML, CSV).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'Format';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Hash of the container schema for change detection.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'SchemaHash';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the surrogate key constraint.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'SurrogateKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of column names forming the surrogate key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'SurrogateKeyColumns';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the natural key constraint.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'NaturalKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of column names forming the natural key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'NaturalKeyColumns';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainer', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
