CREATE TABLE [cfg].[FederatedDataSet]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_RowId] DEFAULT NEWID(),
    [DataSetId] UNIQUEIDENTIFIER NOT NULL,
    [FederationStrategy] NVARCHAR(100) NOT NULL DEFAULT 'Join',
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_cfg_FederatedDataSet_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_FederatedDataSet] PRIMARY KEY ([RowId]),
    CONSTRAINT [CK_FederatedDataSet_Strategy] CHECK ([FederationStrategy] IN ('Join', 'Union', 'Merge'))
    -- No FK constraint: DataSetId references DataSet.Id which isn't unique (versioning)
);
GO

-- One current version per parent DataSetId
CREATE UNIQUE INDEX [UX_FederatedDataSet_DataSetId_Current] ON [cfg].[FederatedDataSet]([DataSetId]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_FederatedDataSet_DataSetId] ON [cfg].[FederatedDataSet]([DataSetId]);
GO

CREATE INDEX [IX_FederatedDataSet_IsCurrent] ON [cfg].[FederatedDataSet]([IsCurrent]);
GO

CREATE INDEX [IX_FederatedDataSet_IsDeleted] ON [cfg].[FederatedDataSet]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Federated datasets that combine data from multiple sources with join/union/merge strategies. Child of cfg.DataSet.',
    @level0type=N'SCHEMA', @level0name=N'cfg',
    @level1type=N'TABLE',  @level1name=N'FederatedDataSet';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'FK to DataSet.Id (logical, join WHERE IsCurrent=1).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'DataSetId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Strategy for combining data from multiple sources (Join, Union, or Merge).', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'FederationStrategy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Original creation date from source system.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who created the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was created.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'When the record was last modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Database principal who last modified the record.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Application user on whose behalf the record was modified.', @level0type=N'SCHEMA', @level0name=N'cfg', @level1type=N'TABLE', @level1name=N'FederatedDataSet', @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
