CREATE TABLE [cfg].[DataSetField]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataSetField_RowId] DEFAULT NEWID(),
    [DataSetId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [TypeName] NVARCHAR(500) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL CONSTRAINT [DF_cfg_DataSetField_Role] DEFAULT ('Attribute'),
    [IsRequired] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetField_IsRequired] DEFAULT (0),
    [MaxLength] INT NULL,
    [Ordinal] INT NOT NULL CONSTRAINT [DF_cfg_DataSetField_Ordinal] DEFAULT (0),
    [Description] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetField_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetField_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetField_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetField_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetField_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetField_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetField_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetField_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSetField] PRIMARY KEY ([RowId])
    -- No FK constraint: DataSetId references DataSet.Id which isn't unique (versioning)
    -- Referential integrity enforced by application + IsCurrent=1 filter
);
GO

-- Unique current version per DataSetId + Name combination
CREATE UNIQUE INDEX [UX_DataSetField_DataSetId_Name_Current] ON [cfg].[DataSetField]([DataSetId], [Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataSetField_DataSetId] ON [cfg].[DataSetField]([DataSetId]);
GO

CREATE INDEX [IX_DataSetField_IsCurrent] ON [cfg].[DataSetField]([IsCurrent]);
GO

CREATE INDEX [IX_DataSetField_IsDeleted] ON [cfg].[DataSetField]([IsDeleted]);
GO

-- Table description
EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Field definitions for datasets. Describes the logical schema of data records. Supports row-level versioning.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE', @level1name=N'DataSetField';
GO

-- Column descriptions
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'RowId';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Foreign key to parent DataSet.Id (logical identity, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'DataSetId';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the field in the logical dataset schema.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'Name';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Fully qualified .NET type name for the field (e.g., System.String, System.Int32).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'TypeName';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Property role for the field (Surrogate, NaturalKey, Lookup, Attribute, Measure).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'Role';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates if this field is required (cannot be null).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'IsRequired';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Maximum length for string fields. NULL for non-string types.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'MaxLength';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Display order for the field within the dataset schema.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'Ordinal';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Description of the field and its purpose.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'Description';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates if this is the current active version of the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Indicates if this record has been soft-deleted.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system (if migrated).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when the record was created in this system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database user who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Timestamp when the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database user who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetField', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
