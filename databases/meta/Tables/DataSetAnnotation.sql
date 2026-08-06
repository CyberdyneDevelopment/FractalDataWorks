CREATE TABLE [meta].[DataSetAnnotation]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_RowId] DEFAULT NEWID(),
    [DataSetName] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [BusinessOwner] NVARCHAR(256) NULL,
    [TechnicalOwner] NVARCHAR(256) NULL,
    [TagsJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_TagsJson] DEFAULT '[]',
    [UpdateFrequency] NVARCHAR(64) NULL,
    [DataClassification] NVARCHAR(64) NULL,
    [FieldDescriptionsJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_FieldDescriptionsJson] DEFAULT '{}',
    [FieldBusinessNamesJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_FieldBusinessNamesJson] DEFAULT '{}',
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_DataSetAnnotation_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_DataSetAnnotation] PRIMARY KEY ([RowId])
);
GO

-- One current annotation per DataSet
CREATE UNIQUE INDEX [UX_DataSetAnnotation_DataSetName_Current] ON [meta].[DataSetAnnotation]([DataSetName]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
GO

CREATE INDEX [IX_DataSetAnnotation_BusinessOwner] ON [meta].[DataSetAnnotation]([BusinessOwner]);
GO

CREATE INDEX [IX_DataSetAnnotation_DataClassification] ON [meta].[DataSetAnnotation]([DataClassification]);
GO

CREATE INDEX [IX_DataSetAnnotation_IsCurrent] ON [meta].[DataSetAnnotation]([IsCurrent]);
GO

CREATE INDEX [IX_DataSetAnnotation_IsDeleted] ON [meta].[DataSetAnnotation]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Metadata annotations for DataSets including ownership, classification, and field descriptions.', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key)', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name of the DataSet being annotated', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'DataSetName';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business description of the DataSet', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'Description';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business owner responsible for the data', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'BusinessOwner';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Technical owner responsible for data pipeline', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'TechnicalOwner';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of tag strings for categorization', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'TagsJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'How often the data is updated (Daily, Hourly, RealTime, etc.)', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'UpdateFrequency';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Data classification level (Public, Internal, Confidential, Restricted)', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'DataClassification';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON object mapping field names to descriptions', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'FieldDescriptionsJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON object mapping field names to business-friendly names', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'FieldBusinessNamesJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'DataSetAnnotation', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO
