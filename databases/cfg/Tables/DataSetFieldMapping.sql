CREATE TABLE [cfg].[DataSetFieldMapping]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [DataSetSourceId] UNIQUEIDENTIFIER NOT NULL,
    [LogicalFieldName] NVARCHAR(200) NOT NULL,
    [PhysicalFieldName] NVARCHAR(200) NOT NULL,
    [TransformExpression] NVARCHAR(MAX) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_DataSetFieldMapping_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSetFieldMapping] PRIMARY KEY ([RowId])
    -- No FK constraint: DataSetSourceId references DataSetSource.Id which isn't unique (versioning)
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_DataSetFieldMapping_Id_Current] ON [cfg].[DataSetFieldMapping]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per DataSetSource + LogicalFieldName combination
CREATE UNIQUE INDEX [UX_DataSetFieldMapping_DataSetSourceId_LogicalFieldName_Current] ON [cfg].[DataSetFieldMapping]([DataSetSourceId], [LogicalFieldName]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_DataSetFieldMapping_Id] ON [cfg].[DataSetFieldMapping]([Id]);
GO

CREATE INDEX [IX_DataSetFieldMapping_DataSetSourceId] ON [cfg].[DataSetFieldMapping]([DataSetSourceId]);
GO

CREATE INDEX [IX_DataSetFieldMapping_IsCurrent] ON [cfg].[DataSetFieldMapping]([IsCurrent]);
GO

CREATE INDEX [IX_DataSetFieldMapping_IsDeleted] ON [cfg].[DataSetFieldMapping]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Maps logical dataset fields to physical source fields with row-level versioning. Enables field renaming and transformations.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'DataSetFieldMapping';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the field mapping (persists across versions).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataSetSource.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'DataSetSourceId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical field name in the dataset schema.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'LogicalFieldName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Physical field name in the source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'PhysicalFieldName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional transformation expression to apply when mapping.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'TransformExpression';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'DataSetFieldMapping', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
