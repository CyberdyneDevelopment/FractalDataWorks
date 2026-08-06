CREATE TABLE [types].[TypeOption]
(
    [Id] INT NOT NULL,
    [TypeCollectionId] INT NOT NULL,
    [Name] VARCHAR(100) NOT NULL,
    [FullTypeName] VARCHAR(500) NOT NULL,
    [Category] VARCHAR(100) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_types_TypeOption_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_types_TypeOption_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_types_TypeOption_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeOption_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeOption_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_types_TypeOption_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeOption_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeOption_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_TypeOption] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TypeOption_Collection] FOREIGN KEY ([TypeCollectionId])
        REFERENCES [types].[TypeCollection]([Id])
);
GO

CREATE INDEX [IX_TypeOption_TypeCollectionId] ON [types].[TypeOption]([TypeCollectionId]);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'TypeOption metadata for individual types within a TypeCollection. Links types to their parent collection.',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique identifier for the TypeOption (hash of FullTypeName)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Foreign key to the parent TypeCollection',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'TypeCollectionId';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Short name of the type (e.g., MsSql, PostgreSql)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Fully qualified type name including namespace',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'FullTypeName';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Optional category for grouping related types',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'Category';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this is the current version of the record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this record has been soft deleted',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation date from source system (if migrated)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was created',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that created this record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was created',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that last modified this record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeOption',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
