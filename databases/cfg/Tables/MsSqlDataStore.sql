CREATE TABLE [cfg].[MsSqlDataStore]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_RowId] DEFAULT NEWID(),
    [DataStoreId] UNIQUEIDENTIFIER NOT NULL,
    [SchemaName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_SchemaName] DEFAULT ('dbo'),
    [TablePrefix] NVARCHAR(50) NULL,
    [UseQuotedIdentifiers] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_UseQuotedIdentifiers] DEFAULT (1),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_MsSqlDataStore_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_MsSqlDataStore] PRIMARY KEY ([RowId])
    -- No FK constraint: DataStoreId references DataStore.Id which isn't unique (versioning)
);
GO

-- One current version per parent DataStoreId
CREATE UNIQUE INDEX [UX_MsSqlDataStore_DataStoreId_Current] ON [cfg].[MsSqlDataStore]([DataStoreId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_MsSqlDataStore_DataStoreId] ON [cfg].[MsSqlDataStore]([DataStoreId]);
GO

CREATE INDEX [IX_MsSqlDataStore_SchemaName] ON [cfg].[MsSqlDataStore]([SchemaName]);
GO

CREATE INDEX [IX_MsSqlDataStore_IsCurrent] ON [cfg].[MsSqlDataStore]([IsCurrent]);
GO

CREATE INDEX [IX_MsSqlDataStore_IsDeleted] ON [cfg].[MsSqlDataStore]([IsDeleted]);
GO

-- Table description
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Microsoft SQL Server data store configuration with row-level versioning. Child of cfg.DataStore.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'MsSqlDataStore';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataStore.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'DataStoreId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'SQL Server schema name (default: dbo).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'SchemaName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional prefix for table names.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'TablePrefix';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Use square brackets for identifiers.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'UseQuotedIdentifiers';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'MsSqlDataStore', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
