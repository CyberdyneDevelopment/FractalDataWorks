CREATE TABLE [cfg].[DataContainerField]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataContainerField_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [DataContainerId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [TypeName] NVARCHAR(100) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL DEFAULT 'Attribute',
    [IsNullable] BIT NOT NULL DEFAULT 1,
    [IsPrimaryKey] BIT NOT NULL DEFAULT 0,
    [IsIdentity] BIT NOT NULL DEFAULT 0,
    [IsComputed] BIT NOT NULL DEFAULT 0,
    [Description] NVARCHAR(MAX) NULL,
    [Ordinal] INT NOT NULL DEFAULT 0,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataContainerField_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataContainerField_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataContainerField_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainerField_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainerField_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataContainerField_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainerField_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataContainerField_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataContainerField] PRIMARY KEY ([RowId])
    -- No FK constraint: DataContainerId references DataContainer.Id which isn't unique (versioning)
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataContainerField_Id_Current] ON [cfg].[DataContainerField]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per DataContainerId+Name combination
CREATE UNIQUE INDEX [UX_DataContainerField_DataContainerId_Name_Current] ON [cfg].[DataContainerField]([DataContainerId], [Name]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataContainerField_Id] ON [cfg].[DataContainerField]([Id]);
GO

CREATE INDEX [IX_DataContainerField_DataContainerId] ON [cfg].[DataContainerField]([DataContainerId]);
GO

CREATE INDEX [IX_DataContainerField_IsCurrent] ON [cfg].[DataContainerField]([IsCurrent]);
GO

CREATE INDEX [IX_DataContainerField_IsDeleted] ON [cfg].[DataContainerField]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Field/column definitions with row-level versioning. Describes schema structure of containers.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'DataContainerField';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the field (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataContainer.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'DataContainerId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the field.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Data type of the field.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'TypeName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Semantic role (Attribute, Key, Measure, Dimension).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'Role';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether null values are allowed.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'IsNullable';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is a primary key field.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'IsPrimaryKey';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is an identity column.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'IsIdentity';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is a computed column.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'IsComputed';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Description of the field.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Position in schema (0-based).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'Ordinal';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataContainerField', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
