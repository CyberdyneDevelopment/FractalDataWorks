CREATE TABLE [types].[TypeCollection]
(
    [Id] INT NOT NULL,
    [Name] VARCHAR(100) NOT NULL,
    [FullName] VARCHAR(500) NOT NULL,
    [CollectionKind] VARCHAR(50) NOT NULL,
    [ServiceCategory] VARCHAR(100) NULL,
    [IsCurrent] BIT NOT NULL CONSTRAINT [DF_types_TypeCollection_IsCurrent] DEFAULT (1),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_types_TypeCollection_IsDeleted] DEFAULT (0),
    [SrcCreateDate] DATETIMEOFFSET(0) NULL,
    [CreateDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_types_TypeCollection_CreateDate] DEFAULT (sysdatetimeoffset()),
    [CreateBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeCollection_CreateBy] DEFAULT (suser_sname()),
    [CreateOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeCollection_CreateOnBehalfOf] DEFAULT (suser_sname()),
    [ModifyDate] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_types_TypeCollection_ModifyDate] DEFAULT (sysdatetimeoffset()),
    [ModifyBy] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeCollection_ModifyBy] DEFAULT (suser_sname()),
    [ModifyOnBehalfOf] NVARCHAR(128) NOT NULL CONSTRAINT [DF_types_TypeCollection_ModifyOnBehalfOf] DEFAULT (suser_sname()),
    CONSTRAINT [PK_TypeCollection] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_TypeCollection_FullName] UNIQUE ([FullName])
);
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'TypeCollection metadata for runtime type discovery. Stores collection-level information for TypeCollections and ServiceTypeCollections.',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Unique identifier for the TypeCollection (hash of FullName)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'Id';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Short name of the collection class (e.g., ConnectionTypes)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'Name';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Fully qualified type name including namespace',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'FullName';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Kind of collection: TypeCollection, ServiceTypeCollection, MutableTypeCollection, etc.',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'CollectionKind';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Service category for ServiceTypeCollections (e.g., Connection, DataStore)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'ServiceCategory';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this is the current version of the record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'IsCurrent';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Flag indicating whether this record has been soft deleted',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'IsDeleted';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Original creation date from source system (if migrated)',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'SrcCreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was created',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'CreateDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that created this record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'CreateBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was created',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'CreateOnBehalfOf';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'Timestamp when this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'ModifyDate';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User or service account that last modified this record',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'ModifyBy';
GO

EXEC sys.sp_addextendedproperty
    @name=N'MS_Description',
    @value=N'User on whose behalf this record was last modified',
    @level0type=N'SCHEMA', @level0name=N'types',
    @level1type=N'TABLE',  @level1name=N'TypeCollection',
    @level2type=N'COLUMN', @level2name=N'ModifyOnBehalfOf';
GO
