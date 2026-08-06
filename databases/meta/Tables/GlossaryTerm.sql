CREATE TABLE [meta].[GlossaryTerm]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_RowId] DEFAULT NEWID(),
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Definition] NVARCHAR(2000) NOT NULL,
    [Formula] NVARCHAR(1000) NULL,
    [Category] NVARCHAR(100) NOT NULL,
    [Owner] NVARCHAR(256) NOT NULL,
    [Steward] NVARCHAR(256) NULL,
    [RelatedTermsJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_RelatedTermsJson] DEFAULT '[]',
    [LinkedDataSetsJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_LinkedDataSetsJson] DEFAULT '[]',
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_meta_GlossaryTerm_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_GlossaryTerm] PRIMARY KEY ([RowId])
);
GO

-- One current version per logical Id
CREATE UNIQUE INDEX [UX_GlossaryTerm_Id_Current] ON [meta].[GlossaryTerm]([Id]) WHERE [IsCurrent] = 1;
GO

-- One current version per Name
CREATE UNIQUE INDEX [UX_GlossaryTerm_Name_Current] ON [meta].[GlossaryTerm]([Name]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
GO

CREATE INDEX [IX_GlossaryTerm_Category] ON [meta].[GlossaryTerm]([Category]);
GO

CREATE INDEX [IX_GlossaryTerm_Owner] ON [meta].[GlossaryTerm]([Owner]);
GO

CREATE INDEX [IX_GlossaryTerm_IsCurrent] ON [meta].[GlossaryTerm]([IsCurrent]);
GO

CREATE INDEX [IX_GlossaryTerm_IsDeleted] ON [meta].[GlossaryTerm]([IsDeleted]);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business glossary terms with definitions, ownership, and relationships to DataSets.', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Version-specific row identifier (primary key)', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'RowId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Logical identity of the term (persists across versions)', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Unique term name', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business definition of the term', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Definition';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Optional formula or calculation for metrics', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Formula';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Category for grouping related terms', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Category';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Business owner of the term', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Owner';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Data steward responsible for term accuracy', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'Steward';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of related term names', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'RelatedTermsJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'JSON array of linked DataSet names', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'LinkedDataSetsJson';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this is the current version of the record', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Soft delete flag', @level0type=N'SCHEMA', @level0name=N'meta', @level1type=N'TABLE', @level1name=N'GlossaryTerm', @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO
