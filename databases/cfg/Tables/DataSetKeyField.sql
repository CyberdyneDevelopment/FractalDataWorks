CREATE TABLE [cfg].[DataSetKeyField]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [DataSetId] UNIQUEIDENTIFIER NOT NULL,
    [FieldName] NVARCHAR(200) NOT NULL,
    [KeyType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_KeyType] DEFAULT ('Surrogate'),
    [Ordinal] INT NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_Ordinal] DEFAULT (0),
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetKeyField_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSetKeyField] PRIMARY KEY ([RowId])
    -- No FK constraint: DataSetId references DataSet.Id which isn't unique (versioning)
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataSetKeyField_Id_Current] ON [cfg].[DataSetKeyField]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per DataSet + FieldName combination
CREATE UNIQUE INDEX [UX_DataSetKeyField_DataSetId_FieldName_Current] ON [cfg].[DataSetKeyField]([DataSetId], [FieldName]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataSetKeyField_Id] ON [cfg].[DataSetKeyField]([Id]);
GO

CREATE INDEX [IX_DataSetKeyField_DataSetId] ON [cfg].[DataSetKeyField]([DataSetId]);
GO

CREATE INDEX [IX_DataSetKeyField_IsCurrent] ON [cfg].[DataSetKeyField]([IsCurrent]);
GO

CREATE INDEX [IX_DataSetKeyField_IsDeleted] ON [cfg].[DataSetKeyField]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Key field definitions for datasets with row-level versioning. Identifies which fields form the primary key.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'DataSetKeyField';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the key field definition (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataSet.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'DataSetId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the field that is part of the primary key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'FieldName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Type of key (Surrogate for auto-generated, Natural for business identifiers).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'KeyType';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Position of this field within the composite key.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'Ordinal';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetKeyField', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
