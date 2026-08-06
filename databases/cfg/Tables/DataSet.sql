CREATE TABLE [cfg].[DataSet]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataSet_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [ServiceOptionType] NVARCHAR(100) NOT NULL DEFAULT 'Standard',
    [Description] NVARCHAR(MAX) NULL,
    [Version] NVARCHAR(50) NOT NULL DEFAULT '1.0.0',
    [Category] NVARCHAR(100) NOT NULL DEFAULT 'Dataset',
    [RecordTypeName] NVARCHAR(500) NULL,
    [SurrogateKeyName] NVARCHAR(256) NULL,
    [SurrogateKeyFields] NVARCHAR(MAX) NULL,
    [NaturalKeyName] NVARCHAR(256) NULL,
    [NaturalKeyFields] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataSet_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataSet_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSet_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSet_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSet_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSet_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSet_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSet_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSet] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataSet_Id_Current] ON [cfg].[DataSet]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Name
CREATE UNIQUE INDEX [UX_DataSet_Name_Current] ON [cfg].[DataSet]([Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataSet_Id] ON [cfg].[DataSet]([Id]);
GO

CREATE INDEX [IX_DataSet_ServiceOptionType] ON [cfg].[DataSet]([ServiceOptionType]);
GO

CREATE INDEX [IX_DataSet_Category] ON [cfg].[DataSet]([Category]);
GO

CREATE INDEX [IX_DataSet_IsCurrent] ON [cfg].[DataSet]([IsCurrent]);
GO

CREATE INDEX [IX_DataSet_IsDeleted] ON [cfg].[DataSet]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical dataset definitions with schema and metadata. Maps to physical sources via DataSetSource. Supports row-level versioning.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key)', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the dataset (persists across versions)', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique display name for the dataset', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Dataset type discriminator (Standard, Federated, Compound)', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'ServiceOptionType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional description of the dataset purpose', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Schema version for tracking changes', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'Version';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical grouping category for the dataset', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'Category';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Full .NET type name for strongly-typed record mapping', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'RecordTypeName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the surrogate key definition (e.g., PK_DataSetName).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'SurrogateKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of field names forming the surrogate (auto-generated) key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'SurrogateKeyFields';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the natural key definition (e.g., AK_DataSetName).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'NaturalKeyName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of field names forming the natural (business) key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'NaturalKeyFields';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSet', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
