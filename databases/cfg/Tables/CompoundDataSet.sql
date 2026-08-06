CREATE TABLE [cfg].[CompoundDataSet]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_RowId] DEFAULT NEWID(),
    [DataSetId] UNIQUEIDENTIFIER NOT NULL,
    [TransformExpression] NVARCHAR(MAX) NULL,
    [SourceDataSetName] NVARCHAR(200) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_CompoundDataSet_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_CompoundDataSet] PRIMARY KEY ([RowId])
    -- No FK constraint: DataSetId references DataSet.Id which isn't unique (versioning)
);
GO

-- One current version per parent DataSetId
CREATE UNIQUE INDEX [UX_CompoundDataSet_DataSetId_Current] ON [cfg].[CompoundDataSet]([DataSetId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_CompoundDataSet_DataSetId] ON [cfg].[CompoundDataSet]([DataSetId]);
GO

CREATE INDEX [IX_CompoundDataSet_IsCurrent] ON [cfg].[CompoundDataSet]([IsCurrent]);
GO

CREATE INDEX [IX_CompoundDataSet_IsDeleted] ON [cfg].[CompoundDataSet]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Compound datasets derived from other datasets through transformations or aggregations. Child of cfg.DataSet.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'CompoundDataSet';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataSet.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'DataSetId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Transformation expression or calculation applied to source dataset(s).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'TransformExpression';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the source dataset used in the transformation.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'SourceDataSetName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'CompoundDataSet', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
