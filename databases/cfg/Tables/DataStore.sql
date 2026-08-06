CREATE TABLE [cfg].[DataStore]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataStore_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [ServiceOptionType] NVARCHAR(100) NULL,
    [ConnectionId] UNIQUEIDENTIFIER NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [SchemaHash] NVARCHAR(64) NULL,
    [LastSchemaImport] DATETIME2 NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataStore_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataStore_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataStore_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataStore_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataStore_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataStore_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataStore_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataStore_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataStore] PRIMARY KEY ([RowId])
    -- No FK constraint: ConnectionId references Connection.Id which isn't unique (versioning)
    -- Referential integrity enforced by application + IsCurrent=1 filter
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataStore_Id_Current] ON [cfg].[DataStore]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Name
CREATE UNIQUE INDEX [UX_DataStore_Name_Current] ON [cfg].[DataStore]([Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataStore_Id] ON [cfg].[DataStore]([Id]);
GO

CREATE INDEX [IX_DataStore_ConnectionId] ON [cfg].[DataStore]([ConnectionId]);
GO

CREATE INDEX [IX_DataStore_IsCurrent] ON [cfg].[DataStore]([IsCurrent]);
GO

CREATE INDEX [IX_DataStore_IsDeleted] ON [cfg].[DataStore]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'DataStore configuration with row-level versioning. Defines physical storage locations accessible via Connections. A DataStore represents WHERE data lives.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'DataStore';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the data store (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique name identifying this data store (e.g., ConfigurationDb, NflStatsDb).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Service option type for this data store (e.g., MsSql, Rest, File).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'ServiceOptionType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to Connection.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'ConnectionId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional description of this data store.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Hash of the complete data store schema for change detection.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'SchemaHash';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp of the last schema import operation for this data store.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'LastSchemaImport';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataStore', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
